using Battlehub.RTCommon;
using Battlehub.RTHandles;
using UnityEngine;

namespace Battlehub.RTEditor.Demo
{
    public class UIScaleOverrideExample : EditorOverride
    {
        [SerializeField]
        private float Scale = 2;

        protected override void OnEditorExist()
        {
            IRTEAppearance appearance = IOC.Resolve<IRTEAppearance>();
            appearance.UIScaler.scaleFactor = Scale;

            IRuntimeHandlesComponent handles = IOC.Resolve<IRuntimeHandlesComponent>();
            handles.HandleScale = Scale;
            handles.SceneGizmoScale = Scale;
        }
    }
}
