﻿
using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

using Battlehub.RTCommon;

using UnityObject = UnityEngine.Object;
using System.Linq;
using System.Collections;
using TMPro;
using UnityEngine.EventSystems;

namespace Battlehub.RTEditor
{
    public class CustomTypeFieldAccessor
    {
        private MemberInfo m_memberInfo;
        private PropertyEditor<object> m_editor;

        public string Name
        {
            get;
            private set;
        }

        public Type Type
        {
            get;
            private set;
        }

        public object Value
        {
            get
            {
                object obj = m_editor.GetValue();
                if (obj == null)
                {
                    return null;
                }
                if(m_memberInfo is FieldInfo)
                {
                    return ((FieldInfo)m_memberInfo).GetValue(obj);
                }
                else if (m_memberInfo is PropertyInfo)
                {
                    return ((PropertyInfo)m_memberInfo).GetValue(obj, null);
                }
                return null;
            }
            set
            {
                object obj = m_editor.GetValue();
                if (m_memberInfo is FieldInfo)
                {
                    ((FieldInfo)m_memberInfo).SetValue(obj, value);
                }
                else if (m_memberInfo is PropertyInfo)
                {
                    ((PropertyInfo)m_memberInfo).SetValue(obj, value, null);
                }
                m_editor.SetValue(obj);
            }
        }

        public CustomTypeFieldAccessor(PropertyEditor<object> editor, MemberInfo fieldInfo, string name)
        {
            m_editor = editor;
            m_memberInfo = fieldInfo;
            Name = name;
            if (m_memberInfo is PropertyInfo)
            {
                PropertyInfo pInfo = (PropertyInfo)m_memberInfo;
                Type = pInfo.PropertyType;
            }
            else
            {
                FieldInfo fInfo = (FieldInfo)m_memberInfo;
                Type = fInfo.FieldType;
            }
        }
    }

    public class IListElementAccessor
    {
        private int m_index;
        private IListEditor m_editor;

        public string Name
        {
            get;
            private set;
        }

        public Type Type
        {
            get { return m_editor.ElementType; }
        }

        public object Value
        {
            get
            {
                IList list = GetList();
                if(list == null)
                {
                    return null;
                }

                if(m_index < 0 || m_index >= list.Count)
                {
                    return null;
                }

                return list[m_index];
            }
            set
            {
                IList list = GetList();
                list[m_index] = value;
                m_editor.SetValue(list);
            }
        }

        private IList GetList()
        {
            return m_editor.GetValue();
        }

        public IListElementAccessor(IListEditor editor, int index, string name)
        {
            m_editor = editor;
            m_index = index;
            Name = name;
        }
    }

    public delegate void PropertyEditorCallback();

    public class PropertyEditor : MonoBehaviour
    {
        private PropertyEditorCallback m_valueChangingCallback;
        private PropertyEditorCallback m_valueChangedCallback;
        private PropertyEditorCallback m_endEditCallback;

        private Dictionary<MemberInfo, PropertyDescriptor> m_childDescriptors;
        protected Dictionary<MemberInfo, PropertyDescriptor> ChildDescriptors
        {
            get { return m_childDescriptors; }
        }

        [SerializeField]
        protected TextMeshProUGUI Label;
        [SerializeField]
        protected int Indent = 10;
        private int m_effectiveIndent;
        private bool m_enableUndo;
        private bool m_isEditing;
        private bool m_lockValue;
        protected bool LockValue
        {
            get { return m_lockValue; }
        }

        protected object Target
        {
            get;
            private set;
        }

        protected object Accessor
        {
            get;
            private set;
        }

        //protected object[] m_targets;
        //protected object[] m_accessors;

        //protected object Target
        //{
        //    get
        //    {
        //        return m_targets != null && m_targets.Length > 0 ? m_targets[0] : null;
        //    }
        //    private set
        //    {
        //        if(value == null)
        //        {
        //            m_targets = null;
        //        }
        //        else
        //        {
        //            m_targets = new[] { value };
        //        }
        //    }
        //}
        
