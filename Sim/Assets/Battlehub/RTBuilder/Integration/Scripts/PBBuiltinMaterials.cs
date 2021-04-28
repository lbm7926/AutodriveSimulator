using UnityEngine;
using UnityEngine.ProBuilder;

namespace Battlehub.ProBuilderIntegration
{
    public static class PBBuiltinMaterials
    {
        public static Material DefaultMaterial
        {
            get { return BuiltinMaterials.defaultMaterial; }
        }
 
        private static Material m_linesMaterial;
        public static Material LinesMaterial
        {
            get
            {
                if(m_linesMaterial == null)
                {
                    m_linesMaterial = new Material(Shader.Find(BuiltinMaterials.lineShader));
                }
                return m_linesMaterial;
            }
        }
    }
}
