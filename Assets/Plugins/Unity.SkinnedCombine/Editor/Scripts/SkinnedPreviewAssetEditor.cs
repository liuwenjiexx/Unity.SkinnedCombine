using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System;
using UnityEditor.Animations;
using System.Linq;


namespace SkinnedPreview
{
    [CustomEditor(typeof(SkinnedPreviewAsset))]
    public class SkinnedPreviewAssetEditor : Editor
    {
        private PreviewRenderUtility previewRender;
        private GameObject skeleton;
        SkinnedCombine skinned;
        private Transform root;
        private int selectedAvatarIndex;
        private float animationTime;
        private float animtionDeltaTime;
        private float lastAnimationTime;

        private static float selectedWidth = 120;
        private static float selectedHeight = EditorGUIUtility.singleLineHeight;
        private static float buttonWidth = 20;
        private static float spaceWidth = 2;
        private static float labelWidth = selectedWidth - buttonWidth * 2 - spaceWidth * 2;

        Animation animation;
        Animator animator;
        AvatarRes avatarRes;
        private bool isDraging;
        private Vector2 dragStartPos;
        private float distance;
        private float size;
        private float height;
        private Vector3 angels;
        private float dragSpeed = 1f;
        private List<AvatarRes> avatars = new List<AvatarRes>();
        private bool combine = true;

        public SkinnedPreviewAsset Asset
        {
            get { return target as SkinnedPreviewAsset; }
        }

        private void InitPreview()
        {
            if (previewRender == null)
            {
                previewRender = new PreviewRenderUtility(false);

                previewRender.cameraFieldOfView = 60f;
                previewRender.camera.nearClipPlane = 0.1f;
                previewRender.camera.farClipPlane = 100;
                previewRender.camera.clearFlags = CameraClearFlags.Color;
            }

            if (!root)
            {
                root = new GameObject("skinned preview root").transform;
                previewRender.AddSingleGO(root.gameObject);
                CreateAllAvatarRes();

            }

        }

        private void DestroyInstances()
        {

            if (skeleton)
            {
                DestroyImmediate(skeleton);
            }
        }

        private void OnDisable()
        {
            if (previewRender != null)
            {
                previewRender.Cleanup();
                previewRender = null;
            }

            DestroyInstances();
            if (root)
            {
                DestroyImmediate(root.gameObject);
            }
            AnimationMode.StopAnimationMode();
        }


        public override bool HasPreviewGUI()
        {
            return true;
        }
        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("Skinned Preview");
        }

        private void SelectAvatar(int index)
        {

            DestroyInstances();
            if (Asset.avatars.Length == 0)
                return;

            index = (index + Asset.avatars.Length) % Asset.avatars.Length;
            selectedAvatarIndex = index;

            var asset = Asset;
            if (selectedAvatarIndex >= asset.avatars.Length)
            {
                selectedAvatarIndex = 0;
            }
            if (selectedAvatarIndex < asset.avatars.Length)
            {
                avatarRes = avatars[selectedAvatarIndex];
            }

            if (avatarRes != null)
            {
                if (avatarRes.skeleton)
                {
                    skeleton = Instantiate(avatarRes.skeleton);
                    //skeleton.transform.forward = -Vector3.forward;
                    skeleton.transform.eulerAngles = new Vector3(0, 90, 0);
                    skeleton.transform.parent = root;
                    bool first = true;
                    Vector3 min = new Vector3(), max = new Vector3();
                    foreach (var smr in skeleton.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        if (first)
                        {
                            min = smr.bounds.min;
                            max = smr.bounds.max;
                        }
                        else
                        {
                            min = Vector3.Min(min, smr.bounds.min);
                            max = Vector3.Max(max, smr.bounds.max);
                        }
                    }
                    size = (max - min).magnitude;
                    distance = size * 1.3f;
                    height = (max - min).y;
                    skinned = skeleton.AddComponent<SkinnedCombine>();
                    animation = skeleton.GetComponent<Animation>();
                    animator = skeleton.GetComponent<Animator>();

                    if (animator)
                    {
                        if (avatarRes.config.animator)
                            animator.runtimeAnimatorController = avatarRes.config.animator;
                    }

                    var smrs = skeleton.GetComponentsInChildren<SkinnedMeshRenderer>();

                    if (avatarRes.awatarParts.Length > 0)
                    {
                        foreach (var smr in smrs)
                        {
           
                            //if (smr.gameObject!=skeleton&& avatarRes.awatarParts.Where(o => string.Equals(o.partName, smr.name, StringComparison.InvariantCultureIgnoreCase)).Count() == 0)
                            //    continue;
                            smr.materials = new Material[0];
                        }
                        skinned.Clear();
                        for (int i = 0; i < avatarRes.awatarParts.Length; i++)
                        {
                            var part = avatarRes.awatarParts[i];
                            int partIndex = avatarRes.selectedIndexs[i];
                            if (!(0 <= partIndex && partIndex < avatarRes.selectedIndexs.Length))
                                continue;
                            skinned.AddPart(part.partName, part.parts[partIndex], combine);
                        }

                        skinned.GennerateSkin();
                    }

                }
            }
            SelectAnimation(avatarRes.selectedAnimationIndex);
        }

