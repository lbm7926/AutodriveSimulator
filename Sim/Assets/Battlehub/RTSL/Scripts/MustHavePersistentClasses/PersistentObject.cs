using ProtoBuf;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using Battlehub.RTSL;

namespace UnityEngine.Battlehub.SL2
{
    [ProtoContract]    
    public class PersistentObject : PersistentSurrogate
    {
        [ProtoMember(1)]
        public string name;

        [ProtoMember(2)]
        public int hideFlags;

        protected override void ReadFromImpl(object obj)
        {
            UnityObject uo = (UnityObject)obj;
            try
            {
                name = uo.name;
            }
            catch
            {
                Debug.Log("Exc");
            }
            
            hideFlags = (int)uo.hideFlags;
        }

        protected override object WriteToImpl(object obj)
        {
            UnityObject uo = (UnityObject)obj;
            uo.name = name;
            uo.hideFlags = (HideFlags)hideFlags;
            return obj;
        }  
        
    }

}
