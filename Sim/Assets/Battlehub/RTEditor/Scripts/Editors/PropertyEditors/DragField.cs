using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Battlehub.RTCommon;
using TMPro;

namespace Battlehub.RTEditor
{
    public class DragField : MonoBehaviour, IDragHandler, IBeginDragHandler, IDropHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public TMP_InputField Field;
        public float IncrementFactor = 0.1f;
        public Texture2D DragCursor;
        public UnityEvent BeginDrag;
        public UnityEvent EndDrag;

        private IRTE m_editor;

        private void Start()
        {
            m_editor = IOC.Resolve<IRTE>();

            if(Field == null)
            {
                Debug.LogWarning("Set Field " + gameObject.name);
                return;
            }
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            BeginDrag.Invoke();  
        }

        void IDropHandler.OnDrop(PointerEventData eventData)
        {
            EndDrag.Invoke();
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            EndDrag.Invoke();
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (Field == null)
            {
                return;
            }

            float d;
            if (float.TryParse(Field.text, out d))
            {
                d += IncrementFactor * eventData.delta.x;
                Field.text = d.ToString();
            }
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            m_editor.CursorHelper.SetCursor(this, DragCursor, new Vector2(0.5f, 0.5f), CursorMode.Auto);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            m_editor.CursorHelper.ResetCursor(this);
        }
    }

}
