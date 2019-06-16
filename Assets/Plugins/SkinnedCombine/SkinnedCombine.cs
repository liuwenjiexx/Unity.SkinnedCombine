using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
public class SkinnedCombine : MonoBehaviour
{
    private List<SkinnedPart> parts;
    private Dictionary<string, GameObject> instanceParts;
    private bool isDried;
    private bool lastGennerateCombine;

    private SkinnedMeshRenderer combineSkinned;

    public IEnumerable<string> Parts { get { return parts.Select(o => o.partName); } }
    private Dictionary<string, Transform> cachedSkeletons;


    void Awake()
    {
        if (parts == null)
            parts = new List<SkinnedPart>();
        instanceParts = new Dictionary<string, GameObject>(StringComparer.InvariantCultureIgnoreCase);
        cachedSkeletons = new Dictionary<string, Transform>();

    }



    // Use this for initialization
    void Start()
    {
        GennerateSkin();
    }

    private void Update()
    {
        if (isDried)
            GennerateSkin();
    }

    public void GennerateSkin()
    {
        isDried = false;

        GenerateCombineSkin();

        GennerateUncombineSkin();

    }

    void GennerateUncombineSkin()
    {

        foreach (var part in instanceParts.Keys.ToArray())
        {
            if (parts.Where(o => o.partName.ToLower() == part).Count() == 0)
            {
                GameObject instance = instanceParts[part];
                if (instance)
                    DestroyImmediate(instance);
                instanceParts.Remove(part);
            }
        }



        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part.combine)
                continue;

            if (!instanceParts.ContainsKey(part.partName))
            {
                if (part.prefab)
                {
                    GameObject go = Instantiate(part.prefab, transform);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localEulerAngles = Vector3.zero;

                    foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        ShareSkeleton(smr);
                    }

                    instanceParts[part.partName.ToLower()] = go;
                }
            }
        }

    }

    public void ShareSkeleton(SkinnedMeshRenderer smr)
    {
        var bones = smr.bones;
        Transform[] newBones = new Transform[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i])
            {
                Transform t = FindSkeleton(bones[i].name);
                if (!t)
                {
                    Debug.LogError("not found bones :" + bones[i].name + ", " + smr);
                }
                newBones[i] = t;
            }
        }
        smr.bones = newBones;
    }


    void GenerateCombineSkin()
    {


        List<CombineInstance> combineInstances = new List<CombineInstance>();
        List<Material> materials = new List<Material>();
        List<Transform> bones = new List<Transform>();

        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (!part.combine)
                continue;

            GameObject go = GameObject.Instantiate(part.prefab);

            foreach (SkinnedMeshRenderer smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {

                materials.AddRange(smr.materials);
                for (int sub = 0; sub < smr.sharedMesh.subMeshCount; sub++)
                {
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = smr.sharedMesh;
                    ci.subMeshIndex = sub;
                    combineInstances.Add(ci);
                }

                foreach (Transform bone in smr.bones)
                {
                    Transform t = FindSkeleton(bone.name);
                    if (!t)
                        Debug.LogError("not found bones :" + bone.name + ", " + smr);
                    bones.Add(t);
                }
            }

            DestroyImmediate(go);
        }


        SkinnedMeshRenderer r;
        r = GetComponent<SkinnedMeshRenderer>();
        if (!r)
        {
            r = gameObject.AddComponent<SkinnedMeshRenderer>();
        }

        var newMesh = new Mesh();
        newMesh.CombineMeshes(combineInstances.ToArray(), false, false);
        r.sharedMesh = newMesh;
        r.bones = bones.ToArray();
        r.materials = materials.ToArray();

    }

    public void AddPart(string partName, GameObject prefab, bool combine = true)
    {

        int index = FindPartIndex(partName);
        if (index >= 0)
        {
            if (parts[index].prefab == prefab)
                return;
            RemovePart(partName);
        }
        var part = new SkinnedPart()
        {
            partName = partName,
            prefab = prefab,
            combine = combine,
        };
        parts.Add(part);
        isDried = true;
    }


    public void RemovePart(string partName)
    {
        if (string.IsNullOrEmpty(partName))
            return;
        int index = FindPartIndex(partName);
        if (index >= 0)
        {
            parts.RemoveAt(index);
            isDried = true;
        }
        if (instanceParts.ContainsKey(partName))
        {
            GameObject go = instanceParts[partName];
            if (go)
                DestroyImmediate(go);
            instanceParts.Remove(partName);
        }
    }

    public void Clear()
    {
        while (parts.Count > 0)
        {
            RemovePart(parts[0].partName);
        }

    }

    int FindPartIndex(string partName)
    {
        int index = -1;
        for (int i = 0; i < parts.Count; i++)
        {
            if (string.Equals(parts[i].partName, partName, System.StringComparison.InvariantCultureIgnoreCase))
            {
                index = i;
                break;
            }
        }
        return index;
    }

    Transform FindSkeleton(string name)
    {
        if (cachedSkeletons.Count == 0)
        {
            foreach (var t in transform.GetComponentsInChildren<Transform>())
            {
                cachedSkeletons[t.name] = t;
            }
        }
        Transform find;
        if (!cachedSkeletons.TryGetValue(name, out find))
            return null;
        return find;
    }




    static Transform FindSkeleton(Transform t, string name)
    {
        Transform result = null;
        foreach (Transform child in t)
        {
            if (child.name == name)
            {
                result = child;
                break;
            }
            result = FindSkeleton(child, name);
            if (result != null)
                break;
        }

        return result;
    }

    public static string ToRelativePath(Transform root, Transform relative)
    {
        if (root == relative)
            return string.Empty;

        string path = "";
        Transform current = relative;
        bool first = true;
        while (current && current != root)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                path += "/";
            }
            path += current.name;
            current = current.parent;
        }
        if (current && current != root)
            throw new Exception("error");
        return path;
    }

    class SkinnedPart
    {
        public string partName;
        public GameObject prefab;
        public bool combine;

        public override bool Equals(object obj)
        {
            var part = obj as SkinnedPart;
            if (part == null)
                return false;
            if (!string.Equals(partName, part.partName, StringComparison.InvariantCultureIgnoreCase))
                return false;
            if (prefab != part.prefab)
                return false;
            return true;
        }
        public override int GetHashCode()
        {
            return partName.GetHashCode();
        }

    }
}
