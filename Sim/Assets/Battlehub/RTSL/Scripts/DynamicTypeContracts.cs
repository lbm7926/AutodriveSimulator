using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Events;
using UnityObject = UnityEngine.Object;

namespace Battlehub.RTSL
{
    public static class DictionaryExt
    {
        public static U Get<T, U>(this Dictionary<T, U> dict, T key)
        {
            U val;
            if (dict.TryGetValue(key, out val))
            {
                return val;
            }
            return default(U);
        }
    }

    public interface IRTSerializable
    {
        void Serialize();

        void Deserialize(Dictionary<long, UnityObject> dependencies);

        void GetDependencies(Dictionary<long, UnityObject> dependencies);

        void FindDependencies<T>(Dictionary<long, T> dependencies, Dictionary<long, T> objects, bool allowNulls);

    }

    [ProtoContract]
    public abstract class PrimitiveContract
    {
        public static PrimitiveContract<T> Create<T>(T value)
        {
            return new PrimitiveContract<T>(value);
        }

        //public static PrimitiveContract Create(Type type)
        //{
        //    Type d1 = typeof(PrimitiveContract<>);
        //    Type constructed = d1.MakeGenericType(type);
        //    return (PrimitiveContract)Activator.CreateInstance(constructed);
        //}

        public object ValueBase
        {
            get { return ValueImpl; }
            set { ValueImpl = value; }
        }
        protected abstract object ValueImpl { get; set; }
        protected PrimitiveContract() { }
    }

    [ProtoContract]
    public class PrimitiveContract<T> : PrimitiveContract
    {
        public PrimitiveContract() { }
        public PrimitiveContract(T value) { Value = value; }
        [ProtoMember(1)]
        public T Value { get; set; }
        protected override object ValueImpl
        {
            get { return Value; }
            set { Value = (T)value; }
        }
    }
}

