using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.Battlehub.SL2;
using UnityObject = UnityEngine.Object;
using UnityEngine;

namespace Battlehub.RTSL.Battlehub.SL2
{
    [ProtoContract]
    public class PersistentRuntimeScene : PersistentRuntimePrefab
    {
        [ProtoMember(1)]
        public PersistentObject[] Assets;

        [ProtoMember(2)]
        public int[] AssetIdentifiers;

        protected override void ReadFromImpl(object obj)
        {
            ClearReferencesCache();

            Scene scene = (Scene)obj;
            GameObject[] rootGameObjects;
            if (scene.IsValid())
            {
                rootGameObjects = scene.GetRootGameObjects();
            }
            else
            {
                rootGameObjects = new GameObject[0];
            }

            List<PersistentObject> data = new List<PersistentObject>();
            List<long> identifiers = new List<long>();    
            List<PersistentDescriptor> descriptors = new List<PersistentDescriptor>(rootGameObjects.Length);
            GetDepsFromContext getSceneDepsCtx = new GetDepsFromContext();

            for(int i = 0; i < rootGameObjects.Length; ++i)
            {
                GameObject rootGO = rootGameObjects[i];
                PersistentDescriptor descriptor = CreateDescriptorAndData(rootGO, data, identifiers, getSceneDepsCtx);
                if(descriptor != null)
                {
                    descriptors.Add(descriptor);
                }
            }

            HashSet<object> allDeps = getSceneDepsCtx.Dependencies;
            
            Queue<UnityObject> depsQueue = new Queue<UnityObject>(allDeps.OfType<UnityObject>());

            List<PersistentObject> assets = new List<PersistentObject>();
            List<int> assetIdentifiers = new List<int>();

            GetDepsFromContext getDepsCtx = new GetDepsFromContext();
            while (depsQueue.Count > 0)
            {
                UnityObject uo = depsQueue.Dequeue();
                if (!uo)
                {
                    continue;
                }


                Type persistentType = m_typeMap.ToPersistentType(uo.GetType());
                if (persistentType != null)
                {
                    getDepsCtx.Clear();

                    try
                    {
                        PersistentObject persistentObject = (PersistentObject)Activator.CreateInstance(persistentType);
                        if (!(uo is GameObject) && !(uo is Component))
                        {
                            if (!m_assetDB.IsMapped(uo))
                            {
                                if(uo is Texture2D)
                                {
                                    Texture2D texture = (Texture2D)uo;
                                    if(texture.isReadable)  //
                                    {
                                        persistentObject.ReadFrom(uo);
                                        assets.Add(persistentObject);
                                        assetIdentifiers.Add(uo.GetInstanceID());
                                        persistentObject.GetDepsFrom(uo, getDepsCtx);
                                    }
                                }
                                else
                                {
                                    persistentObject.ReadFrom(uo);
                                    assets.Add(persistentObject);
                                    assetIdentifiers.Add(uo.GetInstanceID());
                                    persistentObject.GetDepsFrom(uo, getDepsCtx);
                                }
                            }
                            else
                            {
                                persistentObject.GetDepsFrom(uo, getDepsCtx);
                            }
                        }
                        else
                        {
                            persistentObject.GetDepsFrom(uo, getDepsCtx);
                        }
                        
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }

                    foreach (UnityObject dep in getDepsCtx.Dependencies)
                    {
                        if (!allDeps.Contains(dep))
                        {
                            allDeps.Add(dep);
                            depsQueue.Enqueue(dep);
                        }
                    }

                }
            }

            List<UnityObject> externalDeps = new List<UnityObject>(allDeps.OfType<UnityObject>());
            for(int i = externalDeps.Count - 1; i >= 0; i--)
            {
                if(!m_assetDB.IsMapped(externalDeps[i]))
                {
                    externalDeps.RemoveAt(i);
                }
            }

            Descriptors = descriptors.ToArray();
            Identifiers = identifiers.ToArray();
            Data = data.ToArray();
            Dependencies = externalDeps.Select(uo => m_assetDB.ToID(uo)).ToArray();

            Assets = assets.ToArray();
            AssetIdentifiers = assetIdentifiers.ToArray();

            ClearReferencesCache();
        }

