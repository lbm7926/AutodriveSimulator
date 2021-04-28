using UnityEngine;
using UnityEngine.UI;
using Battlehub.RTCommon;
using Battlehub.UIControls.Dialogs;

namespace Battlehub.RTEditor
{
    public class SelectColorDialog : RuntimeWindow
    {
        [HideInInspector]
        public Color SelectedColor = Color.white;

        [SerializeField]
        private ColorPicker m_colorPicker = null;

        private Dialog m_parentDialog;

        protected override void AwakeOverride()
        {
            WindowType = RuntimeWindowType.SelectColor;
            base.AwakeOverride();
        }

        private void Start()
        {
            m_parentDialog = GetComponentInParent<Dialog>();
            m_parentDialog.Ok += OnOk;
            m_parentDialog.IsOkVisible = true;
            m_parentDialog.OkText = "Select";
            m_parentDialog.IsCancelVisible = true;
            m_parentDialog.CancelText = "Cancel";
            m_colorPicker.CurrentColor = SelectedColor;
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            if (m_parentDialog != null) m_parentDialog.Ok -= OnOk;

        }
        private void OnOk(Dialog sender, DialogCancelArgs args)
        {
            SelectedColor = m_colorPicker.CurrentColor;
        }
    }
}
