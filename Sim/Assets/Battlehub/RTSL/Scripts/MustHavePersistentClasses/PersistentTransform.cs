using ProtoBuf;

namespace UnityEngine.Battlehub.SL2
{
    [ProtoContract]
    public class PersistentTransform : PersistentComponent
    {
        [ProtoMember(256)]
        public Vector3 position;

        [ProtoMember(263)]
        public Quaternion rotation;

        [ProtoMember(265)]
        public Vector3 localScale;

        protected override void ReadFromImpl(object obj)
        {
            base.ReadFromImpl(obj);
            Transform uo = (Transform)obj;
            position = uo.localPosition;
            rotation = uo.localRotation;
            localScale = uo.localScale;
        }

        protected override object WriteToImpl(object obj)
        {
            obj = base.WriteToImpl(obj);
            Transform uo = (Transform)obj;
            uo.localPosition = position;
            uo.localRotation = rotation;
            uo.localScale = localScale;
            return obj;
        }

    
        public static implicit operator PersistentTransform(Transform obj)
        {
            PersistentTransform surrogate = new PersistentTransform();
            surrogate.ReadFrom(obj);
            return surrogate;
        }
    }
}

