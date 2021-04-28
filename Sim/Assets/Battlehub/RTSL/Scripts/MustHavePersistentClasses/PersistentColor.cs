using ProtoBuf;
using Battlehub.RTSL;

namespace UnityEngine.Battlehub.SL2
{
    [ProtoContract]
    public partial class PersistentColor : PersistentSurrogate
    {
        [ProtoMember(256)]
        public float r;

        [ProtoMember(257)]
        public float g;

        [ProtoMember(258)]
        public float b;

        [ProtoMember(259)]
        public float a;

        protected override void ReadFromImpl(object obj)
        {
            base.ReadFromImpl(obj);
            Color uo = (Color)obj;
            r = uo.r;
            g = uo.g;
            b = uo.b;
            a = uo.a;
        }

        protected override object WriteToImpl(object obj)
        {
            obj = base.WriteToImpl(obj);
            Color uo = (Color)obj;
            uo.r = r;
            uo.g = g;
            uo.b = b;
            uo.a = a;
            return uo;
        }

        public static implicit operator Color(PersistentColor surrogate)
        {
            if(surrogate == null) return default(Color);
            return (Color)surrogate.WriteTo(new Color());
        }
        
        public static implicit operator PersistentColor(Color obj)
        {
            PersistentColor surrogate = new PersistentColor();
            surrogate.ReadFrom(obj);
            return surrogate;
        }
    }
}

