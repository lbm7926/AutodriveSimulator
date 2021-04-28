using System;
using Battlehub.RTCommon;
using Battlehub.UIControls;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.RTEditor
{
    public class ColorEditor : PropertyEditor<Color>
    {
        [SerializeField]
        private Image MainColor = null;

        [SerializeField]
        private RectTransform Alpha = null;

        [SerializeField]
        private Button BtnSelect = null;

        protected override void SetInputField(Color value)
        {
            Color color = value;
            color.a = 1.0f;
            MainColor.color = color;
            Alpha.transform.localScale = new Vector3(value.a, 1, 1);   
        }


        protected override void AwakeOverride()
        {
            base.AwakeOverride();
            BtnSelect.onClick.AddListener(OnSelect);
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            if (BtnSelect != null)
            {
                BtnSelect.onClick.RemoveListener(OnSelect);
            }
        }

        private void OnSelect()
        {
            SelectColorDialog colorSelector = null;
            Transform dialogTransform = IOC.Resolve<IWindowManager>().CreateDialogWindow(RuntimeWindowType.SelectColor.ToString(), "Select " + MemberInfoType.Name,
                (sender, args) =>
                {
                    SetValue(colorSelector.SelectedColor);
                    EndEdit();
                    SetInputField(colorSelector.SelectedColor);
                }, (sender, args) => { }, 200, 345, 200, 345, false);

            colorSelector = dialogTransform.GetComponentInChildren<SelectColorDialog>();
            colorSelector.SelectedColor = GetValue();
        }
    }

}
