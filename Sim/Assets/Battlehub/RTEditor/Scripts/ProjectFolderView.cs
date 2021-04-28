using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using Battlehub.UIControls;
using Battlehub.UIControls.MenuControl;
using Battlehub.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using UnityObject = UnityEngine.Object;

namespace Battlehub.RTEditor
{
    public class ProjectFolderView : RuntimeWindow
    {
        public event EventHandler<ProjectTreeEventArgs> ItemDeleted;
        public event EventHandler<ProjectTreeEventArgs> ItemDoubleClick;
        public event EventHandler<ProjectTreeRenamedEventArgs> ItemRenamed;
        public event EventHandler<ProjectTreeEventArgs> SelectionChanged;
        
        private IProject m_project;
        private IWindowManager m_windowManager;

        private Dictionary<long, ProjectItem> m_idToItem = new Dictionary<long, ProjectItem>();
        private List<ProjectItem> m_items;
        private ProjectItem[] m_folders;
        public void SetItems(ProjectItem[] folders, ProjectItem[] items, bool reload)
        {
            if(folders == null || items == null)
            {
                m_folders = null;
                m_items = null;
                m_idToItem = new Dictionary<long, ProjectItem>();
                m_listBox.Items = null;
            }
            else
            {
                m_folders = folders;
                m_items = new List<ProjectItem>(items);
                m_idToItem = m_items.Where(item => !item.IsFolder).ToDictionary(item => item.ItemID);
                if (m_items != null)
                {
                    m_items = m_items.Where(item => item.IsFolder).OrderBy(item => item.Name).Union(m_items.Where(item => !item.IsFolder).OrderBy(item => item.Name)).ToList();
                }
                DataBind(reload);
                EditorSelectionChanged(null);
            }
        }

        public void InsertItems(ProjectItem[] items, bool selectAndScrollIntoView)
        {
            if(m_folders == null)
            {
                return;
            }

            items = items.Where(item => m_folders.Contains(item.Parent)).ToArray();
            if(items.Length == 0)
            {
                return;
            }

            m_items = m_items.Union(items).ToList();
            List<ProjectItem> sorted = m_items.Where(item => item.IsFolder).OrderBy(item => item.Name).Union(m_items.Where(item => !item.IsFolder).OrderBy(item => item.Name)).ToList();
            ProjectItem selectItem = null;
            for(int i = 0; i < sorted.Count; ++i)
            {
                if(items.Contains(sorted[i]))
                {
                    m_listBox.Insert(i, sorted[i]);
                    selectItem = sorted[i];
                }
                else
                {
                    VirtualizingItemContainer itemContainer = m_listBox.GetItemContainer(sorted[i]);
                    if(itemContainer != null)
                    {
                        m_listBox.DataBindItem(sorted[i], itemContainer);
                    }
                }

                if(!m_idToItem.ContainsKey(sorted[i].ItemID))
                {
                    m_idToItem.Add(sorted[i].ItemID, sorted[i]);
                }
            }
            m_items = sorted;

            if(selectItem != null)
            {
                if(selectAndScrollIntoView)
                {
                    m_listBox.SelectedItem = selectItem;
                    m_listBox.ScrollIntoView(selectItem);
                }
            }
        }


        [SerializeField]
        private GameObject ListBoxPrefab = null;
        private VirtualizingTreeView m_listBox;
        private bool m_raiseSelectionChange = true;
        private bool m_raiseItemDeletedEvent = true;

        public ProjectItem[] SelectedItems
        {
            get { return m_listBox.SelectedItems != null ? m_listBox.SelectedItems.OfType<ProjectItem>().ToArray() : null; }
        }

        private bool m_handleEditorSelectionChange = true;
        public bool HandleEditorSelectionChange
        {
            get { return m_handleEditorSelectionChange; }
            set { m_handleEditorSelectionChange = value; }
        }

        private void DataBind(bool clearItems)
        {
            if (m_items == null)
            {
                m_listBox.SelectedItems = null;
                m_listBox.Items = null;
            }
            else
            {
                if (clearItems)
                {
                    if (m_listBox == null)
                    {
                        Debug.LogError("ListBox is null");
                    }
                    m_listBox.Items = null;
                }

                m_listBox.SelectedItems = null;

                List<ProjectItem> itemsList = m_items.ToList();
                m_listBox.Items = itemsList;
            }
        }

