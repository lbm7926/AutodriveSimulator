using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.UIControls.DockPanels
{
    public class TabPanelScroller : MonoBehaviour
    {
        [SerializeField]
        private RepeatButton m_left = null;

        [SerializeField]
        private RepeatButton m_right = null;

        [SerializeField]
        private RectTransform m_viewport;

        [SerializeField]
        private HorizontalLayoutGroup m_content = null;

        [SerializeField]
        private float m_sensitivity = 500;


        private float ViewportLeft
        {
            get { return m_viewport.localPosition.x; }
        }

        private float ViewportRight
        {
            get { return m_viewport.localPosition.x + m_viewport.rect.width; }
        }

        private float ContentLeft
        {
            get { return m_content.transform.localPosition.x; }
            set
            {
                Vector3 pos = m_content.transform.localPosition;
                pos.x = value;
                m_content.transform.localPosition = pos;
            }
        }

        private float ContentRight
        {
            get { return m_content.transform.localPosition.x + ContentSize; }
            set
            {
                Vector3 pos = m_content.transform.localPosition;
                pos.x = value - ContentSize;
                m_content.transform.localPosition = pos;
            }
        }

        private float ContentSize
        {
            get { return m_content.transform.childCount * (m_tabSize + m_content.spacing); }
        }


        private float m_tabSize;

        private Region m_region;

        private TransformChildrenChangeListener m_transformChildrenChangeListener;

        private bool m_updateButtonsState;

        private void Awake()
        {
            m_region = GetComponentInParent<Region>();
            LayoutElement layoutElement = m_region.TabPrefab.GetComponent<LayoutElement>();

            m_tabSize = layoutElement.minWidth;
            m_viewport = GetComponent<RectTransform>();
            m_transformChildrenChangeListener = m_content.gameObject.AddComponent<TransformChildrenChangeListener>();
            m_transformChildrenChangeListener.TransformChildrenChanged += UpdateButtonsState;
        }

        private void Start()
        {
            m_updateButtonsState = true;
        }

        private void OnDestroy()
        {
       
            if (m_transformChildrenChangeListener != null)
            {
                m_transformChildrenChangeListener.TransformChildrenChanged -= UpdateButtonsState;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            m_updateButtonsState = true;
        }

        private void UpdateButtonsState()
        {
            if (ContentRight < ViewportRight && ContentLeft < ViewportLeft)
            {
                ContentRight = ViewportRight;
                if (ContentLeft > ViewportLeft)
                {
                    ContentLeft = ViewportLeft;
                }
            }

            if (m_viewport.rect.width < ContentSize)
            {
                if (ContentLeft < ViewportLeft)
                {
                    m_left.gameObject.SetActive(true);
                }
                else
                {
                    DisableLeft();
                }

                if (ContentRight > ViewportRight)
                {
                    m_right.gameObject.SetActive(true);
                }
                else
                {
                    DisableRight();
                }
            }
            else
            {
                DisableRight();
                DisableLeft();
            }
        }

        private void DisableRight()
        {
            if (m_right.IsPressed)
            {
                m_right.OnPointerUp(null);
            }
            m_right.gameObject.SetActive(false);
        }

        private void DisableLeft()
        {
            if (m_left.IsPressed)
            {
                m_left.OnPointerUp(null);
            }
            m_left.gameObject.SetActive(false);
        }

        private void Update()
        {
            if(m_updateButtonsState)
            {
                UpdateButtonsState();
                m_updateButtonsState = false;
            }

            if (m_right.IsPressed)
            {
                ContentLeft -= Time.deltaTime * m_sensitivity;
                if (ContentLeft < ViewportLeft)
                {
                    m_left.gameObject.SetActive(true);
                }

                if (ContentRight <= ViewportRight)
                {
                    ContentRight = ViewportRight;
                    DisableRight();
                }
            }
            else if (m_left.IsPressed)
            {
                ContentLeft += Time.deltaTime * m_sensitivity;
                if (ContentRight > ViewportRight)
                {
                    m_right.gameObject.SetActive(true);
                }

                if (ContentLeft >= ViewportLeft)
                {
                    ContentLeft = ViewportLeft;
                    DisableLeft();
                }
            }
        }

        public void ScrollToRight()
        {
            if (m_viewport.rect.width < ContentSize)
            {
                ContentRight = ViewportRight;
                DisableRight();

                UpdateButtonsState();
            }
        }

        public void ScrollToLeft()
        {
            ContentLeft = ViewportLeft;
            DisableLeft();

            UpdateButtonsState();
        }
    }
}

