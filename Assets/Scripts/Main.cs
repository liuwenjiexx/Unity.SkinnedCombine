using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public class AvatarRes
{
    public string mName;
    public GameObject mSkeleton;
    public List<AnimationClip> mAnimList = new List<AnimationClip>();

    public int mEyesIdx = 0;
    public int mFaceIdx = 0;
    public int mHairIdx = 0;
    public int mPantsIdx = 0;
    public int mShoesIdx = 0;
    public int mTopIdx = 0;
    public int mAnimIdx = 0;

    public AvatarPartInfo[] awatarParts;
    public int[] selectedIndexs;



    public void Reset()
    {
        mEyesIdx = 0;
        mFaceIdx = 0;
        mHairIdx = 0;
        mPantsIdx = 0;
        mShoesIdx = 0;
        mTopIdx = 0;
        mAnimIdx = 0;
    }

    public void AddAnimIdx()
    {
        mAnimIdx++;
        if (mAnimIdx >= mAnimList.Count)
            mAnimIdx = 0;
    }

    public void ReduceAnimIdx()
    {
        mAnimIdx--;
        if (mAnimIdx < 0)
            mAnimIdx = mAnimList.Count - 1;
    }

}
public class AvatarPartInfo
{
    public string partName;
    public GameObject[] parts;
}

public class Main : MonoBehaviour
{
    #region 常量

    private const string SkeletonName = "skeleton";
    private const string EyesName = "eyes";
    private const string FaceName = "face";
    private const string HairName = "hair";
    private const string PantsName = "pants";
    private const string ShoesName = "shoes";
    private const string TopName = "top";

    private const int typeWidth = 120;
    private const int typeheight = 25;
    private const int buttonWidth = 25;

    #endregion

    #region 变量

    private List<AvatarRes> mAvatarResList = new List<AvatarRes>();
    private AvatarRes mAvatarRes = null;
    private int mAvatarResIdx = 0;

    private Character mCharacter = null;
    [SerializeField]
    private bool mCombine = false;
    private SkinnedMeshComposite skinned;

    #endregion

    #region 内置函数

