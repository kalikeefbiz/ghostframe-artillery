using System.Collections.Generic;
using UnityEngine;

namespace AetherWild
{
    public sealed class MirrorField : MonoBehaviour
    {
        private static readonly List<MirrorField> active=new List<MirrorField>();
        private Vector2 a,b;
        private int expireTurn;
        private LineRenderer line;

        public static MirrorField Create(Vector2 anchor,Vector2 facing,int currentTurn,int durationTurns)
        {
            var go=new GameObject("Mirror Sigil",typeof(LineRenderer),typeof(MirrorField));
            var field=go.GetComponent<MirrorField>();
            Vector2 tangent=facing.sqrMagnitude>.01f?facing.normalized:Vector2.up;
            Vector2 center=anchor+Vector2.up*1.25f;
            field.a=center-tangent*1.35f;
            field.b=center+tangent*1.35f;
            field.expireTurn=currentTurn+Mathf.Max(2,durationTurns*2);
            field.line=go.GetComponent<LineRenderer>();
            field.line.positionCount=2;
            field.line.SetPositions(new[]{(Vector3)field.a,(Vector3)field.b});
            field.line.startWidth=field.line.endWidth=.09f;
            field.line.startColor=field.line.endColor=new Color(.55f,.9f,1,1);
            field.line.material=new Material(Shader.Find("Sprites/Default"));
            field.line.sortingOrder=6;
            active.Add(field);
            return field;
        }

        public static bool TryRedirect(Vector2 from,Vector2 to,ref Vector2 instantaneousVelocity,out Vector2 intersection)
        {
            intersection=default;
            for(int i=active.Count-1;i>=0;i--)
            {
                var field=active[i];
                if(!field){active.RemoveAt(i);continue;}
                if(!Intersect(from,to,field.a,field.b,out intersection)) continue;
                Vector2 tangent=(field.b-field.a).normalized;
                Vector2 normal=new Vector2(-tangent.y,tangent.x);
                if(Vector2.Dot(instantaneousVelocity,normal)>0) normal=-normal;
                instantaneousVelocity=Vector2.Reflect(instantaneousVelocity,normal);
                field.Consume();
                return true;
            }
            return false;
        }

        public static void RemoveExpired(int turn)
        {
            for(int i=active.Count-1;i>=0;i--)
            {
                var field=active[i];
                if(!field){active.RemoveAt(i);continue;}
                if(turn>field.expireTurn) field.Consume();
            }
        }

        public static void ClearAll()
        {
            for(int i=active.Count-1;i>=0;i--) if(active[i]) Destroy(active[i].gameObject);
            active.Clear();
        }

        private void Consume()
        {
            active.Remove(this);
            Destroy(gameObject);
        }

        private static bool Intersect(Vector2 p,Vector2 p2,Vector2 q,Vector2 q2,out Vector2 hit)
        {
            hit=default;
            Vector2 r=p2-p,s=q2-q;
            float cross=r.x*s.y-r.y*s.x;
            if(Mathf.Abs(cross)<.0001f) return false;
            Vector2 qp=q-p;
            float t=(qp.x*s.y-qp.y*s.x)/cross;
            float u=(qp.x*r.y-qp.y*r.x)/cross;
            if(t<0 || t>1 || u<0 || u>1) return false;
            hit=p+t*r;
            return true;
        }

        private void OnDestroy()
        {
            active.Remove(this);
            if(line && line.material) Destroy(line.material);
        }
    }

    public sealed class ResoAnchor : MonoBehaviour
    {
        public SummonerCombat Owner {get;private set;}
        public int Slot {get;private set;}
        public int ExpireTurn {get;private set;}
        public Vector2 Point => transform.position;

        public static ResoAnchor Create(SummonerCombat owner,int slot,Vector2 point,int expireTurn,Sprite sprite)
        {
            var go=new GameObject("Embedded Reso Blade",typeof(SpriteRenderer),typeof(ResoAnchor));
            go.transform.position=point;
            var anchor=go.GetComponent<ResoAnchor>();
            anchor.Owner=owner;anchor.Slot=slot;anchor.ExpireTurn=expireTurn;
            var renderer=go.GetComponent<SpriteRenderer>();
            renderer.sprite=sprite;
            renderer.sortingOrder=6;
            renderer.color=Color.white;
            go.transform.localScale=Vector3.one*.38f;
            return anchor;
        }
    }
}