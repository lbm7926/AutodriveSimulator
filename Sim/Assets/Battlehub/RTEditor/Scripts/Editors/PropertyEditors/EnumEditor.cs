using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;

namespace Battlehub.RTEditor
{
    public class EnumEditor : PropertyEditor<Enum>
    {
        [SerializeField]
        private TMP_Dropdown m_input = null;

        protected override void AwakeOverride()
        {
            base.AwakeOverride();
            m_input.onValueChanged.AddListener(OnValueChanged);
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            if (m_input != null)
            {
                m_input.onValueChanged.RemoveListener(OnValueChanged);
            }
        }

        public Type GetEnumType(object target)
        {
            CustomTypeFieldAccessor fieldAccessor = target as CustomTypeFieldAccessor;
            if (fieldAccessor != null)
            {
                return fieldAccessor.Type;
            }
            else
            {
                return MemberInfoType;
            }
        }

        protected override void InitOverride(object target, object accessor, MemberInfo memberInfo, Action<object, object> eraseTargetCallback, string label = null)
        {
            base.InitOverride(target, accessor, memberInfo, eraseTargetCallback, label);
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

            Type enumType = GetEnumType(accessor);
            string[] names = Enum.GetNames(enumType);

            for (int i = 0; i < names.Length; ++i)
            {
                options.Add(new TMP_Dropdown.OptionData(names[i].Replace('_', ' ')));
            }

            m_input.options = options;
        }

        protected override void SetInputField(Enum value)
        {
            int index = 0;

            Type enumType = GetEnumType(Accessor);
            index = Array.IndexOf(Enum.GetValues(enumType), value);
            
            m_input.value = index;
        }

        private void OnValueChanged(int index)
        {
            Type enumType = GetEnumType(Accessor);
            Enum value = (Enum)Enum.GetValues(enumType).GetValue(index);
            SetValue(value);
            EndEdit();
        }
    }
}
