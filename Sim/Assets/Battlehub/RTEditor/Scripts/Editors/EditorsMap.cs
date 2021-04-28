using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Battlehub.RTCommon;

namespace Battlehub.RTEditor
{
    public interface IEditorsMap
    {
        void AddMapping(Type type, Type editorType, bool enabled, bool isPropertyEditor);
        void AddMapping(Type type, GameObject editor, bool enabled, bool isPropertyEditor);
        bool IsObjectEditorEnabled(Type type);
        bool IsPropertyEditorEnabled(Type type, bool strict = false);
        bool IsMaterialEditorEnabled(Shader shader);
        GameObject GetObjectEditor(Type type, bool strict = false);
        GameObject GetPropertyEditor(Type type, bool strict = false);
        GameObject GetMaterialEditor(Shader shader, bool strict = false);
        Type[] GetEditableTypes();
    }

    public partial class EditorsMap : IEditorsMap
    {
        private class EditorDescriptor
        {
            public int Index;
            public bool Enabled;
            public bool IsPropertyEditor;

            public EditorDescriptor(int index, bool enabled, bool isPropertyEditor)
            {
                Index = index;
                Enabled = enabled;
                IsPropertyEditor = isPropertyEditor;
            }
        }

        private class MaterialEditorDescriptor
        {
            public GameObject Editor;
            public bool Enabled;

            public MaterialEditorDescriptor(GameObject editor, bool enabled)
            {
                Editor = editor;
                Enabled = enabled;
            }
        }

        private GameObject m_defaultMaterialEditor;
        private Dictionary<Shader, MaterialEditorDescriptor> m_materialMap = new Dictionary<Shader, MaterialEditorDescriptor>();
        private Dictionary<Type, EditorDescriptor> m_map = new Dictionary<Type, EditorDescriptor>();
        private GameObject[] m_editors = new GameObject[0];
        private bool m_isLoaded = false;

        public EditorsMap()
        {
            LoadMap();
        }

        public void Reset()
        {
            if(!m_isLoaded)
            {
                return;
            }
            m_materialMap = new Dictionary<Shader, MaterialEditorDescriptor>();
            m_map = new Dictionary<Type, EditorDescriptor>();
            m_editors = new GameObject[0];
            m_defaultMaterialEditor = null;
            m_isLoaded = false;
        }

        private void DefaultEditorsMap()
        {
            m_map = new Dictionary<Type, EditorDescriptor>
            {
                { typeof(UnityEngine.GameObject), new EditorDescriptor(0, true, false) },
                { typeof(System.Object), new EditorDescriptor(1, true, true) },
                { typeof(UnityEngine.Object), new EditorDescriptor(2, true, true) },
                { typeof(System.Boolean), new EditorDescriptor(3, true, true) },
                { typeof(System.Enum), new EditorDescriptor(4, true, true) },
                { typeof(System.Collections.Generic.List<>), new EditorDescriptor(5, true, true) },
                { typeof(System.Array), new EditorDescriptor(6, true, true) },
                { typeof(System.String), new EditorDescriptor(7, true, true) },
                { typeof(System.Int32), new EditorDescriptor(8, true, true) },
                { typeof(System.Single), new EditorDescriptor(9, true, true) },
                { typeof(Range), new EditorDescriptor(10, true, true) },
                { typeof(UnityEngine.Vector2), new EditorDescriptor(11, true, true) },
                { typeof(UnityEngine.Vector3), new EditorDescriptor(12, true, true) },
                { typeof(UnityEngine.Vector4), new EditorDescriptor(13, true, true) },
                { typeof(UnityEngine.Quaternion), new EditorDescriptor(14, true, true) },
                { typeof(UnityEngine.Color), new EditorDescriptor(15, true, true) },
                { typeof(UnityEngine.Bounds), new EditorDescriptor(16, true, true) },
                { typeof(RangeInt), new EditorDescriptor(17, true, true) },
                { typeof(RangeOptions), new EditorDescriptor(18, true, true) },
                { typeof(HeaderText), new EditorDescriptor(19, true, true) },
                { typeof(UnityEngine.Component), new EditorDescriptor(20, true, false) },
                { typeof(UnityEngine.BoxCollider), new EditorDescriptor(21, true, false) },
                { typeof(UnityEngine.Camera), new EditorDescriptor(20, true, false) },
                { typeof(UnityEngine.CapsuleCollider), new EditorDescriptor(21, true, false) },
                { typeof(UnityEngine.FixedJoint), new EditorDescriptor(20, true, false) },
                { typeof(UnityEngine.HingeJoint), new EditorDescriptor(20, true, false) },
                { typeof(UnityEngine.Light), new EditorDescriptor(20, true, false) },
                { typeof(UnityEngine.MeshCollider), new EditorDescriptor(20, true, false) },
                { typeof(UnityEngine.MeshFilter), new EditorDescriptor(20, true, false) },
                { typeof(UnityEngine.MeshRenderer), new EditorDescriptor(20, true, false) },
                { typeof(UnityEngine.MonoBehaviour), new EditorDescriptor(20, false, false) },
                { typeof(UnityEngine.Rigidbody), new EditorDescriptor(20, true, false) },
                { typeof(UnityEngine.SkinnedMeshRenderer), new EditorDescriptor(20, true, false) },
                { typeof(UnityEngine.Skybox), new EditorDescriptor(20, true, false) },
                { typeof(UnityEngine.SphereCollider), new EditorDescriptor(21, true, false) },
                { typeof(UnityEngine.SpringJoint), new EditorDescriptor(20, true, false) },
                { typeof(UnityEngine.Transform), new EditorDescriptor(22, true, false) },
                { typeof(Cubeman.CubemanCharacter), new EditorDescriptor(20, true, false) },
                { typeof(Cubeman.CubemanUserControl), new EditorDescriptor(20, true, false) },
                { typeof(Cubeman.GameCameraFollow), new EditorDescriptor(20, true, false) },
                { typeof(Cubeman.GameCharacter), new EditorDescriptor(20, true, false) },
            };
        }