    // Use this for initialization
    void Start()
    {
        CreateAllAvatarRes();
        InitCharacter();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public float uiScale = 1f;

    private void OnGUI()
    {
        GUI.matrix = Matrix4x4.Scale(Vector3.one * uiScale);

   
        GUILayout.BeginArea(new Rect(10, 10, typeWidth + 2 * buttonWidth + 8, 1000));

        if (GUILayout.Toggle(skinned.IsCombine, "Combine") != skinned.IsCombine)
        {
            skinned.IsCombine = !skinned.IsCombine;
        }

        // Buttons for changing the active character.
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("<", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
        {
            ReduceAvatarRes();
            ResetSkin(mAvatarRes);
        }

        GUILayout.Box("Character", GUILayout.Width(typeWidth), GUILayout.Height(typeheight));

        if (GUILayout.Button(">", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
        {
            AddAvatarRes();
            ResetSkin(mAvatarRes);
        }

        GUILayout.EndHorizontal();

        // Buttons for changing character elements.

        for (int i = 0; i < mAvatarRes.awatarParts.Length; i++)
        {
            var partInfo = mAvatarRes.awatarParts[i];
            AddCategory(i, partInfo.partName, null);
        }


        // anim
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("<", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
        {
            mAvatarRes.ReduceAnimIdx();
            mCharacter.ChangeAnim(mAvatarRes);
        }

        GUILayout.Box("Anim", GUILayout.Width(typeWidth), GUILayout.Height(typeheight));

        if (GUILayout.Button(">", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
        {
            mAvatarRes.AddAnimIdx();
            mCharacter.ChangeAnim(mAvatarRes);
        }

        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    // Draws buttons for configuring a specific category of items, like pants or shoes.
    void AddCategory(int parttype, string displayName, string anim)
    {
        GUILayout.BeginHorizontal();
        int selectedIndex;
        selectedIndex = mAvatarRes.selectedIndexs[parttype];
        var part = mAvatarRes.awatarParts[parttype];

        if (GUILayout.Button("<", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
        {
            selectedIndex--;
            selectedIndex = (selectedIndex + part.parts.Length) % part.parts.Length;

        }

        GUILayout.Box(displayName, GUILayout.Width(typeWidth), GUILayout.Height(typeheight));

        if (GUILayout.Button(">", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
        {
            selectedIndex++;
            selectedIndex = (selectedIndex + part.parts.Length) % part.parts.Length;

        }

        if (selectedIndex != mAvatarRes.selectedIndexs[parttype])
        {
            mAvatarRes.selectedIndexs[parttype] = selectedIndex;
            skinned.AddPart(part.partName, part.parts[selectedIndex]);
        }

        GUILayout.EndHorizontal();
    }

    #endregion

    #region 函数

    private void InitCharacter()
    {
        GameObject go = new GameObject();
        mCharacter = go.AddComponent<Character>();
        mCharacter.SetName("Character");

        mAvatarRes = mAvatarResList[mAvatarResIdx];
       
        skinned = go.AddComponent<SkinnedMeshComposite>();
        skinned.IsCombine = mCombine;
        ResetSkin(mAvatarRes);
    }

    void ResetSkin(AvatarRes avatarRes)
    {
        mCharacter.GenerateSkeleton(mAvatarRes);

        skinned.SkeletonRoot = mCharacter.mSkeleton.transform;

        skinned.Clear();
        for (int i = 0; i < avatarRes.awatarParts.Length; i++)
        {
            var part = avatarRes.awatarParts[i];
            int index = avatarRes.selectedIndexs[i];
            skinned.AddPart(part.partName, part.parts[index]);
        }

        skinned.GennerateSkin();
    }


    private void CreateAllAvatarRes()
    {
        DirectoryInfo dir = new DirectoryInfo("Assets/Resources/");
        foreach (var subdir in dir.GetDirectories())
        {
            string[] splits = subdir.Name.Split('/');
            string dirname = splits[splits.Length - 1];

            GameObject[] golist = Resources.LoadAll<GameObject>(dirname);

            AvatarRes avatarres = new AvatarRes();
            mAvatarResList.Add(avatarres);

            avatarres.mName = dirname;
            List<AvatarPartInfo> list = new List<AvatarPartInfo>();

            foreach (var partName in new string[] { EyesName, FaceName, HairName, PantsName, ShoesName, TopName })
            {
                list.Add(new AvatarPartInfo()
                {
                    partName = partName,
                    parts = FindRes(golist, partName).ToArray()
                });
            }

            avatarres.awatarParts = list.ToArray();
            avatarres.selectedIndexs = new int[avatarres.awatarParts.Length];

            avatarres.mSkeleton = FindRes(golist, SkeletonName)[0];


            string animpath = "Assets/Anims/" + dirname + "/";
            List<AnimationClip> clips = FunctionUtil.CollectAll<AnimationClip>(animpath);
            avatarres.mAnimList.AddRange(clips);
        }
    }

    private List<GameObject> FindRes(GameObject[] golist, string findname)
    {
        List<GameObject> findlist = new List<GameObject>();
        foreach (var go in golist)
        {
            if (go.name.Contains(findname))
            {
                findlist.Add(go);
            }
        }

        return findlist;
    }

    private void AddAvatarRes()
    {
        mAvatarResIdx++;
        if (mAvatarResIdx >= mAvatarResList.Count)
            mAvatarResIdx = 0;

        mAvatarRes = mAvatarResList[mAvatarResIdx];

    }

    private void ReduceAvatarRes()
    {
        mAvatarResIdx--;
        if (mAvatarResIdx < 0)
            mAvatarResIdx = mAvatarResList.Count - 1;

        mAvatarRes = mAvatarResList[mAvatarResIdx];

    }

    #endregion
}
