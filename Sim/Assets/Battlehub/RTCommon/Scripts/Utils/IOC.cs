using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Battlehub.RTCommon
{
    public class IOCContainer
    {
        private class Item
        {
            public object Instance;
            public MulticastDelegate Function;
            public Item(object instance)
            {
                Instance = instance;
            }

            public Item(MulticastDelegate function)
            {
                Function = function;
            }

            public T Resolve<T>()
            {
                if(Instance != null)
                {
                    return (T)Instance;
                }

                return ((Func<T>)Function)();
            }
        }

        private Dictionary<Type, Item> m_registered = new Dictionary<Type, Item>();
        private Dictionary<Type, Item> m_fallbacks = new Dictionary<Type, Item>();

        public void Register<T>(Func<T> func)
        {
            if(func == null)
            {
                throw new ArgumentNullException("func");
            }
            if(m_registered.ContainsKey(typeof(T)))
            {
                Debug.LogWarning("type {0} already registered.");
            }

            m_registered[typeof(T)] = new Item(func);
        }

        public void Register<T>(T instance)
        {
            if(instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if(m_registered.ContainsKey(typeof(T)))
            {
                Debug.LogWarningFormat("type {0} already registered.", typeof(T).FullName);
            }

            m_registered[typeof(T)] = new Item(instance);
        }

        public void Unregister<T>(Func<T> func)
        {
            Item item;
            if(m_registered.TryGetValue(typeof(T), out item))
            {
                if(item.Function != null && item.Function.Equals(func))
                {
                    m_registered.Remove(typeof(T));
                }
            }
        }

        public void Unregister<T>(T instance)
        {
            Item item;
            if(m_registered.TryGetValue(typeof(T), out item))
            {
                if(ReferenceEquals(item.Instance, instance))
                {
                    m_registered.Remove(typeof(T));
                }
            }
        }

        public void RegisterFallback<T>(Func<T> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException("func");
            }
            if (m_fallbacks.ContainsKey(typeof(T)))
            {
                Debug.LogWarningFormat("fallback for type {0} already registered.", typeof(T).FullName);
            }
            m_fallbacks[typeof(T)] = new Item(func);
        }


        public void RegisterFallback<T>(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if (m_fallbacks.ContainsKey(typeof(T)))
            {
                Debug.LogWarningFormat("type {0} already registered.", typeof(T).FullName);
            }

            m_fallbacks[typeof(T)] = new Item(instance);
        }

        public void UnregisterFallback<T>(Func<T> func)
        {
            Item item;
            if (m_fallbacks.TryGetValue(typeof(T), out item))
            {
                if (item.Function != null && item.Function.Equals(func))
                {
                    m_fallbacks.Remove(typeof(T));
                }
            }
        }

        public void UnregisterFallback<T>(T instance)
        {
            Item item;
            if (m_fallbacks.TryGetValue(typeof(T), out item))
            {
                if (ReferenceEquals(item.Instance, instance))
                {
                    m_fallbacks.Remove(typeof(T));
                }
            }
        }

        public T Resolve<T>()
        {
            Item item;
            if (m_registered.TryGetValue(typeof(T), out item))
            {
                return item.Resolve<T>();
            }
            else
            {
                if(m_fallbacks.TryGetValue(typeof(T), out item))
                {
                    return item.Resolve<T>();
                }
            }
            return default(T);
        }

        public void Clear()
        {
            m_registered.Clear();
            m_fallbacks.Clear();  
        }
    }

    public static class IOC
    {
        public static void Register<T>(Func<T> func)
        {
            m_container.Register(func);
        }

        public static void Register<T>(T instance)
        {
            m_container.Register(instance);
        }

        public static void Unregister<T>(Func<T> func)
        {
            m_container.Unregister(func);
        }

        public static void Unregister<T>(T instance)
        {
            m_container.Unregister(instance);
        }

        public static void RegisterFallback<T>(Func<T> func)
        {
            m_container.RegisterFallback(func);
        }

        public static void RegisterFallback<T>(T instance)
        {
            m_container.RegisterFallback(instance);
        }

        public static void UnregisterFallback<T>(Func<T> func)
        {
            m_container.UnregisterFallback(func);
        }

        public static void UnregisterFallback<T>(T instance)
        {
            m_container.UnregisterFallback(instance);
        }

        public static T Resolve<T>()
        {
            return m_container.Resolve<T>();
        }

        private static IOCContainer m_container;

        static IOC()
        {
            m_container = new IOCContainer();
        }

        public static void ClearAll()
        {
            m_container.Clear();
        }
    }


}

