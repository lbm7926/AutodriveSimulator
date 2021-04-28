using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Battlehub.Spline3
{
    public class SplineRenderer : MonoBehaviour
    {
        [SerializeField]
        private float m_step = 0.05f;
        [SerializeField]
        private float m_normalLength = 0.0f;
        [SerializeField]
        private Color m_lineColor = Color.green;
        [SerializeField]
        private Color m_controlPointColor = Color.gray;
        
        private BaseSpline m_spline;
        private int m_segCount;
        private int m_perSegCount;
        private int[] m_indexes;
        private MeshFilter m_lineMeshFilter;
        private MeshFilter m_normalMeshFilter;
        private MeshFilter m_pointMeshFilter;

        private Renderer m_lineRenderer;
        private Renderer m_normalRenderer;
        private Renderer m_pointRenderer;

        private static Material m_lineMaterial;
        private static Material m_normalMaterial;
        private static Material m_controlPointMaterial;

        private void Start()
        {
            if(m_lineMaterial == null)
            {
                m_lineMaterial = new Material(Shader.Find("Hidden/Spline3/LineBillboard"));
                m_lineMaterial.SetFloat("_Scale", 0.9f);
                m_lineMaterial.SetColor("_Color", m_lineColor);
                m_lineMaterial.SetInt("_HandleZTest", (int)CompareFunction.Always);
            }
            if(m_normalMaterial == null)
            {
                m_normalMaterial = new Material(Shader.Find("Hidden/Spline3/LineBillboard"));
                m_normalMaterial.SetFloat("_Scale", 0.9f);
                m_normalMaterial.SetColor("_Color", m_lineColor);
                m_normalMaterial.SetInt("_HandleZTest", (int)CompareFunction.Always);
            }

            if(m_controlPointMaterial == null)
            {
                m_controlPointMaterial = new Material(Shader.Find("Hidden/Spline3/PointBillboard"));
                m_controlPointMaterial.SetFloat("_Scale", 4.5f);
                m_controlPointMaterial.SetColor("_Color", m_controlPointColor);
                m_controlPointMaterial.SetInt("_HandleZTest", (int)CompareFunction.Always);
            }

            m_spline = GetComponent<BaseSpline>();

            GameObject lineGo = new GameObject();
            lineGo.name = "Line";
            lineGo.transform.SetParent(transform, false);

            m_lineMeshFilter = lineGo.AddComponent<MeshFilter>();
            m_lineMeshFilter.sharedMesh = new Mesh();
            m_lineMeshFilter.sharedMesh.MarkDynamic();

            m_lineRenderer = lineGo.AddComponent<MeshRenderer>();
            m_lineRenderer.sharedMaterial = m_lineMaterial;

            GameObject normalGo = new GameObject();
            normalGo.name = "Normals";
            normalGo.transform.SetParent(transform, false);

            m_normalMeshFilter = normalGo.AddComponent<MeshFilter>();
            m_normalMeshFilter.sharedMesh = new Mesh();
            m_normalMeshFilter.sharedMesh.MarkDynamic();

            m_normalRenderer = normalGo.AddComponent<MeshRenderer>();
            m_normalRenderer.sharedMaterial = m_normalMaterial;

            GameObject pointsGo = new GameObject();
            pointsGo.name = "Points";
            pointsGo.transform.SetParent(transform, false);

            m_pointMeshFilter = pointsGo.AddComponent<MeshFilter>();
            m_pointMeshFilter.sharedMesh = new Mesh();
            m_pointMeshFilter.sharedMesh.MarkDynamic();

            m_pointRenderer = pointsGo.AddComponent<MeshRenderer>();
            m_pointRenderer.sharedMaterial = m_controlPointMaterial;

            Refresh();
        }

        private void OnDestroy()
        {
            if (m_lineRenderer != null)
            {
                Destroy(m_lineRenderer.gameObject);
            }

            if (m_pointRenderer != null)
            {
                Destroy(m_pointRenderer.gameObject);
            }

            if (m_normalRenderer != null)
            {
                Destroy(m_normalRenderer.gameObject);
            }
        }

        private void OnEnable()
        {
            if(m_lineRenderer != null)
            {
                m_lineRenderer.enabled = true;
            }
            
            if(m_pointRenderer != null)
            {
                m_pointRenderer.enabled = true;
            }
            
            if(m_normalRenderer != null)
            {
                m_normalRenderer.enabled = true;
            }
        }

        private void OnDisable()
        {
            if(m_lineRenderer != null)
            {
                m_lineRenderer.enabled = false;
            }
            
            if(m_pointRenderer != null)
            {
                m_pointRenderer.enabled = false;
            }
            
            if(m_normalRenderer != null)
            {
                m_normalRenderer.enabled = false;
            }
        }

        public void Refresh(bool positionsOnly = false)
        {
            if(m_spline == null)
            {
                return;
            }

            BuildLineMesh(m_lineMeshFilter.sharedMesh, m_lineColor, positionsOnly);
            if(m_normalLength > 0)
            {
                m_normalMeshFilter.gameObject.SetActive(true);
                BuildNormalMesh(m_normalMeshFilter.sharedMesh, m_lineColor, positionsOnly);
            }
            else
            {
                m_normalMeshFilter.gameObject.SetActive(false);
            }
            
            BuildPointsMesh(m_pointMeshFilter.sharedMesh, m_controlPointColor);
        }

        private void BuildLineMesh(Mesh target, Color color, bool positionsOnly)
        {
            int segCount = m_spline.SegmentsCount;
            int perSegCount = Mathf.RoundToInt(1.0f / Mathf.Max(0.0001f, m_step));
            
            if(m_segCount != segCount || m_perSegCount != perSegCount)
            {
                m_segCount = segCount;
                m_perSegCount = perSegCount;
                positionsOnly = false;
            }
            
            if(positionsOnly)
            {
                Vector3[] vertices = target.vertices;
                UpdateLineVertices(segCount, perSegCount, vertices);
                target.vertices = vertices;
                target.RecalculateBounds();
            }
            else
            {
                Vector3[] vertices = new Vector3[segCount * (perSegCount + 1)];
                int[] indexes = new int[(vertices.Length - (m_spline.IsLooping ? 0 : 1)) * 2];
                UpdateLineVertices(segCount, perSegCount, vertices);
                int index = 0;
                for(int i = 0; i < indexes.Length; i+= 2)
                {
                    indexes[i] = index;
                    indexes[i + 1] = (index + 1) % vertices.Length;
                    index++;
                }

                target.Clear();
                target.subMeshCount = 1;

                target.name = "SplineMesh" + target.GetInstanceID();
                Color[] colors = new Color[target.vertexCount];
                for (int i = 0; i < colors.Length; ++i)
                {
                    colors[i] = color;
                }

                target.vertices = vertices;
                target.SetIndices(indexes, MeshTopology.Lines, 0);
                target.colors = colors;
                target.RecalculateBounds();
            }
        }

        private void UpdateLineVertices(int segCount, int perSegCount, Vector3[] vertices)
        {
            for (int segIndex = 0; segIndex < segCount; segIndex++)
            {
                for (int offset = 0; offset <= perSegCount; offset++)
                {
                    vertices[segIndex * (perSegCount + 1) + offset] = m_spline.GetLocalPosition(segIndex, (float)offset / perSegCount);
                }
            }
            vertices[vertices.Length - 1] = m_spline.GetLocalPosition(segCount - 1, 1.0f);
        }

        private void BuildNormalMesh(Mesh target, Color color, bool positionsOnly)
        {
            int segCount = m_spline.SegmentsCount;
            int perSegCount = Mathf.RoundToInt(1.0f / Mathf.Max(0.0001f, m_step));

            if (m_segCount != segCount || m_perSegCount != perSegCount)
            {
                m_segCount = segCount;
                m_perSegCount = perSegCount;
                positionsOnly = false;
            }

            if (positionsOnly)
            {
                Vector3[] vertices = target.vertices;
                UpdateNormalVertices(segCount, perSegCount, vertices);
                target.vertices = vertices;
                target.RecalculateBounds();
            }
            else
            {
                Vector3[] vertices = new Vector3[segCount * (perSegCount + 1) * 2];
                int[] indexes = new int[vertices.Length];
                UpdateNormalVertices(segCount, perSegCount, vertices);
                for (int i = 0; i < indexes.Length; i++)
                {
                    indexes[i] = i;
                }

                target.Clear();
                target.subMeshCount = 1;

                target.name = "SplineMesh" + target.GetInstanceID();
                Color[] colors = new Color[target.vertexCount];
                for (int i = 0; i < colors.Length; ++i)
                {
                    colors[i] = color;
                }

                target.vertices = vertices;
                target.SetIndices(indexes, MeshTopology.Lines, 0);
                target.colors = colors;
                target.RecalculateBounds();
            }
        }

        private void UpdateNormalVertices(int segCount, int perSegCount, Vector3[] vertices)
        {
            for (int segIndex = 0; segIndex < segCount; segIndex++)
            {
                for (int offset = 0; offset <= perSegCount; offset++)
                {
                    Vector3 position = m_spline.GetLocalPosition(segIndex, (float)offset / perSegCount);
                    Vector3 tangent = m_spline.GetLocalTangent(segIndex, (float)offset / perSegCount).normalized;
                    int index = (segIndex * (perSegCount + 1) + offset) * 2;
                    vertices[index] = position;
                    vertices[index + 1] = position + Vector3.Cross(tangent, Vector3.up) * m_normalLength;
                }
            }

            Vector3 lastPosition = m_spline.GetLocalPosition(segCount - 1, 1.0f);
            Vector3 lastTangent = m_spline.GetLocalTangent(segCount - 1, 1.0f).normalized;
            vertices[vertices.Length - 2] = lastPosition;
            vertices[vertices.Length - 1] = lastPosition + Vector3.Cross(lastTangent, Vector3.up) * m_normalLength;
        }

        private void BuildPointsMesh(Mesh target, Color color)
        {
            Vector3[] vertices = m_spline.LocalControlPoints.ToArray();
            if(m_indexes == null)
            {
                m_indexes = new int[0];
            }
            if (m_indexes.Length != vertices.Length)
            {
                System.Array.Resize(ref m_indexes, vertices.Length);
                for(int i = 0; i < m_indexes.Length; ++i)
                {
                    m_indexes[i] = i;
                }

                target.Clear();
                target.subMeshCount = 1;

                Color[] colors = new Color[vertices.Length];
                for (int i = 0; i < colors.Length; ++i)
                {
                    colors[i] = color;
                }

                target.vertices = vertices;
                target.SetIndices(m_indexes, MeshTopology.Points, 0);
                target.colors = colors;
                target.RecalculateBounds();
                
            }
            else
            {
                target.vertices = vertices;
                target.RecalculateBounds();
            }
        }
    }
}

