using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.UIControls;
using Battlehub.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.RTBuilder
{
    public class MaterialPaletteView : RuntimeWindow
    {
        private const string DataFolder = "RTBuilderData/";
        private const string PaletteFile = "DefaultMaterialPalette";
        private const string MaterialFile = "Material";

        [SerializeField]
        private Button m_createMaterialButton = null;
        public Button CreateMaterialButton
        {
            get { return m_createMaterialButton; }
        }

        [SerializeField]
        private VirtualizingTreeView m_treeView = null;
        public VirtualizingTreeView TreeView
        {
            get { return m_treeView; }
        }

        [SerializeField]
        private RawImage m_texturePreview = null;
        public RawImage TexturePreview
        {
            get { return m_texturePreview; }
        }

        private Texture m_texture = null;
        public Texture Texture
        {
            get { return m_texture; }
            set
            {
                m_texture = value;
                m_texturePreview.gameObject.SetActive(m_texture != null);
                m_texturePreview.texture = m_texture;
                m_textureEditor.Reload();
                Material material = (Material)m_treeView.SelectedItem;
                if (material != null)
                {
                    material.mainTexture = value;
                }
            }
        }

        public Material SelectedMaterial
        {
            get { return (Material)m_treeView.SelectedItem; }
        }


        [SerializeField]
        private ObjectEditor m_textureEditor = null;

        [SerializeField]
        private Transform m_texturePicker = null;
        public Transform TexturePicker
        {
            get { return m_texturePicker; }
        }

        protected override void AwakeOverride()
        {
            m_texturePicker.gameObject.SetActive(false);
            m_textureEditor.Init(this, this, Strong.PropertyInfo((MaterialPaletteView x) => x.Texture));

            WindowType = RuntimeWindowType.Custom;
            base.AwakeOverride();
        }

        protected virtual void Start()
        {
            if (!GetComponent<MaterialPaletteViewImpl>())
            {
                gameObject.AddComponent<MaterialPaletteViewImpl>();
            }
        }
    }
}

