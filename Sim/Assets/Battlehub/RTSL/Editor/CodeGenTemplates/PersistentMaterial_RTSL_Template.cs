//#define RTSL_COMPILE_TEMPLATES
#if RTSL_COMPILE_TEMPLATES
//<TEMPLATE_USINGS_START>
using Battlehub.RTCommon;
using ProtoBuf;
using System.Collections.Generic;
using UnityEngine;
//<TEMPLATE_USINGS_END>
#else
using UnityEngine;
#endif

namespace Battlehub.RTSL.Internal
{
    using PersistentVector2 = PersistentSurrogateTemplate;

    [PersistentTemplate("UnityEngine.Material", 
        new[] { "color", "mainTexture", "mainTextureOffset", "mainTextureScale", "shader" },
        new[] { "UnityEngine.Vector4", "UnityEngine.Color", "UnityEngine.Vector2" })]
    public class PersistentMaterial_RTSL_Template : PersistentSurrogateTemplate
    {
#if RTSL_COMPILE_TEMPLATES
        //<TEMPLATE_BODY_START>

        [ProtoMember(1)]
        public RTShaderPropertyType[] m_propertyTypes;

        [ProtoMember(2)]
        public string[] m_propertyNames;

        [ProtoMember(3)]
        public PrimitiveContract[] m_propertyValues;

        [ProtoMember(4)]
        public string[] m_keywords;

        [ProtoMember(5)]
        public PersistentVector2 mainTextureOffset;

        [ProtoMember(6)]
        public PersistentVector2 mainTextureScale;

        [ProtoMember(7)]
        public long shader;

        [ProtoMember(8)]
        public string m_shaderName;

        public override object WriteTo(object obj)
        {
            obj = base.WriteTo(obj);
            if (obj == null)
            {
                return null;
            }

            Material o = (Material)obj;
            if (m_assetDB.IsMapped(shader))
            {
                o.shader = m_assetDB.FromID<Shader>(shader);
            }
            else
            {
                o.shader = Shader.Find(m_shaderName);
            }

            if (o.HasProperty("_MainTex"))
            {
                o.mainTextureOffset = (Vector2)mainTextureOffset.WriteTo(o.mainTextureOffset);
                o.mainTextureScale = (Vector2)mainTextureScale.WriteTo(o.mainTextureScale);
            }

            if (m_keywords != null)
            {
                foreach (string keyword in m_keywords)
                {
                    o.EnableKeyword(keyword);
                }
            }

            if (m_propertyNames != null)
            {
                for (int i = 0; i < m_propertyNames.Length; ++i)
                {
                    string name = m_propertyNames[i];
                    RTShaderPropertyType type = m_propertyTypes[i];
                    switch (type)
                    {
                        case RTShaderPropertyType.Color:
                            if (m_propertyValues[i].ValueBase is PersistentColor)
                            {
                                o.SetColor(name, (PersistentColor)m_propertyValues[i].ValueBase);
                            }
                            break;
                        case RTShaderPropertyType.Float:
                            if (m_propertyValues[i].ValueBase is float)
                            {
                                o.SetFloat(name, (float)m_propertyValues[i].ValueBase);
                            }
                            break;
                        case RTShaderPropertyType.Range:
                            if (m_propertyValues[i].ValueBase is float)
                            {
                                o.SetFloat(name, (float)m_propertyValues[i].ValueBase);
                            }
                            break;
                        case RTShaderPropertyType.TexEnv:
                            if (m_propertyValues[i].ValueBase is long)
                            {
                                o.SetTexture(name, FromID<Texture>((long)m_propertyValues[i].ValueBase));
                            }
                            break;
                        case RTShaderPropertyType.Vector:
                            if (m_propertyValues[i].ValueBase is PersistentVector4)
                            {
                                o.SetVector(name, (PersistentVector4)m_propertyValues[i].ValueBase);
                            }
                            break;
                        case RTShaderPropertyType.Unknown:
                            break;
                    }
                }
            }

            return obj;
        }

