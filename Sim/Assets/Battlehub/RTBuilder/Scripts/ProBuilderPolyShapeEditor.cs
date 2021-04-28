using UnityEngine;

using Battlehub.ProBuilderIntegration;
using Battlehub.RTCommon;
using Battlehub.RTHandles;
using Battlehub.Utils;

namespace Battlehub.RTBuilder
{
    public interface IPolyShapeEditor
    {
        
    }

    [DefaultExecutionOrder(-89)]
    public class ProBuilderPolyShapeEditor : MonoBehaviour, IPolyShapeEditor
    {
        private PBPolyShape m_polyShape;
        private bool m_endEditOnPointerUp;
        private Transform m_pivot;

        private IRTE m_rte;
        private IProBuilderTool m_tool;
        private IRuntimeSelectionComponent m_selectionComponent;

        private void Awake()
        {
            m_rte = IOC.Resolve<IRTE>();
            m_pivot = new GameObject("PolyShapePivot").transform;
            LockAxes axes = m_pivot.gameObject.AddComponent<LockAxes>();
            axes.PositionY = true;
            axes.RotationFree = axes.RotationScreen = axes.RotationX = axes.RotationY = axes.RotationZ = true;
            axes.ScaleX = axes.ScaleY = axes.ScaleZ = true;
            m_pivot.transform.SetParent(transform);
        }

        private void Start()
        {
            m_tool = IOC.Resolve<IProBuilderTool>();
            m_tool.ModeChanged += OnModeChanged;

            if (m_rte != null)
            {
                m_rte.ActiveWindowChanged += OnActiveWindowChanged;

                if (m_rte.ActiveWindow != null && m_rte.ActiveWindow.WindowType == RuntimeWindowType.Scene)
                {
                    m_selectionComponent = m_rte.ActiveWindow.IOCContainer.Resolve<IRuntimeSelectionComponent>();
                    SubscribeToEvents();
                }
            }
        }

        private void OnDestroy()
        {
            if (m_tool != null)
            {
                m_tool.ModeChanged -= OnModeChanged;
            }

            if (m_rte != null)
            {
                m_rte.ActiveWindowChanged -= OnActiveWindowChanged;
            }

            UnsubscribeFromEvents();
        }

