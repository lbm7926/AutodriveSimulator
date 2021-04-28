using UnityEngine;

namespace Battlehub.RTTerrain
{
    public class TerrainToolState : MonoBehaviour
    {
        [SerializeField]
        public TerrainTool.Interpolation Interpolation = TerrainTool.Interpolation.Bicubic;

        [SerializeField]
        public int Height = 32;
        [SerializeField]
        public int Size = 200;
        [SerializeField]
        public int Spacing = 20;
        
        public float[] Grid;
        public float[] HeightMap;
        public Texture2D CutoutTexture;
    }
}

