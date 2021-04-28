using Battlehub.RTCommon;
using Battlehub.RTEditor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.RTBuilder
{
    public class UVEditorView : RuntimeWindow
    {
        private IProBuilderTool m_tool;
        private IWindowManager m_wm;

        [SerializeField]
        private GameObject m_uvModePanel = null;

        [SerializeField]
        private Button m_convertToAutoUVsButton = null;

        [SerializeField]
        private Button m_convertToManualUVsButton = null;

        [SerializeField]
        private GameObject m_uvAutoEditorPanel = null;

        [SerializeField]
        private GameObject m_uvManualEditorPanel = null;

        [SerializeField]
        private TextMeshProUGUI m_modeText = null;

        protected override void AwakeOverride()
        {
            WindowType = RuntimeWindowType.Custom;
            base.AwakeOverride();
        }

        protected virtual void Start()
        {
            m_tool = IOC.Resolve<IProBuilderTool>();

            m_wm = IOC.Resolve<IWindowManager>();
            m_wm.WindowCreated += OnWindowCreated;

            OnToolSelectionChanged();
            m_tool.SelectionChanged += OnToolSelectionChanged;
            Editor.Selection.SelectionChanged += OnSelectionChanged;

            if (m_convertToAutoUVsButton != null)
            {
                m_convertToAutoUVsButton.onClick.AddListener(OnConvertToAutoUVsClick);
            }

            if(m_convertToManualUVsButton != null)
            {
                m_convertToManualUVsButton.onClick.AddListener(OnConvertToManualUVsClick);
            }
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            if(m_wm != null)
            {
                m_wm.WindowCreated -= OnWindowCreated;
            }

            if(m_tool != null)
            {
                m_tool.SelectionChanged -= OnToolSelectionChanged;
            }

            if(Editor != null)
            {
                Editor.Selection.SelectionChanged -= OnSelectionChanged;
            }

            if (m_convertToAutoUVsButton != null)
            {
                m_convertToAutoUVsButton.onClick.RemoveListener(OnConvertToAutoUVsClick);
            }

            if (m_convertToManualUVsButton != null)
            {
                m_convertToManualUVsButton.onClick.RemoveListener(OnConvertToManualUVsClick);
            }
        }

        private void OnConvertToAutoUVsClick()
        {
            m_tool.ConvertUVs(true);
            UpdateVisualState();
        }

        private void OnConvertToManualUVsClick()
        {
            m_tool.ConvertUVs(false);
            UpdateVisualState();
        }

        private void OnToolSelectionChanged()
        {
            UpdateVisualState();
        }

        private void OnSelectionChanged(Object[] unselectedObjects)
        {
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (!m_tool.HasSelectedFaces)
            {
                if (m_uvAutoEditorPanel != null)
                {
                    m_uvAutoEditorPanel.gameObject.SetActive(false);
                }
                if(m_uvManualEditorPanel != null)
                {
                    m_uvManualEditorPanel.gameObject.SetActive(false);
                }
                if(m_uvModePanel != null)
                {
                    m_uvModePanel.gameObject.SetActive(false);
                }
            }
            else
            {
                m_tool.UpdatePivot();

                bool hasSelectedManualUVs = m_tool.HasSelectedManualUVs;
                bool hasSelectedAutoUVs = m_tool.HasSelectedAutoUVs;
                if (m_uvAutoEditorPanel != null)
                {
                    m_uvAutoEditorPanel.gameObject.SetActive(hasSelectedAutoUVs && !hasSelectedManualUVs);
                }
                if(m_uvManualEditorPanel != null)
                {
                    m_uvManualEditorPanel.gameObject.SetActive(hasSelectedManualUVs && !hasSelectedAutoUVs);
                }

                if(m_modeText != null)
                {
                    if(hasSelectedAutoUVs && hasSelectedManualUVs)
                    {
                        m_modeText.text = "UV Mode: Mixed";
                    }
                    else if(hasSelectedAutoUVs)
                    {
                        m_modeText.text = "UV Mode: Auto";
                    }
                    else if (hasSelectedManualUVs)
                    {
                        m_modeText.text = "UV Mode: Manual";
                    }
                }

                if(m_uvModePanel != null)
                {
                    m_uvModePanel.gameObject.SetActive(true);
                }

                if(m_convertToAutoUVsButton != null)
                {
                    m_convertToAutoUVsButton.gameObject.SetActive(!hasSelectedAutoUVs || hasSelectedManualUVs);
                }
                
                if(m_convertToManualUVsButton != null)
                {
                    m_convertToManualUVsButton.gameObject.SetActive(!hasSelectedManualUVs || hasSelectedAutoUVs);
                }
            }
        }

        private void OnWindowCreated(Transform obj)
        {
            if(obj == m_wm.GetWindow("ProBuilder"))
            {
                if (m_tool != null)
                {
                    m_tool.SelectionChanged -= OnToolSelectionChanged;
                }
                m_tool = IOC.Resolve<IProBuilderTool>();
                OnToolSelectionChanged();
                m_tool.SelectionChanged += OnToolSelectionChanged;
            }
        }
    }
}

