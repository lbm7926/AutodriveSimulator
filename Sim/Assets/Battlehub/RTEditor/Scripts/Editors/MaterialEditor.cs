using UnityEngine;
using System.Collections;

using System.Reflection;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Battlehub.Utils;
using System;
using System.Collections.Generic;

using Battlehub.RTCommon;
using Battlehub.RTSL;
using Battlehub.RTSL.Interface;
using TMPro;
#if PROC_MATERIAL
using ProcPropertyDescription = UnityEngine.ProceduralPropertyDescription;
using ProcPropertyType = UnityEngine.ProceduralPropertyType;
#endif
namespace Battlehub.RTEditor
{
 

    public class MaterialPropertyDescriptor
    {
        public object Target;
        public object Accessor;
        public string Label;
        public RTShaderPropertyType Type;
        public Action<object, object> EraseTargetCallback;

        public PropertyInfo PropertyInfo;
        public RuntimeShaderInfo.RangeLimits Limits;
        public TextureDimension TexDims;
        
        public PropertyEditorCallback ValueChangedCallback;

        public MaterialPropertyDescriptor(object target, object acessor, string label, RTShaderPropertyType type, PropertyInfo propertyInfo, RuntimeShaderInfo.RangeLimits limits, TextureDimension dims, PropertyEditorCallback callback, Action<object, object> eraseTargetCallback)
        {
            Target = target;
            Accessor = acessor;
            Label = label;
            Type = type;
            PropertyInfo = propertyInfo;
            Limits = limits;
            TexDims = dims;
            ValueChangedCallback = callback;
            EraseTargetCallback = eraseTargetCallback;
        }
    }


    public interface IMaterialDescriptor
    {
        string ShaderName
        {
            get;
        }

        object CreateConverter(MaterialEditor editor);

        MaterialPropertyDescriptor[] GetProperties(MaterialEditor editor, object converter);
    }

    public class MaterialEditor : MonoBehaviour
    {
        private static Dictionary<string, IMaterialDescriptor> m_propertySelectors;
        static MaterialEditor()
        {
            var type = typeof(IMaterialDescriptor);

            var types = Reflection.GetAssignableFromTypes(type);

            m_propertySelectors = new Dictionary<string, IMaterialDescriptor>();
            foreach (Type t in types)
            {
                IMaterialDescriptor selector = (IMaterialDescriptor)Activator.CreateInstance(t);
                if (selector == null)
                {
                    Debug.LogWarningFormat("Unable to instantiate selector of type " + t.FullName);
                    continue;
                }
                if (selector.ShaderName == null)
                {
                    Debug.LogWarningFormat("ComponentType is null. Selector ShaderName is null {0}", t.FullName);
                    continue;
                }
                if (m_propertySelectors.ContainsKey(selector.ShaderName))
                {
                    Debug.LogWarningFormat("Duplicate component selector for {0} found. Type name {1}. Using {2} instead", selector.ShaderName, selector.GetType().FullName, m_propertySelectors[selector.ShaderName].GetType().FullName);
                }
                else
                {
                    m_propertySelectors.Add(selector.ShaderName, selector);
                }
            }
        }
        
        [SerializeField]
        private RangeEditor RangeEditor = null;

        [SerializeField]
        private Image m_image = null;
        [SerializeField]
        private TextMeshProUGUI TxtMaterialName = null;
        [SerializeField]
        private TextMeshProUGUI TxtShaderName = null;

        [SerializeField]
        private Transform EditorsPanel = null;

        [HideInInspector]
        public Material Material = null;

        private IRuntimeEditor m_editor;
        private IResourcePreviewUtility m_resourcePreviewUtility;
        private Texture2D m_previewTexture;
        private IEditorsMap m_editorsMap;

        private void Start()
        {
            m_editor = IOC.Resolve<IRuntimeEditor>();
            m_editor.Undo.UndoCompleted += OnUndoCompleted;
            m_editor.Undo.RedoCompleted += OnRedoCompleted;
            m_resourcePreviewUtility = IOC.Resolve<IResourcePreviewUtility>();
            m_editorsMap = IOC.Resolve<IEditorsMap>();

            if (Material == null)
            {
                Material = m_editor.Selection.activeObject as Material;
            }

            if (Material == null)
            {
                Debug.LogError("Select material");
                return;
            }

            m_previewTexture = new Texture2D(1, 1, TextureFormat.ARGB32, true);

            TxtMaterialName.text = Material.name;
            if (Material.shader != null)
            {
                TxtShaderName.text = Material.shader.name;
            }
            else
            {
                TxtShaderName.text = "Shader missing";
            }

    
            UpdatePreview(Material);

            BuildEditor();
        }

        

