using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildMapBundle
{
    [MenuItem("Tools/Build Map Bundle")]
    public static void Build()
    {
        string folder = "Assets/AssetBundles";

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        BuildPipeline.BuildAssetBundles(
            folder,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows
        );

        Debug.Log("Built AssetBundle to " + folder);
    }
}