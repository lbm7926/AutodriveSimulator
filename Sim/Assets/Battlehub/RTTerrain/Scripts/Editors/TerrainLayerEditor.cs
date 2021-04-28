using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.UIControls;
using Battlehub.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.RTTerrain
{
    public class TerrainLayerEditor : MonoBehaviour
    {
        public event EventHandler SelectedLayerChanged;

        [SerializeField]
        private VirtualizingTreeView m_layersList = null;

        [SerializeField]
        private Button m_createLayer = null;

        [SerializeField]
        private Button m_replaceLayer = null;

        [SerializeField]
        private Button m_removeLayer = null;

        [SerializeField]
        private CanvasGroup m_tileEditorGroup = null;

        [SerializeField]
        private Vector2Editor m_tileSizeEditor = null;

        [SerializeField]
        private Vector2Editor m_tileOffsetEditor = null;

        [SerializeField]
        private string m_selectTextureWindow = RuntimeWindowType.SelectObject.ToString();
        public string SelecteTextureWindowName
        {
            get { return m_selectTextureWindow; }
            set { m_selectTextureWindow = value; }
        }

        public TerrainData TerrainData
        {
            get;
            set;
        }

        public TerrainLayer SelectedLayer
        {
            get { return (TerrainLayer)m_layersList.SelectedItem; }
        }

        public Vector2 TileSize
        {
            get
            {
                if(SelectedLayer == null)
                {
                    return Vector2.zero;
                }
                return SelectedLayer.tileSize;
            }
            set
            {
                if(SelectedLayer != null)
                {
                    SelectedLayer.tileSize = value;
                }
            }
        }

        public Vector2 TileOffset
        {
            get
            {
                if (SelectedLayer == null)
                {
                    return Vector2.zero;
                }
                return SelectedLayer.tileOffset;
            }
            set
            {
                if (SelectedLayer != null)
                {
                    SelectedLayer.tileOffset = value;
                }
            }
        }

        private void Awake()
        {
            if (m_createLayer != null) m_createLayer.onClick.AddListener(OnCreateLayer);
            if (m_replaceLayer != null) m_replaceLayer.onClick.AddListener(OnReplaceLayer);
            if (m_removeLayer != null) m_removeLayer.onClick.AddListener(OnRemoveLayer);

            if (m_layersList != null)
            {
                m_layersList.SelectionChanged += OnLayersSelectionChanged;
                m_layersList.ItemDataBinding += OnLayersDataBinding;
                m_layersList.CanDrag = false;
                m_layersList.CanEdit = false;
                m_layersList.CanRemove = false;
                m_layersList.CanReorder = false;
                m_layersList.CanReparent = false;
                m_layersList.CanSelectAll = false;
            }

            if (m_tileSizeEditor != null) m_tileSizeEditor.Init(this, this, Strong.PropertyInfo((TerrainLayerEditor x) => x.TileSize));
            if (m_tileOffsetEditor != null) m_tileOffsetEditor.Init(this, this, Strong.PropertyInfo((TerrainLayerEditor x) => x.TileOffset));
        }

        private void Start()
        {
            if(m_layersList != null)
            {
                m_layersList.Items = TerrainData.terrainLayers;
            }

            UpdateVisualState();
        }

        private void OnDestroy()
        {
            if(m_layersList != null)
            {
                m_layersList.SelectionChanged -= OnLayersSelectionChanged;
                m_layersList.ItemDataBinding -= OnLayersDataBinding;
            }

            if(m_createLayer != null) m_createLayer.onClick.RemoveListener(OnCreateLayer);
            if(m_replaceLayer != null) m_replaceLayer.onClick.RemoveListener(OnReplaceLayer);
            if(m_removeLayer != null) m_removeLayer.onClick.RemoveListener(OnRemoveLayer);
        }

        private void OnLayersDataBinding(object sender, VirtualizingTreeViewItemDataBindingArgs e)
        {
            TerrainLayer layer = (TerrainLayer)e.Item;
            RawImage image = e.ItemPresenter.GetComponentInChildren<RawImage>();
            if(image != null)
            {
                image.texture = layer.diffuseTexture;
            }
        }

        private void OnLayersSelectionChanged(object sender, SelectionChangedArgs e)
        {
            UpdateVisualState();

            if (SelectedLayerChanged != null)
            {
                SelectedLayerChanged(this, EventArgs.Empty);
            }
        }

        private void UpdateVisualState()
        {
            if (m_replaceLayer != null)
            {
                m_replaceLayer.interactable = m_layersList.SelectedItem != null;
            }

            if (m_removeLayer != null)
            {
                m_removeLayer.interactable = m_layersList.SelectedItem != null;
            }

            if(m_tileEditorGroup != null)
            {
                m_tileEditorGroup.interactable = m_layersList.SelectedItem != null;
            }
        }

        private void OnCreateLayer()
        {
            SelectTexture(true);
        }

        private void OnReplaceLayer()
        {
            SelectTexture(false);
        }

        private void OnRemoveLayer()
        {
            List<TerrainLayer> layers = TerrainData.terrainLayers.ToList();
            TerrainLayer selectedLayer = (TerrainLayer)m_layersList.SelectedItem;
            layers.Remove(selectedLayer);
            TerrainData.terrainLayers = layers.ToArray();
            m_layersList.RemoveSelectedItems();
        }

        private void SelectTexture(bool create)
        {
            ISelectObjectDialog objectSelector = null;
            Transform dialogTransform = IOC.Resolve<IWindowManager>().CreateDialogWindow(m_selectTextureWindow, "Select Texture", 
                 (sender, args) =>
                 {
                     if(!objectSelector.IsNoneSelected)
                     {
                         OnTextureSelected((Texture2D)objectSelector.SelectedObject, create);
                     }
                 });
            objectSelector = IOC.Resolve<ISelectObjectDialog>();// dialogTransform.GetComponentInChildren<SelectObjectDialog>();
            objectSelector.ObjectType = typeof(Texture2D);
        }

        private void OnTextureSelected(Texture2D texture, bool create)
        {
            TerrainLayer layer;
            if(create)
            {
                layer = new TerrainLayer() { name = "TerrainLayer" };
                layer.diffuseTexture = texture;

                List<TerrainLayer> layers = TerrainData.terrainLayers.ToList();
                layers.Add(layer);
                TerrainData.terrainLayers = layers.ToArray();

                m_layersList.Add(layer);
            }
            else
            {
                layer = (TerrainLayer)m_layersList.SelectedItem;
                layer.diffuseTexture = texture;

                m_layersList.DataBindItem(layer);
            }
        }
    }
}

