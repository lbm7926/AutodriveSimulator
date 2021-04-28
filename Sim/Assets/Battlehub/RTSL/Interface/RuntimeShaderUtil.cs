using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.RTSL
{
    public interface IRuntimeShaderUtil
    {
        RuntimeShaderInfo GetShaderInfo(Shader shader);
    }

    public class RuntimeShaderUtil : IRuntimeShaderUtil
    {
        private Dictionary<string, RuntimeShaderInfo> m_nameToShaderInfo;
        
        public RuntimeShaderUtil()
        {
            RuntimeShaderProfilesAsset asset = Resources.Load<RuntimeShaderProfilesAsset>("Lists/ShaderProfiles");
            if(asset == null)
            {
                Debug.LogError("Unable to find RuntimeShaderProfilesAsset. Click Tools->Runtime SaveLoad2->Libraries->Create Shader Profiles");
                return;
            }
            m_nameToShaderInfo = new Dictionary<string, RuntimeShaderInfo>();
            for(int i = 0; i < asset.ShaderInfo.Count; ++i)
            {
                RuntimeShaderInfo info = asset.ShaderInfo[i];
                if(info != null)
                {
                    if(m_nameToShaderInfo.ContainsKey(info.Name))
                    {
                        Debug.LogWarning("Shader with " + info.Name + " already exists.");
                    }
                    else
                    {
                        m_nameToShaderInfo.Add(info.Name, info);
                    }
                }
            }
        }

        public RuntimeShaderInfo GetShaderInfo(Shader shader)
        {
            RuntimeShaderInfo shaderInfo = null;
            if(m_nameToShaderInfo.TryGetValue(shader.name, out shaderInfo))
            {
                return shaderInfo;
            }
            return null;
        }
    }
}
    

