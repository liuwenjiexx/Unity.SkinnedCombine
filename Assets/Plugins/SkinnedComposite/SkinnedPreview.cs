using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using System.Text.RegularExpressions;

namespace SkinnedPreview
{

    public class SkinnedPreview : MonoBehaviour
    {
        #region 常量


        private const int typeWidth = 120;
        private const int typeheight = 25;
        private const int buttonWidth = 25;

        #endregion

        #region 变量
        private GameObject goAvatar;
        private GameObject skeleton;
        private Transform skeletonRoot;
        private Animation mAnim;

        private List<AvatarRes> avatars = new List<AvatarRes>();
        private AvatarRes avatarConfig = null;
        private int selectedAvatarIndex = 0;


        private SkinnedMeshComposite skinned;
        public SkinnedPreviewAsset config;


        public float uiScale = 1f;
        #endregion

        #region 内置函数

        // Use this for initialization
        void Start()
        {

            if (!config)
                throw new Exception("set field config");


            CreateAllAvatarRes();
            InitCharacter();
        }

        // Update is called once per frame
        void Update()
        {
            Debug.Log(Time.deltaTime);
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
                SelectAvatar(selectedAvatarIndex - 1);
            }

            GUILayout.Box(avatarConfig.name, GUILayout.Width(typeWidth), GUILayout.Height(typeheight));

