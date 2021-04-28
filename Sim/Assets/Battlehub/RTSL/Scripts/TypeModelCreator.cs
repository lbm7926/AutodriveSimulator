using Battlehub.RTSL.Interface;
using UnityEngine.Battlehub.SL2;
using UnityEngine.Events.Battlehub.SL2;
using Battlehub.RTSL.Battlehub.SL2;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using UnityEngine;
using Battlehub.RTSL;

namespace Battlehub.RTSL
{
    public static partial class TypeModelCreator
    {
        private static bool m_createDefaultTypeModel = true;

        public static RuntimeTypeModel Create()
        {
            RuntimeTypeModel model = TypeModel.Create();
            
            model.Add(typeof(IntArray), true);
            model.Add(typeof(ProjectItem), true)
                .AddSubType(1025, typeof(AssetItem));
            model.Add(typeof(AssetItem), true);
            model.Add(typeof(AssetBundleItemInfo), true);
            model.Add(typeof(AssetBundleInfo), true);
            model.Add(typeof(ProjectInfo), true);
            model.Add(typeof(PrefabPart), true);
            model.Add(typeof(Preview), true);
            model.Add(typeof(PersistentDescriptor), true);
            model.Add(typeof(PersistentPersistentCall), true);
            model.Add(typeof(PersistentArgumentCache), true);

            RegisterAutoTypes(model);
            RegisterUserDefinedTypes(model);

            if(m_createDefaultTypeModel)
            {
                DefaultTypeModel(model);
            }
            
            MetaType primitiveContract = model.Add(typeof(PrimitiveContract), false);
            int fieldNumber = 16;

            //NOTE: Items should be added to TypeModel in exactly the same order!!!
            //It is allowed to append new types, but not to insert new types in the middle.

            Type[] types = new[] {
                typeof(bool),
                typeof(char),
                typeof(byte),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(string),
                typeof(float),
                typeof(double),
                typeof(decimal),
                typeof(PersistentColor),
                typeof(PersistentVector4)};

            foreach (Type type in types)
            {
                if (type.IsGenericType())
                {
                    continue;
                }

                Type derivedType = typeof(PrimitiveContract<>).MakeGenericType(type.MakeArrayType());
                primitiveContract.AddSubType(fieldNumber, derivedType);
                fieldNumber++;
                model.Add(derivedType, true);

                derivedType = typeof(PrimitiveContract<>).MakeGenericType(type);
                primitiveContract.AddSubType(fieldNumber, derivedType);
                fieldNumber++;
                model.Add(derivedType, true);

                model.Add(typeof(List<>).MakeGenericType(type), true);
            }

            model.AutoAddMissingTypes = false;
            return model;
        }

        private static void DefaultTypeModel(RuntimeTypeModel model)
        {
            model.Add(typeof(PersistentRuntimeScene), true);
            model.Add(typeof(PersistentRuntimePrefab), true)
                .AddSubType(1025, typeof(PersistentRuntimeScene));
            
            model.Add(typeof(PersistentGameObject), true);
            model.Add(typeof(PersistentTransform), true);

            model.Add(typeof(PersistentObject), true)
               .AddSubType(1025, typeof(PersistentGameObject))
               .AddSubType(1029, typeof(PersistentComponent))
               .AddSubType(1045, typeof(PersistentRuntimePrefab));

            model.Add(typeof(PersistentComponent), true)
                .AddSubType(1026, typeof(PersistentTransform));

            model.Add(typeof(PersistentUnityEvent), true);
            model.Add(typeof(PersistentUnityEventBase), true)
                .AddSubType(1025, typeof(PersistentUnityEvent));

            model.Add(typeof(PersistentColor), true);
            model.Add(typeof(Color), false).SetSurrogate(typeof(PersistentColor));
            model.Add(typeof(PersistentVector3), true);
            model.Add(typeof(Vector3), false).SetSurrogate(typeof(PersistentVector3));
            model.Add(typeof(PersistentVector4), true);
            model.Add(typeof(Vector4), false).SetSurrogate(typeof(PersistentVector4));
            model.Add(typeof(PersistentQuaternion), true);
            model.Add(typeof(Quaternion), false).SetSurrogate(typeof(PersistentQuaternion));
        }

        static partial void RegisterAutoTypes(RuntimeTypeModel model);

        static partial void RegisterUserDefinedTypes(RuntimeTypeModel model);
    }
}