        //protected object Accessor
        //{
        //    get
        //    {
        //        return m_accessors != null && m_accessors.Length > 0 ? m_accessors[0] : null;
        //    }
        //    private set
        //    {
        //        if (value == null)
        //        {
        //            m_accessors = null;
        //        }
        //        else
        //        {
        //            m_accessors = new[] { value };
        //        }
        //    }
        //}

        private Action<object, object> m_eraseTargetCallback;

        public MemberInfo MemberInfo
        {
            get;
            private set;
        }
        protected Type MemberInfoType
        {
            get;
            private set;
        }

        private IRTE m_editor;
        public IRTE Editor
        {
            get { return m_editor; }
        }
        
        private void Awake()
        {
            m_editor = IOC.Resolve<IRTE>();
            AwakeOverride();
            Editor.Undo.BeforeUndo += OnBeforeUndo;
        }
        
        private void Start()
        {
            StartOverride();
        }

        private void OnTransformParentChanged()
        {
            if(transform.parent != null)
            {
                PropertyEditor parentEditor = transform.parent.GetComponentInParent<PropertyEditor>();
                if (parentEditor != null)
                {
                    m_effectiveIndent = parentEditor.m_effectiveIndent + Indent;
                    SetIndent(m_effectiveIndent);
                }
            }
        }

        protected virtual void SetIndent(float indent)
        {
            if (Label != null)
            {
                RectTransform rt = Label.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.offsetMin = new Vector2(indent, rt.offsetMin.y);
                }
            }
        }

        private void OnDestroy()
        {
            if(Editor != null)
            {
                Editor.Undo.BeforeUndo -= OnBeforeUndo;
            }
            
            OnDestroyOverride();
        }

        protected virtual void AwakeOverride()
        {

        }

        protected virtual void StartOverride()
        {

        }

        protected virtual void OnDestroyOverride()
        {

        }

        public void Init(object target, object accessor,
            MemberInfo memberInfo,
            Action<object, object> eraseTargetCallback = null,
            string label = null,
            PropertyEditorCallback valueChangingCallback = null,
            PropertyEditorCallback valueChangedCallback = null,
            PropertyEditorCallback endEditCallback = null,
            bool enableUndo = true,
            PropertyDescriptor[] childDescriptors = null)
        {
            m_enableUndo = enableUndo;
            m_valueChangingCallback = valueChangingCallback;
            m_valueChangedCallback = valueChangedCallback;
            m_endEditCallback = endEditCallback;
            if (childDescriptors != null)
            {
                m_childDescriptors = childDescriptors.ToDictionary(d => d.MemberInfo);
            }
            m_lockValue = true;
            InitOverride(target, accessor, memberInfo, eraseTargetCallback, label);
            m_lockValue = false;
        }       

        protected virtual void InitOverride(object target, object accessor, MemberInfo memberInfo, Action<object, object> eraseTargetCallback = null, string label = null)
        {
            if(target == null)
            {
                if (Label != null)
                {
                    if (label != null)
                    {
                        Label.text = label;
                    }
                }
                return;
                //throw new ArgumentNullException("target");
            }

            IListElementAccessor arrayElement = target as IListElementAccessor;
            if (arrayElement == null)
            {
                if (!(memberInfo is PropertyInfo) && !(memberInfo is FieldInfo))
                {
                    throw new ArgumentException("memberInfo should be PropertyInfo or FieldInfo");
                }

                if (memberInfo is PropertyInfo)
                {
                    Type propType = ((PropertyInfo)memberInfo).PropertyType;
                    MemberInfoType = propType;
                }
                else
                {
                    Type fieldType = ((FieldInfo)memberInfo).FieldType;
                    MemberInfoType = fieldType;
                }
                if(Label != null)
                {
                    if (label != null)
                    {
                        Label.text = label;
                    }
                    else
                    {
                        Label.text = memberInfo.Name;
                    }
                }
            }
            else
            {
                if(Label != null)
                {
                    Label.text = arrayElement.Name;
                }
                
                MemberInfoType = arrayElement.Type;
            }

            Target = target;
            Accessor = accessor;
            MemberInfo = memberInfo;
            m_eraseTargetCallback = eraseTargetCallback;
        }

