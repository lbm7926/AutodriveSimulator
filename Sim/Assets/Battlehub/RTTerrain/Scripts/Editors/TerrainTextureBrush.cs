using Battlehub.RTCommon;
using UnityEngine;

namespace Battlehub.RTTerrain
{
    public class TerrainTextureBrush : Brush
    {
        public int TerrainLayerIndex
        {
            get;
            set;
        }

        private float[,,] m_oldAlphamaps;
        private IRTE m_editor;

        public TerrainTextureBrush()
        {
            m_editor = IOC.Resolve<IRTE>();
            Blend = BlendFunction.Add;
        }

        public override void BeginPaint()
        {
            base.BeginPaint();
            m_oldAlphamaps = GetAlphamaps();
        }

        public override void EndPaint()
        {
            base.EndPaint();

            Terrain terrain = Terrain;
            float[,,] oldAlphamaps = m_oldAlphamaps;
            float[,,] newAlphamaps = GetAlphamaps();
            m_oldAlphamaps = null;

            m_editor.Undo.CreateRecord(record =>
            {
                terrain.terrainData.SetAlphamaps(0, 0, newAlphamaps);
                return true;
            },
            record =>
            {
                terrain.terrainData.SetAlphamaps(0, 0, oldAlphamaps);
                return true;
            });
        }

        private float[,,] GetAlphamaps()
        {
            int w = Terrain.terrainData.alphamapWidth;
            int h = Terrain.terrainData.alphamapHeight;
            return Terrain.terrainData.GetAlphamaps(0, 0, w, h);
        }

        public override void Modify(Vector2Int minPos, Vector2Int maxPos, float value)
        {
            float heightMapResoulution = Terrain.terrainData.heightmapResolution;
            int px = Mathf.Max(0, minPos.x);
            int py = Mathf.Max(0, minPos.y);
            float[,,] alphaMaps = Terrain.terrainData.GetAlphamaps(
                px,
                py,
                Mathf.Min((int)heightMapResoulution - 1, maxPos.x) - px,
                Mathf.Min((int)heightMapResoulution - 1, maxPos.y) - py);

            int sizeY = maxPos.y - minPos.y;
            int sizeX = maxPos.x - minPos.x;

            int amapY = alphaMaps.GetLength(0);
            int amapX = alphaMaps.GetLength(1);
            int amapZ = alphaMaps.GetLength(2);

            if (minPos.x > 0)
            {
                minPos.x = 0;
            }
            if (minPos.y > 0)
            {
                minPos.y = 0;
            }

            for (int y = 0; y < amapY; y++)
            {
                for (int x = 0; x < amapX; x++)
                {
                    float u = (x - minPos.x) / (float)(sizeX - 1);
                    float v = (y - minPos.y) / (float)(sizeY - 1);
                    float f = Eval(u, v);

                    alphaMaps[y, x, TerrainLayerIndex] = m_blender(alphaMaps[y, x, TerrainLayerIndex], f * value);
                    for (int z = 0; z < amapZ; z++)
                    {
                        if(z == TerrainLayerIndex)
                        {
                            continue;
                        }

                        alphaMaps[y, x, z] = (1 - alphaMaps[y, x, TerrainLayerIndex]);
                    }

                }
            }

            Terrain.terrainData.SetAlphamaps(px, py, alphaMaps);
        }
    }
}

