using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    #region 变量

    public GameObject mSkeleton;
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
        mAnim = mSkeleton.GetComponent<Animation>();

        ChangeAnim(avatarres);
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
