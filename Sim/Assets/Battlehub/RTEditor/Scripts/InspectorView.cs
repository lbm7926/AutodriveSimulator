using System;
using System.Linq;
using UnityEngine;

using Battlehub.RTCommon;
using UnityObject = UnityEngine.Object;
using Battlehub.RTSL.Interface;

namespace Battlehub.RTEditor
{
    public class InspectorView : RuntimeWindow
    {
        [SerializeField]
        private GameObject m_gameObjectEditor = null;

        [SerializeField]
        private GameObject m_materialEditor = null;

        [SerializeField]
        private Transform m_panel = null;

        [SerializeField]
        private GameObject m_addComponentRoot = null;

        [SerializeField]
        private AddComponentControl m_addComponentControl = null;

        private GameObject m_editor;

        private IEditorsMap m_editorsMap;

        protected override void AwakeOverride()
        {
            WindowType = RuntimeWindowType.Inspector;
            base.AwakeOverride();

            if (m_gameObjectEditor == null)
            {
                Debug.LogError("GameObjectEditor is not set");
            }
            if (m_materialEditor == null)
            {
                Debug.LogError("MaterialEditor is not set");
            }

            m_editorsMap = IOC.Resolve<IEditorsMap>();

            Editor.Selection.SelectionChanged += OnRuntimeSelectionChanged;
            CreateEditor();
        }

        protected override void UpdateOverride()
        {
            base.UpdateOverride();
            UnityObject obj = Editor.Selection.activeObject;
            if(obj == null)
            {
                DestroyEditor();
            }
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            if(Editor != null)
            {
                Editor.Selection.SelectionChanged -= OnRuntimeSelectionChanged;
            }
        }

        private void OnRuntimeSelectionChanged(UnityObject[] unselectedObjects)
        {
            if (m_editor != null &&  unselectedObjects != null && unselectedObjects.Length > 0)
            {
                IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
                if(editor.IsDirty)
                {
                    editor.IsDirty = false;
                    editor.SaveAsset(unselectedObjects[0], result =>
                    {
                        CreateEditor();
                    });
                }
                else
                {
                    CreateEditor();
                }
            }
            else
            {
                CreateEditor();
            }
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();
            if (editor.IsDirty && editor.Selection.activeObject != null)
            {
                editor.IsDirty = false;
                editor.SaveAsset(editor.Selection.activeObject, result =>
                {
                });
            }
        }

        private void DestroyEditor()
        {
            if (m_editor != null)
            {
                Destroy(m_editor);
            }

            if(m_addComponentRoot != null)
            {
                m_addComponentRoot.SetActive(false);
            }

            if (m_addComponentControl != null)
            {
                m_addComponentControl.ComponentSelected -= OnAddComponent;
            }
        }

        private void CreateEditor()
        {
            DestroyEditor();

            if (Editor.Selection.activeObject == null)
            {
                return;
            }

            UnityObject[] selectedObjects = Editor.Selection.objects.Where(o => o != null).ToArray();
            if (selectedObjects.Length != 1)
            {
                return;
            }

            Type objType = selectedObjects[0].GetType();
            for (int i = 1; i < selectedObjects.Length; ++i)
            {
                if (objType != selectedObjects[i].GetType())
                {
                    return;
                }
            }

            ExposeToEditor exposeToEditor = Editor.Selection.activeGameObject != null ?
                Editor.Selection.activeGameObject.GetComponent<ExposeToEditor>() : 
                null;

            if(exposeToEditor != null && !exposeToEditor.CanInspect)
            {
                return;
            }

            GameObject editorPrefab;
            if (objType == typeof(Material))
            {
                Material mat = selectedObjects[0] as Material;
                if (mat.shader == null)
                {
                    return;
                }

                editorPrefab = m_editorsMap.GetMaterialEditor(mat.shader);
            }
            else
            {
                if (!m_editorsMap.IsObjectEditorEnabled(objType))
                {
                    return;
                }
                editorPrefab = m_editorsMap.GetObjectEditor(objType);
            }

            if (editorPrefab != null)
            {
                m_editor = Instantiate(editorPrefab);
                m_editor.transform.SetParent(m_panel, false);
                m_editor.transform.SetAsFirstSibling();
            }

            if (m_addComponentRoot != null && exposeToEditor)
            {
                IProject project = IOC.Resolve<IProject>();
                if(project == null || project.ToAssetItem(Editor.Selection.activeGameObject) == null)
                {
                    m_addComponentRoot.SetActive(true);
                    if (m_addComponentControl != null)
                    {
                        m_addComponentControl.ComponentSelected += OnAddComponent;
                    }
                }
            }
        }


        private void OnAddComponent(Type type)
        {
            IRuntimeEditor editor = IOC.Resolve<IRuntimeEditor>();

            GameObject go = editor.Selection.activeGameObject;

            editor.Undo.AddComponent(go.GetComponent<ExposeToEditor>(), type);
        }
    }
}
