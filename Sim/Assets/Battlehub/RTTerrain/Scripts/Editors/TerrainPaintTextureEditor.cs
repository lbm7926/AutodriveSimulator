using System;
using UnityEngine;

namespace Battlehub.RTTerrain
{
    public class TerrainPaintTextureEditor : MonoBehaviour
    {
        [SerializeField]
        private TerrainLayerEditor m_terrainLayerEditor = null;

        [SerializeField]
        private TerrainBrushEditor m_terrainBrushEditor = null;

        private TerrainEditor m_terrainEditor;

        private void Awake()
        {
            m_terrainEditor = GetComponentInParent<TerrainEditor>();

            if(m_terrainLayerEditor != null)
            {
                m_terrainLayerEditor.TerrainData = m_terrainEditor.Terrain.terrainData;
                m_terrainLayerEditor.SelectedLayerChanged += OnSelectedLayerChanged;
            }
            
            if(m_terrainBrushEditor != null)
            {
                m_terrainBrushEditor.SelectedBrushChanged += OnSelectedBrushChanged;
                m_terrainBrushEditor.BrushParamsChanged += OnBrushParamsChanged;
            }
        }

        
        private void OnDestroy()
        {
            if(m_terrainLayerEditor != null)
            {
                m_terrainLayerEditor.SelectedLayerChanged -= OnSelectedLayerChanged;
            }

            if (m_terrainBrushEditor != null)
            {
                m_terrainBrushEditor.SelectedBrushChanged -= OnSelectedBrushChanged;
                m_terrainBrushEditor.BrushParamsChanged -= OnBrushParamsChanged;
            }
        }

        private void OnEnable()
        {
            //m_terrainEditor.Projector.gameObject.SetActive(true);
            if (m_terrainBrushEditor.SelectedBrush != null)
            {
                OnSelectedBrushChanged(this, EventArgs.Empty);
            }
            OnBrushParamsChanged(this, EventArgs.Empty);
        }

        private void OnDisable()
        {
            //m_terrainEditor.Projector.gameObject.SetActive(false);
        }

        private void OnSelectedLayerChanged(object sender, EventArgs e)
        {
            InitializeTerrainTextureBrush();
            TerrainTextureBrush brush = (TerrainTextureBrush)m_terrainEditor.Projector.TerrainBrush;
            brush.TerrainLayerIndex = GetTerrainLayerIndex();
        }

        private void OnSelectedBrushChanged(object sender, EventArgs e)
        {
            InitializeTerrainTextureBrush();
            m_terrainEditor.Projector.Brush = m_terrainBrushEditor.SelectedBrush.texture;
        }

        private void OnBrushParamsChanged(object sender, EventArgs e)
        {
            InitializeTerrainTextureBrush();
            m_terrainEditor.Projector.Size = m_terrainBrushEditor.BrushSize;
            m_terrainEditor.Projector.Opacity = m_terrainBrushEditor.BrushOpacity;
        }

        private void InitializeTerrainTextureBrush()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (m_terrainEditor.Projector.TerrainBrush is TerrainTextureBrush)
            {
                return;
            }

            m_terrainEditor.Projector.TerrainBrush = new TerrainTextureBrush()
            {
                TerrainLayerIndex = GetTerrainLayerIndex()
            };
        }

        private int GetTerrainLayerIndex()
        {
            return m_terrainLayerEditor.SelectedLayer != null ? Array.IndexOf(m_terrainLayerEditor.TerrainData.terrainLayers, m_terrainLayerEditor.SelectedLayer) : 0;
        }
    }
}
