using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Battlehub.ProBuilderIntegration
{
    public class PBTextureRotateTool : PBTextureTool
    {
        public override void Drag(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            base.Drag(position, rotation, scale);

            float angle = rotation.eulerAngles.z;

            List<Face> faces = new List<Face>();
            for (int m = 0; m < Meshes.Length; ++m)
            {
                ProBuilderMesh mesh = Meshes[m];
                Vector2[] origins = Origins[m];
                Vector2[] centers = FaceCenters[m];
                var uvTransforms = UVTransforms[m];
                int[] indexes = Indexes[m];

                Vector2[] textures = mesh.textures.ToArray();

                for (int i = 0; i < indexes.Length; ++i)
                {
                    int index = indexes[i];
                    textures[index] = centers[i] + PBMath.RotateAroundPoint(origins[i] - centers[i], Vector2.zero, uvTransforms[i].rotation + angle);
                }

                mesh.textures = textures;
                mesh.GetFaces(Selection.SelectedFaces[mesh], faces);

                mesh.RefreshUV(faces);
                faces.Clear();
            }
        }

      
    }
}
