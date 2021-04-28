using UnityEngine;
using Battlehub.RTEditor;
using Battlehub.RTCommon;
using UnityEngine.UI;

namespace Battlehub.RTBuilder
{
    public class UVManualEditorPanel : MonoBehaviour
    {
        [SerializeField]
        private Toggle m_useGizmosToggle = null;

        private IProBuilderTool m_tool;
        private IRTE m_editor;

        private void Awake()
        {
            m_tool = IOC.Resolve<IProBuilderTool>();
            m_tool.UVEditingModeChanged += OnUVEditingModeChanged;

            m_editor = IOC.Resolve<IRuntimeEditor>();
            m_editor.Undo.UndoCompleted += OnUpdateVisualState;
            m_editor.Undo.RedoCompleted += OnUpdateVisualState;
            m_editor.Undo.StateChanged += OnUpdateVisualState;

            if (m_useGizmosToggle != null)
            {
                m_useGizmosToggle.onValueChanged.AddListener(OnUseGizmosValueChanged);
            }

            OnUpdateVisualState();
        }


        private void OnDestroy()
        {
            if(m_tool != null)
            {
                m_tool.UVEditingModeChanged -= OnUVEditingModeChanged;
            }

            if(m_editor != null)
            {
                m_editor.Undo.UndoCompleted -= OnUpdateVisualState;
                m_editor.Undo.RedoCompleted -= OnUpdateVisualState;
                m_editor.Undo.StateChanged -= OnUpdateVisualState;
            }
            
            if (m_useGizmosToggle != null)
            {
                m_useGizmosToggle.onValueChanged.RemoveListener(OnUseGizmosValueChanged);
            }
        }

        private void OnUseGizmosValueChanged(bool value)
        {
            m_tool.UVEditingMode = value;
        }

        private void OnUVEditingModeChanged(bool obj)
        {
            OnUpdateVisualState();
        }

        private void OnUpdateVisualState()
        {
            m_useGizmosToggle.isOn = m_tool.UVEditingMode;
        }
    }
}
