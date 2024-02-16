using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Debugger
{
    public class DraggableBtn : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private bool isPressed = false;
        private Vector3 delta = new Vector3(0, 0, 0);
        private Vector3 startPos = new Vector3(0, 0, 0);
        private Action clickCallback = null;
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            startPos = transform.position;
            delta = transform.position - Input.mousePosition;
        }
        internal void Update()
        {
            if (isPressed)
            {
                transform.position = Input.mousePosition + delta;
            }
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            if ( TestValidClick(startPos, transform.position) &&
                clickCallback != null)
            {
                clickCallback();
            }
        }

        public void SetClickCallback(Action callback)
        {
            clickCallback = callback;
        }

        bool TestValidClick(Vector3 startPos, Vector3 newPos)
        {
            return ( Mathf.Abs(newPos.x - startPos.x)<2 && 
                     Mathf.Abs(newPos.y-startPos.y) < 2);
        }
    }
}
