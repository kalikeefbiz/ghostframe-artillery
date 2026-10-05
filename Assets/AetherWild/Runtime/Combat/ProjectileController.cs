using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AetherWild
{
    // Analytic constant-gravity path, sampled in fixed 5ms steps with swept-circle collision.
    // No Rigidbody integration drift, no trigger damage, and a single resolution guard.
    public sealed class ProjectileController : MonoBehaviour
    {
        private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
        private readonly Collider2D[] overlaps = new Collider2D[32];
        private Vector2 origin, velocity, gravity;
        private float age;
        private SigilDefinition definition;
        private SummonerCombat owner;
        private Action<Collider2D, Vector2> onResolved;
        private bool resolved;
        private bool rolling;
        private Rigidbody2D rollingBody;
        private CircleCollider2D rollingCollider;
        private Collider2D lastTerrain;
        private PhysicsMaterial2D rollingMaterial;
        private float rollingAge, stableAge, groundedGrace;
        private const float Step = 0.005f;
        private const float MaxLifetime = 10;
        private float minimumY=-12;

        public void Initialize(SummonerCombat caster, SigilDefinition sigil, Vector2 direction, float power,
            Action<Collider2D, Vector2> callback)
        {
            owner = caster;
            definition = sigil;
            origin = caster.LaunchOrigin;
            velocity = direction.normalized * sigil.Speed(power);
            gravity = sigil.Gravity;
            transform.position = origin;
            onResolved = callback;
            var terrain=FindFirstObjectByType<TerrainSystem>();
            if(terrain) minimumY=terrain.Map.killY-2;
        }

        private bool Valid(Collider2D collider)
            => collider && !collider.isTrigger && collider.GetComponentInParent<SummonerCombat>() != owner;

        private void FixedUpdate()
        {
            if (resolved || !definition) return;
            if(rolling)
            {
                rollingAge+=Time.fixedDeltaTime;
                if(groundedGrace>0) groundedGrace-=Time.fixedDeltaTime;
                bool slow=rollingBody && rollingBody.linearVelocity.magnitude<1.15f;
                if(slow && groundedGrace>0) stableAge+=Time.fixedDeltaTime;
                else stableAge=0;
                if((stableAge>=.18f && lastTerrain) || rollingAge>=2.25f)
                {
                    Resolve(lastTerrain,transform.position);
                    return;
                }
                return;
            }
            // Query the initial overlap as well, including shots launched against a wall.
            Physics2D.SyncTransforms();
            float end = Mathf.Min(age + Time.fixedDeltaTime, MaxLifetime);
            while (!resolved && age < end)
            {
                float nextAge = Mathf.Min(age + Step, end);
                Vector2 from = Ballistics.Position(origin, velocity, gravity, age);
                var filter = new ContactFilter2D { useTriggers = false };
                int overlapCount = Physics2D.OverlapCircle(from, definition.collisionRadius, filter, overlaps);
                for (int i = 0; i < overlapCount; i++)
                    if (Valid(overlaps[i])) { Resolve(overlaps[i], from); return; }
                Vector2 to = Ballistics.Position(origin, velocity, gravity, nextAge);
                Vector2 instantaneous=velocity+gravity*nextAge;
                if(definition.projectileMotion==ProjectileMotion.Arrow && instantaneous.sqrMagnitude>.001f)
                    transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(instantaneous.y,instantaneous.x)*Mathf.Rad2Deg);
                if(MirrorField.TryRedirect(from,to,ref instantaneous,out var mirrorHit))
                {
                    origin=mirrorHit+instantaneous.normalized*.04f;
                    velocity=instantaneous;
                    age=0;
                    transform.position=origin;
                    continue;
                }
                Vector2 delta = to - from;
                int count = Physics2D.CircleCast(from, definition.collisionRadius, delta.normalized,
                    filter, hits, delta.magnitude);
                RaycastHit2D nearest = default;
                float distance = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                    if (Valid(hits[i].collider) && hits[i].distance < distance)
                    { nearest = hits[i]; distance = hits[i].distance; }
                if (nearest.collider)
                {
                    if(definition.projectileMotion==ProjectileMotion.RollingBomb &&
                        nearest.collider.GetComponent<Tilemap>())
                    {
                        BeginRolling(nearest,instantaneous);
                        return;
                    }
                    Resolve(nearest.collider, nearest.point); return;
                }
                age = nextAge;
                transform.position = to;
                if (to.y < minimumY || Mathf.Abs(to.x) > 80 || age >= MaxLifetime)
                { Resolve(null, to); return; }
            }
        }

        private void BeginRolling(RaycastHit2D impact,Vector2 impactVelocity)
        {
            rolling=true;
            rollingAge=stableAge=0;
            lastTerrain=impact.collider;
            transform.position=impact.point+impact.normal*Mathf.Max(.03f,definition.collisionRadius*.8f);
            rollingBody=gameObject.AddComponent<Rigidbody2D>();
            rollingBody.gravityScale=Mathf.Max(.1f,definition.gravityScale);
            rollingBody.interpolation=RigidbodyInterpolation2D.Interpolate;
            rollingBody.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            rollingBody.linearVelocity=impactVelocity*.72f;
            rollingBody.angularVelocity=-impactVelocity.x*80f;
            rollingCollider=gameObject.AddComponent<CircleCollider2D>();
            rollingCollider.radius=Mathf.Max(.08f,definition.collisionRadius);
            rollingMaterial=new PhysicsMaterial2D("Lil Bomb roll")
            {
                friction=.72f,
                bounciness=.28f
            };
            rollingCollider.sharedMaterial=rollingMaterial;
            var ownerCollider=owner?owner.GetComponent<Collider2D>():null;
            if(ownerCollider) Physics2D.IgnoreCollision(rollingCollider,ownerCollider,true);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if(!rolling || resolved) return;
            var victim=collision.collider.GetComponentInParent<SummonerCombat>();
            if(victim && victim!=owner)
            {
                Vector2 point=collision.contactCount>0?collision.GetContact(0).point:(Vector2)transform.position;
                Resolve(collision.collider,point);
                return;
            }
            if(collision.collider.GetComponent<Tilemap>())
            {
                lastTerrain=collision.collider;
                for(int i=0;i<collision.contactCount;i++)
                    if(collision.GetContact(i).normal.y>.7f) groundedGrace=.12f;
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if(!rolling || resolved || !collision.collider.GetComponent<Tilemap>()) return;
            lastTerrain=collision.collider;
            for(int i=0;i<collision.contactCount;i++)
                if(collision.GetContact(i).normal.y>.7f) groundedGrace=.12f;
        }

        private void Resolve(Collider2D collider, Vector2 point)
        {
            if (resolved) return;
            resolved = true;
            var callback = onResolved;
            onResolved = null;
            gameObject.SetActive(false);
            if(rollingMaterial) Destroy(rollingMaterial);
            Destroy(gameObject);
            callback?.Invoke(collider, point);
        }
        public void Cancel()
        {
            resolved = true;
            onResolved = null;
            gameObject.SetActive(false);
            if(rollingMaterial) Destroy(rollingMaterial);
            Destroy(gameObject);
        }
    }
}
