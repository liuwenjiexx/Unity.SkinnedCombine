using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{

    public static Transform FindByName(this Transform t, string name)
    {
        Transform result = null;
        foreach (Transform child in t)
        {
            if (child.name == name)
            {
                result = child;
                break;
            }
            result = FindByName(child, name);
            if (result != null)
                break;
        }

        return result;
    }
}
