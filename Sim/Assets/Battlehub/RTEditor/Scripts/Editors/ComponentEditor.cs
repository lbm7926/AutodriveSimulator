using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using TMPro;

namespace Battlehub.RTEditor
{
    public struct PropertyDescriptor
    {
        public string Label;
        public MemberInfo MemberInfo;
        public MemberInfo ComponentMemberInfo;
        public PropertyEditorCallback ValueChangedCallback;
        public PropertyEditorCallback EndEditCallback;
        public Range Range;

        public PropertyDescriptor[] ChildDesciptors;
     
        public Type MemberType
        {
            get
            {
                if(Range != null)
                {
                    return Range.GetType();
                }

                if (MemberInfo is PropertyInfo)
                {
                    PropertyInfo prop = (PropertyInfo)MemberInfo;
                    return prop.PropertyType;
                }
                else if (MemberInfo is FieldInfo)
                {
                    FieldInfo field = (FieldInfo)MemberInfo;
                    return field.FieldType;
                }

                return null;
            }
        }

		public object Target;
        
        public PropertyDescriptor(string label, object target, MemberInfo memberInfo)
        {
            MemberInfo = memberInfo;
            ComponentMemberInfo = memberInfo;
            Label = label;
            Target = target;
            ValueChangedCallback = null;
            EndEditCallback = null;
            Range = TryGetRange(memberInfo);
            ChildDesciptors = null;
        }

        public PropertyDescriptor(string label, object target, MemberInfo memberInfo, MemberInfo componentMemberInfo)
        {
            MemberInfo = memberInfo;
            ComponentMemberInfo = componentMemberInfo;
            Label = label;
            Target = target;
            ValueChangedCallback = null;
            EndEditCallback = null;
            Range = TryGetRange(memberInfo);
            ChildDesciptors = null;
        }

        public PropertyDescriptor(string label, object target, MemberInfo memberInfo, MemberInfo componentMemberInfo, PropertyEditorCallback valueChangedCallback)
        {
            MemberInfo = memberInfo;
            ComponentMemberInfo = componentMemberInfo;
            Label = label;
            Target = target;
            ValueChangedCallback = valueChangedCallback;
            EndEditCallback = null;
            Range = TryGetRange(memberInfo);
            ChildDesciptors = null;
        }

        public PropertyDescriptor(string label, object target, MemberInfo memberInfo, MemberInfo componentMemberInfo, PropertyEditorCallback valueChangedCallback, PropertyEditorCallback endEditCallback)
        {
            MemberInfo = memberInfo;
            ComponentMemberInfo = componentMemberInfo;
            Label = label;
            Target = target;
            ValueChangedCallback = valueChangedCallback;
            EndEditCallback = endEditCallback;
            Range = TryGetRange(memberInfo);
            ChildDesciptors = null;
        }

        public PropertyDescriptor(string label, object target, MemberInfo memberInfo, MemberInfo componentMemberInfo, PropertyEditorCallback valueChangedCallback, Range range)
        {
            MemberInfo = memberInfo;
            ComponentMemberInfo = componentMemberInfo;
            Label = label;
            Target = target;
            ValueChangedCallback = valueChangedCallback;
            EndEditCallback = null;
            Range = range;
            ChildDesciptors = null;
        }

        private static Range TryGetRange(MemberInfo memberInfo)
        {
            RangeAttribute range = memberInfo.GetCustomAttribute<RangeAttribute>();
            if (range != null)
            {
                if (memberInfo.GetUnderlyingType() == typeof(int))
                {
                    return new RangeInt((int)range.min, (int)range.max);
                }
                else if (memberInfo.GetUnderlyingType() == typeof(float))
                {
                    return new Range(range.min, range.max);
                }
            }
            return null;
        }

    }


    public class ComponentEditor : MonoBehaviour
    {
        private static Dictionary<Type, IComponentDescriptor> m_componentDescriptors;
        static ComponentEditor()
        {
            var type = typeof(IComponentDescriptor);

#if !UNITY_WSA || UNITY_EDITOR
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);
#else
            var types = type.GetTypeInfo().Assembly.GetTypes().
                Where(p => type.IsAssignableFrom(p) && p.GetTypeInfo().IsClass);
#endif

