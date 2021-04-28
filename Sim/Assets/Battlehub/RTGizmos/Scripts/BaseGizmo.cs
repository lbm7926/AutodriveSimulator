using UnityEngine;
using Battlehub.RTCommon;
using UnityEngine.EventSystems;

namespace Battlehub.RTGizmos
{
    [DefaultExecutionOrder(-50)]
    public abstract class BaseGizmo : RTEComponent, IGL
    {
        public float GridSize = 1.0f;
        public Color LineColor = new Color(0.0f, 1, 0.0f, 0.75f);
        public Color HandlesColor = new Color(0.0f, 1, 0.0f, 0.75f);
        public Color SelectionColor = new Color(1.0f, 1.0f, 0, 1.0f);

        public bool EnableUndo = true;

        /// <summary>
        /// Key which activates Unit Snapping
        /// </summary>
        public KeyCode UnitSnapKey = KeyCode.LeftControl;
        public Camera SceneCamera;

        /// <summary>
        /// Screen space selection margin in pixels
        /// </summary>
        public float SelectionMargin = 10;


        public Transform Target;

        private bool m_isDragging;
        private int m_dragIndex;
        private Plane m_dragPlane;
        private Vector3 m_prevPoint;
        private Vector3 m_normal;

        // private static BaseGizmo m_dragGizmo;
        protected int DragIndex
        {
            get { return m_dragIndex; }
        }

        protected bool IsDragging
        {
            get { return m_isDragging; }
        }

        protected abstract Matrix4x4 HandlesTransform
        {
            get;
        }

        protected virtual Matrix4x4 HandlesTransformInverse
        {
            get { return Matrix4x4.TRS(Target.position, Target.rotation, Target.lossyScale).inverse; }
        }

        private Vector3[] m_handlesNormals;
        private Vector3[] m_handlesPositions;
        protected virtual Vector3[] HandlesPositions
        {
            get { return m_handlesPositions; }
        }

        protected virtual Vector3[] HandlesNormals
        {
            get { return m_handlesNormals; }
        }

        private Matrix4x4 m_handlesTransform;
        private Matrix4x4 m_handlesInverseTransform;


        public override RuntimeWindow Window
        {
            get { return m_window; }
            set
            {
                m_window = value;
                m_editor = IOC.Resolve<IRTE>();
            }
        }

        private void Start()
        {
            BaseGizmoInput input = GetComponent<BaseGizmoInput>();
            if (input == null || input.Gizmo != this)
            {
                input = gameObject.AddComponent<BaseGizmoInput>();
                input.Gizmo = this;
            }

            if (SceneCamera == null)
            {
                SceneCamera = Window.Camera;
            }

            if (SceneCamera == null)
            {
                SceneCamera = Camera.main;
            }

            if(Target == null)
            {
                Target = transform;
            }

            if (EnableUndo)
            {
                if (!RuntimeUndoInput.IsInitialized)
                {
                    GameObject runtimeUndo = new GameObject();
                    runtimeUndo.name = "RuntimeUndo";
                    runtimeUndo.AddComponent<RuntimeUndoInput>();
                }
            }

            if (GLRenderer.Instance == null)
            {
                GameObject glRenderer = new GameObject();
                glRenderer.name = "GLRenderer";
                glRenderer.AddComponent<GLRenderer>();
            }

            if (SceneCamera != null)
            {
                if (!SceneCamera.GetComponent<GLCamera>())
                {
                    SceneCamera.gameObject.AddComponent<GLCamera>();
                }
            }

            if (GLRenderer.Instance != null)
            {
                GLRenderer.Instance.Add(this);
            }

            StartOverride();
        }

        private void OnEnable()
        {
            if (GLRenderer.Instance != null)
            {
                GLRenderer.Instance.Add(this);
            }

            OnEnableOverride();
        }

        private void OnDisable()
        {
            if (GLRenderer.Instance != null)
            {
                GLRenderer.Instance.Remove(this);
            }

            OnDisableOverride();
        }

        private void Update()
        {
            if (m_isDragging)
            {
                Vector3 point;
                if (GetPointOnDragPlane(Window.Editor.Input.GetPointerXY(0), out point))
                {
                    Vector3 offset = m_handlesInverseTransform.MultiplyVector(point - m_prevPoint);
                    offset = Vector3.Project(offset, m_normal);
                    if (Window.Editor.Input.GetKey(UnitSnapKey) || Window.Editor.Tools.UnitSnapping)
                    {
                        Vector3 gridOffset = Vector3.zero;
                        if (Mathf.Abs(offset.x * 1.5f) >= GridSize)
                        {
                            gridOffset.x = GridSize * Mathf.Sign(offset.x);
                        }

                        if (Mathf.Abs(offset.y * 1.5f) >= GridSize)
                        {
                            gridOffset.y = GridSize * Mathf.Sign(offset.y);
                        }

                        if (Mathf.Abs(offset.z * 1.5f) >= GridSize)
                        {
                            gridOffset.z = GridSize * Mathf.Sign(offset.z);
                        }

                        if (gridOffset != Vector3.zero)
                        {
                            if (OnDrag(m_dragIndex, gridOffset))
                            {
                                m_prevPoint = point;
                            }
                        }
                    }
                    else
                    {
                        if (OnDrag(m_dragIndex, offset))
                        {
                            m_prevPoint = point;
                        }
                    }

                }

            }

            UpdateOverride();
        }

