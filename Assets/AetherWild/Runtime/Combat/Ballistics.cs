using UnityEngine;

namespace AetherWild
{
    public static class Ballistics
    {
        public static Vector2 Position(Vector2 origin, Vector2 velocity, Vector2 gravity, float time)
            => origin + velocity * time + gravity * (0.5f * time * time);

        // High arc for basic artillery aiming; no terrain search or tactical scoring.
        public static bool TryHighArc(Vector2 delta, float speed, float gravity, out Vector2 direction)
        {
            float x = Mathf.Abs(delta.x);
            if (x < 0.01f) { direction = Vector2.up; return true; }
            float speed2 = speed * speed;
            float discriminant = speed2 * speed2 - gravity * (gravity * x * x + 2 * delta.y * speed2);
            if (discriminant < 0 || gravity <= 0) { direction = Vector2.up; return false; }
            float angle = Mathf.Atan((speed2 + Mathf.Sqrt(discriminant)) / (gravity * x));
            direction = new Vector2(Mathf.Cos(angle) * Mathf.Sign(delta.x), Mathf.Sin(angle));
            return true;
        }
    }
}
