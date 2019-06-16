#if UNITY_EDITOR


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


        private const int typeWidth = 120;
        private const int typeheight = 25;
        private const int buttonWidth = 25;


        private GameObject goAvatar;
        private GameObject skeleton;
        private Transform skeletonRoot;


        private List<AvatarRes> avatars = new List<AvatarRes>();
        private AvatarRes avatarConfig = null;
        private int selectedAvatarIndex = 0;


        private SkinnedCombine skinned;
        public SkinnedPreviewAsset config;
        public bool combine = true;

        public float uiScale = 1f;



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
            if (goAvatar)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    StartCoroutine(StartDrag());
                }
            }



        }

        IEnumerator StartDrag()
        {
            Vector3 lastPos = Input.mousePosition;

            while (Input.GetMouseButton(0))
            {
                Vector3 delta = (Input.mousePosition - lastPos);
                lastPos = Input.mousePosition;

                var rot = goAvatar.transform.localRotation * Quaternion.Euler(0, -delta.x, 0);
                goAvatar.transform.localRotation = rot;

                yield return null;
            }
        }

        private void OnGUI()
        {
            GUI.matrix = Matrix4x4.Scale(Vector3.one * uiScale);

            using (new GUILayout.AreaScope(new Rect(10, 10, typeWidth + 2 * buttonWidth + 8, 1000)))
            {


                if (GUILayout.Toggle(combine, "Combine") != combine)
                {
                    //skinned.IsCombine = !skinned.IsCombine;
                    combine = !combine;
                }

                // Buttons for changing the active character.
                using (new GUILayout.HorizontalScope())
                {

                    if (GUILayout.Button("<", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
                    {
                        SelectAvatar(selectedAvatarIndex - 1);
                    }

                    GUILayout.Box(avatarConfig.name, GUILayout.Width(typeWidth), GUILayout.Height(typeheight));

                    if (GUILayout.Button(">", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
                    {
                        SelectAvatar(selectedAvatarIndex + 1);
                    }

                }

                // Buttons for changing character elements.

                for (int i = 0; i < avatarConfig.awatarParts.Length; i++)
                {
                    var partInfo = avatarConfig.awatarParts[i];
                    AddCategory(i, partInfo.partName, null);
                }


                if (avatarConfig.animationNames.Count > 0)
                {
                    // anim
                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("<", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
                        {
                            SelectAnimation(avatarConfig.selectedAnimationIndex - 1);
                        }

                        GUILayout.Box(avatarConfig.animations[avatarConfig.selectedAnimationIndex].name, GUILayout.Width(typeWidth), GUILayout.Height(typeheight));

                        if (GUILayout.Button(">", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
                        {
                            SelectAnimation(avatarConfig.selectedAnimationIndex + 1);
                        }
                    }
                }


            }
        }

        // Draws buttons for configuring a specific category of items, like pants or shoes.
        void AddCategory(int parttype, string displayName, string anim)
        {
            using (new GUILayout.HorizontalScope())
            {
                int selectedIndex;
                selectedIndex = avatarConfig.selectedIndexs[parttype];
                var part = avatarConfig.awatarParts[parttype];

                if (GUILayout.Button("<", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
                {
                    selectedIndex--;
                    selectedIndex = (selectedIndex + part.parts.Length) % part.parts.Length;
                }

                GUILayout.Box(displayName + "(" + (part.parts.Length > 0 ? part.partNames[selectedIndex] : "") + ")", GUILayout.Width(typeWidth), GUILayout.Height(typeheight));

                if (GUILayout.Button(">", GUILayout.Width(buttonWidth), GUILayout.Height(typeheight)))
                {
                    selectedIndex++;
                    selectedIndex = (selectedIndex + part.parts.Length) % part.parts.Length;

                }

                if (selectedIndex != avatarConfig.selectedIndexs[parttype])
                {
                    avatarConfig.selectedIndexs[parttype] = selectedIndex;
                    skinned.AddPart(part.partName, part.parts[selectedIndex], combine);
                }

            }
        }




        private void InitCharacter()
        {
            GameObject go = new GameObject();
            go.name = "Character";
            goAvatar = go;
            avatarConfig = avatars[selectedAvatarIndex];



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
            skeleton.name = avatarConfig.skeleton.name;

            skinned = skeleton.AddComponent<SkinnedCombine>();

            if (skeleton.GetComponent<Animator>())
            {
                //DestroyImmediate(skeleton.GetComponent<Animator>());
                var animator = skeleton.GetComponent<Animator>();
                var ctrl = animator.runtimeAnimatorController;
                if (ctrl)
                {
                    var clips = ctrl.animationClips;
                    avatarConfig.animations.Clear();
                    avatarConfig.animationNames.Clear();
                    avatarConfig.animations.AddRange(clips);
                    avatarConfig.animationNames.AddRange(clips.Select(o => o.name));
                }
            }
            else
            {
                if (skeleton.GetComponent<Animation>())
                {
                    DestroyImmediate(skeleton.GetComponent<Animation>());
                }

                var mAnim = skeleton.AddComponent<Animation>();
                foreach (var anim in avatarConfig.animations)
                {
                    mAnim.AddClip(anim, anim.name);
                }
                ChangeAnim();

            }

            StartCoroutine(Delay());

            //skinned.SkeletonRoot = skeletonRoot;
            //skinned.skeletonRootPath = avatarConfig.config.skeletonRootPath;
            //skinned.IsCombine = combine;
            skinned.Clear();
            for (int i = 0; i < avatarConfig.awatarParts.Length; i++)
            {
                var part = avatarConfig.awatarParts[i];
                int index = avatarConfig.selectedIndexs[i];
                if (!(0 <= index && index < avatarConfig.selectedIndexs.Length))
                    continue;
                skinned.AddPart(part.partName, part.parts[index], combine);
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
            string animName = avatarConfig.animationNames[avatarConfig.selectedAnimationIndex];

            var animation = skeleton.GetComponent<Animation>();
            if (animation != null)
            {
                animation.wrapMode = WrapMode.Loop;
                animation.Stop();
                animation.Play(animName);
            }
            else
            {
                var animator = skeleton.GetComponent<Animator>();
                if (animator)
                {
                    animator.Play(animName);
                }
            }
        }


        private void CreateAllAvatarRes()
        {

            foreach (var avatar in config.avatars)
            {

                AvatarRes avatarres = new AvatarRes();

                avatarres.name = avatar.name;
                avatarres.skeleton = avatar.skeleton;
                avatarres.config = avatar;

                var clips = AnimationUtility.GetAnimationClips(avatar.skeleton);
                if (clips != null)
                {
                    foreach (var clip in clips)
                    {
                        avatarres.animations.Add(clip);
                        avatarres.animationNames.Add(clip.name);
                    }
                }

                //string dir = avatar.animationDirectory;
                //if (string.IsNullOrEmpty(dir))
                //    dir = avatar.baseDirectory;

                //Regex regex = null;
                //if (!string.IsNullOrEmpty(dir))
                //{
                //    regex = new Regex(avatar.animationNamePattern);
                //    foreach (var clip in FindAssets<AnimationClip>("t:AnimationClip", new string[] { dir }))
                //    {
                //        string assetPath = AssetDatabase.GetAssetPath(clip);
                //        var modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                //        if (modelImporter)
                //        {
                //            if (modelImporter.animationType != ModelImporterAnimationType.Legacy)
                //            {
                //                continue;
                //            }
                //        }

                //        Match m = regex.Match(assetPath);
                //        if (m.Success)
                //        {
                //            avatarres.animations.Add(clip);
                //            avatarres.animationNames.Add(clip.name);
                //        }
                //    }
                //}

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

                string dir;
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
                            List<string> nameList = new List<string>();
                            foreach (string guid in AssetDatabase.FindAssets("t:Prefab t:Model", new string[] { dir }))
                            {
                                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                                var m = fileRegex.Match(assetPath);
                                if (!m.Success)
                                    continue;
                                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                                if (m.Groups.Count > 1)
                                    nameList.Add(m.Groups[1].Value);
                                else
                                    nameList.Add(go.name);

                                list.Add(go);
                            }
                            part.parts = list.ToArray();
                            part.partNames = nameList.ToArray();
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
            animationIndex = (animationIndex + avatarConfig.animationNames.Count) % avatarConfig.animationNames.Count;
            if (animationIndex != avatarConfig.selectedAnimationIndex)
            {
                avatarConfig.selectedAnimationIndex = animationIndex;
                ChangeAnim();
            }
        }



    }



    public class AvatarRes
    {
        public string name;
        public GameObject skeleton;
        public List<AnimationClip> animations = new List<AnimationClip>();
        public List<string> animationNames = new List<string>();
        public SkinnedPreviewAsset.AvatarConfig config;

        public int selectedAnimationIndex = 0;

        public AvatarPartInfo[] awatarParts;
        public int[] selectedIndexs;

    }

    public class AvatarPartInfo
    {
        public string partName;
        public GameObject[] parts;
        public string[] partNames;
    }
}

#endif

