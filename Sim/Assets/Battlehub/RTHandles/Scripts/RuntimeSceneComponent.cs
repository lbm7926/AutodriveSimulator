using UnityEngine;
using Battlehub.Utils;
using Battlehub.RTCommon;
namespace Battlehub.RTHandles
{
    public interface IRuntimeSceneComponent : IRuntimeSelectionComponent
    {
        RectTransform SceneGizmoTransform
        {
            get;
        }

        bool IsSceneGizmoEnabled
        {
            get;
            set;
        }

        bool CanPan
        {
            get;
            set;
        }

        bool CanZoom
        {
            get;
            set;
        }

        bool ChangeOrthographicSizeOnly
        {
            get;
            set;
        }

        [System.Obsolete("Use CanRotate instead")]
        bool CanOrbit
        {
            get;
            set;
        }

        bool CanRotate
        {
            get;
            set;
        }

        float SizeOfGrid
        {
            get;
            set;
        }

        GameObject GameObject
        {
            get;
        }
    }

    public class RuntimeSceneComponent : RuntimeSelectionComponent, IRuntimeSceneComponent
    {
        public Texture2D ViewTexture;
        public Texture2D MoveTexture;
        
        private Plane m_dragPlane;
        private Vector3 m_lastMousePosition;
        private bool m_lockInput;

        private MouseOrbit m_mouseOrbit;
        private IAnimationInfo m_focusAnimation;
        private Transform m_autoFocusTransform;
        public float GridSize = 1;

        [SerializeField]
        private SceneGizmo m_sceneGizmo;

        [SerializeField]
        private RectTransform m_sceneGizmoTransform = null;

        [SerializeField]
        private bool m_isSceneGizmoEnabled = true;
        [SerializeField]
        private bool m_canPan = true;
        [SerializeField]
        private bool m_canZoom = true;
        [SerializeField]
        private bool m_changeOrthographicSizeOnly = true;
        [SerializeField]
        private bool m_canRotate = true;
        
        public bool IsSceneGizmoEnabled
        {
            get { return m_isSceneGizmoEnabled && m_sceneGizmo != null; }
            set
            {
                m_isSceneGizmoEnabled = value;
                if(m_sceneGizmo != null)
                {
                    m_sceneGizmo.gameObject.SetActive(value);
                }
            }
        }

        public RectTransform SceneGizmoTransform
        {
            get { return m_sceneGizmoTransform; }
        }

        public bool CanPan
        {
            get { return m_canPan; }
            set { m_canPan = value; }
        }
        public bool CanZoom
        {
            get { return m_canZoom; }
            set
            {
                m_canZoom = value;
                m_mouseOrbit.CanZoom = value;
            }
        }

        public bool CanOrbit
        {
            get { return CanRotate; }
            set
            {
                CanRotate = value;
            }
        }

        public bool CanRotate
        {
            get { return m_canRotate; }
            set
            {
                m_canRotate = value;
                m_mouseOrbit.CanOrbit = value;
            }
        }

        public bool ChangeOrthographicSizeOnly
        {
            get { return m_changeOrthographicSizeOnly; }
            set
            {
                m_changeOrthographicSizeOnly = value;
                m_mouseOrbit.ChangeOrthographicSizeOnly = value;
            }
        }

        public float SizeOfGrid
        {
            get { return GridSize; }
            set { GridSize = value; }
        }

        public override Vector3 Pivot
        {
            get { return base.Pivot; }
            set
            {
                base.Pivot = value;
                m_mouseOrbit.SyncAngles();
            }
        }

        public override Vector3 CameraPosition
        {
            get { return base.CameraPosition; }
            set
            { 
                base.CameraPosition = value;
                m_mouseOrbit.SyncAngles();
            }
        }

        public GameObject GameObject
        {
            get { return gameObject; }
        }

        public override bool IsOrthographic
        {
            get { return IsOrthographic; }
            set
            {
                if(m_sceneGizmo != null)
                {
                    if(m_sceneGizmo.IsOrthographic != value)
                    {
                        m_sceneGizmo.IsOrthographic = value;
                    }
                }
                else
                {
                    IsOrthographic = value;
                }
                
            }
        }

