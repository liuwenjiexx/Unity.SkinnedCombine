using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "SkinnedConfig.asset", menuName = "Skinned/EditorConfig")]
public class EditorSkinnedConfigAsset : ScriptableObject
{
    public AvatarConfig[] avatars;

    [System.Serializable]
    public class AvatarConfig
    {
        public string name;
        public GameObject skeleton;        
        public string directory;

        public SkinnedPart[] parts;
    }

    [System.Serializable]
    public class SkinnedPart
    {
        public string partName;
        public string directory;
        public string file;
    }


}
