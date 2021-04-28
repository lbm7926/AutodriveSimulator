using Battlehub.RTCommon;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.RTBuilder
{
    public class MaterialPaletteTextureDropArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private MaterialPaletteViewImpl m_paletteView;
        private IRTE m_rte;

        [SerializeField]
        private GameObject m_highlight = null;

        private bool m_isPointerOver;
        private bool IsPointerOver
        {
            get { return m_isPointerOver; }
            set
            {
                if(m_isPointerOver != value)
                {
                    m_isPointerOver = value;
                    m_highlight.SetActive(value);
                }
            }
        }

        private void Start()
        {
            m_rte = IOC.Resolve<IRTE>();
            m_paletteView = GetComponentInParent<MaterialPaletteViewImpl>();
            m_highlight.SetActive(IsPointerOver);

            m_rte.DragDrop.Drop += OnDrop;
        }

        private void OnDestroy()
        {
            if(m_rte != null && m_rte.DragDrop != null)
            {
                m_rte.DragDrop.Drop -= OnDrop;
            }
        }

        private void OnDrop(PointerEventData pointerEventData)
        {
            if(!IsPointerOver)
            {
                return;
            }

            m_paletteView.CompleteDragDrop(); 
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if(!m_rte.DragDrop.InProgress)
            {
                return;
            }

            if(m_paletteView.CanDrop())
            {
                m_rte.DragDrop.SetCursor(Utils.KnownCursor.DropAllowed);
            }
            else
            {
                m_rte.DragDrop.SetCursor(Utils.KnownCursor.DropNotAllowed);
            }

            IsPointerOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if(m_rte.DragDrop.InProgress)
            {
                m_rte.DragDrop.SetCursor(Utils.KnownCursor.DropNotAllowed);
            }
            
            IsPointerOver = false;
        }

        private Texture2D GetTexture()
        {
            object[] objects = m_rte.DragDrop.DragObjects;
            if (objects == null || objects.Length == 0)
            {
                return null;
            }

            Texture2D texture = objects[0] as Texture2D;
            return texture;
        }

    }

}
