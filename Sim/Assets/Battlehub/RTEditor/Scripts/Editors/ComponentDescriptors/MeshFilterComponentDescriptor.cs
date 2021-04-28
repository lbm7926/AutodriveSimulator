using UnityEngine;
using System.Reflection;
using System;
using Battlehub.Utils;

namespace Battlehub.RTEditor
{
    public class MeshFilterComponentDescriptor : ComponentDescriptorBase<MeshFilter>
    {
        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter)
        {
            MemberInfo sharedMeshInfo = Strong.PropertyInfo((MeshFilter x) => x.sharedMesh, "sharedMesh");
            return new[]
            {
                new PropertyDescriptor("Mesh", editor.Component, sharedMeshInfo, sharedMeshInfo)
            };
        }
    }
}

