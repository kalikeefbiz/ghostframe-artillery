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

        private void OnCollisionEnter2D(Collision2D collision) => HandleCollision(collision);
        private void OnCollisionStay2D(Collision2D collision) => HandleCollision(collision);

        private void HandleCollision(Collision2D collision)
        {
            if(age<.03f || !sigil || !owner) return;
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
                var body=owner.GetComponent<Rigidbody2D>();
                bool drivingIntoSurface=false;
                Vector2 point=collision.GetContact(0).point;
                for(int i=0;i<collision.contactCount;i++)
                {
                    var contact=collision.GetContact(i);
                    if(body && Vector2.Dot(body.linearVelocity,contact.normal)<-.15f)
                    { drivingIntoSurface=true;point=contact.point;break; }
                }
                if(drivingIntoSurface || age>.1f)
                {
                    hitTerrain=true;
                    if(impactSprite) SigilPresentation.Burst(impactSprite,point+Vector2.up*.35f,3.2f,.28f);
                    terrain.DestroyCircle(point,Mathf.Max(.35f,sigil.terrainDamageRadius));
                    owner.Movement.EndLaunch(true);
                }
            }
            if(hitTerrain) Destroy(this);
        }
    }
}