        protected override void AwakeOverride()
        {
            base.AwakeOverride();

            Window.IOCContainer.RegisterFallback<IRuntimeSceneComponent>(this);

            GameObject runGO = new GameObject("Run");
            runGO.transform.SetParent(transform, false);
            runGO.name = "Run";
            runGO.AddComponent<Run>();

            if (ViewTexture == null)
            {
                ViewTexture = Resources.Load<Texture2D>("RTH_Eye");
            }
            if (MoveTexture == null)
            {
                MoveTexture = Resources.Load<Texture2D>("RTH_Hand");
            }

            if (GetComponent<RuntimeSelectionInputBase>() == null)
            {
                gameObject.AddComponent<RuntimeSceneInput>();
            }

            m_mouseOrbit = Window.Camera.GetComponent<MouseOrbit>();
            if (!m_mouseOrbit)
            {
                m_mouseOrbit = Window.Camera.gameObject.AddComponent<MouseOrbit>();
            }
            m_mouseOrbit.Target = PivotTransform;
            m_mouseOrbit.SecondaryTarget = SecondaryPivotTransform;
            m_mouseOrbit.CanZoom = CanZoom;
            m_mouseOrbit.ChangeOrthographicSizeOnly = ChangeOrthographicSizeOnly;
            m_mouseOrbit.CanOrbit = CanOrbit;

            if (m_sceneGizmo == null)
            {
                m_sceneGizmo = GetComponentInChildren<SceneGizmo>(true);
            }

            if (m_sceneGizmo != null)
            {
                if (m_sceneGizmo.Window == null)
                {
                    m_sceneGizmo.Window = Window;
                }
                m_sceneGizmo.OrientationChanging.AddListener(OnSceneGizmoOrientationChanging);
                m_sceneGizmo.OrientationChanged.AddListener(OnSceneGizmoOrientationChanged);
                m_sceneGizmo.ProjectionChanged.AddListener(OnSceneGizmoProjectionChanged);
                m_sceneGizmo.Pivot = PivotTransform;
                if (!IsSceneGizmoEnabled)
                {
                    m_sceneGizmo.gameObject.SetActive(false);
                }
            }

            Window.Camera.transform.LookAt(Pivot);
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            Window.IOCContainer.UnregisterFallback<IRuntimeSceneComponent>(this);

            if (m_mouseOrbit != null)
            {
                Destroy(m_mouseOrbit);
            }

            if (m_sceneGizmo != null)
            {
                m_sceneGizmo.OrientationChanging.RemoveListener(OnSceneGizmoOrientationChanging);
                m_sceneGizmo.OrientationChanged.RemoveListener(OnSceneGizmoOrientationChanged);
                m_sceneGizmo.ProjectionChanged.RemoveListener(OnSceneGizmoProjectionChanged);
            }
        }

        protected virtual void Update()
        {
            if (Editor.Tools.AutoFocus)
            {
                do
                {
                    if (Editor.Tools.ActiveTool != null)
                    {
                        break;
                    }

                    if (m_autoFocusTransform == null)
                    {
                        break;
                    }

                    if (m_autoFocusTransform.position == m_mouseOrbit.Target.position)
                    {
                        break;
                    }

                    if (m_focusAnimation != null && m_focusAnimation.InProgress)
                    {
                        break;
                    }

                    if(m_lockInput)
                    {
                        break;
                    }

                    Vector3 offset = (m_autoFocusTransform.position - m_mouseOrbit.SecondaryTarget.position);
                    Window.Camera.transform.position += offset;
                    m_mouseOrbit.Target.transform.position += offset;
                    m_mouseOrbit.SecondaryTarget.transform.position += offset;
                }
                while (false);
            }
        }

    
        public void UpdateCursorState(bool isPointerOverEditorArea, bool pan, bool rotate)
        {
            if (!isPointerOverEditorArea)
            {
                Window.Editor.CursorHelper.ResetCursor(this);
                return;
            }

            if (pan && CanPan)
            {
                if (rotate && Editor.Tools.Current == RuntimeTool.View)
                {
                    Editor.CursorHelper.SetCursor(this, ViewTexture, new Vector2(0.5f, 0.5f), CursorMode.Auto);
                }
                else
                {
                    Editor.CursorHelper.SetCursor(this, MoveTexture, new Vector2(0.5f, 0.5f), CursorMode.Auto);
                }
            }
            else if (rotate && CanOrbit)
            {
                Editor.CursorHelper.SetCursor(this, ViewTexture, new Vector2(0.5f, 0.5f), CursorMode.Auto);
            }
            else
            {
                Editor.CursorHelper.ResetCursor(this);
            }
        }

