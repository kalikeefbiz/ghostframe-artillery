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
        private LineRenderer placement, placementOutline;
        private readonly SpriteRenderer[] dots = new SpriteRenderer[14];
        public Vector2 Direction { get; private set; }
        public float Power { get; private set; }
        public Vector2 Target { get; private set; }
        public bool TargetValid
        {
            get
            {
                var s=match.Player.Loadout.Get(match.SelectedSlot);
                if(s.form==SigilForm.Construct) return match.Terrain.WallPosition(Target,match.Player,s.targetingRange,
                    s.wallSize*match.Player.Bonus(s,"terrain"),out _);
                if(s.form==SigilForm.Shift) return match.Terrain.Standing(Target,match.Player,match.Enemy,
                    s.displacementDistance*match.Player.Bonus(s,"movement"),out _);
                return true;
            }
        }
        public void Initialize(MatchManager session, Sprite sprite)
        {
            match = session;
            vector = MakeMarker("Aim direction", sprite, new Color(1, 0.96f, 0.3f));
            for (int i = 0; i < dots.Length; i++)
                dots[i] = MakeMarker("Partial trajectory", sprite, new Color(.65f,1,1,1));
            placementOutline=MakeOutline("Wall footprint shadow",.15f,Color.black,7);
            placement=MakeOutline("Wall footprint",.07f,Color.green,8);
            ResetAim();
        }
        private LineRenderer MakeOutline(string label,float width,Color color,int order)
        {
            var line=new GameObject(label,typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.sharedMaterial=vector.sharedMaterial;
            line.startWidth=line.endWidth=width;
            line.startColor=line.endColor=color;
            line.sortingOrder=order;line.loop=true;line.positionCount=4;
            line.enabled=false;
            return line;
        }
        private SpriteRenderer MakeMarker(string label, Sprite sprite, Color color)
        {
            var go = new GameObject(label, typeof(SpriteRenderer));
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 8;
            var shadow=new GameObject("Dark outline",typeof(SpriteRenderer));
            shadow.transform.SetParent(go.transform,false);
            shadow.transform.localScale=new Vector3(1.35f,1.8f,1);
            var outline=shadow.GetComponent<SpriteRenderer>();
            outline.sprite=sprite;outline.color=new Color(.015f,.025f,.035f,1);outline.sortingOrder=7;
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
            Target=Camera.main.ScreenToWorldPoint(e.position);
        }
        public void OnDrag(PointerEventData e)
        {
            if (pointer != e.pointerId || !match.PlayerCanAct) return;
            Target=Camera.main.ScreenToWorldPoint(e.position);
            if(!match.Player.Loadout.Get(match.SelectedSlot).usesProjectile) return;
            Vector2 drag = e.position - start;
            if (drag.magnitude < 8) return;
            var sigil = match.Player.Loadout.Get(match.SelectedSlot);
            Direction = drag.normalized;
            float fullDrag = Mathf.Max(80, Mathf.Min(Screen.width, Screen.height) * 0.36f);
            Power = Mathf.Lerp(sigil.launchPowerMin, sigil.launchPowerMax, Mathf.Clamp01(drag.magnitude / fullDrag));
        }
        public void OnPointerUp(PointerEventData e) { if (pointer == e.pointerId) CancelDrag(); }
        private void LateUpdate()
        {
            if (!match) return;
            bool visible = match.PlayerCanAct;
            vector.gameObject.SetActive(visible);
            for (int i = 0; i < dots.Length; i++) dots[i].gameObject.SetActive(visible);
            placement.enabled=placementOutline.enabled=false;
            if (!visible) { CancelDrag(); return; }
            var sigil = match.Player.Loadout.Get(match.SelectedSlot);
            if(!sigil.usesProjectile)
            {
                foreach(var dot in dots) dot.gameObject.SetActive(false);
                vector.gameObject.SetActive(sigil.form!=SigilForm.Ward);
                vector.transform.position=Target;
                vector.transform.rotation=Quaternion.identity;
                vector.transform.localScale=new Vector3(.7f,.22f,1);
                bool valid=TargetValid;
                vector.color=valid?new Color(.25f,1,.35f):new Color(1,.2f,.18f);
                if(sigil.form==SigilForm.Construct)
                {
                    Vector2 size=sigil.wallSize*match.Player.Bonus(sigil,"terrain");
                    match.Terrain.WallPosition(Target,match.Player,sigil.targetingRange,size,out var bottom);
                    if(!valid) bottom=Target;
                    vector.transform.position=bottom;
                    var corners=new[]{(Vector3)(bottom+Vector2.left*size.x/2),
                        (Vector3)(bottom+Vector2.right*size.x/2),
                        (Vector3)(bottom+new Vector2(size.x/2,size.y)),
                        (Vector3)(bottom+new Vector2(-size.x/2,size.y))};
                    placement.SetPositions(corners);placementOutline.SetPositions(corners);
                    placement.startColor=placement.endColor=vector.color;
                    placement.enabled=placementOutline.enabled=true;
                }
                return;
            }
            vector.color=new Color(1,.96f,.3f);
            Vector2 origin = match.Player.LaunchOrigin;
            vector.transform.position = origin + Direction * 0.7f;
            vector.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg);
            vector.transform.localScale = new Vector3(1.4f, 0.09f, 1);
            // Fixed short time window, never a computed landing marker.
            for (int i = 0; i < dots.Length; i++)
            {
                float time = (i + 1) * 0.04f;
                dots[i].transform.position = Ballistics.Position(origin, Direction * sigil.Speed(Power), sigil.Gravity, time);
                dots[i].transform.localScale = Vector3.one * 0.13f;
            }
        }
        private void OnDisable() => CancelDrag();
        private void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
        private void OnApplicationPause(bool paused) { if (paused) CancelDrag(); }
        private void OnDestroy()
        {
            if (vector) Destroy(vector.gameObject);
            foreach (var dot in dots) if (dot) Destroy(dot.gameObject);
            if(placement) Destroy(placement.gameObject);
            if(placementOutline) Destroy(placementOutline.gameObject);
        }
    }
}