        private void Update()
        {   
            if (Material == null)
            {
                return;
            }
            if (TxtMaterialName != null && TxtMaterialName.text != Material.name)
            {
                TxtMaterialName.text = Material.name;
            }
        }


        private void OnDestroy()
        {
            if(m_editor != null && m_editor.Undo != null)
            {
                m_editor.Undo.UndoCompleted -= OnUndoCompleted;
                m_editor.Undo.RedoCompleted -= OnRedoCompleted;
            }
            
            if (m_previewTexture != null)
            {
                Destroy(m_previewTexture);
            }
        }

        public void BuildEditor()
        {
            foreach(Transform t in EditorsPanel)
            {
                Destroy(t.gameObject);
            }

            IMaterialDescriptor selector;
            if(!m_propertySelectors.TryGetValue(Material.shader.name, out selector))
            {
                selector = new MaterialDescriptor();
            }


            object converter = selector.CreateConverter(this);
            MaterialPropertyDescriptor[] descriptors = selector.GetProperties(this, converter);
            if(descriptors == null)
            {
                Destroy(gameObject);
                return;
            }

            for(int i = 0; i < descriptors.Length; ++i)
            {
                MaterialPropertyDescriptor descriptor = descriptors[i];
                PropertyEditor editor = null;
                PropertyInfo propertyInfo = descriptor.PropertyInfo;

                RTShaderPropertyType propertyType = descriptor.Type;

                switch (propertyType)
                {
                    case RTShaderPropertyType.Range:
                        if (RangeEditor != null)
                        {
                            RangeEditor range = Instantiate(RangeEditor);
                            range.transform.SetParent(EditorsPanel, false);

                            var rangeLimits = descriptor.Limits;
                            range.Min = rangeLimits.Min;
                            range.Max = rangeLimits.Max;
                            editor = range;
                        }
                        break;
                    default:
                        if (m_editorsMap.IsPropertyEditorEnabled(propertyInfo.PropertyType))
                        {
                            GameObject editorPrefab = m_editorsMap.GetPropertyEditor(propertyInfo.PropertyType);
                            GameObject instance = Instantiate(editorPrefab);
                            instance.transform.SetParent(EditorsPanel, false);

                            if (instance != null)
                            {
                                editor = instance.GetComponent<PropertyEditor>();
                            }
                        }
                        break;
                }
                

                if (editor == null)
                {
                    continue;
                }

                editor.Init(descriptor.Target, descriptor.Accessor, propertyInfo, descriptor.EraseTargetCallback, descriptor.Label, null, descriptor.ValueChangedCallback, () => 
                {
                    m_editor.IsDirty = true;
                    UpdatePreview(Material);
                });
            }
        }


        private PropertyEditor InstantiateEditor( PropertyInfo propertyInfo)
        {
            PropertyEditor editor = null;
            if (m_editorsMap.IsPropertyEditorEnabled(propertyInfo.PropertyType))
            {
                GameObject prefab = m_editorsMap.GetPropertyEditor(propertyInfo.PropertyType);
                if (prefab != null)
                {
                    editor = Instantiate(prefab).GetComponent<PropertyEditor>();
                    editor.transform.SetParent(EditorsPanel, false);
                }
            }

            return editor;
        }

        private void OnRedoCompleted()
        {
            if (Material != null)
            {
                UpdatePreview(Material);
            }
        }

        private void OnUndoCompleted()
        {
            if (Material != null)
            {
                UpdatePreview(Material);
            }
        }

        private void UpdatePreview(Material material)
        {
            m_editor.UpdatePreview(material, assetItem =>
            {
                if (m_image != null && assetItem != null)
                {
                    m_previewTexture.LoadImage(assetItem.Preview.PreviewData);
                    m_image.sprite = Sprite.Create(m_previewTexture, new Rect(0, 0, m_previewTexture.width, m_previewTexture.height), new Vector2(0.5f, 0.5f));
                }
            });
        }

        //private int m_updateCounter = 0;
        //private void Update()
        //{
        //    m_updateCounter++;
        //    m_updateCounter %= 120;
        //    if (m_updateCounter == 0)
        //    {
        //        UpdatePreview(Material);
        //    }
        //}
    }
}

