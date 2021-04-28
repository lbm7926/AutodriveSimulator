using Battlehub.RTCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace Battlehub.RTSL
{     
    public interface IIDMap
    {
        long NullID { get; }

        bool IsNullID(long id);
        bool IsInstanceID(long id);
        bool IsStaticResourceID(long id);
        bool IsStaticFolderID(long id);
        bool IsDynamicResourceID(long id);
        bool IsDynamicFolderID(long id);
        bool IsSceneID(long id);
        bool IsResourceID(long id);

        int ToOrdinal(long id);
        int ToOrdinal(int id);

        int ToInt(long id);

        long ToStaticResourceID(int ordinal, int id);
        long ToStaticFolderID(int ordinal, int id);
        long ToDynamicResourceID(int ordinal, int id);
        long ToDynamicFolderID(int ordinal, int id);
        long ToSceneID(int ordinal, int id);

        bool IsMapped(UnityObject uo);
        bool TryToReplaceID(UnityObject uo, long persistentID);
        long ToID(UnityObject uo);
        long[] ToID(UnityObject[] uo);
        long[] ToID<T>(List<T> uo) where T : UnityObject;
        long[] ToID<T>(IEnumerable<T> uo) where T : UnityObject;
        bool IsMapped(long id);
        T FromID<T>(long id) where T : UnityObject;
        T[] FromID<T>(long[] id) where T : UnityObject;
    }


    public interface IAssetDB : IIDMap
    {
        bool IsStaticLibrary(int ordinal);
        bool IsSceneLibrary(int ordinal);
        bool IsBuiltinLibrary(int ordinal);
        bool IsBundledLibrary(int ordinal);
        bool IsDynamicLibrary(int ordinal);

        void RegisterSceneObjects(Dictionary<int, UnityObject> idToObj);
        void UnregisterSceneObjects();

        void RegisterDynamicResource(int persistentID, UnityObject obj);
        void UnregisterDynamicResource(int persistentID);
        void RegisterDynamicResources(Dictionary<int, UnityObject> idToObj);
        void UnregisterDynamicResources(Dictionary<int, UnityObject> idToObj);
        
        void UnregisterDynamicResources();
        UnityObject[] GetDynamicResources();
        
        bool IsLibraryLoaded(int ordinal);
        bool AddLibrary(AssetLibraryAsset assetLib, int ordinal, bool IIDtoObj, bool PIDtoObj);
        void LoadLibrary(string assetLibrary, int ordinal, bool loadIIDtoPID, bool loadPIDtoObj, Action<bool> callback);
        void UnloadLibrary(int ordinal);
        void UnloadLibraries();
        
        AsyncOperation UnloadUnusedAssets(Action<AsyncOperation> completedCallback = null);   
    }

    public class AssetDB : IAssetDB
    {
        private readonly HashSet<AssetLibraryAsset> m_loadedLibraries = new HashSet<AssetLibraryAsset>();
        private readonly Dictionary<int, AssetLibraryAsset> m_ordinalToLib = new Dictionary<int, AssetLibraryAsset>();

        private MappingInfo m_mapping = new MappingInfo();

        private readonly Dictionary<int, UnityObject> m_persistentIDToSceneObject = new Dictionary<int, UnityObject>();
        private readonly Dictionary<int, int> m_sceneObjectIDToPersistentID = new Dictionary<int, int>();

        private readonly Dictionary<int, UnityObject> m_persistentIDToDynamicResource = new Dictionary<int, UnityObject>();
        private readonly Dictionary<int, int> m_dynamicResourceIDToPersistentID = new Dictionary<int, int>();

        public bool IsStaticLibrary(int ordinal)
        {
            return AssetLibraryInfo.STATICLIB_FIRST <= ordinal && ordinal <= AssetLibraryInfo.STATICLIB_LAST;
        }

        public bool IsSceneLibrary(int ordinal)
        {
            return AssetLibraryInfo.SCENELIB_FIRST <= ordinal && ordinal <= AssetLibraryInfo.SCENELIB_LAST;
        }

        public bool IsBuiltinLibrary(int ordinal)
        {
            return AssetLibraryInfo.BUILTIN_FIRST <= ordinal && ordinal <= AssetLibraryInfo.BUILTIN_LAST;
        }

        public bool IsBundledLibrary(int ordinal)
        {
            return AssetLibraryInfo.BUNDLEDLIB_FIRST <= ordinal && ordinal <= AssetLibraryInfo.BUNDLEDLIB_LAST;
        }

        public bool IsDynamicLibrary(int ordinal)
        {
            return AssetLibraryInfo.DYNAMICLIB_FIRST <= ordinal && ordinal <= AssetLibraryInfo.DYNAMICLIB_LAST;
        }

        public void RegisterSceneObjects(Dictionary<int, UnityObject> idToObj)
        {
            if (m_persistentIDToSceneObject.Count != 0)
            {
                Debug.LogWarning("scene objects were not unregistered");
            }

            foreach (KeyValuePair<int, UnityObject> kvp in idToObj)
            {
                if (!m_persistentIDToSceneObject.ContainsKey(kvp.Key))
                {
                    m_persistentIDToSceneObject.Add(kvp.Key, kvp.Value);
                }

                if(kvp.Value != null)
                {
                    int instanceId = kvp.Value.GetInstanceID();
                    if (!m_sceneObjectIDToPersistentID.ContainsKey(instanceId))
                    {
                        m_sceneObjectIDToPersistentID.Add(instanceId, kvp.Key);
                    }
                }
            }
        }

        public void UnregisterSceneObjects()
        {
            m_persistentIDToSceneObject.Clear();
            m_sceneObjectIDToPersistentID.Clear();
        }

        public void RegisterDynamicResource(int persistentID, UnityObject obj)
        {
            m_persistentIDToDynamicResource[persistentID] = obj;
            if (obj != null)
            {
                m_dynamicResourceIDToPersistentID[obj.GetInstanceID()] = persistentID;
            }
        }
        public void RegisterDynamicResources(Dictionary<int, UnityObject> idToObj)
        {
            foreach (KeyValuePair<int, UnityObject> kvp in idToObj)
            {
                m_persistentIDToDynamicResource[kvp.Key] = kvp.Value;
                if (kvp.Value != null)
                {
                    m_dynamicResourceIDToPersistentID[kvp.Value.GetInstanceID()] = kvp.Key;
                }
            }
        }

        public void UnregisterDynamicResources(Dictionary<int, UnityObject> idToObj)
        {
            foreach (KeyValuePair<int, UnityObject> kvp in idToObj)
            {
                m_persistentIDToDynamicResource.Remove(kvp.Key);
                if (kvp.Value != null)
                {
                    m_dynamicResourceIDToPersistentID.Remove(kvp.Value.GetInstanceID());
                }
            }
        }

        public void UnregisterDynamicResource(int persistentID)
        {
            UnityObject obj;
            if(m_persistentIDToDynamicResource.TryGetValue(persistentID, out obj))
            {
                m_persistentIDToDynamicResource.Remove(persistentID);
                m_dynamicResourceIDToPersistentID.Remove(obj.GetInstanceID());
            }
        }

        public void UnregisterDynamicResources()
        {
            m_persistentIDToDynamicResource.Clear();
            m_dynamicResourceIDToPersistentID.Clear();
        }

        public UnityObject[] GetDynamicResources()
        {
            return m_persistentIDToDynamicResource.Values.Where(o => o != null).ToArray();
        }

        public bool IsLibraryLoaded(int ordinal)
        {
            return m_ordinalToLib.ContainsKey(ordinal);
        }

        public bool AddLibrary(AssetLibraryAsset assetLib, int ordinal, bool IIDtoObj, bool PIDtoObj)
        {
            if (m_ordinalToLib.ContainsKey(ordinal))
            {
                Debug.LogWarningFormat("Asset Library with ordinal {0} already loadeded", assetLib.Ordinal);
                return false;
            }

            if (m_loadedLibraries.Contains(assetLib))
            {
                Debug.LogWarning("Asset Library already added");
                return false;
            }

            assetLib.Ordinal = ordinal;
            m_loadedLibraries.Add(assetLib);
            m_ordinalToLib.Add(ordinal, assetLib);
            LoadMapping(ordinal, IIDtoObj, PIDtoObj);

            return true;
        }

        public void LoadLibrary(string assetLibrary, int ordinal, bool loadIIDtoPID, bool loadPIDtoObj, Action<bool> callback)
        {
            if (m_ordinalToLib.ContainsKey(ordinal))
            {
                Debug.LogWarningFormat("Asset Library {0} with this same ordinal {1} already loaded", m_ordinalToLib[ordinal].name, ordinal);
                callback(false);
                return;
            }

            ResourceRequest request = Resources.LoadAsync<AssetLibraryAsset>(assetLibrary);
            Action<AsyncOperation> completed = null;
            completed = ao =>
            {
                AssetLibraryAsset assetLib = (AssetLibraryAsset)request.asset;
                if (assetLib == null)
                {
                    if(IsBuiltinLibrary(ordinal))
                    {
                        if (ordinal - AssetLibraryInfo.BUILTIN_FIRST == 0)
                        {
                            Debug.LogWarningFormat("Asset Library was not found : {0}. Click Tools->Runtime SaveLoad2->Libraries->Create Built-in asset library.", assetLibrary);
                        }
                    }
                    else if(IsSceneLibrary(ordinal))
                    {
                        if (ordinal - AssetLibraryInfo.SCENELIB_FIRST == 0)
                        {
                            Debug.LogWarningFormat("Asset Library was not found : {0}. Click Tools->Runtime SaveLoad2->Libraries->Collect Scene Dependencies.", assetLibrary);
                        }
                    }
                    else
                    {
                        Debug.LogWarningFormat("Asset Library was not found : {0}", assetLibrary);
                    }
                    
                    callback(false);
                    return;
                }
                AddLibrary(assetLib, ordinal, loadIIDtoPID, loadPIDtoObj);
                callback(true);
                request.completed -= completed;
            };
            request.completed += completed;
        } 

        public void UnloadLibrary(int ordinal)
        {
            AssetLibraryAsset assetLib;
            if(m_ordinalToLib.TryGetValue(ordinal, out assetLib))
            {
                UnloadMapping(ordinal);
                m_loadedLibraries.Remove(assetLib);
                m_ordinalToLib.Remove(ordinal);
                if(!IsBundledLibrary(assetLib.Ordinal))
                {
                    Resources.UnloadAsset(assetLib);
                }
            }
        }

        public void UnloadLibraries()
        {
            foreach (AssetLibraryAsset assetLibrary in m_loadedLibraries)
            {
                if(!IsBundledLibrary(assetLibrary.Ordinal))
                {
                    Resources.UnloadAsset(assetLibrary);
                }
            }
            
            m_ordinalToLib.Clear();
            m_loadedLibraries.Clear();
            UnloadMappings();
        }

        public AsyncOperation UnloadUnusedAssets(Action<AsyncOperation> completedCallback = null)
        {
            AsyncOperation operation = Resources.UnloadUnusedAssets();

            if(completedCallback != null)
            {
                if(operation.isDone)
                {
                    completedCallback(operation);
                }
                else
                {
                    Action<AsyncOperation> onCompleted = null;
                    onCompleted = ao =>
                    {
                        operation.completed -= onCompleted;
                        completedCallback(operation);
                    };
                    operation.completed += onCompleted;
                }
            }
           
            return operation;
        }

        private void LoadMapping(int ordinal, bool IIDtoPID, bool PIDtoObj)
        {
            AssetLibraryAsset assetLib;
            if(m_ordinalToLib.TryGetValue(ordinal, out assetLib))
            {
                assetLib.LoadIDMappingTo(m_mapping, IIDtoPID, PIDtoObj);
            }
            else
            {
                throw new ArgumentException(string.Format("Unable to find assetLibrary with ordinal = {0}", ordinal), "ordinal");
            }
        }

        private void UnloadMapping(int ordinal)
        {
            AssetLibraryAsset assetLib;
            if (m_ordinalToLib.TryGetValue(ordinal, out assetLib))
            {
                assetLib.UnloadIDMappingFrom(m_mapping);
            }
            else
            {
                throw new ArgumentException(string.Format("Unable to find assetLibrary with ordinal = {0}", ordinal), "ordinal");
            }
        }


        private void UnloadMappings()
        {
            m_mapping = new MappingInfo();
        }

        private const long m_nullID = 1L << 32;
        private const long m_instanceIDMask = 1L << 33;
        private const long m_staticResourceIDMask = 1L << 34;
        private const long m_staticFolderIDMask = 1L << 35;
        private const long m_dynamicResourceIDMask = 1L << 36;
        private const long m_dynamicFolderIDMask = 1L << 37;
        private const long m_sceneIDMask = 1L << 38;

        public long NullID { get { return m_nullID; } }

        public bool IsNullID(long id)
        {
            return (id & m_nullID) != 0;
        }

        public bool IsInstanceID(long id)
        {
            return (id & m_instanceIDMask) != 0;
        }

        public bool IsStaticResourceID(long id)
        {
            return (id & m_staticResourceIDMask) != 0;
        }

        public bool IsStaticFolderID(long id)
        {
            return (id & m_staticFolderIDMask) != 0;
        }
        
        public bool IsDynamicResourceID(long id)
        {
            return (id & m_dynamicResourceIDMask) != 0;
        }

        public bool IsDynamicFolderID(long id)
        {
            return (id & m_dynamicFolderIDMask) != 0;
        }

        public bool IsSceneID(long id)
        {
            return (id & m_sceneIDMask) != 0;
        }

        public bool IsResourceID(long id)
        {
            return IsStaticResourceID(id) || IsDynamicResourceID(id);
        }

        public long ToStaticResourceID(int ordinal, int id)
        {
            return ToID(ordinal, id, m_staticResourceIDMask);
        }

        public long ToStaticFolderID(int ordinal, int id)
        {
            return ToID(ordinal, id, m_staticFolderIDMask);
        }

        public long ToDynamicResourceID(int ordinal, int id)
        {
            return ToID(ordinal, id, m_dynamicResourceIDMask);
        }

        public long ToDynamicFolderID(int ordinal, int id)
        {
            return ToID(ordinal, id, m_dynamicFolderIDMask);
        }

        public long ToSceneID(int ordinal, int id)
        {
            return ToID(ordinal, id, m_sceneIDMask);
        }

        private static long ToID(int ordinal, int id, long mask)
        {
            if (id > AssetLibraryInfo.ORDINAL_MASK)
            {
                throw new ArgumentException("id > AssetLibraryInfo.ORDINAL_MASK");
            }

            id = (ordinal << AssetLibraryInfo.ORDINAL_OFFSET) | (AssetLibraryInfo.ORDINAL_MASK & id);
            return mask | (0x00000000FFFFFFFFL & id);
        }

        public int ToOrdinal(long id)
        {
            int intId = (int)(0x00000000FFFFFFFFL & id);
            return (intId >> AssetLibraryInfo.ORDINAL_OFFSET) & AssetLibraryInfo.ORDINAL_MASK;
            
        }
        public int ToOrdinal(int id)
        {
            return (id >> AssetLibraryInfo.ORDINAL_OFFSET) & AssetLibraryInfo.ORDINAL_MASK;
        }

        public bool IsMapped(UnityObject uo)
        {
            if (uo == null)
            {
                return false;
            }

            int instanceID = uo.GetInstanceID();
            int persistentID;
            if (m_mapping.InstanceIDtoPID.TryGetValue(instanceID, out persistentID))
            {
                return true;
            }

            //if (m_sceneObjectIDToPersistentID != null && m_sceneObjectIDToPersistentID.TryGetValue(instanceID, out persistentID))
            //{
            //    return true;
            //}

            if (m_dynamicResourceIDToPersistentID.TryGetValue(instanceID, out persistentID))
            {
                return true;
            }

            return false;
        }


        public bool TryToReplaceID(UnityObject uo, long persistentID)
        {
            if (uo == null)
            {
                return false;
            }

            int instanceID = uo.GetInstanceID();
            if (m_mapping.InstanceIDtoPID.ContainsKey(instanceID))
            {
                int id = ToInt(persistentID);
                if(id != m_mapping.InstanceIDtoPID[instanceID])
                {
                    m_mapping.InstanceIDtoPID[instanceID] = id;
                    return true;
                }
            }

            return false;
        }

        public long ToID(UnityObject uo)
        {
            if(uo == null)
            {
                return m_nullID;
            }

            int instanceID = uo.GetInstanceID();
            int persistentID;
            if(m_mapping.InstanceIDtoPID.TryGetValue(instanceID, out persistentID))
            {
                return m_staticResourceIDMask | (0x00000000FFFFFFFFL & persistentID);
            }
            
            //if(m_sceneObjectIDToPersistentID != null && m_sceneObjectIDToPersistentID.TryGetValue(instanceID, out persistentID))
            //{
            //    return m_instanceIDMask | (0x00000000FFFFFFFFL & persistentID);
            //}

            if(m_dynamicResourceIDToPersistentID.TryGetValue(instanceID, out persistentID))
            {
                return m_dynamicResourceIDMask | (0x00000000FFFFFFFFL & persistentID);
            }

            return m_instanceIDMask | (0x00000000FFFFFFFFL & instanceID);
        }

        public long[] ToID(UnityObject[] uo)
        {
            if(uo == null)
            {
                return null;
            }
            long[] ids = new long[uo.Length];
            for(int i = 0; i < uo.Length; ++i)
            {
                ids[i] = ToID(uo[i]);
            }
            return ids;
        }

        public long[] ToID<T>(List<T> uo) where T : UnityObject
        {
            if (uo == null)
            {
                return null;
            }
            long[] ids = new long[uo.Count];
            for (int i = 0; i < uo.Count; ++i)
            {
                ids[i] = ToID(uo[i]);
            }
            return ids;
        }

        public long[] ToID<T>(IEnumerable<T> uo) where T : UnityObject
        {
            if (uo == null)
            {
                return null;
            }
            List<long> ids = new List<long>();
            foreach(T obj in uo)
            {
                ids.Add(ToID(obj));
            }
            return ids.ToArray();
        }

        public int ToInt(long id)
        {
            return (int)(0x00000000FFFFFFFFL & id);
        }

        public bool IsMapped(long id)
        {
            if (IsNullID(id))
            {
                return true;
            }
            if (IsStaticFolderID(id))
            {
                return true;
            }
            if (IsDynamicFolderID(id))
            {
                return true;
            }
            if (IsInstanceID(id))
            {
                int persistentID = ToInt(id);
                return m_persistentIDToSceneObject != null && m_persistentIDToSceneObject.ContainsKey(persistentID);
            }
            if (IsStaticResourceID(id))
            {
                int persistentID = ToInt(id);
                return m_mapping.PersistentIDtoObj.ContainsKey(persistentID);
            }
            if(IsDynamicResourceID(id))
            {
                int persistentID = ToInt(id);
                return m_persistentIDToDynamicResource.ContainsKey(persistentID);
            }
            return false;
        }

        public T FromID<T>(long id) where T : UnityObject
        {
            if(IsNullID(id))
            {
                return null;
            }

            if(IsStaticResourceID(id))
            {
                UnityObject obj;
                int persistentID = ToInt(id);
                if (m_mapping.PersistentIDtoObj.TryGetValue(persistentID, out obj))
                {
                    return obj as T;
                }
            }
            else if (IsInstanceID(id))
            {
                UnityObject obj;
                int persistentID = ToInt(id);
                if (m_persistentIDToSceneObject != null && m_persistentIDToSceneObject.TryGetValue(persistentID, out obj))
                {
                    return obj as T;
                }
            }
            else if(IsDynamicResourceID(id))
            {
                UnityObject obj;
                int persistentID = ToInt(id);
                if (m_persistentIDToDynamicResource.TryGetValue(persistentID, out obj))
                {
                    return obj as T;
                }
            }
            return null;
        }

        public T[] FromID<T>(long[] id) where T : UnityObject
        {
            if(id == null)
            {
                return null;
            }

            T[] objs = new T[id.Length];
            for(int i = 0; i < id.Length; ++i)
            {
                objs[i] = FromID<T>(id[i]);
            }
            return objs;
        }
    }
}
