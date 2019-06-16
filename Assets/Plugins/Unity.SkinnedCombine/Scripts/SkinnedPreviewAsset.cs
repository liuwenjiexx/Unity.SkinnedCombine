using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SkinnedPreview
{

    [CreateAssetMenu(fileName = "SkinnedPreview.asset", menuName = "Preview/Skinned Preview")]
    public class SkinnedPreviewAsset : ScriptableObject
    {
        public Color ambientColor = new Color(0.5f, 0.5f, 0.5f);
        [Range(0f,1f)]
        public float lightIntensity = 1f;
        public AvatarConfig[] avatars;

        [System.Serializable]
        public class AvatarConfig : ISerializationCallbackReceiver
        {
            public GameObject skeleton;
            public RuntimeAnimatorController animator; 
            public string baseDirectory;             
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