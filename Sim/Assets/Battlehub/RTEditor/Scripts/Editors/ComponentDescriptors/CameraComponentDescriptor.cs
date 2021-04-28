using Battlehub.Utils;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public class CameraComponentDescriptor : ComponentDescriptorBase<Camera>
    {
        public enum Projection
        {
            Perspective,
            Orthographic
        }

        public class CameraPropertyConverter
        {
            public Projection Projection
            {
                get
                {
                    if (Component == null) {return Projection.Perspective; }
                    return Component.orthographic ? Projection.Orthographic : Projection.Perspective;
                }
                set
                {
                    if (Component == null) { return; }
                    Component.orthographic = value == Projection.Orthographic;
                }
            }

            public Camera Component { get; set; }
        }

        public override object CreateConverter(ComponentEditor editor)
        {
            CameraPropertyConverter converter = new CameraPropertyConverter();
            converter.Component = (Camera)editor.Component;
            return converter;
        }

        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter)
        {
            Camera camera = (Camera)editor.Component;

            PropertyEditorCallback valueChanged = () => editor.BuildEditor();
            MemberInfo projection = Strong.PropertyInfo((CameraPropertyConverter x) => x.Projection, "Projection");
            MemberInfo orthographic = Strong.PropertyInfo((Camera x) => x.orthographic, "orthographic");
            MemberInfo fov = Strong.PropertyInfo((Camera x) => x.fieldOfView, "fieldOfView");
            MemberInfo orthographicSize = Strong.PropertyInfo((Camera x) => x.orthographicSize, "orthographicSize");

            List<PropertyDescriptor> descriptors = new List<PropertyDescriptor>();
            descriptors.Add(new PropertyDescriptor("Projection", converter, projection, orthographic, valueChanged));
            
            if(!camera.orthographic)
            {
                descriptors.Add(new PropertyDescriptor("Field Of View", editor.Component, fov, fov));
            }
            else
            {
                descriptors.Add(new PropertyDescriptor("Size", editor.Component, orthographicSize, orthographicSize));
            }
            
            return descriptors.ToArray();
        }
    }

}