        private void OnActiveWindowChanged(RuntimeWindow window)
        {
            UnsubscribeFromEvents();

            if (m_rte.ActiveWindow != null && m_rte.ActiveWindow.WindowType == RuntimeWindowType.Scene)
            {
                m_selectionComponent = m_rte.ActiveWindow.IOCContainer.Resolve<IRuntimeSelectionComponent>();
            }
            else
            {
                m_selectionComponent = null;
            }

            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
           
            if (m_selectionComponent != null)
            {
                if (m_selectionComponent.PositionHandle != null)
                {
                    m_selectionComponent.PositionHandle.BeforeDrag.AddListener(OnBeginMove);
                    m_selectionComponent.PositionHandle.Drop.AddListener(OnEndMove);
                }
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (m_selectionComponent != null)
            {
                if (m_selectionComponent.PositionHandle != null)
                {
                    m_selectionComponent.PositionHandle.BeforeDrag.RemoveListener(OnBeginMove);
                    m_selectionComponent.PositionHandle.Drop.RemoveListener(OnEndMove);
                }
            }
        }

        private void OnModeChanged(ProBuilderToolMode oldMode)
        {
            if(m_tool.Mode == ProBuilderToolMode.PolyShape)
            {
                m_polyShape = m_rte.Selection.activeGameObject.GetComponent<PBPolyShape>();
                PolyShapeUpdatePivot();
                m_polyShape.IsEditing = true;
                SetLayer(m_polyShape.gameObject);

                if (m_polyShape.Stage > 0)
                {
                    m_rte.Selection.Enabled = false;
                }

                MeshEditorState oldState = m_polyShape.GetState(false);
                m_polyShape.Refresh();
                MeshEditorState newState = m_polyShape.GetState(false);
                RecordState(oldState, newState);
            }
            else if(oldMode == ProBuilderToolMode.PolyShape)
            {
                m_polyShape.IsEditing = false;
                m_rte.Selection.Enabled = true;
                m_rte.Selection.activeGameObject = m_polyShape.gameObject;
            }
        }

        private void SetLayer(GameObject go)
        {
            int layer = m_rte.CameraLayerSettings.AllScenesLayer;

            foreach(Transform child in go.GetComponentsInChildren<Transform>(true))
            {
                if(child.transform == go.transform)
                {
                    continue;
                }

                child.gameObject.layer = layer;
            }
        }

        private void LateUpdate()
        {
            if (m_rte.ActiveWindow == null)
            {
                return;
            }

            if (m_rte.ActiveWindow.WindowType != RuntimeWindowType.Scene)
            {
                return;
            }

            if (!m_rte.ActiveWindow.Camera)
            {
                return;
            }

            if (m_polyShape != null && m_polyShape.IsEditing)
            {
                BaseHandle baseHandle = m_rte.Tools.ActiveTool as BaseHandle;
                if (baseHandle != null && baseHandle.IsDragging && m_rte.Selection.activeGameObject == m_pivot.gameObject)
                {
                    if(baseHandle is PositionHandle)
                    {
                        m_polyShape.SelectedPosition = m_polyShape.transform.InverseTransformPoint(m_pivot.position);
                        m_endEditOnPointerUp = false;
                    }
                }
                else
                {
                    if (m_rte.Input.GetPointerDown(0))
                    {
                        m_endEditOnPointerUp = true;
                        if (m_rte.Tools.ActiveTool is BaseHandle)
                        {
                            BaseHandle handle = (BaseHandle)m_rte.Tools.ActiveTool;
                            if (handle.IsDragging)
                            {
                                m_endEditOnPointerUp = false;
                            }
                        }
                    }
                    else if (m_rte.Input.GetKeyDown(KeyCode.Return))
                    {
                        EndEditing(true);
                    }
                    else if (m_rte.Input.GetPointerUp(0))
                    {
                        if (!m_rte.ActiveWindow.IsPointerOver)
                        {
                            return;
                        }

                        if (m_endEditOnPointerUp)
                        {
                            RuntimeWindow window = m_rte.ActiveWindow;
                            int oldSelectedIndex = m_polyShape.SelectedIndex;
                            if (m_polyShape.Click(window.Camera, m_rte.Input.GetPointerXY(0)))
                            {
                                EndEditing(false, oldSelectedIndex);
                            }
                        }
                    }
                } 
            }
        }

        private void EndEditing(bool forceEndEditing, int oldSelectedIndex = -1)
        {
            m_rte.Selection.Enabled = true;
            if (m_polyShape == null || !m_polyShape.IsEditing)
            {
                return;
            }
            if (m_polyShape.Stage == 0)
            {
                if (m_polyShape.VertexCount < 3)
                {
                    m_tool.Mode = ProBuilderToolMode.Object;
                    return;
                }
                else
                {
                    m_polyShape.Stage++;
                }
            }

            if (m_polyShape.Stage > 0)
            {
                if (forceEndEditing)
                {
                    m_tool.Mode = ProBuilderToolMode.Object;
                }
                else
                {
                    Debug.Assert(m_polyShape.SelectedIndex >= 0);
                    m_rte.Undo.BeginRecord();
                    RecordSelection(m_polyShape, oldSelectedIndex);
                    PolyShapeUpdatePivot();
                    m_rte.Selection.activeObject = m_pivot.gameObject;
                    m_rte.Undo.EndRecord();
                    m_rte.Selection.Enabled = false;
                }
            }
        }

        private void PolyShapeUpdatePivot()
        {
            if (m_polyShape.SelectedIndex >= 0)
            {
                m_pivot.position = m_polyShape.transform.TransformPoint(m_polyShape.SelectedPosition);
                m_pivot.rotation = Quaternion.identity;
            }
        }


        private void RecordIsEditing(PBPolyShape polyShape, bool value)
        {
            UndoRedoCallback redo = record =>
            {
                m_polyShape = polyShape;
                m_polyShape.IsEditing = value;
                return true;
            };

            UndoRedoCallback undo = record =>
            {
                m_polyShape = polyShape;
                m_polyShape.IsEditing = !value;
                return true;
            };

            m_rte.Undo.CreateRecord(redo, undo);
        }

        private void RecordSelection(PBPolyShape polyShape, int oldSelectedIndex)
        {
            int selectedIndex = polyShape.SelectedIndex;
            UndoRedoCallback redo = record =>
            {
                m_polyShape = polyShape;
                m_polyShape.SelectedIndex = selectedIndex;
                PolyShapeUpdatePivot();
                return true;
            };

            UndoRedoCallback undo = record =>
            {
                m_polyShape = polyShape;
                m_polyShape.SelectedIndex = oldSelectedIndex;
                PolyShapeUpdatePivot();
                return true;
            };

            m_rte.Undo.CreateRecord(redo, undo);
        }

        public void RecordState(MeshEditorState oldState, MeshEditorState newState,
           bool oldStateChanged = true, bool newStateChanged = true)
        {
            UndoRedoCallback redo = record =>
            {
                if (newState != null)
                {
                    m_polyShape.SetState(newState);
                    return newStateChanged;
                }
                return false;
            };

            UndoRedoCallback undo = record =>
            {
                if (oldState != null)
                {
                    m_polyShape.SetState(oldState);
                    return oldStateChanged;
                }
                return false;
            };

            IOC.Resolve<IRTE>().Undo.CreateRecord(redo, undo);
        }

        private void OnBeginMove(BaseHandle positionHandle)
        {
            if (m_tool.Mode != ProBuilderToolMode.PolyShape)
            {
                return;
            }

            if(m_polyShape.Stage == 0)
            {
                return;
            }

            if(m_rte.Selection.activeGameObject != m_pivot.gameObject)
            {
                return;
            }

            positionHandle.EnableUndo = false;

            m_rte.Undo.BeginRecord();
            m_rte.Undo.BeginRecordTransform(m_pivot);
            m_rte.Undo.RecordValue(m_polyShape, Strong.PropertyInfo((PBPolyShape x) => x.SelectedPosition));
            m_rte.Undo.EndRecord();
        }

        private void OnEndMove(BaseHandle positionHandle)
        {
            if (m_tool.Mode != ProBuilderToolMode.PolyShape)
            {
                return;
            }
            if (m_polyShape.Stage == 0)
            {
                return;
            }
            if (m_rte.Selection.activeGameObject != m_pivot.gameObject)
            {
                return;
            }

            positionHandle.EnableUndo = true;

            m_rte.Undo.BeginRecord();
            m_rte.Undo.EndRecordTransform(m_pivot);
            m_rte.Undo.RecordValue(m_polyShape, Strong.PropertyInfo((PBPolyShape x) => x.SelectedPosition));
            m_rte.Undo.EndRecord();
        }
    }
}
