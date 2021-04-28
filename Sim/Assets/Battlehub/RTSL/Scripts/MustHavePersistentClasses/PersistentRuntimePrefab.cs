using Battlehub.RTCommon;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Battlehub.SL2;

using UnityObject = UnityEngine.Object;
namespace Battlehub.RTSL.Battlehub.SL2
{
    [ProtoContract]
    public class PersistentRuntimePrefab : PersistentObject
    {
        [ProtoMember(1)]
        public PersistentDescriptor[] Descriptors;
        [ProtoMember(2)]
        public PersistentObject[] Data;
        [ProtoMember(3)]
        public long[] Identifiers;

        //Identifiers of assets PersistentPrefab depends on
        [ProtoMember(4)]
        public long[] Dependencies;

        //[ProtoMember(5)]
        //public int[] Usings;

        protected readonly ITypeMap m_typeMap;

        public PersistentRuntimePrefab()
        {
            m_typeMap = IOC.Resolve<ITypeMap>();
        }

        protected override void ReadFromImpl(object obj)
        {
            base.ReadFromImpl(obj);
            GameObject go = (GameObject)obj;

            List<PersistentObject> data = new List<PersistentObject>();
            List<long> identifiers = new List<long>();
            //HashSet<int> usings = new HashSet<int>();
            GetDepsFromContext getDepsCtx = new GetDepsFromContext();
            Descriptors = new PersistentDescriptor[1];
            Descriptors[0] = CreateDescriptorAndData(go, data, identifiers, /*usings,*/ getDepsCtx);

            Identifiers = identifiers.ToArray();
            Data = data.ToArray();
            Dependencies = getDepsCtx.Dependencies.OfType<UnityObject>().Select(d => m_assetDB.ToID(d)).ToArray();
           // Usings = usings.ToArray();
        }

        protected override object WriteToImpl(object obj)
        {
            obj = base.WriteToImpl(obj);
            RestoreDataAndResolveDependencies();
            //for(int i = 0; i < Data.Length; ++i)
            //{
            //    PersistentObject data = Data[i];
            //    long id = Identifiers[i];
 
            //    UnityObject unityObj = m_assetDB.FromID<UnityObject>(id);
            //    data.WriteTo(unityObj);
            //}

            return obj;
        }

        protected override void GetDepsImpl(GetDepsContext context)
        {
            base.GetDepsImpl(context);
            if(Dependencies != null)
            {
                for (int i = 0; i < Dependencies.Length; ++i)
                {
                    context.Dependencies.Add(Dependencies[i]);
                }
            }
        }

        protected override void GetDepsFromImpl(object obj, GetDepsFromContext context)
        {
            base.GetDepsFromImpl(obj, context);
            if(!(obj is GameObject))
            {
                return;
            }

            //Prefab parts should not be considered as external dependencies. This list required to remove prefab parts (children and components) from dependencies collection. 
            List<object> prefabParts = new List<object>();

            GetDependenciesFrom((GameObject)obj, prefabParts, context);

            for(int i = 0; i < prefabParts.Count; ++i)
            {
                context.Dependencies.Remove(prefabParts[i]);
            }
        }

