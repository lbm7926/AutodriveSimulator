using UnityEngine;

namespace Battlehub.RTTerrain
{
    public class TerrainToolHandle : MonoBehaviour
    {
        [SerializeField]
        private Color m_pointerOverColor = Color.yellow;

        [SerializeField]
        private Color m_selectedColor = Color.yellow;

        [SerializeField]
        private Color m_normalColor = Color.white;

        private bool m_isPointerOver;
        public bool IsPointerOver
        {
            get { return m_isPointerOver; }
            set
            {
                m_isPointerOver = value;
                UpdateVisualState();
            }
        }

        private bool m_isSelected;
        public bool IsSelected
        {
            get { return m_isSelected; }
            set
            {
                m_isSelected = value;
                UpdateVisualState();
            }
        }

        public bool ZTest
        {
            get { return m_renderer.sharedMaterial.GetFloat("_ZTest") != 0; }
            set
            {
                if(ZTest != value)
                {
                    m_renderer.sharedMaterial.SetFloat("_ZTest", value ? 2 : 0);
                }
            }
        }

        private void UpdateVisualState()
        {
            if(m_isSelected)
            {
                m_renderer.sharedMaterial.color = m_selectedColor;
            }
            else if(m_isPointerOver)
            {
                m_renderer.sharedMaterial.color = m_pointerOverColor;
            }
            else
            {
                m_renderer.sharedMaterial.color = m_normalColor;
            }
        }


        private Renderer m_renderer;
        private void Awake()
        {
            m_renderer = GetComponent<Renderer>();
            m_renderer.sharedMaterial = m_renderer.material;
            UpdateVisualState();
        }
    }
}
