using Battlehub.RTEditor;
using Battlehub.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.RTBuilder
{
    public class MaterialPaletteItem : MonoBehaviour
    {
        [SerializeField]
        private Button m_applyButton = null;

        [SerializeField]
        private Button m_selectButton = null;

        [SerializeField]
        private Button m_unselectButton = null;

        [SerializeField]
        private Button m_removeButton = null;

        [SerializeField]
        private ObjectEditor m_objectEditor = null;

        [SerializeField]
        private ObjectEditorEventHandler m_objectEditorEventHandler;

        private Material m_material;
        public Material Material
        {
            get { return m_material; }
            set
            {
                m_material = value;
                m_objectEditor.Reload();
            }
        }

        private TextMeshProUGUI m_text;
        public string Text
        {
            get
            {
                if(m_text == null)
                {
                    return null;
                }

                return m_text.text;
            }
            set
            {
                if(m_text == null)
                {
                    return;
                }

                m_text.text = value;
            }
        }

        private MaterialPaletteViewImpl m_paletteView;

        private void Awake()
        {
            m_applyButton.onClick.AddListener(OnApply);
            m_selectButton.onClick.AddListener(OnSelect);
            m_unselectButton.onClick.AddListener(OnUnselect);
            m_removeButton.onClick.AddListener(OnRemove);

            m_objectEditorEventHandler = m_objectEditor.GetComponentInChildren<ObjectEditorEventHandler>(true);
            if(m_objectEditorEventHandler != null)
            {
                m_objectEditorEventHandler.PointerDown += OnObjectEditorPointerDown;
            }
            
            m_text = m_applyButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void Start()
        {
            m_objectEditor.Init(this, this, Strong.PropertyInfo((MaterialPaletteItem x) => x.Material));
            m_paletteView = GetComponentInParent<MaterialPaletteViewImpl>();   
        }

        private void OnDestroy()
        {
            if(m_applyButton != null)
            {
                m_applyButton.onClick.RemoveListener(OnApply);
            }

            if(m_selectButton != null)
            {
                m_selectButton.onClick.RemoveListener(OnSelect);
            }

            if(m_unselectButton != null)
            {
                m_unselectButton.onClick.RemoveListener(OnUnselect);
            }

            if(m_removeButton != null)
            {
                m_removeButton.onClick.RemoveListener(OnRemove);
            }

            if (m_objectEditorEventHandler != null)
            {
                m_objectEditorEventHandler.PointerDown -= OnObjectEditorPointerDown;
            }
        }

        private void OnSelect()
        {
            m_paletteView.SelectFacesByMaterial(Material);
        }

        private void OnUnselect()
        {
            m_paletteView.UnselectFacesByMaterial(Material);
        }

        private void OnApply()
        {
            m_paletteView.ApplyMaterial(Material);
        }

        private void OnRemove()
        {
            m_paletteView.RemoveMaterial(Material);
        }

        private void OnObjectEditorPointerDown(object sender, PointerEventData e)
        {
            m_paletteView.SelectMaterial(Material);
        }
    }
}
