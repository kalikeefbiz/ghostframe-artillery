using UnityEngine;

namespace AetherWild
{
    [CreateAssetMenu(menuName = "AetherWild/Sigil")]
    public sealed class SigilDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public SigilSchool school;
        public SigilForm form;
        public SummonerClass classAffinity;
        public SigilBehavior behavior;
        [Header("Match resources")]
        public int maxUses;
        public bool unlimitedUses;
        [Header("Projectile")]
        public bool usesProjectile;
        public ProjectileMotion projectileMotion = ProjectileMotion.Impact;
        public float projectileSpeed = 20;
        public float gravityScale = 1;
        public float launchPowerMin = 0.25f;
        public float launchPowerMax = 1;
        public float collisionRadius = 0.16f;
        [Header("Damage")]
        public int baseDamage;
        public int secondaryDamage;
        public float splashRadius;
        public bool damageFalloff;
        public float knockbackForce;
        [Header("Terrain (metadata only in M1)")]
        public float terrainDamageRadius;
        public bool createsTerrain;
        public bool destroysTerrain;
        [Header("Defense / movement (metadata only in M1)")]
        public int shieldAmount;
        public float displacementDistance;
        public int persistentTurns;
        public float targetingRange = 7;
        public Vector2 wallSize = new Vector2(1.5f, 2.5f);
        [Header("Resonance (metadata only; threshold is 75% of equipped loadout)")]
        public bool resonanceEligible = true;
        public string resonanceModifiedStat;
        public float resonanceBonusPercent;
        [Header("Optional presentation")]
        public Sprite icon;
        public GameObject projectileVFX;
        public GameObject impactVFX;
        public AudioClip sfx;
        public Vector2 Gravity => Vector2.down * (9.81f * gravityScale);
        public float Speed(float power) => projectileSpeed * Mathf.Clamp(power, launchPowerMin, launchPowerMax);
    }
}