            if (GUILayout.Button(">", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
            {
                SelectAvatar(selectedAvatarIndex + 1);
            }

            GUILayout.EndHorizontal();

            // Buttons for changing character elements.

            for (int i = 0; i < avatarConfig.awatarParts.Length; i++)
            {
                var partInfo = avatarConfig.awatarParts[i];
                AddCategory(i, partInfo.partName, null);
            }


            // anim
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("<", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
            {
                SelectAnimation(avatarConfig.selectedAnimationIndex - 1);
            }

            GUILayout.Box(avatarConfig.animations[avatarConfig.selectedAnimationIndex].name, GUILayout.Width(typeWidth), GUILayout.Height(typeheight));

            if (GUILayout.Button(">", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
            {
                SelectAnimation(avatarConfig.selectedAnimationIndex + 1);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        // Draws buttons for configuring a specific category of items, like pants or shoes.
        void AddCategory(int parttype, string displayName, string anim)
        {
            GUILayout.BeginHorizontal();
            int selectedIndex;
            selectedIndex = avatarConfig.selectedIndexs[parttype];
            var part = avatarConfig.awatarParts[parttype];

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

            if (selectedIndex != avatarConfig.selectedIndexs[parttype])
            {
                avatarConfig.selectedIndexs[parttype] = selectedIndex;
                skinned.AddPart(part.partName, part.parts[selectedIndex]);
            }

            GUILayout.EndHorizontal();
        }

        #endregion

        #region 函数

        private void InitCharacter()
        {
            GameObject go = new GameObject();
            go.name = "Character";
            goAvatar = go;
            avatarConfig = avatars[selectedAvatarIndex];

            skinned = go.AddComponent<SkinnedMeshComposite>();

            ResetSkin();
        }

        void ResetSkin()
        {
            if (skeleton != null)
            {
                DestroyImmediate(skeleton);
            }
            skeleton = GameObject.Instantiate(avatarConfig.skeleton, goAvatar.transform);

            skeleton.transform.position = Vector3.zero;
            skeleton.transform.rotation = Quaternion.identity;
            skeleton.transform.localScale = Vector3.one;
            skeleton.name = avatarConfig.skeleton.name;
            foreach (Transform child in skeleton.transform)
            {
                if (child.name.IndexOf("hips", System.StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    skeletonRoot = child;
                    break;
                }
            }

            if (skeleton.GetComponent<Animator>())
            {
                DestroyImmediate(skeleton.GetComponent<Animator>());
            }
            if (skeleton.GetComponent<Animation>())
            {
                DestroyImmediate(skeleton.GetComponent<Animation>());
            }

            mAnim = skeleton.AddComponent<Animation>();
            foreach (var anim in avatarConfig.animations)
            {
                mAnim.AddClip(anim, anim.name);
            }
            ChangeAnim();

            StartCoroutine(Delay());

            skinned.SkeletonRoot = skeletonRoot;
            skinned.skeletonRootPath = SkinnedMeshComposite.ToRelativePath(skeleton.transform, skeletonRoot);

            skinned.Clear();
            for (int i = 0; i < avatarConfig.awatarParts.Length; i++)
            {
                var part = avatarConfig.awatarParts[i];
                int index = avatarConfig.selectedIndexs[i];
                skinned.AddPart(part.partName, part.parts[index]);
            }

            skinned.GennerateSkin();
        }




        IEnumerator Delay()
        {
            var smrs = skeleton.GetComponentsInChildren<SkinnedMeshRenderer>();
            //yield return null;
            foreach (var smr in smrs)
            {
                //Destroy(smr);

                smr.materials = new Material[0];
                //smr.enabled = false;
            }
            yield return null;
        }

        public void ChangeAnim()
        {
            if (mAnim == null)
                return;

            AnimationClip animclip = avatarConfig.animations[avatarConfig.selectedAnimationIndex];
            mAnim.wrapMode = WrapMode.Loop;
            mAnim.Stop();
            mAnim.Play(animclip.name);
        }


        private void CreateAllAvatarRes()
        {

            foreach (var avatar in config.avatars)
            {

                AvatarRes avatarres = new AvatarRes();

                avatarres.name = avatar.name;

                avatarres.skeleton = avatar.skeleton;
                //if (avatar.skeleton)
                //{
                //string assetPath = AssetDatabase.GetAssetPath(avatar.skeleton);
                //ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                //if (modelImporter)
                //{
                //    avatarres.mAnimList.AddRange(
                //        modelImporter.referencedClips.Select(o => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(o), typeof(AnimationClip)))
                //        .Select(o => (AnimationClip)o)
                //        .ToArray());

                //}
                //else
                //{
                //    var anim = avatar.skeleton.GetComponentInChildren<Animation>();
                //    if (anim)
                //    {
                //        foreach (AnimationState animationState in anim)
                //        {
                //            avatarres.mAnimList.Add(animationState.clip);
                //        }
                //    }
                //}
                //}

                string dir = avatar.animationDirectory;
                if (string.IsNullOrEmpty(dir))
                    dir = avatar.baseDirectory;

                Regex regex = null;
                if (!string.IsNullOrEmpty(dir))
                {
                    regex = new Regex(avatar.animationNamePattern);
                    foreach (var clip in FindAssets<AnimationClip>("t:AnimationClip", new string[] { dir }))
                    {
                        string assetPath = AssetDatabase.GetAssetPath(clip);
                        Match m = regex.Match(assetPath);
                        if (m.Success)
                        {
                            avatarres.animations.Add(clip);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(avatar.defaultAnimation))
                {
                    for (int i = 0; i < avatarres.animations.Count; i++)
                    {
                        if (avatarres.animations[i].name == avatar.defaultAnimation)
                        {
                            avatarres.selectedAnimationIndex = i;
                            break;
                        }
                    }
                }


                List<AvatarPartInfo> parts = new List<AvatarPartInfo>();
                foreach (var partConfig in avatar.parts)
                {
                    var part = new AvatarPartInfo();
                    part.partName = partConfig.partName;
                    if (!string.IsNullOrEmpty(partConfig.namePattern))
                    {
                        Regex fileRegex = new Regex(partConfig.namePattern);
                        dir = partConfig.directory;
                        if (string.IsNullOrEmpty(dir))
                            dir = avatar.partDirectory;

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
                avatars.Add(avatarres);
            }


        }

        //static string CombineLocalPath(string parent,string subPath)
        //{

        //}

        static IEnumerable<string> FindAssetPaths(string filter, string[] searchInFolders)
        {
            foreach (string guid in AssetDatabase.FindAssets(filter, searchInFolders))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                yield return assetPath;
            }
        }
        static IEnumerable<T> FindAssets<T>(string filter, string[] searchInFolders)
            where T : UnityEngine.Object
        {
            foreach (string guid in AssetDatabase.FindAssets(filter, searchInFolders))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                T obj = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                yield return obj;
            }
        }
        static IEnumerable<string> FindAssets(string filter, string[] searchInFolders, string assetPathPattern)
        {
            Regex regex = null;
            if (!string.IsNullOrEmpty(assetPathPattern))
                regex = new Regex(assetPathPattern);
            foreach (string guid in AssetDatabase.FindAssets(filter, searchInFolders))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (regex != null && !regex.IsMatch(assetPath))
                    continue;
                yield return assetPath;
            }
        }

        static IEnumerable<Match> FindAssetMatchs(string filter, string[] searchInFolders, string assetPathPattern)
        {
            Regex regex = null;

            regex = new Regex(assetPathPattern);
            foreach (string guid in AssetDatabase.FindAssets(filter, searchInFolders))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Match m = regex.Match(assetPath);
                if (!m.Success)
                    continue;
                yield return m;
            }
        }

        void SelectAvatar(int avatarIndex)
        {

            avatarIndex = (avatarIndex + avatars.Count) % avatars.Count;
            if (avatarIndex != selectedAvatarIndex)
            {
                selectedAvatarIndex = avatarIndex;
                avatarConfig = avatars[selectedAvatarIndex];
                ResetSkin();
            }
        }

        void SelectAnimation(int animationIndex)
        {
            animationIndex = (animationIndex + avatars.Count) % avatars.Count;
            if (animationIndex != avatarConfig.selectedAnimationIndex)
            {
                avatarConfig.selectedAnimationIndex = animationIndex;
                ChangeAnim();
            }
        }



        #endregion
    }



    public class AvatarRes
    {
        public string name;
        public GameObject skeleton;
        public List<AnimationClip> animations = new List<AnimationClip>();


        public int selectedAnimationIndex = 0;

        public AvatarPartInfo[] awatarParts;
        public int[] selectedIndexs;

    }

    public class AvatarPartInfo
    {
        public string partName;
        public GameObject[] parts;
    }
}