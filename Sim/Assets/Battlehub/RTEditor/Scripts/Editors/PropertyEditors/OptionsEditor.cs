using UnityEngine;

using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;

namespace Battlehub.RTEditor
{
    public class RangeOptions : Range
    {
        public string[] Options;

        public RangeOptions(params string[] options) : base(-1, -1)
        {
            Options = options;
        }
    }

    public class OptionsEditor : PropertyEditor<int>
    {
        [SerializeField]
        private TMP_Dropdown m_input = null;

        public string[] Options = new string[0];

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

            for (int i = 0; i < Options.Length; ++i)
            {
                options.Add(new TMP_Dropdown.OptionData(Options[i]));
            }

            m_input.options = options;
        }

        protected override void SetInputField(int value)
        {
            m_input.value = value;
        }

        private void OnValueChanged(int index)
        {
            SetValue(index);
            EndEdit();
        }
    }
}
