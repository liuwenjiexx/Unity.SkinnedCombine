using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    #region 变量

    private GameObject mSkeleton;
    private GameObject[] parts;
    private Animation mAnim;

    /// <summary>
    /// 是否组合
    /// </summary>
    private bool mCombine = false;

    #endregion

    #region 内置函数

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    #endregion

    #region 函数

    public void SetName(string name)
    {
        gameObject.name = name;
    }

    public void Generate(AvatarRes avatarres, bool combine = false)
    {
        if (!combine)
            GenerateUnCombine(avatarres);
        else
            GenerateCombine(avatarres);
    }

    private void DestroyAll()
    {
        if (mSkeleton != null)
            GameObject.DestroyImmediate(mSkeleton);


        if (parts != null)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i])
                    DestroyImmediate(parts[i]);
                parts[i] = null;
            }
        }
    }

    private void GenerateUnCombine(AvatarRes avatarres)
    {
        DestroyAll();

        mSkeleton = GameObject.Instantiate(avatarres.mSkeleton);
        mSkeleton.Reset(gameObject);
        mSkeleton.name = avatarres.mSkeleton.name;

        mAnim = mSkeleton.GetComponent<Animation>();

        for (int i = 0; i < avatarres.awatarParts.Length; i++)
        {
            ChangeEquipUnCombine(i, avatarres);
        }

        ChangeAnim(avatarres);
    }

    private void GenerateCombine(AvatarRes avatarres)
    {
        if (mSkeleton != null)
        {
            bool iscontain = mSkeleton.name.Equals(avatarres.mSkeleton.name);
            if (!iscontain)
            {
                GameObject.DestroyImmediate(mSkeleton);
            }
        }

        if (mSkeleton == null)
        {
            mSkeleton = GameObject.Instantiate(avatarres.mSkeleton);
            mSkeleton.Reset(gameObject);
            mSkeleton.name = avatarres.mSkeleton.name;
        }

        mAnim = mSkeleton.GetComponent<Animation>();

        List<CombineInstance> combineInstances = new List<CombineInstance>();
        List<Material> materials = new List<Material>();
        List<Transform> bones = new List<Transform>();

        for (int i = 0; i < avatarres.awatarParts.Length; i++)
        {
            ChangeEquipCombine(i, avatarres, ref combineInstances, ref materials, ref bones);
        }

        // Obtain and configure the SkinnedMeshRenderer attached to
        // the character base.
        SkinnedMeshRenderer r = mSkeleton.GetComponent<SkinnedMeshRenderer>();
        if (r != null)
        {
            GameObject.DestroyImmediate(r);
        }

        r = mSkeleton.AddComponent<SkinnedMeshRenderer>();
        r.sharedMesh = new Mesh();
        r.sharedMesh.CombineMeshes(combineInstances.ToArray(), false, false);
        r.bones = bones.ToArray();
        r.materials = materials.ToArray();

        ChangeAnim(avatarres);
    }

    private void ChangeEquipCombine(int type, AvatarRes avatarres, ref List<CombineInstance> combineInstances,
                        ref List<Material> materials, ref List<Transform> bones)
    {

        int partIndex = avatarres.selectedIndexs[type];
        ChangeEquipCombine(avatarres.awatarParts[type].parts[partIndex], ref combineInstances, ref materials, ref bones);

    }

    private void ChangeEquipCombine(GameObject resgo, ref List<CombineInstance> combineInstances,
                        ref List<Material> materials, ref List<Transform> bones)
    {
        Transform[] skettrans = mSkeleton.GetComponentsInChildren<Transform>();

        GameObject go = GameObject.Instantiate(resgo);
        SkinnedMeshRenderer smr = go.GetComponentInChildren<SkinnedMeshRenderer>();

        materials.AddRange(smr.materials);
        for (int sub = 0; sub < smr.sharedMesh.subMeshCount; sub++)
        {
            CombineInstance ci = new CombineInstance();
            ci.mesh = smr.sharedMesh;
            ci.subMeshIndex = sub;
            combineInstances.Add(ci);
        }

        // As the SkinnedMeshRenders are stored in assetbundles that do not
        // contain their bones (those are stored in the characterbase assetbundles)
        // we need to collect references to the bones we are using
        foreach (Transform bone in smr.bones)
        {
            string bonename = bone.name;
            foreach (Transform transform in skettrans)
            {
                if (transform.name != bonename)
                    continue;

                bones.Add(transform);
                break;
            }
        }

        GameObject.DestroyImmediate(go);
    }

    public void ChangeEquipUnCombine(int type, AvatarRes avatarres)
    {

        int partIndex = avatarres.selectedIndexs[type];
        if (parts == null)
            parts = new GameObject[avatarres.awatarParts.Length];

        ChangeEquipUnCombine(ref parts[type], avatarres.awatarParts[type].parts[partIndex]);
    }

    private void ChangeEquipUnCombine(ref GameObject go, GameObject resgo)
    {
        if (go != null)
        {
            GameObject.DestroyImmediate(go);
        }

        go = GameObject.Instantiate(resgo);
        go.Reset(mSkeleton);
        go.name = resgo.name;

        SkinnedMeshRenderer render = go.GetComponentInChildren<SkinnedMeshRenderer>();
        ShareSkeletonInstanceWith(render, mSkeleton);
    }

    // 共享骨骼
    public void ShareSkeletonInstanceWith(SkinnedMeshRenderer selfSkin, GameObject target)
    {
        Transform[] newBones = new Transform[selfSkin.bones.Length];
        for (int i = 0; i < selfSkin.bones.GetLength(0); ++i)
        {
            GameObject bone = selfSkin.bones[i].gameObject;

            // 目标的SkinnedMeshRenderer.bones保存的只是目标mesh相关的骨骼,要获得目标全部骨骼,可以通过查找的方式.
            newBones[i] = FindChildRecursion(target.transform, bone.name);
        }

        selfSkin.bones = newBones;
    }

    // 递归查找
    public Transform FindChildRecursion(Transform t, string name)
    {
        foreach (Transform child in t)
        {
            if (child.name == name)
            {
                return child;
            }
            else
            {
                Transform ret = FindChildRecursion(child, name);
                if (ret != null)
                    return ret;
            }
        }

        return null;
    }

    public void ChangeAnim(AvatarRes avatarres)
    {
        if (mAnim == null)
            return;

        AnimationClip animclip = avatarres.mAnimList[avatarres.mAnimIdx];
        mAnim.wrapMode = WrapMode.Loop;
        mAnim.Play(animclip.name);
    }

    #endregion
}