        protected virtual void GetDependenciesFrom(GameObject go, List<object> prefabParts, GetDepsFromContext context)
        {
            if (go.GetComponent<RTSLIgnore>())
            {
                //Do not save persistent ignore objects
                return;
            }

            Type persistentType = m_typeMap.ToPersistentType(go.GetType());
            if (persistentType == null)
            {
                return;
            }

            prefabParts.Add(go);

            PersistentObject goData = (PersistentObject)Activator.CreateInstance(persistentType);
            goData.GetDepsFrom(go, context);

            Component[] components = go.GetComponents<Component>().Where(c => c != null).ToArray();
            if (components.Length > 0)
            {
                for (int i = 0; i < components.Length; ++i)
                {
                    Component component = components[i];
                    Type persistentComponentType = m_typeMap.ToPersistentType(component.GetType());
                    if (persistentComponentType == null)
                    {
                        continue;
                    }

                    prefabParts.Add(component);

                    PersistentObject componentData = (PersistentObject)Activator.CreateInstance(persistentComponentType);
                    componentData.GetDepsFrom(component, context);
                }
            }

            Transform transform = go.transform;
            if (transform.childCount > 0)
            {
                foreach(Transform child in transform)
                {
                    GetDependenciesFrom(child.gameObject, prefabParts, context);
                }
            }
        }

  
        /// <summary>
        /// Create GameObjects hierarchy and Add Components recursively
        /// </summary>
        /// <param name="descriptor">PersistentObject descriptor (initially root descriptor)</param>
        /// <param name="idToObj">Dictionary instanceId->UnityObject which will be populated with GameObjects and Components</param>
        public void CreateGameObjectWithComponents(ITypeMap typeMap, PersistentDescriptor descriptor, Dictionary<int, UnityObject> idToObj, Transform parent, List<GameObject> createdGameObjects = null, Dictionary<long, UnityObject> decomposition = null)
        {
            UnityObject objGo;
            GameObject go;
            if (idToObj.TryGetValue(m_assetDB.ToInt(descriptor.PersistentID), out objGo))
            {
                throw new ArgumentException(string.Format("duplicate object descriptor found in descriptors hierarchy. {0}", descriptor.ToString()), "descriptor");
            }
            else
            {
                go = new GameObject();
                if(parent != null)
                {
                    go.transform.SetParent(parent, false);
                }
                idToObj.Add(m_assetDB.ToInt(descriptor.PersistentID), go);
            }

            if (decomposition != null)
            {
                if (!decomposition.ContainsKey(descriptor.PersistentID))
                {
                    decomposition.Add(descriptor.PersistentID, go);
                }
            }

            if (createdGameObjects != null)
            {
                createdGameObjects.Add(go);
            }

            go.SetActive(false);

            if (descriptor.Parent != null)
            {
                UnityObject parentGO;
                if (!idToObj.TryGetValue(m_assetDB.ToInt(descriptor.Parent.PersistentID), out parentGO))
                {
                    throw new ArgumentException(string.Format("objects dictionary is supposed to have object with PersistentID {0} at this stage. Descriptor {1}", descriptor.Parent.PersistentID, descriptor, "descriptor"));
                }

                if (parentGO == null)
                {
                    throw new ArgumentException(string.Format("object with PersistentID {0} should have GameObject type. Descriptor {1}", descriptor.Parent.PersistentID, descriptor, "descriptor"));
                }
                go.transform.SetParent(((GameObject)parentGO).transform, false);
            }

            if (descriptor.Components != null)
            {
                Dictionary<Type, bool> requirements = new Dictionary<Type, bool>();
                for (int i = 0; i < descriptor.Components.Length; ++i)
                {
                    PersistentDescriptor componentDescriptor = descriptor.Components[i];

                    Type persistentComponentType = m_typeMap.ToType(componentDescriptor.PersistentTypeGuid);
                    if (persistentComponentType == null)
                    {
                        Debug.LogWarningFormat("Unknown type {0} associated with component Descriptor {1}", componentDescriptor.PersistentTypeGuid, componentDescriptor.ToString());
                        idToObj.Add(m_assetDB.ToInt(componentDescriptor.PersistentID), null);
                        continue;
                    }
                    Type componentType = typeMap.ToUnityType(persistentComponentType);
                    if (componentType == null)
                    {
                        Debug.LogWarningFormat("There is no mapped type for " + persistentComponentType.FullName + " in TypeMap");
                        idToObj.Add(m_assetDB.ToInt(componentDescriptor.PersistentID), null);
                        continue;
                    }

                    if (!componentType.IsSubclassOf(typeof(Component)))
                    {
                        Debug.LogErrorFormat("{0} is not subclass of {1}", componentType.FullName, typeof(Component).FullName);
                        idToObj.Add(m_assetDB.ToInt(componentDescriptor.PersistentID), null);
                        continue;
                    }

                    UnityObject obj;
                    if (idToObj.TryGetValue(m_assetDB.ToInt(componentDescriptor.PersistentID), out obj))
                    {
                        if (obj != null && !(obj is Component))
                        {
                            Debug.LogError("Invalid Type. Component " + obj.name + " " + obj.GetType() + " " + obj.GetInstanceID() + " " + descriptor.PersistentTypeGuid + " " + componentDescriptor.PersistentTypeGuid);
                        }
                    }
                    else
                    {
                        obj = AddComponent(idToObj, go, requirements, componentDescriptor, componentType);
                    }

                    if (decomposition != null)
                    {
                        if (!decomposition.ContainsKey(componentDescriptor.PersistentID))
                        {
                            decomposition.Add(componentDescriptor.PersistentID, obj);
                        }
                    }
                }
            }

            if (descriptor.Children != null)
            {
                for (int i = 0; i < descriptor.Children.Length; ++i)
                {
                    PersistentDescriptor childDescriptor = descriptor.Children[i];
                    CreateGameObjectWithComponents(typeMap, childDescriptor, idToObj, null, createdGameObjects, decomposition);
                }
            }
        }

