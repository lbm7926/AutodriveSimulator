using UnityEngine;

namespace Battlehub.RTTerrain
{
    public class TerrainStampBrush : TerrainBrush
    {
        public float Height
        {
            get;
            set;
        }

        private bool m_allowPaint;

        public TerrainStampBrush()
        {
            Blend = BlendFunction.Add;
        }

        public override void BeginPaint()
        {
            base.BeginPaint();
            m_allowPaint = true;
        }

        public override void EndPaint()
        {
            base.EndPaint();
            m_allowPaint = false;
        }

        public override void Paint(Vector3 pos, float value)
        {
            if(!m_allowPaint)
            {
                return;
            }
            m_allowPaint = false;

            base.Paint(pos, value);
        }

        protected override float ClampValue(float value)
        {
            return Height / Terrain.terrainData.size.y * Mathf.Sign(value);
        }
    }
}

