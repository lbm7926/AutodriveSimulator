using Battlehub.UIControls.MenuControl;
using UnityEngine;

namespace Battlehub.UIControls
{
    public partial class UIStyle 
    {
        public void ApplyMainButtonColor(Color normal, Color pointerOver, Color focused)
        {
            MainMenuButton mainMenuButton = GetComponent<MainMenuButton>();
            if (mainMenuButton != null)
            {
                mainMenuButton.NormalColor = normal;
                mainMenuButton.PointerOverColor = pointerOver;
                mainMenuButton.FocusedColor = focused;
            }
        }

        public void ApplyMenuItemColor(Color selectionColor, Color textColor, Color disabledSelectionColor, Color disabledTextColor)
        {
            MenuItem menuItem = GetComponent<MenuItem>();
            if(menuItem != null)
            {
                menuItem.SelectionColor = selectionColor;
                menuItem.TextColor = textColor;
                menuItem.DisabledSelectionColor = disabledSelectionColor;
                menuItem.DisableTextColor = disabledTextColor;
            }
        }
    }
}