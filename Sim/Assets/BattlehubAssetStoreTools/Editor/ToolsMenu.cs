using Battlehub.RTSL;
using System.IO;
using UnityEditor;

namespace Battlehub.AssetStoreTools
{
    public static class ToolsMenu
    {
        [MenuItem("Asset Store Tools/RT SaveLoad Clean")]
        public static void CleanRTSL()
        {
            AssetDatabase.DeleteAsset("Assets/Battlehub/RTSL_Data/CustomImplementation");
            AssetDatabase.DeleteAsset("Assets/Battlehub/RTSL_Data/Mappings");
            AssetDatabase.DeleteAsset("Assets/Battlehub/RTSL_Data/Scripts");
            AssetDatabase.DeleteAsset("Assets/Battlehub/RTSL_Data/RTSLTypeModel.dll");
        }

        [MenuItem("Asset Store Tools/RT Editor Clean")]
        public static void CleanRTE()
        {
            AssetDatabase.DeleteAsset("Assets/Battlehub/RTEditor_Data");
        }

        [MenuItem("Asset Store Tools/Clean All")]
        public static void CleanAll()
        {
            CleanRTSL();
            CleanRTE();
        }
    }

}
