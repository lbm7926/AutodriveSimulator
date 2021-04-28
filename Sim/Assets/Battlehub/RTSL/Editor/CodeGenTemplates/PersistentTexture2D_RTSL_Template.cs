//#define RTSL_COMPILE_TEMPLATES
#if RTSL_COMPILE_TEMPLATES
//<TEMPLATE_USINGS_START>
using Battlehub.RTSL;
using ProtoBuf;
using System;
using Battlehub.Utils;
using UnityEngine;
//<TEMPLATE_USINGS_END>
#else
using UnityEngine;
#endif

namespace Battlehub.RTSL.Internal
{
    [PersistentTemplate("UnityEngine.Texture2D")]
    public class PersistentTexture2D_RTSL_Template : PersistentSurrogateTemplate
    {
#if RTSL_COMPILE_TEMPLATES
        //<TEMPLATE_BODY_START>

        [ProtoMember(1)]
        public byte[] m_bytes;

        [ProtoMember(2)]
        public bool m_isReadable;

        [ProtoMember(3)]
        public bool m_mipChain;

        public override void ReadFrom(object obj)
        {
            base.ReadFrom(obj);

            Texture2D texture = (Texture2D)obj;
            if (texture == null)
            {
                return;
            }

            try
            {
                bool supportedFormat = texture.format == TextureFormat.ARGB32 ||
                                        texture.format == TextureFormat.RGBA32 ||
                                        texture.format == TextureFormat.RGB24 ||
                                        texture.format == TextureFormat.Alpha8;

                if (texture.isReadable && supportedFormat)
                {
                    m_bytes = texture.EncodeToPNG();
                }
                else
                {
                    Texture2D decompressed = texture.DeCompress();
                    m_bytes = decompressed.EncodeToPNG();
                    UnityEngine.Object.Destroy(decompressed);
                }

                m_isReadable = true;
                m_mipChain = texture.mipmapCount > 1;
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                m_bytes = new byte[0];
                m_isReadable = false;
            }
        }

        public override object WriteTo(object obj)
        {
            obj = base.WriteTo(obj);
            Texture2D texture = (Texture2D)obj;
            if (texture == null)
            {
                return null;
            }

            if (m_isReadable)
            {
                texture.LoadImage(m_bytes, false);
            }
            return texture;

        }

        public override void GetDeps(GetDepsContext context)
        {
            base.GetDeps(context);
        }

        public override void GetDepsFrom(object obj, GetDepsFromContext context)
        {
            base.GetDepsFrom(obj, context);
        }

        public override bool CanInstantiate(Type type)
        {
            return true;
        }

        public override object Instantiate(Type type)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, m_mipChain);
            return texture;
        }
        //<TEMPLATE_BODY_END>
#endif
    }
}


