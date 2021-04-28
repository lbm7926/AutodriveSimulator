using Battlehub.RTCommon;
using System;

namespace Battlehub.RTEditor
{
    public struct HeaderDescriptor
    {
        public string DisplayName
        {
            get; set;
        }
        public bool ShowExpander
        {
            get; set;
        }

        public bool ShowResetButton
        {
            get; set;
        }

        public bool ShowEnableButton
        {
            get; set;
        }

        public bool ShowRemoveButton
        {
            get; set;
        }

        public HeaderDescriptor(string displayName, bool showExpander = true, bool showResetButton = true, bool showEnableButton = true, bool showRemoveButton = true)
        {
            DisplayName = displayName;
            ShowExpander = showExpander;
            ShowResetButton = showResetButton;
            ShowEnableButton = showEnableButton;
            ShowRemoveButton = showRemoveButton;
        }
    }

    public interface IComponentDescriptor
    {
        HeaderDescriptor GetHeaderDescriptor(IRTE editor);
        
        Type ComponentType { get; }

        Type GizmoType { get; }

        object CreateConverter(ComponentEditor editor);

        PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter);
    }

    public abstract class ComponentDescriptorBase<TComponent> : IComponentDescriptor
    {
        public virtual HeaderDescriptor GetHeaderDescriptor(IRTE editor)
        {
            return new HeaderDescriptor(
                ComponentType.Name,
                editor.ComponentEditorSettings.ShowExpander,
                editor.ComponentEditorSettings.ShowResetButton,
                editor.ComponentEditorSettings.ShowEnableButton);
        }

        public virtual Type ComponentType
        {
            get { return typeof(TComponent); }
        }

        public virtual Type GizmoType
        {
            get { return null; }
        }

        public virtual object CreateConverter(ComponentEditor editor)
        {
            return null;
        }

        public abstract PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter);
    }

    public abstract class ComponentDescriptorBase<TComponent, TGizmo> : ComponentDescriptorBase<TComponent>
    {
        public override Type GizmoType
        {
            get { return typeof(TGizmo); }
        }
    }

}