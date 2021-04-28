using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using Battlehub.RTSL.Interface;
using Battlehub.RTCommon;

namespace Battlehub.RTSL
{
    public class UnityObjectFactory : IUnityObjectFactory
    {
        private static Shader m_standardShader;
        private ITypeMap m_typeMap;
        public UnityObjectFactory()
        {
            m_standardShader = Shader.Find("Standard");
            Debug.Assert(m_standardShader != null, "Standard shader is not found");

            m_typeMap = IOC.Resolve<ITypeMap>();
        }

        public bool CanCreateInstance(Type type)
        {
            Type persistentType = m_typeMap.ToPersistentType(type);
            PersistentSurrogate surrogate = null;
            if(persistentType != null)
            {
                surrogate = (PersistentSurrogate)Activator.CreateInstance(persistentType);
            }
            return CanCreateInstance(type, surrogate);
        }

        public bool CanCreateInstance(Type type, IPersistentSurrogate surrogate)
        {
            return type == typeof(Material) ||
                type == typeof(Texture2D) ||
                type == typeof(Mesh) ||
                type == typeof(PhysicMaterial) ||
                type.IsSubclassOf(typeof(ScriptableObject)) ||
                type == typeof(GameObject) ||
                surrogate != null && surrogate.CanInstantiate(type);
        }

        public UnityObject CreateInstance(Type type)
        {
            Type persistentType = m_typeMap.ToPersistentType(type);
            PersistentSurrogate surrogate = null;
            if (persistentType != null)
            {
                surrogate = (PersistentSurrogate)Activator.CreateInstance(persistentType);
            }
            return CreateInstance(type, surrogate);
        }

        public UnityObject CreateInstance(Type type, IPersistentSurrogate surrogate)
        {
            if(type == null)
            {
                Debug.LogError("type is null");
                return null;
            }

            if (type == typeof(Material))
            {
                Material material = new Material(m_standardShader);
                return material;
            }
            else if (type == typeof(Texture2D))
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, true);
                return texture;
            }
            else if(type == typeof(Shader))
            {
                Debug.LogWarning("Unable to instantiate Shader");
                return null;
            }
            else if(type.IsSubclassOf(typeof(ScriptableObject)))
            {
                return ScriptableObject.CreateInstance(type);
            }
               
            try
            {
                if (surrogate != null)
                {
                    return (UnityObject)surrogate.Instantiate(type);
                }

                return (UnityObject)Activator.CreateInstance(type);
            }
            catch(Exception e)
            {
                Debug.LogError(e);
                Debug.LogWarning("Collecting scene dependencies could fix this exeption. Tools->Runtime Save Load->Collect Scene Dependencies"); 
                return null;
            }
            
        }
    }

}

