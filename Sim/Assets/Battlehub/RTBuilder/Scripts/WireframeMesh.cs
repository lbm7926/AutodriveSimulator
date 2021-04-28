using Battlehub.ProBuilderIntegration;
using UnityEngine;
using UnityEngine.Rendering;

namespace Battlehub.RTBuilder
{
    public class WireframeMesh : MonoBehaviour
    {
        private PBMesh m_pbMesh;
        private MeshFilter m_filter;
        private Color m_color;
        private bool m_update;
        public bool IsIndividual
        {
            get;
            set;
        }

        private void Awake()
        {
            m_color = new Color(Random.value, Random.value, Random.value);
            m_filter = GetComponent<MeshFilter>();
            if(!m_filter)
            {
                m_filter = gameObject.AddComponent<MeshFilter>();
            }

            if(!m_filter.sharedMesh)
            {
                m_filter.sharedMesh = new Mesh();
            }

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if(renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }

            renderer.sharedMaterial = PBBuiltinMaterials.LinesMaterial;
            renderer.sharedMaterial.SetColor("_Color", Color.white);
            renderer.sharedMaterial.SetInt("_HandleZTest", (int)CompareFunction.LessEqual);
            renderer.sharedMaterial.SetFloat("_Scale", 0.5f);

            m_pbMesh = GetComponentInParent<PBMesh>();
            m_pbMesh.Selected += OnPBMeshSelected;
            m_pbMesh.Changed += OnPBMeshChanged;
            m_pbMesh.Unselected += OnPBMeshUnselected;
            m_pbMesh.BuildEdgeMesh(m_filter.sharedMesh, m_color, false);
        }

        private void OnDestroy()
        {
            if(m_pbMesh != null)
            {
                m_pbMesh.Selected -= OnPBMeshSelected;
                m_pbMesh.Changed -= OnPBMeshChanged;
                m_pbMesh.Unselected -= OnPBMeshUnselected;
            }
        }

        private void OnPBMeshSelected(bool clear)
        {
            if(clear)
            {
                if (m_filter.sharedMesh != null)
                {
                    Destroy(m_filter.sharedMesh);
                    m_filter.sharedMesh = new Mesh();
                }
            }

            m_update = !clear;
            
        }

        private void OnPBMeshChanged(bool positionsOnly)
        {
            if(m_update)
            {
                m_pbMesh.BuildEdgeMesh(m_filter.sharedMesh, m_color, positionsOnly);
            }
        }

        private void OnPBMeshUnselected()
        {
            m_pbMesh.BuildEdgeMesh(m_filter.sharedMesh, m_color, false);
            m_update = false;
        }
    }

}
