using Battlehub.RTCommon;
using Battlehub.UIControls.DockPanels;

namespace Battlehub.RTEditor.Demo
{
    public class PersistentLayoutExample : EditorOverride
    {
        public const string LayoutName = "Default";

        protected override void OnEditorExist()
        {
          //  UnityEngine.PlayerPrefs.DeleteAll();

            base.OnEditorExist();

            IWindowManager wm = IOC.Resolve<IWindowManager>();

            if (wm.LayoutExist(LayoutName))
            {
                wm.OverrideDefaultLayout(DefaultLayout, RuntimeWindowType.Scene.ToString());

                IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
                if (editor.IsOpened)
                {
                    wm.SetLayout(DefaultLayout, RuntimeWindowType.Scene.ToString());
                }
            }
        }

        protected override void OnEditorClosed()
        {
            base.OnEditorClosed();
            IWindowManager wm = IOC.Resolve<IWindowManager>();
            wm.SaveLayout(LayoutName);
        }

        static LayoutInfo DefaultLayout(IWindowManager wm)
        {
            return wm.GetLayout(LayoutName);
        }
    }
}