        public override void GetDeps(GetDepsContext context)
        {
            base.GetDeps(context);

            AddDep(shader, context);

            if (m_propertyValues != null)
            {
                for (int i = 0; i < m_propertyValues.Length; ++i)
                {
                    RTShaderPropertyType type = m_propertyTypes[i];
                    switch (type)
                    {
                        case RTShaderPropertyType.TexEnv:
                            if (m_propertyValues[i].ValueBase is long)
                            {
                                AddDep((long)m_propertyValues[i].ValueBase, context);
                            }
                            break;
                    }
                }
            }
        }

        public override void GetDepsFrom(object obj, GetDepsFromContext context)
        {
            base.GetDepsFrom(obj, context);
            if (obj == null)
            {
                return;
            }

            Material o = (Material)obj;
            AddDep(o.shader, context);

            RuntimeShaderInfo shaderInfo = null;
            IRuntimeShaderUtil shaderUtil = IOC.Resolve<IRuntimeShaderUtil>();
            if (shaderUtil != null)
            {
                shaderInfo = shaderUtil.GetShaderInfo(o.shader);
            }

            if (shaderInfo == null)
            {
                return;
            }

            for (int i = 0; i < shaderInfo.PropertyCount; ++i)
            {
                string name = shaderInfo.PropertyNames[i];
                RTShaderPropertyType type = shaderInfo.PropertyTypes[i];
                switch (type)
                {
                    case RTShaderPropertyType.TexEnv:
                        AddDep(o.GetTexture(name), context);
                        break;
                }
            }
        }

        public override void ReadFrom(object obj)
        {
            base.ReadFrom(obj);
            if (obj == null)
            {
                return;
            }

            Material o = (Material)obj;
            if (o.HasProperty("_MainTex"))
            {
                mainTextureOffset = new PersistentVector2();
                mainTextureOffset.ReadFrom(o.mainTextureOffset);
                mainTextureScale = new PersistentVector2();
                mainTextureScale.ReadFrom(o.mainTextureScale);
            }

            if (o.shader == null)
            {
                shader = m_assetDB.NullID;
                m_shaderName = null;
                return;
            }

            shader = m_assetDB.ToID(o.shader);
            m_shaderName = o.shader.name;

            RuntimeShaderInfo shaderInfo = null;
            IRuntimeShaderUtil shaderUtil = IOC.Resolve<IRuntimeShaderUtil>();
            if (shaderUtil != null)
            {
                shaderInfo = shaderUtil.GetShaderInfo(o.shader);
            }
            if (shaderInfo == null)
            {
                return;
            }

            m_propertyNames = new string[shaderInfo.PropertyCount];
            m_propertyTypes = new RTShaderPropertyType[shaderInfo.PropertyCount];
            m_propertyValues = new PrimitiveContract[shaderInfo.PropertyCount];

            for (int i = 0; i < shaderInfo.PropertyCount; ++i)
            {
                string name = shaderInfo.PropertyNames[i];
                RTShaderPropertyType type = shaderInfo.PropertyTypes[i];
                m_propertyNames[i] = name;
                m_propertyTypes[i] = type;
                switch (type)
                {
                    case RTShaderPropertyType.Color:
                        m_propertyValues[i] = PrimitiveContract.Create((PersistentColor)o.GetColor(name));
                        break;
                    case RTShaderPropertyType.Float:
                        m_propertyValues[i] = PrimitiveContract.Create(o.GetFloat(name));
                        break;
                    case RTShaderPropertyType.Range:
                        m_propertyValues[i] = PrimitiveContract.Create(o.GetFloat(name));
                        break;
                    case RTShaderPropertyType.TexEnv:
                        Texture2D texture = (Texture2D)o.GetTexture(name);
                        if (texture == null)
                        {
                            m_propertyValues[i] = PrimitiveContract.Create(m_assetDB.NullID);
                        }
                        else
                        {
                            m_propertyValues[i] = PrimitiveContract.Create(m_assetDB.ToID(texture));
                        }
                        break;
                    case RTShaderPropertyType.Vector:
                        m_propertyValues[i] = PrimitiveContract.Create((PersistentVector4)o.GetVector(name));
                        break;
                    case RTShaderPropertyType.Unknown:
                        m_propertyValues[i] = null;
                        break;
                }
            }

            m_keywords = o.shaderKeywords;
        }
        //<TEMPLATE_BODY_END>
#endif
    }
}


