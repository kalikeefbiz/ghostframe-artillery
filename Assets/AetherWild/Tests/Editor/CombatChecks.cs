using System;
using UnityEditor;
using UnityEngine;

namespace AetherWild.Editor
{
    // Dependency-free editor assertions, run automatically before Cloud export/build.
    // These complement, and do not replace, the physical-device acceptance checklist.
    public static class CombatChecks
    {
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("M1 check failed: " + message);
        }
        [MenuItem("AetherWild/Run M1 checks")]
        public static void Run()
        {
            var health = new HealthState(100);
            for (int i = 0; i < 4; i++) health.Damage(20);
            Check(health.Current == 20 && !health.Defeated, "four hits leave 20 HP");
            health.Damage(20);
            Check(health.Defeated, "fifth hit defeats");
            health.Damage(20);
            Check(health.Current == 0, "HP lower bound");
            health.Reset(); health.Damage(-20);
            Check(health.Current == 100, "negative damage cannot heal");
            var turns = new TurnManager();
            turns.Reset();
            Check(turns.CanAct(Side.Player) && !turns.CanAct(Side.Enemy), "player first");
            Check(!turns.BeginCast(Side.Enemy), "inactive side rejected");
            Check(turns.BeginCast(Side.Player), "valid first cast");
            Check(!turns.BeginCast(Side.Player), "duplicate cast rejected");
            turns.Tick(100);
            Check(turns.Phase == TurnPhase.Resolving, "timer cannot steal projectile resolution");
            turns.ResolveProjectile(); turns.ResolveProjectile();
            Check(turns.CanAct(Side.Enemy) && turns.TurnNumber == 2, "single handoff");
            turns.Tick(30);
            Check(turns.CanAct(Side.Player) && turns.TurnNumber == 3, "timeout handoff");
            turns.Finish(); turns.Tick(100); turns.ResolveProjectile();
            Check(!turns.BeginCast(Side.Player) && turns.Phase == TurnPhase.Finished, "finished state is terminal");
            turns.Reset();
            for (int i = 0; i < 100; i++)
            {
                Check(turns.BeginCast(turns.ActiveSide), "repeat cast");
                turns.ResolveProjectile();
            }
            Check(turns.TurnNumber == 101 && turns.CanAct(Side.Player), "100 alternating turns");
            turns.Reset();
            Check(turns.SecondsRemaining == 30 && turns.TurnNumber == 1, "rematch resets timer and turn");
            var bolt = AssetDatabase.LoadAssetAtPath<SigilDefinition>("Assets/AetherWild/Data/AetherBolt.asset");
            var mae = AssetDatabase.LoadAssetAtPath<SummonerDefinition>("Assets/AetherWild/Data/Mae.asset");
            Check(bolt && mae, "data assets import");
            Check(mae.summonerClass == SummonerClass.Conduit && mae.startingHP == 100, "Mae identity");
            Check(mae.startingLoadout.Length == 1 && mae.startingLoadout[0] == bolt, "single universal Sigil");
            Check(bolt.baseDamage == 20 && bolt.unlimitedUses && bolt.classAffinity == SummonerClass.Expellant,
                "baseline Bolt values");
            Check(bolt.school == SigilSchool.Origin && bolt.form == SigilForm.Projectile && bolt.usesProjectile,
                "Bolt school and form");
            Check(!bolt.destroysTerrain && !bolt.createsTerrain && bolt.shieldAmount == 0, "M1 scope");
            var loadout = new SigilLoadout(mae.startingLoadout);
            for (int i = 0; i < 100; i++) Check(loadout.Spend(0), "unlimited uses and off-class allowed");
            var finite = ScriptableObject.CreateInstance<SigilDefinition>();
            try
            {
                finite.maxUses = 2;
                var charges = new SigilLoadout(new[] { finite });
                Check(charges.Spend(0) && charges.Spend(0) && !charges.Spend(0), "finite charges exhaust");
                charges.Reset();
                Check(charges.Available(0), "rematch restores charges");
            }
            finally { UnityEngine.Object.DestroyImmediate(finite); }
            foreach (float x in new[] { -24f, -12f, 12f, 24f })
            {
                var delta = new Vector2(x, -0.45f);
                float speed = bolt.Speed(0.82f);
                Check(Ballistics.TryHighArc(delta, speed, -bolt.Gravity.y, out Vector2 direction), "AI reachable target");
                float time = delta.x / (direction.x * speed);
                Vector2 hit = Ballistics.Position(Vector2.zero, direction * speed, bolt.Gravity, time);
                Check(Vector2.Distance(hit, delta) < 0.005f, "AI analytic solution");
            }
            Debug.Log("AetherWild M1 core/data checks passed. Physics and iPhone checks still required.");
        }
    }
}