            m_componentDescriptors = new Dictionary<Type, IComponentDescriptor>();
            foreach (Type t in types)
            {
                IComponentDescriptor descriptor = (IComponentDescriptor)Activator.CreateInstance(t);
                if (descriptor == null)
                {
                    Debug.LogWarningFormat("Unable to instantiate selector of type " + t.FullName);
                    continue;
                }
                if (descriptor.ComponentType == null)
                {
                    Debug.LogWarningFormat("ComponentType is null. Selector Type {0}", t.FullName);
                    continue;
                }
                if (m_componentDescriptors.ContainsKey(descriptor.ComponentType))
                {
                    Debug.LogWarningFormat("Duplicate selector for {0} found. Type name {1}. Using {2} instead", descriptor.ComponentType.FullName, descriptor.GetType().FullName, m_componentDescriptors[descriptor.ComponentType].GetType().FullName);
                }
                else
                {
                    m_componentDescriptors.Add(descriptor.ComponentType, descriptor);
                }
            }
        }

        public PropertyEditorCallback EndEditCallback;

        [SerializeField]
        protected Transform EditorsPanel = null;
        [SerializeField]
        private BoolEditor EnabledEditor = null;
        [SerializeField]
        private TextMeshProUGUI Header = null;
        [SerializeField]
        private Toggle Expander = null;
        [SerializeField]
        private GameObject ExpanderGraphics = null;
        [SerializeField]
        private Button ResetButton = null;
        [SerializeField]
        private Button RemoveButton = null;

        private object m_converter;

        private Component m_gizmo;

        private bool IsComponentExpanded
        {
            get
            {
                string componentName = "BH_CE_EX_" + Component.GetType().AssemblyQualifiedName;
                return PlayerPrefs.GetInt(componentName, 1) == 1;
            }
            set
            {
                string componentName = "BH_CE_EX_" + Component.GetType().AssemblyQualifiedName;
                PlayerPrefs.SetInt(componentName, value ? 1 : 0);
            }
        }

        private Component m_component;
        public Component Component
        {
            get { return m_component; }
            set
            {
                m_component = value;
                if(m_component == null)
                {
                    throw new ArgumentNullException("value");
                }

                IComponentDescriptor componentDescriptor = GetComponentDescriptor();

                PropertyInfo enabledProperty = EnabledProperty;
                if (enabledProperty != null && (componentDescriptor == null || componentDescriptor.GetHeaderDescriptor(m_editor).ShowEnableButton))
                {
                    EnabledEditor.gameObject.SetActive(true);
                    EnabledEditor.Init(Component, Component, enabledProperty, null, string.Empty, () => { },
                        () =>
                        {
                            if(IsComponentEnabled)
                            {
                                TryCreateGizmo(componentDescriptor);
                            }
                            else
                            {
                                DestroyGizmo();
                            }
                        }, 
                        () =>
                        {
                            if (EndEditCallback != null)
                            {
                                EndEditCallback();
                            }
                        });
                }
                else
                {
                    EnabledEditor.gameObject.SetActive(false);
                }

                if(componentDescriptor != null)
                {
                    Header.text = componentDescriptor.GetHeaderDescriptor(m_editor).DisplayName;
                }
                else
                {
                    Header.text = Component.GetType().Name;
                }
                
                if(Expander != null)
                {
                    Expander.isOn = IsComponentExpanded;
                }
                
                BuildEditor();

            }
        }

        private bool IsComponentEnabled
        {
            get
            {
                if (EnabledProperty == null)
                {
                    return true;
                }

                object v = EnabledProperty.GetValue(Component, null);
                if (v is bool)
                {
                    bool isEnabled = (bool)v;
                    return isEnabled;
                }
                return true;
            }   
        }

