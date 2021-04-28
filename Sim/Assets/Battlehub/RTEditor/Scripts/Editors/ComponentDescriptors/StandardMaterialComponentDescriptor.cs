using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

using Battlehub.Utils;
using Battlehub.RTSL;

namespace Battlehub.RTEditor
{
    public class StandardMaterialValueConverter 
    {
        private enum WorkflowMode
        {
            Specular,
            Metallic,
            Dielectric
        }

        public enum SmoothnessMapChannel
        {
            SpecularMetallicAlpha,
            AlbedoAlpha,
        }


        public enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,       // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
        }

        public enum UVSec
        {
            UV0 = 0,
            UV1 = 1
        }


        private WorkflowMode m_workflow = WorkflowMode.Metallic;


        public BlendMode Mode
        {
            get { return (BlendMode)Material.GetFloat("_Mode"); }
            set
            {
                if (Mode != value)
                {
                    Material.SetFloat("_Mode", (float)value);
                    SetupMaterialWithBlendMode(Material, Mode);
                    SetMaterialKeywords(Material, m_workflow);
                }
            }
        }

        public float Cutoff
        {
            get { return Material.GetFloat("_Cutoff"); }
            set
            {
                Material.SetFloat("_Cutoff", value);
                SetMaterialKeywords(Material, m_workflow);
                SetupMaterialWithBlendMode(Material, Mode);
            }
        }

        public Texture MetallicGlossMap
        {
            get { return Material.GetTexture("_MetallicGlossMap"); }
            set
            {
                Material.SetTexture("_MetallicGlossMap", value);
                SetMaterialKeywords(Material, m_workflow);
                SetupMaterialWithBlendMode(Material, Mode);
            }
        }



        public Texture BumpMap
        {
            get { return Material.GetTexture("_BumpMap"); }
            set
            {
                Material.SetTexture("_BumpMap", value);
                SetMaterialKeywords(Material, m_workflow);
                SetupMaterialWithBlendMode(Material, Mode);
            }
        }

        public Texture ParallaxMap
        {
            get { return Material.GetTexture("_ParallaxMap"); }
            set
            {
                Material.SetTexture("_ParallaxMap", value);
                SetMaterialKeywords(Material, m_workflow);
                SetupMaterialWithBlendMode(Material, Mode);
            }
        }

        public Texture OcclusionMap
        {
            get { return Material.GetTexture("_OcclusionMap"); }
            set
            {
                Material.SetTexture("_OcclusionMap", value);
                SetMaterialKeywords(Material, m_workflow);
                SetupMaterialWithBlendMode(Material, Mode);
            }
        }

        public Texture EmissionMap
        {
            get { return Material.GetTexture("_EmissionMap"); }
            set
            {
                Material.SetTexture("_EmissionMap", value);
                SetMaterialKeywords(Material, m_workflow);
                SetupMaterialWithBlendMode(Material, Mode);
            }
        }

        public Color EmissionColor
        {
            get { return Material.GetColor("_EmissionColor"); }
            set
            {
                Material.SetColor("_EmissionColor", value);
                SetMaterialKeywords(Material, m_workflow);
                SetupMaterialWithBlendMode(Material, Mode);
            }
        }

        public Texture DetailMask
        {
            get { return Material.GetTexture("_DetailMask"); }
            set
            {
                Material.SetTexture("_DetailMask", value);
                SetMaterialKeywords(Material, m_workflow);
            }
        }

        public Texture DetailAlbedoMap
        {
            get { return Material.GetTexture("_DetailAlbedoMap"); }
            set
            {
                Material.SetTexture("_DetailAlbedoMap", value);
                SetMaterialKeywords(Material, m_workflow);
            }
        }

        public Texture DetailNormalMap
        {
            get { return Material.GetTexture("_DetailNormalMap"); }
            set
            {
                Material.SetTexture("_DetailNormalMap", value);
                SetMaterialKeywords(Material, m_workflow);
            }
        }

        public UVSec UVSecondary
        {
            get { return (UVSec)Material.GetFloat("_UVSec"); }
            set
            {
                Material.SetFloat("_UVSec", (float)value);
                SetMaterialKeywords(Material, m_workflow);
            }
        }

        public Vector2 Tiling
        {
            get { return Material.mainTextureScale; }
            set { Material.mainTextureScale = value; }
        }

        public Vector2 Offset
        {
            get { return Material.mainTextureOffset; }
            set { Material.mainTextureOffset = value; }
        }

