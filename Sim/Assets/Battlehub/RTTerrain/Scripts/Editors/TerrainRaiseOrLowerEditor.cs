using UnityEngine;

namespace Battlehub.RTTerrain
{
    public class TerrainRaiseOrLowerEditor : MonoBehaviour
    {
        [SerializeField]
        private TerrainBrushEditor m_terrainBrushEditor = null;
        private TerrainBrush m_terrainBrush;
        private TerrainEditor m_terrainEditor;
        protected TerrainEditor TerrainEditor
        {
            get { return m_terrainEditor; }
        }
     
        protected virtual void Awake()
        {
            m_terrainEditor = GetComponentInParent<TerrainEditor>();
                       
            if (m_terrainBrushEditor != null)
            {
                m_terrainBrushEditor.SelectedBrushChanged += OnSelectedBrushChanged;
                m_terrainBrushEditor.BrushParamsChanged += OnBrushParamsChanged;
            }
        }        

        protected virtual void OnDestroy()
        {
            if (m_terrainBrushEditor != null)
            {
                m_terrainBrushEditor.SelectedBrushChanged -= OnSelectedBrushChanged;
                m_terrainBrushEditor.BrushParamsChanged -= OnBrushParamsChanged;
            }
        }

        protected virtual void OnEnable()
        {
            //m_terrainEditor.Projector.gameObject.SetActive(true);
            if (m_terrainBrushEditor.SelectedBrush != null)
            {
                OnSelectedBrushChanged(this, System.EventArgs.Empty);
            }
            OnBrushParamsChanged(this, System.EventArgs.Empty);
        }

        protected virtual void OnDisable()
        {
            //m_terrainEditor.Projector.gameObject.SetActive(false);
        }

        protected virtual void OnSelectedBrushChanged(object sender, System.EventArgs e)
        {
            InitializeTerrainBrush();
            m_terrainEditor.Projector.Brush = m_terrainBrushEditor.SelectedBrush.texture;
        }

        protected virtual void OnBrushParamsChanged(object sender, System.EventArgs e)
        {
            InitializeTerrainBrush();
            m_terrainEditor.Projector.Size = m_terrainBrushEditor.BrushSize;
            m_terrainEditor.Projector.Opacity = m_terrainBrushEditor.BrushOpacity;
        }

        protected virtual void InitializeTerrainBrush()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }
            if (m_terrainEditor.Projector.TerrainBrush != null && m_terrainBrush != null && m_terrainEditor.Projector.TerrainBrush == m_terrainBrush)
            {
                return;
            }

            m_terrainEditor.Projector.TerrainBrush = CreateBrush();
        }

        protected virtual Brush CreateBrush()
        {
            return m_terrainBrush = new TerrainBrush();
        }
    }
}

