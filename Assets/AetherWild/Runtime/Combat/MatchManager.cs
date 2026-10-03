using System;
using UnityEngine;

namespace AetherWild
{
    public sealed class MatchManager : MonoBehaviour
    {
        public TurnManager Turns { get; } = new TurnManager();
        public SummonerCombat Player { get; private set; }
        public SummonerCombat Enemy { get; private set; }
        public string Result { get; private set; } = "";
        public bool Suspended { get; set; }
        public bool PlayerCanAct => !Suspended && Turns.CanAct(Side.Player);
        public event Action StateChanged;
        public event Action<Vector2> Impact;
        private BattlefieldDefinition battlefield;
        private ProjectileController projectile;
        private Sprite projectileSprite;
        private int generation;
        private float aiWait;
        private readonly System.Random aimError = new System.Random(1729);

        public void Initialize(SummonerCombat player, SummonerCombat enemy, BattlefieldDefinition map, Sprite sprite)
        {
            Player = player;
            Enemy = enemy;
            battlefield = map;
            projectileSprite = sprite;
            Turns.Changed += OnTurnChanged;
            Rematch();
        }
        private void OnTurnChanged()
        {
            Player.Movement.ClearInput();
            Enemy.Movement.ClearInput();
            aiWait = 1.1f;
            StateChanged?.Invoke();
        }
        private void Update()
        {
            if (!Player || Suspended || Turns.Phase == TurnPhase.Finished) return;
            Turns.Tick(Time.deltaTime);
            if (Turns.CanAct(Side.Enemy))
            {
                aiWait -= Time.deltaTime;
                if (aiWait <= 0) FireAI();
            }
        }
        public bool Cast(Side side, Vector2 direction, float power)
        {
            if (Suspended || projectile || !Turns.CanAct(side) || direction.sqrMagnitude < 0.01f
                || float.IsNaN(power) || float.IsInfinity(power)
                || float.IsNaN(direction.x) || float.IsNaN(direction.y)
                || float.IsInfinity(direction.x) || float.IsInfinity(direction.y)) return false;
            var caster = side == Side.Player ? Player : Enemy;
            var sigil = caster.Loadout.Get(0);
            if (!sigil || !sigil.usesProjectile || !caster.Loadout.Available(0)) return false;
            if (!Turns.BeginCast(side)) return false;
            caster.Loadout.Spend(0);
            int castGeneration = generation;
            var go = new GameObject("Sigil projectile", typeof(SpriteRenderer), typeof(ProjectileController));
            var visual = go.GetComponent<SpriteRenderer>();
            visual.sprite = projectileSprite;
            visual.color = new Color(0.68f, 0.89f, 1);
            visual.sortingOrder = 5;
            go.transform.localScale = Vector3.one * sigil.collisionRadius * 2;
            projectile = go.GetComponent<ProjectileController>();
            projectile.Initialize(caster, sigil, direction, power, (hit, point) =>
            {
                if (castGeneration != generation || Turns.Phase != TurnPhase.Resolving) return;
                projectile = null;
                var victim = hit ? hit.GetComponentInParent<SummonerCombat>() : null;
                if (victim && victim.Side != caster.Side) victim.Health.Damage(sigil.baseDamage);
                Impact?.Invoke(point);
                if (Player.Health.Defeated || Enemy.Health.Defeated)
                {
                    Result = Player.Health.Defeated ? "DEFEAT" : "VICTORY";
                    Turns.Finish();
                }
                else Turns.ResolveProjectile();
                StateChanged?.Invoke();
            });
            return true;
        }
        private void FireAI()
        {
            var sigil = Enemy.Loadout.Get(0);
            if (!sigil) return;
            Vector2 delta = (Vector2)Player.transform.position - Enemy.LaunchOrigin;
            float power = Mathf.Clamp(0.82f, sigil.launchPowerMin, sigil.launchPowerMax);
            if (!Ballistics.TryHighArc(delta, sigil.Speed(power), -sigil.Gravity.y, out Vector2 direction))
            {
                power = sigil.launchPowerMax;
                if (!Ballistics.TryHighArc(delta, sigil.Speed(power), -sigil.Gravity.y, out direction))
                    direction = new Vector2(Mathf.Sign(delta.x), 1).normalized;
            }
            // Small repeatable random errors; neither health nor hit decisions are randomized.
            float angleError = (float)(aimError.NextDouble() * 1.2 - 0.6);
            float powerError = (float)(aimError.NextDouble() * 0.01 - 0.005);
            direction = Quaternion.Euler(0, 0, angleError) * direction;
            Cast(Side.Enemy, direction, power + powerError);
        }
        public void Rematch()
        {
            generation++;
            if (projectile) projectile.Cancel();
            projectile = null;
            Result = "";
            Player.ResetMatch(battlefield.playerSpawn);
            Enemy.ResetMatch(battlefield.enemySpawn);
            Physics2D.SyncTransforms();
            Turns.Reset();
        }
        private void OnDestroy()
        {
            generation++;
            if (projectile) projectile.Cancel();
            Turns.Changed -= OnTurnChanged;
        }
    }
}
