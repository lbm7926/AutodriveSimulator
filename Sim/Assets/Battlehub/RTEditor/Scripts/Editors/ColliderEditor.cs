using Battlehub.RTCommon;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.RTEditor
{
    public class ColliderEditor : ComponentEditor
    {
        [SerializeField]
        private GameObject ToggleButton = null;

        private Toggle m_editColliderButton;

        private bool m_isEditing;

        private RuntimeTool m_lastTool;

        protected override void AwakeOverride()
        {
            base.AwakeOverride();
            m_lastTool = Editor.Tools.Current;
            Editor.Tools.ToolChanged += OnToolChanged;
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            if (m_editColliderButton != null)
            {
                m_editColliderButton.onValueChanged.RemoveListener(OnEditCollider);
            }

            if (Editor != null)
            {
                Editor.Tools.ToolChanged -= OnToolChanged;
                Editor.Tools.Current = m_lastTool;
            }
        }

        private void OnToolChanged()
        {
            if(Editor.Tools.Current != RuntimeTool.None)
            {
                m_lastTool = Editor.Tools.Current;
                m_isEditing = false;
                if (m_editColliderButton != null)
                {
                    m_editColliderButton.isOn = false;
                }
                
            }
        }

        protected override void BuildEditor(IComponentDescriptor componentDescriptor, PropertyDescriptor[] descriptors)
        {
            base.BuildEditor(componentDescriptor, descriptors);
            m_editColliderButton = Instantiate(ToggleButton).GetComponent<Toggle>();
            m_editColliderButton.transform.SetParent(EditorsPanel, false);
            m_editColliderButton.onValueChanged.RemoveListener(OnEditCollider);
            m_editColliderButton.isOn = m_isEditing;
            m_editColliderButton.onValueChanged.AddListener(OnEditCollider);
        }

        protected override void DestroyEditor()
        {
            base.DestroyEditor();
            if(m_editColliderButton != null)
            {
                m_editColliderButton.onValueChanged.RemoveListener(OnEditCollider);
                Destroy(m_editColliderButton.gameObject);
            }
        }

      
        private void OnEditCollider(bool edit)
        {
            m_isEditing = edit;
            if(m_isEditing)
            {
                m_lastTool = Editor.Tools.Current;
                Editor.Tools.Current = RuntimeTool.None;
                TryCreateGizmo(GetComponentDescriptor());
            }
            else
            {
                Editor.Tools.Current = m_lastTool;
                if (EndEditCallback != null)
                {
                    EndEditCallback();
                }
                DestroyGizmo();
            }
        }

        protected override void TryCreateGizmo(IComponentDescriptor componentDescriptor)
        {
            if(m_isEditing)
            {
                base.TryCreateGizmo(componentDescriptor);
            }   
        }

        protected override void DestroyGizmo()
        {
            base.DestroyGizmo();
        }
    }
}

