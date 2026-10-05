using System.Collections.Generic;
using UnityEngine;

namespace AetherWild
{
    // Presentation-only helpers. Combat/terrain rules remain authoritative elsewhere.
    public static class SigilPresentation
    {
        private static readonly List<GameObject> persistent=new List<GameObject>();
        public static SpriteRenderer Spawn(Sprite sprite,Vector2 point,float worldSize,int order=7,float rotation=0)
        {
            if(!sprite) return null;
            var go=new GameObject("Sigil presentation",typeof(SpriteRenderer));
            go.transform.position=point;
            go.transform.rotation=Quaternion.Euler(0,0,rotation);
            var renderer=go.GetComponent<SpriteRenderer>();
            renderer.sprite=sprite;
            renderer.color=Color.white;
            renderer.sortingOrder=order;
            Fit(go.transform,sprite,worldSize);
            return renderer;
        }

        public static void Burst(Sprite sprite,Vector2 point,float worldSize,float lifetime,float rotation=0)
        {
            var renderer=Spawn(sprite,point,worldSize,8,rotation);
            if(renderer) Object.Destroy(renderer.gameObject,Mathf.Max(.05f,lifetime));
        }

        public static SpriteRenderer Persistent(Sprite sprite,Vector2 point,float worldSize,int order=6,float rotation=0)
        {
            var renderer=Spawn(sprite,point,worldSize,order,rotation);
            if(renderer) persistent.Add(renderer.gameObject);
            return renderer;
        }

        public static void ClearPersistent()
        {
            for(int i=persistent.Count-1;i>=0;i--) if(persistent[i]) Object.Destroy(persistent[i]);
            persistent.Clear();
        }

        public static void Travel(Sprite sprite,Vector2 from,Vector2 to,float worldSize,float duration)
        {
            if(!sprite) return;
            var renderer=Spawn(sprite,from,worldSize,8);
            if(!renderer) return;
            var mover=renderer.gameObject.AddComponent<SigilTravelView>();
            mover.Initialize(from,to,Mathf.Max(.05f,duration));
        }

        public static void Fit(Transform target,Sprite sprite,float worldSize)
        {
            if(!target || !sprite) return;
            float longest=Mathf.Max(.01f,Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y));
            target.localScale=Vector3.one*(Mathf.Max(.05f,worldSize)/longest);
        }
    }

    public sealed class SigilTravelView : MonoBehaviour
    {
        private Vector2 start,end;
        private float duration,age;
        public void Initialize(Vector2 from,Vector2 to,float seconds)
        {
            start=from;end=to;duration=seconds;
            Vector2 delta=end-start;
            if(delta.sqrMagnitude>.001f)
                transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
        private void Update()
        {
            age+=Time.deltaTime;
            float t=Mathf.Clamp01(age/duration);
            transform.position=Vector2.Lerp(start,end,t);
            if(t>=1) Destroy(gameObject);
        }
    }
}