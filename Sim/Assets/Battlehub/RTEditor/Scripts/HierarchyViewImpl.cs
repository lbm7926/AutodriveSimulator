using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using Battlehub.UIControls;
using Battlehub.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Battlehub.RTEditor
{
    public class HierarchyViewImpl : MonoBehaviour
    {
        private bool m_lockSelection;
        private bool m_isSpawningPrefab;
        private ItemDropAction m_lastDropAction;
        protected ItemDropAction LastDropAction
        {
            get { return m_lastDropAction; }
        }

        private VirtualizingTreeView m_treeView;
        protected VirtualizingTreeView TreeView
        {
            get { return m_treeView; }
        }

        private List<GameObject> m_rootGameObjects;
        protected List<GameObject> RootGameObjects
        {
            get { return m_rootGameObjects; }
        }

 
        private IProject m_project;
        protected IProject Project
        {
            get { return m_project; }
        }

        private IRuntimeEditor m_editor;
        protected IRuntimeEditor Editor
        {
            get { return m_editor; }
        }

        private RuntimeWindow m_window;
        protected RuntimeWindow Window
        {
            get { return m_window; }
        }

        private HierarchyView m_hierarchyView;

        protected virtual void Awake()
        {
            m_hierarchyView = GetComponent<HierarchyView>();
            if (!m_hierarchyView.TreeViewPrefab)
            {
                Debug.LogError("Set TreeViewPrefab field");
                return;
            }

            m_project = IOC.Resolve<IProject>();
            m_editor = IOC.Resolve<IRuntimeEditor>();

            m_treeView = Instantiate(m_hierarchyView.TreeViewPrefab, transform).GetComponent<VirtualizingTreeView>();
            m_treeView.name = "HierarchyTreeView";
            m_treeView.CanSelectAll = false;
            m_treeView.SelectOnPointerUp = true;
            
            RectTransform rt = (RectTransform)m_treeView.transform;
            rt.Stretch();

            m_treeView.ItemDataBinding += OnItemDataBinding;
            m_treeView.SelectionChanged += OnSelectionChanged;
            m_treeView.ItemsRemoving += OnItemRemoving;
            m_treeView.ItemsRemoved += OnItemsRemoved;
            m_treeView.ItemExpanding += OnItemExpanding;
            m_treeView.ItemBeginDrag += OnItemBeginDrag;
            m_treeView.ItemBeginDrop += OnItemBeginDrop;
            m_treeView.ItemDrag += OnItemDrag;
            m_treeView.ItemDrop += OnItemDrop;
            m_treeView.ItemEndDrag += OnItemEndDrag;
            m_treeView.ItemDragEnter += OnItemDragEnter;
            m_treeView.ItemDragExit += OnItemDragExit;
            m_treeView.ItemDoubleClick += OnItemDoubleClicked;
            m_treeView.ItemBeginEdit += OnItemBeginEdit;
            m_treeView.ItemEndEdit += OnItemEndEdit;
            m_treeView.PointerEnter += OnTreeViewPointerEnter;
            m_treeView.PointerExit += OnTreeViewPointerExit;

            m_window = GetComponent<RuntimeWindow>();

            m_window.DragEnterEvent += OnDragEnter;
            m_window.DragLeaveEvent += OnDragLeave;
            m_window.DragEvent += OnDrag;
            m_window.DropEvent += OnDrop;
        }

        protected virtual void OnEnable()
        {
            if (m_editor != null)
            {
                m_editor.SceneLoading += OnSceneLoading;
                m_editor.SceneLoaded += OnSceneLoaded;
            }

            EnableHierarchy();
        }

        protected virtual void OnDisable()
        {
            if (m_editor != null)
            {
                m_editor.SceneLoading -= OnSceneLoading;
                m_editor.SceneLoaded -= OnSceneLoaded;
            }

            DisableHierarchy();
        }

        protected virtual void OnDestroy()
        {
            if(m_window != null)
            {
                m_window.DragEnterEvent -= OnDragEnter;
                m_window.DragLeaveEvent -= OnDragLeave;
                m_window.DragEvent -= OnDrag;
                m_window.DropEvent -= OnDrop;
            }

            if (m_treeView != null)
            {
                m_treeView.ItemDataBinding -= OnItemDataBinding;
                m_treeView.SelectionChanged -= OnSelectionChanged;
                m_treeView.ItemsRemoving -= OnItemRemoving;
                m_treeView.ItemsRemoved -= OnItemsRemoved;
                m_treeView.ItemExpanding -= OnItemExpanding;
                m_treeView.ItemBeginDrag -= OnItemBeginDrag;
                m_treeView.ItemBeginDrop -= OnItemBeginDrop;
                m_treeView.ItemDrag -= OnItemDrag;
                m_treeView.ItemDrop -= OnItemDrop;
                m_treeView.ItemEndDrag -= OnItemEndDrag;
                m_treeView.ItemDragEnter -= OnItemDragEnter;
                m_treeView.ItemDragExit -= OnItemDragExit;
                m_treeView.ItemDoubleClick -= OnItemDoubleClicked;
                m_treeView.ItemBeginEdit -= OnItemBeginEdit;
                m_treeView.ItemEndEdit -= OnItemEndEdit;
                m_treeView.PointerEnter -= OnTreeViewPointerEnter;
                m_treeView.PointerExit -= OnTreeViewPointerExit;
            }
        }

        protected virtual void LateUpdate()
        {
            m_rootGameObjects = null;
        }

        public virtual void SelectAll()
        {
            m_treeView.SelectedItems = m_treeView.Items;
        }

        protected virtual void EnableHierarchy()
        {
            BindGameObjects();
            m_lockSelection = true;
            m_treeView.SelectedItems = Editor.Selection.gameObjects != null ? Editor.Selection.gameObjects.Select(g => g.GetComponent<ExposeToEditor>()).Where(e => e != null).ToArray() : null;
            m_lockSelection = false;

            Editor.Selection.SelectionChanged += OnRuntimeSelectionChanged;

            Editor.Object.Awaked += OnObjectAwaked;
            Editor.Object.Started += OnObjectStarted;
            Editor.Object.Enabled += OnObjectEnabled;
            Editor.Object.Disabled += OnObjectDisabled;
            Editor.Object.Destroying += OnObjectDestroying;
            Editor.Object.Destroyed += OnObjectDestroyed;
            Editor.Object.MarkAsDestroyedChanging += OnObjectMarkAsDestoryedChanging;
            Editor.Object.MarkAsDestroyedChanged += OnObjectMarkAsDestroyedChanged;
            Editor.Object.ParentChanged += OnParentChanged;
            Editor.Object.NameChanged += OnNameChanged;

            Editor.PlaymodeStateChanged += OnPlaymodeStateChanged;
        }

        protected virtual void DisableHierarchy()
        {
            if (Editor != null)
            {
                if (Editor.Selection != null)
                {
                    Editor.Selection.SelectionChanged -= OnRuntimeSelectionChanged;
                }

                if (Editor.Object != null)
                {
                    Editor.Object.Awaked -= OnObjectAwaked;
                    Editor.Object.Started -= OnObjectStarted;
                    Editor.Object.Enabled -= OnObjectEnabled;
                    Editor.Object.Disabled -= OnObjectDisabled;
                    Editor.Object.Destroying -= OnObjectDestroying;
                    Editor.Object.Destroyed -= OnObjectDestroyed;
                    Editor.Object.MarkAsDestroyedChanging -= OnObjectMarkAsDestoryedChanging;
                    Editor.Object.MarkAsDestroyedChanged -= OnObjectMarkAsDestroyedChanged;
                    Editor.Object.ParentChanged -= OnParentChanged;
                    Editor.Object.NameChanged -= OnNameChanged;
                }

                Editor.PlaymodeStateChanged -= OnPlaymodeStateChanged;
            }
        }

        protected virtual void OnSceneLoading()
        {
            DisableHierarchy();
        }

        protected virtual void OnSceneLoaded()
        {
            EnableHierarchy();
        }

        protected virtual void BindGameObjects(bool forceUseCache = false, bool updateSelection = true)
        {
            bool useCache = Editor.IsPlaying;
            IEnumerable<ExposeToEditor> objects = Editor.Object.Get(true, useCache || forceUseCache);

            if (objects.Any())
            {
                Transform commonParent = objects.First().transform.parent;
                foreach (ExposeToEditor obj in objects)
                {
                    if (obj.transform.parent != commonParent)
                    {
                        Debug.LogWarning("ExposeToEditor objects have different parents, hierarchy may not work correctly.");
                        break;
                    }
                }
            }

            m_treeView.SetItems(objects.OrderBy(g => g.transform.GetSiblingIndex()), updateSelection);
        }

        protected virtual void OnPlaymodeStateChanged()
        {
            BindGameObjects();
        }

        protected virtual void OnItemExpanding(object sender, VirtualizingItemExpandingArgs e)
        {
            ExposeToEditor exposeToEditor = (ExposeToEditor)e.Item;

            if (exposeToEditor.HasChildren())
            {
                e.Children = exposeToEditor.GetChildren().Where(obj => !obj.MarkAsDestroyed);

                //This line is required to syncronize selection, runtime selection and treeview selection
                OnTreeViewSelectionChanged(m_treeView.SelectedItems, m_treeView.SelectedItems);
            }
            else
            {
                e.Children = new ExposeToEditor[0];
            }
        }

        protected virtual void OnRuntimeSelectionChanged(Object[] unselected)
        {
            if (m_lockSelection)
            {
                return;
            }
            m_lockSelection = true;

            if (Editor.Selection.gameObjects == null)
            {
                m_treeView.SelectedItems = new ExposeToEditor[0];
            }
            else
            {
                m_treeView.SelectedItems = Editor.Selection.gameObjects.Select(g => g.GetComponent<ExposeToEditor>()).Where(e => e != null && !e.gameObject.IsPrefab() && (e.gameObject.hideFlags & HideFlags.HideInHierarchy) == 0).ToArray();
            }

            m_lockSelection = false;
        }

        protected virtual void OnSelectionChanged(object sender, SelectionChangedArgs e)
        {
            OnTreeViewSelectionChanged(e.OldItems, e.NewItems);
        }

        protected virtual void OnTreeViewSelectionChanged(IEnumerable oldItems, IEnumerable newItems)
        {
            if (m_lockSelection)
            {
                return;
            }

            m_lockSelection = true;

            if (newItems == null)
            {
                newItems = new ExposeToEditor[0];
            }
            ExposeToEditor[] selectableObjects = newItems.OfType<ExposeToEditor>().ToArray();
            Editor.Selection.objects = selectableObjects.Select(o => o.gameObject).ToArray();

            //sync with RunitimeSelectiom.objects because of OnBeforeSelectionChanged event
            m_treeView.SelectedItems = selectableObjects;

            m_lockSelection = false;
        }

        protected virtual void OnItemRemoving(object sender, ItemsCancelArgs e)
        {
            if (e.Items == null)
            {
                return;
            }

            if (Editor.ActiveWindow == this)
            {
                IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
                editor.Delete(e.Items.OfType<ExposeToEditor>().Select(exposed => exposed.gameObject).ToArray());
            }

            for (int i = e.Items.Count - 1; i >= 0; i--)
            {
                ExposeToEditor item = (ExposeToEditor)e.Items[i];
                if (!item.CanDelete)
                {
                    e.Items.RemoveAt(i);
                }
            }
        }

        protected virtual void OnItemsRemoved(object sender, ItemsRemovedArgs e)
        {

        }

        protected virtual void OnItemDataBinding(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            ExposeToEditor dataItem = (ExposeToEditor)e.Item;
            if (dataItem != null)
            {
                TextMeshProUGUI text = e.ItemPresenter.GetComponentInChildren<TextMeshProUGUI>(true);
                text.text = dataItem.name;
                if (dataItem.gameObject.activeInHierarchy)
                {
                    text.color = m_hierarchyView.EnabledItemColor;
                }
                else
                {
                    text.color = m_hierarchyView.DisabledItemColor;
                }

                e.CanEdit = dataItem.CanRename;
                e.HasChildren = dataItem.HasChildren();
            }
        }

        protected virtual void OnItemDoubleClicked(object sender, ItemArgs e)
        {
            ExposeToEditor exposeToEditor = (ExposeToEditor)e.Items[0];
            Editor.Selection.activeObject = exposeToEditor.gameObject;
        }    
        

        protected virtual void OnItemBeginEdit(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            ExposeToEditor dataItem = (ExposeToEditor)e.Item;
            if (dataItem != null)
            {
                TMP_InputField inputField = e.EditorPresenter.GetComponentInChildren<TMP_InputField>(true);
                inputField.text = dataItem.name;
                inputField.ActivateInputField();
                inputField.Select();
                LayoutElement layout = inputField.GetComponent<LayoutElement>();

                TextMeshProUGUI text = e.ItemPresenter.GetComponentInChildren<TextMeshProUGUI>(true);
                text.text = dataItem.name;

                RectTransform rt = text.GetComponent<RectTransform>();
                layout.preferredWidth = rt.rect.width;
            }
        }

        protected virtual void OnItemEndEdit(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            ExposeToEditor dataItem = (ExposeToEditor)e.Item;
            if (dataItem != null)
            {

                TMP_InputField inputField = e.EditorPresenter.GetComponentInChildren<TMP_InputField>(true);
                if (!string.IsNullOrEmpty(inputField.text))
                {
                    dataItem.name = inputField.text;
                    TextMeshProUGUI text = e.ItemPresenter.GetComponentInChildren<TextMeshProUGUI>(true);
                    text.text = dataItem.name;
                }
                else
                {
                    inputField.text = dataItem.name;
                }
            }

            //Following code is required to unfocus inputfield if focused and release InputManager
            if (EventSystem.current != null && !EventSystem.current.alreadySelecting)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        protected virtual void OnItemDragEnter(object sender, ItemDropCancelArgs e)
        {
            Editor.DragDrop.SetCursor(KnownCursor.DropAllowed);
        }

        protected virtual void OnItemDragExit(object sender, System.EventArgs e)
        {

        }

        protected virtual void OnTreeViewPointerEnter(object sender, PointerEventArgs e)
        {
            if (m_treeView.DragItems != null)
            {
                Editor.DragDrop.SetCursor(KnownCursor.DropAllowed);
            }
        }

        protected virtual void OnTreeViewPointerExit(object sender, PointerEventArgs e)
        {
            if (m_treeView.DragItems != null)
            {
                Editor.DragDrop.SetCursor(KnownCursor.DropNotAllowed);
            }
        }

        protected virtual void OnItemBeginDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseBeginDrag(m_window, e.Items, e.PointerEventData);
        }

        protected virtual void OnItemDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseDrag(e.PointerEventData);
        }

        protected virtual void OnItemBeginDrop(object sender, ItemDropCancelArgs e)
        {
            if (e.IsExternal)
            {

            }
            else
            {
                Editor.Undo.BeginRecord();
                Editor.Undo.CreateRecord(null, null, false,
                    record => RefreshTree(record, true),
                    record => RefreshTree(record, false));

                IEnumerable<ExposeToEditor> dragItems = e.DragItems.OfType<ExposeToEditor>();

                if (e.Action == ItemDropAction.SetLastChild || dragItems.Any(d => (object)d.GetParent() != e.DropTarget))
                {
                    foreach (ExposeToEditor exposed in dragItems.Reverse())
                    {
                        Transform dragT = exposed.transform;
                        int siblingIndex = dragT.GetSiblingIndex();
                        Editor.Undo.BeginRecordTransform(dragT, dragT.parent, siblingIndex);
                    }
                }
                else
                {
                    Transform dropT = ((ExposeToEditor)e.DropTarget).transform;
                    int dropTIndex = dropT.GetSiblingIndex();

                    foreach (ExposeToEditor exposed in dragItems
                        .Where(o => o.transform.GetSiblingIndex() > dropTIndex)
                        .OrderBy(o => o.transform.GetSiblingIndex())
                        .Union(dragItems
                            .Where(o => o.transform.GetSiblingIndex() < dropTIndex)
                            .OrderByDescending(o => o.transform.GetSiblingIndex())))
                    {
                        Transform dragT = exposed.transform;
                        int siblingIndex = dragT.GetSiblingIndex();
                        Editor.Undo.BeginRecordTransform(dragT, dragT.parent, siblingIndex);
                    }
                }

                Editor.Undo.EndRecord();
            }
        }

        protected virtual void OnItemDrop(object sender, ItemDropArgs e)
        {
            if (e.IsExternal)
            {
                
            }
            else
            {
                Transform dropT = ((ExposeToEditor)e.DropTarget).transform;
                if (e.Action == ItemDropAction.SetLastChild)
                {
                    Editor.Undo.BeginRecord();
                    for (int i = 0; i < e.DragItems.Length; ++i)
                    {
                        ExposeToEditor exposed = (ExposeToEditor)e.DragItems[i];
                        Transform dragT = exposed.transform;
                        dragT.SetParent(dropT, true);
                        dragT.SetAsLastSibling();

                        Editor.Undo.EndRecordTransform(dragT, dropT, dragT.GetSiblingIndex());
                    }
                    Editor.Undo.CreateRecord(null, null, true,
                       record => RefreshTree(record, true),
                       record => RefreshTree(record, false));
                    Editor.Undo.EndRecord();
                }
                else if (e.Action == ItemDropAction.SetNextSibling)
                {
                    Editor.Undo.BeginRecord();

                    for (int i = e.DragItems.Length - 1; i >= 0; --i)
                    {
                        ExposeToEditor exposed = (ExposeToEditor)e.DragItems[i];
                        Transform dragT = exposed.transform;

                        int dropTIndex = dropT.GetSiblingIndex();
                        if (dragT.parent != dropT.parent)
                        {
                            dragT.SetParent(dropT.parent, true);
                            dragT.SetSiblingIndex(dropTIndex + 1);
                        }
                        else
                        {
                            int dragTIndex = dragT.GetSiblingIndex();
                            if (dropTIndex < dragTIndex)
                            {
                                dragT.SetSiblingIndex(dropTIndex + 1);
                            }
                            else
                            {
                                dragT.SetSiblingIndex(dropTIndex);
                            }
                        }
                        Editor.Undo.EndRecordTransform(dragT, dragT.parent, dragT.GetSiblingIndex());
                    }
                    Editor.Undo.CreateRecord(null, null, true,
                        record => RefreshTree(record, true),
                        record => RefreshTree(record, false));
                    Editor.Undo.EndRecord();

                }
                else if (e.Action == ItemDropAction.SetPrevSibling)
                {
                    Editor.Undo.BeginRecord();
                    for (int i = 0; i < e.DragItems.Length; ++i)
                    {
                        ExposeToEditor exposed = (ExposeToEditor)e.DragItems[i];
                        Transform dragT = exposed.transform;
                        if (dragT.parent != dropT.parent)
                        {
                            dragT.SetParent(dropT.parent, true);
                        }

                        int dropTIndex = dropT.GetSiblingIndex();
                        int dragTIndex = dragT.GetSiblingIndex();
                        if (dropTIndex > dragTIndex)
                        {
                            dragT.SetSiblingIndex(dropTIndex - 1);
                        }
                        else
                        {
                            dragT.SetSiblingIndex(dropTIndex);
                        }

                        Editor.Undo.EndRecordTransform(dragT, dragT.parent, dragT.GetSiblingIndex());
                    }
                    Editor.Undo.CreateRecord(null, null, true,
                        record => RefreshTree(record, true),
                        record => RefreshTree(record, false));
                    Editor.Undo.EndRecord();
                }

                Editor.DragDrop.RaiseDrop(e.PointerEventData);
            }
        }

        protected virtual void OnItemEndDrag(object sender, ItemArgs e)
        {
            if (Editor.DragDrop.InProgress)
            {
                Editor.DragDrop.RaiseDrop(e.PointerEventData);
            }
        }

        protected virtual bool RefreshTree(Record record, bool isRedo)
        {
            bool applyOnRedo = (bool)record.OldState;
            if (applyOnRedo != isRedo)
            {
                return false;
            }

            BindGameObjects(true, false);


            if (m_treeView.SelectedItems != null)
            {
                foreach (ExposeToEditor obj in m_treeView.SelectedItems.OfType<ExposeToEditor>().OrderBy(o => o.transform.GetSiblingIndex()))
                {
                    Expand(obj);
                }
            }

            return false;
        }

        protected virtual void Expand(ExposeToEditor item)
        {
            if (item == null)
            {
                return;
            }
            ExposeToEditor parent = item.GetParent();
            if (parent != null && !m_treeView.IsExpanded(parent))
            {
                Expand(parent);
            }

            if (item.HasChildren())
            {
                m_treeView.Expand(item);
            }
        }

        protected virtual void OnObjectAwaked(ExposeToEditor obj)
        {
            if (!m_isSpawningPrefab)
            {
                if (!obj.MarkAsDestroyed && m_treeView.IndexOf(obj) == -1)
                {
                    ExposeToEditor parent = obj.GetParent();
                    m_treeView.AddChild(parent, obj);
                }
            }
        }

        protected virtual void OnObjectStarted(ExposeToEditor obj)
        {

        }

        protected virtual void OnObjectEnabled(ExposeToEditor obj)
        {
            VirtualizingTreeViewItem tvItem = m_treeView.GetTreeViewItem(obj);
            if (tvItem == null)
            {
                return;
            }
            TextMeshProUGUI text = tvItem.GetComponentInChildren<TextMeshProUGUI>();
            text.color = m_hierarchyView.EnabledItemColor;
        }

        protected virtual void OnObjectDisabled(ExposeToEditor obj)
        {
            VirtualizingTreeViewItem tvItem = m_treeView.GetTreeViewItem(obj);
            if (tvItem == null)
            {
                return;
            }
            TextMeshProUGUI text = tvItem.GetComponentInChildren<TextMeshProUGUI>();
            text.color = m_hierarchyView.DisabledItemColor;
        }

        protected virtual void OnObjectDestroying(ExposeToEditor o)
        {
            ExposeToEditor parent = o.GetParent();
            try
            {
                m_treeView.ItemsRemoved -= OnItemsRemoved;
                m_treeView.RemoveChild(parent, o);
            }
            finally
            {
                m_treeView.ItemsRemoved += OnItemsRemoved;
            }
        }

        protected virtual void OnObjectDestroyed(ExposeToEditor o)
        {

        }

        protected virtual void OnObjectMarkAsDestoryedChanging(ExposeToEditor o)
        {
            if (o.MarkAsDestroyed)
            {

            }
            else
            {

            }
        }

        protected virtual void OnObjectMarkAsDestroyedChanged(ExposeToEditor o)
        {
            if (o.MarkAsDestroyed)
            {
                m_treeView.RemoveChild(o.GetParent(), o);
            }
            else
            {
                ExposeToEditor parent = o.GetParent();
                m_treeView.AddChild(parent, o);
                SetSiblingIndex(o);
            }
        }

        protected virtual void SetSiblingIndex(ExposeToEditor o)
        {
            if (o.transform.parent == null && m_rootGameObjects == null)
            {
                m_rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects().OrderBy(g => g.transform.GetSiblingIndex()).ToList();
            }

            ExposeToEditor nextSibling = o.NextSibling(m_rootGameObjects);
            if (nextSibling != null)
            {
                m_treeView.SetPrevSibling(nextSibling, o);
            }
        }

        protected virtual void OnParentChanged(ExposeToEditor obj, ExposeToEditor oldParent, ExposeToEditor newParent)
        {
            if (Editor.IsPlaymodeStateChanging)
            {
                return;
            }

            bool isNewParentExpanded = true;
            bool isOldParentExpanded = true;
            bool isLastChild = false;
            if (newParent != null)
            {
                isNewParentExpanded = m_treeView.IsExpanded(newParent);
            }

            if (oldParent != null)
            {
                TreeViewItemContainerData itemContainerData = (TreeViewItemContainerData)m_treeView.GetItemContainerData(oldParent);

                isLastChild = !oldParent.HasChildren(); //!itemContainerData.HasChildren(m_treeView);

                isOldParentExpanded = m_treeView.IsExpanded(oldParent);
            }

            if (isNewParentExpanded)
            {
                m_treeView.ChangeParent(newParent, obj);

                if (!isOldParentExpanded)
                {
                    if (isLastChild)
                    {
                        VirtualizingTreeViewItem oldParentContainer = m_treeView.GetTreeViewItem(oldParent);
                        if (oldParentContainer)
                        {
                            oldParentContainer.CanExpand = false;
                        }
                    }
                }
            }
            else
            {
                if (newParent != null)
                {
                    VirtualizingTreeViewItem newParentTreeViewItem = m_treeView.GetTreeViewItem(newParent);
                    if (newParentTreeViewItem != null)
                    {
                        newParentTreeViewItem.CanExpand = true;
                    }
                }

                m_treeView.RemoveChild(oldParent, obj);
            }

        }

        protected virtual void OnNameChanged(ExposeToEditor obj)
        {
            VirtualizingTreeViewItem tvItem = m_treeView.GetTreeViewItem(obj);
            if (tvItem == null)
            {
                return;
            }

            TextMeshProUGUI text = tvItem.GetComponentInChildren<TextMeshProUGUI>();
            text.text = obj.name;
        }

        protected virtual bool CanDrop(object[] dragObjects)
        {
            IEnumerable<AssetItem> assetItems = dragObjects.OfType<AssetItem>();
            return assetItems.Count() > 0 && assetItems.Any(assetItem => m_project.ToType(assetItem) == typeof(GameObject));
        }

        protected virtual void OnDragEnter(PointerEventData pointerEventData)
        {
            m_treeView.ExternalBeginDrag(pointerEventData.position);
        }

        protected virtual void OnDragLeave(PointerEventData pointerEventData)
        {
            m_treeView.ExternalItemDrop();
            Editor.DragDrop.SetCursor(KnownCursor.DropNotAllowed);
        }

        protected virtual void OnDrag(PointerEventData pointerEventData)
        {
            object[] dragObjects = Editor.DragDrop.DragObjects;
            m_treeView.ExternalItemDrag(pointerEventData.position);
            m_lastDropAction = m_treeView.DropAction;
            if (CanDrop(dragObjects))
            {
                Editor.DragDrop.SetCursor(KnownCursor.DropAllowed);
            }
            else
            {
                Editor.DragDrop.SetCursor(KnownCursor.DropNotAllowed);
                m_treeView.ClearTarget();
            }
        }

        protected virtual void OnDrop(PointerEventData pointerEventData)
        {
            object[] dragObjects = Editor.DragDrop.DragObjects;
            if (CanDrop(dragObjects))
            {
                ExposeToEditor dropTarget = (ExposeToEditor)m_treeView.DropTarget;
                VirtualizingTreeViewItem treeViewItem = null;
                if (dropTarget != null)
                {
                    treeViewItem = m_treeView.GetTreeViewItem(m_treeView.DropTarget);
                }

                AssetItem[] loadAssetItems = dragObjects.Where(o => o is AssetItem && m_project.ToType((AssetItem)o) == typeof(GameObject)).Select(o => (AssetItem)o).ToArray();
                if (loadAssetItems.Length > 0)
                {
                    m_isSpawningPrefab = true;
                    Editor.IsBusy = true;
                    m_project.Load(loadAssetItems, (error, objects) =>
                    {
                        Editor.IsBusy = false;
                        if (error.HasError)
                        {
                            IWindowManager wm = IOC.Resolve<IWindowManager>();
                            wm.MessageBox("Unable to load asset items.", error.ErrorText);
                            return;
                        }

                        OnAssetItemsLoaded(objects, dropTarget, treeViewItem);
                    });
                }
                else
                {
                    m_treeView.ExternalItemDrop();
                }
            }
            else
            {
                m_treeView.ExternalItemDrop();
            }
        }

        private void OnAssetItemsLoaded(Object[] objects, ExposeToEditor dropTarget, VirtualizingTreeViewItem treeViewItem)
        {
            GameObject[] createdObjects = new GameObject[objects.Length];
            for (int i = 0; i < objects.Length; ++i)
            {
                GameObject prefab = (GameObject)objects[i];
                bool wasPrefabEnabled = prefab.activeSelf;
                prefab.SetActive(false);
                GameObject prefabInstance = InstantiatePrefab(prefab);
                prefab.SetActive(wasPrefabEnabled);

                ExposeToEditor exposeToEditor = prefabInstance.GetComponent<ExposeToEditor>();
                if (exposeToEditor == null)
                {
                    exposeToEditor = prefabInstance.AddComponent<ExposeToEditor>();
                }

                exposeToEditor.SetName(prefab.name);

                if (dropTarget == null)
                {
                    exposeToEditor.transform.SetParent(null);
                    m_treeView.Add(exposeToEditor);
                }
                else
                {
                    if (m_treeView.DropAction == ItemDropAction.SetLastChild)
                    {
                        exposeToEditor.transform.SetParent(dropTarget.transform);
                        m_treeView.AddChild(dropTarget, exposeToEditor);
                        treeViewItem.CanExpand = true;
                        treeViewItem.IsExpanded = true;
                    }
                    if (m_lastDropAction != ItemDropAction.None)
                    {
                        int index;
                        int siblingIndex;
                        if (m_lastDropAction == ItemDropAction.SetNextSibling)
                        {
                            index = m_treeView.IndexOf(dropTarget) + 1;
                            siblingIndex = dropTarget.transform.GetSiblingIndex() + 1;
                        }
                        else
                        {
                            index = m_treeView.IndexOf(dropTarget);
                            siblingIndex = dropTarget.transform.GetSiblingIndex();
                        }

                        exposeToEditor.transform.SetParent(dropTarget.transform.parent != null ? dropTarget.transform.parent : null);
                        exposeToEditor.transform.SetSiblingIndex(siblingIndex);

                        TreeViewItemContainerData newTreeViewItemData = (TreeViewItemContainerData)m_treeView.Insert(index, exposeToEditor);
                        VirtualizingTreeViewItem newTreeViewItem = m_treeView.GetTreeViewItem(exposeToEditor);
                        if (newTreeViewItem != null)
                        {
                            newTreeViewItem.Parent = treeViewItem.Parent;
                        }
                        else
                        {
                            newTreeViewItemData.Parent = treeViewItem.Parent;
                        }
                    }
                }

                OnActivatePrefabInstance(prefabInstance);
                createdObjects[i] = prefabInstance;
            }

            if (createdObjects.Length > 0)
            {
                IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
                editor.RegisterCreatedObjects(createdObjects);
            }

            m_treeView.ExternalItemDrop();
            m_isSpawningPrefab = false;
        }

        protected virtual void OnActivatePrefabInstance(GameObject prefabInstance)
        {
            prefabInstance.SetActive(true);
        }

        protected virtual GameObject InstantiatePrefab(GameObject prefab)
        {
            return Instantiate(prefab, Vector3.zero, Quaternion.identity);
        }
    }

}


