using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.UIControls.DockPanels
{
    public class TabPreview : MonoBehaviour
    {
        [SerializeField]
        private Image m_img = null;

        [SerializeField]
        private TextMeshProUGUI m_text = null;

        [SerializeField]
        private RectTransform m_contentPart = null;

        [SerializeField]
        private RectTransform m_rt = null;

        [SerializeField]
        private Button m_closeButton = null;

        public Sprite Icon
        {
            get { return m_img.sprite; }
            set
            {
                m_img.sprite = value;
                m_img.gameObject.SetActive(value != null);
            }
        }

        public string Text
        {
            get { return m_text.text; }
            set { m_text.text = value; }
        }

        public bool IsContentActive
        {
            get { return m_contentPart.gameObject.activeSelf; }
            set { m_contentPart.gameObject.SetActive(value); }
        }

        private float m_maxWidth;
        public float MaxWidth
        {
            set { m_maxWidth = value; }
        }

        public Vector2 HeaderSize
        {
            get
            {
                return m_rt.rect.size;
            }
        }

        public Vector2 Size
        {
            set
            {
                m_contentPart.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value.x);
                m_contentPart.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value.y);
                m_rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(value.x, m_maxWidth));
            }
        }

        public bool IsCloseButtonVisible
        {
            get
            {
                if (m_closeButton != null)
                {
                    return m_closeButton.gameObject.activeSelf;
                }
                return false;
            }
            set
            {
                if (m_closeButton != null)
                {
                    m_closeButton.gameObject.SetActive(value);
                }
            }
        }


        private void Awake()
        {
            if(m_rt == null)
            {
                m_rt = (RectTransform)transform;
            }

            IsContentActive = false;
        }

    }
}

