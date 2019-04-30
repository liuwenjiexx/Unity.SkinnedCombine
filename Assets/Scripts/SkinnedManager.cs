using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class SkinnedManager : MonoBehaviour
{
    [SerializeField]
    private List<SkinnedPart> parts;
    public bool combine;
    public IEnumerable<SkinnedPart> Parts { get { return parts; } }

    private Dictionary<SkinnedPart, GameObject> instanceParts;
    private bool lastGennerateCombine;
    public Transform root;

    void Awake()
    {
        if (parts == null)
            parts = new List<SkinnedPart>();
        instanceParts = new Dictionary<SkinnedPart, GameObject>();
        if (!root)
            root = transform;
    }

    // Use this for initialization
    void Start()
    {
        Gennerate();
    }

    public void Gennerate()
    {

        foreach (var part in instanceParts.Keys.ToArray())
        {
            if (!parts.Contains(part))
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
            if (!instanceParts.ContainsKey(part))
            {
                if (part.prefab)
                {
                    GameObject go = Instantiate(part.prefab);

                    foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var bones = smr.bones;
                        Transform[] newBones = new Transform[bones.Length];
                        for (int j = 0; j < bones.Length; j++)
                        {
                            if (bones[j])
                            {
                                Transform t = root.FindByName(bones[j].name);
                                if (!t)
                                {
                                    Debug.LogError("not found bones :" + bones[j].name + ", " + smr);
                                }
                                newBones[j] = t;
                            }
                        }
                        smr.bones = bones;
                    }

                    instanceParts[part] = go;
                }
            }
        }
        lastGennerateCombine = combine;
    }

    public void ClearGennerate()
    {
        foreach (var item in instanceParts)
        {
            if (item.Value)
                DestroyImmediate(item.Value);
        }
        instanceParts.Clear();
    }

    public void AddPart(SkinnedPart part)
    {
        RemovePart(part.partName);
        parts.Add(part);
    }
    public void RemovePart(SkinnedPart part)
    {
        RemovePart(part.partName);
    }

    public void RemovePart(string partName)
    {
        if (string.IsNullOrEmpty(partName))
            return;
        int index = FindPartIndex(partName);
        if (index >= 0)
            parts.RemoveAt(index);
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

}

public class SkinnedPart
{
    public string partName;
    public GameObject prefab;
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