using Battlehub.RTCommon;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEngine.Battlehub.SL2
{ }
namespace Battlehub.RTSL
{
    public class CustomImplementationAttribute : Attribute
    {
    }

    [ProtoContract]
    public class IntArray
    {
        [ProtoMember(1)]
        public int[] Array;
    }


    public abstract class PersistentSurrogate : IPersistentSurrogate
    {
        protected readonly IAssetDB m_assetDB;
        protected PersistentSurrogate()
        {
            m_assetDB = IOC.Resolve<IAssetDB>();
        }

        protected virtual void ReadFromImpl(object obj) { }
        protected virtual object WriteToImpl(object obj) { return obj; }
        protected virtual void GetDepsImpl(GetDepsContext context) { }
        protected virtual void GetDepsFromImpl(object obj, GetDepsFromContext context) { }

        public virtual bool CanInstantiate(Type type)
        {
            return type != null && type.GetConstructor(Type.EmptyTypes) != null;
        }

        public virtual object Instantiate(Type type)
        {
            return Activator.CreateInstance(type);
        }

        public virtual void ReadFrom(object obj)
        {
            if(obj == null)
            {
                return;
            }
            ReadFromImpl(obj);
        }

        public virtual object WriteTo(object obj)
        {
            if(obj == null)
            {
                return null;
            }
            obj = WriteToImpl(obj);
            return obj;
        }

        public virtual void GetDeps(GetDepsContext context)
        {
            if (context.VisitedObjects.Contains(this))
            {
                return;
            }
            context.VisitedObjects.Add(this);
            GetDepsImpl(context);
        }

        public virtual void GetDepsFrom(object obj, GetDepsFromContext context)
        {
            if (context.VisitedObjects.Contains(obj))
            {
                return;
            }
            context.VisitedObjects.Add(obj);
            GetDepsFromImpl(obj, context);
        }

        protected void WriteSurrogateTo(IPersistentSurrogate from, object to)
        {
            if(from == null)
            {
                return;
            }

            from.WriteTo(to);
        }

        protected T ReadSurrogateFrom<T>(object obj) where T : IPersistentSurrogate, new()
        {
            T surrogate = new T();
            surrogate.ReadFrom(obj);
            return surrogate;
        }

        protected void AddDep(long depenency, GetDepsContext context)
        {
            if (depenency > 0 && !m_assetDB.IsNullID(depenency) && !context.Dependencies.Contains(depenency))
            {
                context.Dependencies.Add(depenency);
            }
        }

        protected void AddDep(long[] depenencies, GetDepsContext context)
        {
            if(depenencies == null)
            {
                return;
            }

            for (int i = 0; i < depenencies.Length; ++i)
            {
                AddDep(depenencies[i], context);
            }
        }

        protected void AddDep(Dictionary<long,long> depenencies, GetDepsContext context)
        {
            if (depenencies == null)
            {
                return;
            }

            foreach(KeyValuePair<long, long> kvp in depenencies)
            {
                AddDep(kvp.Key, context);
                AddDep(kvp.Value, context);
            }
        }

        protected void AddDep<V>(Dictionary<long, V> depenencies, GetDepsContext context)
        {
            if (depenencies == null)
            {
                return;
            }

            foreach (KeyValuePair<long, V> kvp in depenencies)
            {
                AddDep(kvp.Key, context);
            }
        }

        protected void AddDep<T>(Dictionary<T, long> depenencies, GetDepsContext context)
        {
            if (depenencies == null)
            {
                return;
            }

            foreach (KeyValuePair<T, long> kvp in depenencies)
            {
                AddDep(kvp.Value, context);
            }
        }

        protected void AddDep(object obj, GetDepsFromContext context)
        {
            if (obj != null && !context.Dependencies.Contains(obj))
            {
                context.Dependencies.Add(obj);
            }
        }

        protected void AddDep<T>(T[] dependencies, GetDepsFromContext context)
        {
            if(dependencies == null)
            {
                return;
            }
            for (int i = 0; i < dependencies.Length; ++i)
            {
                AddDep(dependencies[i], context);
            }
        }

        protected void AddDep<T>(List<T> dependencies, GetDepsFromContext context)
        {
            if (dependencies == null)
            {
                return;
            }
            for (int i = 0; i < dependencies.Count; ++i)
            {
                AddDep(dependencies[i], context);
            }
        }