        private PropertyInfo EnabledProperty
        {
            get
            {
                Type type = Component.GetType();

                while(type != typeof(UnityEngine.Object))
                {
                    PropertyInfo prop = type.GetProperty("enabled", BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                    if (prop != null && prop.PropertyType == typeof(bool) && prop.CanRead && prop.CanWrite)
                    {
                        return prop;
                    }
                    type = type.BaseType();
                }

                return null;
            }
        }

        private IRTE m_editor;
        public IRTE Editor
        {
            get { return m_editor; }
        }

        private IProject m_project;

        private IEditorsMap m_editorsMap;

        private void Awake()
        {
            m_editor = IOC.Resolve<IRTE>();
            m_project = IOC.Resolve<IProject>();
            m_editorsMap = IOC.Resolve<IEditorsMap>();

            AwakeOverride();
        }

        protected virtual void AwakeOverride()
        {

        }

        private void Start()
        {
            if (Component == null)
            {
                return;
            }

            
            if(Expander != null)
            {
                Expander.onValueChanged.AddListener(OnExpanded);
            }
            
            if(ResetButton != null)
            {
                ResetButton.onClick.AddListener(OnResetClick);
            }

            if(RemoveButton != null)
            {
                RemoveButton.onClick.AddListener(OnRemove);
            }
            
            m_editor.Undo.UndoCompleted += OnUndoCompleted;
            m_editor.Undo.RedoCompleted += OnRedoCompleted;

            StartOverride();
        }

        protected virtual void StartOverride()
        {

        }

        private void OnDestroy()
        {
            if(m_editor != null)
            {
                m_editor.Undo.UndoCompleted -= OnUndoCompleted;
                m_editor.Undo.RedoCompleted -= OnRedoCompleted;
            }
            
            if (Expander != null)
            {
                Expander.onValueChanged.RemoveListener(OnExpanded);
            }

            if(ResetButton != null)
            {
                ResetButton.onClick.RemoveListener(OnResetClick);
            }

            if (RemoveButton != null)
            {
                RemoveButton.onClick.RemoveListener(OnRemove);
            }

            if (m_gizmo != null)
            {
                Destroy(m_gizmo);
                m_gizmo = null;
            }            

            OnDestroyOverride();
        }

        protected virtual void OnDestroyOverride()
        {

        }

        private void Update()
        {
            UpdateOverride();
        }

        protected virtual void UpdateOverride()
        {
            if(Component == null)
            {
                Destroy(gameObject);
            }
        }

        protected IComponentDescriptor GetComponentDescriptor()
        {
            IComponentDescriptor componentDescriptor;
            if (m_componentDescriptors.TryGetValue(Component.GetType(), out componentDescriptor))
            {
                return componentDescriptor;
            }
            return null;
        }

        private PropertyDescriptor[] GetDescriptors(object converter)
        {
            IComponentDescriptor componentDescriptor = GetComponentDescriptor();
            if (componentDescriptor != null)
            {
                PropertyDescriptor[] properties = componentDescriptor.GetProperties(this, converter);
                return properties;
            }
            else
            {
                if (Component.GetType().IsScript())
                {
                    FieldInfo[] serializableFields = Component.GetType().GetSerializableFields();
                    return serializableFields.Select(f => new PropertyDescriptor(f.Name, Component, f, f)).ToArray();
                }
                else
                {
                    PropertyInfo[] properties = Component.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite).ToArray();
                    return properties.Select(p => new PropertyDescriptor(p.Name, Component, p, p)).ToArray();
                }
            }
        }

        public void BuildEditor()
        {
            IComponentDescriptor componentDescriptor = GetComponentDescriptor();
            if (componentDescriptor != null)
            {
                m_converter = componentDescriptor.CreateConverter(this);
            }

            PropertyDescriptor[] descriptors = GetDescriptors(m_converter);
            if (descriptors == null || descriptors.Length == 0)
            {
                if(ExpanderGraphics != null)
                {
                    ExpanderGraphics.SetActive(false);
                }
                
                return;
            }

            
            if (ResetButton != null)
            {
                ResetButton.gameObject.SetActive(componentDescriptor != null ?
                    componentDescriptor.GetHeaderDescriptor(m_editor).ShowResetButton :
                    m_editor.ComponentEditorSettings.ShowResetButton);
            }

            if (RemoveButton != null)
            {
                bool showRemoveButton = componentDescriptor != null ?
                    componentDescriptor.GetHeaderDescriptor(m_editor).ShowRemoveButton :
                    m_editor.ComponentEditorSettings.ShowRemoveButton;
                if (showRemoveButton)
                {
                    bool canRemove = m_project == null || m_project.ToAssetItem(Component.gameObject) == null;
                    if (!canRemove)
                    {
                        showRemoveButton = false;
                    }
                }

                RemoveButton.gameObject.SetActive(showRemoveButton);
            }

            if (EnabledEditor != null && EnabledProperty != null)
            {
                EnabledEditor.gameObject.SetActive(componentDescriptor != null ?
                    componentDescriptor.GetHeaderDescriptor(m_editor).ShowEnableButton :
                    m_editor.ComponentEditorSettings.ShowEnableButton);
            }

            if (Expander == null)
            {
                BuildEditor(componentDescriptor, descriptors);
            }
            else
            {
                if (componentDescriptor != null ? !componentDescriptor.GetHeaderDescriptor(m_editor).ShowExpander : !m_editor.ComponentEditorSettings.ShowExpander)
                {
                    Expander.isOn = true;
                    Expander.enabled = false;
                }
                
                if (Expander.isOn)
                {
                    if (ExpanderGraphics != null)
                    {
                        ExpanderGraphics.SetActive(componentDescriptor != null ? componentDescriptor.GetHeaderDescriptor(m_editor).ShowExpander : m_editor.ComponentEditorSettings.ShowExpander);
                    }
                    BuildEditor(componentDescriptor, descriptors);
                }
            }
        }

