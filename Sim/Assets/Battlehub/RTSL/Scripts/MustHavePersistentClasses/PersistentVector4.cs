using ProtoBuf;
using Battlehub.RTSL;

namespace UnityEngine.Battlehub.SL2
{
    [ProtoContract]
    public partial class PersistentVector4 : PersistentSurrogate
    {
        [ProtoMember(256)]
        public float x;

        [ProtoMember(257)]
        public float y;

        [ProtoMember(258)]
        public float z;

        [ProtoMember(259)]
        public float w;

        protected override void ReadFromImpl(object obj)
        {
            base.ReadFromImpl(obj);
            Vector4 uo = (Vector4)obj;
            x = uo.x;
            y = uo.y;
            z = uo.z;
            w = uo.w;
        }

        protected override object WriteToImpl(object obj)
        {
            obj = base.WriteToImpl(obj);
            Vector4 uo = (Vector4)obj;
            uo.x = x;
            uo.y = y;
            uo.z = z;
            uo.w = w;
            return uo;
        }

        public static implicit operator Vector4(PersistentVector4 surrogate)
        {
            if(surrogate == null) { return default(Vector4); }
            return (Vector4)surrogate.WriteTo(new Vector4());
        }

        public static implicit operator PersistentVector4(Vector4 obj)
        {
            PersistentVector4 surrogate = new PersistentVector4();
            surrogate.ReadFrom(obj);
            return surrogate;
        }
    }
}

