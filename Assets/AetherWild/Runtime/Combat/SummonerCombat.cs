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
        public void EquipSlot(int slot,SigilDefinition sigil) => Loadout.Equip(slot,sigil);
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
        public int IndexOf(SigilDefinition sigil)
        {
            for(int i=0;i<equipped.Length;i++) if(equipped[i]==sigil) return i;
            return -1;
        }
        public void Equip(int slot,SigilDefinition sigil)
        {
            if(slot<0 || slot>=equipped.Length || !sigil) return;
            int existing=IndexOf(sigil);
            if(existing>=0 && existing!=slot)
            {
                var swap=equipped[slot];
                equipped[slot]=sigil;
                equipped[existing]=swap;
                remaining[slot]=sigil.maxUses;
                remaining[existing]=swap?swap.maxUses:0;
                return;
            }
            equipped[slot]=sigil;
            remaining[slot]=sigil.maxUses;
        }
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
