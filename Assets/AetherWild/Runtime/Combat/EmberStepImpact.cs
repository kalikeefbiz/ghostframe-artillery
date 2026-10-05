using UnityEngine;
using UnityEngine.Tilemaps;

namespace AetherWild
{
    // Short-lived collision observer for the physical Ember Step launch.
    public sealed class EmberStepImpact : MonoBehaviour
    {
        private SummonerCombat owner,opponent;
        private TerrainSystem terrain;
        private SigilDefinition sigil;
        private Sprite impactSprite;
        private float age;
        private bool hitOpponent,hitTerrain;

        public void Initialize(SummonerCombat caster,SummonerCombat enemy,TerrainSystem map,SigilDefinition definition,
            Sprite presentation)
        {
            owner=caster;opponent=enemy;terrain=map;sigil=definition;impactSprite=presentation;
            Destroy(this,3f);
        }

        private void FixedUpdate(){age+=Time.fixedDeltaTime;}

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if(age<.08f || !sigil) return;
            var summoner=collision.collider.GetComponentInParent<SummonerCombat>();
            if(!hitOpponent && summoner && summoner==opponent)
            {
                hitOpponent=true;
                opponent.Health.Damage(Mathf.Max(0,sigil.baseDamage));
                float x=Mathf.Sign(opponent.transform.position.x-owner.transform.position.x);
                opponent.Movement.ApplyKnockback(new Vector2(x*Mathf.Max(2,sigil.knockbackForce),1.1f));
            }
            if(!hitTerrain && collision.collider.GetComponent<Tilemap>() && collision.contactCount>0)
            {
                hitTerrain=true;
                Vector2 point=collision.GetContact(0).point;
                if(impactSprite) SigilPresentation.Burst(impactSprite,point+Vector2.up*.35f,3.2f,.28f);
                terrain.DestroyCircle(point,Mathf.Max(.35f,sigil.terrainDamageRadius));
            }
            if(hitTerrain) Destroy(this);
        }
    }
}