        protected void AddDep<T>(HashSet<T> dependencies, GetDepsFromContext context)
        {
            if (dependencies == null)
            {
                return;
            }
            foreach(T dep in dependencies)
            {
                AddDep(dep, context);
            }
        }

        protected void AddDep<T,V>(Dictionary<T,V> dependencies, GetDepsFromContext context)
        {
            if (dependencies == null)
            {
                return;
            }
            foreach(KeyValuePair<T, V> kvp in dependencies)
            {
                AddDep(kvp.Key, context);
                if(kvp.Value != null)
                {
                    AddDep(kvp.Value, context);
                }
            }
        }

        protected void AddSurrogateDeps(PersistentSurrogate surrogate, GetDepsContext context)
        {
            if(surrogate == null)
            {
                return;
            }

            surrogate.GetDeps(context);
        }

        protected void AddSurrogateDeps<T>(T[] surrogateArray, GetDepsContext context) where T : PersistentSurrogate
        {
            if(surrogateArray == null)
            {
                return;
            }
            for (int i = 0; i < surrogateArray.Length; ++i)
            {
                PersistentSurrogate surrogate = surrogateArray[i];
                surrogate.GetDeps(context);
            }
        }

        protected void AddSurrogateDeps<T>(List<T> surrogateList, GetDepsContext context) where T : PersistentSurrogate
        {
            if(surrogateList == null)
            {
                return;
            }
            for (int i = 0; i < surrogateList.Count; ++i)
            {
                PersistentSurrogate surrogate = surrogateList[i];
                surrogate.GetDeps(context);
            }
        }

        protected void AddSurrogateDeps<T>(HashSet<T> surrogatesHS, GetDepsContext context) where T : PersistentSurrogate
        {
            if (surrogatesHS == null)
            {
                return;
            }
            foreach(PersistentSurrogate surrogate in surrogatesHS)
            {
                surrogate.GetDeps(context);
            }
        }

        protected void AddSurrogateDeps<T,V>(Dictionary<T,V> surrogateDict, GetDepsContext context)
        {
            if (surrogateDict == null)
            {
                return;
            }

            foreach(KeyValuePair<T, V> kvp in surrogateDict)
            {
                PersistentSurrogate surrogate = kvp.Key as PersistentSurrogate;
                if(surrogate != null)
                {
                    surrogate.GetDeps(context);
                }

                surrogate = kvp.Value as PersistentSurrogate;
                if (surrogate != null)
                {
                    surrogate.GetDeps(context);
                }
            }
        }

        protected void AddSurrogateDeps<V>(Dictionary<long, V> surrogateDict, GetDepsContext context) where V : PersistentSurrogate
        {
            if (surrogateDict == null)
            {
                return;
            }

            foreach (KeyValuePair<long, V> kvp in surrogateDict)
            {
                AddDep(kvp.Key, context);
                if (kvp.Value != null)
                {
                    kvp.Value.GetDeps(context);
                }
            }
        }

        protected void AddSurrogateDeps<T>(Dictionary<T, long> surrogateDict, GetDepsContext context) where T : PersistentSurrogate
        {
            if (surrogateDict == null)
            {
                return;
            }
            foreach (KeyValuePair<T, long> kvp in surrogateDict)
            {
                kvp.Key.GetDeps(context);
                AddDep(kvp.Value, context);
            }
        }

        protected void AddSurrogateDeps<T>(T obj, Func<T, PersistentSurrogate> convert, GetDepsContext context)
        {
            if (obj != null)
            {
                PersistentSurrogate surrogate = convert(obj);
                surrogate.GetDeps(context);
            }
        }

        protected void AddSurrogateDeps<T>(T[] objArray, Func<T, PersistentSurrogate> convert, GetDepsContext context)
        {
            if (objArray == null)
            {
                return;
            }
            for (int i = 0; i < objArray.Length; ++i)
            {
                T obj = objArray[i];
                if (obj != null)
                {
                    PersistentSurrogate surrogate = convert(obj);
                    surrogate.GetDeps(context);
                }
            }
        }

        protected void AddSurrogateDeps<T>(List<T> objList, Func<T, PersistentSurrogate> convert, GetDepsContext context)
        {
            if (objList == null)
            {
                return;
            }
            for (int i = 0; i < objList.Count; ++i)
            {
                T obj = objList[i];
                if (obj != null)
                {
                    PersistentSurrogate surrogate = convert(obj);
                    surrogate.GetDeps(context);
                }
            }
        }

