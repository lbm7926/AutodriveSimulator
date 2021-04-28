using Battlehub.RTCommon;
using Battlehub.RTHandles;
using Battlehub.UIControls.DockPanels;
using UnityEngine;

namespace Battlehub.RTEditor.Demo
{
    public class SceneParametersOverrideExample : EditorOverride
    {
        protected override void OnEditorExist()
        {
            IWindowManager wm = IOC.Resolve<IWindowManager>();
            wm.AfterLayout += OnAfterLayout;
        }

        private void OnAfterLayout(IWindowManager wm)
        {
            wm.AfterLayout -= OnAfterLayout;

            const int windowNumber = 0;
            RuntimeWindow window = wm.GetWindows(RuntimeWindowType.Scene.ToString())[windowNumber].GetComponent<RuntimeWindow>();
            
            IRuntimeSceneComponent scene = window.IOCContainer.Resolve<IRuntimeSceneComponent>();

            scene.Pivot = new Vector3(5, 0, 0);
            scene.CameraPosition = Vector3.right * 20;
            scene.IsOrthographic = true;

            scene.PositionHandle.GridSize = 2;
            scene.RotationHandle.GridSize = 5;
            scene.SizeOfGrid = 2;

            scene.IsScaleHandleEnabled = false;
            scene.IsSceneGizmoEnabled = true;
            scene.IsBoxSelectionEnabled = false;

            scene.CanSelect = true;
            scene.CanSelectAll = true;

            scene.CanRotate = true;
            scene.CanPan = false;
            scene.CanZoom = true;

            Tab tab = Region.FindTab(window.transform);
            tab.CanClose = false;

            scene.SceneGizmoTransform.anchorMax = new Vector2(1, 0);
            scene.SceneGizmoTransform.anchorMin = new Vector2(1, 0);
            scene.SceneGizmoTransform.pivot = new Vector2(1, 0);

            
        }
    }
}
