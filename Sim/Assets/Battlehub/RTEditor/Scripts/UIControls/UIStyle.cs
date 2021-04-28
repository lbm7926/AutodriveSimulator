using Battlehub.RTEditor;
using UnityEngine;

namespace Battlehub.UIControls
{
    public partial class UIStyle
    {
        public void ApplyHierarchyColors(Color enabledItem, Color disabledItem)
        {
            HierarchyView hierarchy = GetComponent<HierarchyView>();
            if (hierarchy != null)
            {
                hierarchy.EnabledItemColor = enabledItem;
                hierarchy.DisabledItemColor = disabledItem;
            }
        }
    }
}