        protected void RestoreDataAndResolveDependencies()
        {
            List<GameObject> goList = new List<GameObject>();
            List<bool> goActivationList = new List<bool>();

            for (int i = 0; i < Data.Length; ++i)
            {
                PersistentObject data = Data[i];
                long id = Identifiers[i];

                UnityObject obj = FromID<UnityObject>(id);
                if (obj == null)
                {
                    Debug.LogWarningFormat("objects does not have object with instance id {0} however PersistentData of type {1} is present", id, data.GetType());
                    continue;
                }

                data.WriteTo(obj);
                if (obj is GameObject)
                {
                    goList.Add((GameObject)obj);
                    PersistentGameObject goData = (PersistentGameObject)data;
                    goActivationList.Add(goData.ActiveSelf);
                }
            }

            for (int i = 0; i < goList.Count; ++i)
            {
                bool activeSelf = goActivationList[i];
                GameObject go = goList[i];
                if(go != null)
                {
                    go.SetActive(activeSelf);
                }
            }
        }

        /// <summary>
        /// Add  dependencies here to let AddComponent method to figure out which components automatically added
        /// for example ParticleSystemRenderer should be added automatically if ParticleSystem component exists 
        /// </summary>
        public readonly static Dictionary<Type, HashSet<Type>> ComponentDependencies = new Dictionary<Type, HashSet<Type>>
            {
                //type depends on <- { types }
                { typeof(ParticleSystemRenderer), new HashSet<Type> { typeof(ParticleSystem) } }
            };

        private UnityObject AddComponent(Dictionary<int, UnityObject> idToObj, GameObject go, Dictionary<Type, bool> requirements, PersistentDescriptor componentDescriptor, Type componentType)
        {
            Component component;
            bool isReqFulfilled = requirements.ContainsKey(componentType) && requirements[componentType];
            bool maybeComponentAlreadyAdded =
                !isReqFulfilled ||
                componentType.IsSubclassOf(typeof(Transform)) ||
                componentType == typeof(Transform) ||
                componentType.IsDefined(typeof(DisallowMultipleComponent), true) ||
                ComponentDependencies.ContainsKey(componentType) && ComponentDependencies[componentType].Any(d => go.GetComponent(d) != null);

            if (maybeComponentAlreadyAdded)
            {
                component = go.GetComponent(componentType);
                if (component == null)
                {
                    component = go.AddComponent(componentType);
                }
                if (!isReqFulfilled)
                {
                    requirements[componentType] = true;
                }
            }
            else
            {
                component = go.AddComponent(componentType);
                if (component == null)
                {
                    component = go.GetComponent(componentType);
                }
            }
            if (component == null)
            {
                Debug.LogErrorFormat("Unable to add or get component of type {0}", componentType);
            }
            else
            {
                object[] requireComponents = component.GetType().GetCustomAttributes(typeof(RequireComponent), true);
                for (int j = 0; j < requireComponents.Length; ++j)
                {
                    RequireComponent requireComponent = requireComponents[j] as RequireComponent;
                    if (requireComponent != null)
                    {
                        if (requireComponent.m_Type0 != null && !requirements.ContainsKey(requireComponent.m_Type0))
                        {
                            bool fulfilled = go.GetComponent(requireComponent.m_Type0);
                            requirements.Add(requireComponent.m_Type0, fulfilled);
                        }
                        if (requireComponent.m_Type1 != null && !requirements.ContainsKey(requireComponent.m_Type1))
                        {
                            bool fulfilled = go.GetComponent(requireComponent.m_Type1);
                            requirements.Add(requireComponent.m_Type1, fulfilled);
                        }
                        if (requireComponent.m_Type2 != null && !requirements.ContainsKey(requireComponent.m_Type2))
                        {
                            bool fulfilled = go.GetComponent(requireComponent.m_Type2);
                            requirements.Add(requireComponent.m_Type2, fulfilled);
                        }
                    }
                }
                idToObj.Add(m_assetDB.ToInt(componentDescriptor.PersistentID), component);
            }

            return component;
        }