        private static bool ShouldEmissionBeEnabled(Material mat, Color color)
        {
            var realtimeEmission = (mat.globalIlluminationFlags & MaterialGlobalIlluminationFlags.RealtimeEmissive) > 0;
            return color.maxColorComponent > 0.1f / 255.0f || realtimeEmission;
        }

        public static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
        {
            int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
            if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
                return SmoothnessMapChannel.AlbedoAlpha;
            else
                return SmoothnessMapChannel.SpecularMetallicAlpha;
        }


        private static void SetMaterialKeywords(Material material, WorkflowMode workflowMode)
        {
            SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap") || material.GetTexture("_DetailNormalMap"));
            if (workflowMode == WorkflowMode.Specular)
                SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
            else if (workflowMode == WorkflowMode.Metallic)
                SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
            SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
            SetKeyword(material, "_DETAIL_MULX2", material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap"));

            bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled(material, material.GetColor("_EmissionColor"));
            SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha);
            }

            // Setup lightmap emissive flags
            MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
            if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
            {
                flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                if (!shouldEmissionBeEnabled)
                    flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                material.globalIlluminationFlags = flags;
            }
        }

        static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = -1;
                    break;
                case BlendMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                    break;
                case BlendMode.Fade:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
                case BlendMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
            }
        }

        public Material Material
        {
            get;
            set;
        }
    }

    public class StandardMaterialDescriptor : IMaterialDescriptor
    {
        const string _Mode = "_Mode";
        const string _MainTex = "_MainTex";
        const string _Color = "_Color";
        const string _Cutoff = "_Cutoff";
        const string _MetallicGlossMap = "_MetallicGlossMap";
        const string _Metallic = "_Metallic";
        const string _Glossiness = "_Glossiness";
        const string _GlossMapScale = "_GlossMapScale";
        const string _BumpScale = "_BumpScale";
        const string _Parallax = "_Parallax";
        const string _OcclusionStrength = "_OcclusionStrength";
        const string _DetailNormalMapScale = "_DetailNormalMapScale";

        public enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,       // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
        }


        private BlendMode GetBlendMode(Material material)
        {
            return (BlendMode)material.GetFloat(_Mode);
        }

        private bool IsMetallicGlossMapSet(Material material)
        {
            Texture texture = material.GetTexture(_MetallicGlossMap);

            return texture != null;
        }


        public string ShaderName
        {
            get { return "Standard"; }
        }

        public object CreateConverter(MaterialEditor editor)
        {
            StandardMaterialValueConverter converter = new StandardMaterialValueConverter();
            converter.Material = editor.Material;
            return converter;
        }

        public void EraseAccessorTarget(object accessorRef, object target)
        {
            if(accessorRef is StandardMaterialValueConverter)
            {
                StandardMaterialValueConverter accessor = (StandardMaterialValueConverter)accessorRef;
                accessor.Material = target as Material;
            }
            else if(accessorRef is MaterialPropertyAccessor)
            {
                MaterialPropertyAccessor accessor = (MaterialPropertyAccessor)accessorRef;
                accessor.Material = target as Material;
            }
        }

        public MaterialPropertyDescriptor[] GetProperties(MaterialEditor editor, object converterObject)
        {
            PropertyEditorCallback valueChangedCallback = () => editor.BuildEditor();
            StandardMaterialValueConverter converter = (StandardMaterialValueConverter)converterObject;

            PropertyInfo modeInfo = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.Mode, "Mode");
            PropertyInfo cutoffInfo = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.Cutoff, "Cutoff");
            PropertyInfo metallicMapInfo = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.MetallicGlossMap, "MetallicGlossMap");
            PropertyInfo bumpMapInfo = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.BumpMap, "BumpMap");
            PropertyInfo parallaxMapInfo = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.ParallaxMap, "ParallaxMap");
            PropertyInfo occlusionMapInfo = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.OcclusionMap, "OcclusionMap");
            PropertyInfo emissionMapInfo = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.EmissionMap, "EmissionMap");
            PropertyInfo emissionColorInfo = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.EmissionColor, "EmissionColor");
            PropertyInfo detailMaskInfo = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.DetailMask, "DetailMask");
            PropertyInfo detailAlbedoMap = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.DetailAlbedoMap, "DetailAlbedoMap");
            PropertyInfo detailNormalMap = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.DetailNormalMap, "DetailNormalMap");
            // PropertyInfo uvSecondaryInfo = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.UVSecondary);

            PropertyInfo texInfo = Strong.PropertyInfo((MaterialPropertyAccessor x) => x.Texture, "Texture");
            PropertyInfo colorInfo = Strong.PropertyInfo((MaterialPropertyAccessor x) => x.Color, "Color");
            PropertyInfo floatInfo = Strong.PropertyInfo((MaterialPropertyAccessor x) => x.Float, "Float");

            BlendMode mode = GetBlendMode(editor.Material);
            List<MaterialPropertyDescriptor> properties = new List<MaterialPropertyDescriptor>();
            properties.Add(new MaterialPropertyDescriptor(editor.Material, converter, "Rendering Mode", RTShaderPropertyType.Float, modeInfo, new RuntimeShaderInfo.RangeLimits(), TextureDimension.None, valueChangedCallback, EraseAccessorTarget));
            properties.Add(new MaterialPropertyDescriptor(editor.Material, new MaterialPropertyAccessor(editor.Material, _MainTex), "Albedo", RTShaderPropertyType.TexEnv, texInfo, new RuntimeShaderInfo.RangeLimits(), TextureDimension.Tex2D, null, EraseAccessorTarget));
            properties.Add(new MaterialPropertyDescriptor(editor.Material, new MaterialPropertyAccessor(editor.Material, _Color), "Albedo Color", RTShaderPropertyType.Color, colorInfo, new RuntimeShaderInfo.RangeLimits(), TextureDimension.None, null, EraseAccessorTarget));
            if (mode == BlendMode.Cutout)
            {
                properties.Add(new MaterialPropertyDescriptor(editor.Material, converter, "Alpha Cutoff", RTShaderPropertyType.Range, cutoffInfo, new RuntimeShaderInfo.RangeLimits(0.5f, 0.0f, 1.0f), TextureDimension.None, null, EraseAccessorTarget));
            }

            properties.Add(new MaterialPropertyDescriptor(editor.Material, converter, "Metallic", RTShaderPropertyType.TexEnv, metallicMapInfo, new RuntimeShaderInfo.RangeLimits(0.0f, 0.0f, 0.0f), TextureDimension.Tex2D, valueChangedCallback, EraseAccessorTarget));
            bool hasGlossMap = IsMetallicGlossMapSet(editor.Material);
            if (!hasGlossMap)
            {
                properties.Add(new MaterialPropertyDescriptor(editor.Material, new MaterialPropertyAccessor(editor.Material, _Metallic), "Metallic", RTShaderPropertyType.Range, floatInfo, new RuntimeShaderInfo.RangeLimits(0.0f, 0.0f, 1.0f), TextureDimension.None, null, EraseAccessorTarget));
                if (StandardMaterialValueConverter.GetSmoothnessMapChannel(editor.Material) == StandardMaterialValueConverter.SmoothnessMapChannel.SpecularMetallicAlpha)
                {
                    properties.Add(new MaterialPropertyDescriptor(editor.Material, new MaterialPropertyAccessor(editor.Material, _Glossiness), "Smoothness", RTShaderPropertyType.Range, floatInfo, new RuntimeShaderInfo.RangeLimits(0.5f, 0.0f, 1.0f), TextureDimension.None, null, EraseAccessorTarget));
                }
                else
                {
                    properties.Add(new MaterialPropertyDescriptor(editor.Material, new MaterialPropertyAccessor(editor.Material, _GlossMapScale), "Smoothness", RTShaderPropertyType.Range, floatInfo, new RuntimeShaderInfo.RangeLimits(1.0f, 0.0f, 1.0f), TextureDimension.None, null, EraseAccessorTarget));
                }
            }
            else
            {
                properties.Add(new MaterialPropertyDescriptor(editor.Material, new MaterialPropertyAccessor(editor.Material, _GlossMapScale), "Smoothness", RTShaderPropertyType.Range, floatInfo, new RuntimeShaderInfo.RangeLimits(1.0f, 0.0f, 1.0f), TextureDimension.None, null, EraseAccessorTarget));
            }


            properties.Add(new MaterialPropertyDescriptor(editor.Material, converter, "Normal Map", RTShaderPropertyType.TexEnv, bumpMapInfo, new RuntimeShaderInfo.RangeLimits(0.0f, 0.0f, 0.0f), TextureDimension.Tex2D, valueChangedCallback, EraseAccessorTarget));
            if (converter.BumpMap != null)
            {
                properties.Add(new MaterialPropertyDescriptor(editor.Material, new MaterialPropertyAccessor(editor.Material, _BumpScale), "Normal Map Scale", RTShaderPropertyType.Float, floatInfo, new RuntimeShaderInfo.RangeLimits(0.0f, 0.0f, 0.0f), TextureDimension.None, null, EraseAccessorTarget));
            }

            properties.Add(new MaterialPropertyDescriptor(editor.Material, converter, "Height Map", RTShaderPropertyType.TexEnv, parallaxMapInfo, new RuntimeShaderInfo.RangeLimits(0.0f, 0.0f, 0.0f), TextureDimension.Tex2D, valueChangedCallback, EraseAccessorTarget));
            if (converter.ParallaxMap != null)
            {
                properties.Add(new MaterialPropertyDescriptor(editor.Material, new MaterialPropertyAccessor(editor.Material, _Parallax), "Height Map Scale", RTShaderPropertyType.Range, floatInfo, new RuntimeShaderInfo.RangeLimits(0.02f, 0.005f, 0.08f), TextureDimension.None, null, EraseAccessorTarget));
            }

            properties.Add(new MaterialPropertyDescriptor(editor.Material, converter, "Occlusion Map", RTShaderPropertyType.TexEnv, occlusionMapInfo, new RuntimeShaderInfo.RangeLimits(0.0f, 0.0f, 0.0f), TextureDimension.Tex2D, valueChangedCallback, EraseAccessorTarget));
            if (converter.OcclusionMap != null)
            {
                properties.Add(new MaterialPropertyDescriptor(editor.Material, new MaterialPropertyAccessor(editor.Material, _OcclusionStrength), "Occlusion Strength", RTShaderPropertyType.Range, floatInfo, new RuntimeShaderInfo.RangeLimits(1.0f, 0, 1.0f), TextureDimension.None, null, EraseAccessorTarget));
            }

            properties.Add(new MaterialPropertyDescriptor(editor.Material, converter, "Emission Map", RTShaderPropertyType.TexEnv, emissionMapInfo, new RuntimeShaderInfo.RangeLimits(0.0f, 0.0f, 0.0f), TextureDimension.Tex2D, null, EraseAccessorTarget));
            properties.Add(new MaterialPropertyDescriptor(editor.Material, converter, "Emission Color", RTShaderPropertyType.Color, emissionColorInfo, new RuntimeShaderInfo.RangeLimits(0.0f, 0.0f, 0.0f), TextureDimension.None, null, EraseAccessorTarget));
            properties.Add(new MaterialPropertyDescriptor(editor.Material, converter, "Detail Mask", RTShaderPropertyType.TexEnv, detailMaskInfo, new RuntimeShaderInfo.RangeLimits(0.0f, 0.0f, 0.0f), TextureDimension.Tex2D, null, EraseAccessorTarget));
            properties.Add(new MaterialPropertyDescriptor(editor.Material, converter, "Detail Albedo Map", RTShaderPropertyType.TexEnv, detailAlbedoMap, new RuntimeShaderInfo.RangeLimits(0.0f, 0.0f, 0.0f), TextureDimension.Tex2D, null, EraseAccessorTarget));
            properties.Add(new MaterialPropertyDescriptor(editor.Material, converter, "Detail Normal Map", RTShaderPropertyType.TexEnv, detailNormalMap, new RuntimeShaderInfo.RangeLimits(0.0f, 0.0f, 0.0f), TextureDimension.Tex2D, null, EraseAccessorTarget));
            properties.Add(new MaterialPropertyDescriptor(editor.Material, new MaterialPropertyAccessor(editor.Material, _DetailNormalMapScale), "Detail Scale", RTShaderPropertyType.Float, floatInfo, new RuntimeShaderInfo.RangeLimits(0, 0, 0), TextureDimension.None, null, EraseAccessorTarget));

            PropertyInfo tilingInfo = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.Tiling, "Tiling");
            PropertyInfo offsetInfo = Strong.PropertyInfo((StandardMaterialValueConverter x) => x.Offset, "Offset");
            properties.Add(new MaterialPropertyDescriptor(editor.Material, converter, "Tiling", RTShaderPropertyType.Vector, tilingInfo, new RuntimeShaderInfo.RangeLimits(), TextureDimension.None, null, EraseAccessorTarget));
            properties.Add(new MaterialPropertyDescriptor(editor.Material, converter, "Offset", RTShaderPropertyType.Vector, offsetInfo, new RuntimeShaderInfo.RangeLimits(), TextureDimension.None, null, EraseAccessorTarget));

            return properties.ToArray();
        }
    }
}