        protected override void AwakeOverride()
        {
            WindowType = RuntimeWindowType.ProjectFolder;
            base.AwakeOverride();
            
            if (!ListBoxPrefab)
            {
                Debug.LogError("Set ListBoxPrefab field");
                return;
            }

            m_project = IOC.Resolve<IProject>();
            m_windowManager = IOC.Resolve<IWindowManager>();

            m_listBox = GetComponentInChildren<VirtualizingTreeView>();
            if (m_listBox == null)
            {
                m_listBox = Instantiate(ListBoxPrefab, transform).GetComponent<VirtualizingTreeView>();
                m_listBox.name = "AssetsListBox";
            }

            m_listBox.CanDrag = true;
            m_listBox.CanReorder = false;
            m_listBox.SelectOnPointerUp = true;
            m_listBox.CanRemove = false;
            m_listBox.CanSelectAll = false;

            m_listBox.ItemDataBinding += OnItemDataBinding;
            m_listBox.ItemBeginDrag += OnItemBeginDrag;
            m_listBox.ItemDragEnter += OnItemDragEnter;
            m_listBox.ItemDrag += OnItemDrag;
            m_listBox.ItemDragExit += OnItemDragExit;
            m_listBox.ItemDrop += OnItemDrop;
            m_listBox.ItemEndDrag += OnItemEndDrag;
            m_listBox.ItemsRemoving += OnItemRemoving;
            m_listBox.ItemsRemoved += OnItemRemoved;
            m_listBox.ItemDoubleClick += OnItemDoubleClick;
            m_listBox.ItemBeginEdit += OnItemBeginEdit;
            m_listBox.ItemEndEdit += OnItemEndEdit;
            m_listBox.SelectionChanged += OnSelectionChanged;
            m_listBox.ItemClick += OnItemClick;
            m_listBox.Click += OnListBoxClick;

            Editor.Selection.SelectionChanged += EditorSelectionChanged;

            if (!GetComponent<ProjectFolderViewInput>())
            {
                gameObject.AddComponent<ProjectFolderViewInput>();
            }
        }

        private void Start()
        {
                
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            if (m_listBox != null)
            {
                m_listBox.ItemDataBinding -= OnItemDataBinding;
                m_listBox.ItemBeginDrag -= OnItemBeginDrag;
                m_listBox.ItemDragEnter -= OnItemDragEnter;
                m_listBox.ItemDrag -= OnItemDrag;
                m_listBox.ItemDragExit -= OnItemDragExit;
                m_listBox.ItemDrop -= OnItemDrop;
                m_listBox.ItemEndDrag -= OnItemEndDrag;
                m_listBox.ItemsRemoving -= OnItemRemoving;
                m_listBox.ItemsRemoved -= OnItemRemoved;
                m_listBox.ItemDoubleClick -= OnItemDoubleClick;
                m_listBox.ItemBeginEdit -= OnItemBeginEdit;
                m_listBox.ItemEndEdit -= OnItemEndEdit;
                m_listBox.ItemClick -= OnItemClick;
                m_listBox.Click -= OnListBoxClick;

                m_listBox.SelectionChanged -= OnSelectionChanged;
            }

            if(Editor != null)
            {
                Editor.Selection.SelectionChanged -= EditorSelectionChanged;
            }
        }

        private void OnItemBeginDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseBeginDrag(this, e.Items, e.PointerEventData);
        }

