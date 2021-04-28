//#define SIMPLIFIED_MESHRENDERER

using UnityEngine;
using System.Reflection;
using System;

using Battlehub.Utils;
using System.Linq;

namespace Battlehub.RTEditor
{
#if SIMPLIFIED_MESHRENDERER

    public class MeshRendererComponentDescriptor : ComponentDescriptorBase<MeshRenderer>
    {
        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter)
        {
            MemberInfo materials = Strong.PropertyInfo((MeshRenderer x) => x.sharedMaterials, "sharedMaterials");
            return new[]
                {
                    new PropertyDescriptor( "Materials", editor.Component, materials, materials),
                };
        }
    }
#else
    public class MeshRendererComponentDescriptor : ComponentDescriptorBase<MeshRenderer>
    {
        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter)
        {
            MemberInfo shadowCastingMode = Strong.PropertyInfo((MeshRenderer x) => x.shadowCastingMode, "shadowCastingMode");
            MemberInfo receiveShadows = Strong.PropertyInfo((MeshRenderer x) => x.receiveShadows, "receiveShadows");
            MemberInfo materials = Strong.PropertyInfo((MeshRenderer x) => x.sharedMaterials, "sharedMaterials");
            MemberInfo lightProbes = Strong.PropertyInfo((MeshRenderer x) => x.lightProbeUsage, "lightProbeUsage");
            MemberInfo reflectionProbes = Strong.PropertyInfo((MeshRenderer x) => x.reflectionProbeUsage, "reflectionProbeUsage");
            MemberInfo anchorOverride = Strong.PropertyInfo((MeshRenderer x) => x.probeAnchor, "probeAnchor");

            return new[]
                {
                    new PropertyDescriptor( "Cast Shadows", editor.Component, shadowCastingMode, shadowCastingMode),
                    new PropertyDescriptor( "Receive Shadows", editor.Component, receiveShadows, receiveShadows),
                    new PropertyDescriptor( "Materials", editor.Component, materials, materials),
                    new PropertyDescriptor( "Light Probes", editor.Component, lightProbes, lightProbes),
                    new PropertyDescriptor( "Reflection Probes", editor.Component, reflectionProbes, reflectionProbes),
                    new PropertyDescriptor( "Anchor Override", editor.Component, anchorOverride, anchorOverride),
                };
        }
    }
#endif
}