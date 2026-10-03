using System;

namespace AetherWild
{
    public enum Side { Player, Enemy }
    public enum TurnPhase { Acting, Resolving, Finished }
    public enum SummonerClass { Embodiment, Conduit, Shaper, Manipulator, Expellant, Specialist }
    public enum SigilSchool { Origin, Aerth, Ash, Aurora, Tempest, Lunar, XO, Seeker }
    public enum SigilForm { Projectile, Bomb, Construct, Shift, Ward }

    public sealed class HealthState
    {
        public int Maximum { get; }
        public int Current { get; private set; }
        public bool Defeated => Current == 0;
        public HealthState(int maximum) { Maximum = Math.Max(1, maximum); Reset(); }
        public void Reset() => Current = Maximum;
        public void Damage(int amount) => Current = Math.Max(0, Current - Math.Max(0, amount));
    }

    // Pure turn rules: callers cannot double-cast or advance a finished match.
    public sealed class TurnManager
    {
        public Side ActiveSide { get; private set; }
        public TurnPhase Phase { get; private set; }
        public float SecondsRemaining { get; private set; }
        public int TurnNumber { get; private set; }
        public event Action Changed;
        public const float Duration = 30;
        public void Reset()
        {
            ActiveSide = Side.Player;
            Phase = TurnPhase.Acting;
            SecondsRemaining = Duration;
            TurnNumber = 1;
            Changed?.Invoke();
        }
        public bool CanAct(Side side) => Phase == TurnPhase.Acting && ActiveSide == side;
        public bool BeginCast(Side side)
        {
            if (!CanAct(side)) return false;
            Phase = TurnPhase.Resolving;
            Changed?.Invoke();
            return true;
        }
        public void Tick(float seconds)
        {
            if (Phase != TurnPhase.Acting || seconds <= 0) return;
            SecondsRemaining = Math.Max(0, SecondsRemaining - seconds);
            if (SecondsRemaining == 0) NextTurn();
        }
        public void ResolveProjectile()
        {
            if (Phase == TurnPhase.Resolving) NextTurn();
        }
        private void NextTurn()
        {
            ActiveSide = ActiveSide == Side.Player ? Side.Enemy : Side.Player;
            Phase = TurnPhase.Acting;
            SecondsRemaining = Duration;
            TurnNumber++;
            Changed?.Invoke();
        }
        public void Finish()
        {
            Phase = TurnPhase.Finished;
            Changed?.Invoke();
        }
    }
}