        /// Lifecycle method overrides
        protected override void AwakeOverride()
        {
            base.AwakeOverride();
            m_handlesPositions = RuntimeGizmos.GetHandlesPositions();
            m_handlesNormals = RuntimeGizmos.GetHandlesNormals();

            
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            BaseGizmoInput gizmoInput = GetComponent<BaseGizmoInput>();
            if (gizmoInput)
            {
                Destroy(gizmoInput);
            }

            if (GLRenderer.Instance != null)
            {
                GLRenderer.Instance.Remove(this);
            }

            if (Window.Editor.Tools.ActiveTool == this)
            {
                Window.Editor.Tools.ActiveTool = null;
            }
        }

        protected virtual void StartOverride()
        {

        }

        protected virtual void OnEnableOverride()
        {

        }

        protected virtual void OnDisableOverride()
        {

        }

        protected virtual void UpdateOverride()
        {

        }

        protected virtual void BeginRecordOverride()
        {

        }

        protected virtual void EndRecordOverride()
        {

        }

        protected override void OnActiveWindowChanged(RuntimeWindow deactivatedWindow)
        {
            if (Editor.ActiveWindow != null && Editor.ActiveWindow.WindowType == RuntimeWindowType.Scene)
            {
                Window = Editor.ActiveWindow;
                SceneCamera = Window.Camera;
            }

            base.OnActiveWindowChanged(deactivatedWindow);
        }


        /// Drag And Drop virtual methods
        protected virtual bool OnBeginDrag(int index)
        {
            return true;
        }

        protected virtual bool OnDrag(int index, Vector3 offset)
        {
            return true;
        }

        protected virtual void OnDrop()
        {

        }

        void IGL.Draw(int cullingMask, Camera camera)
        {
            //RTLayer layer = RTLayer.SceneView;
            //if ((cullingMask & (int)layer) == 0)
            //{
            //    return;
            //}

            if(Target == null)
            {
                return;
            }

            DrawOverride(camera);
        }

        protected virtual void DrawOverride(Camera camera)
        {
            
        }

        protected virtual bool HitOverride(int index, Vector3 vertex, Vector3 normal)
        {
            return true;
        }

        private int Hit(Vector2 pointer, Vector3[] vertices, Vector3[] normals)
        {
            float minMag = float.MaxValue;
            int index = -1;
            for (int i = 0; i < vertices.Length; ++i)
            {
                Vector3 normal = normals[i];
                normal = HandlesTransform.MultiplyVector(normal);
                Vector3 vertex = vertices[i];
                Vector3 vertexWorld = HandlesTransform.MultiplyPoint(vertices[i]);

                if (Mathf.Abs(Vector3.Dot((SceneCamera.transform.position - vertexWorld).normalized, normal.normalized)) > 0.999f)
                {
                    continue;
                }

                if (!HitOverride(i, vertex, normal))
                {
                    continue;
                }

                Vector2 vertexScreen = SceneCamera.WorldToScreenPoint(vertexWorld);
                float distance = (vertexScreen - pointer).magnitude;
                if(distance < minMag && distance <= SelectionMargin)
                {
                    minMag = distance;
                    index = i;
                }
            }

            return index;
        }

        protected Plane GetDragPlane()
        {
            Vector3 toCam = SceneCamera.transform.position - HandlesTransform.MultiplyPoint(HandlesPositions[m_dragIndex]); // SceneCamera.cameraToWorldMatrix.MultiplyVector(Vector3.forward); 
            Vector3 dragPlaneVector = toCam.normalized;

            Vector3 position = m_handlesTransform.MultiplyPoint(Vector3.zero); 
           
            Plane dragPlane = new Plane(dragPlaneVector, position);
            return dragPlane;
        }

        protected bool GetPointOnDragPlane(Vector3 screenPos, out Vector3 point)
        {
            return GetPointOnDragPlane(m_dragPlane, screenPos, out point);
        }

        protected bool GetPointOnDragPlane(Plane dragPlane, Vector3 screenPos, out Vector3 point)
        {
            Ray ray = SceneCamera.ScreenPointToRay(screenPos);
            float distance;
            if (dragPlane.Raycast(ray, out distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        public void BeginDrag()
        {
            if (!IsWindowActive)
            {
                return;
            }

            if (SceneCamera == null)
            {
                Debug.LogError("Camera is null");
                return;
            }

            if (Window.Editor.Tools.IsViewing)
            {
                return;
            }

            if (Window.Editor.Tools.ActiveTool != null)
            {
                return;
            }

            if (Window.Camera != null && (!Window.IsPointerOver || Window.WindowType != RuntimeWindowType.Scene))
            {
                return;
            }

            Vector2 pointer = Window.Editor.Input.GetPointerXY(0);
            m_dragIndex = Hit(pointer, HandlesPositions, HandlesNormals);
            if (m_dragIndex >= 0 && OnBeginDrag(m_dragIndex))
            {
                m_handlesTransform = HandlesTransform;
                m_handlesInverseTransform = HandlesTransformInverse;
                m_dragPlane = GetDragPlane();
                m_isDragging = GetPointOnDragPlane(Window.Editor.Input.GetPointerXY(0), out m_prevPoint);
                m_normal = HandlesNormals[m_dragIndex].normalized;
                if (m_isDragging)
                {
                    Window.Editor.Tools.ActiveTool = this;
                }
                if (EnableUndo)
                {
                    BeginRecordOverride();
                }
            }
        }

        public void EndDrag()
        {
            if (m_isDragging)
            {
                OnDrop();
                bool isRecording = Window.Editor.Undo.IsRecording;
                if (!isRecording)
                {
                    Window.Editor.Undo.BeginRecord();
                }
                EndRecordOverride();
                if (!isRecording)
                {
                    Window.Editor.Undo.EndRecord();
                }
                m_isDragging = false;
                Window.Editor.Tools.ActiveTool = null;
            }
        }

    }
}


