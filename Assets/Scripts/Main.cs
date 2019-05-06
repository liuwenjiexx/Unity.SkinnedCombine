using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using System.Text.RegularExpressions;

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


    private const int typeWidth = 120;
    private const int typeheight = 25;
    private const int buttonWidth = 25;

    #endregion

    #region 变量

    private List<AvatarRes> mAvatarResList = new List<AvatarRes>();
    private AvatarRes mAvatarRes = null;
    private int mAvatarResIdx = 0;

    private Character mCharacter = null;

    private SkinnedMeshComposite skinned;
    public EditorSkinnedConfigAsset asset;
    public float uiScale = 1f;
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

        GUILayout.Box(mAvatarRes.mName , GUILayout.Width(typeWidth), GUILayout.Height(typeheight));

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

        GUILayout.Box(mAvatarRes.mAnimList[mAvatarRes.mAnimIdx].name, GUILayout.Width(typeWidth), GUILayout.Height(typeheight));

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

        ResetSkin(mAvatarRes);
    }

    void ResetSkin(AvatarRes avatarRes)
    {
        mCharacter.GenerateSkeleton(mAvatarRes);

        skinned.SkeletonRoot = mCharacter.skeletonRoot;
        skinned.skeletonRootPath = SkinnedMeshComposite.ToRelativePath(mCharacter.mSkeleton.transform, mCharacter.skeletonRoot);

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
        if (!asset)
            throw new Exception("config null");

        foreach (var avatar in asset.avatars)
        {

            AvatarRes avatarres = new AvatarRes();

            avatarres.mName = avatar.name;

            avatarres.mSkeleton = avatar.skeleton;
            if (avatar.skeleton)
            {
                string assetPath = AssetDatabase.GetAssetPath(avatar.skeleton);

                ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (modelImporter)
                {
                    avatarres.mAnimList.AddRange(
                        modelImporter.referencedClips.Select(o => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(o), typeof(AnimationClip)))
                        .Select(o => (AnimationClip)o)
                        .ToArray());

                }
                else
                {
                    var anim = avatar.skeleton.GetComponentInChildren<Animation>();
                    if (anim)
                    {
                        foreach (AnimationState animationState in anim)
                        {
                            avatarres.mAnimList.Add(animationState.clip);
                        }
                    }
                }
            }

            List<AvatarPartInfo> parts = new List<AvatarPartInfo>();
            foreach (var partConfig in avatar.parts)
            {
                var part = new AvatarPartInfo();
                part.partName = partConfig.partName;
                if (!string.IsNullOrEmpty(partConfig.file))
                {
                    Regex fileRegex = new Regex(partConfig.file);
                    string dir = !string.IsNullOrEmpty(partConfig.directory) ? partConfig.directory : avatar.directory;
                    if (!string.IsNullOrEmpty(dir))
                    {
                        List<GameObject> list = new List<GameObject>();
                        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new string[] { dir }))
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                            if (!fileRegex.IsMatch(assetPath))
                                continue;
                            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                            list.Add(go);
                        }
                        part.parts = list.ToArray();
                    }
                }
                if (part.parts == null)
                    part.parts = new GameObject[0];
                parts.Add(part);
            }
            avatarres.awatarParts = parts.ToArray();
            avatarres.selectedIndexs = new int[avatarres.awatarParts.Length];
            mAvatarResList.Add(avatarres);
        }


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