        private void DestroyGameObjects(Scene scene)
        {
            GameObject[] rootGameObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootGameObjects.Length; ++i)
            {
                GameObject rootGO = rootGameObjects[i];
                if (rootGO.GetComponent<RTSLIgnore>() || (rootGO.hideFlags & HideFlags.DontSave) != 0)
                {
                    continue;
                }

                UnityObject.DestroyImmediate(rootGO);
            }
        }

        protected override object WriteToImpl(object obj)
        {
            ClearReferencesCache();

            Scene scene = (Scene)obj;
            if (Descriptors == null && Data == null)
            {
                DestroyGameObjects(scene);
                return obj;
            }

            if (Descriptors == null && Data != null || Data != null && Descriptors == null)
            {
                throw new ArgumentException("data is corrupted", "scene");
            }

            if (Descriptors.Length == 0)
            {
                DestroyGameObjects(scene);
                return obj;
            }

            if(Identifiers == null || Identifiers.Length != Data.Length)
            {
                throw new ArgumentException("data is corrupted", "scene");
            }
      
            DestroyGameObjects(scene);
            Dictionary<int, UnityObject> idToUnityObj = new Dictionary<int, UnityObject>();
            for (int i = 0; i < Descriptors.Length; ++i)
            {
                PersistentDescriptor descriptor = Descriptors[i];
                if(descriptor != null)
                {
                    CreateGameObjectWithComponents(m_typeMap, descriptor, idToUnityObj, null);
                }
            }

            
            UnityObject[] assetInstances = null;
            if (AssetIdentifiers != null)
            {
                IUnityObjectFactory factory = IOC.Resolve<IUnityObjectFactory>();
                assetInstances = new UnityObject[AssetIdentifiers.Length];
                for (int i = 0; i < AssetIdentifiers.Length; ++i)
                {
                    PersistentObject asset = Assets[i];

                    Type uoType = m_typeMap.ToUnityType(asset.GetType());
                    if (uoType != null)
                    {
                        if(factory.CanCreateInstance(uoType, asset))
                        {
                            UnityObject assetInstance = factory.CreateInstance(uoType, asset);
                            if (assetInstance != null)
                            {
                                assetInstances[i] = assetInstance;
                                idToUnityObj.Add(AssetIdentifiers[i], assetInstance);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Unable to create object of type " + uoType.ToString());
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Unable to resolve unity type for " + asset.GetType().FullName);
                    }
                }
            }
          
            m_assetDB.RegisterSceneObjects(idToUnityObj);

            if(assetInstances != null)
            {
                for (int i = 0; i < AssetIdentifiers.Length; ++i)
                {
                    UnityObject assetInstance = assetInstances[i];
                    if (assetInstance != null)
                    {
                        PersistentObject asset = Assets[i];
                        asset.WriteTo(assetInstance);
                    }
                }
            }

            RestoreDataAndResolveDependencies();
            m_assetDB.UnregisterSceneObjects();

            ClearReferencesCache();

            return scene;
        }

        protected override void GetDepsFromImpl(object obj, GetDepsFromContext context)
        {
            if(!(obj is Scene))
            {
                return;
            }

            Scene scene = (Scene)obj;
            GameObject[] gameObjects = scene.GetRootGameObjects();

            for(int i = 0; i < gameObjects.Length; ++i)
            {
                base.GetDepsFromImpl(gameObjects[i], context);
            }
        }

        protected override void GetDependenciesFrom(GameObject go, List<object> prefabParts, GetDepsFromContext context)
        {
            if ((go.hideFlags & HideFlags.DontSave) != 0)
            {
                //Do not save persistent ignore objects
                return;
            }
            base.GetDependenciesFrom(go, prefabParts, context);
        }

        protected override PersistentDescriptor CreateDescriptorAndData(GameObject go, List<PersistentObject> persistentData, List<long> persistentIdentifiers, GetDepsFromContext getDepsFromCtx, PersistentDescriptor parentDescriptor = null)
        {
            if ((go.hideFlags & HideFlags.DontSave) != 0)
            {
                return null;
            }
            return base.CreateDescriptorAndData(go, persistentData, persistentIdentifiers, getDepsFromCtx, parentDescriptor);
        }
    }
}


