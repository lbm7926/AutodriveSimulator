using ProtoBuf;
using UnityEngine;

namespace UnityEngine.Battlehub.SL2
{
    [ProtoContract]
    public class PersistentGameObject : PersistentObject
    {
        [ProtoMember(256)]
        public int layer;

        [ProtoMember(258)]
        public bool isStatic;

        [ProtoMember(259)]
        public string tag;

        [ProtoMember(1)]
        public bool ActiveSelf;

        protected override void ReadFromImpl(object obj)
        {
            base.ReadFromImpl(obj);
            GameObject uo = (GameObject)obj;
            layer = uo.layer;
            isStatic = uo.isStatic;
            tag = uo.tag;
            ActiveSelf = uo.activeSelf;
        }

        protected override object WriteToImpl(object obj)
        {
            obj = base.WriteToImpl(obj);
            GameObject uo = (GameObject)obj;
            uo.layer = layer;
            uo.isStatic = isStatic;
            uo.tag = tag;
            return obj;
        }

        public static implicit operator GameObject(PersistentGameObject surrogate)
        {
            return (GameObject)surrogate.WriteTo(new GameObject());
        }
        
        public static implicit operator PersistentGameObject(GameObject obj)
        {
            PersistentGameObject surrogate = new PersistentGameObject();
            surrogate.ReadFrom(obj);
            return surrogate;
        }
    }
}

