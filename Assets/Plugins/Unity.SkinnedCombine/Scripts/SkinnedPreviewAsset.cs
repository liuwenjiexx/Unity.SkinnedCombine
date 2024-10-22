using System.Collections;
using System.Collections.Generic;


namespace UnityEngine
{

    [CreateAssetMenu(fileName = "SkinnedPreview.asset", menuName = "Preview/Skinned Preview")]
    public class SkinnedPreviewAsset : ScriptableObject
    {
        public Color ambientColor = new Color(0.5f, 0.5f, 0.5f);
        [Range(0f, 1f)]
        public float lightIntensity = 1f;

        public GameObject skeleton;
        public RuntimeAnimatorController animator;
        public string defaultAnimation;
        public SkinnedPart[] parts;

        [System.Serializable]
        public class SkinnedPart : ISerializationCallbackReceiver
        {
            public string name;
            public GameObject[] prefabs;

            public void OnAfterDeserialize()
            {

            }

            public void OnBeforeSerialize()
            {

            }
        }


    }


}