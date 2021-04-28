using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Battlehub.ProBuilderIntegration
{
    public class PBTextureMoveTool : PBTextureTool
    {
        private static float k_vector3Magnitude = Vector3.one.magnitude;

        public override void Drag(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            base.Drag(position, rotation, scale);

            Vector3 lp = MatrixInv.MultiplyPoint(position);

            Vector2 delta = new Vector2(-lp.x, -lp.y);

            List<Face> faces = new List<Face>();

            for(int m = 0; m < Meshes.Length; ++m)
            {
                ProBuilderMesh mesh = Meshes[m];
                Vector2[] origins = Origins[m];
                var uvTransforms = UVTransforms[m];
                int[] indexes = Indexes[m];

                Vector2[] textures = mesh.textures.ToArray();

                // Account for object scale
                delta *= k_vector3Magnitude / mesh.transform.lossyScale.magnitude;

                for (int i = 0; i < indexes.Length; ++i)
                {
                    int index = indexes[i];
                    var uvTransform = uvTransforms[i];
                    textures[index] = origins[i] + new Vector2(delta.x / uvTransform.scale.x, delta.y / uvTransform.scale.y);
                }

                mesh.textures = textures;
                mesh.GetFaces(Selection.SelectedFaces[mesh], faces);
               
                mesh.RefreshUV(faces);
                faces.Clear();
            }
        }
    }

}
