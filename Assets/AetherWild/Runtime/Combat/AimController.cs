using UnityEngine;
using UnityEngine.EventSystems;

namespace AetherWild
{
    // Drag on the battlefield; release stores aim only. FIRE is a separate UI button.
    public sealed class AimController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private MatchManager match;
        private int? pointer;
        private Vector2 start;
        private SpriteRenderer vector;
        private readonly SpriteRenderer[] dots = new SpriteRenderer[14];
        public Vector2 Direction { get; private set; }
        public float Power { get; private set; }
        public void Initialize(MatchManager session, Sprite sprite)
        {
            match = session;
            vector = MakeMarker("Aim direction", sprite, new Color(1, 0.86f, 0.38f));
            for (int i = 0; i < dots.Length; i++)
                dots[i] = MakeMarker("Partial trajectory", sprite, new Color(0.65f, 0.85f, 1, 0.85f));
            ResetAim();
        }
        private SpriteRenderer MakeMarker(string label, Sprite sprite, Color color)
        {
            var go = new GameObject(label, typeof(SpriteRenderer));
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 4;
            return renderer;
        }
        public void ResetAim()
        {
            CancelDrag();
            Direction = new Vector2(1, 1).normalized;
            Power = 0.8f;
        }
        public void CancelDrag() => pointer = null;
        public void OnPointerDown(PointerEventData e)
        {
            if (!match.PlayerCanAct || pointer.HasValue) return;
            pointer = e.pointerId;
            start = e.position;
        }
        public void OnDrag(PointerEventData e)
        {
            if (pointer != e.pointerId || !match.PlayerCanAct) return;
            Vector2 drag = e.position - start;
            if (drag.magnitude < 8) return;
            var sigil = match.Player.Loadout.Get(0);
            Direction = drag.normalized;
            float fullDrag = Mathf.Max(80, Mathf.Min(Screen.width, Screen.height) * 0.36f);
            Power = Mathf.Lerp(sigil.launchPowerMin, sigil.launchPowerMax, Mathf.Clamp01(drag.magnitude / fullDrag));
        }
        public void OnPointerUp(PointerEventData e) { if (pointer == e.pointerId) CancelDrag(); }
        private void LateUpdate()
        {
            if (!match) return;
            bool visible = match.PlayerCanAct;
            vector.enabled = visible;
            for (int i = 0; i < dots.Length; i++) dots[i].enabled = visible;
            if (!visible) { CancelDrag(); return; }
            var sigil = match.Player.Loadout.Get(0);
            Vector2 origin = match.Player.LaunchOrigin;
            vector.transform.position = origin + Direction * 0.7f;
            vector.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg);
            vector.transform.localScale = new Vector3(1.4f, 0.055f, 1);
            // Fixed short time window, never a computed landing marker.
            for (int i = 0; i < dots.Length; i++)
            {
                float time = (i + 1) * 0.04f;
                dots[i].transform.position = Ballistics.Position(origin, Direction * sigil.Speed(Power), sigil.Gravity, time);
                dots[i].transform.localScale = Vector3.one * 0.075f;
            }
        }
        private void OnDisable() => CancelDrag();
        private void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelDrag(); }
        private void OnDestroy()
        {
            if (vector) Destroy(vector.gameObject);
            foreach (var dot in dots) if (dot) Destroy(dot.gameObject);
        }
    }
}
