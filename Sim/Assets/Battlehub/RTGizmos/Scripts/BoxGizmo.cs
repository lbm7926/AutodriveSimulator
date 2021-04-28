using UnityEngine;
using Battlehub.RTCommon;
using UnityEngine.Events;

namespace Battlehub.RTGizmos
{
    public abstract class BoxGizmo : BaseGizmo
    {
        protected abstract Bounds Bounds
        {
            get;
            set;
        }

        protected override Matrix4x4 HandlesTransform
        {
            get
            {
                return Matrix4x4.TRS(Target.TransformPoint(Bounds.center), Target.rotation, Vector3.Scale(Bounds.extents, Target.lossyScale));
            }
        }

 
        protected override bool OnDrag(int index, Vector3 offset)
        {
       
            Bounds b = Bounds;
            b.center += offset / 2;
            b.extents += Vector3.Scale(offset / 2, HandlesPositions[index]);
            Bounds = b;
            return true;
        }


        protected override void DrawOverride(Camera camera)
        {
            base.DrawOverride(camera);

            Bounds b = Bounds;

            Vector3 parentScale = Target.parent == null ? Vector3.one : Target.parent.lossyScale;
            Vector3 scale = Vector3.Scale(Vector3.Scale(b.extents, Target.localScale), parentScale);
            RuntimeGizmos.DrawCubeHandles(Target.TransformPoint(b.center) , Target.rotation, scale, HandlesColor);
            RuntimeGizmos.DrawWireCubeGL(ref b, Target.TransformPoint(b.center), Target.rotation, Target.lossyScale, LineColor);

            if (IsDragging)
            {
                RuntimeGizmos.DrawSelection(Target.TransformPoint(b.center + Vector3.Scale(HandlesPositions[DragIndex], b.extents)), Target.rotation, Target.lossyScale, SelectionColor);
            }
        }
    }

}
