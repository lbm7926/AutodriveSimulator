using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace Battlehub.ProBuilderIntegration
{
    public static class ProBuilderMeshOperationsExt
    {
        public static void Rebuild(this ProBuilderMesh mesh, IList<Vector3> positions, IList<Face> faces, IList<Vector2> textures)
        {
            mesh.Clear();
            mesh.positions = positions;
            mesh.faces = faces;
            mesh.textures = textures;
            mesh.sharedVertices = SharedVertex.GetSharedVerticesWithPositions(positions);
            mesh.ToMesh();
            mesh.Refresh();
        }
    }

    public struct PBFace
    {
        public int[] Indexes;
        public int SubmeshIndex;
        public int TextureGroup;
        public bool IsManualUV;
        public PBAutoUnwrapSettings UnwrapSettings;

        public PBFace(Face face, bool recordUV)
        {
            Indexes = face.indexes.ToArray();
            SubmeshIndex = face.submeshIndex;
            TextureGroup = face.textureGroup;
            if(recordUV)
            {
                IsManualUV = face.manualUV;
                UnwrapSettings = face.uv;
            }
            else
            {
                IsManualUV = face.manualUV;
                UnwrapSettings = null;
            }
        }

        public Face ToFace()
        {
            Face face = new Face(Indexes);
            face.submeshIndex = SubmeshIndex;
            
            if(UnwrapSettings != null)
            {
                face.textureGroup = TextureGroup;
                face.uv = UnwrapSettings;
                face.manualUV = IsManualUV;
            }
            else
            {
                face.manualUV = IsManualUV;
            }
            return face;
        }
    }

    public delegate void PBMeshEvent();
    public delegate void PBMeshEvent<T>(T arg);

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class PBMesh : MonoBehaviour
    {
        public event PBMeshEvent<bool> Selected;
        public event PBMeshEvent Unselected;
        public event PBMeshEvent<bool> Changed;

        private ProBuilderMesh m_pbMesh;
        private MeshFilter m_meshFilter;

        private PBFace[] m_faces;
        public PBFace[] Faces
        {
            get
            {
                m_faces = m_pbMesh.faces.Select(f => new PBFace(f, true)).ToArray();
                return m_faces;
            }
            set { m_faces = value; }
        }

        private Vector3[] m_positions;
        public Vector3[] Positions
        {
            get
            {
                m_positions = m_pbMesh.positions.ToArray();
                return m_positions;
            }
            set { m_positions = value; }
        }

        private Vector2[] m_textures;
        public Vector2[] Textures
        {
            get
            {
                m_textures = m_pbMesh.textures.ToArray();
                return m_textures;
            }
            set { m_textures = value; }
        }

        internal ProBuilderMesh Mesh
        {
            get { return m_pbMesh; }
        }

        private void Awake()
        {
            Init(this, Vector2.one);
        }

        private static void Init(PBMesh mesh, Vector2 scale)
        {
            if(mesh.m_pbMesh != null)
            {
                return;
            }

            mesh.m_meshFilter = mesh.GetComponent<MeshFilter>();
            mesh.m_pbMesh = mesh.GetComponent<ProBuilderMesh>();
            if (mesh.m_pbMesh == null)
            {
                mesh.m_pbMesh = mesh.gameObject.AddComponent<ProBuilderMesh>();
                if (mesh.m_positions != null)
                {
                    Face[] faces = mesh.m_faces.Select(f => f.ToFace()).ToArray();
                    mesh.m_pbMesh.Rebuild(mesh.m_positions, faces, mesh.m_textures);

                    IList<Face> actualFaces = mesh.m_pbMesh.faces;
                    for (int i = 0; i < actualFaces.Count; ++i)
                    {
                        actualFaces[i].submeshIndex = mesh.m_faces[i].SubmeshIndex;
                    }

                    mesh.m_pbMesh.Refresh();
                    mesh.m_pbMesh.ToMesh();
                }
                else
                {
                    ImportMesh(mesh.m_meshFilter, mesh.m_pbMesh, scale);
                }
            }
        }

        private void OnDestroy()
        {
            if(m_pbMesh != null)
            {
                Destroy(m_pbMesh);
            }
        }

        public bool CreateShapeFromPolygon(IList<Vector3> points, float extrude, bool flipNormals)
        {
            ActionResult result = m_pbMesh.CreateShapeFromPolygon(points, extrude, flipNormals);
            RaiseChanged(false);
            return result.ToBool();
        }

        public void Subdivide()
        {
            ConnectElements.Connect(m_pbMesh, m_pbMesh.faces);
            m_pbMesh.Refresh();
            m_pbMesh.ToMesh();

            RaiseChanged(false);
        }

        public void CenterPivot()
        {
            m_pbMesh.CenterPivot(null);

            RaiseChanged(false);
        }

        public void Clear()
        {
            m_pbMesh.Clear();
            m_pbMesh.Refresh();
            m_pbMesh.ToMesh();

            MeshFilter filter = m_pbMesh.GetComponent<MeshFilter>();
            filter.sharedMesh.bounds = new Bounds(Vector3.zero, Vector3.zero);

            RaiseChanged(false);
        }

        public void Refresh()
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            if(filter != null)
            {
                filter.sharedMesh = new Mesh();// filter.mesh;

                m_pbMesh.ToMesh();
                m_pbMesh.Refresh();
            }

            RaiseChanged(false);
        }

        public void RaiseSelected(bool clear)
        {
            if(Selected != null)
            {
                Selected(clear);
            }
        }

        public void RaiseChanged(bool positionsOnly)
        {
            if(Changed != null)
            {
                Changed(positionsOnly);
            }
        }

        public void RaiseUnselected()
        {
            if(Unselected != null)
            {
                Unselected(); 
            }
        }

        public void BuildEdgeMesh(Mesh target, Color color, bool positionsOnly)
        {
            IList<Vector3> positions = m_pbMesh.positions;

            int edgeIndex = 0;
            int edgeCount = 0;
            int faceCount = m_pbMesh.faceCount;

            IList<Face> faces = m_pbMesh.faces;
            for (int i = 0; i < faceCount; i++)
            {
                edgeCount += faces[i].edges.Count;
            }
            edgeCount = System.Math.Min(edgeCount, int.MaxValue / 2 - 1);

            int[] tris;
            Vector3[] vertices;
            if (positionsOnly)
            {
                vertices = target.vertices;
                tris = null;
            }
            else
            {
                tris = new int[edgeCount * 2];
                vertices = new Vector3[edgeCount * 2];
            }

            for (int i = 0; i < faceCount && edgeIndex < edgeCount; i++)
            {
                ReadOnlyCollection<Edge> edges = faces[i].edges;
                for (int n = 0; n < edges.Count && edgeIndex < edgeCount; n++)
                {
                    Edge edge = edges[n];

                    int positionIndex = edgeIndex * 2;

                    vertices[positionIndex + 0] = positions[edge.a];
                    vertices[positionIndex + 1] = positions[edge.b];

                    if (!positionsOnly)
                    {
                        tris[positionIndex + 0] = positionIndex + 0;
                        tris[positionIndex + 1] = positionIndex + 1;
                    }

                    edgeIndex++;
                }
            }

            if (!positionsOnly)
            {
                target.Clear();
                target.name = "EdgeMesh" + target.GetInstanceID();
                target.vertices = vertices.ToArray();
                Color[] colors = new Color[target.vertexCount];
                for (int i = 0; i < colors.Length; ++i)
                {
                    colors[i] = color;
                }
                target.colors = colors;
                target.subMeshCount = 1;
                target.SetIndices(tris, MeshTopology.Lines, 0);
            }
            else
            {
                target.vertices = vertices.ToArray();
            }
        }

        public static PBMesh ProBuilderize(GameObject gameObject, bool hierarchy)
        {
            return ProBuilderize(gameObject, hierarchy, Vector2.one);
        }

        public static PBMesh ProBuilderize(GameObject gameObject, bool hierarchy, Vector2 uvScale)
        {
            bool wasActive = false;
            if(uvScale != Vector2.one)
            {
                wasActive = gameObject.activeSelf;
                gameObject.SetActive(false);
            }
            
            if(hierarchy)
            {
                MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>(true);
                for(int i = 0; i < meshFilters.Length; ++i)
                {
                    if(meshFilters[i].GetComponent<PBMesh>() == null)
                    {
                        PBMesh pbMesh = meshFilters[i].gameObject.AddComponent<PBMesh>();
                        Init(pbMesh, uvScale);
                    }
                }

                if (uvScale != Vector2.one)
                {
                    gameObject.SetActive(wasActive);
                }

                return gameObject.GetComponent<PBMesh>();
            }
            else
            {
                PBMesh mesh = gameObject.GetComponent<PBMesh>();
                if (mesh != null)
                {
                    if (uvScale != Vector2.one)
                    {
                        gameObject.SetActive(wasActive);
                    }
                    return mesh;
                }

                mesh = gameObject.AddComponent<PBMesh>();
                Init(mesh, uvScale);
                if (uvScale != Vector2.one)
                {
                    gameObject.SetActive(wasActive);
                }
                return mesh;
            }
        }

        public static void ImportMesh(ProBuilderMesh mesh)
        {
            MeshFilter filter = mesh.GetComponent<MeshFilter>();
            ImportMesh(filter, mesh, Vector2.one);
        }

        private static void ImportMesh(ProBuilderMesh mesh, Vector2 uvScale)
        {
            MeshFilter filter = mesh.GetComponent<MeshFilter>();
            ImportMesh(filter, mesh, uvScale);
        }

        private static void ImportMesh(MeshFilter filter, ProBuilderMesh mesh, Vector2 uvScale)
        {
            MeshImporter importer = new MeshImporter(mesh);
            Renderer renderer = mesh.GetComponent<Renderer>();
            importer.Import(filter.sharedMesh, renderer.sharedMaterials);

            Dictionary<int, List<Face>> submeshIndexToFace = new Dictionary<int, List<Face>>();
            int submeshCount = filter.sharedMesh.subMeshCount;
            for(int i = 0; i < submeshCount; ++i)
            {
                submeshIndexToFace.Add(i, new List<Face>());
            }

            IList<Face> faces = mesh.faces;
            if(uvScale != Vector2.one)
            {
                AutoUnwrapSettings uv = AutoUnwrapSettings.defaultAutoUnwrapSettings;
                uv.scale = uvScale;
                for (int i = 0; i < mesh.faceCount; ++i)
                {
                    Face face = faces[i];
                    face.uv = uv;
                    submeshIndexToFace[face.submeshIndex].Add(face);
                }
            }
            else
            {
                for (int i = 0; i < mesh.faceCount; ++i)
                {
                    Face face = faces[i];
                    submeshIndexToFace[face.submeshIndex].Add(face);
                }
            }

            filter.sharedMesh = new Mesh();
            mesh.ToMesh();
            mesh.Refresh();

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < submeshCount && i < materials.Length; ++i)
            {
                List<Face> submeshFaces = submeshIndexToFace[i];
                Material material = materials[i];
                
                if (material != null)
                {
                    mesh.SetMaterial(submeshFaces, material);
                }
            }

            mesh.ToMesh();
            mesh.Refresh();
        }

        public bool UvTo3D(Vector2 uv, out Vector3 p3d)
        {
            Mesh mesh = m_meshFilter.sharedMesh;
            int[] tris = mesh.triangles;
            Vector2[] uvs = mesh.uv;
            Vector3[] verts = mesh.vertices;
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector2 u1 = uvs[tris[i]]; // get the triangle UVs
                Vector2 u2 = uvs[tris[i + 1]];
                Vector3 u3 = uvs[tris[i + 2]];
                // calculate triangle area - if zero, skip it
                float a = Area(u1, u2, u3);
                if (a == 0)
                {
                    continue;
                }
                // calculate barycentric coordinates of u1, u2 and u3
                // if anyone is negative, point is outside the triangle: skip it
                float a1 = Area(u2, u3, uv) / a;
                if (a1 < 0)
                {
                    continue;
                }

                float a2 = Area(u3, u1, uv) / a;
                if (a2 < 0)
                {
                    continue;
                }
                float a3 = Area(u1, u2, uv) / a;
                if (a3 < 0)
                {
                    continue;
                }
                // point inside the triangle - find mesh position by interpolation...
                p3d = a1 * verts[tris[i]] + a2 * verts[tris[i + 1]] + a3 * verts[tris[i + 2]];
                // and return it in world coordinates:
                p3d = transform.TransformPoint(p3d);
                return true;
            }
            // point outside any uv triangle: return Vector3.zero
            p3d = Vector3.zero;
            return false;
        }

        // calculate signed triangle area using a kind of "2D cross product":
        private float Area(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            Vector2 v1 = p1 - p3;
            Vector2 v2 = p2 - p3;
            return (v1.x* v2.y - v1.y* v2.x)/2;
        }

        
    }
}
