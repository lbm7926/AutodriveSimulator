using UnityEditor;

namespace Battlehub.RTSL
{

    public class CreateAssetBundles
    {
        [MenuItem("Tools/Runtime SaveLoad/Build Asset Bundles")]
        [MenuItem("Assets/Build AssetBundles")]
        public static void BuildAllAssetBundles()
        {
            if (!AssetDatabase.IsValidFolder("Assets/StreamingAssets"))
            {
                AssetDatabase.CreateFolder("Assets", "StreamingAssets");
            }

            BuildPipeline.BuildAssetBundles("Assets/StreamingAssets", BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
            AssetDatabase.Refresh();
        }
    }


}