        protected virtual PersistentDescriptor CreateDescriptorAndData(GameObject go, List<PersistentObject> persistentData, List<long> persistentIdentifiers, /*HashSet<int> usings,*/ GetDepsFromContext getDepsFromCtx, PersistentDescriptor parentDescriptor = null)
        {
            if (go.GetComponent<RTSLIgnore>())
            {
                //Do not save persistent ignore objects
                return null;
            }
            Type persistentType = m_typeMap.ToPersistentType(go.GetType());
            if (persistentType == null)
            {
                return null;
            }

            long persistentID = ToID(go);
            //if(m_assetDB.IsResourceID(persistentID))
            //{
            //    int ordinal = m_assetDB.ToOrdinal(persistentID);
            //    usings.Add(ordinal);
            //}
            
            PersistentDescriptor descriptor = new PersistentDescriptor(m_typeMap.ToGuid(persistentType), persistentID, go.name);
            descriptor.Parent = parentDescriptor;

            PersistentObject goData = (PersistentObject)Activator.CreateInstance(persistentType);
            goData.ReadFrom(go);
            goData.GetDepsFrom(go, getDepsFromCtx);
            persistentData.Add(goData);
            persistentIdentifiers.Add(persistentID);

            Component[] components = go.GetComponents<Component>().Where(c => c != null).ToArray();
            if (components.Length > 0)
            {
                List<PersistentDescriptor> componentDescriptors = new List<PersistentDescriptor>();
                for (int i = 0; i < components.Length; ++i)
                {
                    Component component = components[i];
                    Type persistentComponentType = m_typeMap.ToPersistentType(component.GetType());
                    if (persistentComponentType == null)
                    {
                        continue;
                    }

                    long componentID = ToID(component);
                    //if (m_assetDB.IsResourceID(componentID))
                    //{
                    //    int ordinal = m_assetDB.ToOrdinal(componentID);
                    //    usings.Add(ordinal);
                    //}
                    PersistentDescriptor componentDescriptor = new PersistentDescriptor(m_typeMap.ToGuid(persistentComponentType), componentID, component.name);
                    componentDescriptor.Parent = descriptor;
                    componentDescriptors.Add(componentDescriptor);

                    PersistentObject componentData = (PersistentObject)Activator.CreateInstance(persistentComponentType);
                    componentData.ReadFrom(component);
                    componentData.GetDepsFrom(component, getDepsFromCtx);
                    persistentData.Add(componentData);
                    persistentIdentifiers.Add(componentID);
                }

                if (componentDescriptors.Count > 0)
                {
                    descriptor.Components = componentDescriptors.ToArray();
                }
            }

            Transform transform = go.transform;
            if (transform.childCount > 0)
            {
                List<PersistentDescriptor> children = new List<PersistentDescriptor>();
                foreach (Transform child in transform)
                {
                    PersistentDescriptor childDescriptor = CreateDescriptorAndData(child.gameObject, persistentData, persistentIdentifiers, /*usings,*/ getDepsFromCtx, descriptor);
                    if (childDescriptor != null)
                    {
                        children.Add(childDescriptor);
                    }
                }

                descriptor.Children = children.ToArray();
            }

            return descriptor;
        }

       
    }
}


