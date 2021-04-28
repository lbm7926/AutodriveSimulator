using UnityEngine;
using System.Reflection;
using Battlehub.Utils;

namespace Battlehub.RTEditor
{
    public class MeshColliderComponentDescriptor : ComponentDescriptorBase<MeshCollider>
    {
        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter)
        {
            PropertyEditorCallback valueChanged = () => editor.BuildEditor();

            MemberInfo convexInfo = Strong.PropertyInfo((MeshCollider x) => x.convex, "convex");
            MemberInfo isTriggerInfo = Strong.PropertyInfo((MeshCollider x) => x.isTrigger, "isTrigger");
            MemberInfo materialInfo = Strong.PropertyInfo((MeshCollider x) => x.sharedMaterial, "sharedMaterial");
            MemberInfo meshInfo = Strong.PropertyInfo((MeshCollider x) => x.sharedMesh, "sharedMesh");

            MeshCollider collider = (MeshCollider)editor.Component;
            if (collider.convex)
            {
                return new[]
                {
                    new PropertyDescriptor("Convex", editor.Component, convexInfo, convexInfo, valueChanged),
                    new PropertyDescriptor("Is Trigger", editor.Component, isTriggerInfo, isTriggerInfo),
                    new PropertyDescriptor("Material", editor.Component, materialInfo, materialInfo),
                    new PropertyDescriptor("Mesh", editor.Component, meshInfo, meshInfo),
                };
            }
            else
            {
                return new[]
                {
                    new PropertyDescriptor("Convex", editor.Component, convexInfo, convexInfo, valueChanged),
                    new PropertyDescriptor("Material", editor.Component, materialInfo, materialInfo),
                    new PropertyDescriptor("Mesh", editor.Component, meshInfo, meshInfo),
                };
            }
        }
    }

}

