using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;

public class AssetBundler
{
#if UNITY_EDITOR
    const string BUILD_DIR = "../AssetBundles";
    const string BUNDLE_NAME = "resources_tsuyao_faces";

    public static void CopyFile(string path, string to)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"tried to copy nonexisting file at '{path}'");
            return;
        }
        Debug.Log($"copying '{path}' to '{to}'");
        var to_name = System.IO.Path.GetFileName(to);
        var dir_target = to.TrimEnd(to_name);
        var bytes = File.ReadAllBytes(path);

        if (!Directory.Exists(dir_target))
            Directory.CreateDirectory(dir_target);

        File.WriteAllBytes(to, bytes);
    }

    [MenuItem("Assets/Bundle")]
    public static void BundleAssets()
    {
        Debug.Log("starting bundling...");
        foreach (BuildTarget tar in new[] {
            BuildTarget.StandaloneWindows64,
            BuildTarget.StandaloneLinux64,
            BuildTarget.StandaloneOSX
        }) {
            var tar_path = tar switch
            {
                BuildTarget.StandaloneWindows64 => "win",
                BuildTarget.StandaloneLinux64 => "linux",
                BuildTarget.StandaloneOSX => "mac",
                _ => throw new System.Exception("invalid build target")
            };
            var output_path = $"Assets/AssetBundles/{tar_path}";
            if (!Directory.Exists(output_path))
                Directory.CreateDirectory(output_path);
            try
            {
                BuildPipeline.BuildAssetBundles(output_path, BuildAssetBundleOptions.ChunkBasedCompression, tar);

                CopyFile($"{output_path}/{BUNDLE_NAME}", $"{BUILD_DIR}/{BUNDLE_NAME}_{tar_path}");
                CopyFile($"{output_path}/{BUNDLE_NAME}.manifest", $"{BUILD_DIR}/{BUNDLE_NAME}_{tar_path}.manifest");
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }

        Debug.Log("Bundles bundled!!!");
    }
#endif
}