        protected virtual void BuildEditor(IComponentDescriptor componentDescriptor, PropertyDescriptor[] descriptors)
        {
            DestroyEditor();
            TryCreateGizmo(componentDescriptor);

            for (int i = 0; i < descriptors.Length; ++i)
            {
                PropertyDescriptor descriptor = descriptors[i];
                if (descriptor.MemberInfo == EnabledProperty)
                {
                    continue;
                }
                BuildPropertyEditor(descriptor);
            }
        }


        protected virtual void BuildPropertyEditor(PropertyDescriptor descriptor)
        {
            PropertyEditor editor = InstantiatePropertyEditor(descriptor);
            if (editor == null)
            {
                return;
            }
            if (descriptor.Range != null)
            {
                if(descriptor.Range is RangeInt)
                {
                    RangeIntEditor rangeEditor = editor as RangeIntEditor;
                    rangeEditor.Min = (int)descriptor.Range.Min;
                    rangeEditor.Max = (int)descriptor.Range.Max;
                }
                else if(descriptor.Range is RangeOptions)
                {
                    RangeOptions range = (RangeOptions)descriptor.Range;
                    OptionsEditor optionsEditor = editor as OptionsEditor;
                    optionsEditor.Options = range.Options;
                }
                else
                {
                    RangeEditor rangeEditor = editor as RangeEditor;
                    rangeEditor.Min = descriptor.Range.Min;
                    rangeEditor.Max = descriptor.Range.Max;
                }   
            }

            InitEditor(editor, descriptor);
        }

        protected virtual void InitEditor(PropertyEditor editor, PropertyDescriptor descriptor)
        {
            editor.Init(descriptor.Target, descriptor.Target, descriptor.MemberInfo, null, descriptor.Label, null, descriptor.ValueChangedCallback, () => { descriptor.EndEditCallback?.Invoke(); EndEditCallback?.Invoke(); }, true, descriptor.ChildDesciptors);
        }

        protected virtual void DestroyEditor()
        {
            DestroyGizmo();
            foreach (Transform t in EditorsPanel)
            {
                Destroy(t.gameObject);
            }
        }


        protected virtual void TryCreateGizmo(IComponentDescriptor componentDescriptor)
        {
            if (componentDescriptor != null && componentDescriptor.GizmoType != null && IsComponentEnabled)
            {
                m_gizmo = Component.gameObject.GetComponent(componentDescriptor.GizmoType);
                if (m_gizmo == null)
                {
                    m_gizmo = Component.gameObject.AddComponent(componentDescriptor.GizmoType);
                }
                m_gizmo.SendMessageUpwards("Reset", SendMessageOptions.DontRequireReceiver);
            }
        }


        protected virtual void DestroyGizmo()
        {
            if (m_gizmo != null)
            {
                DestroyImmediate(m_gizmo);
                m_gizmo = null;
            }
        }


