
using System;
using UnityEngine;
using UnityEngine.UI;

using Battlehub.RTSL.Interface;
using Battlehub.RTCommon;
using UnityEngine.SceneManagement;
using System.Collections;

using UnityObject = UnityEngine.Object;
using System.Linq;

namespace Battlehub.RTEditor
{
    public class ProjectItemView : MonoBehaviour
    {
        private IProject m_project;

        [SerializeField]
        private Image m_imgPreview = null;

        [SerializeField]
        public Sprite m_folder = null;
        [SerializeField]
        public Sprite m_readonlyFolder = null;
        [SerializeField]
        public Sprite m_scene = null;
        [SerializeField]
        private Sprite m_mesh = null;
        [SerializeField]
        public Sprite m_defaultPrefab = null;
        [SerializeField]
        public Sprite m_none = null;

        private Texture2D m_texture;

        private ProjectItem m_projectItem;
        public ProjectItem ProjectItem
        {
            get { return m_projectItem; }
            set
            {
                if(m_projectItem != null)
                {
                    if(m_projectItem is AssetItem)
                    {
                        AssetItem assetItem = (AssetItem)m_projectItem;
                        assetItem.PreviewDataChanged -= OnPreviewDataChanged;
                    }
                }

                m_projectItem = value;
                UpdateImage();

                if (m_projectItem != null)
                {
                    if (m_projectItem is AssetItem)
                    {
                        AssetItem assetItem = (AssetItem)m_projectItem;
                        assetItem.PreviewDataChanged += OnPreviewDataChanged;
                    }
                }
            }
        }

        private void OnPreviewDataChanged(object sender, System.EventArgs e)
        {
            UpdateImage();
        }

        private void UpdateImage()
        {
            if(m_project == null)
            {
                m_project = IOC.Resolve<IProject>();
                if(m_project == null)
                {
                    Debug.LogError("Project is null");
                }
            }

            if (m_texture != null)
            {
                Destroy(m_texture);
                m_texture = null;
            }
            if (m_projectItem == null)
            {
                m_imgPreview.sprite = null;
                
            }
            else if (m_projectItem is AssetItem)
            {
                AssetItem assetItem = (AssetItem)m_projectItem;
                if (m_project.ToType(assetItem) == typeof(Scene))
                {
                    m_imgPreview.sprite = m_scene;
                }
                else if(m_project.ToType(assetItem) == typeof(Mesh))
                {
                    m_imgPreview.sprite = m_mesh;
                }
                else if(assetItem.Preview == null || assetItem.Preview.PreviewData == null)
                {
                    m_imgPreview.sprite = m_none;
                }
                else if(assetItem.Preview.PreviewData.Length == 0)
                {
                    m_imgPreview.sprite = m_defaultPrefab;
                }
                else
                {
                    m_texture = new Texture2D(1, 1, TextureFormat.ARGB32, true);
                    m_texture.LoadImage(assetItem.Preview.PreviewData);
                    m_imgPreview.sprite = Sprite.Create(m_texture, new Rect(0, 0, m_texture.width, m_texture.height), new Vector2(0.5f, 0.5f));
                }
            }
            else if (m_projectItem.IsFolder)
            {
                if(m_project.IsStatic(m_projectItem))
                {
                    m_imgPreview.sprite = m_readonlyFolder;
                }
                else
                {
                    m_imgPreview.sprite = m_folder;
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private void Awake()
        {
            m_project = IOC.Resolve<IProject>();
        }

        private void OnDestroy()
        {
            if(m_texture != null)
            {
                Destroy(m_texture);
                m_texture = null;
            }
            if (m_projectItem != null)
            {
                if (m_projectItem is AssetItem)
                {
                    AssetItem assetItem = (AssetItem)m_projectItem;
                    assetItem.PreviewDataChanged -= OnPreviewDataChanged;
                }
            }
        }

        public static IEnumerator CoCreatePreviews(ProjectItem[] items, IProject project, IResourcePreviewUtility resourcePreview, Action done = null)
        {
            if (resourcePreview == null)
            {
                if(done != null)
                {
                    done();
                }
                yield break;
            }

            IRTE rte = IOC.Resolve<IRTE>();
            
            if(rte.Selection.activeObject != null)
            {
                long id = project.ToID(rte.Selection.activeObject);
                ProjectItem selectedProjectItem = items.Where(item => item.ItemID == id).FirstOrDefault();
                if(selectedProjectItem != null && selectedProjectItem is AssetItem)
                {
                    AssetItem selectedAssetItem = (AssetItem)selectedProjectItem;
                    selectedAssetItem.Preview = null;
                }
            }

            for (int i = 0; i < items.Length; ++i)
            {
                ImportItem importItem = items[i] as ImportItem;
                if (importItem != null)
                {
                    if (/*importItem.Preview == null &&*/ importItem.Object != null)
                    {
                        importItem.Preview = new Preview
                        {
                            ItemID = importItem.ItemID,
                            PreviewData = resourcePreview.CreatePreviewData(importItem.Object)
                        };
                    }
                }
                else
                {
                    AssetItem assetItem = items[i] as AssetItem;
                    if (assetItem != null)
                    {
                        UnityObject obj = null;
                        if (assetItem.Preview == null)
                        {
                            obj = project.FromID<UnityObject>(assetItem.ItemID);
                        }

                        if (obj != null)
                        {
                            assetItem.Preview = new Preview
                            {
                                ItemID = assetItem.ItemID,
                                PreviewData = resourcePreview.CreatePreviewData(obj)
                            };
                        }
                    }
                }

                if(i % 10 == 0)
                {
                    yield return new WaitForSeconds(0.005f);
                }
            }

            if(done != null)
            {
                done();
            }
        }
    }
}