        public void SnapToGrid()
        {
            if (m_lockInput)
            {
                return;
            }

            GameObject[] selection = Editor.Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                return;
            }

            Transform activeTransform = selection[0].transform;
            
            Vector3 position = activeTransform.position;
            if (GridSize < 0.01)
            {
                GridSize = 0.01f;
            }
            position.x = Mathf.Round(position.x / GridSize) * GridSize;
            position.y = Mathf.Round(position.y / GridSize) * GridSize;
            position.z = Mathf.Round(position.z / GridSize) * GridSize;
            Vector3 offset = position - activeTransform.position;

            Editor.Undo.BeginRecord();
            for (int i = 0; i < selection.Length; ++i)
            {
                Editor.Undo.BeginRecordTransform(selection[i].transform);
                selection[i].transform.position += offset;
                Editor.Undo.EndRecordTransform(selection[i].transform);
            }
            Editor.Undo.EndRecord();
        }

        public override void Focus()
        {
            if (m_lockInput)
            {
                return;
            }

            if (Editor.Selection.activeTransform == null)
            {
                return;
            }

            m_autoFocusTransform = Editor.Selection.activeTransform;
            if ((Editor.Selection.activeTransform.gameObject.hideFlags & HideFlags.DontSave) != 0)
            {
                return;
            }

            Bounds bounds = CalculateBounds(Editor.Selection.activeTransform);
            float fov = Window.Camera.fieldOfView * Mathf.Deg2Rad;
            float objSize = Mathf.Max(bounds.extents.y, bounds.extents.x, bounds.extents.z) * 2.0f;
            float distance = Mathf.Abs(objSize / Mathf.Sin(fov / 2.0f));

            m_mouseOrbit.Target.position = bounds.center;
            m_mouseOrbit.SecondaryTarget.position = Editor.Selection.activeTransform.position;
            const float duration = 0.1f;

            m_focusAnimation = new Vector3AnimationInfo(Window.Camera.transform.position, m_mouseOrbit.Target.position - distance * Window.Camera.transform.forward, duration, Vector3AnimationInfo.EaseOutCubic,
                (target, value, t, completed) =>
                {
                    if (Window.Camera)
                    {
                        Window.Camera.transform.position = value;
                    }
                });
            Run.Instance.Animation(m_focusAnimation);
            Run.Instance.Animation(new FloatAnimationInfo(m_mouseOrbit.Distance, distance, duration, Vector3AnimationInfo.EaseOutCubic,
                (target, value, t, completed) =>
                {
                    if (m_mouseOrbit)
                    {
                        m_mouseOrbit.Distance = value;
                    }
                }));

            Run.Instance.Animation(new FloatAnimationInfo(Window.Camera.orthographicSize, objSize, duration, Vector3AnimationInfo.EaseOutCubic,
                (target, value, t, completed) =>
                {
                    if (Window.Camera)
                    {
                        Window.Camera.orthographicSize = value;
                    }
                }));
        }

        public void BeginPan(Vector3 mousePosition)
        {
            if (m_lockInput || !CanPan)
            {
                return;
            }
            m_lastMousePosition = mousePosition;
            m_dragPlane = new Plane(-Window.Camera.transform.forward, m_mouseOrbit.Target.position);
        }

