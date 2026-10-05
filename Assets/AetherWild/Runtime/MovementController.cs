using UnityEngine;

namespace AetherWild
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class MovementController : MonoBehaviour
    {
        private Rigidbody2D body;
        private CapsuleCollider2D shape;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        private float direction;
        private bool hop;
        private float speed;
        private float hopSpeed;
        private float impulseX;
        private bool launching;
        private float launchAge;
        public bool IsLaunching => launching;

        public void Initialize(float moveSpeed, float jumpSpeed)
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<CapsuleCollider2D>();
            body.freezeRotation = true;
            body.gravityScale = 1.5f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            speed = moveSpeed;
            hopSpeed = jumpSpeed;
        }

        public void SetDirection(float value)
        {
            if(launching) return;
            direction = Mathf.Clamp(value, -1, 1);
        }
        public void RequestHop() { if(!launching) hop = true; }
        public void ClearInput() { direction = 0; hop = false; }

        private void FixedUpdate()
        {
            if (!body) return;
            if(launching)
            {
                launchAge+=Time.fixedDeltaTime;
                if(launchAge>3f) EndLaunch(false);
                return;
            }
            var velocity = body.linearVelocity;
            velocity.x = direction * speed + impulseX;
            impulseX = Mathf.MoveTowards(impulseX, 0, 12 * Time.fixedDeltaTime);
            if (hop && Grounded()) velocity.y = hopSpeed;
            hop = false;
            body.linearVelocity = velocity;
        }

        private bool Grounded()
        {
            var filter = new ContactFilter2D { useTriggers = false };
            int count = shape.Cast(Vector2.down, filter, hits, 0.08f);
            for (int i = 0; i < count; i++)
                if (hits[i].normal.y > 0.5f) return true;
            return false;
        }

        public void ResetPosition(Vector2 position)
        {
            ClearInput();
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0;
            impulseX = 0;
            launching=false;
            launchAge=0;
        }
        public void ApplyKnockback(Vector2 impulse)
        {
            impulseX += impulse.x;
            body.linearVelocity += Vector2.up * impulse.y;
        }
        public void Launch(Vector2 velocity)
        {
            ClearInput();
            impulseX=0;
            launching=true;
            launchAge=0;
            body.WakeUp();
            body.linearVelocity=velocity;
        }

        public void EndLaunch(bool stop)
        {
            if(!body) return;
            launching=false;
            launchAge=0;
            if(stop) body.linearVelocity=Vector2.zero;
        }
    }
}