        public void Reload()
        {
            if(m_isEditing)
            {
                return;
            }

            m_lockValue = true;
            ReloadOverride();
            m_lockValue = false;
        }

        protected virtual void ReloadOverride()
        {

        }

        protected void BeginEdit(bool record = true)
        {
            if(!m_isEditing && !m_lockValue)
            {
                if(record)
                {
                    BeginRecord();
                }
                
                m_isEditing = true;
            }
        }

        protected void EndEdit(bool record = true)
        {
            if(m_isEditing && record)
            {
                EndRecord();
                if(m_endEditCallback != null)
                {
                    m_endEditCallback();
                }
            }
            m_isEditing = false;
        }

        protected virtual void OnBeforeUndo()
        {
            if (m_isEditing)
            {
                EndRecord();
            }
            m_isEditing = false;
        }

        protected void BeginRecord()
        {
            if(m_enableUndo)
            {
                Editor.Undo.BeginRecordValue(Target, Accessor, MemberInfo);
            }
        }

        protected void EndRecord()
        {
            if(m_enableUndo)
            {
                Editor.Undo.EndRecordValue(Target, Accessor, MemberInfo, m_eraseTargetCallback);
            }
        }

        protected void RaiseValueChanging()
        {
            if(m_valueChangingCallback != null)
            {
                m_valueChangingCallback();
            }
        }

        protected void RaiseValueChanged()
        {
            if(m_valueChangedCallback != null)
            {
                m_valueChangedCallback();
            }
        }
    }

    public abstract class PropertyEditor<T> : PropertyEditor
    {
        protected T m_currentValue;
   
        protected override void InitOverride(object target, object accessor, MemberInfo memberInfo, Action<object, object> eraseTargetCallback = null, string label = null)
        {
            base.InitOverride(target, accessor, memberInfo, eraseTargetCallback, label);
            SetInputField(GetValue());
        }

        protected virtual void SetInputField(T value)
        {
        }

        public T GetValue()
        {
            if(Target is UnityObject)
            {
                UnityObject obj = (UnityObject)Target;
                if(obj == null)
                {
                    return default(T);
                }
            }

            if (Accessor == null)
            {
                return default(T);
            }

            if (MemberInfo is PropertyInfo)
            {
                PropertyInfo prop = (PropertyInfo)MemberInfo;
                return (T)prop.GetValue(Accessor, null);
            }
            

            FieldInfo field = (FieldInfo)MemberInfo;
            return (T)field.GetValue(Accessor);
        }

        public void SetValue(T value)
        {
            if(LockValue)
            {
                return;
            }
            if (Target is UnityObject)
            {
                UnityObject obj = (UnityObject)Target;
                if (obj == null)
                {
                    return;
                }
            }

            RaiseValueChanging();
            BeginEdit();
            if (MemberInfo is PropertyInfo)
            {
                PropertyInfo prop = (PropertyInfo)MemberInfo;
                prop.SetValue(Accessor, value, null);
            }
            else
            {
                FieldInfo field = (FieldInfo)MemberInfo;
                field.SetValue(Accessor, value);
            }

            m_currentValue = value;
            RaiseValueChanged();
        }

        private const float m_updateInterval = 0.25f;
        private float m_nextUpate;
        protected void Update()
        {
            if(m_nextUpate > Time.time)
            {
                return;
            }
            
            m_nextUpate = Time.time + m_updateInterval;
            
            if (MemberInfo == null)
            {
                return;
            }

            
            if(Target == null)
            {
                return;
            }

            if(Target is UnityObject)
            {
                UnityObject uobj = (UnityObject)Target;
                if(uobj == null)
                {
                    return;
                }
            }

            Reload();
        }

        protected override void ReloadOverride()
        {
            base.ReloadOverride();
            
            T value = GetValue();
            if (!EqualityComparer<T>.Default.Equals(m_currentValue, value))
            {
                m_currentValue = value;
                SetInputField(value);
            }
        }
    }
}
