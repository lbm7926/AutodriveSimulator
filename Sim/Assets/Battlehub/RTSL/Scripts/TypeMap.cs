using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Battlehub.SL2;
using UnityEngine.Events.Battlehub.SL2;

using UnityObject = UnityEngine.Object;
using Battlehub.RTSL;
using Battlehub.RTSL.Battlehub.SL2;

namespace Battlehub.RTSL
{
    public interface ITypeMap
    {
        Type ToPersistentType(Type unityType);
        Type ToUnityType(Type persistentType);
        Type ToType(Guid typeGuid);
        Guid ToGuid(Type type);
    }

    public partial class TypeMap : ITypeMap
    {
        protected readonly Dictionary<Type, Type> m_toPeristentType = new Dictionary<Type, Type>();
        protected readonly Dictionary<Type, Type> m_toUnityType = new Dictionary<Type, Type>();
        protected readonly Dictionary<Type, Guid> m_toGuid = new Dictionary<Type, Guid>();
        protected readonly Dictionary<Guid, Type> m_toType = new Dictionary<Guid, Type>();

        private bool m_registerDefault = true;
        public TypeMap()
        {
            RegisterAutoTypes();

            if(m_registerDefault)
            {
                m_toPeristentType.Add(typeof(RuntimePrefab), typeof(PersistentRuntimePrefab));
                m_toUnityType.Add(typeof(PersistentRuntimePrefab), typeof(RuntimePrefab));
                m_toGuid.Add(typeof(PersistentRuntimePrefab), new Guid("6c20bf67-8156-4e43-99f9-025d03e7d0a5"));
                m_toGuid.Add(typeof(RuntimePrefab), new Guid("430451d7-d09d-45e0-8fac-3d061e10f654"));
                m_toType.Add(new Guid("6c20bf67-8156-4e43-99f9-025d03e7d0a5"), typeof(PersistentRuntimePrefab));
                m_toType.Add(new Guid("430451d7-d09d-45e0-8fac-3d061e10f654"), typeof(RuntimePrefab));

                m_toPeristentType.Add(typeof(GameObject), typeof(PersistentGameObject));
                m_toUnityType.Add(typeof(PersistentGameObject), typeof(GameObject));
                m_toGuid.Add(typeof(PersistentGameObject), new Guid("163584cb-bfc6-423c-ab02-e43c961af6d6"));
                m_toGuid.Add(typeof(GameObject), new Guid("2e76e2f0-289d-4c48-936f-56033397359c"));
                m_toType.Add(new Guid("163584cb-bfc6-423c-ab02-e43c961af6d6"), typeof(PersistentGameObject));
                m_toType.Add(new Guid("2e76e2f0-289d-4c48-936f-56033397359c"), typeof(GameObject));

                m_toPeristentType.Add(typeof(Transform), typeof(PersistentTransform));
                m_toUnityType.Add(typeof(PersistentTransform), typeof(Transform));
                m_toGuid.Add(typeof(PersistentTransform), new Guid("3b5d0310-44df-44a1-8689-245048c8151a"));
                m_toGuid.Add(typeof(Transform), new Guid("b542d079-2da8-4468-80b4-ba4c6adaa225"));
                m_toType.Add(new Guid("3b5d0310-44df-44a1-8689-245048c8151a"), typeof(PersistentTransform));
                m_toType.Add(new Guid("b542d079-2da8-4468-80b4-ba4c6adaa225"), typeof(Transform));

                m_toPeristentType.Add(typeof(UnityObject), typeof(PersistentObject));
                m_toUnityType.Add(typeof(PersistentObject), typeof(UnityObject));
                m_toGuid.Add(typeof(PersistentObject), new Guid("b4abccaa-7fd8-4035-8c90-2d46ac7278ed"));
                m_toGuid.Add(typeof(UnityObject), new Guid("9cdd70ef-9948-4b85-8432-09eaf0faf4b7"));
                m_toType.Add(new Guid("b4abccaa-7fd8-4035-8c90-2d46ac7278ed"), typeof(PersistentObject));
                m_toType.Add(new Guid("9cdd70ef-9948-4b85-8432-09eaf0faf4b7"), typeof(UnityObject));

                m_toPeristentType.Add(typeof(Component), typeof(PersistentComponent));
                m_toUnityType.Add(typeof(PersistentComponent), typeof(Component));
                m_toGuid.Add(typeof(PersistentComponent), new Guid("f7be1b4c-1306-4074-8076-f8bef011ab72"));
                m_toGuid.Add(typeof(Component), new Guid("d19c5e1f-80d6-4294-9be4-713150ba5152"));
                m_toType.Add(new Guid("f7be1b4c-1306-4074-8076-f8bef011ab72"), typeof(PersistentComponent));
                m_toType.Add(new Guid("d19c5e1f-80d6-4294-9be4-713150ba5152"), typeof(Component));

                m_toPeristentType.Add(typeof(UnityEvent), typeof(PersistentUnityEvent));
                m_toUnityType.Add(typeof(PersistentUnityEvent), typeof(UnityEvent));
                m_toGuid.Add(typeof(PersistentUnityEvent), new Guid("9f432f32-23f3-432c-992d-6cc43b8edbad"));
                m_toGuid.Add(typeof(UnityEvent), new Guid("3ed54cee-4405-475c-8954-a5eaed086ad7"));
                m_toType.Add(new Guid("9f432f32-23f3-432c-992d-6cc43b8edbad"), typeof(PersistentUnityEvent));
                m_toType.Add(new Guid("3ed54cee-4405-475c-8954-a5eaed086ad7"), typeof(UnityEvent));

                m_toPeristentType.Add(typeof(UnityEventBase), typeof(PersistentUnityEventBase));
                m_toUnityType.Add(typeof(PersistentUnityEventBase), typeof(UnityEventBase));
                m_toGuid.Add(typeof(PersistentUnityEventBase), new System.Guid("1f0e42fc-2817-49d9-af67-1fd15dd6f9ed"));
                m_toGuid.Add(typeof(UnityEventBase), new Guid("9e9f6774-aeb8-4cc5-b149-597a62077a89"));
                m_toType.Add(new Guid("1f0e42fc-2817-49d9-af67-1fd15dd6f9ed"), typeof(PersistentUnityEventBase));
                m_toType.Add(new Guid("9e9f6774-aeb8-4cc5-b149-597a62077a89"), typeof(UnityEventBase));

                m_toPeristentType.Add(typeof(Color), typeof(PersistentColor));
                m_toUnityType.Add(typeof(PersistentColor), typeof(Color));
                m_toGuid.Add(typeof(PersistentColor), new System.Guid("baea5d8e-ae9a-4eb7-bbf4-e6e80e0c85d3"));
                m_toGuid.Add(typeof(Color), new Guid("c5aaef8c-fce2-4ce9-8474-7094e24e7ea6"));
                m_toType.Add(new Guid("baea5d8e-ae9a-4eb7-bbf4-e6e80e0c85d3"), typeof(PersistentColor));
                m_toType.Add(new Guid("c5aaef8c-fce2-4ce9-8474-7094e24e7ea6"), typeof(Color));

                m_toPeristentType.Add(typeof(Vector3), typeof(PersistentVector3));
                m_toUnityType.Add(typeof(PersistentVector3), typeof(Vector3));
                m_toGuid.Add(typeof(PersistentVector3), new Guid("d1b8fcf4-3b8a-48f1-836f-86666243ece8"));
                m_toGuid.Add(typeof(Vector3), new Guid("28d91efe-0de0-478d-9e7b-c76f1ba91807"));
                m_toType.Add(new Guid("d1b8fcf4-3b8a-48f1-836f-86666243ece8"), typeof(PersistentVector3));
                m_toType.Add(new Guid("28d91efe-0de0-478d-9e7b-c76f1ba91807"), typeof(Vector3));

                m_toPeristentType.Add(typeof(Vector4), typeof(PersistentVector4));
                m_toUnityType.Add(typeof(PersistentVector4), typeof(Vector4));
                m_toGuid.Add(typeof(PersistentVector4), new Guid("dce295a4-7b33-408c-b87e-6b85a3358dbf"));
                m_toGuid.Add(typeof(Vector4), new Guid("210fd541-e678-45dc-bb96-69333099ddf4"));
                m_toType.Add(new Guid("dce295a4-7b33-408c-b87e-6b85a3358dbf"), typeof(PersistentVector4));
                m_toType.Add(new Guid("210fd541-e678-45dc-bb96-69333099ddf4"), typeof(Vector4));

                m_toPeristentType.Add(typeof(Quaternion), typeof(PersistentQuaternion));
                m_toUnityType.Add(typeof(PersistentQuaternion), typeof(Quaternion));
                m_toGuid.Add(typeof(PersistentQuaternion), new Guid("7e0cfd74-0fa9-4160-bdba-2498f6069b4b"));
                m_toGuid.Add(typeof(Quaternion), new Guid("a2c70442-63fd-4b71-a7ff-ce1749c513f3"));
                m_toType.Add(new Guid("7e0cfd74-0fa9-4160-bdba-2498f6069b4b"), typeof(PersistentQuaternion));
                m_toType.Add(new Guid("a2c70442-63fd-4b71-a7ff-ce1749c513f3"), typeof(Quaternion));
            }

            m_toPeristentType[typeof(Scene)] = typeof(PersistentRuntimeScene);
            m_toUnityType[typeof(PersistentRuntimeScene)] = typeof(Scene);
            Guid sceneGuid = new Guid("d144fbe0-d2c0-4bcf-aa9f-251376262202");
            m_toGuid[typeof(Scene)] = sceneGuid;
            m_toType[sceneGuid] = typeof(Scene);
        }

        partial void RegisterAutoTypes();
        
        public Type ToPersistentType(Type unityType)
        {
            Type persistentType;
            if(m_toPeristentType.TryGetValue(unityType, out persistentType))
            {
                return persistentType;
            }
            return null;
        }

        public Type ToUnityType(Type persistentType)
        {
            Type unityType;
            if(m_toUnityType.TryGetValue(persistentType, out unityType))
            {
                return unityType;
            }
            return null;
        }

        public Type ToType(Guid typeGuid)
        {
            Type type;
            if (m_toType.TryGetValue(typeGuid, out type))
            {
                return type;
            }
            return null;
        }

        public Guid ToGuid(Type type)
        {
            Guid guid;
            if(m_toGuid.TryGetValue(type, out guid))
            {
                return guid;
            }

            return Guid.Empty;
        }
    }
}


