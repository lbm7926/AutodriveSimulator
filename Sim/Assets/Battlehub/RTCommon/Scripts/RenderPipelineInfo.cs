using UnityEngine;
using UnityEngine.Rendering;

namespace Battlehub.RTCommon
{
    public enum RPType
    {
        Unknown,
        Legacy,
        LWRP,
        HDRP,
    }

    public static class RenderPipelineInfo 
    {
        public static readonly RPType Type;
        public static readonly string DefaultShaderName;

        private static Material m_defaultMaterial;
        public static Material DefaultMaterial
        {
            get
            {
                if(m_defaultMaterial == null)
                {
                    m_defaultMaterial = new Material(Shader.Find(DefaultShaderName));
                }

                return m_defaultMaterial;
            }
        }

        static RenderPipelineInfo()
        {
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                Type = RPType.Legacy;
                DefaultShaderName = "Standard";
            }
            else if(GraphicsSettings.renderPipelineAsset.GetType().Name == "LightweightPipelineAsset")
            {
                Type = RPType.LWRP;
                DefaultShaderName = "Lightweight Render Pipeline/Lit";
            }
            else if(GraphicsSettings.renderPipelineAsset.GetType().Name == "HDRenderPipelineAsset")
            {
                Type = RPType.HDRP;
                DefaultShaderName = "HD Render Pipeline/Lit";
            }
            else
            {
                Type = RPType.Unknown;
            }
        }


    }
}

