using System;
using UnityEngine;

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
                Vector2 delta = to - from;
                int count = Physics2D.CircleCast(from, definition.collisionRadius, delta.normalized,
                    filter, hits, delta.magnitude);
                RaycastHit2D nearest = default;
                float distance = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                    if (Valid(hits[i].collider) && hits[i].distance < distance)
                    { nearest = hits[i]; distance = hits[i].distance; }
                if (nearest.collider) { Resolve(nearest.collider, nearest.point); return; }
                age = nextAge;
                transform.position = to;
                if (to.y < minimumY || Mathf.Abs(to.x) > 80 || age >= MaxLifetime)
                { Resolve(null, to); return; }
            }
        }

        private void Resolve(Collider2D collider, Vector2 point)
        {
            if (resolved) return;
            resolved = true;
            var callback = onResolved;
            onResolved = null;
            gameObject.SetActive(false);
            Destroy(gameObject);
            callback?.Invoke(collider, point);
        }
        public void Cancel()
        {
            resolved = true;
            onResolved = null;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
