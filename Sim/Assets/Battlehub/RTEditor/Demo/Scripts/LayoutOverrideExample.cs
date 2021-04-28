using Battlehub.RTCommon;
using Battlehub.UIControls.DockPanels;
using UnityEngine;

namespace Battlehub.RTEditor.Demo
{
    public class LayoutOverrideExample : EditorOverride
    {
        protected override void OnEditorCreated(object obj)
        {
            OverrideDefaultLayout();
        }

        protected override void OnEditorExist()
        {
            OverrideDefaultLayout();

            IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
            if (editor.IsOpened)
            {
                IWindowManager wm = IOC.Resolve<IWindowManager>();
                wm.SetLayout(DefaultLayout, RuntimeWindowType.Scene.ToString());
            }
        }

        private void OverrideDefaultLayout()
        {
            IWindowManager wm = IOC.Resolve<IWindowManager>();
            wm.OverrideDefaultLayout(DefaultLayout, RuntimeWindowType.Scene.ToString());
        }

        static LayoutInfo DefaultLayout(IWindowManager wm)
        {
            bool isDialog;

            WindowDescriptor sceneWd;
            GameObject sceneContent;
            wm.CreateWindow(RuntimeWindowType.Scene.ToString(), out sceneWd, out sceneContent, out isDialog);

            WindowDescriptor gameWd;
            GameObject gameContent;
            wm.CreateWindow(RuntimeWindowType.Game.ToString(), out gameWd, out gameContent, out isDialog);

            WindowDescriptor inspectorWd;
            GameObject inspectorContent;
            wm.CreateWindow(RuntimeWindowType.Inspector.ToString(), out inspectorWd, out inspectorContent, out isDialog);

            WindowDescriptor hierarchyWd;
            GameObject hierarchyContent;
            wm.CreateWindow(RuntimeWindowType.Hierarchy.ToString(), out hierarchyWd, out hierarchyContent, out isDialog);

            LayoutInfo layout = new LayoutInfo(false,
                new LayoutInfo(
                    new LayoutInfo(sceneContent.transform, sceneWd.Header, sceneWd.Icon),
                    new LayoutInfo(gameContent.transform, gameWd.Header, gameWd.Icon)),
                new LayoutInfo(true,
                    new LayoutInfo(inspectorContent.transform, inspectorWd.Header, inspectorWd.Icon),
                    new LayoutInfo(hierarchyContent.transform, hierarchyWd.Header, hierarchyWd.Icon),
                    0.5f),
                0.75f);

            return layout;
        }
    }
}
