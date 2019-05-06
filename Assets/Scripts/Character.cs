using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    #region 变量

    public GameObject mSkeleton;
    public Transform skeletonRoot;
    private Animation mAnim;

    #endregion


    #region 函数

    public void SetName(string name)
    {
        gameObject.name = name;
    }

    public void GenerateSkeleton(AvatarRes avatarres)
    {
        if (mSkeleton != null)
        {
            DestroyImmediate(mSkeleton);
        }
        mSkeleton = GameObject.Instantiate(avatarres.mSkeleton);
        mSkeleton.Reset(gameObject);
        mSkeleton.name = avatarres.mSkeleton.name;
        foreach(Transform child in mSkeleton.transform)
        {
            if (child.name.IndexOf("hips",System.StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                skeletonRoot = child;
                break;
            }
        }
        mAnim = mSkeleton.GetComponent<Animation>();
        
            ChangeAnim(avatarres);

       StartCoroutine( Delay());
    }

    IEnumerator Delay()
    {
        var smrs= mSkeleton.GetComponentsInChildren<SkinnedMeshRenderer>();
        //yield return null;
        foreach (var smr in smrs)
        {
            //Destroy(smr);
            
            smr.materials = new Material[0];
            //smr.enabled = false;
        }
        yield return null;
    }

    public void ChangeAnim(AvatarRes avatarres)
    {
        if (mAnim == null)
            return;

        AnimationClip animclip = avatarres.mAnimList[avatarres.mAnimIdx];
        mAnim.wrapMode = WrapMode.Loop;
        mAnim.Stop();
        mAnim.Play(animclip.name);
    }

    #endregion
}
