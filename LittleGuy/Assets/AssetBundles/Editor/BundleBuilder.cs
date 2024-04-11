using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BundleBuilder
{
    [MenuItem("Assets/Build AssetBundles")]
	public static void BuildBundles()
    {
        BuildPipeline.BuildAssetBundles("Assets/AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
    }
}