        partial void InitEditorsMap();

        public void LoadMap()
        {
            if(m_isLoaded)
            {
                return;
            }
            m_isLoaded = true;

            DefaultEditorsMap();
            InitEditorsMap();

            EditorsMapStorage editorsMap = Resources.Load<EditorsMapStorage>(EditorsMapStorage.EditorsMapPrefabName);
            if (editorsMap == null)
            {
                editorsMap = Resources.Load<EditorsMapStorage>(EditorsMapStorage.EditorsMapTemplateName);
            }
            if (editorsMap != null)
            {
                m_editors = editorsMap.Editors;
                
                for(int i = 0; i < editorsMap.MaterialEditors.Length; ++i)
                {
                    GameObject materialEditor = editorsMap.MaterialEditors[i];
                    Shader shader = editorsMap.Shaders[i];
                    bool enabled = editorsMap.IsMaterialEditorEnabled[i];
                    if(!m_materialMap.ContainsKey(shader))
                    {
                        m_materialMap.Add(shader, new MaterialEditorDescriptor(materialEditor, enabled));
                    }
                    m_defaultMaterialEditor = editorsMap.DefaultMaterialEditor;
                }
            }
            else
            {
                Debug.LogError("Editors map is null");
            }
        }

        public void AddMapping(Type type, Type editorType, bool enabled, bool isPropertyEditor)
        {
            GameObject editor = m_editors.Where(ed => ed.GetComponents<Component>().Any(c => c.GetType() == editorType)).FirstOrDefault();
            if (editor == null)
            {
                throw new ArgumentException("editorType");
            }

            AddMapping(type, editor, enabled, isPropertyEditor);
        }

        public void AddMapping(Type type, GameObject editor, bool enabled, bool isPropertyEditor)
        {
            int index = Array.IndexOf(m_editors, editor);
            if(index < 0)
            {
                Array.Resize(ref m_editors, m_editors.Length + 1);
                index = m_editors.Length - 1;
                m_editors[index] = editor;
            }
            m_map.Add(type, new EditorDescriptor(index, enabled, isPropertyEditor));
        }

        public bool IsObjectEditorEnabled(Type type)
        {
            return IsEditorEnabled(type, false, true);
        }

        public bool IsPropertyEditorEnabled(Type type, bool strict = false)
        {
            return IsEditorEnabled(type, true, strict);
        }

        private bool IsEditorEnabled(Type type, bool isPropertyEditor, bool strict)
        {
            EditorDescriptor descriptor = GetEditorDescriptor(type, isPropertyEditor, strict);
            if (descriptor != null)
            {
                return descriptor.Enabled;
            }
            return false;
        }

        public bool IsMaterialEditorEnabled(Shader shader)
        {
            MaterialEditorDescriptor descriptor = GetEditorDescriptor(shader);
            if (descriptor != null)
            {
                return descriptor.Enabled;
            }

            return false;
        }

        public GameObject GetObjectEditor(Type type, bool strict = false)
        {
            return GetEditor(type, false, strict);
        }

        public GameObject GetPropertyEditor(Type type, bool strict = false)
        {
            return GetEditor(type, true, strict);
        }

        private GameObject GetEditor(Type type, bool isPropertyEditor, bool strict = false)
        {
            EditorDescriptor descriptor = GetEditorDescriptor(type, isPropertyEditor, strict);
            if (descriptor != null)
            {
                return m_editors[descriptor.Index];
            }
            return null;
        }

        public GameObject GetMaterialEditor(Shader shader, bool strict = false)
        {
            MaterialEditorDescriptor descriptor = GetEditorDescriptor(shader);
            if(descriptor != null)
            {
                return descriptor.Editor;
            }

            if(strict)
            {
                return null;
            }

            return m_defaultMaterialEditor;
        }

        private MaterialEditorDescriptor GetEditorDescriptor(Shader shader)
        {
            MaterialEditorDescriptor descriptor;
            if(m_materialMap.TryGetValue(shader, out descriptor))
            {
                return m_materialMap[shader];
            }

            return null;
        }

        private EditorDescriptor GetEditorDescriptor(Type type, bool isPropertyEditor, bool strict)
        {
            do
            {
                EditorDescriptor descriptor;
                if (m_map.TryGetValue(type, out descriptor))
                {
                    if (descriptor.IsPropertyEditor == isPropertyEditor)
                    {
                        return descriptor;
                    }
                }
                else
                {
                    if (type.IsGenericType)
                    {
                        if (m_map.TryGetValue(type.GetGenericTypeDefinition(), out descriptor))
                        {
                            if (descriptor.IsPropertyEditor == isPropertyEditor)
                            {
                                return descriptor;
                            }
                        }
                    }
                }

                if (strict)
                {
                    break;
                }

                type = type.BaseType();
            }
            while (type != null);
            return null;
        }

        public Type[] GetEditableTypes()
        {
            return m_map.Where(kvp => kvp.Value != null && kvp.Value.Enabled).Select(kvp => kvp.Key).ToArray();
        }
    }
}