        protected void AddSurrogateDeps<T>(HashSet<T> objHs, Func<T, PersistentSurrogate> convert, GetDepsContext context)
        {
            if (objHs == null)
            {
                return;
            }
            foreach(T obj in objHs)
            {
                if (obj != null)
                {
                    PersistentSurrogate surrogate = convert(obj);
                    surrogate.GetDeps(context);
                }
            }
        }

        protected void AddSurrogateDeps<T>(T obj, Func<T, PersistentSurrogate> convert, GetDepsFromContext context)
        {
            if (obj != null)
            {
                PersistentSurrogate surrogate = convert(obj);
                surrogate.GetDepsFrom(obj, context);
            }
        }

        protected void AddSurrogateDeps<T>(T[] objArray, Func<T, PersistentSurrogate> convert, GetDepsFromContext context)
        {
            if(objArray == null)
            {
                return;
            }
            for (int i = 0; i < objArray.Length; ++i)
            {
                T obj = objArray[i];
                if (obj != null)
                {
                    PersistentSurrogate surrogate = convert(obj);
                    surrogate.GetDepsFrom(obj, context);
                }
            }
        }

        protected void AddSurrogateDeps<T>(List<T> objList, Func<T, PersistentSurrogate> convert, GetDepsFromContext context)
        {
            if(objList == null)
            {
                return;
            }
            for (int i = 0; i < objList.Count; ++i)
            {
                T obj = objList[i];
                if (obj != null)
                {
                    PersistentSurrogate surrogate = convert(obj);
                    surrogate.GetDepsFrom(obj, context);
                }
            }
        }

        protected void AddSurrogateDeps<T>(HashSet<T> objHs, Func<T, PersistentSurrogate> convert, GetDepsFromContext context)
        {
            if (objHs == null)
            {
                return;
            }
            foreach(T obj in objHs)
            { 
                if (obj != null)
                {
                    PersistentSurrogate surrogate = convert(obj);
                    surrogate.GetDepsFrom(obj, context);
                }
            }
        }

        protected void AddSurrogateDeps<T, V, T1, V1>(Dictionary<T, V> dict, Func<T, T1> convertKey, Func<V, V1> convertValue, GetDepsFromContext context)
        {
            if (dict == null)
            {
                return;
            }
            foreach (KeyValuePair<T, V> kvp in dict)
            {
                T obj = kvp.Key;

                PersistentSurrogate surrogate = convertKey(obj) as PersistentSurrogate;
                if(surrogate != null)
                {
                    surrogate.GetDepsFrom(obj, context);
                }

                surrogate = convertValue(kvp.Value) as PersistentSurrogate;
                if(surrogate != null)
                {
                    surrogate.GetDepsFrom(obj, context);
                }
            }
        }

        public T[] Assign<V, T>(V[] arr, Func<V, T> convert)
        {
            if (arr == null)
            {
                return null;
            }

            T[] result = new T[arr.Length];
            for (int i = 0; i < arr.Length; ++i)
            {
                result[i] = convert(arr[i]);
            }
            return result;
        }

        public List<T> Assign<V, T>(List<V> list, Func<V, T> convert)
        {
            if (list == null)
            {
                return null;
            }

            List<T> result = new List<T>(list.Count);
            for (int i = 0; i < list.Count; ++i)
            {
                result.Add(convert(list[i]));
            }
            return result;
        }

        public HashSet<T> Assign<V, T>(HashSet<V> hs, Func<V, T> convert)
        {
            if (hs == null)
            {
                return null;
            }

            HashSet<T> result = new HashSet<T>();
            foreach(V obj in hs)
            {
                result.Add(convert(obj));
            }
            return result;
        }

        protected Dictionary<TOUT, VOUT> Assign<TIN, TOUT, VIN, VOUT>(Dictionary<TIN, VIN> dict, Func<TIN, TOUT> convertKey, Func<VIN, VOUT> convertValue)
        {
            if(dict == null)
            {
                return null;
            }

            Dictionary<TOUT, VOUT> result = new Dictionary<TOUT, VOUT>();
            foreach(KeyValuePair<TIN, VIN> kvp in dict)
            {
                TOUT key = convertKey(kvp.Key);
                VOUT value = convertValue(kvp.Value);

                if(key != null)
                {
                    result.Add(key, value);
                }
            }
            return result;
        }

