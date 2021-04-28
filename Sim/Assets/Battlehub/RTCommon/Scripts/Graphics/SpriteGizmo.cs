using UnityEngine;

namespace Battlehub.RTCommon
{
    public class SpriteGizmo : MonoBehaviour, IGL
    {
        public Material Material;
        [SerializeField, HideInInspector]
        private SphereCollider m_collider;

        private void Awake()
        {
            if (GLRenderer.Instance == null)
            {
                GameObject glRenderer = new GameObject();
                glRenderer.name = "GLRenderer";
                glRenderer.AddComponent<GLRenderer>();
            }
        }

        private void OnEnable()
        {    
            GLRenderer glRenderer = GLRenderer.Instance;
            if(glRenderer)
            {
                glRenderer.Add(this);
            }

            m_collider = GetComponent<SphereCollider>();

            if(m_collider == null)
            {
                m_collider = gameObject.AddComponent<SphereCollider>();
                m_collider.radius = 0.25f;
            }
            if(m_collider != null)
            {
                if(m_collider.hideFlags == HideFlags.None)
                {
                    m_collider.hideFlags = HideFlags.HideInInspector;
                }
            } 
        }

        private void OnDisable()
        {
            GLRenderer glRenderer = GLRenderer.Instance;
            if (glRenderer)
            {
                glRenderer.Remove(this);
            }

            if(m_collider != null)
            {
                Destroy(m_collider);
                m_collider = null;
            }
        }

        void IGL.Draw(int cullingMask, Camera camera)
        {
            Material.SetPass(0);
            RuntimeGraphics.DrawQuad(transform.localToWorldMatrix);
        }


    }
}

