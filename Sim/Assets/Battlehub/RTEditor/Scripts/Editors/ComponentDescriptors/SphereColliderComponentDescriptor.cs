using UnityEngine;
using System.Reflection;
using Battlehub.Utils;
using Battlehub.RTGizmos;

namespace Battlehub.RTEditor
{
    public class SphereColliderComponentDescriptor : ComponentDescriptorBase<SphereCollider, SphereColliderGizmo>
    {
        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter)
        {
            MemberInfo isTriggerInfo = Strong.PropertyInfo((SphereCollider x) => x.isTrigger, "isTrigger");
            MemberInfo materialInfo = Strong.PropertyInfo((SphereCollider x) => x.sharedMaterial, "sharedMaterial");
            MemberInfo centerInfo = Strong.PropertyInfo((SphereCollider x) => x.center, "center");
            MemberInfo radiusInfo = Strong.PropertyInfo((SphereCollider x) => x.radius, "radius");


            return new[]
            {
                new PropertyDescriptor("Is Trigger", editor.Component, isTriggerInfo, isTriggerInfo),
                new PropertyDescriptor("Material", editor.Component, materialInfo, materialInfo),
                new PropertyDescriptor("Center", editor.Component, centerInfo, centerInfo),
                new PropertyDescriptor("Radius", editor.Component, radiusInfo, radiusInfo),
            };
        }
    }
}

