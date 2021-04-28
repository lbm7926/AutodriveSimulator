using Battlehub.RTCommon;
using UnityEngine;

namespace Battlehub.RTEditor.Demo
{
    public class UIColorsOverrideExample : EditorOverride
    {
        protected override void OnEditorExist()
        {
            RTEColors colors = new RTEColors();
            colors.Primary = Color.red;

            IRTEAppearance appearance = IOC.Resolve<IRTEAppearance>();
            appearance.Colors = colors; 
        }
    }
}
