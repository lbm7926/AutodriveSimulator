using Battlehub.RTCommon;
using UnityEngine;

namespace Battlehub.RTTerrain
{
    [DefaultExecutionOrder(1)]
    public class TerrainProjector : MonoBehaviour
    {
        private Projector m_projector;
        private IRTE m_editor;
        private Brush m_terrainBrush;
        private Vector2 m_prevPos;
        private Vector3 m_position;
        private Terrain m_terrain;

        public Brush TerrainBrush
        {
            get { return m_terrainBrush; }
            set { m_terrainBrush = value; }
        }

        public Texture2D Brush
        {
            get { return (Texture2D)m_projector.material.GetTexture("_ShadowTex"); }
            set
            {
                m_projector.material.SetTexture("_ShadowTex", value);
                m_terrainBrush.Texture = value;
            }
        }

        public float Size
        {
            get { return m_projector.orthographicSize / 2; }
            set
            {
                m_projector.orthographicSize = value * 2;
                m_terrainBrush.Radius = value * 2;
            }
        }

        public float Opacity
        {
            get;
            set;
        }

        private void Awake()
        {
            m_projector = GetComponent<Projector>();
            m_projector.enabled = false;
            m_editor = IOC.Resolve<IRTE>();
        }

        private void Update()
        {
            if(m_terrainBrush.IsPainting)
            {
                if(m_editor.Input.GetPointerUp(0))
                {
                    m_terrainBrush.EndPaint();
                }
            }

            if(m_editor.ActiveWindow == null || m_editor.ActiveWindow.WindowType != RuntimeWindowType.Scene || !m_editor.ActiveWindow.IsPointerOver)
            {
                if(m_projector.enabled)
                {
                    m_projector.enabled = false;
                }
                
                return;
            }

            if(!m_projector.enabled)
            {
                m_projector.enabled = true;
            }

            if(m_prevPos != m_editor.ActiveWindow.Pointer.ScreenPoint)
            {
                m_prevPos = m_editor.ActiveWindow.Pointer.ScreenPoint;
                m_terrain = null;

                Ray ray = m_editor.ActiveWindow.Pointer;
                RaycastHit[] hits = Physics.RaycastAll(ray);
                foreach (RaycastHit hit in hits)
                {
                    if (!(hit.collider is TerrainCollider))
                    {
                        continue;
                    }

                    m_terrain = hit.collider.GetComponent<Terrain>();
                    m_position = hit.point;
                    if (m_terrain != null)
                    {
                        break;
                    }
                }

                if (m_terrain == null)
                {
                    return;
                }

                m_position.y += 500;
                transform.position = m_position;
            }
           
            if(m_terrain != null)
            {
                if(m_editor.Input.GetPointerDown(0))
                {
                    m_terrainBrush.Terrain = m_terrain;
                    m_terrainBrush.BeginPaint();
                }
                else if (m_editor.Input.GetPointer(0))
                {
                    m_terrainBrush.Paint(m_terrain.transform.InverseTransformPoint(m_position), (m_editor.Input.GetKey(KeyCode.LeftShift) ? -Time.deltaTime : Time.deltaTime) * Opacity);
                }
            }
        }
    }

}
