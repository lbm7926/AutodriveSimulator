using ProtoBuf.Meta;
using System;
using System.IO;
using UnityEngine;

namespace Battlehub.RTSL
{
    public interface ISerializer
    {
        TData DeepClone<TData>(TData data);

        TData Deserialize<TData>(Stream stream);

        TData Deserialize<TData>(byte[] b);

        object Deserialize(Stream stream, Type type);

        void Serialize<TData>(TData data, Stream stream);

        byte[] Serialize<TData>(TData data);
    }
  
    [ProtoBuf.ProtoContract]
    public class NilContainer { }

    public class ProtobufSerializer : ISerializer
    {
#if !UNITY_EDITOR 
        private static RTSLTypeModel model = new RTSLTypeModel();
#else
        private static RuntimeTypeModel model = TypeModelCreator.Create();
#endif

        static ProtobufSerializer()
        {
            model.DynamicTypeFormatting += (sender, args) =>
            {
                if (args.FormattedName == null)
                {
                    return;
                }

                if (Type.GetType(args.FormattedName) == null)
                {
                    args.Type = typeof(NilContainer);
                }
            };

#if UNITY_EDITOR
            model.CompileInPlace();
#endif
        }


        public TData DeepClone<TData>(TData data)
        {
            return (TData)model.DeepClone(data);
        }

        public TData Deserialize<TData>(Stream stream)
        {
            TData deserialized = (TData)model.Deserialize(stream, null, typeof(TData));
            return deserialized;
        }

        public object Deserialize(Stream stream, Type type)
        {
            return model.Deserialize(stream, null, type);
        }

        public TData Deserialize<TData>(byte[] b)
        {
            using (var stream = new MemoryStream(b))
            {
                TData deserialized = (TData)model.Deserialize(stream, null, typeof(TData));
                return deserialized;
            }
        }

        public void Serialize<TData>(TData data, Stream stream)
        {
            model.Serialize(stream, data);
        }

        public byte[] Serialize<TData>(TData data)
        {
            using (var stream = new MemoryStream())
            {
                model.Serialize(stream, data);
                stream.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }
    }
 }
