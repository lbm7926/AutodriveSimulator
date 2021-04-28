using Battlehub.RTCommon;
using Battlehub.RTHandles;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Battlehub.RTEditor
{
    [DefaultExecutionOrder(-100)]
    public class RTEDeps : MonoBehaviour
    {
        private IRuntimeConsole m_console;
        private IResourcePreviewUtility m_resourcePreview;
        private IRTEAppearance m_rteAppearance;
        private IRuntimeHandlesComponent m_runtimeHandlesComponent;
        private IRuntimeEditor m_rte;
        private IWindowManager m_windowManager;
        private IGameObjectCmd m_gameObjectCmd;
        private IEditCmd m_editCmd;
        private IContextMenu m_contextMenu;
        private IEditorsMap m_editorsMap;

        protected virtual IResourcePreviewUtility ResourcePreview
        {
            get
            {
                IResourcePreviewUtility resourcePreviewUtility = FindObjectOfType<ResourcePreviewUtility>();
                if (resourcePreviewUtility == null)
                {
                    resourcePreviewUtility = gameObject.AddComponent<ResourcePreviewUtility>();
                }
                return resourcePreviewUtility;
            }
        }

        protected virtual IRTEAppearance RTEAppearance
        {
            get
            {
                IRTEAppearance rteAppearance = FindObjectOfType<RTEAppearance>();
                if (rteAppearance == null)
                {
                    rteAppearance = gameObject.AddComponent<RTEAppearance>();
                }
                return rteAppearance;
            }
        }

        protected virtual IRuntimeHandlesComponent RuntimeHandlesComponent
        {
            get
            {
                IRuntimeHandlesComponent runtimeHandles = FindObjectOfType<RuntimeHandlesComponent>();
                if(runtimeHandles == null)
                {
                    runtimeHandles = gameObject.AddComponent<RuntimeHandlesComponent>();
                }
                return runtimeHandles;
            }
        }

        protected virtual IRuntimeEditor RTE
        {
            get
            {
                IRuntimeEditor rte = FindObjectOfType<RuntimeEditor>();
                if (rte == null)
                {
                    rte = gameObject.AddComponent<RuntimeEditor>();
                }
                return rte;
            }
        }

        protected virtual IWindowManager WindowManager
        {
            get
            {
                IWindowManager windowManager = FindObjectOfType<WindowManager>();
                if (windowManager == null)
                {
                    windowManager = gameObject.AddComponent<WindowManager>();
                }
                return windowManager;
            }
        }

        protected virtual IRuntimeConsole RuntimeConsole
        {
            get
            {
                IRuntimeConsole console = FindObjectOfType<RuntimeConsole>();
                if (console == null)
                {
                    console = gameObject.AddComponent<RuntimeConsole>();
                }
                return console;
            }
        }

        protected virtual IGameObjectCmd GameObjectCmd
        {
            get
            {
                return FindObjectOfType<GameObjectCmd>();
            }
        }

        protected virtual IEditCmd EditCmd
        {
            get
            {
                return FindObjectOfType<EditCmd>();
            }
        }

        protected virtual IContextMenu ContextMenu
        {
            get
            {
                return FindObjectOfType<ContextMenu>();
            }
        }

        protected virtual IEditorsMap EditorsMap
        {
            get
            {
                return new EditorsMap();
            }
        }


        private void Awake()
        {
            if (m_instance != null)
            {
                Debug.LogWarning("AnotherInstance of RTEDeps exists");
            }
            m_instance = this;
            AwakeOverride();
        }

        protected virtual void AwakeOverride()
        {
            m_rte = RTE;
            IOC.Register<IRTE>(m_rte);
            IOC.Register(m_rte);

            m_resourcePreview = ResourcePreview;
            m_rteAppearance = RTEAppearance;
            m_windowManager = WindowManager;
            m_console = RuntimeConsole;
            m_gameObjectCmd = GameObjectCmd;
            m_editCmd = EditCmd;
            m_contextMenu = ContextMenu;
            m_runtimeHandlesComponent = RuntimeHandlesComponent;
            m_editorsMap = EditorsMap;
        }

        private void OnDestroy()
        {
            if (m_instance == this)
            {
                m_instance = null;
            }

            OnDestroyOverride();

            IOC.Unregister<IRTE>(m_rte);
            IOC.Unregister(m_rte);

            m_resourcePreview = null;
            m_rteAppearance = null;
            m_windowManager = null;
            m_console = null;
            m_gameObjectCmd = null;
            m_editCmd = null;
            m_contextMenu = null;
            m_runtimeHandlesComponent = null;
            m_editorsMap = null;
        }

        protected virtual void OnDestroyOverride()
        {

        }

        private static RTEDeps m_instance;
        private static RTEDeps Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = FindObjectOfType<RTEDeps>();
                    if(m_instance == null)
                    {
                        GameObject go = new GameObject("RTEDeps");
                        go.AddComponent<RTEDeps>();
                    }
                }
                return m_instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            IOC.RegisterFallback(() => Instance.m_console);
            IOC.RegisterFallback(() => Instance.m_resourcePreview);
            IOC.RegisterFallback(() => Instance.m_rteAppearance);
            IOC.RegisterFallback(() => Instance.m_windowManager);
            IOC.RegisterFallback(() => Instance.m_gameObjectCmd);
            IOC.RegisterFallback(() => Instance.m_editCmd);
            IOC.RegisterFallback(() => Instance.m_contextMenu);
            IOC.RegisterFallback(() => Instance.m_runtimeHandlesComponent);
            IOC.RegisterFallback(() => Instance.m_editorsMap);
        }

        private static void OnSceneUnloaded(Scene arg0)
        {
            m_instance = null;
        }
    }
}