        protected long ToID(UnityObject uo)
        {
            return m_assetDB.ToID(uo);
        }

        protected long[] ToID(UnityObject[] uo)
        {
            return m_assetDB.ToID(uo);
        }

        protected long[] ToID<T>(List<T> list) where T : UnityObject
        {
            return m_assetDB.ToID(list);
        }

        protected long[] ToID<T>(HashSet<T> hs) where T : UnityObject
        {
            return m_assetDB.ToID(hs);
        }

        protected Dictionary<long, long> ToID<T, V>(Dictionary<T, V> uo) where T : UnityObject where V : UnityObject
        {
            if(uo == null)
            {
                return null;
            }

            Dictionary<long, long> result = new Dictionary<long, long>();
            foreach(KeyValuePair<T, V> kvp in uo)
            {
                long key = ToID(kvp.Key);
                long value = ToID(kvp.Value);
                if(!result.ContainsKey(key))
                {
                    result.Add(key, value);
                }
            }
            return result;
        }

        protected Dictionary<long, VOUT> ToID<T, VOUT, VIN>(Dictionary<T, VIN> uo, Func<VIN, VOUT> convert) where T : UnityObject
        {
            if (uo == null)
            {
                return null;
            }

            Dictionary<long, VOUT> result = new Dictionary<long, VOUT>();
            foreach (KeyValuePair<T, VIN> kvp in uo)
            {
                long key = ToID(kvp.Key);
                VOUT value = convert(kvp.Value);
                if (!result.ContainsKey(key))
                {
                    result.Add(key, value);
                }
            }
            return result;
        }

        protected Dictionary<TOUT, long> ToID<TOUT, TIN, V>(Dictionary<TIN, V> uo, Func<TIN, TOUT> convert) where V : UnityObject
        {
            if (uo == null)
            {
                return null;
            }

            Dictionary<TOUT, long> result = new Dictionary<TOUT, long>();
            foreach (KeyValuePair<TIN, V> kvp in uo)
            {
                TOUT key = convert(kvp.Key);
                long value = ToID(kvp.Value);
                if (key != null)
                {
                    result.Add(key, value);
                }
            }
            return result;
        }

        protected T FromID<T>(long id, T fallback = null) where T : UnityObject
        {
            if(m_assetDB.IsNullID(id))
            {
                return default(T);
            }

            T value = m_assetDB.FromID<T>(id);
            if(value == default(T))
            {
                return fallback;
            }

            return value;
        }

        protected T[] FromID<T>(long[] id, T[] fallback = null) where T : UnityObject
        {
            if (id == null)
            {
                return null;
            }

            T[] objs = new T[id.Length];
            for (int i = 0; i < id.Length; ++i)
            {
                if(fallback != null && i < fallback.Length)
                {
                    objs[i] = FromID(id[i], fallback[i]);
                }
                else
                { 
                    objs[i] = FromID<T>(id[i]);
                }
            }
            return objs;
        }

        protected List<T> FromID<T>(long[] id, List<T> fallback = null) where T : UnityObject
        {
            if (id == null)
            {
                return null;
            }

            List<T> objs = new List<T>();
            for (int i = 0; i < id.Length; ++i)
            {
                if (fallback != null && i < fallback.Count)
                {
                    objs.Add(FromID(id[i], fallback[i]));
                }
                else
                {
                    objs.Add(FromID<T>(id[i]));
                }
            }
            return objs;
        }

        protected HashSet<T> FromID<T>(long[] id, HashSet<T> fallback = null) where T : UnityObject
        {
            if (id == null)
            {
                return null;
            }

            HashSet<T> objs = new HashSet<T>();

            int count = 0;
            if(fallback != null)
            {
                foreach(T f in fallback)
                {
                    if (count >= id.Length)
                    {
                        break;
                    }

                    T obj = FromID(id[count], f);
                    if(obj != null)
                    {
                        objs.Add(obj);
                    }
                    
                    count++;
                }
            }

            for (int i = count; i < id.Length; ++i)
            {
                T obj = FromID<T>(id[i]);
                if(obj != null)
                {
                    objs.Add(obj);
                }
            }            
            return objs;
        }

