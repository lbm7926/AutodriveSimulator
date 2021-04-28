using Battlehub.ProBuilderIntegration;
using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.RTHandles;
using Battlehub.UIControls;
using Battlehub.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Battlehub.RTBuilder
{
    public class ProBuilderView : RuntimeWindow
    {
        [SerializeField]
        private VirtualizingTreeView m_commandsList = null;

        [SerializeField]
        private bool m_useSceneViewToolbar = true;
        [SerializeField]
        private ProBuilderToolbar m_sceneViewToolbarPrefab = null;
        [SerializeField]
        private bool m_useToolbar = false;
        [SerializeField]
        private ProBuilderToolbar m_toolbar = null;

        private ToolCmd[] m_commands;
        private GameObject m_proBuilderToolGO;
        private IProBuilderTool m_proBuilderTool;

        
        private bool m_isProBuilderMeshSelected = false;
        private bool m_isNonProBuilderMeshSelected = false;
        private bool m_isPolyShapeSelected = false;
        
        private IWindowManager m_wm;
        
        protected override void AwakeOverride()
        {
            WindowType = RuntimeWindowType.Custom;
            base.AwakeOverride();

            m_wm = IOC.Resolve<IWindowManager>();
            m_wm.WindowCreated += OnWindowCreated;
            m_wm.WindowDestroyed += OnWindowDestroyed;

            m_proBuilderToolGO = new GameObject("ProBuilderTool");
            m_proBuilderToolGO.transform.SetParent(Editor.Root, false);
            m_proBuilderTool = m_proBuilderToolGO.AddComponent<ProBuilderTool>();
            m_proBuilderToolGO.AddComponent<MaterialPaletteManager>();
            m_proBuilderTool.ModeChanged += OnProBuilderToolModeChanged;
            m_proBuilderTool.SelectionChanged += OnProBuilderToolSelectionChanged;
            
            CreateToolbar();
            m_toolbar.gameObject.SetActive(m_useToolbar);

            Editor.Undo.Store();

            Editor.Selection.SelectionChanged += OnSelectionChanged;

            m_commandsList.ItemClick += OnItemClick;
            m_commandsList.ItemDataBinding += OnItemDataBinding;
            m_commandsList.ItemExpanding += OnItemExpanding;
            m_commandsList.ItemBeginDrag += OnItemBeginDrag;
            m_commandsList.ItemDrop += OnItemDrop;
            m_commandsList.ItemDragEnter += OnItemDragEnter;
            m_commandsList.ItemDragExit += OnItemDragExit;
            m_commandsList.ItemEndDrag += OnItemEndDrag;

            m_commandsList.CanEdit = false;
            m_commandsList.CanReorder = false;
            m_commandsList.CanReparent = false;
            m_commandsList.CanSelectAll = false;
            m_commandsList.CanUnselectAll = true;
            m_commandsList.CanRemove = false;
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            if (m_wm != null)
            {
                m_wm.WindowCreated -= OnWindowCreated;
                m_wm.WindowDestroyed -= OnWindowDestroyed;
                DestroyToolbar();
            }

            if (m_proBuilderToolGO != null)
            {
                Destroy(m_proBuilderToolGO);
            }

            Editor.Undo.Restore();

            if (Editor != null)
            {
                Editor.Selection.SelectionChanged -= OnSelectionChanged;
            }

            if (m_commandsList != null)
            {
                m_commandsList.ItemClick -= OnItemClick;
                m_commandsList.ItemDataBinding -= OnItemDataBinding;
                m_commandsList.ItemExpanding -= OnItemExpanding;
                m_commandsList.ItemBeginDrag -= OnItemBeginDrag;
                m_commandsList.ItemDrop -= OnItemDrop;
                m_commandsList.ItemDragEnter -= OnItemDragEnter;
                m_commandsList.ItemDragExit -= OnItemDragExit;
                m_commandsList.ItemEndDrag -= OnItemEndDrag;
            }

            if (m_proBuilderTool != null)
            {
                m_proBuilderTool.ModeChanged -= OnProBuilderToolModeChanged;
                m_proBuilderTool.SelectionChanged -= OnProBuilderToolSelectionChanged;
            }
        }

        protected virtual void Start()
        {
            UpdateFlags();
            m_commands = GetCommands().ToArray();
            m_commandsList.Items = m_commands;
        }

        private void OnProBuilderToolModeChanged(ProBuilderToolMode mode)
        {
            m_commands = GetCommands().ToArray();
            m_commandsList.Items = m_commands;
        }

        private List<ToolCmd> GetCommands()
        {
            switch (m_proBuilderTool.Mode)
            {
                case ProBuilderToolMode.Object:
                    return GetObjectCommands();
                case ProBuilderToolMode.Face:
                    return GetFaceCommands();
                case ProBuilderToolMode.Edge:
                    return GetEdgeCommands();
                case ProBuilderToolMode.Vertex:
                    return GetVertexCommands();
            }
            return new List<ToolCmd>();
        }

        private List<ToolCmd> GetObjectCommands()
        {
            List<ToolCmd> commands = GetCommonCommands();
            commands.Add(new ToolCmd("ProBuilderize", OnProBuilderize, CanProBuilderize));
            commands.Add(new ToolCmd("Subdivide", () => m_proBuilderTool.Subdivide(), () => m_isProBuilderMeshSelected));
            commands.Add(new ToolCmd("Center Pivot", OnCenterPivot, () => m_isProBuilderMeshSelected));

            return commands;
        }

        private List<ToolCmd> GetFaceCommands()
        {
            List<ToolCmd> commands = GetCommonCommands();
            commands.Add(new ToolCmd("Extrude Face", OnExtrudeFace, () => m_proBuilderTool.Mode == ProBuilderToolMode.Face && m_proBuilderTool.HasSelection));
            commands.Add(new ToolCmd("Delete Face", OnDeleteFace, () => m_proBuilderTool.Mode == ProBuilderToolMode.Face && m_proBuilderTool.HasSelection));
            commands.Add(new ToolCmd("Subdivide Faces", OnSubdivideFaces, () => m_proBuilderTool.Mode == ProBuilderToolMode.Face && m_proBuilderTool.HasSelection));
            commands.Add(new ToolCmd("Merge Faces", OnMergeFaces, () => m_proBuilderTool.Mode == ProBuilderToolMode.Face && m_proBuilderTool.HasSelection));
            return commands;
        }

        private List<ToolCmd> GetEdgeCommands()
        {
            List<ToolCmd> commands = GetCommonCommands();
            commands.Add(new ToolCmd("Find Holes", () => m_proBuilderTool.SelectHoles(), () => m_proBuilderTool.HasSelection || m_isProBuilderMeshSelected));
            commands.Add(new ToolCmd("Fill Holes", () => m_proBuilderTool.FillHoles(), () => m_proBuilderTool.HasSelection || m_isProBuilderMeshSelected));
            commands.Add(new ToolCmd("Subdivide Edges", OnSubdivideEdges, () => m_proBuilderTool.Mode == ProBuilderToolMode.Edge && m_proBuilderTool.HasSelection));
            return commands;
        }

        private List<ToolCmd> GetVertexCommands()
        {
            List<ToolCmd> commands = GetCommonCommands();
            commands.Add(new ToolCmd("Find Holes", () => m_proBuilderTool.SelectHoles(), () => m_proBuilderTool.HasSelection || m_isProBuilderMeshSelected));
            commands.Add(new ToolCmd("Fill Holes", () => m_proBuilderTool.FillHoles(), () => m_proBuilderTool.HasSelection || m_isProBuilderMeshSelected));
            return commands;
        }

        private List<ToolCmd> GetCommonCommands()
        {
            List<ToolCmd> commands = new List<ToolCmd>();
            ToolCmd newShapeCmd = new ToolCmd("New Shape", OnNewShape, true) { Arg = PBShapeType.Cube };
            newShapeCmd.Children = new List<ToolCmd>
            {
                new ToolCmd("Arch", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Arch },
                new ToolCmd("Cone", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Cone },
                new ToolCmd("Cube", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Cube },
                new ToolCmd("Curved Stair", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.CurvedStair },
                new ToolCmd("Cylinder", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Cylinder },
                new ToolCmd("Door", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Door },
                new ToolCmd("Pipe", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Pipe },
                new ToolCmd("Plane", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Plane },
                new ToolCmd("Prism", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Prism },
                new ToolCmd("Sphere", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Sphere },
                new ToolCmd("Sprite", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Sprite },
                new ToolCmd("Stair", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Stair },
                new ToolCmd("Torus", OnNewShape, true) { Parent = newShapeCmd, Arg = PBShapeType.Torus },
            };

            commands.Add(newShapeCmd);
            commands.Add(new ToolCmd("New Poly Shape", OnNewPolyShape, true));
            commands.Add(new ToolCmd("Edit Poly Shape", OnEditPolyShape, () => m_isPolyShapeSelected));
            commands.Add(new ToolCmd("Edit Materials", OnEditMaterials));
            commands.Add(new ToolCmd("Edit UV", OnEditUV));
            return commands;
        }

        private void UpdateFlags()
        {
            GameObject[] selected = Editor.Selection.gameObjects;
            if (selected != null && selected.Length > 0)
            {
                m_isProBuilderMeshSelected = selected.Where(go => go.GetComponent<PBMesh>() != null).Any();
                m_isNonProBuilderMeshSelected = selected.Where(go => go.GetComponent<PBMesh>() == null).Any();
                m_isPolyShapeSelected = selected.Where(go => go.GetComponent<PBPolyShape>() != null).Count() == 1;
            }
            else
            {
                m_isProBuilderMeshSelected = false;
                m_isNonProBuilderMeshSelected = false;
                m_isPolyShapeSelected = false;
            }
        }

        private void OnSelectionChanged(UnityEngine.Object[] unselectedObjects)
        {
            UpdateFlags();
            m_commandsList.DataBindVisible();
        }

        private void OnProBuilderToolSelectionChanged()
        {
            UpdateFlags();
            m_commandsList.DataBindVisible();
        }

        private void OnItemDataBinding(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            TextMeshProUGUI text = e.ItemPresenter.GetComponentInChildren<TextMeshProUGUI>();
            ToolCmd cmd = (ToolCmd)e.Item;
            text.text = cmd.Text;

            bool isValid = cmd.Validate();
            Color color = text.color;
            color.a = isValid ? 1 : 0.5f;
            text.color = color;
          
            e.CanDrag = cmd.CanDrag;
            e.HasChildren = cmd.HasChildren;
        }

        private void OnItemExpanding(object sender, VirtualizingItemExpandingArgs e)
        {
            ToolCmd cmd = (ToolCmd)e.Item;
            e.Children = cmd.Children;
        }

        private void OnItemClick(object sender, ItemArgs e)
        {
            ToolCmd cmd = (ToolCmd)e.Items[0];
            if(cmd.Validate())
            {
                cmd.Run();
            }
        }

        private void CreateNewShape(PBShapeType type, out GameObject go, out ExposeToEditor exposeToEditor)
        {
            go = PBShapeGenerator.CreateShape(type);
            go.AddComponent<PBMesh>();

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterials.Length == 1 && renderer.sharedMaterials[0] == PBBuiltinMaterials.DefaultMaterial)
            {
                IMaterialPaletteManager paletteManager = IOC.Resolve<IMaterialPaletteManager>();
                if (paletteManager.Palette.Materials.Count > 0)
                {
                    renderer.sharedMaterial = paletteManager.Palette.Materials[0];
                }
            }

            IRuntimeEditor rte = IOC.Resolve<IRuntimeEditor>();
            RuntimeWindow scene = rte.GetWindow(RuntimeWindowType.Scene);
            Vector3 position;
            Quaternion rotation;
            GetPositionAndRotation(scene, out position, out rotation);

            exposeToEditor = go.AddComponent<ExposeToEditor>();
            go.transform.position = position + rotation * Vector3.up * exposeToEditor.Bounds.extents.y;
            go.transform.rotation = rotation;
        }

        private object OnNewShape(object arg)
        {
            GameObject go;
            ExposeToEditor exposeToEditor;
            CreateNewShape((PBShapeType)arg, out go, out exposeToEditor);

            Editor.Undo.BeginRecord();
            Editor.Selection.activeGameObject = go;
            Editor.Undo.RegisterCreatedObjects(new[] { exposeToEditor });
            Editor.Undo.EndRecord();

            return go;
        }

        private object OnNewPolyShape(object arg)
        {
            GameObject go;
            ExposeToEditor exposeToEditor;
            CreateNewShape(PBShapeType.Cube, out go, out exposeToEditor);
            go.name = "Poly Shape";
          
            IRuntimeEditor rte = IOC.Resolve<IRuntimeEditor>();
            RuntimeWindow scene = rte.GetWindow(RuntimeWindowType.Scene);
            Vector3 position;
            Quaternion rotation;
            GetPositionAndRotation(scene, out position, out rotation);
            go.transform.position = position;
            go.transform.rotation = rotation;

            PBMesh pbMesh = go.GetComponent<PBMesh>();
            pbMesh.Clear();
            
            PBPolyShape polyShape = go.AddComponent<PBPolyShape>();
            polyShape.IsEditing = true;

            Editor.Undo.BeginRecord();
            Editor.Selection.activeGameObject = go;
            m_proBuilderTool.Mode = ProBuilderToolMode.PolyShape;
            Editor.Undo.RegisterCreatedObjects(new[] { exposeToEditor });
            Editor.Undo.EndRecord();

            return go;
        }

        private void GetPositionAndRotation(RuntimeWindow window, out Vector3 position, out Quaternion rotation, bool rotateToTerrain = false)
        {
            Ray ray = window != null ? 
                new Ray(window.Camera.transform.position, window.Camera.transform.forward) : 
                new Ray(Vector3.up * 100000, Vector3.down);

            RaycastHit[] hits = Physics.RaycastAll(ray);
            for (int i = 0; i < hits.Length; ++i)
            {
                RaycastHit hit = hits[i];
                if (hit.collider is TerrainCollider)
                {
                    position = hit.point;
                    if(rotateToTerrain)
                    {
                        rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    }
                    else
                    {
                        rotation = Quaternion.identity;
                    }
                    return;
                }
            }

            Vector3 up = Vector3.up;
            Vector3 pivot = Vector3.zero;
            if (window != null)
            {
                IScenePivot scenePivot = window.IOCContainer.Resolve<IScenePivot>();
                if (Mathf.Abs(Vector3.Dot(window.Camera.transform.up, Vector3.up)) > Mathf.Cos(Mathf.Deg2Rad))
                {
                    up = Vector3.Cross(window.Camera.transform.right, Vector3.up);
                }

                pivot = scenePivot.SecondaryPivot;
            }

            Plane dragPlane = new Plane(up, pivot);
            rotation = Quaternion.identity;
            if (!GetPointOnDragPlane(ray, dragPlane, out position))
            {
                position = window.Camera.transform.position + window.Camera.transform.forward * 10.0f;
            }
        }

        private bool GetPointOnDragPlane(Ray ray, Plane dragPlane, out Vector3 point)
        {
            float distance;
            if (dragPlane.Raycast(ray, out distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }
            point = Vector3.zero;
            return false;
        }

        private void OnEditPolyShape()
        {
            m_proBuilderTool.Mode = ProBuilderToolMode.PolyShape;
        }

        private void OnEditMaterials()
        {
            m_wm.CreateWindow("MaterialPalette", false, UIControls.DockPanels.RegionSplitType.Left, 0.2f);
        }

        private void OnEditUV()
        {
            m_wm.CreateWindow("UVEditor", false, UIControls.DockPanels.RegionSplitType.Left, 0.2f);
        }

        private void OnCenterPivot()
        {
            foreach(GameObject go in Editor.Selection.gameObjects)
            {
                PBMesh mesh = go.GetComponent<PBMesh>();
                if(mesh != null)
                {
                    mesh.CenterPivot();
                }
            }
        }

        private bool CanProBuilderize()
        {
            return m_isNonProBuilderMeshSelected;
        }

        private bool IsDescendant(Transform ancestor, Transform obj)
        {
            obj = obj.parent;
            while(obj != null)
            {
                if(obj == ancestor)
                {
                    return true;
                }

                obj = obj.parent;
            }

            return false;
        }

        private object OnProBuilderize(object arg)
        {
            GameObject[] gameObjects = Editor.Selection.gameObjects;
            if(gameObjects == null)
            {
                return null;
            }

            Transform[] transforms = gameObjects.Select(g => g.transform).ToArray();
            gameObjects = gameObjects.Where(g => !transforms.Any(t => IsDescendant(t, g.transform))).ToArray();

            for(int i = 0; i < gameObjects.Length; ++i)
            {
                Vector3 scale = gameObjects[i].transform.localScale;
                float minScale = Mathf.Min(scale.x, scale.y, scale.z);
                PBMesh.ProBuilderize(gameObjects[i], true, new Vector2(minScale, minScale));
            }
            return null;
        }

        private object OnExtrudeFace(object arg)
        {
            m_proBuilderTool.Extrude(0.01f);
            return null;
        }

        private void OnDeleteFace()
        {
            m_proBuilderTool.DeleteFaces();
        }

        private void OnSubdivideFaces()
        {
            m_proBuilderTool.SubdivideFaces();
        }

        private void OnMergeFaces()
        {
            m_proBuilderTool.MergeFaces();
        }

        private void OnSubdivideEdges()
        {
            m_proBuilderTool.SubdivideEdges();
        }

        private void OnItemBeginDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseBeginDrag(this, e.Items, e.PointerEventData);
        }

        private void OnItemDragEnter(object sender, ItemDropCancelArgs e)
        {
            Editor.DragDrop.SetCursor(KnownCursor.DropNotAllowed);
            e.Cancel = true;
        }

        private void OnItemDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseDrag(e.PointerEventData);
        }

        private void OnItemDragExit(object sender, EventArgs e)
        {
            Editor.DragDrop.SetCursor(KnownCursor.DropNotAllowed);
        }

        private void OnItemDrop(object sender, ItemDropArgs e)
        {
            Editor.DragDrop.RaiseDrop(e.PointerEventData);
        }

        private void OnItemEndDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseDrop(e.PointerEventData);
        }

        private void CreateToolbar()
        {
            Transform[] scenes = m_wm.GetWindows(RuntimeWindowType.Scene.ToString());
            for(int i = 0; i < scenes.Length; ++i)
            {
                RuntimeWindow window = scenes[i].GetComponent<RuntimeWindow>();
                CreateToolbar(scenes[i], window);
            }
        }

        private void DestroyToolbar()
        {
            Transform[] scenes = m_wm.GetWindows(RuntimeWindowType.Scene.ToString());
            for(int i = 0; i < scenes.Length; ++i)
            {
                RuntimeWindow window = scenes[i].GetComponent<RuntimeWindow>();
                DestroyToolbar(scenes[i], window);
            }
        }

        private void OnWindowCreated(Transform windowTransform)
        {
            RuntimeWindow window = windowTransform.GetComponent<RuntimeWindow>();
            CreateToolbar(windowTransform, window);
        }

        private void CreateToolbar(Transform windowTransform, RuntimeWindow window)
        {
            if(m_useSceneViewToolbar)
            {
                if (window != null && window.WindowType == RuntimeWindowType.Scene)
                {
                    if (m_sceneViewToolbarPrefab != null)
                    {
                        RectTransform rt = (RectTransform)Instantiate(m_sceneViewToolbarPrefab, windowTransform, false).transform;
                        rt.Stretch();
                    }
                }
            }
        }

        private void OnWindowDestroyed(Transform windowTransform)
        {
            if (m_useSceneViewToolbar)
            {
                RuntimeWindow window = windowTransform.GetComponent<RuntimeWindow>();
                DestroyToolbar(windowTransform, window);
            }
        }

        private void DestroyToolbar(Transform windowTransform, RuntimeWindow window)
        {
            if (window != null && window.WindowType == RuntimeWindowType.Scene)
            {
                if (m_sceneViewToolbarPrefab != null)
                {
                    ProBuilderToolbar toolbar = windowTransform.GetComponentInChildren<ProBuilderToolbar>();
                    if (toolbar != null)
                    {
                        Destroy(toolbar.gameObject);
                    }
                }
            }
        }
    }
}


