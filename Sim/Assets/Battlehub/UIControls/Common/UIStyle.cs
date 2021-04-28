using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.UIControls
{
    public partial class UIStyle : MonoBehaviour
    {
        public string Name;

        public void ApplyImageColor(Color color)
        {
            Image image = GetComponent<Image>();
            if(image != null)
            {
                image.color = color;
            }
        }

        public void ApplyOutlineColor(Color color)
        {
            Outline outline = GetComponent<Outline>();
            if(outline != null)
            {
                outline.effectColor = color;
            }
        }

        public void ApplyTextColor(Color color)
        {
            TextMeshProUGUI text = GetComponent<TextMeshProUGUI>();
            if(text != null)
            {
                text.color = color;
            }
            else
            {
                Text uitext = GetComponent<Text>();
                if(uitext != null)
                {
                    uitext.color = color;
                }
            }
        }

        public void ApplyInputFieldColor(Color normalColor, Color highlighedColor, Color pressedColor, Color disabledColor, Color selectedColor)
        {
            TMP_InputField inputField = GetComponent<TMP_InputField>();
            if(inputField != null)
            {
                ColorBlock colors = inputField.colors;
                colors.normalColor = normalColor;
                colors.highlightedColor = highlighedColor;
                colors.pressedColor = pressedColor;
                colors.disabledColor = disabledColor;
                inputField.colors = colors;
                inputField.selectionColor = selectedColor;
            }
            else
            {
                InputField uiInputField = GetComponent<InputField>();

                ColorBlock colors = uiInputField.colors;
                colors.normalColor = normalColor;
                colors.highlightedColor = highlighedColor;
                colors.pressedColor = pressedColor;
                colors.disabledColor = disabledColor;
                uiInputField.colors = colors;
                uiInputField.selectionColor = selectedColor;
            }
        }

        public void ApplySelectableColor(Color normalColor, Color highlighedColor, Color pressedColor, Color disabledColor)
        {
            Selectable selectable = GetComponent<Selectable>();
            if(selectable != null)
            {
                ColorBlock colors = selectable.colors;
                colors.normalColor = normalColor;
                colors.highlightedColor = highlighedColor;
                colors.pressedColor = pressedColor;
                colors.disabledColor = disabledColor;
                selectable.colors = colors;
                
            }
        }
    }
}
