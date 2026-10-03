using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AetherWild
{
    public sealed class HoldControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private readonly HashSet<int> pointers = new HashSet<int>();
        public bool Held => pointers.Count > 0;
        public System.Action Pressed;
        public void OnPointerDown(PointerEventData e)
        {
            if (pointers.Add(e.pointerId)) Pressed?.Invoke();
        }
        public void OnPointerUp(PointerEventData e) => pointers.Remove(e.pointerId);
        public void OnPointerExit(PointerEventData e) => pointers.Remove(e.pointerId);
        public void Clear() => pointers.Clear();
        private void OnDisable() => Clear();
        private void OnApplicationFocus(bool focused) { if (!focused) Clear(); }
        private void OnApplicationPause(bool paused) { if (paused) Clear(); }
    }
}
