using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Battlehub.ProBuilderIntegration
{
    public enum MeshEditorSelectionMode
    {
        Add,
        Substract,
        Difference
    }

    public class MeshEditorState
    {
        internal readonly Dictionary<ProBuilderMesh, MeshState> State = new Dictionary<ProBuilderMesh, MeshState>();
    }

    internal class MeshState
    {
        public readonly IList<Vector3> Positions;
        public readonly IList<PBFace> Faces;
        public readonly IList<Vector2> Textures;

        public MeshState(IList<Vector3> positions, IList<Face> faces, IList<Vector2> textures, bool recordUV)
        {
            Positions = positions;
            Faces = faces.Select(f => new PBFace(f, recordUV)).ToList();
            Textures = textures;
        }
    }

    public static class ProBuilderExt
    {
        public static void GetFaces(this ProBuilderMesh mesh, IList<int> faceIndexes, IList<Face> faces)
        {
            IList<Face> allFaces = mesh.faces;
            for(int i = 0; i < faceIndexes.Count; ++i)
            {
                Face face = allFaces[faceIndexes[i]];
                faces.Add(face);
            }
        }
    }

    public class MeshSelection
    {
        internal Dictionary<ProBuilderMesh, IList<int>> SelectedFaces = new Dictionary<ProBuilderMesh, IList<int>>();
        internal Dictionary<ProBuilderMesh, IList<int>> UnselectedFaces = new Dictionary<ProBuilderMesh, IList<int>>();

        internal Dictionary<ProBuilderMesh, IList<Edge>> SelectedEdges = new Dictionary<ProBuilderMesh, IList<Edge>>();
        internal Dictionary<ProBuilderMesh, IList<Edge>> UnselectedEdges = new Dictionary<ProBuilderMesh, IList<Edge>>();

        internal Dictionary<ProBuilderMesh, IList<int>> SelectedIndices = new Dictionary<ProBuilderMesh, IList<int>>();
        internal Dictionary<ProBuilderMesh, IList<int>> UnselectedIndices = new Dictionary<ProBuilderMesh, IList<int>>();

        public bool HasFaces
        {
            get { return SelectedFaces.Count != 0 || UnselectedFaces.Count != 0; }
        }

        public bool HasEdges
        {
            get { return SelectedEdges.Count != 0 || UnselectedEdges.Count != 0; }
        }

        public bool HasVertices
        {
            get { return SelectedIndices.Count != 0 || UnselectedIndices.Count != 0; }
        }

        public MeshSelection()
        {

        }

        public MeshSelection(MeshSelection selection)
        {
            SelectedFaces = selection.SelectedFaces.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            UnselectedFaces = selection.UnselectedFaces.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            SelectedEdges = selection.SelectedEdges.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            UnselectedEdges = selection.UnselectedEdges.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            SelectedIndices = selection.SelectedIndices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            UnselectedIndices = selection.UnselectedIndices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public MeshSelection(params GameObject[] gameObjects)
        {
            AddToSelection(gameObjects, SelectedFaces, SelectedEdges, SelectedIndices);
        }

        public MeshSelection Invert()
        {
            var temp1 = SelectedFaces;
            SelectedFaces = UnselectedFaces;
            UnselectedFaces = temp1;

            var temp2 = SelectedEdges;
            SelectedEdges = UnselectedEdges;
            UnselectedEdges = temp2;

            var temp3 = SelectedIndices;
            SelectedIndices = UnselectedIndices;
            UnselectedIndices = temp3;

            return this;
        }

        private static void AddToSelection(GameObject[] gameObjects, Dictionary<ProBuilderMesh, IList<int>> faces, Dictionary<ProBuilderMesh, IList<Edge>> edges, Dictionary<ProBuilderMesh, IList<int>> indices)
        {
            for (int i = 0; i < gameObjects.Length; ++i)
            {
                GameObject go = gameObjects[i];
                ProBuilderMesh mesh = go.GetComponent<ProBuilderMesh>();
                if (mesh != null)
                {
                    if (!faces.ContainsKey(mesh))
                    {
                        faces.Add(mesh, new List<int>());
                    }

                    if(!edges.ContainsKey(mesh))
                    {
                        edges.Add(mesh, new List<Edge>());
                    }

                    if (!indices.ContainsKey(mesh))
                    {
                        indices.Add(mesh, new List<int>());
                    }
                }
            }
        }

        public void FacesToVertices(bool invert)
        {
            SelectedIndices.Clear();
            UnselectedIndices.Clear();

            foreach(KeyValuePair<ProBuilderMesh, IList<int>> kvp in invert ? UnselectedFaces : SelectedFaces)
            {
                ProBuilderMesh mesh;
                List<int> indices;
                GetCoindicentIndices(kvp, out mesh, out indices);

                SelectedIndices.Add(mesh, indices);
            }

            foreach(KeyValuePair<ProBuilderMesh, IList<int>> kvp in invert ? SelectedFaces : UnselectedFaces)
            {
                ProBuilderMesh mesh;
                List<int> indices;
                GetCoindicentIndices(kvp, out mesh, out indices);

                UnselectedIndices.Add(mesh, indices);
            }
        }

        public void VerticesToFaces(bool invert)
        {
            SelectedFaces.Clear();
            UnselectedFaces.Clear();

            foreach (KeyValuePair<ProBuilderMesh, IList<int>> kvp in invert ? UnselectedIndices : SelectedIndices)
            {
                ProBuilderMesh mesh = kvp.Key;
                HashSet<int> indicesHs = new HashSet<int>(mesh.GetCoincidentVertices(kvp.Value));
                List<int> faces = GetFaces(mesh, indicesHs);

                if (faces.Count > 0)
                {
                    SelectedFaces.Add(mesh, faces);
                }
            }

            foreach (KeyValuePair<ProBuilderMesh, IList<int>> kvp in invert ? SelectedIndices : UnselectedIndices)
            {
                ProBuilderMesh mesh = kvp.Key;
                HashSet<int> indicesHs = new HashSet<int>(mesh.GetCoincidentVertices(kvp.Value));
                List<int> faces = GetFaces(mesh, indicesHs);

                if (faces.Count > 0)
                {
                    UnselectedFaces.Add(mesh, faces);
                }
            }
        }

        public void FacesToEdges(bool invert)
        {
            SelectedEdges.Clear();
            UnselectedEdges.Clear();

            foreach (KeyValuePair<ProBuilderMesh, IList<int>> kvp in invert ? UnselectedFaces : SelectedFaces)
            {
                ProBuilderMesh mesh;
                HashSet<Edge> edgesHs;
                GetEdges(kvp, out mesh, out edgesHs);
                SelectedEdges.Add(mesh, edgesHs.ToArray());
            }

            foreach (KeyValuePair<ProBuilderMesh, IList<int>> kvp in invert ? SelectedFaces : UnselectedFaces)
            {
                ProBuilderMesh mesh;
                HashSet<Edge> edgesHs;
                GetEdges(kvp, out mesh, out edgesHs);
                UnselectedEdges.Add(mesh, edgesHs.ToArray());
            }
        }

        public void EdgesToFaces(bool invert)
        {
            SelectedFaces.Clear();
            UnselectedFaces.Clear();

            foreach (KeyValuePair<ProBuilderMesh, IList<Edge>> kvp in invert ? UnselectedEdges : SelectedEdges)
            {
                ProBuilderMesh mesh = kvp.Key;
                HashSet<Edge> edgesHs = new HashSet<Edge>(kvp.Value);
                List<int> faces = GetFaces(mesh, edgesHs);

                if (faces.Count > 0)
                {
                    SelectedFaces.Add(mesh, faces);
                }
            }

            foreach (KeyValuePair<ProBuilderMesh, IList<Edge>> kvp in invert ? SelectedEdges : UnselectedEdges)
            {
                ProBuilderMesh mesh = kvp.Key;
                HashSet<Edge> edgesHs = new HashSet<Edge>(kvp.Value);
                List<int> faces = GetFaces(mesh, edgesHs);

                if (faces.Count > 0)
                {
                    UnselectedFaces.Add(mesh, faces);
                }
            }
        }

        public void EdgesToVertices(bool invert)
        {
            SelectedIndices.Clear();
            UnselectedIndices.Clear();

            foreach (KeyValuePair<ProBuilderMesh, IList<Edge>> kvp in invert ? UnselectedEdges : SelectedEdges)
            {
                ProBuilderMesh mesh;
                List<int> indices;
                GetCoindicentIndices(kvp, out mesh, out indices);

                SelectedIndices.Add(mesh, indices);
            }

            foreach (KeyValuePair<ProBuilderMesh, IList<Edge>> kvp in invert ? SelectedEdges : UnselectedEdges)
            {
                ProBuilderMesh mesh;
                List<int> indices;
                GetCoindicentIndices(kvp, out mesh, out indices);

                UnselectedIndices.Add(mesh, indices);
            }
        }

        public void VerticesToEdges(bool invert)
        {
            SelectedEdges.Clear();
            UnselectedEdges.Clear();

            foreach (KeyValuePair<ProBuilderMesh, IList<int>> kvp in invert ? UnselectedIndices : SelectedIndices)
            {
                ProBuilderMesh mesh = kvp.Key;
                HashSet<int> indicesHs = new HashSet<int>(mesh.GetCoincidentVertices(kvp.Value));
                List<Edge> edges = GetEdges(mesh, indicesHs);

                if (edges.Count > 0)
                {
                    SelectedEdges.Add(mesh, edges);
                }
            }

            foreach (KeyValuePair<ProBuilderMesh, IList<int>> kvp in invert ? SelectedIndices : UnselectedIndices)
            {
                ProBuilderMesh mesh = kvp.Key;
                HashSet<int> indicesHs = new HashSet<int>(mesh.GetCoincidentVertices(kvp.Value));
                List<Edge> edges = GetEdges(mesh, indicesHs);

                if (edges.Count > 0)
                {
                    UnselectedEdges.Add(mesh, edges);
                }
            }
        }

        private static List<int> GetFaces(ProBuilderMesh mesh, HashSet<int> indicesHs)
        {
            IList<Face> allFaces = mesh.faces;
            List<int> faces = new List<int>();
            for (int i = 0; i < allFaces.Count; ++i)
            {
                Face face = allFaces[i];
                if (face.indexes.All(index => indicesHs.Contains(index)))
                {
                    faces.Add(i);
                }
            }

            return faces;
        }

        private static List<int> GetFaces(ProBuilderMesh mesh, HashSet<Edge> edgesHs)
        {
            IList<Face> allFaces = mesh.faces;
            List<int> faces = new List<int>();
            for (int i = 0; i < allFaces.Count; ++i)
            {
                Face face = allFaces[i];

                if (face.edges.All(index => edgesHs.Contains(index)))
                {
                    faces.Add(i);
                }
            }
            return faces;
        }

        private static List<Edge> GetEdges(ProBuilderMesh mesh, HashSet<int> indicesHs)
        {
            IList<Face> allFaces = mesh.faces;
            HashSet<Edge> edgesHs = new HashSet<Edge>();
            for (int i = 0; i < allFaces.Count; ++i)
            {
                Face face = allFaces[i];
                ReadOnlyCollection<Edge> edges = face.edges;
                for(int e = 0; e < edges.Count; ++e)
                {
                    Edge edge = edges[e];
                    if(!edgesHs.Contains(edge))
                    {
                        if(indicesHs.Contains(edge.a) && indicesHs.Contains(edge.b))
                        {
                            edgesHs.Add(edge);
                        }
                    }
                }
            }

            return edgesHs.ToList();
        }

        private static void GetEdges(KeyValuePair<ProBuilderMesh, IList<int>> kvp, out ProBuilderMesh mesh, out HashSet<Edge> edgesHs)
        {
            mesh = kvp.Key;
            edgesHs = new HashSet<Edge>();
            IList<Face> faces = new List<Face>();
            mesh.GetFaces(kvp.Value, faces);
            for (int i = 0; i < faces.Count; ++i)
            {
                ReadOnlyCollection<Edge> edges = faces[i].edges;
                for (int e = 0; e < edges.Count; ++e)
                {
                    if (!edgesHs.Contains(edges[e]))
                    {
                        edgesHs.Add(edges[e]);
                    }
                }
            }
        }

        private static void GetCoindicentIndices(KeyValuePair<ProBuilderMesh, IList<int>> kvp, out ProBuilderMesh mesh, out List<int> indices)
        {
            mesh = kvp.Key;
            IList<Face> faces = new List<Face>();
            mesh.GetFaces(kvp.Value, faces);
            indices = new List<int>();
            mesh.GetCoincidentVertices(faces, indices);
        }

        private static void GetCoindicentIndices(KeyValuePair<ProBuilderMesh, IList<Edge>> kvp, out ProBuilderMesh mesh, out List<int> indices)
        {
            mesh = kvp.Key;
            IList<Edge> edges = kvp.Value;
            indices = new List<int>();
            mesh.GetCoincidentVertices(edges, indices);
        }

        public void Merge(MeshSelection selection)
        {
            if(HasFaces)
            {
                if (selection.HasEdges)
                {
                    selection.EdgesToFaces(false);
                }
                else if (selection.HasVertices)
                {
                    selection.VerticesToFaces(false);
                }

                foreach (KeyValuePair<ProBuilderMesh, IList<int>> kvp in selection.SelectedFaces)
                {
                    ProBuilderMesh mesh = kvp.Key;
                    IList<int> faces = kvp.Value;

                    if (!SelectedFaces.ContainsKey(mesh))
                    {
                        SelectedFaces.Add(mesh, faces);
                    }
                    else
                    {
                        IList<int> existingFaces = SelectedFaces[mesh].ToList();
                        MergeLists(faces, existingFaces);
                        SelectedFaces[mesh] = existingFaces;
                    }
                }

                foreach (KeyValuePair<ProBuilderMesh, IList<int>> kvp in selection.UnselectedFaces)
                {
                    ProBuilderMesh mesh = kvp.Key;
                    IList<int> faces = kvp.Value;

                    if (!UnselectedFaces.ContainsKey(mesh))
                    {
                        UnselectedFaces.Add(mesh, faces);
                    }
                    else
                    {
                        IList<int> existingFaces = UnselectedFaces[mesh].ToList();
                        MergeLists(faces, existingFaces);
                        UnselectedFaces[mesh] = existingFaces;
                    }
                }
            }
            else if(HasEdges)
            {
                if (selection.HasFaces)
                {
                    selection.FacesToEdges(false);
                }
                else if (selection.HasVertices)
                {
                    selection.VerticesToEdges(false);
                }

                foreach (KeyValuePair<ProBuilderMesh, IList<Edge>> kvp in selection.SelectedEdges)
                {
                    ProBuilderMesh mesh = kvp.Key;
                    IList<Edge> edges = kvp.Value;

                    if (!SelectedEdges.ContainsKey(mesh))
                    {
                        SelectedEdges.Add(mesh, edges);
                    }
                    else
                    {
                        IList<Edge> existingEdges = SelectedEdges[mesh].ToList();
                        MergeLists(edges, existingEdges);
                        SelectedEdges[mesh] = existingEdges;
                    }
                }

                foreach (KeyValuePair<ProBuilderMesh, IList<Edge>> kvp in selection.UnselectedEdges)
                {
                    ProBuilderMesh mesh = kvp.Key;
                    IList<Edge> edges = kvp.Value;

                    if (!UnselectedEdges.ContainsKey(mesh))
                    {
                        UnselectedEdges.Add(mesh, edges);
                    }
                    else
                    {
                        IList<Edge> existingEdges = UnselectedEdges[mesh].ToList();
                        MergeLists(edges, existingEdges);
                        UnselectedEdges[mesh] = existingEdges;
                    }
                }
            }
            else if(HasVertices)
            {
                if (selection.HasFaces)
                {
                    selection.FacesToVertices(false);
                }
                else if (selection.HasEdges)
                {
                    selection.EdgesToVertices(false);
                }

                foreach (KeyValuePair<ProBuilderMesh, IList<int>> kvp in selection.SelectedIndices)
                {
                    ProBuilderMesh mesh = kvp.Key;
                    IList<int> indexes = kvp.Value;

                    if (!SelectedIndices.ContainsKey(mesh))
                    {
                        SelectedIndices.Add(mesh, indexes);
                    }
                    else
                    {
                        IList<int> existingIndexes = SelectedIndices[mesh].ToList();
                        MergeLists(indexes, existingIndexes);
                        SelectedIndices[mesh] = existingIndexes;
                    }
                }

                foreach (KeyValuePair<ProBuilderMesh, IList<int>> kvp in selection.SelectedIndices)
                {
                    ProBuilderMesh mesh = kvp.Key;
                    IList<int> indexes = kvp.Value;

                    if (!UnselectedIndices.ContainsKey(mesh))
                    {
                        UnselectedIndices.Add(mesh, indexes);
                    }
                    else
                    {
                        IList<int> existingIndexes = UnselectedIndices[mesh].ToList();
                        MergeLists(indexes, existingIndexes);
                        UnselectedIndices[mesh] = existingIndexes;
                    }
                }
            }
        }

        private static void MergeLists<T>(IList<T> list, IList<T> existingList)
        {
            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (existingList.Contains(list[i]))
                {
                    list.RemoveAt(i);
                }
            }
            for (int i = 0; i < list.Count; ++i)
            {
                existingList.Add(list[i]);
            }
        }
    }

    public interface IMeshEditor
    {
        int GraphicsLayer
        {
            get;
            set;
        }

        bool HasSelection
        {
            get;
        }

        bool CenterMode
        {
            get;
            set;
        }

        bool GlobalMode
        {
            get;
            set;
        }

        bool UVEditingMode
        {
            get;
            set;
        }

        Vector3 Position
        {
            get;
            set;
        }

        Vector3 Normal
        {
            get;
        }

        Quaternion Rotation
        {
            get;
        }

        GameObject Target
        {
            get;
        }

        void Hover(Camera camera, Vector3 pointer);
        void Extrude(float distance = 0.0f);
        void Delete();
        void Subdivide();
        void Merge();
        MeshSelection SelectHoles();
        void FillHoles();
        

        MeshSelection Select(Camera camera, Vector3 pointer, bool shift, bool ctrl);
        MeshSelection Select(Camera camera, Rect rect, GameObject[] gameObjects, MeshEditorSelectionMode mode);
        MeshSelection Select(Material material);
        MeshSelection Unselect(Material material);

        void ApplySelection(MeshSelection selection);
        void RollbackSelection(MeshSelection selection);

        MeshSelection GetSelection();
        MeshSelection ClearSelection();
        
        MeshEditorState GetState(bool recordUV);
        void SetState(MeshEditorState state);

        void BeginMove();
        void EndMove();

        void BeginRotate(Quaternion initialRotation);
        void Rotate(Quaternion rotation);
        void EndRotate();

        void BeginScale();
        void Scale(Vector3 scale, Quaternion rotation);
        void EndScale();
    }

    public static class IMeshEditorExt
    {
        public static MeshSelection Select(Material material)
        {
            MeshSelection selection = new MeshSelection();
            ProBuilderMesh[] meshes = UnityEngine.Object.FindObjectsOfType<ProBuilderMesh>();
            foreach (ProBuilderMesh mesh in meshes)
            {
                Renderer renderer = mesh.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                int index = Array.IndexOf(materials, material);
                if (index < 0)
                {
                    continue;
                }

                List<int> selectedFaces = new List<int>();
                IList<Face> faces = mesh.faces;
                for (int i = 0; i < faces.Count; ++i)
                {
                    Face face = faces[i];
                    if (face.submeshIndex == index)
                    {
                        selectedFaces.Add(i);
                    }
                }

                if (selectedFaces.Count > 0)
                {
                    selection.SelectedFaces.Add(mesh, selectedFaces);
                }
            }
            return selection;
        }

    }
}