        private bool FolderContainsItemWithSameName(object dropTarget, object[] dragItems)
        {
            ProjectItem folder = (ProjectItem)dropTarget;
            if(folder.Children == null || folder.Children.Count == 0)
            {
                return false;
            }

            foreach(ProjectItem projectItem in dragItems)
            {
                if(folder.Children.Any(child => child.NameExt == projectItem.NameExt))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnItemDragEnter(object sender, ItemDropCancelArgs e)
        {
            if (e.DropTarget == null || e.DropTarget is AssetItem || e.DragItems != null && e.DragItems.Contains(e.DropTarget) || FolderContainsItemWithSameName(e.DropTarget, e.DragItems))
            {
                Editor.DragDrop.SetCursor(KnownCursor.DropNotAllowed);
                e.Cancel = true;
            }
            else
            {
                Editor.DragDrop.SetCursor(KnownCursor.DropAllowed);
            }
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

            if (!(e.DropTarget is AssetItem) && (e.DragItems == null || !e.DragItems.Contains(e.DropTarget)))
            {
                ProjectItem[] projectItems = e.DragItems.OfType<ProjectItem>().ToArray();
                m_project.Move(projectItems, (ProjectItem)e.DropTarget);
            }
        }

        private void OnItemEndDrag(object sender, ItemArgs e)
        {
            Editor.DragDrop.RaiseDrop(e.PointerEventData);

            foreach (ProjectItem item in e.Items)
            {
                if (m_folders.All(f => f.Children == null || !f.Children.Contains(item)))
                {
                    m_raiseItemDeletedEvent = false;
                    try
                    {
                        m_listBox.RemoveChild(item.Parent, item);
                    }
                    finally
                    {
                        m_raiseItemDeletedEvent = true;
                    }   
                }
            }
        }

        private void OnItemDataBinding(object sender, ItemDataBindingArgs e)
        {
            ProjectItem projectItem = e.Item as ProjectItem;
            if (projectItem == null)
            {
                TextMeshProUGUI text = e.ItemPresenter.GetComponentInChildren<TextMeshProUGUI>(true);
                text.text = null;
                ProjectItemView itemView = e.ItemPresenter.GetComponentInChildren<ProjectItemView>(true);
                itemView.ProjectItem = null;
            }
            else
            {
                TextMeshProUGUI text = e.ItemPresenter.GetComponentInChildren<TextMeshProUGUI>(true);
                text.text = projectItem.Name;
                ProjectItemView itemView = e.ItemPresenter.GetComponentInChildren<ProjectItemView>(true);
                itemView.ProjectItem = projectItem;
            }
        }

        private void OnItemRemoving(object sender, ItemsCancelArgs e)
        {
            
        }

        private void OnItemRemoved(object sender, ItemsRemovedArgs e)
        {
            for(int i = 0; i < e.Items.Length; ++i)
            {
                ProjectItem item = (ProjectItem)e.Items[i];
                m_items.Remove(item);
                m_idToItem.Remove(item.ItemID);               
            }

            if(m_raiseItemDeletedEvent)
            {
                if (ItemDeleted != null)
                {
                    ItemDeleted(this, new ProjectTreeEventArgs(e.Items.OfType<ProjectItem>().ToArray()));
                }
            }
        }

        private void OnItemDoubleClick(object sender, ItemArgs e)
        {
            if(e.PointerEventData.button == PointerEventData.InputButton.Left)
            {
                if (ItemDoubleClick != null)
                {
                    ItemDoubleClick(this, new ProjectTreeEventArgs(e.Items.OfType<ProjectItem>().ToArray()));
                }
            }
        }

        private void OnItemBeginEdit(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            ProjectItem item = e.Item as ProjectItem;
            if (item != null)
            {
                TMP_InputField inputField = e.EditorPresenter.GetComponentInChildren<TMP_InputField>(true);
                inputField.text = item.Name;
                inputField.ActivateInputField();
                inputField.Select();

                Image itemImage = e.ItemPresenter.GetComponentInChildren<Image>(true);
                Image image = e.EditorPresenter.GetComponentInChildren<Image>(true);
                image.sprite = itemImage.sprite;
                image.gameObject.SetActive(true);


                TextMeshProUGUI text = e.ItemPresenter.GetComponentInChildren<TextMeshProUGUI>(true);
                text.text = item.Name;

                LayoutElement layout = inputField.GetComponent<LayoutElement>();
                if(layout != null)
                {
                    RectTransform rt = text.GetComponent<RectTransform>();
                    layout.preferredWidth = rt.rect.width;
                }
            }
        }

        private void OnItemEndEdit(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            TMP_InputField inputField = e.EditorPresenter.GetComponentInChildren<TMP_InputField>(true);
            TextMeshProUGUI text = e.ItemPresenter.GetComponentInChildren<TextMeshProUGUI>(true);

            ProjectItem projectItem = (ProjectItem)e.Item;
            string oldName = projectItem.Name;
            if (projectItem.Parent != null)
            {
                ProjectItem parentItem = projectItem.Parent;
                string newNameExt = inputField.text.Trim() + projectItem.Ext;
                if (!string.IsNullOrEmpty(inputField.text.Trim()) && ProjectItem.IsValidName(inputField.text.Trim()) && !parentItem.Children.Any(p => p.NameExt == newNameExt))
                {
                    projectItem.Name = inputField.text.Trim();
                }
            }

            if (projectItem.Name != oldName)
            {
                if (ItemRenamed != null)
                {
                    ItemRenamed(this, new ProjectTreeRenamedEventArgs(new[] { projectItem }, new[] { oldName }));
                }
            }

            text.text = projectItem.Name;

            //Following code is required to unfocus inputfield if focused and release InputManager
            if (EventSystem.current != null && !EventSystem.current.alreadySelecting)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs e)
        {
            if(!m_raiseSelectionChange)
            {
                return;
            }

            if(SelectionChanged != null)
            {
                ProjectItem[] selectedItems = e.NewItems == null ? new ProjectItem[0] : e.NewItems.OfType<ProjectItem>().ToArray();
                SelectionChanged(this, new ProjectTreeEventArgs(selectedItems));
            }
        }

        private bool CanCreatePrefab(ProjectItem dropTarget, object[] dragItems)
        {
            ExposeToEditor[] objects = dragItems.OfType<ExposeToEditor>().ToArray();
            if (objects.Length == 0)
            {
                return false;
            }

            if(!objects.All(o => o.CanCreatePrefab))
            {
                return false;
            }

            return true;
        }

        public override void DragEnter(object[] dragObjects, PointerEventData pointerEventData)
        {
            base.DragEnter(dragObjects, pointerEventData);
            m_listBox.ExternalBeginDrag(pointerEventData.position);
        }

        public override void DragLeave(PointerEventData pointerEventData)
        {
            base.DragLeave(pointerEventData);
            m_listBox.ExternalItemDrop();
            Editor.DragDrop.SetCursor(KnownCursor.DropNotAllowed);
        }

        public override void Drag(object[] dragObjects, PointerEventData pointerEventData)
        {
            base.Drag(dragObjects, pointerEventData);
            m_listBox.ExternalItemDrag(pointerEventData.position);
            if (!CanCreatePrefab((ProjectItem)m_listBox.DropTarget, dragObjects))
            {
                m_listBox.ClearTarget();

                Editor.DragDrop.SetCursor(KnownCursor.DropNotAllowed);

            }
            else
            {
                ProjectItem dropTarget = (ProjectItem)m_listBox.DropTarget;
                if (dropTarget != null && !dropTarget.IsFolder)
                {
                    m_listBox.ClearTarget();
                }

                Editor.DragDrop.SetCursor(KnownCursor.DropAllowed);
            }
        }

        public override void Drop(object[] dragObjects, PointerEventData pointerEventData)
        {
            base.Drop(dragObjects, pointerEventData);
            ProjectItem dropTarget = (ProjectItem)m_listBox.DropTarget;
            IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
            if (dropTarget != null && dropTarget.IsFolder && CanCreatePrefab(dropTarget, dragObjects))
            {
                ExposeToEditor dragObject = (ExposeToEditor)dragObjects[0];
                if (dropTarget.IsFolder)
                {
                    editor.CreatePrefab(dropTarget, dragObject, null, assetItem =>
                    {
                    });
                } 
            }
            else
            {
                if(dragObjects[0] is ExposeToEditor)
                {
                    ExposeToEditor dragObject = (ExposeToEditor)dragObjects[0];
                    if(dragObject.CanCreatePrefab)
                    {
                        editor.CreatePrefab(m_folders[0], dragObject, null, assetItem =>
                        {

                        });
                    }
                }
            }
            m_listBox.ExternalItemDrop();
        }

        private void EditorSelectionChanged(UnityObject[] unselectedObjects)
        {
            if(!HandleEditorSelectionChange)
            {
                return;
            }

            m_raiseSelectionChange = false;
            UnityObject[] selectedObjects = Editor.Selection.objects;
            if(selectedObjects != null)
            {
                List<ProjectItem> selectedItems = new List<ProjectItem>();
                for (int i = 0; i < selectedObjects.Length; ++i)
                {
                    UnityObject selectedObject = selectedObjects[i];
                    long id = m_project.ToID(selectedObject);
                    if(m_idToItem.ContainsKey(id))
                    {
                        ProjectItem item = m_idToItem[id];
                        if(item != null)
                        {
                            selectedItems.Add(item);
                        }
                    }
                }
                if(selectedItems.Count > 0)
                {
                    m_listBox.SelectedItems = selectedItems;
                }
                else if(m_listBox.SelectedItem != null)
                {
                    m_listBox.SelectedItem = null;
                }
            }
            else if (m_listBox.SelectedItem != null)
            {
                m_listBox.SelectedItem = null;
            }
            m_raiseSelectionChange = true;
        }

        private void OnItemClick(object sender, ItemArgs e)
        {
            if (e.PointerEventData.button == PointerEventData.InputButton.Right)
            {
                IContextMenu menu = IOC.Resolve<IContextMenu>();
                List<MenuItemInfo> menuItems = new List<MenuItemInfo>();

                MenuItemInfo createFolder = new MenuItemInfo { Path = "Create/Folder" };
                createFolder.Action = new MenuItemEvent();
                createFolder.Action.AddListener(CreateFolder);
                createFolder.Validate = new MenuItemValidationEvent();
                createFolder.Validate.AddListener(CreateValidate);
                menuItems.Add(createFolder);

                if (m_project.ToGuid(typeof(Material)) != Guid.Empty)
                {
                    MenuItemInfo createMaterial = new MenuItemInfo { Path = "Create/Material" };
                    createMaterial.Action = new MenuItemEvent();
                    createMaterial.Action.AddListener(CreateMaterial);
                    createMaterial.Validate = new MenuItemValidationEvent();
                    createMaterial.Validate.AddListener(CreateValidate);
                    menuItems.Add(createMaterial);
                }

                MenuItemInfo open = new MenuItemInfo { Path = "Open" };
                open.Action = new MenuItemEvent();
                open.Action.AddListener(Open);
                open.Validate = new MenuItemValidationEvent();
                open.Validate.AddListener(OpenValidate);
                menuItems.Add(open);

                MenuItemInfo duplicate = new MenuItemInfo { Path = "Duplicate" };
                duplicate.Action = new MenuItemEvent();
                duplicate.Action.AddListener(Duplicate);
                duplicate.Validate = new MenuItemValidationEvent();
                duplicate.Validate.AddListener(DuplicateValidate);
                menuItems.Add(duplicate);
                
                
                MenuItemInfo deleteFolder = new MenuItemInfo { Path = "Delete" };
                deleteFolder.Action = new MenuItemEvent();
                deleteFolder.Action.AddListener(Delete);
                menuItems.Add(deleteFolder);

                MenuItemInfo renameFolder = new MenuItemInfo { Path = "Rename" };
                renameFolder.Action = new MenuItemEvent();
                renameFolder.Action.AddListener(Rename);
                menuItems.Add(renameFolder);

                menu.Open(menuItems.ToArray());
            }
        }

        private void OnListBoxClick(object sender, PointerEventArgs e)
        {
            if (e.Data.button == PointerEventData.InputButton.Right)
            {
                IContextMenu menu = IOC.Resolve<IContextMenu>();

                List<MenuItemInfo> menuItems = new List<MenuItemInfo>();

                MenuItemInfo createFolder = new MenuItemInfo { Path = "Create/Folder" };
                createFolder.Action = new MenuItemEvent();
                createFolder.Command = "CurrentFolder";
                createFolder.Action.AddListener(CreateFolder);
                menuItems.Add(createFolder);

                if (m_project.ToGuid(typeof(Material)) != Guid.Empty)
                {
                    MenuItemInfo createMaterial = new MenuItemInfo { Path = "Create/Material" };
                    createMaterial.Action = new MenuItemEvent();
                    createMaterial.Command = "CurrentFolder";
                    createMaterial.Action.AddListener(CreateMaterial);
                    menuItems.Add(createMaterial);
                }

                menu.Open(menuItems.ToArray());
            }
        }

        private void CreateValidate(MenuItemValidationArgs args)
        {
            ProjectItem selectedItem = (ProjectItem)m_listBox.SelectedItem;
            if(selectedItem != null && !selectedItem.IsFolder)
            {
                args.IsValid = false;
            }
        }

        private void CreateFolder(string arg)
        {
            bool currentFolder = !string.IsNullOrEmpty(arg);

            ProjectItem parentFolder = currentFolder ? m_folders.FirstOrDefault() : (ProjectItem)m_listBox.SelectedItem;
            if(parentFolder == null)
            {
                return;
            }
            ProjectItem folder = new ProjectItem();

            string[] existingNames = parentFolder.Children.Where(c => c.IsFolder).Select(c => c.Name).ToArray();
            folder.Name = m_project.GetUniqueName("Folder", parentFolder.Children == null ? new string[0] : existingNames);
            folder.Children = new List<ProjectItem>();
            parentFolder.AddChild(folder);

            if(currentFolder)
            {
                InsertItems(new[] { folder }, true);
            }

            Editor.IsBusy = true;
            m_project.CreateFolder(folder, (error, projectItem) => Editor.IsBusy = false);
        }

        private void CreateMaterial(string arg)
        {
            bool currentFolder = !string.IsNullOrEmpty(arg);

            ProjectItem parentFolder = currentFolder ? m_folders.FirstOrDefault() : (ProjectItem)m_listBox.SelectedItem;
            if (parentFolder == null)
            {
                return;
            }

            IUnityObjectFactory objectFactory = IOC.Resolve<IUnityObjectFactory>();
            UnityObject unityObject = objectFactory.CreateInstance(typeof(Material), null);

            IResourcePreviewUtility resourcePreview = IOC.Resolve<IResourcePreviewUtility>();
            byte[] preview = resourcePreview.CreatePreviewData(unityObject);
            Editor.IsBusy = true;
            m_project.Save(new[] { parentFolder }, new[] { preview }, new[] { unityObject }, null, (error, assetItems) =>
            {
                if(!currentFolder)
                {
                    if (ItemDoubleClick != null)
                    {
                        ItemDoubleClick(this, new ProjectTreeEventArgs(new[] { parentFolder }));
                    }
                }

                Editor.ActivateWindow(this);
                Destroy(unityObject);
            });
        }

        private void OpenValidate(MenuItemValidationArgs args)
        {
            ProjectItem selectedItem = (ProjectItem)m_listBox.SelectedItem;
            if (m_listBox.SelectedItemsCount != 1 || !selectedItem.IsFolder && !m_project.IsScene(selectedItem))
            {
                args.IsValid = false;
            }
        }

        private void Open(string arg)
        {
            ProjectItem selectedItem = (ProjectItem)m_listBox.SelectedItem;
            if (ItemDoubleClick != null)
            {
                ItemDoubleClick(this, new ProjectTreeEventArgs(new[] { selectedItem }));
            }
        }

        private void DuplicateValidate(MenuItemValidationArgs args)
        {
            if (m_listBox.SelectedItems == null || m_listBox.SelectedItems.OfType<AssetItem>().FirstOrDefault() == null)
            {
                args.IsValid = false;
            }
        }

        private void Duplicate(string arg)
        {
            AssetItem[] assetItems = m_listBox.SelectedItems.OfType<AssetItem>().ToArray();
            Editor.IsBusy = true;
            m_project.Duplicate(assetItems, (error, duplicates) => Editor.IsBusy = false);
        }

        private void Delete(string arg)
        {
            DeleteSelectedAssets();
        }

        private void Rename(string arg)
        {
            VirtualizingTreeViewItem treeViewItem = m_listBox.GetTreeViewItem(m_listBox.SelectedItem);
            if (treeViewItem != null && treeViewItem.CanEdit)
            {
                treeViewItem.IsEditing = true;
            }
        }

        public void DeleteSelectedAssets()
        {
            m_windowManager.Confirmation("Delete Selected Assets", "You cannot undo this action", (sender, arg) =>
            {
                m_listBox.RemoveSelectedItems();
                bool wasEnabled = Editor.Undo.Enabled;
                Editor.Undo.Enabled = false;
                Editor.Selection.objects = null;
                Editor.Undo.Enabled = wasEnabled;
            },
            (sender, arg) => { },
            "Delete");
        }

        public void OnDeleted(ProjectItem[] projectItems)
        {
            for(int i = 0; i < projectItems.Length; ++i)
            {
                m_listBox.RemoveChild(null, projectItems[i]);
            }
        }

        public void SelectAll()
        {
            m_listBox.SelectedItems = m_listBox.Items;
        }

    }
}
