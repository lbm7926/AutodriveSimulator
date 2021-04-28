using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.Utils;
using UnityEngine;

namespace Battlehub.RTTerrain
{
    public class TerrainStampEditor : TerrainRaiseOrLowerEditor
    {
        [SerializeField]
        private RangeEditor m_heightEditor = null;

        private TerrainStampBrush m_stampBrush;

        protected float m_height;
        public virtual float Height
        {
            get { return m_height;}
            set
            {
                if(m_height != value)
                {
                    m_height = value;
                    if(m_stampBrush != null)
                    {
                        m_stampBrush.Height = value;
                    }
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();

            if(m_heightEditor != null)
            {
                m_heightEditor.Min = 0;
                m_heightEditor.Max = TerrainEditor.Terrain.terrainData.size.y;
                m_height = m_heightEditor.Max / 3;
                m_heightEditor.Init(this, this, Strong.PropertyInfo((TerrainStampEditor x) => x.Height));
            }
        }

        protected override void InitializeTerrainBrush()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (TerrainEditor.Projector.TerrainBrush is TerrainStampBrush)
            {
                return;
            }
            TerrainEditor.Projector.TerrainBrush = CreateBrush();
        }

        protected override Brush CreateBrush()
        {
            m_stampBrush = new TerrainStampBrush();
            m_stampBrush.Height = Height;
            return m_stampBrush;
        }

    }
}


