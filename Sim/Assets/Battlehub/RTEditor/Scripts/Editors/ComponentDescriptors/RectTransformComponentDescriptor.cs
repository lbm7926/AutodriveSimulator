using UnityEngine;
using System.Reflection;
using Battlehub.Utils;

namespace Battlehub.RTEditor
{
    public class RectTransformComponentDescriptor : ComponentDescriptorBase<RectTransform>
    {
        public override object CreateConverter(ComponentEditor editor)
        {
            TransformPropertyConverter converter = new TransformPropertyConverter();
            converter.Component = (Transform)editor.Component;
            return converter;
        }

        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converterObj)
        {
            TransformPropertyConverter converter = (TransformPropertyConverter)converterObj;

            MemberInfo position = Strong.PropertyInfo((Transform x) => x.localPosition, "position");
            MemberInfo rotation = Strong.PropertyInfo((Transform x) => x.localRotation, "rotation");
            MemberInfo rotationConverted = Strong.PropertyInfo((TransformPropertyConverter x) => x.Rotation, "Rotation");
            MemberInfo scale = Strong.PropertyInfo((Transform x) => x.localScale, "localScale");

            return new[]
                {
                    new PropertyDescriptor( "Position", editor.Component, position, position) ,
                    new PropertyDescriptor( "Rotation", converter, rotationConverted, rotation),
                    new PropertyDescriptor( "Scale", editor.Component, scale, scale)
                };
        }
    }
}

