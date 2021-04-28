using Battlehub.RTCommon;
using Battlehub.Utils;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public class TransformEditor : ComponentEditor
    {
        protected override void InitEditor(PropertyEditor editor, PropertyDescriptor descriptor)
        {
            base.InitEditor(editor, descriptor);

            bool canTransform = true;
            if(Component != null)
            {
                ExposeToEditor exposeToEditor = Component.gameObject.GetComponentInParent<ExposeToEditor>();
                if(exposeToEditor != null && !exposeToEditor.CanTransform)
                {
                    canTransform = false;
                }
            }

            if(Editor.Tools.LockAxes == null && canTransform)
            {
                return;
            }

            if (descriptor.ComponentMemberInfo == Strong.PropertyInfo((Transform x) => x.localPosition, "localPosition"))
            {
                Vector3Editor vector3Editor = (Vector3Editor)editor;
                if (!canTransform)
                {
                    vector3Editor.IsXInteractable = false;
                    vector3Editor.IsYInteractable = false;
                    vector3Editor.IsZInteractable = false;
                }
                else if (Editor.Tools.LockAxes != null)
                {
                    vector3Editor.IsXInteractable = !Editor.Tools.LockAxes.PositionX;
                    vector3Editor.IsYInteractable = !Editor.Tools.LockAxes.PositionY;
                    vector3Editor.IsZInteractable = !Editor.Tools.LockAxes.PositionZ;
                }
            }

            if (descriptor.ComponentMemberInfo == Strong.PropertyInfo((Transform x) => x.localRotation, "localRotation"))
            {
                Vector3Editor vector3Editor = (Vector3Editor)editor;
                if (!canTransform)
                {
                    vector3Editor.IsXInteractable = false;
                    vector3Editor.IsYInteractable = false;
                    vector3Editor.IsZInteractable = false;
                }
                else if(Editor.Tools.LockAxes != null)
                {
                    vector3Editor.IsXInteractable = !Editor.Tools.LockAxes.RotationX;
                    vector3Editor.IsYInteractable = !Editor.Tools.LockAxes.RotationY;
                    vector3Editor.IsZInteractable = !Editor.Tools.LockAxes.RotationZ;
                }
            }

            if (descriptor.ComponentMemberInfo == Strong.PropertyInfo((Transform x) => x.localScale, "localScale"))
            {
                Vector3Editor vector3Editor = (Vector3Editor)editor;
                if (!canTransform)
                {
                    vector3Editor.IsXInteractable = false;
                    vector3Editor.IsYInteractable = false;
                    vector3Editor.IsZInteractable = false;
                }
                else if (Editor.Tools.LockAxes != null)
                {
                    vector3Editor.IsXInteractable = !Editor.Tools.LockAxes.ScaleX;
                    vector3Editor.IsYInteractable = !Editor.Tools.LockAxes.ScaleY;
                    vector3Editor.IsZInteractable = !Editor.Tools.LockAxes.ScaleZ;
                }
            }
        }
    }
}

