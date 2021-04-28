using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using UnityObject = UnityEngine.Object;
namespace Battlehub.RTEditor
{
    public class ObjectEditor : PropertyEditor<UnityObject>, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private GameObject DragHighlight = null;
        [SerializeField]
        private TMP_InputField Input = null;
        [SerializeField]
        private Button BtnSelect = null;
        protected override void SetInputField(UnityObject value)
        {
            if (value != null)
            {
                Input.text = string.Format("{1} ({0})", MemberInfoType.Name, value.name);
            }
            else
            {
                Input.text = string.Format("None ({0})", MemberInfoType.Name);
            }
        }

        protected override void AwakeOverride()
        {
            base.AwakeOverride();
            BtnSelect.onClick.AddListener(OnSelect);
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            if(BtnSelect != null)
            {
                BtnSelect.onClick.RemoveListener(OnSelect);
            }

            if(Editor != null)
            {
                Editor.DragDrop.Drop -= OnDrop;
            }
        }

        private void OnSelect()
        {
            ISelectObjectDialog objectSelector = null;
            Transform dialogTransform = IOC.Resolve<IWindowManager>().CreateDialogWindow(RuntimeWindowType.SelectObject.ToString(), "Select " + MemberInfoType.Name,
                 (sender, args) =>
                 {
                     if (objectSelector.IsNoneSelected)
                     {
                         SetValue(null);
                         EndEdit();
                         SetInputField(null);
                     }
                     else
                     {
                         SetValue(objectSelector.SelectedObject);
                         EndEdit();
                         SetInputField(objectSelector.SelectedObject);
                     }
                 });
            objectSelector = IOC.Resolve<ISelectObjectDialog>();// dialogTransform.GetComponentInChildren<SelectObjectDialog>();
            objectSelector.ObjectType = MemberInfoType;
        }

        private void OnDrop(PointerEventData pointerEventData)
        {
            object dragObject = Editor.DragDrop.DragObjects[0];
            if(dragObject is AssetItem)
            {
                AssetItem assetItem = (AssetItem)dragObject;
                IProject project = IOC.Resolve<IProject>();
                Editor.IsBusy = true;
                project.Load(new[] { assetItem }, (error, loadedObjects) =>
                {
                    Editor.IsBusy = false;
                    if (error.HasError)
                    {
                        IWindowManager wnd = IOC.Resolve<IWindowManager>();
                        wnd.MessageBox("Unable to load object", error.ErrorText);
                        return;
                    }

                    SetValue(loadedObjects[0]);
                    EndEdit();
                    SetInputField(loadedObjects[0]);
                    HideDragHighlight();
                });
            }
            else if(dragObject is GameObject)
            {
                UnityObject value = GetGameObjectOrComponent((GameObject)dragObject);
                SetValue(value);
                EndEdit();
                SetInputField(value);
                HideDragHighlight();
            }
            else if(dragObject is ExposeToEditor)
            {
                UnityObject value = GetGameObjectOrComponent(((ExposeToEditor)dragObject).gameObject);
                SetValue(value);
                EndEdit();
                SetInputField(value);
                HideDragHighlight();
            }
        }

        private UnityObject GetGameObjectOrComponent(GameObject go)
        {
            Component component = go.GetComponent(MemberInfoType);
            if (component != null)
            {
                return component;
            }
            return go;
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if(!Editor.DragDrop.InProgress)
            {
                return;
            }
            object dragObject = Editor.DragDrop.DragObjects[0];            
            Type type = null;
            if(dragObject is ExposeToEditor)
            {
                ExposeToEditor exposeToEditor = (ExposeToEditor)dragObject;
                GameObject go = exposeToEditor.gameObject;
                type = ToType(go);
            }
            else if(dragObject is GameObject)
            {
                type = ToType((GameObject)dragObject);
            }
            else if(dragObject is AssetItem)
            {
                AssetItem assetItem = (AssetItem)dragObject;
                IProject project = IOC.Resolve<IProject>();
                type = project.ToType(assetItem);
            }

            if (type != null && MemberInfoType.IsAssignableFrom(type))
            {
                Editor.DragDrop.Drop -= OnDrop;
                Editor.DragDrop.Drop += OnDrop;
                ShowDragHighlight();
                Editor.DragDrop.SetCursor(Utils.KnownCursor.DropAllowed);
            }
        }

        private Type ToType(GameObject go)
        {
            Type type;
            if (go.GetComponent(MemberInfoType) != null)
            {
                type = MemberInfoType;
            }
            else
            {
                type = typeof(GameObject);
            }

            return type;
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            Editor.DragDrop.Drop -= OnDrop;
            if(Editor.DragDrop.InProgress)
            {
                Editor.DragDrop.SetCursor(Utils.KnownCursor.DropNotAllowed);
                HideDragHighlight();
            }
        }

        private void ShowDragHighlight()
        {
            if(DragHighlight != null)
            {
                DragHighlight.SetActive(true);
            }
        }

        private void HideDragHighlight()
        {
            if(DragHighlight != null)
            {
                DragHighlight.SetActive(false);
            }
        }
    }
}