        public void Pan(Vector3 mousePosition)
        {
            if (m_lockInput || !CanPan)
            {
                return;
            }
            Vector3 pointOnDragPlane;
            Vector3 prevPointOnDragPlane;
            if (GetPointOnDragPlane(mousePosition, out pointOnDragPlane) &&
                GetPointOnDragPlane(m_lastMousePosition, out prevPointOnDragPlane))
            {
                Vector3 delta = (pointOnDragPlane - prevPointOnDragPlane);
                m_lastMousePosition = mousePosition;
                Window.Camera.transform.position -= delta;
                m_mouseOrbit.Target.position -= delta;
                m_mouseOrbit.SecondaryTarget.position -= delta;
            }
        }

        public void Orbit(float deltaX, float deltaY, float deltaZ)
        {
            if (m_lockInput || !CanOrbit)
            {
                return;
            }
            m_mouseOrbit.Orbit(deltaX, deltaY, deltaZ);
        }
            
        private void OnSceneGizmoOrientationChanging()
        {
            m_lockInput = true;
        }

        private void OnSceneGizmoOrientationChanged()
        {
            m_lockInput = false;
            if (m_mouseOrbit != null)
            {
                m_mouseOrbit.Target.position = Window.Camera.transform.position + Window.Camera.transform.forward * m_mouseOrbit.Distance;
                m_mouseOrbit.SecondaryTarget.position = m_mouseOrbit.Target.position;
                m_mouseOrbit.SyncAngles();
            }
        }

        private void OnSceneGizmoProjectionChanged()
        {
            float fov = Window.Camera.fieldOfView * Mathf.Deg2Rad;
            float distance = (Window.Camera.transform.position - m_mouseOrbit.Target.position).magnitude;
            float objSize = distance * Mathf.Sin(fov / 2);
            Window.Camera.orthographicSize = objSize;
        }

        private Bounds CalculateBounds(Transform t)
        {
            Renderer renderer = t.GetComponentInChildren<Renderer>();
            if (renderer)
            {
                Bounds bounds = renderer.bounds;
                if (bounds.size == Vector3.zero && bounds.center != renderer.transform.position)
                {
                    bounds = TransformBounds(renderer.transform.localToWorldMatrix, bounds);
                }
                CalculateBounds(t, ref bounds);
                if (bounds.extents == Vector3.zero)
                {
                    bounds.extents = new Vector3(0.5f, 0.5f, 0.5f);
                }
                return bounds;
            }

            return new Bounds(t.position, new Vector3(0.5f, 0.5f, 0.5f));
        }

        private void CalculateBounds(Transform t, ref Bounds totalBounds)
        {
            foreach (Transform child in t)
            {
                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer)
                {
                    Bounds bounds = renderer.bounds;
                    if (bounds.size == Vector3.zero && bounds.center != renderer.transform.position)
                    {
                        bounds = TransformBounds(renderer.transform.localToWorldMatrix, bounds);
                    }
                    totalBounds.Encapsulate(bounds.min);
                    totalBounds.Encapsulate(bounds.max);
                }

                CalculateBounds(child, ref totalBounds);
            }
        }

        private static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
        {
            var center = matrix.MultiplyPoint(bounds.center);

            // transform the local extents' axes
            var extents = bounds.extents;
            var axisX = matrix.MultiplyVector(new Vector3(extents.x, 0, 0));
            var axisY = matrix.MultiplyVector(new Vector3(0, extents.y, 0));
            var axisZ = matrix.MultiplyVector(new Vector3(0, 0, extents.z));

            // sum their absolute value to get the world extents
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds { center = center, extents = extents };
        }

        private bool GetPointOnDragPlane(Vector3 mouse, out Vector3 point)
        {
            Ray ray = Window.Camera.ScreenPointToRay(mouse);
            float distance;
            if (m_dragPlane.Raycast(ray, out distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }

            point = Vector3.zero;
            return false;
        }
    }
}
