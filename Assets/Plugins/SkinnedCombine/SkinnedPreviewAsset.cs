using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SkinnedPreview
{

    [CreateAssetMenu(fileName = "SkinnedPreview.asset", menuName = "Skinned Preview")]
    public class SkinnedPreviewAsset : ScriptableObject
    {
        public AvatarConfig[] avatars;

        [System.Serializable]
        public class AvatarConfig : ISerializationCallbackReceiver
        {
            public string name;
            public GameObject skeleton;
            public string skeletonRootPath;
            public float scale=1f;

            public string baseDirectory;

            public string animationDirectory;
            public string animationNamePattern;

            public string defaultAnimation;

            public string partDirectory;
            public SkinnedPart[] parts;

            public void OnAfterDeserialize()
            {

            }

            public void OnBeforeSerialize()
            {

            }
        }

        [System.Serializable]
        public class SkinnedPart : ISerializationCallbackReceiver
        {
            public string partName;
            public string directory;
            public string namePattern;

            public void OnAfterDeserialize()
            {

            }

            public void OnBeforeSerialize()
            {

            }
        }


    }


}