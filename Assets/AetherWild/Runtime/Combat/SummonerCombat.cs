using UnityEngine;

namespace AetherWild
{
    // Both sides use this component and the same Mae definition. No class restrictions.
    public sealed class SummonerCombat : MonoBehaviour
    {
        public Side Side { get; private set; }
        public SummonerDefinition Definition { get; private set; }
        public HealthState Health { get; private set; }
        public MovementController Movement { get; private set; }
        public SigilLoadout Loadout { get; private set; }
        public Vector2 LaunchOrigin => (Vector2)transform.position + Vector2.up * 0.45f;
        public bool Resonant => Loadout.Matches(Definition.summonerClass) * 4 >= Loadout.Count * 3 && Loadout.Count > 0;
        public float Bonus(SigilDefinition sigil, string stat)
            => Resonant && sigil.resonanceEligible && sigil.classAffinity == Definition.summonerClass
                && sigil.resonanceModifiedStat == stat ? 1 + sigil.resonanceBonusPercent / 100f : 1;
        public void Initialize(Side side, SummonerDefinition definition)
        {
            Side = side;
            Definition = definition;
            Health = new HealthState(definition.startingHP);
            Movement = GetComponent<MovementController>();
            Loadout = new SigilLoadout(definition.startingLoadout);
        }
        public void ResetMatch(Vector2 position)
        {
            Health.Reset();
            Loadout.Reset();
            Movement.ResetPosition(position);
        }
    }

    public sealed class SigilLoadout
    {
        private readonly SigilDefinition[] equipped;
        private readonly int[] remaining;
        public int Count => equipped.Length;
        public int Uses(int slot) => Get(slot) ? remaining[slot] : 0;
        public int Matches(SummonerClass kind)
        {
            int count = 0;
            foreach (var sigil in equipped) if (sigil && sigil.classAffinity == kind) count++;
            return count;
        }
        public SigilLoadout(SigilDefinition[] definitions)
        {
            equipped = (SigilDefinition[])definitions.Clone();
            remaining = new int[equipped.Length];
            Reset();
        }
        public SigilDefinition Get(int slot) => slot >= 0 && slot < equipped.Length ? equipped[slot] : null;
        public bool Available(int slot) => Get(slot) && (Get(slot).unlimitedUses || remaining[slot] > 0);
        public bool Spend(int slot)
        {
            if (!Available(slot)) return false;
            if (!equipped[slot].unlimitedUses) remaining[slot]--;
            return true;
        }
        public void Reset()
        {
            for (int i = 0; i < equipped.Length; i++) remaining[i] = equipped[i] ? equipped[i].maxUses : 0;
        }
    }
}