        void ResetSkin()
        {

        }

        public void SelectAnimation(int index)
        {
            if (!skeleton || avatarRes.animationNames.Count == 0)
                return;
            avatarRes.selectedAnimationIndex = (index + avatarRes.animationNames.Count) % avatarRes.animationNames.Count;
            if (avatarRes.selectedAnimationIndex >= avatarRes.animationNames.Count)
                return;

            string animName;

            if (animation != null)
            {
                var clip = avatarRes.animations[avatarRes.selectedAnimationIndex];
                animName = clip.name;
                animation.wrapMode = WrapMode.Loop;
                animation.clip = clip;
            }
            else
            {
                if (animator)
                {
                    animName = avatarRes.animationNames[avatarRes.selectedAnimationIndex];
                    animator.Play(animName);
                }
            }
            UpdateAnimation(skeleton);
        }
        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnInteractivePreviewGUI(r, background);
        }
        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            //if (!Application.isPlaying)
            //    return;
            InitPreview();
            var asset = Asset;


            bool fog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);
            previewRender.BeginPreview(r, background);
            previewRender.ambientColor = asset.ambientColor;
            previewRender.lights[0].intensity = asset.lightIntensity;


            Camera camera = previewRender.camera;

            if (skeleton)
            {
                camera.transform.position = skeleton.transform.position + Quaternion.Euler(angels) * skeleton.transform.rotation * new Vector3(0, height, distance);
                camera.transform.LookAt(skeleton.transform.position + new Vector3(0, height * 1f, 0), skeleton.transform.up);
            }
            camera.Render();
            previewRender.EndAndDrawPreview(r);

            Unsupported.SetRenderSettingsUseFogNoDirty(fog);

            Rect itemRect = new Rect(r.x, r.y, selectedWidth, selectedHeight);

    
            if (avatarRes == null)
            {
                GUI.Label(itemRect, "Avatars Empty");
                return;
            }

            if (GUI.Toggle(new Rect(itemRect.x, itemRect.y, selectedWidth, selectedHeight), combine, "Combine") != combine)
            {
                combine = !combine;
            }
            itemRect.y += itemRect.height + spaceWidth;

            itemRect = GUIOptions(itemRect, avatarRes.name, () =>
             {
                 SelectAvatar(selectedAvatarIndex - 1);
             }, () =>
             {
                 SelectAvatar(selectedAvatarIndex + 1);
             });

            itemRect = GUIOptions(itemRect, avatarRes.animationNames.Count > 0 ? avatarRes.animationNames[avatarRes.selectedAnimationIndex] : "(None)", () =>
                 {
                     SelectAnimation(avatarRes.selectedAnimationIndex - 1);
                 }, () =>
                 {
                     SelectAnimation(avatarRes.selectedAnimationIndex + 1);
                 });


            var parts = avatarRes.awatarParts;
            for (int i = 0; i < parts.Length; i++)
            {
                itemRect = AddCategory(itemRect, i, parts[i].partName);

            }

            OnGUIDrag(r);

            if (Event.current.type == EventType.Repaint)
            {
                if (skeleton)
                {
                    UpdateAnimation(skeleton);
                    Repaint();
                }
            }
        }

        void UpdateAnimation(GameObject go)
        {


            animtionDeltaTime = (float)EditorApplication.timeSinceStartup - lastAnimationTime;
            animationTime += animtionDeltaTime;
            lastAnimationTime = (float)EditorApplication.timeSinceStartup;

            if (go)
            {
                var anim = go.GetComponent<Animation>();
                if (anim)
                {
                    if (!anim.isPlaying)
                    {
                        anim.Play();
                    }
                    if (animationTime > anim.clip.length)
                        animationTime = 0;

                    if (!AnimationMode.InAnimationMode())
                        AnimationMode.StartAnimationMode();
                    AnimationMode.SampleAnimationClip(skeleton, anim.clip, animationTime);
                    //AnimationMode.StopAnimationMode();

                }
                else
                {
                    Animator animator = go.GetComponent<Animator>();
                    if (animator)
                    {
                        animator.Update(animtionDeltaTime);
                    }
                }
            }
        }

        Rect GUIOptions(Rect rect, string label, Action left, Action right)
        {
            var itemRect = rect;
            if (GUI.Button(new Rect(itemRect.x, itemRect.y, buttonWidth, itemRect.height), "<"))
            {
                left();
            }
            itemRect.x += buttonWidth + spaceWidth;
            GUI.Box(new Rect(itemRect.x, itemRect.y, labelWidth, itemRect.height), label);
            itemRect.x += labelWidth + spaceWidth;
            if (GUI.Button(new Rect(itemRect.x, itemRect.y, buttonWidth, itemRect.height), ">"))
            {
                right();
            }

            rect.y += selectedHeight + spaceWidth;
            return rect;
        }


        void OnGUIDrag(Rect rect)
        {
            Event e = Event.current;

            if (isDraging)
            {
                if (e.type == EventType.MouseDrag)
                {
                    Vector3 delta = e.mousePosition - dragStartPos;
                    delta *= dragSpeed;
                    if (delta.sqrMagnitude > 0.001f)
                    {
                        //   rotation = Quaternion.Euler(0, delta.x, 0) * rotation;
                        //angels += new Vector3(delta.y, delta.x);
                        angels += new Vector3(0, delta.x);
                        dragStartPos = e.mousePosition;
                        Repaint();
                    }
                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    isDraging = false;
                    e.Use();
                }
            }
            else
            {
                if (e.type == EventType.MouseDown)
                {
                    if (!isDraging && rect.Contains(e.mousePosition))
                    {
                        dragStartPos = e.mousePosition;
                        isDraging = true;
                        e.Use();
                    }
                }
            }

            if (e.type == EventType.ScrollWheel)
            {
                distance += e.delta.y * size * 0.05f;
                distance = Mathf.Clamp(distance, size * 0.3f, size * 3);
                e.Use();
            }

        }



        private void CreateAllAvatarRes()
        {
            var config = Asset;
            avatars.Clear();
            if (config.avatars == null)
            {
                SelectAvatar(0);
                return;
            }
            foreach (var avatar in config.avatars)
            {

                AvatarRes avatarres = new AvatarRes();

                avatarres.name = "";
                avatarres.skeleton = avatar.skeleton;
                avatarres.config = avatar;


                Animator animator = null;
                if (avatar.skeleton)
                {
                    avatarres.name = avatar.skeleton.name;
                    animator = avatar.skeleton.GetComponent<Animator>();
                }
                AnimatorController ac = null;
                if (avatar.animator)
                    ac = avatar.animator as AnimatorController;
                if (!ac && animator)
                {
                    ac = animator.runtimeAnimatorController as AnimatorController;
                }
                if (ac)
                {
                    foreach (var layer in ac.layers)
                    {
                        foreach (var state in layer.stateMachine.states)
                        {
                            avatarres.animationNames.Add(state.state.name);
                        }

                    }
                }
                else
                {
                    var clips = AnimationUtility.GetAnimationClips(avatar.skeleton);
                    if (clips != null)
                    {
                        foreach (var clip in clips)
                        {
                            avatarres.animations.Add(clip);
                            avatarres.animationNames.Add(clip.name);
                        }
                    }
                }


                if (!string.IsNullOrEmpty(avatar.defaultAnimation))
                {
                    for (int i = 0; i < avatarres.animationNames.Count; i++)
                    {
                        if (avatarres.animationNames[i] == avatar.defaultAnimation)
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

            SelectAvatar(0);
        }


        Rect AddCategory(Rect rect, int parttype, string displayName)
        {

            int selectedIndex;
            selectedIndex = avatarRes.selectedIndexs[parttype];
            var part = avatarRes.awatarParts[parttype];

            rect = GUIOptions(rect, displayName + "(" + (part.parts.Length > 0 ? part.partNames[selectedIndex] : "") + ")", () =>
            {
                selectedIndex--;
                selectedIndex = (selectedIndex + part.parts.Length) % part.parts.Length;
            }, () =>
            {
                selectedIndex++;
                selectedIndex = (selectedIndex + part.parts.Length) % part.parts.Length;
            });


            if (selectedIndex != avatarRes.selectedIndexs[parttype])
            {
                avatarRes.selectedIndexs[parttype] = selectedIndex;
                skinned.AddPart(part.partName, part.parts[selectedIndex], combine);
            }
            return rect;
        }

        public override void OnPreviewSettings()
        {
            if (GUILayout.Button("R"))
            {
                CreateAllAvatarRes();
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
}