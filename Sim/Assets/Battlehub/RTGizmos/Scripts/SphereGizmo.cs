using System;
using Battlehub.RTCommon;
using UnityEngine;
namespace Battlehub.RTGizmos
{
    public abstract class SphereGizmo : BaseGizmo
    {
        protected abstract Vector3 Center
        {
            get;
            set;
        }

        protected abstract float Radius
        {
            get;
            set;
        }

        protected override Matrix4x4 HandlesTransform
        {
            get
            {
                Vector3 scale = Target.lossyScale;
                scale = Vector3.one * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                return Matrix4x4.TRS(Target.TransformPoint(Center), Target.rotation, scale * Radius);
            }
        }

        protected override Matrix4x4 HandlesTransformInverse
        {
            get
            {
                Vector3 scale = Target.lossyScale;
                scale = Vector3.one * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                return Matrix4x4.TRS(Target.position, Target.rotation, scale).inverse;
            }
        }

        protected override bool OnDrag(int index, Vector3 offset)
        {
            Radius += offset.magnitude * Math.Sign(Vector3.Dot(offset, HandlesNormals[index]));
            if(Radius < 0)
            {
                Radius = 0;
                return false;
            }
            return true;
        }

        protected override void DrawOverride(Camera camera)
        {
            base.DrawOverride(camera);

            if(Target == null)
            {
                return;
            }

            Vector3 scale = Target.lossyScale * Radius;

            scale = Vector3.one * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

            RuntimeGizmos.DrawCubeHandles(Target.TransformPoint(Center) , Target.rotation, scale , HandlesColor);
            RuntimeGizmos.DrawWireSphereGL(camera, Target.TransformPoint(Center), Target.rotation, scale, LineColor);

            if (IsDragging)
            {
                scale = Target.lossyScale;
                scale = Vector3.one * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                RuntimeGizmos.DrawSelection(HandlesTransform.MultiplyPoint(Center + HandlesPositions[DragIndex]), Target.rotation, scale, SelectionColor);
            }
        }
    }

}
