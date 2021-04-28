using UnityEngine;
using System.Reflection;
using Battlehub.Utils;
using Battlehub.RTGizmos;

namespace Battlehub.RTEditor
{
    public class CapsuleColliderPropertyConverter 
    {
        public enum CapsuleColliderDirection
        {
            X,
            Y,
            Z
        }

        public CapsuleColliderDirection Direction
        {
            get
            {
                if(Component == null)
                {
                    return CapsuleColliderDirection.X;
                }

                return (CapsuleColliderDirection)Component.direction;
            }
            set
            {
                if (Component == null)
                {
                    return;
                }
                Component.direction = (int)value;
            }
        }

        public CapsuleCollider Component
        {
            get;
            set;
        }
    }

    public class CapsuleColliderComponentDescriptor : ComponentDescriptorBase<CapsuleCollider, CapsuleColliderGizmo>
    {
        public override object CreateConverter(ComponentEditor editor)
        {
            CapsuleColliderPropertyConverter converter = new CapsuleColliderPropertyConverter();
            converter.Component = (CapsuleCollider)editor.Component;
            return converter;
        }

        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converterObj)
        {
            CapsuleColliderPropertyConverter converter = (CapsuleColliderPropertyConverter)converterObj;

            MemberInfo isTriggerInfo = Strong.PropertyInfo((CapsuleCollider x) => x.isTrigger, "isTrigger");
            MemberInfo materialInfo = Strong.PropertyInfo((CapsuleCollider x) => x.sharedMaterial, "sharedMaterial");
            MemberInfo centerInfo = Strong.PropertyInfo((CapsuleCollider x) => x.center, "center");
            MemberInfo radiusInfo = Strong.PropertyInfo((CapsuleCollider x) => x.radius, "radius");
            MemberInfo heightInfo = Strong.PropertyInfo((CapsuleCollider x) => x.height, "height");
            MemberInfo directionInfo = Strong.PropertyInfo((CapsuleCollider x) => x.direction, "direction");
            MemberInfo directionConvertedInfo = Strong.PropertyInfo((CapsuleColliderPropertyConverter x) => x.Direction, "Direction");

            return new[]
            {
                new PropertyDescriptor("Is Trigger", editor.Component, isTriggerInfo, isTriggerInfo),
                new PropertyDescriptor("Material", editor.Component, materialInfo, materialInfo),
                new PropertyDescriptor("Center", editor.Component, centerInfo, centerInfo),
                new PropertyDescriptor("Radius", editor.Component, radiusInfo, radiusInfo),
                new PropertyDescriptor("Height", editor.Component, heightInfo, heightInfo),
                new PropertyDescriptor("Direction", converter, directionConvertedInfo, directionInfo),
            };
        }
    }
}