        protected Dictionary<T, V> FromID<T, V>(Dictionary<long, long> id, Dictionary<T, V> fallback = null) where T : UnityObject where V : UnityObject
        {
            if (id == null)
            {
                return null;
            }

            Dictionary<T, V> objs = new Dictionary<T, V>();
            foreach (KeyValuePair<long, long> kvp in id)
            {
                T key = FromID<T>(kvp.Key);
                V value = FromID<V>(kvp.Value);
                if (key != null)
                {
                    objs.Add(key, value);
                }
            }
            return objs;
        }

        protected Dictionary<T, VOUT> FromID<T, VOUT, VIN>(Dictionary<long, VIN> id, Func<VIN, VOUT> convert, Dictionary<T, VOUT> fallback = null) where T : UnityObject
        {
            if (id == null)
            {
                return null;
            }

            Dictionary<T, VOUT> objs = new Dictionary<T, VOUT>();
            foreach (KeyValuePair<long, VIN> kvp in id)
            {
                T key = FromID<T>(kvp.Key);
                if (key != null)
                {
                    objs.Add(key, convert(kvp.Value));
                }
            }
            return objs;
        }

        protected Dictionary<TOUT, V> FromID<TOUT, TIN, V>(Dictionary<TIN, long> id, Func<TIN, TOUT> convert, Dictionary<TOUT, V> fallback = null) where V : UnityObject
        {
            if (id == null)
            {
                return null;
            }

            Dictionary<TOUT, V> objs = new Dictionary<TOUT, V>();
            foreach (KeyValuePair<TIN, long> kvp in id)
            {
                TOUT key = convert(kvp.Key);
                V value = FromID<V>(kvp.Value);
                if (key != null)
                {
                    objs.Add(key, value);
                }
            }
            return objs;
        }

        protected T GetPrivate<T>(object obj, string fieldName)
        {
            FieldInfo fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if(fieldInfo == null)
            {
                return default(T);
            }
            object val = fieldInfo.GetValue(obj);
            if(val is T)
            {
                return (T)val;
            }
            return default(T);
        }

        protected T GetPrivate<V, T>(object obj, string fieldName)
        {
            FieldInfo fieldInfo = typeof(V).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (fieldInfo == null)
            {
                return default(T);
            }
            object val = fieldInfo.GetValue(obj);
            if (val is T)
            {
                return (T)val;
            }
            return default(T);
        }

        protected void SetPrivate<T>(object obj, string fieldName, T value)
        {
            FieldInfo fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (fieldInfo == null)
            {
                return;
            }

            if(!fieldInfo.FieldType.IsAssignableFrom(typeof(T)))
            {
                return;
            }

            fieldInfo.SetValue(obj, value);
        }

        protected void SetPrivate<V,T>(V obj, string fieldName, T value)
        {
            FieldInfo fieldInfo = typeof(V).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (fieldInfo == null)
            {
                return;
            }

            if (!fieldInfo.FieldType.IsAssignableFrom(typeof(T)))
            {
                return;
            }

            fieldInfo.SetValue(obj, value);
        }

        private static readonly Dictionary<object, object> m_refrencesCache = new Dictionary<object, object>();
        protected T ResolveReference<T, V>(V v, Func<T> fallback)
        {
            if(v == null)
            {
                return default(T);
            }

            object result;
            if(!m_refrencesCache.TryGetValue(v, out result))
            {
                result = fallback();
                m_refrencesCache.Add(v, result);
            }
            return (T)result;
        }

        protected T[] ResolveReference<T, V>(V[] v, Func<int, T> fallback)
        {
            if(v == null)
            {
                return null;
            }

            T[] result = new T[v.Length];
            for (int i = 0; i < v.Length; ++i)
            {
                if(v[i] == null)
                {
                    continue;
                }
                object res;
                if (!m_refrencesCache.TryGetValue(v[i], out res))
                {
                    res = fallback(i);
                    m_refrencesCache.Add(v[i], res);
                }
                result[i] = (T)res;
            }
            return result;
        }

        protected List<T> ResolveReference<T, V>(List<V> v, Func<int, T> fallback)
        {
            if (v == null)
            {
                return null;
            }

            List<T> result = new List<T>(v.Count);
            for (int i = 0; i < v.Count; ++i)
            {
                if (v[i] == null)
                {
                    continue;
                }
                object res;
                if (!m_refrencesCache.TryGetValue(v[i], out res))
                {
                    res = fallback(i);
                    m_refrencesCache.Add(v[i], res);
                }
                result.Add((T)res);
            }
            return result;
        }


        protected void ClearReferencesCache()
        {
            m_refrencesCache.Clear();
        }

    }
}