        private PropertyEditor InstantiatePropertyEditor(PropertyDescriptor descriptor)
        {
            if (descriptor.MemberInfo == null)
            {
                Debug.LogError("desciptor.MemberInfo is null");
                return null;
            }

            if (descriptor.MemberType == null)
            {
                Debug.LogError("descriptor.MemberType is null");
                return null;
            }

            GameObject editorGo = m_editorsMap.GetPropertyEditor(descriptor.MemberType);
            if (editorGo == null)
            {
                return null;
            }

            if (!m_editorsMap.IsPropertyEditorEnabled(descriptor.MemberType))
            {
                return null;
            }
            PropertyEditor editor = editorGo.GetComponent<PropertyEditor>();
            if (editor == null)
            {
                Debug.LogErrorFormat("editor {0} is not PropertyEditor", editorGo);
                return null;
            }
            PropertyEditor instance = Instantiate(editor);
            instance.transform.SetParent(EditorsPanel, false);
            return instance;
        }

        private void OnExpanded(bool expanded)
        {
            IsComponentExpanded = expanded;
            if (expanded)
            {
                IComponentDescriptor componentDescriptor = GetComponentDescriptor();
                PropertyDescriptor[] descriptors = GetDescriptors(m_converter);
                if(ExpanderGraphics != null)
                {
                    ExpanderGraphics.SetActive(true);
                }
                
                BuildEditor(componentDescriptor, descriptors);
            }
            else
            {
                DestroyEditor();
            }
        }

        private PropertyEditor GetPropertyEditor(MemberInfo memberInfo)
        {
            foreach(Transform t in EditorsPanel)
            {
                PropertyEditor propertyEditor = t.GetComponent<PropertyEditor>();
                if(propertyEditor != null && propertyEditor.MemberInfo == memberInfo)
                {
                    return propertyEditor;
                }
            }
            return null;
        }

        private void OnRedoCompleted()
        {
            ReloadEditors();
        }

        private void OnUndoCompleted()
        {
            ReloadEditors();
        }

        private void ReloadEditors()
        {
            foreach (Transform t in EditorsPanel)
            {
                PropertyEditor propertyEditor = t.GetComponent<PropertyEditor>();
                if (propertyEditor != null)
                {
                    propertyEditor.Reload();
                }
            }
        }

        private void OnResetClick()
        {
            GameObject go = new GameObject();
            go.SetActive(false);

            Component component = go.GetComponent(Component.GetType());
            if (component == null)
            {
                component = go.AddComponent(Component.GetType());
            }

            bool isMonoBehavior = component is MonoBehaviour;

            PropertyDescriptor[] descriptors = GetDescriptors(m_converter);
            for(int i = 0; i < descriptors.Length; ++i)
            {
                PropertyDescriptor descriptor = descriptors[i];
                MemberInfo memberInfo = descriptor.ComponentMemberInfo;
                if(memberInfo is PropertyInfo)
                {
                    PropertyInfo p = (PropertyInfo)memberInfo;
                    object defaultValue = p.GetValue(component, null);
                    m_editor.Undo.BeginRecordValue(Component, memberInfo);
                    p.SetValue(Component, defaultValue, null);
                }
                else
                {
                    if (isMonoBehavior)
                    {
                        if(memberInfo is FieldInfo)
                        {
                            FieldInfo f = (FieldInfo)memberInfo;
                            object defaultValue = f.GetValue(component);
                            m_editor.Undo.BeginRecordValue(Component, memberInfo);
                            f.SetValue(Component, defaultValue);
                        }
                    }
                }
                
            }

            for (int i = 0; i < descriptors.Length; ++i)
            {
                PropertyDescriptor descriptor = descriptors[i];
                MemberInfo memberInfo = descriptor.MemberInfo;
                PropertyEditor propertyEditor = GetPropertyEditor(memberInfo);
                if (propertyEditor != null)
                {
                    propertyEditor.Reload();
                }
            }

            Destroy(go);

            m_editor.Undo.BeginRecord();

            for (int i = 0; i < descriptors.Length; ++i)
            {
                PropertyDescriptor descriptor = descriptors[i];
                MemberInfo memberInfo = descriptor.ComponentMemberInfo;
                if (memberInfo is PropertyInfo)
                {
                    m_editor.Undo.EndRecordValue(Component, memberInfo);
                }
                else
                {
                    if(isMonoBehavior)
                    {
                        m_editor.Undo.EndRecordValue(Component, memberInfo);
                    }
                }
            }

            m_editor.Undo.EndRecord();
        }

        private void OnRemove()
        {
            PropertyDescriptor[] descriptors = GetDescriptors(m_converter);
            Editor.Undo.DestroyComponent(Component, descriptors.Select(d => d.ComponentMemberInfo).ToArray());
        }
    }

}
