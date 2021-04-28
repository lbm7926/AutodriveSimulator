//#define RTSL_COMPILE_TEMPLATES
#if RTSL_COMPILE_TEMPLATES
//<TEMPLATE_USINGS_START>
using ProtoBuf;
using UnityEngine;
//<TEMPLATE_USINGS_END>
#endif

namespace Battlehub.RTSL.Internal
{
    [PersistentTemplate("UnityEngine.Mesh", new[] { "vertices", "subMeshCount", "indexFormat", "triangles" })]
    public class PersistentMesh_RTSL_Template : PersistentSurrogateTemplate
    {
#if RTSL_COMPILE_TEMPLATES
        //<TEMPLATE_BODY_START>    
        [ProtoMember(1)]
        public Vector3[] vertices;

        [ProtoMember(2)]
        public int subMeshCount;

        [ProtoMember(3)]
        public IntArray[] m_tris;

        [ProtoMember(4)]
        public UnityEngine.Rendering.IndexFormat indexFormat;

        [ProtoMember(5)]
        public MeshTopology[] m_topology;

        public override object WriteTo(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            Mesh o = (Mesh)obj;
            o.indexFormat = indexFormat;
            if(vertices != null)
            {
                o.vertices = vertices;
            }
            
            o.subMeshCount = subMeshCount;
            if (m_tris != null)
            {
                if(m_topology != null && m_topology.Length == subMeshCount)
                {
                    for (int i = 0; i < subMeshCount; ++i)
                    {
                        MeshTopology topology = m_topology[i];
                        switch (topology)
                        {
                            case MeshTopology.Points:
                            case MeshTopology.Lines:
                                o.SetIndices(m_tris[i].Array, topology, i);
                                break;
                            case MeshTopology.Triangles:
                                o.SetTriangles(m_tris[i].Array, i);
                                break;
                        }   
                    }
                }
                else
                {
                    for (int i = 0; i < subMeshCount; ++i)
                    {
                        o.SetTriangles(m_tris[i].Array, i);
                    }
                }
                
            }
            return  base.WriteTo(obj); 
        }

        public override void ReadFrom(object obj)
        {
            base.ReadFrom(obj);
            if (obj == null)
            {
                return;
            }
            Mesh o = (Mesh)obj;
            
            indexFormat = o.indexFormat;
            subMeshCount = o.subMeshCount;
            if(o.vertices != null)
            {
                vertices = o.vertices;
            }

            m_tris = new IntArray[subMeshCount];
            m_topology = new MeshTopology[subMeshCount];
            for (int i = 0; i < subMeshCount; ++i)
            {
                MeshTopology topology = o.GetTopology(i);
                m_topology[i] = topology;
                switch (topology)
                {
                    case MeshTopology.Points:
                        m_tris[i] = new IntArray();
                        m_tris[i].Array = o.GetIndices(i);
                        break;
                    case MeshTopology.Lines:
                        m_tris[i] = new IntArray();
                        m_tris[i].Array = o.GetIndices(i);
                        break;
                    case MeshTopology.Triangles:
                        m_tris[i] = new IntArray();
                        m_tris[i].Array = o.GetTriangles(i);
                        break;
                }
            }
        }
        //<TEMPLATE_BODY_END>
#endif
    }
}


