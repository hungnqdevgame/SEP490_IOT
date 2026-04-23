#if UNITY_EDITOR
using UnityEditor; // Cực kỳ quan trọng để sửa lỗi 'namespace name could not be found'
using System.IO;
using UnityEngine;

public class CreateAssetBundles
{
    // MenuItem giúp tạo một nút bấm trên thanh công cụ của Unity
    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        string assetBundleDirectory = "Assets/AssetBundles";

        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        BuildPipeline.BuildAssetBundles(assetBundleDirectory,
                                        BuildAssetBundleOptions.None,
                                        BuildTarget.Android);
    }
}
#endif