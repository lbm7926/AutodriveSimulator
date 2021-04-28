using Battlehub.UIControls;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.RTCommon
{
    [DefaultExecutionOrder(-90)]
    public class RenderTextureCamera : MonoBehaviour
    {
        [SerializeField]
        private RectTransform m_outputRoot = null;
        public RectTransform OutputRoot
        {
            get { return m_outputRoot; }
            set { m_outputRoot = value; }
        }

        [SerializeField]
        private Material m_overlayMaterial;
        public Material OverlayMaterial
        {
            get { return m_overlayMaterial; }
            set
            {
                m_overlayMaterial = value;
                if(m_output != null)
                {
                    m_output.material = m_overlayMaterial;
                }
            }
        }
     
        [SerializeField]
        private bool m_allowMSAA = true;
        public bool AllowMSAA
        {
            get { return m_allowMSAA; }
            set
            {
                if(m_output == null || m_camera == null)
                {
                    return;
                }

                int sizeX;
                int sizeY;
                if (m_fullscreen)
                {
                    sizeX = Screen.width;
                    sizeY = Screen.height;
                }
                else
                {
                    Rect rect = m_output.rectTransform.rect;
                    Vector2 size = rect.size * ((m_canvasScaler != null) ? m_canvasScaler.scaleFactor : 1);
                    sizeX = Mathf.RoundToInt(size.x);
                    sizeY = Mathf.RoundToInt(size.y);
                }
                
                RenderTexture texture = m_camera.targetTexture;
                ResizeRenderTexture(sizeX, sizeY, texture);
            }
        }

        [SerializeField]
        private bool m_fullscreen = true;
        public bool Fullscreen
        {
            get { return m_fullscreen; }
            set { m_fullscreen = value; }
        }

        private Camera m_camera;
        private RawImage m_output;
        private RectTransformChangeListener m_rectTransformChangeListener;

        public RectTransform RectTransform
        {
            get { return m_output.rectTransform; }
        }

        private Canvas m_canvas;
        public Canvas Canvas
        {
            get { return m_canvas; }
        }

        private CanvasScaler m_canvasScaler;

        private int m_screenWidth;
        private int m_screenHeight;
        private Rect m_outputRect;
        private Vector3 m_position;

        private void Awake()
        {
            m_camera = GetComponent<Camera>();
            if(!m_fullscreen)
            {
                m_camera.rect = new Rect(0, 0, 1, 1);
            }

            GameObject outputGo = new GameObject(m_camera.name + " Output");
            outputGo.SetActive(false);

            
            m_output = outputGo.AddComponent<RawImage>();
            m_output.raycastTarget = false;
            if(m_overlayMaterial != null)
            {
                m_output.material = m_overlayMaterial;
            }
            
            RectTransform rt = outputGo.GetComponent<RectTransform>();
            rt.SetParent(m_outputRoot, false);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            
            m_canvas = m_outputRoot.GetComponentInParent<Canvas>();
            m_canvasScaler = m_outputRoot.GetComponentInParent<CanvasScaler>();

            ResizeRenderTexture();
            ResizeOutput();

            m_rectTransformChangeListener = outputGo.AddComponent<RectTransformChangeListener>();
            m_rectTransformChangeListener.RectTransformChanged += OnRectTransformChanged;

            outputGo.SetActive(true);
        }


        private void OnDestroy()
        {
            if(m_camera != null && m_camera.targetTexture != null)
            {
                m_camera.targetTexture.Release();
                m_camera.targetTexture = null;
            }

            if (m_rectTransformChangeListener != null)
            {
                m_rectTransformChangeListener.RectTransformChanged -= OnRectTransformChanged;
            }

            if (m_output != null)
            {
                Destroy(m_output.gameObject);
            }
        }

        private void OnRectTransformChanged()
        {
            //ResizeOutput();
            //ResizeRenderTexture();
        }

        private void LateUpdate()
        {
            bool resizeRenderTexture = m_outputRect != m_output.rectTransform.rect || m_screenWidth != Screen.width || m_screenHeight != Screen.height;
            bool resizeOutput = resizeRenderTexture || m_output.rectTransform.position != m_position;

            if (m_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                if (m_output.uvRect != m_camera.rect)
                {
                    resizeOutput = true;
                }
            }

           

            if (resizeRenderTexture)
            {
                ResizeRenderTexture();
            }

            if (resizeOutput)
            {
                ResizeOutput();
            }
        }

        private void ResizeRenderTexture()
        {
            RenderTexture texture = m_camera.targetTexture;

            int sizeX;
            int sizeY;
            if(m_fullscreen)
            {
                sizeX = Screen.width;
                sizeY = Screen.height;
            }
            else
            {
                Rect rect = m_output.rectTransform.rect;

                Vector2 size = rect.size * ((m_canvasScaler != null) ? m_canvasScaler.scaleFactor : 1);
                sizeX = Mathf.RoundToInt(size.x);
                sizeY = Mathf.RoundToInt(size.y);
            }

            ResizeRenderTexture(sizeX, sizeY, texture);
        }

        private void ResizeRenderTexture(int sizeX, int sizeY, RenderTexture texture)
        {
            RenderTexture oldTexture = texture;

            texture = new RenderTexture(Mathf.Max(1, sizeX), Mathf.Max(1, sizeY), 24, RenderTextureFormat.ARGB32);
            texture.name = m_camera.name + " RenderTexture";
            texture.filterMode = FilterMode.Point;
            texture.antiAliasing = m_allowMSAA ? Mathf.Max(1, QualitySettings.antiAliasing) : 1;

            m_camera.targetTexture = texture;
            m_output.texture = texture;

            m_outputRect = m_output.rectTransform.rect;
            m_screenWidth = Screen.width;
            m_screenHeight = Screen.height;


            if (oldTexture != null)
            {
                oldTexture.Release();
            }
        }

        private void ResizeOutput()
        {
            if(m_fullscreen)
            {
                if (m_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    m_output.uvRect = m_camera.rect;
                }
                else
                {
                    Vector2 p0;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(m_outputRoot, Vector2.zero, m_canvas.worldCamera, out p0);

                    Vector2 p1;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(m_outputRoot, new Vector2(Screen.width, Screen.height), m_canvas.worldCamera, out p1);

                    m_output.rectTransform.anchoredPosition = p0;
                    m_output.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Abs(p1.x - p0.x));
                    m_output.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Abs(p1.y - p0.y));
                }
            }

            m_position = m_output.rectTransform.position;
        }
    }

}

