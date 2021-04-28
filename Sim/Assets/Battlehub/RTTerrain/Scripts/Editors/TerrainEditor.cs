using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.RTHandles;
using Battlehub.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.RTTerrain
{
    public class TerrainEditor : MonoBehaviour
    {
        public enum EditorTypes
        {
            Selection_Handles = 0,
            Raise_Or_Lower_Terrain = 1,
            Paint_Texture = 2,
            Stamp_Terrain = 3,
            Set_Height = 4,
            Smooth_Height = 5
        }

        [SerializeField]
        private GameObject m_header = null;
        [SerializeField]
        private Toggle m_enableToggle = null;
        [SerializeField]
        private EnumEditor m_editorSelector = null;
        [SerializeField]
        private GameObject[] m_editors = null;
        [SerializeField]
        private TerrainProjector m_terrainProjectorPrefab = null;
        public TerrainProjector Projector
        {
            get;
            private set;
        }

        private IRTE m_editor;
        private IWindowManager m_wm;
        private bool m_wasEnabled;

        private EditorTypes m_editorType = EditorTypes.Raise_Or_Lower_Terrain;
        public EditorTypes EditorType
        {
            get { return m_editorType; }
            set
            {
                if(m_editorType != value)
                {
                    if (value == EditorTypes.Selection_Handles)
                    {
                        m_wasEnabled = m_enableToggle.isOn;
                        m_enableToggle.isOn = false;
                        m_header.SetActive(false);
                    }
                    else
                    {
                        if(m_editorType == EditorTypes.Selection_Handles)
                        {
                            m_header.SetActive(true);
                            m_enableToggle.isOn = m_wasEnabled;
                        }
                    }

                    Projector.gameObject.SetActive(m_enableToggle.isOn);            
                    m_editorType = value;

                    foreach (GameObject disableEditor in m_editors)
                    {
                        disableEditor.SetActive(false);
                    }
                  
                    GameObject editor = m_editors[(int)m_editorType];
                    if(editor)
                    {
                        editor.SetActive(true);
                    }
                }
            }
        }

        public Terrain Terrain
        {
            get;
            set;
        }

        private void Awake()
        {
            m_editor = IOC.Resolve<IRTE>();
            m_wm = IOC.Resolve<IWindowManager>();

            Projector = Instantiate(m_terrainProjectorPrefab, m_editor.Root);
            Projector.gameObject.SetActive(false);

            if (m_editorSelector != null)
            {
                m_editorSelector.Init(this, this, Strong.PropertyInfo((TerrainEditor x) => x.EditorType), null, "Tool");
            }

            if(m_enableToggle != null)
            {
                m_enableToggle.onValueChanged.AddListener(OnEnableValueChanged);
                if(m_enableToggle.isOn)
                {
                    OnEnableValueChanged(m_enableToggle.isOn);
                }
            }
            
        }

        private void OnDestroy()
        {
            if(m_enableToggle != null)
            {
                m_enableToggle.onValueChanged.RemoveListener(OnEnableValueChanged);
            }

            if(Projector != null)
            {
                Destroy(Projector);
            }

            EnableStandardTools();
        }

        private void OnWindowCreated(Transform obj)
        {
            RuntimeWindow window = obj.GetComponent<RuntimeWindow>();
            if (window != null && window.WindowType == RuntimeWindowType.Scene)
            {
                IRuntimeSceneComponent scene = window.IOCContainer.Resolve<IRuntimeSceneComponent>();
                scene.IsBoxSelectionEnabled = false;
            }
        }

        private void OnEnableValueChanged(bool value)
        {
            if(value)
            {
                foreach (RuntimeWindow window in m_editor.Windows)
                {
                    if (window.WindowType == RuntimeWindowType.Scene)
                    {
                        IRuntimeSceneComponent scene = window.IOCContainer.Resolve<IRuntimeSceneComponent>();
                        if(scene != null)
                        {
                            scene.CanSelect = false;
                            scene.CanSelectAll = false;
                            scene.IsPositionHandleEnabled = false;
                            scene.IsRotationHandleEnabled = false;
                            scene.IsScaleHandleEnabled = false;
                            scene.IsBoxSelectionEnabled = false;
                        }
                    }
                }
                m_wm.WindowCreated += OnWindowCreated;
                Projector.gameObject.SetActive(true);
            }
            else
            {
                EnableStandardTools();

                if (m_wm != null)
                {
                    m_wm.WindowCreated -= OnWindowCreated;
                }

                Projector.gameObject.SetActive(false);
            }
        }

        private void EnableStandardTools()
        {
            if (m_editor != null)
            {
                foreach (RuntimeWindow window in m_editor.Windows)
                {
                    if (window.WindowType == RuntimeWindowType.Scene)
                    {
                        IRuntimeSceneComponent scene = window.IOCContainer.Resolve<IRuntimeSceneComponent>();
                        if(scene != null)
                        {
                            scene.CanSelect = true;
                            scene.CanSelectAll = true;
                            scene.IsPositionHandleEnabled = true;
                            scene.IsRotationHandleEnabled = true;
                            scene.IsScaleHandleEnabled = true;
                            scene.IsBoxSelectionEnabled = true;

                        }
                
                    }
                }
            }
        }
    }
}
