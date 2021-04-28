using Battlehub.RTCommon;
using Battlehub.RTCommon.EditorTreeView;
using Battlehub.RTSL.Interface;
using UnityEngine.Battlehub.SL2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;
using Battlehub.RTSL.Battlehub.SL2;

namespace Battlehub.RTSL
{
    /// <summary>
    /// Most important SaveLoad2 funtionality.
    /// </summary>
    public class Project : MonoBehaviour, IProject
    {
        public event ProjectEventHandler NewSceneCreating;
        public event ProjectEventHandler NewSceneCreated;
        public event ProjectEventHandler<ProjectInfo> CreateProjectCompleted;
        public event ProjectEventHandler<ProjectInfo> OpenProjectCompleted;
        public event ProjectEventHandler<string> DeleteProjectCompleted;
        public event ProjectEventHandler<ProjectInfo[]> ListProjectsCompleted;
        public event ProjectEventHandler CloseProjectCompleted;

        public event ProjectEventHandler<ProjectItem[]> GetAssetItemsCompleted;
        public event ProjectEventHandler<object[]> BeginSave;
        public event ProjectEventHandler<AssetItem[], bool> SaveCompleted;
        public event ProjectEventHandler<AssetItem[]> BeginLoad;
        public event ProjectEventHandler<AssetItem[], UnityObject[]> LoadCompleted;
        public event ProjectEventHandler<AssetItem[]> DuplicateCompleted;
        public event ProjectEventHandler UnloadCompleted;
        public event ProjectEventHandler<AssetItem[]> ImportCompleted;
        public event ProjectEventHandler<ProjectItem[]> BeforeDeleteCompleted;
        public event ProjectEventHandler<ProjectItem[]> DeleteCompleted;
        public event ProjectEventHandler<ProjectItem[], ProjectItem[]> MoveCompleted;
        public event ProjectEventHandler<ProjectItem> RenameCompleted;
        public event ProjectEventHandler<ProjectItem> CreateCompleted;
        
        private IStorage m_storage;
        private IAssetDB m_assetDB;
        private ITypeMap m_typeMap;
        private IUnityObjectFactory m_factory;
        
        [SerializeField]
        private AssetLibrariesListAsset m_staticAssetLibraries = null;
        private Dictionary<int, string> m_ordinalToStaticAssetLibrary;
     

        [SerializeField]
        private string m_sceneDepsLibrary = null;

        [SerializeField]
        private string m_builtInLibrary = null;

        private string m_projectPath;
        private ProjectInfo m_projectInfo;
        public ProjectInfo ProjectInfo
        {
            get { return m_projectInfo; }
        }

        private ProjectItem m_root;
        public ProjectItem Root
        {
            get { return m_root; }
        }

        private AssetItem m_loadedScene;
        public AssetItem LoadedScene
        {
            get { return m_loadedScene; }
            set { m_loadedScene = value; }
        }

        /// <summary>
        /// For fast access when resolving dependencies.
        /// </summary>
        private Dictionary<long, AssetItem> m_idToAssetItem = new Dictionary<long, AssetItem>();

        [SerializeField]
        private Transform m_dynamicPrefabsRoot;
        
        private Dictionary<int, AssetBundleInfo> m_ordinalToAssetBundleInfo = new Dictionary<int, AssetBundleInfo>();
        private Dictionary<int, AssetBundle> m_ordinalToAssetBundle = new Dictionary<int, AssetBundle>();

        /// <summary>
        /// only one operation can be active at a time
        /// </summary>
        private bool m_isBusy;
        public bool IsBusy
        {
            get { return m_isBusy; }
            private set
            {
                if (m_isBusy != value)
                {
                    if (value)
                    {
                        m_isBusy = value;
                        Application.logMessageReceived += OnApplicationLogMessageReceived;
                    }
                    else
                    {
                        if(m_actionsQueue.Count > 0)
                        {
                            Action workItem = m_actionsQueue.Dequeue();
                            workItem();
                        }
                        else
                        {
                            m_isBusy = value;
                            Application.logMessageReceived -= OnApplicationLogMessageReceived;
                        }
                    }
                }
            }
        }

        private void OnApplicationLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
            {
                IsBusy = false;
            }
        }

        private readonly Queue<Action> m_actionsQueue = new Queue<Action>();

        public bool IsOpened
        {
            get { return m_projectInfo != null; }
        }

        private IEnumerator CoCallback(Action cb)
        {
            yield return new WaitForEndOfFrame();
            if (cb != null)
            {
                cb();
            }
        }

        private void Awake()
        {
            Awake_Internal();
        }

        public void Awake_Internal()
        {
            if (string.IsNullOrEmpty(m_sceneDepsLibrary))
            {
                m_sceneDepsLibrary = "Scenes/" + SceneManager.GetActiveScene().name + "/SceneAssetLibrary";
            }

            if (string.IsNullOrEmpty(m_builtInLibrary))
            {
                m_builtInLibrary = "BuiltInAssets/BuiltInAssetLibrary";
            }

            m_storage = IOC.Resolve<IStorage>();
            m_assetDB = IOC.Resolve<IAssetDB>();
            m_typeMap = IOC.Resolve<ITypeMap>();
            m_factory = IOC.Resolve<IUnityObjectFactory>();

            if (m_dynamicPrefabsRoot == null)
            {
                GameObject go = new GameObject();
                go.name = "DynamicResourceRoot";
                go.transform.SetParent(transform, false);
                m_dynamicPrefabsRoot = go.transform;
            }
            m_dynamicPrefabsRoot.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            UnloadUnregisterDestroy();
            m_projectPath = null;
            m_projectInfo = null;
            m_actionsQueue.Clear();
        }

        private void UnloadUnregisterDestroy()
        {
            UnityObject[] dynamicResources = m_assetDB.GetDynamicResources();
            m_assetDB.UnloadLibraries();
            m_assetDB.UnregisterSceneObjects();
            m_assetDB.UnregisterDynamicResources();
            foreach (UnityObject dynamicResource in dynamicResources)
            {
                if(dynamicResource is Transform)
                {
                    continue;
                }
                Destroy(dynamicResource);
            }

            m_idToAssetItem = new Dictionary<long, AssetItem>();

            m_ordinalToAssetBundleInfo.Clear();
            foreach (AssetBundle assetBundle in m_ordinalToAssetBundle.Values)
            {
                assetBundle.Unload(true);
            }
            m_ordinalToAssetBundle.Clear();
        }


        public bool IsStatic(ProjectItem projectItem)
        {
            return m_assetDB.IsStaticFolderID(projectItem.ItemID) || m_assetDB.IsStaticResourceID(projectItem.ItemID);
        }

        public bool IsScene(ProjectItem projectItem)
        {
            if (projectItem.IsFolder)
            {
                return false;
            }

            AssetItem assetItem = (AssetItem)projectItem;
            return ToType(assetItem) == typeof(Scene);
        }

        public Type ToType(AssetItem assetItem)
        {
            return m_typeMap.ToType(assetItem.TypeGuid);
        }

        public Guid ToGuid(Type type)
        {
            return m_typeMap.ToGuid(type);
        }

        public long ToID(UnityObject obj)
        {
            return m_assetDB.ToID(obj);
        }

        public AssetItem ToAssetItem(UnityObject obj)
        {
            AssetItem result;
            long id = ToID(obj);
            if(!m_idToAssetItem.TryGetValue(id, out result))
            {
                return null;
            }
            return result;
        }

        public T FromID<T>(long id) where T : UnityObject
        {
            return m_assetDB.FromID<T>(id);
        }

        public string GetExt(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            return GetExt(obj.GetType());
        }

        public string GetExt(Type type)
        {
            if (type == null)
            {
                return null;
            }
            if (type == typeof(Scene))
            {
                return ".rtscene";
            }
            if (type == typeof(GameObject))
            {
                return ".rtprefab";
            }
            if (type == typeof(ScriptableObject))
            {
                return ".rtasset";
            }
            if (type == typeof(Material))
            {
                return ".rtmat";
            }
            if (type == typeof(Mesh))
            {
                return ".rtmesh";
            }
            if (type == typeof(Shader))
            {
                return ".rtshader";
            }
            if (type == typeof(TerrainData))
            {
                return ".rtterdata";
            }
            if(type == typeof(TerrainLayer))
            {
                return ".rtterlayer";
            }
            return ".rt" + type.Name.ToLower().Substring(0, 3);
        }

        public string GetUniqueName(string name, string[] names)
        {
            return PathHelper.GetUniqueName(name, names.ToList());
        }

        public string GetUniqueName(string name, Type type, ProjectItem folder)
        {
            if(folder.Children == null)
            {
                return name;
            }

            string ext = GetExt(type);

            List<string> existingNames = folder.Children.Where(c => !c.IsFolder).Select(c => c.NameExt).ToList();
            return PathHelper.GetUniqueName(name, ext, existingNames);
        }

        public string GetUniquePath(string path, Type type, ProjectItem folder)
        {
            string name = Path.GetFileName(path);
            name = GetUniqueName(name, type, folder);

            path = Path.GetDirectoryName(path).Replace(@"\", "/");
            
            return path + (path.EndsWith("/") ? name : "/" + name);
        }

        public void CreateNewScene()
        {
            if(NewSceneCreating != null)
            {
                NewSceneCreating(new Error(Error.OK));
            }

            GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < rootGameObjects.Length; ++i)
            {
                GameObject rootGO = rootGameObjects[i];
                if (rootGO.GetComponent<RTSLIgnore>() || (rootGO.hideFlags & HideFlags.DontSave) != 0)
                {
                    continue;
                }

                Destroy(rootGO);
            }

            m_loadedScene = null;
            if (NewSceneCreated != null)
            {
                NewSceneCreated(new Error(Error.OK));
            }
        }

