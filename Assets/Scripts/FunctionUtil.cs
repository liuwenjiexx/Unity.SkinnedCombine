using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEditor;

namespace SkinnedPreview
{
    public static class FunctionUtil
    {


        public static List<T> CollectAll<T>(string path) where T : UnityEngine.Object
        {
            List<T> l = new List<T>();
            string[] files = Directory.GetFiles(path);

            foreach (string file in files)
            {
                if (file.Contains(".meta")) continue;
                T asset = (T)AssetDatabase.LoadAssetAtPath(file, typeof(T));
                if (asset == null) throw new Exception("Asset is not " + typeof(T) + ": " + file);
                l.Add(asset);
            }
            return l;
        }
    }
}