using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.UIControls.MenuControl;
using UnityEngine;
namespace Battlehub.RTBuilder
{
    [MenuDefinition(-1)]
    public class ProBuilderInit : EditorOverride
    {
        [SerializeField]
        private GameObject m_proBuilderWindow = null;

        [SerializeField]
        private GameObject m_materialPaletteWindow = null;

        [SerializeField]
        private GameObject m_uvEditorWindow = null;

        protected override void OnEditorExist()
        {
            base.OnEditorExist();
            Register();
        }

        private void Register()
        {
            IWindowManager wm = IOC.Resolve<IWindowManager>();
            if (m_proBuilderWindow != null)
            {
                RegisterWindow(wm, "ProBuilder", "Builder", 
                    Resources.Load<Sprite>("hammer-24"), m_proBuilderWindow, false); 
            }

            if(m_materialPaletteWindow != null)
            {
                RegisterWindow(wm, "MaterialPalette", "Material Editor",
                    Resources.Load<Sprite>("palette-24"), m_materialPaletteWindow, false);
            }

            if(m_uvEditorWindow != null)
            {
                RegisterWindow(wm, "UVEditor", "UV Editor",
                    Resources.Load<Sprite>("uv-24"), m_uvEditorWindow, false);
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

        [MenuCommand("MenuWindow/Builder")]
        public static void OpenProBuilder()
        {
            IWindowManager wm = IOC.Resolve<IWindowManager>();
            wm.CreateWindow("ProBuilder");
        }
    }
}


