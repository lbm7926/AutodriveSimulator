using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using Battlehub.Utils;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Battlehub.RTSL
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(RTSLIgnore))]
    public class RTSLDeps : MonoBehaviour 
    {
        private IAssetDB m_assetDB;
        private ITypeMap m_typeMap;
        private IUnityObjectFactory m_objectFactory;
        private ISerializer m_serializer;
        private IStorage m_storage;
        private IProject m_project;
        private IRuntimeShaderUtil m_shaderUtil;
        private IAssetBundleLoader m_assetBundleLoader;

        protected virtual IAssetBundleLoader AssetBundleLoader
        {
            get
            {
                if(File.Exists(Application.streamingAssetsPath + "/credentials.json"))
                {
                    return new GoogleDriveAssetBundleLoader();
                }
                else
                {
                    return new AssetBundleLoader();
                }
            }
        }

        protected virtual IRuntimeShaderUtil ShaderUtil
        {
            get { return new RuntimeShaderUtil(); }
        }

        protected virtual IAssetDB AssetDB
        {
            get { return new AssetDB(); }
        }

        protected virtual ITypeMap TypeMap
        {
            get { return new TypeMap(); }
        }

        protected virtual IUnityObjectFactory ObjectFactory
        {
            get { return new UnityObjectFactory();  }
        }

        protected virtual ISerializer Serializer
        {
            get { return new ProtobufSerializer(); }
        }

        protected virtual IStorage Storage
        {
            get { return new FileSystemStorage(); }
        }

        protected virtual IProject Project
        {
            get
            {
                Project project = FindObjectOfType<Project>();
                if(project == null)
                {
                    project = gameObject.AddComponent<Project>();
                }
                return project;
            }
        }

        private void Awake()
        {
            if(m_instance != null)
            {
                Debug.LogWarning("AnotherInstance of RTSL exists");
            }
            m_instance = this;

            AwakeOverride();
        }

        protected virtual void AwakeOverride()
        {
            if (gameObject.GetComponent<Dispatcher>() == null)
            {
                gameObject.AddComponent<Dispatcher>();
            }

            m_assetBundleLoader = AssetBundleLoader;
            m_assetDB = AssetDB;
            m_shaderUtil = ShaderUtil;
            m_typeMap = TypeMap;
            m_objectFactory = ObjectFactory;
            m_serializer = Serializer;
            m_storage = Storage;
            m_project = Project;
        }

        private void OnDestroy()
        {
            if(m_instance == this)
            {
                m_instance = null;
            }

            OnDestroyOverride();

            m_assetBundleLoader = null;
            m_shaderUtil = null;
            m_assetDB = null;
            m_typeMap = null;
            m_objectFactory = null;
            m_serializer = null;
            m_storage = null;
            m_project = null;
        }

        protected virtual void OnDestroyOverride()
        {

        }


        private static RTSLDeps m_instance;
        private static RTSLDeps Instance
        {
            get
            {
                if(m_instance == null)
                {
                    RTSLDeps deps = FindObjectOfType<RTSLDeps>();
                    if (deps == null)
                    {
                        GameObject go = new GameObject("RTSL");
                        go.AddComponent<RTSLDeps>();
                        go.transform.SetSiblingIndex(0);
                    }   
                    else
                    {
                        m_instance = deps;
                    }
                }
                return m_instance;
            }    
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if(!Application.isPlaying)
            {
                return;
            }

            RegisterRTSL();
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void RegisterRTSL()
        {
            IOC.RegisterFallback(() => Instance.m_assetBundleLoader);
            IOC.RegisterFallback(() => Instance.m_typeMap);
            IOC.RegisterFallback(() => Instance.m_objectFactory);
            IOC.RegisterFallback(() => Instance.m_serializer);
            IOC.RegisterFallback(() => Instance.m_storage);
            IOC.RegisterFallback(() => Instance.m_assetDB);
            IOC.RegisterFallback<IIDMap>(() => Instance.m_assetDB);
            IOC.RegisterFallback(() => Instance.m_project);
            IOC.RegisterFallback(() => Instance.m_shaderUtil);
        }

        private static void OnSceneUnloaded(Scene arg0)
        {
            m_instance = null;
        }
    }
}

