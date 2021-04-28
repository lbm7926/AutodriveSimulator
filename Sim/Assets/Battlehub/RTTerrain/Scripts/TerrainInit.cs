using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.UIControls.MenuControl;
using UnityEngine;
namespace Battlehub.RTTerrain
{
    [MenuDefinition(-1)]
    public class TerrainInit : EditorOverride
    {
        [SerializeField]
        private GameObject m_terrainView = null;

        protected override void OnEditorExist()
        {
            base.OnEditorExist();
            Register();
        }

        private void Register()
        {
            IWindowManager wm = IOC.Resolve<IWindowManager>();
            if (m_terrainView != null)
            {
                RegisterWindow(wm, "TerrainEditor", "Terrain Editor", 
                    Resources.Load<Sprite>("icons8-earth-element-24"), m_terrainView, false); 
            }
        }

        private void RegisterWindow(IWindowManager wm, string typeName, string header, Sprite icon, GameObject prefab, bool isDialog)
        {
            wm.RegisterWindow(new CustomWindowDescriptor
            {
                IsDialog = isDialog,
                TypeName = typeName,
                Descriptor = new WindowDescriptor
                {
                    Header = header,
                    Icon = icon,
                    MaxWindows = 1,
                    ContentPrefab = prefab
                }
            });
        }

        [MenuCommand("MenuWindow/Terrain Editor")]
        public static void OpenProBuilder()
        {
            IWindowManager wm = IOC.Resolve<IWindowManager>();
            wm.CreateWindow("TerrainEditor");
        }
    }
}