        /// <summary>
        /// Create Project
        /// </summary>
        /// <param name="project"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<ProjectInfo> CreateProject(string project, ProjectEventHandler<ProjectInfo> callback = null)
        {
            ProjectAsyncOperation<ProjectInfo> ao = new ProjectAsyncOperation<ProjectInfo>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _CreateProject(project, callback, ao));
            }
            else
            {
                IsBusy = true;
                _CreateProject(project, callback, ao);
            }
            return ao;
        }

        private void _CreateProject(string project, ProjectEventHandler<ProjectInfo> callback, ProjectAsyncOperation<ProjectInfo> ao)
        {
            m_storage.CreateProject(project, (error, projectInfo) =>
            {
                if (callback != null)
                {
                    callback(error, projectInfo);
                }
                if (CreateProjectCompleted != null)
                {
                    CreateProjectCompleted(error, projectInfo);
                }
                ao.Error = error;
                ao.Result = projectInfo;
                ao.IsCompleted = true;

                IsBusy = false;
            });
        }


        /// <summary>
        /// List all projects
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<ProjectInfo[]> GetProjects(ProjectEventHandler<ProjectInfo[]> callback = null)
        {
            ProjectAsyncOperation<ProjectInfo[]> ao = new ProjectAsyncOperation<ProjectInfo[]>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _GetProjects(callback, ao));
            }
            else
            {
                IsBusy = true;
                _GetProjects(callback, ao);
            }
            return ao;
        }

        private void _GetProjects(ProjectEventHandler<ProjectInfo[]> callback, ProjectAsyncOperation<ProjectInfo[]> ao)
        {
            m_storage.GetProjects((error, projects) =>
            {
                if (callback != null)
                {
                    callback(error, projects);
                }
                
                ao.Error = error;
                ao.Result = projects;
                ao.IsCompleted = true;

                if (ListProjectsCompleted != null)
                {
                    ListProjectsCompleted(error, projects);
                }

                IsBusy = false;
            });
        }

        /// <summary>
        /// Delete Project
        /// </summary>
        /// <param name="project"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<string> DeleteProject(string project, ProjectEventHandler<string> callback = null)
        {
            ProjectAsyncOperation<string> pao = new ProjectAsyncOperation<string>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _DeleteProject(project, callback, pao));
            }
            else
            {
                IsBusy = true;
                _DeleteProject(project, callback, pao);
            }
            
            return pao;
        }

        private void _DeleteProject(string project, ProjectEventHandler<string> callback, ProjectAsyncOperation<string> ao)
        {
            if (m_projectInfo != null && project == m_projectInfo.Name)
            {
                CloseProject();        
            }
            m_storage.DeleteProject(project, error =>
            {
                if (callback != null)
                {
                    callback(error, project);
                }
                
                ao.Error = error;
                ao.Result = project;
                ao.IsCompleted = true;

                if (DeleteProjectCompleted != null)
                {
                    DeleteProjectCompleted(error, project);
                }

                IsBusy = false;
            });
        }

        /// <summary>
        /// Close Project
        /// </summary>
        public void CloseProject()
        {
            if(m_projectInfo != null)
            {
                UnloadUnregisterDestroy();
            }
            m_projectInfo = null;
            m_root = null;
            m_projectPath = null;

            CreateNewScene();

            if (CloseProjectCompleted != null)
            {
                CloseProjectCompleted(new Error(Error.OK));
            }
        }

        /// <summary>
        /// Open Project
        /// </summary>
        /// <param name="project"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<ProjectInfo> OpenProject(string project, ProjectEventHandler<ProjectInfo> callback)
        {
            ProjectAsyncOperation<ProjectInfo> ao = new ProjectAsyncOperation<ProjectInfo>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _OpenProject(project, callback, ao));
            }
            else
            {
                
                IsBusy = true;
                _OpenProject(project, callback, ao);
            }
            return ao;
        }

        private void _OpenProject(string project, ProjectEventHandler<ProjectInfo> callback, ProjectAsyncOperation<ProjectInfo> ao)
        {
            if(m_projectPath == project)
            {
                Debug.Assert(m_projectInfo != null);
                RaiseOpenProjectCompleted(callback, ao, new Error(Error.OK), m_projectInfo);
                return;
            }

            if (m_projectInfo != null)
            {
                CreateNewScene();
            }

            //UnloadUnregisterDestroy();
            m_projectInfo = null;
            m_root = null;

            m_storage.GetProject(project, (error, projectInfo, assetBundleInfo) =>
            {
                if (error.HasError)
                {
                    RaiseOpenProjectCompleted(callback, ao, error, projectInfo);
                    return;
                }

                m_ordinalToAssetBundleInfo = assetBundleInfo.ToDictionary(info => info.Ordinal);
                OnOpened(project, projectInfo, ao, callback);
            });
        }

        private void RaiseOpenProjectCompleted(ProjectEventHandler<ProjectInfo> callback, ProjectAsyncOperation<ProjectInfo> ao, Error error, ProjectInfo projectInfo)
        {
            if (callback != null)
            {
                callback(error, projectInfo);
            }

            ao.Result = projectInfo;
            ao.Error = error;
            ao.IsCompleted = true;

            if (OpenProjectCompleted != null)
            {
                OpenProjectCompleted(error, projectInfo);
            }

            IsBusy = false;
        }

        private void OnOpened(string project, ProjectInfo projectInfo, ProjectAsyncOperation<ProjectInfo> ao, ProjectEventHandler<ProjectInfo> callback)
        {
            if (projectInfo == null)
            {
                projectInfo = new ProjectInfo();
            }

            m_projectPath = project;
            m_projectInfo = projectInfo;
            GetProjectTree(project, ao, callback);
        }

        private void GetProjectTree(string project, ProjectAsyncOperation<ProjectInfo> ao, ProjectEventHandler<ProjectInfo> callback)
        {
            m_storage.GetProjectTree(project, (error, rootFolder) =>
            {
                if (error.HasError)
                {
                    if (callback != null)
                    {
                        callback(error, m_projectInfo);
                    }

                    ao.Result = m_projectInfo;
                    ao.Error = error;
                    ao.IsCompleted = true;

                    if (OpenProjectCompleted != null)
                    {
                        OpenProjectCompleted(error, m_projectInfo);
                    }

                    IsBusy = false;
                    return;
                }

                OnGetProjectTreeCompleted(error, rootFolder, ao, callback);
            });
        }

        private void OnGetProjectTreeCompleted(Error error, ProjectItem rootFolder, ProjectAsyncOperation<ProjectInfo> ao, ProjectEventHandler<ProjectInfo> callback)
        {
            m_root = rootFolder;

            AssetItem[] assetItems = m_root.Flatten(true).OfType<AssetItem>().ToArray();
            m_idToAssetItem = assetItems.ToDictionary(item => item.ItemID);
            for (int i = 0; i < assetItems.Length; ++i)
            {
                AssetItem assetItem = assetItems[i];
                if (assetItem.Parts != null)
                {
                    for (int j = 0; j < assetItem.Parts.Length; ++j)
                    {
                        PrefabPart prefabPart = assetItem.Parts[j];
                        if (prefabPart != null)
                        {
                            m_idToAssetItem.Add(prefabPart.PartID, assetItem);
                        }
                    }
                }
            }

            if (callback != null)
            {
                callback(error, m_projectInfo);
            }
            
            ao.Result = m_projectInfo;
            ao.Error = error;
            ao.IsCompleted = true;

            if (OpenProjectCompleted != null)
            {
                OpenProjectCompleted(error, m_projectInfo);
            }

            IsBusy = false;
        }

        /// <summary>
        /// Get Asset Items
        /// </summary>
        /// <param name="assetItems"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<AssetItem[]> GetAssetItems(AssetItem[] assetItems, ProjectEventHandler<AssetItem[]> callback)
        {
            ProjectAsyncOperation<AssetItem[]> ao = new ProjectAsyncOperation<AssetItem[]>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _GetAssetItems(assetItems, callback, ao));
            }
            else
            {
                IsBusy = true;
                _GetAssetItems(assetItems, callback, ao);
            }
            return ao;
        }

        private void _GetAssetItems(AssetItem[] assetItems, ProjectEventHandler<AssetItem[]> callback, ProjectAsyncOperation<AssetItem[]> ao)
        {
            m_storage.GetPreviews(m_projectPath, assetItems.Select(f => f.ToString()).ToArray(), (error, previews) =>
            {
                if (error.HasError)
                {
                    callback(error, new AssetItem[0]);
                }

                AssetItem[] result = assetItems.ToArray();
                for (int i = 0; i < result.Length; ++i)
                {
                    AssetItem assetItem = result[i];
                    assetItem.Preview = previews[i];
                }

                if (callback != null)
                {
                    callback(error, result);
                }

                ao.Error = error;
                ao.Result = result;
                ao.IsCompleted = true;

                IsBusy = false;
            });
        }
            
        /// <summary>
        /// Get Asset Items with previews
        /// </summary>
        /// <param name="folders"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<ProjectItem[]> GetAssetItems(ProjectItem[] folders, ProjectEventHandler<ProjectItem[]> callback)
        {
            ProjectAsyncOperation<ProjectItem[]> ao = new ProjectAsyncOperation<ProjectItem[]>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _GetAssetItems(folders, callback, ao));
            }
            else
            {
                IsBusy = true;
                _GetAssetItems(folders, callback, ao);
            }
            
            return ao;
        }

        private void _GetAssetItems(ProjectItem[] folders, ProjectEventHandler<ProjectItem[]> callback, ProjectAsyncOperation<ProjectItem[]> ao)
        {
            m_storage.GetPreviews(m_projectPath, folders.Select(f => f.ToString()).ToArray(), (error, result) =>
            {
                if (error.HasError)
                {
                    if (callback != null)
                    {
                        callback(error, new AssetItem[0]);
                    }

                    ao.Result = new AssetItem[0];
                    ao.Error = error;
                    ao.IsCompleted = true;
                    
                    if (GetAssetItemsCompleted != null)
                    {
                        GetAssetItemsCompleted(error, new AssetItem[0]);
                    }

                    IsBusy = false;
                    return;
                }
                OnGetPreviewsCompleted(folders, ao, callback, error, result);
            });
        }

        private void OnGetPreviewsCompleted(ProjectItem[] folders, ProjectAsyncOperation<ProjectItem[]> ao, ProjectEventHandler<ProjectItem[]> callback, Error error, Preview[][] result)
        {
            for (int i = 0; i < result.Length; ++i)
            {
                ProjectItem folder = folders[i];
                Preview[] previews = result[i];
                if (previews != null && previews.Length > 0)
                {
                    for (int j = 0; j < previews.Length; ++j)
                    {
                        Preview preview = previews[j];
                        AssetItem assetItem;

                        if (m_idToAssetItem.TryGetValue(preview.ItemID, out assetItem))
                        {
                            if (assetItem.Parent == null)
                            {
                                Debug.LogErrorFormat("asset item {0} parent is null", assetItem.ToString());
                                continue;
                            }

                            if (assetItem.Parent.ItemID != folder.ItemID)
                            {
                                Debug.LogErrorFormat("asset item {0} with wrong parent selected. Expected parent {1}. Actual parent {2}", folder.ToString(), assetItem.Parent.ToString());
                                continue;
                            }

                            assetItem.Preview = preview;
                        }
                        else
                        {
                            Debug.LogWarningFormat("AssetItem with ItemID {0} does not exists", preview.ItemID);
                        }
                    }
                }
            }

            ProjectItem[] projectItems = folders.Where(f => f.Children != null).SelectMany(f => f.Children).ToArray();
            if (callback != null)
            {
                callback(error, projectItems);
            }
            
            ao.Error = error;
            ao.Result = projectItems;
            ao.IsCompleted = true;

            if (GetAssetItemsCompleted != null)
            {
                GetAssetItemsCompleted(error, projectItems);
            }

            IsBusy = false;
        }

        private void PersistentDescriptorsToPrefabPartItems(PersistentDescriptor[] descriptors, List<PrefabPart> prefabParts, bool includeRoot = false)
        {
            if (descriptors == null)
            {
                return;
            }

            for (int i = 0; i < descriptors.Length; ++i)
            {
                PersistentDescriptor descriptor = descriptors[i];

                if (descriptor != null)
                {
                    bool checkPassed = true;
                    Guid typeGuid = Guid.Empty;
                    Type persistentType = m_typeMap.ToType(descriptor.PersistentTypeGuid);
                    if (persistentType == null)
                    {
                        Debug.LogWarningFormat("Unable to resolve type with guid {0}", descriptor.PersistentTypeGuid);
                        checkPassed = false;
                    }
                    else
                    {
                        Type type = m_typeMap.ToUnityType(persistentType);
                        if (type == null)
                        {
                            Debug.LogWarningFormat("Unable to get unity type from persistent type {1}", type.FullName);
                            checkPassed = false;
                        }
                        else
                        {
                            typeGuid = m_typeMap.ToGuid(type);
                            if (typeGuid == Guid.Empty)
                            {
                                Debug.LogWarningFormat("Unable convert type {0} to guid", type.FullName);
                                checkPassed = false;
                            }
                        }
                    }

                    if (checkPassed && includeRoot)
                    {
                        PrefabPart prefabPartItem = new PrefabPart
                        {
                            Name = descriptor.Name,
                            ParentID = descriptor.Parent != null ? descriptor.Parent.PersistentID : m_assetDB.NullID,
                            PartID = descriptor.PersistentID,
                            TypeGuid = typeGuid
                        };

                        prefabParts.Add(prefabPartItem);
                    }

                    PersistentDescriptorsToPrefabPartItems(descriptor.Children, prefabParts, true);
                    PersistentDescriptorsToPrefabPartItems(descriptor.Components, prefabParts, true);
                }
            }
        }

        private void LoadBuiltinLibrary(Action callback)
        {
            if (!m_assetDB.IsLibraryLoaded(AssetLibraryInfo.BUILTIN_FIRST))
            {
                LoadBuiltinLibrary(m_builtInLibrary, AssetLibraryInfo.BUILTIN_FIRST, callback);
            }
            else
            {
                callback();
            }
        }

        private void LoadBuiltinLibrary(string name, int ordinal, Action callback)
        {
            string libraryName = ordinal == AssetLibraryInfo.BUILTIN_FIRST ? name : name + ((ordinal - AssetLibraryInfo.BUILTIN_FIRST) + 1);
            m_assetDB.LoadLibrary(libraryName, ordinal, true, true, done =>
            {
                if (!done)
                {
                    if (ordinal == AssetLibraryInfo.BUILTIN_FIRST)
                    {
                        Debug.LogWarning("Builtin library was not loaded");
                    }
                    callback();
                    return;
                }

                ordinal++;
                LoadBuiltinLibrary(name, ordinal, callback);
            });
        }

        private void LoadLibraryWithSceneDependencies(Action callback)
        {
            LoadBuiltinLibrary(() =>
            {
                if (!m_assetDB.IsLibraryLoaded(AssetLibraryInfo.SCENELIB_FIRST))
                {
                    LoadLibraryWithSceneDependencies(m_sceneDepsLibrary, AssetLibraryInfo.SCENELIB_FIRST, callback);
                }
                else
                {
                    callback();
                }
            });
        }

        private void LoadLibraryWithSceneDependencies(string name, int ordinal, Action callback)
        {
            string libraryName = ordinal == AssetLibraryInfo.SCENELIB_FIRST ? name : name + ((ordinal - AssetLibraryInfo.SCENELIB_FIRST) + 1);
            m_assetDB.LoadLibrary(libraryName, ordinal, true, true, done =>
            {
                if (!done)
                {
                    if(ordinal == AssetLibraryInfo.SCENELIB_FIRST)
                    {
                        Debug.LogWarning("Library with scene dependencies was not loaded");
                    }
                    callback();
                    return;
                }

                ordinal++;
                LoadLibraryWithSceneDependencies(name, ordinal, callback);
            });
        }

        /// <summary>
        /// Get Dependecies from object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<object[]> GetDependencies(object obj, bool exceptMappedObjects, ProjectEventHandler<object[]> callback)
        {
            ProjectAsyncOperation<object[]> ao = new ProjectAsyncOperation<object[]>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _GetDependencies(obj, exceptMappedObjects, callback, ao));
            }
            else
            {
                IsBusy = true;
                _GetDependencies(obj, exceptMappedObjects, callback, ao);
            }
            
            return ao;
        }

        private void _GetDependencies(object obj, bool exceptMappedObject, ProjectEventHandler<object[]> callback, ProjectAsyncOperation<object[]> ao)
        {
            Type objType = obj.GetType();
            Type persistentType = m_typeMap.ToPersistentType(objType);
            if (persistentType == null)
            {
                Debug.LogWarningFormat(string.Format("PersistentClass for {0} does not exist. To create or edit persistent classes click Tools->Runtime SaveLoad->Persistent Classes->Edit.", obj.GetType()), "obj");
                RaiseGetDependenciesCompleted(callback, ao, new object[0]);
                return;
            }

            if (persistentType == typeof(PersistentGameObject))
            {
                persistentType = typeof(PersistentRuntimePrefab);
            }

            LoadLibraryWithSceneDependencies(() =>
            {
                PersistentObject persistentObject = (PersistentObject)Activator.CreateInstance(persistentType);
                GetDepsFromContext ctx = new GetDepsFromContext();
                persistentObject.GetDepsFrom(obj, ctx);

                object[] deps = ctx.Dependencies.ToArray();
                ctx.Dependencies.Clear();

                for(int i = 0; i < deps.Length; ++i)
                {
                    object dep = deps[i];

                    if (dep is GameObject)
                    {
                        continue;
                    }
                    else if(dep is Component)
                    {
                        continue;
                    }
                    else if(dep is UnityObject)
                    {
                        if (exceptMappedObject && m_assetDB.IsMapped((UnityObject)dep))
                        {
                            continue;
                        }
                        ctx.Dependencies.Add(dep);
                    }
                }
                
                Queue<UnityObject> depsQueue = new Queue<UnityObject>(deps.OfType<UnityObject>());

                GetDeepDependencies(depsQueue, exceptMappedObject, ctx);

                object[] dependencies;
                if (exceptMappedObject)
                {
                    dependencies = ctx.Dependencies.Where(d => d is UnityObject && !m_assetDB.IsMapped((UnityObject)d)).ToArray();
                }
                else
                {
                    dependencies = ctx.Dependencies.ToArray();
                }

                RaiseGetDependenciesCompleted(callback, ao, dependencies);
            });
        }

        private void GetDeepDependencies(Queue<UnityObject> depsQueue, bool exceptMappedObject, GetDepsFromContext ctx)
        {
            GetDepsFromContext getDepsCtx = new GetDepsFromContext();
            while (depsQueue.Count > 0)
            {
                UnityObject uo = depsQueue.Dequeue();
                if (!uo)
                {
                    continue;
                }

                if (exceptMappedObject && m_assetDB.IsMapped(uo))
                {
                    continue;
                }

                if (!(uo is GameObject) && !(uo is Component))
                {
                    Type persistentType = m_typeMap.ToPersistentType(uo.GetType());
                    if (persistentType != null)
                    {
                        getDepsCtx.Clear();

                        PersistentObject persistentObject = (PersistentObject)Activator.CreateInstance(persistentType);
                        persistentObject.ReadFrom(uo);
                        persistentObject.GetDepsFrom(uo, getDepsCtx);

                        foreach (UnityObject dep in getDepsCtx.Dependencies)
                        {   
                            if (!ctx.Dependencies.Contains(dep))
                            {
                                if (dep is GameObject)
                                {
                                    continue;
                                }
                                else if (dep is Component)
                                {
                                    continue;
                                }
                                else
                                {
                                    ctx.Dependencies.Add(dep);
                                }
                                
                                depsQueue.Enqueue(dep);
                            }
                        }
                    }
                }
            }
        }

        private void RaiseGetDependenciesCompleted(ProjectEventHandler<object[]> callback, ProjectAsyncOperation<object[]> ao, object[] dependencies)
        {
            Error error = new Error(Error.OK);
            if (callback != null)
            {
                callback(error, dependencies);
            }

            ao.Error = error;
            ao.Result = dependencies;
            ao.IsCompleted = true;

            IsBusy = false;
        }

        public AssetItem[] GetDependantAssetItems(AssetItem[] assetItems)
        {
            HashSet<AssetItem> resultHs = new HashSet<AssetItem>();
            Queue<AssetItem> queue = new Queue<AssetItem>();
            for(int i = 0; i < assetItems.Length; ++i)
            {
                queue.Enqueue(assetItems[i]);
            }

            while(queue.Count > 0)
            {
                AssetItem assetItem = queue.Dequeue();
                if(resultHs.Contains(assetItem))
                {
                    continue;
                }
                resultHs.Add(assetItem);

                foreach (AssetItem item in m_idToAssetItem.Values)
                {
                    if(item.Dependencies != null)
                    {
                        for (int i = 0; i < item.Dependencies.Length; ++i)
                        {
                            if (assetItem.ItemID == item.Dependencies[i])
                            {
                                queue.Enqueue(item);
                                break;
                            }
                        }
                    }
                }

            }

            for(int i = 0; i < assetItems.Length; ++i)
            {
                AssetItem assetItem = assetItems[i];
                resultHs.Remove(assetItem);
            }
            return resultHs.ToArray();
        }

        public ProjectAsyncOperation<AssetItem[]> SaveScene(string name, ProjectEventHandler<AssetItem[]> callback = null)
        {
            return Save(new[] { Root }, new [] { new byte[0] }, new object[] { SceneManager.GetActiveScene() }, new[] { name }, callback);
        }

        /// <summary>
        /// Save
        /// </summary>
        /// <param name="assetItems"></param>
        /// <param name="obj"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<AssetItem[]> Save(AssetItem[] assetItems, object[] obj, ProjectEventHandler<AssetItem[]> callback = null)
        {
            return Save(assetItems, null, null, obj, null, callback);
        }

        /// <summary>
        /// Save
        /// </summary>
        /// <param name="parents"></param>
        /// <param name="previewData"></param>
        /// <param name="obj"></param>
        /// <param name="nameOverrides"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<AssetItem[]> Save(ProjectItem[] parents, byte[][] previewData, object[] obj, string[] nameOverrides, ProjectEventHandler<AssetItem[]> callback = null)
        {
            return Save(null, parents, previewData, obj, nameOverrides, callback);
        }

        private ProjectAsyncOperation<AssetItem[]> Save(AssetItem[] existingAssetItems, ProjectItem[] parents, byte[][] previewData, object[] objects, string[] nameOverrides, ProjectEventHandler<AssetItem[]> callback)
        {
            ProjectAsyncOperation<AssetItem[]> ao = new ProjectAsyncOperation<AssetItem[]>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => { _Save(existingAssetItems, parents, previewData, objects, nameOverrides, callback, ao); });
            }
            else
            {
                IsBusy = true;
                _Save(existingAssetItems, parents, previewData, objects, nameOverrides, callback, ao);
            }

            return ao;
        }

        private void _Save(AssetItem[] existingAssetItems, ProjectItem[] parents, byte[][] previewData, object[] objects, string[] nameOverrides, ProjectEventHandler<AssetItem[]> callback, ProjectAsyncOperation<AssetItem[]> ao)
        {
            if (BeginSave != null)
            {
                BeginSave(new Error(), objects);
            }
            _Save(parents, previewData, objects, nameOverrides, existingAssetItems, callback, ao, () =>
            {
                if(gameObject.activeInHierarchy)
                {
                    if (SaveCompleted != null)
                    {
                        bool isNew = existingAssetItems == null;
                        SaveCompleted(ao.Error, ao.Result, isNew);
                    }
                    IsBusy = false;
                }
                else
                {
                    StartCoroutine(CoCallback(() =>
                    {
                        if (SaveCompleted != null)
                        {
                            bool isNew = existingAssetItems == null;
                            SaveCompleted(ao.Error, ao.Result, isNew);
                        }
                        IsBusy = false;
                    }));
                }
            });
        }

        private void _Save(ProjectItem[] parents, byte[][] previewData, object[] objects, string[] nameOverrides, AssetItem[] existingAssetItems, ProjectEventHandler<AssetItem[]> callback, ProjectAsyncOperation<AssetItem[]> ao, Action done)
        {
            if (m_root == null)
            {
                throw new InvalidOperationException("Project is not opened. Use OpenProject method");
            }

            if (objects == null)
            {
                throw new ArgumentNullException("objects");
            }

            if(existingAssetItems != null)
            {
                if(existingAssetItems.Length != objects.Length)
                {
                    throw new ArgumentException("existingAssetItems.Length != objects.Length", "existingAssetItems");
                }

                for (int i = 0; i < existingAssetItems.Length; ++i)
                {
                    object obj = objects[i];
                    if (!(obj is Scene))
                    {
                        if (existingAssetItems[i].ItemID != m_assetDB.ToID((UnityObject)obj))
                        {
                            throw new ArgumentException("Unable to override item with different object:  assetItem.ItemID != obj persistent id. Either delete the Asset Item, or load the Object before updating.");
                        }
                    }
                }
            }

            LoadLibraryWithSceneDependencies(() => DoSave(ao, parents, previewData, objects, nameOverrides, existingAssetItems, callback, done));
        }

        private void DoSave(ProjectAsyncOperation<AssetItem[]> ao, ProjectItem[] parents, byte[][] previewData, object[] objects, string[] nameOverrides, AssetItem[] existingAssetItems, ProjectEventHandler<AssetItem[]> callback, Action done)
        {
            if (parents == null)
            {
                if(existingAssetItems == null)
                {
                    parents = new ProjectItem[objects.Length];
                    for(int i = 0; i < parents.Length; ++i)
                    {
                        parents[i] = Root;
                    }
                }
            }
            else
            {
                for (int i = 0; i < parents.Length; ++i)
                {
                    if(!parents[i].IsFolder)
                    {
                        throw new ArgumentException("parent is not folder", "parent");
                    }
                }

                if (parents.Length == 0)
                {
                    Error error = new Error(Error.OK);

                    if (callback != null)
                    {
                        callback(error, new AssetItem[0]);
                    }

                    if(!ao.IsCompleted)
                    {
                        ao.Error = error;
                        ao.Result = new AssetItem[0];
                        ao.IsCompleted = true;
                    }

                    done();
                    return;
                }

                int parentIndex = parents.Length - 1;
                ProjectItem lastParent = parents[parentIndex];
                Array.Resize(ref parents, objects.Length);
                for(int i = parentIndex + 1; i < parents.Length; ++i)
                {
                    parents[i] = lastParent;
                }
            }
            

            int assetIdBackup = m_projectInfo.AssetIdentifier;
            int[] rootIds = new int[objects.Length];
            int[] rootOrdinals = new int[objects.Length];

            Dictionary<int, UnityObject> idToObj = new Dictionary<int, UnityObject>();
            for (int o = 0; o < objects.Length; ++o)
            {
                object obj = objects[o];
                Type objType = obj.GetType();
                Type persistentType = m_typeMap.ToPersistentType(objType);
                if (persistentType == null)
                {
                    Debug.LogWarningFormat(string.Format("PersistentClass for {0} does not exist. Tools->Runtime SaveLoad->Persistent Classes->Edit", obj.GetType()), "obj");
                    continue;
                }

                if (persistentType == typeof(PersistentGameObject))
                {
                    persistentType = typeof(PersistentRuntimePrefab);
                }

                AssetItem existingAssetItem = existingAssetItems != null ? existingAssetItems[o] : null;

                int rootOrdinal;
                int rootId;
                if(existingAssetItem == null)
                {
                    if (!GetOrdinalAndId(ref m_projectInfo.AssetIdentifier, out rootOrdinal, out rootId))
                    {
                        OnDynamicIdentifiersExhausted(callback, null, ao, assetIdBackup);
                        done();
                        return;
                    }
                }
                else
                {
                    rootOrdinal = m_assetDB.ToOrdinal(existingAssetItem.ItemID);
                    rootId = m_assetDB.ToInt(existingAssetItem.ItemID);
                }
                
                rootOrdinals[o] = rootOrdinal;
                rootIds[o] = rootId;

                if(existingAssetItem == null || m_assetDB.IsDynamicResourceID(existingAssetItem.ItemID))
                {
                    if(existingAssetItem != null)
                    {
                        /* object with correct identifier already exists. Only new prefab parts should be processed */
                        Debug.Assert(existingAssetItem.ItemID == m_assetDB.ToID((UnityObject)obj));
                    }

                    if (obj is GameObject)
                    {
                        GameObject go = (GameObject)obj;
                        if(!m_assetDB.IsMapped(go))
                        {
                            idToObj.Add(m_assetDB.ToInt(m_assetDB.ToDynamicResourceID(rootOrdinal, rootId)), go);
                        }
                        
                        Transform[] transforms = go.GetComponentsInChildren<Transform>(true);
                        for (int i = 0; i < transforms.Length; ++i)
                        {
                            Transform tf = transforms[i];
                            if (tf.gameObject != go && !m_assetDB.IsMapped(tf.gameObject))
                            {
                                int ordinal;
                                int id;
                                if (!GetOrdinalAndId(ref m_projectInfo.AssetIdentifier, out ordinal, out id))
                                {
                                    OnDynamicIdentifiersExhausted(callback, null, ao, assetIdBackup);
                                    done();
                                    return;
                                }

                                idToObj.Add(m_assetDB.ToInt(m_assetDB.ToDynamicResourceID(ordinal, id)), tf.gameObject);
                            }

                            Component[] components = tf.GetComponents<Component>();
                            for (int j = 0; j < components.Length; ++j)
                            {
                                Component comp = components[j];
                                if(!m_assetDB.IsMapped(comp))
                                {
                                    int ordinal;
                                    int id;
                                    if (!GetOrdinalAndId(ref m_projectInfo.AssetIdentifier, out ordinal, out id))
                                    {
                                        OnDynamicIdentifiersExhausted(callback, null, ao, assetIdBackup);
                                        done();
                                        return;
                                    }

                                    idToObj.Add(m_assetDB.ToInt(m_assetDB.ToDynamicResourceID(ordinal, id)), comp);
                                }
                            }
                        }
                    }
                    else if (obj is UnityObject)
                    {
                        if(!m_assetDB.IsMapped((UnityObject)obj))
                        {
                            idToObj.Add(m_assetDB.ToInt(m_assetDB.ToDynamicResourceID(rootOrdinal, rootId)), (UnityObject)obj);
                        }
                    }
                }
            }

            m_assetDB.RegisterDynamicResources(idToObj);
            AssetItem[] assetItems = existingAssetItems == null ? new AssetItem[objects.Length] : existingAssetItems;
            PersistentObject[] persistentObjects = new PersistentObject[objects.Length];
            Dictionary<ProjectItem, List<ProjectItem>> parentToPotentialChildren = null;
            if(parents != null)
            {
                parentToPotentialChildren = new Dictionary<ProjectItem, List<ProjectItem>>();
                for(int i = 0; i < parents.Length; ++i)
                {
                    if(!parentToPotentialChildren.ContainsKey(parents[i]))
                    {
                        if(parents[i].Children == null)
                        {
                            parents[i].Children = new List<ProjectItem>();
                        }
                        parentToPotentialChildren.Add(parents[i], parents[i].Children.ToList());
                    }
                }
            }

            for (int objIndex = 0; objIndex < objects.Length; ++objIndex)
            {
                object obj = objects[objIndex];
                Type objType = obj.GetType();
                Type persistentType = m_typeMap.ToPersistentType(objType);
                if (persistentType == null)
                {
                    Debug.LogWarningFormat(string.Format("PersistentClass for {0} does not exist. Tools->Runtime SaveLoad->Persistent Classes->Edit", obj.GetType()), "obj");
                    continue;
                }

                if (persistentType == typeof(PersistentGameObject))
                {
                    persistentType = typeof(PersistentRuntimePrefab);
                }

                string nameOverride = nameOverrides != null ? nameOverrides[objIndex] : null;
                PersistentObject persistentObject = (PersistentObject)Activator.CreateInstance(persistentType);
                persistentObject.ReadFrom(obj);

                AssetItem assetItem = assetItems[objIndex];
                if(assetItem == null)
                {
                    if (!string.IsNullOrEmpty(nameOverride))
                    {
                        persistentObject.name = nameOverride;
                    }

                    List<ProjectItem> potentialChildren = parentToPotentialChildren[parents[objIndex]];
                    persistentObject.name = PathHelper.GetUniqueName(persistentObject.name, GetExt(obj), potentialChildren.Select(c => c.NameExt).ToList());
                    assetItem = new AssetItem();
                    if (obj is Scene)
                    {
                        assetItem.ItemID = m_assetDB.ToSceneID(rootOrdinals[objIndex], rootIds[objIndex]);
                    }
                    else
                    {
                        assetItem.ItemID = m_assetDB.ToDynamicResourceID(rootOrdinals[objIndex], rootIds[objIndex]);
                    }
                    assetItem.Parent = parents[objIndex];
                    assetItem.Name = persistentObject.name;
                    assetItem.Ext = GetExt(obj);
                    assetItem.TypeGuid = m_typeMap.ToGuid(obj.GetType());
                    potentialChildren.Add(assetItem);
                }
                if(previewData != null)
                {
                    assetItem.Preview = new Preview
                    {
                        ItemID = assetItem.ItemID,
                        PreviewData = previewData[objIndex]
                    };
                }
                
                if (persistentObject is PersistentRuntimePrefab && !(persistentObject is PersistentRuntimeScene))
                {
                    PersistentRuntimePrefab persistentPrefab = (PersistentRuntimePrefab)persistentObject;
                    if (persistentPrefab.Descriptors != null)
                    {
                        List<PrefabPart> prefabParts = new List<PrefabPart>();
                        PersistentDescriptorsToPrefabPartItems(persistentPrefab.Descriptors, prefabParts);
                        assetItem.Parts = prefabParts.ToArray();
                    }
                }

                GetDepsContext getDepsCtx = new GetDepsContext();
                persistentObject.GetDeps(getDepsCtx);
                assetItem.Dependencies = getDepsCtx.Dependencies.ToArray();

                persistentObjects[objIndex] = persistentObject;
                assetItems[objIndex] = assetItem;
            }
            m_assetDB.UnregisterDynamicResources(idToObj);

            assetItems = assetItems.Where(ai => ai != null).ToArray();
            string[] path = assetItems.Select(ai => ai.Parent.ToString()).ToArray();
            persistentObjects = persistentObjects.Where(p => p != null).ToArray();

            m_storage.Save(m_projectPath, path, assetItems, persistentObjects, m_projectInfo, false, error =>
            {
                if (!error.HasError)
                {
                    for(int objIndex = 0; objIndex < assetItems.Length; ++objIndex)
                    {
                        AssetItem assetItem = assetItems[objIndex];
                        if (assetItem.Parts != null)
                        {
                            for (int i = 0; i < assetItem.Parts.Length; ++i)
                            {
                                if(!m_idToAssetItem.ContainsKey(assetItem.Parts[i].PartID))
                                {
                                    m_idToAssetItem.Add(assetItem.Parts[i].PartID, assetItem);
                                }
                            }
                        }

                        if (!m_idToAssetItem.ContainsKey(assetItem.ItemID))
                        {
                            m_idToAssetItem.Add(assetItem.ItemID, assetItem);
                        }
                        
                        if(parents != null)
                        {
                            parents[objIndex].AddChild(assetItem);
                        }
                    }

                    if (callback != null)
                    {
                        callback(error, assetItems);
                    }

                    if (!ao.IsCompleted)
                    {
                        ao.Error = error;
                        ao.Result = assetItems;
                        ao.IsCompleted = true;
                    }
                    done();
                }
            });
        }

        private bool GetOrdinalAndId(ref int identifier, out int ordinal, out int id)
        {
            ordinal = AssetLibraryInfo.DYNAMICLIB_FIRST + m_assetDB.ToOrdinal(identifier);
            if (ordinal > AssetLibraryInfo.DYNAMICLIB_LAST)
            {
                Debug.LogError("Unable to generate identifier. Allotted Identifiers range was exhausted");
                id = 0;
                return false;
            }

            id = identifier & AssetLibraryInfo.ORDINAL_MASK;
            identifier++;
            return true;
        }

        private void OnDynamicIdentifiersExhausted(ProjectEventHandler<AssetItem[]> callback, ProjectEventHandler<AssetItem[]> eventHandler, ProjectAsyncOperation<AssetItem[]> ao, int assetIdBackup)
        {
            m_projectInfo.AssetIdentifier = assetIdBackup;
            Error error = new Error(Error.E_InvalidOperation);
            if (callback != null)
            {
                callback(error, null);
            }
            if (eventHandler != null)
            {
                eventHandler(error, null);
            }
            ao.Error = error;
            ao.Result = null;
            ao.IsCompleted = true;
        }

        /// <summary>
        /// Save Preview
        /// </summary>
        /// <param name="assetItems"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<AssetItem[]> SavePreview(AssetItem[] assetItems, ProjectEventHandler<AssetItem[]> callback = null)
        {
            ProjectAsyncOperation<AssetItem[]> ao = new ProjectAsyncOperation<AssetItem[]>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _SavePreview(assetItems, callback, ao));
            }
            else
            {
                IsBusy = true;
                _SavePreview(assetItems, callback, ao);
            }
            return ao;
        }

        private void _SavePreview(AssetItem[] assetItems, ProjectEventHandler<AssetItem[]> callback, ProjectAsyncOperation<AssetItem[]> ao)
        {
            if (m_root == null)
            {
                throw new InvalidOperationException("Project is not opened. Use OpenProject method");
            }

            m_storage.Save(m_projectPath, assetItems.Select(ai => ai.Parent.ToString()).ToArray(), assetItems, null, m_projectInfo, true, error =>
            {
                if (callback != null)
                {
                    callback(error, assetItems);
                }
                
                ao.Result = assetItems;
                ao.Error = error;
                ao.IsCompleted = true;

                if (SaveCompleted != null)
                {
                    const bool isNew = false;
                    SaveCompleted(error, assetItems, isNew);
                }

                IsBusy = false;
            });
        }

        /// <summary>
        /// Duplicate
        /// </summary>
        /// <param name="assetItems"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<AssetItem[]> Duplicate(AssetItem[] assetItems, ProjectEventHandler<AssetItem[]> callback = null)
        {
            ProjectAsyncOperation<AssetItem[]> ao = new ProjectAsyncOperation<AssetItem[]>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _Duplicate(assetItems, callback, ao));
            }
            else
            {
                IsBusy = true;
                _Duplicate(assetItems, callback, ao);
            }
            
            return ao;
        }

        private void _Duplicate(AssetItem[] assetItems, ProjectEventHandler<AssetItem[]> callback, ProjectAsyncOperation<AssetItem[]> ao)
        {
            AssetItem[] scenes =  assetItems.Where(ai => IsScene(ai)).ToArray();
            int[] sceneIndices = new int[scenes.Length];
            for(int i = 0; i < scenes.Length; ++i)
            {
                sceneIndices[i] = Array.IndexOf(assetItems, scenes[i]);
            }

            DuplicateScenes(scenes, ao, callback, (sceneDuplicates) =>
            {
                AssetItem[] nonScenes = assetItems.Where(ai => !IsScene(ai)).ToArray();

                int[] nonSceneIndices = new int[nonScenes.Length];
                for (int i = 0; i < nonScenes.Length; ++i)
                {
                    nonSceneIndices[i] = Array.IndexOf(assetItems, nonScenes[i]);
                }

                ProjectAsyncOperation<UnityObject[]> loadAo = new ProjectAsyncOperation<UnityObject[]>();
                _Load(nonScenes, (loadError, loadedObjects) =>
                {
                    if (loadError.HasError)
                    {
                        callback(loadError, null);
                        return;
                    }

                    for(int i = 0; i < loadedObjects.Length; ++i)
                    {
                        string name = loadedObjects[i].name;
                        loadedObjects[i] = Instantiate(loadedObjects[i]);
                        loadedObjects[i].name = name;
                    }

                    _Save(nonScenes.Select(ai => ai.Parent).ToArray(), nonScenes.Select(ai => ai.Preview.PreviewData).ToArray(), loadedObjects, null, null, (saveError, nonSceneDuplicates) =>
                    {
                        for(int i = 0; i < loadedObjects.Length; ++i)
                        {
                            Destroy(loadedObjects[i]);
                        }

                        AssetItem[] result = null;
                        if(!saveError.HasError)
                        {
                            result = new AssetItem[sceneDuplicates.Length + nonSceneDuplicates.Length];
                            for (int i = 0; i < sceneIndices.Length; ++i)
                            {
                                result[sceneIndices[i]] = sceneDuplicates[i];
                            }
                            for(int i = 0; i < nonSceneIndices.Length; ++i)
                            {
                                result[nonSceneIndices[i]] = nonSceneDuplicates[i];
                            }
                        }
                        
                        callback(saveError, result);

                        ao.Result = result;
                        ao.Error = saveError;
                        ao.IsCompleted = true;
                    }, 
                    ao, 
                    () =>
                    {
                        RaiseDuplicateCompleted(ao);
                    });
                }, 
                loadAo, () => { });
            });
        }

        private void RaiseDuplicateCompleted(ProjectAsyncOperation<AssetItem[]> ao)
        {
            if (DuplicateCompleted != null)
            {
                DuplicateCompleted(ao.Error, ao.Result);
            }
            IsBusy = false;
        }

        private AssetItem DuplicateScene(AssetItem assetItem, ProjectAsyncOperation<AssetItem[]> ao, ProjectEventHandler<AssetItem[]> callback, int assetIdBackup)
        {
            AssetItem copy = new AssetItem();
            copy.TypeGuid = assetItem.TypeGuid;
            copy.Name = assetItem.Name;
            copy.Ext = assetItem.Ext;
            copy.Dependencies = assetItem.Dependencies.ToArray();

            if (assetItem.Preview != null)
            {
                copy.Preview = new Preview
                {
                    PreviewData = assetItem.Preview.PreviewData != null ? assetItem.Preview.PreviewData.ToArray() : null
                };
            }
            
            int ordinal;
            int id;
            if (!GetOrdinalAndId(ref m_projectInfo.AssetIdentifier, out ordinal, out id))
            {
                OnDynamicIdentifiersExhausted(callback, null, ao, assetIdBackup);
                RaiseDuplicateCompleted(ao);
                return null;
            }

            copy.ItemID = m_assetDB.ToSceneID(ordinal, id);
            if (copy.Preview != null)
            {
                copy.Preview.ItemID = copy.ItemID;
            }

            if (!m_idToAssetItem.ContainsKey(copy.ItemID))
            {
                m_idToAssetItem.Add(copy.ItemID, copy);
            }

            return copy;
        }

        private void DuplicateScenes(AssetItem[] scenes, ProjectAsyncOperation<AssetItem[]> ao, ProjectEventHandler<AssetItem[]> callback, Action<AssetItem[]> done)
        {
            if (scenes.Length == 0)
            {
                done(new AssetItem[0]);
                return;
            }

            int assetIdBackup = m_projectInfo.AssetIdentifier;
            AssetItem[] duplicates = new AssetItem[scenes.Length];

            List<string>[] names = scenes.Select(s => s.Parent.Children.Where(c => IsScene(c)).Select(c => c.Name).ToList()).ToArray();
            for (int i = 0; i < duplicates.Length; ++i)
            {
                AssetItem copy = DuplicateScene(scenes[i], ao, callback, assetIdBackup);
                if (copy == null)
                {
                    return;
                }
                copy.Name = PathHelper.GetUniqueName(copy.Name, names[i]);
                names[i].Add(copy.Name);
                duplicates[i] = copy;
            }


           m_storage.Load(m_projectPath, scenes.Select(item => item.ToString()).ToArray(), scenes.Select(s => typeof(PersistentRuntimeScene)).ToArray(), (loadError, persistentObjects) =>
           {
               if (loadError.HasError)
               {
                   if (callback != null)
                   {
                       callback(loadError, null);
                   }
                   ao.Error = loadError;
                   ao.Result = null;
                   ao.IsCompleted = true;
                   RaiseDuplicateCompleted(ao);
                   return;
               }

               m_storage.Save(m_projectPath, scenes.Select(ai => ai.Parent.ToString()).ToArray(), duplicates, persistentObjects, m_projectInfo, false, saveError =>
               {
                   if (saveError.HasError)
                   {
                       if (callback != null)
                       {
                           callback(saveError, null);
                       }
                       ao.Error = saveError;
                       ao.Result = null;
                       ao.IsCompleted = true;
                       RaiseDuplicateCompleted(ao);
                       return;
                   }

                   for (int i = 0; i < scenes.Length; ++i)
                   {
                       scenes[i].Parent.AddChild(duplicates[i]);
                   }

                   done(duplicates);
               });
           });
        }

        /// <summary>
        /// Load asset
        /// </summary>
        /// <param name="assetItem"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<UnityObject[]> Load(AssetItem[] assetItems, ProjectEventHandler<UnityObject[]> callback)
        {
            ProjectAsyncOperation<UnityObject[]> ao = new ProjectAsyncOperation<UnityObject[]>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _Load(assetItems, callback, ao));
            }
            else
            {
                IsBusy = true;
                _Load(assetItems, callback, ao);
            }
           
            return ao;
        }

        private void _Load(AssetItem[] assetItems, ProjectEventHandler<UnityObject[]> callback, ProjectAsyncOperation<UnityObject[]> ao)
        {
            if (BeginLoad != null)
            {
                BeginLoad(new Error(), assetItems);
            }

            _Load(assetItems, callback, ao, () =>
            {
                if(Application.isPlaying)
                {
                    StartCoroutine(CoCallback(() =>
                    {
                        if (!ao.Error.HasError)
                        {
                            if (assetItems.Any(assetItem => ToType(assetItem) == typeof(Scene)))
                            {
                                m_loadedScene = assetItems.First(assetItem => ToType(assetItem) == typeof(Scene));
                            }
                        }

                        if (LoadCompleted != null)
                        {
                            LoadCompleted(ao.Error, assetItems, ao.Result);
                        }

                        IsBusy = false;
                    }));
                }
                else
                {
                    if (!ao.Error.HasError)
                    {
                        if (assetItems.Any(assetItem => ToType(assetItem) == typeof(Scene)))
                        {
                            m_loadedScene = assetItems.First(assetItem => ToType(assetItem) == typeof(Scene));
                        }
                    }

                    if (LoadCompleted != null)
                    {
                        LoadCompleted(ao.Error, assetItems, ao.Result);
                    }

                    IsBusy = false;
                } 
            });
        }

        private void _Load(AssetItem[] assetItems, ProjectEventHandler<UnityObject[]> callback, ProjectAsyncOperation<UnityObject[]> ao, Action done)
        {
            HashSet<AssetItem> loadAssetItemsHs = new HashSet<AssetItem>();
            BeginResolveDependencies(assetItems, loadAssetItemsHs, () =>
            {
                OnDependenciesResolved(assetItems, loadAssetItemsHs, callback, ao, done);
            });
        }

        public void BeginResolveDependencies(AssetItem[] assetItems, HashSet<AssetItem> loadHs, Action callback)
        {
            HashSet<long> unresolvedDependencies = new HashSet<long>();
            GetAssetItemsToLoad(assetItems, loadHs, unresolvedDependencies);

            if (unresolvedDependencies.Count > 0)
            {
                HashSet<int> assetLibrariesToLoad = new HashSet<int>();
                foreach (long unresolvedDependency in unresolvedDependencies)
                {
                    if (m_assetDB.IsStaticResourceID(unresolvedDependency))
                    {
                        int ordinal = m_assetDB.ToOrdinal(unresolvedDependency);
                        if (!assetLibrariesToLoad.Contains(ordinal) && !m_assetDB.IsLibraryLoaded(ordinal))
                        {
                            assetLibrariesToLoad.Add(ordinal);
                        }
                    }
                }

                DoLoadAssetLibraries(assetLibrariesToLoad, () =>
                {
                    foreach (long unresolvedDependency in unresolvedDependencies)
                    {
                        UnityObject obj = m_assetDB.FromID<UnityObject>(unresolvedDependency);
                        if (obj != null)
                        {
                            Guid typeGuid = m_typeMap.ToGuid(obj.GetType());
                            if (typeGuid != Guid.Empty)
                            {
                                AssetItem resolvedAssetItem = new AssetItem();
                                resolvedAssetItem.ItemID = unresolvedDependency;
                                resolvedAssetItem.Name = obj.name;
                                resolvedAssetItem.TypeGuid = typeGuid;
                                m_idToAssetItem.Add(resolvedAssetItem.ItemID, resolvedAssetItem);
                            }
                        }
                    }
                    callback();
                });
            }
            else
            {
                callback();
            }
        }

        private void GetAssetItemsToLoad(AssetItem[] assetItems, HashSet<AssetItem> loadHs, HashSet<long> unresolvedDependencies)
        {
            for(int a = 0; a < assetItems.Length; ++a)
            {
                AssetItem assetItem = assetItems[a];
                Type type = m_typeMap.ToType(assetItem.TypeGuid);
                if (type == null)
                {
                    continue;
                }
                Type persistentType = m_typeMap.ToPersistentType(type);
                if (persistentType == null)
                {
                    continue;
                }

                if (!loadHs.Contains(assetItem) && !m_assetDB.IsMapped(assetItem.ItemID))
                {
                    loadHs.Add(assetItem);
                    if (assetItem.Dependencies != null)
                    {
                        List<AssetItem> deps = new List<AssetItem>();
                        for (int i = 0; i < assetItem.Dependencies.Length; ++i)
                        {
                            long dep = assetItem.Dependencies[i];

                            AssetItem dependencyAssetItem;
                            if (m_idToAssetItem.TryGetValue(dep, out dependencyAssetItem))
                            {
                                deps.Add(dependencyAssetItem);
                            }
                            else
                            {
                                if (!unresolvedDependencies.Contains(dep))
                                {
                                    unresolvedDependencies.Add(dep);
                                }
                            }
                        }

                        if(deps.Count > 0)
                        {
                            GetAssetItemsToLoad(deps.ToArray(), loadHs, unresolvedDependencies);
                        }
                    }
                }
            }
          
        }

        private void OnDependenciesResolved(AssetItem[] rootAssetItems, HashSet<AssetItem> loadAssetItemsHs, ProjectEventHandler<UnityObject[]> callback, ProjectAsyncOperation<UnityObject[]> ao, Action done)
        {
            Type[] persistentTypes = loadAssetItemsHs.Select(item => m_typeMap.ToPersistentType(m_typeMap.ToType(item.TypeGuid))).ToArray();
            for (int i = 0; i < persistentTypes.Length; ++i)
            {
                if (persistentTypes[i] == typeof(PersistentGameObject))
                {
                    persistentTypes[i] = typeof(PersistentRuntimePrefab);
                }
            }

            m_storage.Load(m_projectPath, loadAssetItemsHs.Select(item => item.ToString()).ToArray(), persistentTypes, (error, persistentObjects) =>
            {
                if (error.HasError)
                {
                    if (callback != null)
                    {
                        callback(error, null);
                    }
                    
                    ao.Error = error;
                    ao.Result = new UnityObject[0];
                    ao.IsCompleted = true;

                    done();
                    return;
                }

                AssetItem[] assetItems = loadAssetItemsHs.ToArray();
                LoadAllAssetLibraries(assetItems.Select(ai => ai.ItemID).ToArray(), () =>
                {
                    OnLoadCompleted(rootAssetItems, assetItems, persistentObjects, ao, callback, done);
                });
            });
        }

        private void LoadAllAssetLibraries(long[] deps, Action callback)
        {
            HashSet<int> assetLibrariesToLoad = new HashSet<int>();
            for (int i = 0; i < deps.Length; ++i)
            {
                long id = deps[i];
                if (!m_assetDB.IsMapped(id))
                {
                    if (m_assetDB.IsStaticResourceID(id))
                    {
                        int ordinal = m_assetDB.ToOrdinal(id);
                        if (!assetLibrariesToLoad.Contains(ordinal) && !m_assetDB.IsLibraryLoaded(ordinal))
                        {
                            assetLibrariesToLoad.Add(ordinal);
                        }
                    }
                }
            }

            DoLoadAssetLibraries(assetLibrariesToLoad, callback);
        }

        private void DoLoadAssetLibraries(HashSet<int> assetLibrariesToLoad, Action callback)
        {   
            if (assetLibrariesToLoad.Count == 0)
            {
                callback();
            }
            else
            {
                Dictionary<int, string> ordinalToStaticLib = GetStaticAssetLibraries();

                int loadedLibrariesCount = 0;
                foreach (int ordinal in assetLibrariesToLoad)
                {
                    string assetLibraryName = null;
                    if(ordinalToStaticLib.ContainsKey(ordinal))
                    {
                        assetLibraryName = ordinalToStaticLib[ordinal];
                    }
                    else
                    {
                        if(m_assetDB.IsBuiltinLibrary(ordinal))
                        {
                            if(ordinal != AssetLibraryInfo.BUILTIN_FIRST)
                            {
                                assetLibraryName = m_builtInLibrary + ((ordinal - AssetLibraryInfo.BUILTIN_FIRST) + 1);
                            }
                            else
                            {
                                assetLibraryName = m_builtInLibrary;
                            }
                        }
                        else if(m_assetDB.IsSceneLibrary(ordinal))
                        {
                            if(ordinal != AssetLibraryInfo.SCENELIB_FIRST)
                            {
                                assetLibraryName = m_sceneDepsLibrary + ((ordinal - AssetLibraryInfo.SCENELIB_FIRST) + 1);
                            }
                            else
                            {
                                assetLibraryName = m_sceneDepsLibrary;
                            }       
                        }
                        else if(m_assetDB.IsBundledLibrary(ordinal))
                        {
                            AssetBundleInfo assetBundleInfo = m_ordinalToAssetBundleInfo[ordinal];
                            assetLibraryName = assetBundleInfo.UniqueName;
                        }
                    }

                    if(!string.IsNullOrEmpty(assetLibraryName))
                    {
                        LoadLibrary(ordinal, true, true, done =>
                        {
                            if (!done)
                            {
                                Debug.LogWarning("Asset Library '" + assetLibraryName + "' was not loaded");
                            }
                            loadedLibrariesCount++;
                            if (assetLibrariesToLoad.Count == loadedLibrariesCount)
                            {
                                callback();
                            }
                        });
                    }
                    else
                    {
                        loadedLibrariesCount++;
                        if (assetLibrariesToLoad.Count == loadedLibrariesCount)
                        {
                            callback();
                        }
                    }
                }
            }
        }

        private void LoadLibrary(int ordinal, bool loadIIDtoPID, bool loadPIDtoObj, Action<bool> callback)
        {
            if (m_assetDB.IsLibraryLoaded(ordinal))
            {
                Debug.LogError("Already loaded");
                callback(false);
            }

            if (m_assetDB.IsStaticLibrary(ordinal))
            {
                Dictionary<int, string> staticAssetLibraries = GetStaticAssetLibraries();
                if(!staticAssetLibraries.ContainsKey(ordinal))
                {
                    Debug.LogError("Unable to load static library " + ordinal);
                    callback(false);
                    return;
                }
                m_assetDB.LoadLibrary(staticAssetLibraries[ordinal], ordinal, loadIIDtoPID, loadPIDtoObj, callback);
            }
            else if(m_assetDB.IsBuiltinLibrary(ordinal))
            {
                int num = ordinal - AssetLibraryInfo.BUILTIN_FIRST;
                string builtinLibraryName = m_builtInLibrary;
                if(num > 0)
                {
                    builtinLibraryName += (num + 1);
                }
                m_assetDB.LoadLibrary(builtinLibraryName, ordinal, loadIIDtoPID, loadPIDtoObj, callback);
            }
            else if(m_assetDB.IsSceneLibrary(ordinal))
            {
                int num = ordinal - AssetLibraryInfo.SCENELIB_FIRST;
                string sceneLibraryName = m_sceneDepsLibrary;
                if(num > 0)
                {
                    sceneLibraryName += (num + 1);
                }
                m_assetDB.LoadLibrary(sceneLibraryName, ordinal, loadIIDtoPID, loadPIDtoObj, callback);
            }
            else if (m_assetDB.IsBundledLibrary(ordinal))
            {
                AssetBundleInfo assetBundleInfo;
                if (!m_ordinalToAssetBundleInfo.TryGetValue(ordinal, out assetBundleInfo))
                {
                    throw new InvalidOperationException("asset bundle with ordinal = " + ordinal + " was not imported");
                }

                IAssetBundleLoader loader = IOC.Resolve<IAssetBundleLoader>();
                loader.Load(assetBundleInfo.UniqueName, assetBundle =>
                {
                    if (assetBundle == null)
                    {
                        Debug.LogError("Unable to load asset bundle " + assetBundleInfo.UniqueName);
                        callback(false);
                        return;
                    }

                    m_ordinalToAssetBundle.Add(ordinal, assetBundle);

                    AssetLibraryAsset assetLibraryAsset = ToAssetLibraryAsset(assetBundle, assetBundleInfo);
                    m_assetDB.AddLibrary(assetLibraryAsset, ordinal, loadIIDtoPID, loadPIDtoObj);
                    callback(true);
                });
            }
            else
            {
                throw new ArgumentException("could load static or bundled library", "ordinal");
            }
        }

        private void OnLoadCompleted(AssetItem[] rootAssetItems, AssetItem[] assetItems, PersistentObject[] persistentObjects, ProjectAsyncOperation<UnityObject[]> ao, ProjectEventHandler<UnityObject[]> callback, Action done)
        {
            for (int i = 0; i < assetItems.Length; ++i)
            {
                AssetItem assetItem = assetItems[i];
                if (!m_assetDB.IsMapped(assetItem.ItemID))
                {
                    if (m_assetDB.IsDynamicResourceID(assetItem.ItemID))
                    {
                        PersistentObject persistentObject = persistentObjects[i];
                        if (persistentObject != null)
                        {
                            if (persistentObject is PersistentRuntimeScene)
                            {
                                PersistentRuntimeScene persistentScene = (PersistentRuntimeScene)persistentObject;
                                Dictionary<int, UnityObject> idToObj = new Dictionary<int, UnityObject>();
                                persistentScene.CreateGameObjectWithComponents(m_typeMap, persistentScene.Descriptors[0], idToObj, null);
                            }
                            else if (persistentObject is PersistentRuntimePrefab)
                            {
                                PersistentRuntimePrefab persistentPrefab = (PersistentRuntimePrefab)persistentObject;
                                Dictionary<int, UnityObject> idToObj = new Dictionary<int, UnityObject>();
                                List<GameObject> createdGameObjects = new List<GameObject>();
                                persistentPrefab.CreateGameObjectWithComponents(m_typeMap, persistentPrefab.Descriptors[0], idToObj, m_dynamicPrefabsRoot, createdGameObjects);
                                m_assetDB.RegisterDynamicResources(idToObj);
                                for (int j = 0; j < createdGameObjects.Count; ++j)
                                {
                                    GameObject createdGO = createdGameObjects[j];
                                    createdGO.hideFlags = HideFlags.HideAndDontSave;
                                }
                            }
                            else
                            {
                                Type type = m_typeMap.ToType(assetItem.TypeGuid);
                                if(type != null)
                                {
                                    if (m_factory.CanCreateInstance(type, persistentObject))
                                    {
                                        UnityObject instance = m_factory.CreateInstance(type, persistentObject);
                                        if (instance != null)
                                        {
                                            m_assetDB.RegisterDynamicResource(m_assetDB.ToInt(assetItem.ItemID), instance);
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning("Unable to create object of type " + type.ToString());
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning("Unable to resolve type by guid " + assetItem.TypeGuid);
                                }   
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < persistentObjects.Length; ++i)
            {
                PersistentObject persistentObject = persistentObjects[i];
                if (persistentObject != null)
                {
                    if (m_assetDB.IsSceneID(assetItems[i].ItemID))
                    {
                        persistentObject.WriteTo(SceneManager.GetActiveScene());
                    }
                    else
                    {
                        UnityObject obj = m_assetDB.FromID<UnityObject>(assetItems[i].ItemID);
                        //Debug.Assert(obj != null, "obj is null");
                        if (obj != null)
                        {
                            persistentObject.WriteTo(obj);
                            obj.name = assetItems[i].Name;

                            if (m_assetDB.IsDynamicResourceID(assetItems[i].ItemID))
                            {
                                obj.hideFlags = HideFlags.HideAndDontSave;
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Unable to find UnityEngine.Object for " + assetItems[i].ToString() + " with id: " + assetItems[i].ItemID + ". This typically means that asset or corresponding asset library was removed (or its ordinal was changed).");
                        }
                    }
                }
            }

            Error error = new Error(Error.OK);
            UnityObject[] result = rootAssetItems.Select(rootItem => m_assetDB.FromID<UnityObject>(rootItem.ItemID)).ToArray();
            if (callback != null)
            {
                callback(error, result);
            }
            ao.Error = error;
            ao.Result = result;
            ao.IsCompleted = true;

            done();
        }

        public void Unload(AssetItem[] assetItems)
        {
            ProjectAsyncOperation ao = new ProjectAsyncOperation();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _Unload(assetItems));
            }
            else
            {
                IsBusy = true;
                _Unload(assetItems);
            }
        }

        private void _Unload(AssetItem[] assetItems)
        {
            for (int i = 0; i < assetItems.Length; ++i)
            {
                AssetItem assetItem = assetItems[i];
                if (!m_assetDB.IsDynamicResourceID(assetItem.ItemID))
                {
                    Debug.LogWarning("Unable to unload " + assetItem.ToString() + ". It is possible to unload dynamic resources only"); ;
                    continue;
                }

                if (assetItem.Parts != null)
                {
                    for (int p = 0; p < assetItem.Parts.Length; ++p)
                    {
                        int partID = m_assetDB.ToInt(assetItem.Parts[p].PartID);
                        m_assetDB.UnregisterDynamicResource(partID);
                    }
                }

                if (m_assetDB.IsDynamicResourceID(assetItem.ItemID))
                {
                    UnityObject obj = m_assetDB.FromID<UnityObject>(assetItem.ItemID);
                    int itemID = m_assetDB.ToInt(assetItem.ItemID);
                    m_assetDB.UnregisterDynamicResource(itemID);
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
            }
            IsBusy = false;
        }

        /// <summary>
        /// Unload everything
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation Unload(ProjectEventHandler callback = null)
        {
            ProjectAsyncOperation ao = new ProjectAsyncOperation();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _Unload(callback, ao));
            }
            else
            {
                IsBusy = true;
                _Unload(callback, ao);
            }
            
            return ao;
        }

        private void _Unload(ProjectEventHandler callback, ProjectAsyncOperation ao)
        {
            UnloadUnregisterDestroy();
            m_assetDB.UnloadUnusedAssets(op =>
            {
                if (callback != null)
                {
                    callback(new Error());
                }

                ao.IsCompleted = true;
                ao.Error = new Error();

                if (UnloadCompleted != null)
                {
                    UnloadCompleted(new Error());
                }

                IsBusy = false;
            });
        }

        /// <summary>
        /// Load asset library to import
        /// </summary>
        /// <param name="libraryName"></param>
        /// <param name="isBuiltIn"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<ProjectItem> LoadImportItems(string libraryName, bool isBuiltIn, ProjectEventHandler<ProjectItem> callback = null)
        {
            ProjectAsyncOperation<ProjectItem> ao = new ProjectAsyncOperation<ProjectItem>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _LoadImportItems(libraryName, isBuiltIn, callback, ao));
            }
            else
            {
                IsBusy = true;
                _LoadImportItems(libraryName, isBuiltIn, callback, ao);
            }
            
            return ao;
        }

        private void _LoadImportItems(string libraryName, bool isBuiltIn, ProjectEventHandler<ProjectItem> callback, ProjectAsyncOperation<ProjectItem> ao)
        {
            if (m_root == null)
            {
                Error error = new Error(Error.E_InvalidOperation);
                error.ErrorText = "Unable to load asset library. Open project first";
                RaiseLoadAssetLibraryCallback(callback, ao, error);
            }

            if (isBuiltIn)
            {
                int ordinal = -1;

                Dictionary<int, string> ordinalToStaticAssetLibrary = GetStaticAssetLibraries();
                foreach (KeyValuePair<int, string> kvp in m_ordinalToStaticAssetLibrary)
                {
                    if(kvp.Value == libraryName)
                    {
                        ordinal = kvp.Key;
                        break;
                    }
                }

                if (ordinal < 0)
                {
                    Error error = new Error(Error.E_NotFound);
                    error.ErrorText = "Built-in asset library " + libraryName + " not found";
                    RaiseLoadAssetLibraryCallback(callback, ao, error);
                    return;
                }

                ResourceRequest request = Resources.LoadAsync<AssetLibraryAsset>(libraryName);
                Action<AsyncOperation> completed = null;
                completed = op =>
                {
                    request.completed -= completed;

                    AssetLibraryAsset asset = (AssetLibraryAsset)request.asset;

                    CompleteLoadAssetLibrary(libraryName, callback, ao, ordinal, asset);

                    IsBusy = false;
                };
                request.completed += completed;
                return;
            }
            else
            {
                if(m_projectInfo.BundleIdentifier >= AssetLibraryInfo.MAX_BUNDLEDLIBS - 1)
                {
                    Error error = new Error(Error.E_NotFound);
                    error.ErrorText = "Unable to load asset bundle. Bundle identifiers exhausted";
                    RaiseLoadAssetLibraryCallback(callback, ao, error);
                    return;
                }
                m_storage.Load(m_projectPath, libraryName, (loadError, assetBundleInfo) =>
                {
                    if(loadError.HasError && loadError.ErrorCode != Error.E_NotFound)
                    {
                        RaiseLoadAssetLibraryCallback(callback, ao, loadError);
                        return;
                    }

                    if(assetBundleInfo == null)
                    {
                        assetBundleInfo = new AssetBundleInfo();
                        assetBundleInfo.UniqueName = libraryName;
                        assetBundleInfo.Ordinal = AssetLibraryInfo.BUNDLEDLIB_FIRST + m_projectInfo.BundleIdentifier;
                        m_projectInfo.BundleIdentifier++;
                        m_ordinalToAssetBundleInfo.Add(assetBundleInfo.Ordinal, assetBundleInfo);
                    }

                    AssetBundle loadedAssetBundle;
                    if(m_ordinalToAssetBundle.TryGetValue(assetBundleInfo.Ordinal, out loadedAssetBundle))
                    {
                        Debug.Assert(m_assetDB.IsLibraryLoaded(assetBundleInfo.Ordinal));
                        LoadImportItemsFromAssetBundle(libraryName, callback, assetBundleInfo, loadedAssetBundle, ao);
                    }
                    else
                    {
                        IAssetBundleLoader loader = IOC.Resolve<IAssetBundleLoader>();
                        loader.Load(libraryName, assetBundle =>
                        {
                            LoadImportItemsFromAssetBundle(libraryName, callback, assetBundleInfo, assetBundle, ao);
                        });
                    }
                    
                });
            }
        }

        private void LoadImportItemsFromAssetBundle(string libraryName, ProjectEventHandler<ProjectItem> callback, AssetBundleInfo assetBundleInfo, AssetBundle assetBundle, ProjectAsyncOperation<ProjectItem> ao)
        {
            if (assetBundle == null)
            {
                Error error = new Error(Error.E_NotFound);
                error.ErrorText = "Unable to load asset bundle";
                RaiseLoadAssetLibraryCallback(callback, ao, error);
                return;
            }

            GenerateIdentifiers(assetBundle, assetBundleInfo);
            if (assetBundleInfo.Identifier >= AssetLibraryInfo.MAX_ASSETS)
            {
                Error error = new Error(Error.E_NotFound);
                error.ErrorText = "Unable to load asset bundle. Asset identifier exhausted";
                RaiseLoadAssetLibraryCallback(callback, ao, error);
                return;
            }

            m_storage.Save(m_projectPath, assetBundleInfo, m_projectInfo, saveError =>
            {
                AssetLibraryAsset asset = ToAssetLibraryAsset(assetBundle, assetBundleInfo);
                CompleteLoadAssetLibrary(libraryName, callback, ao, assetBundleInfo.Ordinal, asset);

                if (!m_assetDB.IsLibraryLoaded(assetBundleInfo.Ordinal))
                {
                    assetBundle.Unload(false);
                }

                IsBusy = false;
            });
        }

        private void GenerateIdentifiers(AssetBundle bundle, AssetBundleInfo info)
        {
            Dictionary<string, AssetBundleItemInfo> pathToBundleItem = info.AssetBundleItems != null ? info.AssetBundleItems.ToDictionary(i => i.Path) : new Dictionary<string, AssetBundleItemInfo>();

            string[] assetNames = bundle.GetAllAssetNames();
            for(int i = 0; i < assetNames.Length; ++i)
            {
                string assetName = assetNames[i];
                AssetBundleItemInfo bundleItem;
                if(!pathToBundleItem.TryGetValue(assetName, out bundleItem))
                {
                    bundleItem = new AssetBundleItemInfo
                    {
                        Path = assetName,
                        Id = info.Identifier,
                    };
                    info.Identifier++;
                    pathToBundleItem.Add(bundleItem.Path, bundleItem);
                }

                UnityObject obj = bundle.LoadAsset<UnityObject>(assetName);
                if (obj is GameObject)
                {
                    GenerateIdentifiersForPrefab(assetName, (GameObject)obj, info, pathToBundleItem);
                }
            }

            info.AssetBundleItems = pathToBundleItem.Values.ToArray();
        }

        private void GenerateIdentifiersForPrefab(string assetName, GameObject go, AssetBundleInfo info, Dictionary<string, AssetBundleItemInfo> pathToBundleItem)
        {
            Component[] components = go.GetComponents<Component>();
            for(int i = 0; i < components.Length; ++i)
            {
                Component component = components[i]; 
                if(component != null)
                {
                    string componentName = assetName + "/" + component.GetType().FullName;
                    AssetBundleItemInfo bundleItem;
                    if (!pathToBundleItem.TryGetValue(componentName, out bundleItem)) //Multiple components of same type are not supported
                    {
                        bundleItem = new AssetBundleItemInfo
                        {
                            Path = componentName,
                            Id = info.Identifier,
                            ParentId = pathToBundleItem[assetName].Id
                        };
                        info.Identifier++;
                        pathToBundleItem.Add(bundleItem.Path, bundleItem);
                    }
                }
            }

            Dictionary<string, int> nameToIndex = new Dictionary<string, int>();
            foreach(Transform child in go.transform)
            {
                GameObject childGo = child.gameObject;
                string childName = assetName + "/" + childGo.name + "###";  //Children re-arrangement will lead to new (wrong) identifiers will be generated
                int index = 0;
                if(!nameToIndex.TryGetValue(childName, out index))
                {
                    nameToIndex.Add(childName, 2);
                }

                if(index > 0)
                {
                    childName += index;
                    nameToIndex[childName]++;
                }
                
                AssetBundleItemInfo bundleItem;
                if (!pathToBundleItem.TryGetValue(childName, out bundleItem))
                {
                    bundleItem = new AssetBundleItemInfo
                    {
                        Path = childName,
                        Id = info.Identifier,
                        ParentId = pathToBundleItem[assetName].Id
                    };
                    info.Identifier++;
                    pathToBundleItem.Add(bundleItem.Path, bundleItem);
                }
                index++;
                GenerateIdentifiersForPrefab(childName, child.gameObject, info, pathToBundleItem);
            }
        }

        private AssetLibraryAsset ToAssetLibraryAsset(AssetBundle bundle, AssetBundleInfo info)
        {
            UnityObject[] allAssets = bundle.LoadAllAssets();
            for(int i = 0; i < allAssets.Length; ++i)
            {
                UnityObject asset = allAssets[i];
                if(asset is AssetLibraryAsset)
                {
                    AssetLibraryAsset assetLibraryAsset = (AssetLibraryAsset)asset;
                    assetLibraryAsset.Ordinal = info.Ordinal;
                    
                }
            }
  
            AssetLibraryAsset result = ScriptableObject.CreateInstance<AssetLibraryAsset>();
            result.Ordinal = info.Ordinal;

            AssetLibraryInfo assetLib = result.AssetLibrary;
            AssetFolderInfo assetsFolder = assetLib.Folders[1];
            if(assetsFolder.children == null)
            {
                assetsFolder.children = new List<TreeElement>();
            }
            if(assetsFolder.Assets == null)
            {
                assetsFolder.Assets = new List<AssetInfo>();
            }
            int folderId = assetsFolder.id + 1;
            AssetBundleItemInfo[] assetBundleItems = info.AssetBundleItems.OrderBy(i => i.Path.Length).ToArray(); //components will have greater indices
            List<AssetInfo> assetsList = new List<AssetInfo>();
            for(int i = 0; i < assetBundleItems.Length; ++i)
            {
                AssetFolderInfo folder = assetsFolder;
                AssetBundleItemInfo bundleItem = info.AssetBundleItems[i];
                string[] pathParts = bundleItem.Path.Split('/');
                int p = 1;
                for(;p < pathParts.Length; ++p)
                {
                    string pathPart = pathParts[p];
                    if(pathPart.Contains("."))
                    {
                        break;
                    }

                    AssetFolderInfo childFolder = (AssetFolderInfo)folder.children.FirstOrDefault(f => f.name == pathPart);
                    if(childFolder == null)
                    {
                        childFolder = new AssetFolderInfo(pathPart, folder.depth + 1, folderId);
                        childFolder.children = new List<TreeElement>();
                        childFolder.Assets = new List<AssetInfo>();
                        folderId++;
                        folder.children.Add(childFolder);
                    }
                    folder = childFolder;                    
                }

                if(pathParts.Length > 1)
                {
                    AssetInfo assetInfo = folder.Assets != null ? folder.Assets.Where(a => a.name == pathParts[p]).FirstOrDefault() : null;
                    if (assetInfo == null)
                    {
                        assetInfo = new AssetInfo(pathParts[p], 0, bundleItem.Id);
                        assetInfo.PrefabParts = new List<PrefabPartInfo>();

                        Debug.Assert(p == pathParts.Length - 1);
                        assetInfo.Object = bundle.LoadAsset(bundleItem.Path);
                        
                        folder.Assets.Add(assetInfo);
                        assetsList.Add(assetInfo);
                    }
                    else
                    {
                        UnityObject prefab = assetInfo.Object;
                        if (prefab is GameObject)
                        {
                            GameObject go = (GameObject)prefab;
                            PrefabPartInfo prefabPart = new PrefabPartInfo();
                            prefabPart.Object = GetPrefabPartAtPath(go, pathParts, p + 1);
                            prefabPart.PersistentID = bundleItem.Id;
                            prefabPart.ParentPersistentID = bundleItem.ParentId;
                            prefabPart.Depth = pathParts.Length - p;
                            assetInfo.PrefabParts.Add(prefabPart);
                        }
                    }
                }
            }

            //fix names
            for(int i = 0; i < assetsList.Count; ++i)
            {
                assetsList[i].name = Path.GetFileNameWithoutExtension(assetsList[i].name);
            }

            //convert folders tree to assetLibraryInfo folders array;
            if(assetsFolder.hasChildren)
            {
                for (int i = 0; i < assetsFolder.children.Count; ++i)
                {
                    FoldersTreeToArray(assetLib, (AssetFolderInfo)assetsFolder.children[i]);
                }
            }
            
            return result;
        }

        private UnityObject GetPrefabPartAtPath(GameObject go, string[] path, int pathPartIndex)
        {
            string pathPart = path[pathPartIndex];
            if(pathPart.Contains("###"))
            {
                string[] nameAndNumber = pathPart.Split(new[] { "###" }, StringSplitOptions.RemoveEmptyEntries);
                string name = nameAndNumber[0];
                int number;

                GameObject childGo = null;
                if (nameAndNumber.Length > 1 && int.TryParse(nameAndNumber[1], out number))
                {
                    int n = 2;
                    foreach (Transform child in go.transform)
                    {
                        if (child.name == name)
                        {
                            if(n == number)
                            {
                                childGo = child.gameObject;
                                break;
                            }
                            else
                            {
                                n++;
                            }
                        }
                    }
                }
                else
                {
                    foreach(Transform child in go.transform)
                    {
                        if(child.name == name)
                        {
                            childGo = child.gameObject;
                            break;
                        }
                    }
                }

                if(childGo != null)
                {
                    if (pathPartIndex < path.Length - 1)
                    {
                        return GetPrefabPartAtPath(childGo, path, pathPartIndex + 1);
                    }
                }
                
                return childGo;
            }
            
            Debug.Assert(pathPartIndex == path.Length - 1);
            
            Component component = go.GetComponents<Component>().Where(c => c != null && c.GetType().FullName == path[pathPartIndex]).FirstOrDefault();
            return component;
        }

        private void FoldersTreeToArray(AssetLibraryInfo assetLibraryInfo, AssetFolderInfo folder)
        {
            assetLibraryInfo.Folders.Add(folder);
            if(folder.hasChildren)
            {
                for (int i = 0; i < folder.children.Count; ++i)
                {
                    FoldersTreeToArray(assetLibraryInfo, (AssetFolderInfo)folder.children[i]);
                }
            }
        }

        private void RaiseLoadAssetLibraryCallback(ProjectEventHandler<ProjectItem> callback, ProjectAsyncOperation<ProjectItem> ao, Error error)
        {
            if (callback != null)
            {
                callback(error, null);
            }
            ao.Error = error;
            ao.IsCompleted = true;

            IsBusy = false;
        }

        private void CompleteLoadAssetLibrary(string name, ProjectEventHandler<ProjectItem> callback, ProjectAsyncOperation<ProjectItem> pao, int ordinal, AssetLibraryAsset asset)
        {
            ProjectItem result = new ProjectItem();
            Error error = new Error(Error.OK);
            if (asset == null)
            {
                error.ErrorCode = Error.E_NotFound;
                error.ErrorText = "Asset Library " + name + " does not exist";
                if (callback != null)
                {
                    callback(error, result);
                }

                pao.Error = error;
                pao.Result = null;
                pao.IsCompleted = true;
                return;
            }

            TreeModel<AssetFolderInfo> model = new TreeModel<AssetFolderInfo>(asset.AssetLibrary.Folders);
            BuildImportItemsTree(result, (AssetFolderInfo)model.root.children[0], ordinal);

            if (callback != null)
            {
                callback(error, result);
            }

            pao.Result = result;
            pao.IsCompleted = true;
            pao.Error = error;

            if (!m_assetDB.IsLibraryLoaded(ordinal))
            {
                if (!m_assetDB.IsBundledLibrary(asset.Ordinal))
                {
                    Resources.UnloadAsset(asset);
                }
            }
        }

        private void BuildImportItemsTree(ProjectItem projectItem, AssetFolderInfo folder, int ordinal)
        {
            projectItem.Name = folder.name;

            if (folder.hasChildren)
            {
                projectItem.Children = new List<ProjectItem>();
                for (int i = 0; i < folder.children.Count; ++i)
                {
                    ProjectItem child = new ProjectItem();
                    projectItem.AddChild(child);
                    BuildImportItemsTree(child, (AssetFolderInfo)folder.children[i], ordinal);
                }
            }

            if (folder.Assets != null && folder.Assets.Count > 0)
            {
                if (projectItem.Children == null)
                {
                    projectItem.Children = new List<ProjectItem>();
                }

                List<string> existingNames = new List<string>();
                for (int i = 0; i < folder.Assets.Count; ++i)
                {
                    AssetInfo assetInfo = folder.Assets[i];
                    if (assetInfo.Object != null)
                    {
                        ImportStatus status = ImportStatus.New;
                        string ext = GetExt(assetInfo.Object);
                        string name = PathHelper.GetUniqueName(assetInfo.name, ext, existingNames);
                        long itemID = m_assetDB.ToStaticResourceID(ordinal, assetInfo.PersistentID);
                        Guid typeGuid = m_typeMap.ToGuid(assetInfo.Object.GetType());
                        if (typeGuid == Guid.Empty)
                        {
                            continue;
                        }

                        ImportItem importItem = new ImportItem
                        {
                            Name = name,
                            Ext = ext,
                            Object = assetInfo.Object,
                            TypeGuid = typeGuid,
                            ItemID = itemID
                        };

                        if (assetInfo.PrefabParts != null)
                        {
                            List<PrefabPart> parts = new List<PrefabPart>();
                            for (int j = 0; j < assetInfo.PrefabParts.Count; ++j)
                            {
                                PrefabPartInfo partInfo = assetInfo.PrefabParts[j];

                                if (partInfo.Object != null)
                                {
                                    Guid partTypeGuid = m_typeMap.ToGuid(partInfo.Object.GetType());
                                    if (partTypeGuid == Guid.Empty)
                                    {
                                        continue;
                                    }
                                    PrefabPart part = new PrefabPart
                                    {
                                        Name = partInfo.Object.name,
                                        PartID = m_assetDB.ToStaticResourceID(ordinal, partInfo.PersistentID),
                                        ParentID = m_assetDB.ToStaticResourceID(ordinal, partInfo.ParentPersistentID),
                                        TypeGuid = partTypeGuid,
                                    };

                                    AssetItem partAssetItem;
                                    if (m_idToAssetItem.TryGetValue(part.PartID, out partAssetItem))
                                    {
                                        if (partAssetItem.ItemID != itemID || partAssetItem.TypeGuid != typeGuid)
                                        {
                                            status = ImportStatus.Conflict;
                                        }
                                    }

                                    parts.Add(part);
                                }
                            }
                            importItem.Parts = parts.ToArray();
                        }

                        if (status != ImportStatus.Conflict)
                        {
                            AssetItem exisitingItem;
                            if (m_idToAssetItem.TryGetValue(itemID, out exisitingItem))
                            {
                                if (exisitingItem.TypeGuid == typeGuid)
                                {
                                    status = ImportStatus.Overwrite;
                                }
                                else
                                {
                                    status = ImportStatus.Conflict;
                                }
                                importItem.Name = exisitingItem.Name;
                            }
                            else
                            {
                                status = ImportStatus.New;
                            }
                        }

                        const bool doNotOverwriteItems = true;
                        if(!doNotOverwriteItems || status != ImportStatus.Overwrite)
                        {
                            importItem.Status = status;
                            projectItem.AddChild(importItem);
                            existingNames.Add(importItem.NameExt);
                        }
                    }
                }
            }

            RemoveEmptyFolders(projectItem);
        }

        private void RemoveEmptyFolders(ProjectItem item)
        {
            if(item.Children != null)
            {
                for (int i = item.Children.Count - 1; i >= 0; --i)
                {
                    RemoveEmptyFolders(item.Children[i]);
                    if (item.Children[i].IsFolder && (item.Children[i].Children == null || item.Children[i].Children.Count == 0))
                    {
                        item.RemoveChild(item.Children[i]);
                    }
                }
            }
        }

        public void UnloadImportItems(ProjectItem importItemsRoot)
        {
            if(importItemsRoot == null)
            {
                Debug.LogWarning("importItemsRoot == null");
                return;
            }

            ImportItem[] importItems = importItemsRoot.Flatten(true).OfType<ImportItem>().ToArray();
            for (int i = 0; i < importItems.Length; ++i)
            {
                if (importItems[i].Object != null)
                {
                    int ordinal = m_assetDB.ToOrdinal(importItems[i].ItemID);
                    if (!m_assetDB.IsLibraryLoaded(ordinal))
                    {
                        if (m_assetDB.IsBundledLibrary(ordinal))
                        {
                            DestroyImmediate(importItems[i].Object, true);
                            importItems[i].Object = null;
                        }
                        else if(m_assetDB.IsBuiltinLibrary(ordinal) || m_assetDB.IsSceneLibrary(ordinal) || m_assetDB.IsStaticLibrary(ordinal))
                        {
                            UnityObject uo = importItems[i].Object;
                            if(!(uo is GameObject) && !(uo is Component))
                            {
                                Resources.UnloadAsset(uo);
                            }
                            importItems[i].Object = null;
                        }
                    }
                    else
                    {
                        importItems[i].Object = null;
                    }
                }
            }
        }

        /// <summary>
        /// Import assets
        /// </summary>
        /// <param name="importItems"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<AssetItem[]> Import(ImportItem[] importItems, ProjectEventHandler<AssetItem[]> callback)
        {
            for (int i = 0; i < importItems.Length; ++i)
            {
                if (importItems[i].Preview == null)
                {
                    Debug.LogWarning("Preview is null. Import item: " + importItems[i].Name + " Id: " + importItems[i].ItemID);
                }

                Debug.Assert(importItems[i].Object == null);
            }

            ProjectAsyncOperation<AssetItem[]> ao = new ProjectAsyncOperation<AssetItem[]>();

            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _Import(importItems, callback, ao));
            }
            else
            {
                IsBusy = true;
                _Import(importItems, callback, ao);
            }
            
            return ao;
        }

        private void _Import(ImportItem[] importItems, ProjectEventHandler<AssetItem[]> callback, ProjectAsyncOperation<AssetItem[]> ao)
        {
            HashSet<int> assetLibraryIds = new HashSet<int>();
            
            if (m_root == null)
            {
                Error error = new Error(Error.E_InvalidOperation);
                error.ErrorText = "Unable to load asset library. Open project first";
                RaiseImportAssetsCompletedCallback(error, null, callback, ao);
                return;
            }

            for (int i = 0; i < importItems.Length; ++i)
            {
                ImportItem importItem = importItems[i];

                if (m_typeMap.ToType(importItem.TypeGuid) == null)
                {
                    Error error = new Error(Error.E_InvalidOperation);
                    error.ErrorText = "Type of import item is invalid";
                    RaiseImportAssetsCompletedCallback(error, null, callback, ao);
                    return;
                }

                if (!assetLibraryIds.Contains(m_assetDB.ToOrdinal(importItem.ItemID)))
                {
                    assetLibraryIds.Add(m_assetDB.ToOrdinal(importItem.ItemID));
                }

                if (importItem.Parts != null)
                {
                    for (int p = 0; p < importItem.Parts.Length; ++p)
                    {
                        PrefabPart part = importItem.Parts[p];
                        if (m_typeMap.ToType(part.TypeGuid) == null)
                        {
                            Error error = new Error(Error.E_InvalidOperation);
                            error.ErrorText = "Type of import item part is invalid";
                            RaiseImportAssetsCompletedCallback(error, null, callback, ao);
                            return;
                        }

                        if (!assetLibraryIds.Contains(m_assetDB.ToOrdinal(part.PartID)))
                        {
                            assetLibraryIds.Add(m_assetDB.ToOrdinal(part.PartID));
                        }
                    }
                }
            }

            if(assetLibraryIds.Count == 0)
            {
                RaiseImportAssetsCompletedCallback(new Error(Error.OK), null, callback, ao);
                return;
            }

            if(assetLibraryIds.Count > 1)
            {
                Error error = new Error(Error.E_InvalidOperation);
                error.ErrorText = "Unable to import more then one AssetLibrary";
                RaiseImportAssetsCompletedCallback(error, null, callback, ao);
                return;
            }

            int ordinal = assetLibraryIds.First();

            if(m_assetDB.IsLibraryLoaded(ordinal))
            {
                CompleteImportAssets(importItems, ordinal, ao, false, callback);
            }
            else
            {
                LoadLibrary(ordinal, true, true, loaded =>
                {
                    if (!loaded)
                    {
                        Error error = new Error(Error.E_NotFound);
                        error.ErrorText = "Unable to load library with ordinal " + ordinal;
                        RaiseImportAssetsCompletedCallback(error, null, callback, ao);
                        return;
                    }

                    CompleteImportAssets(importItems, ordinal, ao, true, callback);
                });
            }
        }

        private void CompleteImportAssets(ImportItem[] importItems, int ordinal, ProjectAsyncOperation<AssetItem[]> ao, bool unloadWhenDone, ProjectEventHandler<AssetItem[]> callback)
        {
            AssetItem[] assetItems = new AssetItem[importItems.Length];
            object[] objects = new object[importItems.Length];

            HashSet<string> removePathHs = new HashSet<string>();
            for (int i = 0; i < importItems.Length; ++i)
            {
                ImportItem importItem = importItems[i];
                ProjectItem parent = null;
                AssetItem assetItem;
                if (m_idToAssetItem.TryGetValue(importItem.ItemID, out assetItem))
                {
                    parent = assetItem.Parent;

                    string path = assetItem.ToString();
                    if(!removePathHs.Contains(path))
                    {
                        removePathHs.Add(path);
                    }
                }

                if (importItem.Parts != null)
                {
                    for (int p = 0; p < importItem.Parts.Length; ++p)
                    {
                        PrefabPart part = importItem.Parts[p];
                        AssetItem partAssetItem;
                        if (m_idToAssetItem.TryGetValue(part.PartID, out partAssetItem))
                        {
                            string path = partAssetItem.ToString();
                            if (!removePathHs.Contains(path))
                            {
                                removePathHs.Add(path);
                            }
                            if(assetItem != partAssetItem)
                            {
                                RemoveAssetItem(partAssetItem);
                            }
                        }
                    }
                }

                if(assetItem == null)
                {
                    assetItem = new AssetItem();
                    assetItem.ItemID = importItem.ItemID;
                    m_idToAssetItem.Add(assetItem.ItemID, assetItem);
                }
                else
                {
                    assetItem.ItemID = importItem.ItemID;
                }

                assetItem.Name = PathHelper.GetUniqueName(importItem.Name, importItem.Ext, importItem.Parent.Children.Where(child => child != importItem).Select(child => child.NameExt).ToList());
                assetItem.Ext = importItem.Ext;
                assetItem.Parts = importItem.Parts;
                assetItem.TypeGuid = importItem.TypeGuid;
                assetItem.Preview = importItem.Preview;

                if (assetItem.Parts != null)
                {
                    for (int p = 0; p < assetItem.Parts.Length; ++p)
                    {
                        PrefabPart part = assetItem.Parts[p];
                        if(!m_idToAssetItem.ContainsKey(part.PartID))
                        {
                            m_idToAssetItem.Add(part.PartID, assetItem);
                        }
                    }
                }

                if (parent == null)
                {
                    parent = m_root.Get(importItem.Parent.ToString(), true);
                }

                parent.AddChild(assetItem);
                assetItems[i] = assetItem;

                UnityObject obj = m_assetDB.FromID<UnityObject>(importItem.ItemID);
                objects[i] = obj;
                if(obj != null)
                {
                    if (m_assetDB.TryToReplaceID(obj, importItem.ItemID))
                    {
                        Debug.Log("Object  " + obj + " is present in asset db. This means that it was already loaded from different asset library (SceneAssetLibrary for example). -> PersistentID replaced with " + importItem.ItemID);
                    }
                }
            }

            m_storage.Delete(m_projectPath, removePathHs.ToArray(), deleteError =>
            {
                if(deleteError.HasError)
                {
                    RaiseImportAssetsCompletedCallback(deleteError, null, callback, ao);
                }
                else
                {
                    ProjectAsyncOperation<AssetItem[]> saveAo = new ProjectAsyncOperation<AssetItem[]>();
                    _Save(null, null, objects, null, assetItems, (saveError, savedAssetItems) =>
                    {
                        if(unloadWhenDone)
                        {
                            m_assetDB.UnloadLibrary(ordinal);

                            AssetBundle assetBundle;
                            if(m_ordinalToAssetBundle.TryGetValue(ordinal, out assetBundle))
                            {
                                assetBundle.Unload(true);
                                m_ordinalToAssetBundle.Remove(ordinal);
                            }
                        }
                        RaiseImportAssetsCompletedCallback(saveError, savedAssetItems, callback, ao);
                    }, 
                    saveAo, () => { });
                }
            });
        }

        private void RaiseImportAssetsCompletedCallback(Error error, AssetItem[] assetItems, ProjectEventHandler<AssetItem[]> callback, ProjectAsyncOperation<AssetItem[]> ao)
        {
            if (callback != null)
            {
                callback(error, assetItems);
            }
           
            ao.Result = assetItems;
            ao.Error = error;
            ao.IsCompleted = true;

            if (ImportCompleted != null)
            {
                ImportCompleted(error, assetItems);
            }

            IsBusy = false;
        }

        private void RemoveAssetItem(AssetItem assetItem)
        {
            if (assetItem.Parent != null)
            {
                assetItem.Parent.RemoveChild(assetItem);
            }
            m_idToAssetItem.Remove(assetItem.ItemID);
            if (assetItem.Parts != null)
            {
                for (int p = 0; p < assetItem.Parts.Length; ++p)
                {
                    int partID = m_assetDB.ToInt(assetItem.Parts[p].PartID);
                    m_assetDB.UnregisterDynamicResource(partID);

                    AssetItem partAssetItem;
                    if (m_idToAssetItem.TryGetValue(assetItem.Parts[p].PartID, out partAssetItem))
                    {
                        Debug.Assert(assetItem == partAssetItem);
                        m_idToAssetItem.Remove(assetItem.Parts[p].PartID);
                    }
                }
            }

            if (m_assetDB.IsDynamicResourceID(assetItem.ItemID))
            {
                UnityObject obj = m_assetDB.FromID<UnityObject>(assetItem.ItemID);
                int itemID = m_assetDB.ToInt(assetItem.ItemID);
                m_assetDB.UnregisterDynamicResource(itemID);
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

   
        private void RemoveFolder(ProjectItem projectItem)
        {
            if (projectItem.Children != null)
            {
                for (int i = projectItem.Children.Count - 1; i >= 0; --i)
                {
                    ProjectItem child = projectItem.Children[i];
                    if (child is AssetItem)
                    {
                        RemoveAssetItem((AssetItem)child);
                    }
                    else
                    {
                        RemoveFolder(child);
                    }
                }
            }

            if (projectItem.Parent != null)
            {
                projectItem.Parent.RemoveChild(projectItem);
            }
        }

        /// <summary>
        /// Create folder
        /// </summary>
        /// <param name="projectItem"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<ProjectItem> CreateFolder(ProjectItem projectItem, ProjectEventHandler<ProjectItem> callback = null)
        {
            if (m_root == null)
            {
                throw new InvalidOperationException("Project is not opened. Use OpenProject method");
            }
            if (!projectItem.IsFolder)
            {
                throw new InvalidOperationException("is not a folder");
            }

            ProjectAsyncOperation<ProjectItem> ao = new ProjectAsyncOperation<ProjectItem>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _Create(projectItem, callback, ao));
            }
            else
            {
                IsBusy = true;
                _Create(projectItem, callback, ao);
            }
            return ao;
        }

        private void _Create(ProjectItem projectItem, ProjectEventHandler<ProjectItem> callback, ProjectAsyncOperation<ProjectItem> ao)
        {
            m_storage.Create(m_projectPath, new[] { projectItem.Parent.ToString() }, new[] { projectItem.NameExt }, error =>
            {
                if (callback != null)
                {
                    callback(error, projectItem);
                }

                ao.Result = projectItem;
                ao.Error = error;
                ao.IsCompleted = true;

                if (CreateCompleted != null)
                {
                    CreateCompleted(error, projectItem);
                }

                IsBusy = false;
            });
        }

        /// <summary>
        /// Rename asset or folder
        /// </summary>
        /// <param name="projectItem"></param>
        /// <param name="oldName"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<ProjectItem> Rename(ProjectItem projectItem, string oldName, ProjectEventHandler<ProjectItem> callback)
        {
            if (m_root == null)
            {
                throw new InvalidOperationException("Project is not opened. Use OpenProject method");
            }

            ProjectAsyncOperation<ProjectItem> ao = new ProjectAsyncOperation<ProjectItem>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _Rename(projectItem, oldName, callback, ao));
            }
            else
            {
                IsBusy = true;
                _Rename(projectItem, oldName, callback, ao);
            }
            return ao;
        }

        private void _Rename(ProjectItem projectItem, string oldName, ProjectEventHandler<ProjectItem> callback, ProjectAsyncOperation<ProjectItem> ao)
        {
            UnityObject obj = m_assetDB.FromID<UnityObject>(projectItem.ItemID);
            if (obj != null)
            {
                obj.name = projectItem.Name;
            }

            m_storage.Rename(m_projectPath, new[] { projectItem.Parent.ToString() }, new[] { oldName + projectItem.Ext }, new[] { projectItem.NameExt }, error =>
            {
                if (callback != null)
                {
                    callback(error, projectItem);
                }

                ao.Result = projectItem;
                ao.Error = error;
                ao.IsCompleted = true;

                if (RenameCompleted != null)
                {
                    RenameCompleted(error, projectItem);
                }

                IsBusy = false;
            });
        }

        /// <summary>
        /// Move assets and folders
        /// </summary>
        /// <param name="projectItems"></param>
        /// <param name="target"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<ProjectItem[], ProjectItem[]> Move(ProjectItem[] projectItems, ProjectItem target, ProjectEventHandler<ProjectItem[], ProjectItem[]> callback)
        {
            if (m_root == null)
            {
                throw new InvalidOperationException("Project is not opened. Use OpenProject method");
            }

            ProjectAsyncOperation<ProjectItem[], ProjectItem[]> ao = new ProjectAsyncOperation<ProjectItem[], ProjectItem[]>();

            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _Move(projectItems, target, callback, ao));
            }
            else
            {
                IsBusy = true;
                _Move(projectItems, target, callback, ao);
            }
            return ao;
        }

        private void _Move(ProjectItem[] projectItems, ProjectItem target, ProjectEventHandler<ProjectItem[], ProjectItem[]> callback, ProjectAsyncOperation<ProjectItem[], ProjectItem[]> ao)
        {
            ProjectItem[] oldParents = projectItems.Select(item => item.Parent).ToArray();
            m_storage.Move(m_projectPath, projectItems.Select(p => p.Parent.ToString()).ToArray(), projectItems.Select(p => p.NameExt).ToArray(), target.ToString(), error =>
            {
                if (!error.HasError)
                {
                    ProjectItem targetFolder = m_root.Get(target.ToString());

                    foreach (ProjectItem item in projectItems)
                    {
                        targetFolder.AddChild(item);
                    }
                }

                if (callback != null)
                {
                    callback(error, projectItems, oldParents);
                }

                ao.Result = projectItems;
                ao.Result2 = oldParents;
                ao.Error = error;
                ao.IsCompleted = true;

                if (MoveCompleted != null)
                {
                    MoveCompleted(error, projectItems, oldParents);
                }

                IsBusy = false;
            });
        }

        /// <summary>
        /// Delete assets and folders
        /// </summary>
        /// <param name="projectItems"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<ProjectItem[]> Delete(ProjectItem[] projectItems, ProjectEventHandler<ProjectItem[]> callback)
        {
            if (m_root == null)
            {
                throw new InvalidOperationException("Project is not opened. Use OpenProject method");
            }
            ProjectAsyncOperation<ProjectItem[]> ao = new ProjectAsyncOperation<ProjectItem[]>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _Delete(projectItems, callback, ao));
            }
            else
            {
                IsBusy = true;
                _Delete(projectItems, callback, ao);
            }
            return ao;
        }

        private void _Delete(ProjectItem[] projectItems, ProjectEventHandler<ProjectItem[]> callback, ProjectAsyncOperation<ProjectItem[]> ao)
        {
            m_storage.Delete(m_projectPath, projectItems.Select(p => p.ToString()).ToArray(), error =>
            {
                if (BeforeDeleteCompleted != null)
                {
                    BeforeDeleteCompleted(error, projectItems);
                }

                if (!error.HasError)
                {
                    foreach (ProjectItem item in projectItems)
                    {
                        if (item is AssetItem)
                        {
                            RemoveAssetItem((AssetItem)item);
                        }
                        else
                        {
                            RemoveFolder(item);
                        }
                    }
                }

                if (callback != null)
                {
                    callback(error, projectItems);
                }

                ao.Error = error;
                ao.Result = projectItems;
                ao.IsCompleted = true;

                if (DeleteCompleted != null)
                {
                    DeleteCompleted(error, projectItems);
                }

                IsBusy = false;
            });
        }

        /// <summary>
        /// Get List of all asset bundles could be imported and loaded
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ProjectAsyncOperation<string[]> GetAssetBundles(ProjectEventHandler<string[]> callback = null)
        {
            if (m_root == null)
            {
                throw new InvalidOperationException("Project is not opened. Use OpenProject method");
            }

            ProjectAsyncOperation<string[]> ao = new ProjectAsyncOperation<string[]>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _GetAssetBundles(callback, ao));
            }
            else
            {
                IsBusy = true;
                _GetAssetBundles(callback, ao);
            }
            
            return ao;
        }

        private void _GetAssetBundles(ProjectEventHandler<string[]> callback, ProjectAsyncOperation<string[]> ao)
        {
            IOC.Resolve<IAssetBundleLoader>().GetAssetBundles(result =>
            {
                Error error = new Error(Error.OK);
                if (callback != null)
                {
                    callback(error, result);
                }

                ao.Error = error;
                ao.Result = result;
                ao.IsCompleted = true;

                IsBusy = false;
            });
        }

        public Dictionary<int, string> GetStaticAssetLibraries()
        {
            if (m_ordinalToStaticAssetLibrary != null)
            {
                return m_ordinalToStaticAssetLibrary;
            }

            AssetLibrariesListAsset staticAssetLibraries;
            if (m_staticAssetLibraries != null)
            {
                staticAssetLibraries = m_staticAssetLibraries;
            }
            else
            {
                staticAssetLibraries = Resources.Load<AssetLibrariesListAsset>("Lists/AssetLibrariesList");
            }

            if (staticAssetLibraries == null)
            {
                return new Dictionary<int, string>();
            }

            m_ordinalToStaticAssetLibrary = new Dictionary<int, string>();
            for (int i = 0; i < staticAssetLibraries.List.Count; ++i)
            {
                AssetLibraryListEntry entry = staticAssetLibraries.List[i];
                if (!m_ordinalToStaticAssetLibrary.ContainsKey(entry.Ordinal))
                {
                    m_ordinalToStaticAssetLibrary.Add(entry.Ordinal, entry.Library.Remove(entry.Library.LastIndexOf(".asset")));
                }
            }

            return m_ordinalToStaticAssetLibrary;
        }

        public ProjectAsyncOperation<T[]> GetValues<T>(string searchPattern, ProjectEventHandler<T[]> callback = null) where T : new()
        {
            if (m_root == null)
            {
                throw new InvalidOperationException("Project is not opened. Use OpenProject method");
            }

            ProjectAsyncOperation<T[]> ao = new ProjectAsyncOperation<T[]>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _GetValues(searchPattern, callback, ao));
            }
            else
            {
                IsBusy = true;
                _GetValues(searchPattern, callback, ao);
            }

            return ao;
        }

        private void _GetValues<T>(string searchPattern, ProjectEventHandler<T[]> callback, ProjectAsyncOperation<T[]> ao) where T : new()
        {
            Type objType = typeof(T);
            Type persistentType = m_typeMap.ToPersistentType(objType);
            if (persistentType == null)
            {
                Debug.LogWarningFormat(string.Format("PersistentClass for {0} does not exist. To create or edit persistent classes click Tools->Runtime SaveLoad->Persistent Classes->Edit.", objType), "obj");
                RaiseGetValuesCompleted(new T[0], callback, ao, new Error(Error.E_NotFound));
                return;
            }

            m_storage.GetValues(m_projectPath, searchPattern, persistentType, (getValuesError, persistentObjects) =>
            {
                if (getValuesError.HasError)
                {
                    RaiseGetValuesCompleted(new T[0], callback, ao, getValuesError);
                    return;
                }

                T[] result = new T[persistentObjects.Length];

                if (typeof(T).IsSubclassOf(typeof(ScriptableObject)))
                {
                    for (int i = 0; i < result.Length; ++i)
                    {
                        result[i] = (T)Convert.ChangeType(ScriptableObject.CreateInstance(typeof(T)), typeof(T));
                        persistentObjects[i].WriteTo(result[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < result.Length; ++i)
                    {
                        result[i] = new T();
                        persistentObjects[i].WriteTo(result[i]);
                    }
                }
                
                RaiseGetValuesCompleted(result, callback, ao, getValuesError);
            });
        }

        private void RaiseGetValuesCompleted<T>(T[] result, ProjectEventHandler<T[]> callback, ProjectAsyncOperation<T[]> ao, Error error)
        {
            if (callback != null)
            {
                callback(error, result);
            }

            ao.Result = result;
            ao.Error = error;
            ao.IsCompleted = true;
            IsBusy = false;
        }


        public ProjectAsyncOperation<T> GetValue<T>(string key, ProjectEventHandler<T> callback = null) where T : new()
        {
            if (m_root == null)
            {
                throw new InvalidOperationException("Project is not opened. Use OpenProject method");
            }

            ProjectAsyncOperation<T> ao = new ProjectAsyncOperation<T>();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _GetValue(key, callback, ao));
            }
            else
            {
                IsBusy = true;
                _GetValue(key,  callback, ao);
            }

            return ao;
        }

        private void _GetValue<T>(string key, ProjectEventHandler<T> callback, ProjectAsyncOperation<T> ao) where T : new()
        {
            Type objType = typeof(T);
            Type persistentType = m_typeMap.ToPersistentType(objType);
            if (persistentType == null)
            {
                Debug.LogWarningFormat(string.Format("PersistentClass for {0} does not exist. To create or edit persistent classes click Tools->Runtime SaveLoad->Persistent Classes->Edit.", objType), "obj");
                RaiseGetValueCompleted(default(T), callback, ao, new Error(Error.E_NotFound));
                return;
            }

            m_storage.GetValue(m_projectPath, key + GetExt(objType), persistentType, (getValueError, persistentObject) =>
            {
                if (getValueError.HasError)
                {
                    RaiseGetValueCompleted(default(T), callback, ao, getValueError);
                    return;
                }

                object result = null;
                if (typeof(T).IsSubclassOf(typeof(ScriptableObject)))
                {
                    result = ScriptableObject.CreateInstance(typeof(T));
                }
                else
                {
                    result = new T();
                }
                 
                persistentObject.WriteTo(result);
                RaiseGetValueCompleted((T)result, callback, ao, getValueError);
            });
        }

        private void RaiseGetValueCompleted<T>(T result, ProjectEventHandler<T> callback, ProjectAsyncOperation<T> ao, Error error)
        {
            if (callback != null)
            {
                callback(error, result);
            }

            ao.Result = result;
            ao.Error = error;
            ao.IsCompleted = true;
            IsBusy = false;
        }

        public ProjectAsyncOperation SetValue<T>(string key, T obj, ProjectEventHandler callback = null)
        {
            if (m_root == null)
            {
                throw new InvalidOperationException("Project is not opened. Use OpenProject method");
            }

            ProjectAsyncOperation ao = new ProjectAsyncOperation();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _SetValue(key, obj, callback, ao));
            }
            else
            {
                IsBusy = true;
                _SetValue(key, obj, callback, ao);
            }

            return ao;
        }

        private void _SetValue<T>(string key, T obj, ProjectEventHandler callback, ProjectAsyncOperation ao)
        {
            Type objType = obj.GetType();
            Type persistentType = m_typeMap.ToPersistentType(objType);
            if (persistentType == null)
            {
                Debug.LogWarningFormat(string.Format("PersistentClass for {0} does not exist. To create or edit persistent classes click Tools->Runtime SaveLoad->Persistent Classes->Edit.", obj.GetType()), "obj");
                RaiseSetValueCompleted(callback, ao, new Error(Error.E_NotFound));
                return;
            }

            PersistentObject persistentObject = (PersistentObject)Activator.CreateInstance(persistentType);
            persistentObject.ReadFrom(obj);
            m_storage.SetValue(m_projectPath, key + GetExt(objType), persistentObject, (setValueError) =>
            {
                if (setValueError.HasError)
                {
                    RaiseSetValueCompleted(callback, ao, setValueError);
                    return;
                }

                RaiseSetValueCompleted(callback, ao, new Error(Error.OK));
            });
        }

        private void RaiseSetValueCompleted(ProjectEventHandler callback, ProjectAsyncOperation ao, Error error)
        {
            if(callback != null)
            {
                callback(error);
            }
            
            ao.Error = error;
            ao.IsCompleted = true;

            IsBusy = false;
        }

        public ProjectAsyncOperation DeleteValue<T>(string key, ProjectEventHandler callback = null)
        {
            if (m_root == null)
            {
                throw new InvalidOperationException("Project is not opened. Use OpenProject method");
            }

            ProjectAsyncOperation ao = new ProjectAsyncOperation();
            if (IsBusy)
            {
                m_actionsQueue.Enqueue(() => _DeleteValue<T>(key, callback, ao));
            }
            else
            {
                IsBusy = true;
                _DeleteValue<T>(key, callback, ao);
            }
            return ao;
        }


        private void _DeleteValue<T>(string key, ProjectEventHandler callback, ProjectAsyncOperation ao)
        {
            Type objType = typeof(T);
            m_storage.DeleteValue(m_projectPath, key + GetExt(objType), (deleteValueError) =>
            {
                if (deleteValueError.HasError)
                {
                    RaiseDeleteValueCompleted(callback, ao, deleteValueError);
                    return;
                }

                RaiseDeleteValueCompleted(callback, ao, new Error(Error.OK));
            });
        }

        private void RaiseDeleteValueCompleted(ProjectEventHandler callback, ProjectAsyncOperation ao, Error error)
        {
            if (callback != null)
            {
                callback(error);
            }

            ao.Error = error;
            ao.IsCompleted = true;
            IsBusy = false;
        }
    }
}
