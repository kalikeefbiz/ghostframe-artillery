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
        public bool InMenu { get; private set; } = true;
        public bool PlayerCanAct => !InMenu && !Suspended && Turns.CanAct(Side.Player);
        public int SelectedSlot { get; private set; }
        public TerrainSystem Terrain { get; private set; }
        public float MovementLeft { get; private set; }
        public bool PlayerCanMove => PlayerCanAct && MovementLeft > 0;
        private float previousX;
        private float settleTime;
        private bool pendingResolution;
        private System.Random aiRandom = new System.Random(1729);
        public void Select(int slot)
        {
            if(PlayerCanAct && Player.Loadout.Available(slot)) { SelectedSlot=slot; StateChanged?.Invoke(); }
        }
        public bool HopAllowed()
        {
            if(!PlayerCanMove || MovementLeft < .8f) return false;
            MovementLeft-=.8f; return true;
        }
        public void StartMatch() { InMenu=false; Rematch(); }
        public event Action StateChanged;
        public event Action<Vector2> Impact;
        private BattlefieldDefinition battlefield;
        private ProjectileController projectile;
        private Sprite projectileSprite;
        private int generation;
        private float aiWait;

        public void Initialize(SummonerCombat player, SummonerCombat enemy, BattlefieldDefinition map, Sprite sprite)
        {
            Player = player;
            Enemy = enemy;
            battlefield = map;
            projectileSprite = sprite;
            Terrain = FindFirstObjectByType<TerrainSystem>();
            Turns.Changed += OnTurnChanged;
            Rematch();
        }
        private void OnTurnChanged()
        {
            Player.Movement.ClearInput();
            Enemy.Movement.ClearInput();
            aiWait = 1.1f;
            MovementLeft = battlefield.movementPerTurn;
            previousX = (Turns.ActiveSide == Side.Player ? Player : Enemy).transform.position.x;
            StateChanged?.Invoke();
        }
        private void Update()
        {
            if (!Player || InMenu || Suspended || Turns.Phase == TurnPhase.Finished) return;
            CheckFall(Player); CheckFall(Enemy);
            if(Turns.Phase == TurnPhase.Finished) return;
            if(pendingResolution)
            {
                settleTime-=Time.deltaTime;
                if(settleTime<=0 && (BothSettled() || settleTime < -2))
                { pendingResolution=false; Turns.ResolveProjectile(); }
                return;
            }
            if(Turns.Phase == TurnPhase.Acting)
            {
                var active = Turns.ActiveSide == Side.Player ? Player : Enemy;
                MovementLeft = Mathf.Max(0, MovementLeft - Mathf.Abs(active.transform.position.x-previousX));
                previousX=active.transform.position.x;
                if(MovementLeft<=0) active.Movement.ClearInput();
            }
            Turns.Tick(Time.deltaTime);
            if (Turns.CanAct(Side.Enemy))
            {
                float horizontal=Player.transform.position.x-Enemy.transform.position.x;
                bool canMove=false;
                if(aiWait>.2f && MovementLeft>0 && Mathf.Abs(horizontal)>28)
                {
                    var foot=Physics2D.Raycast((Vector2)Enemy.transform.position+
                        new Vector2(Mathf.Sign(horizontal)*.6f,.5f),Vector2.down,2);
                    canMove=foot.collider && foot.collider.GetComponent<UnityEngine.Tilemaps.Tilemap>()
                        && foot.normal.y>.65f && Mathf.Abs(foot.point.y-(Enemy.transform.position.y-.6f))<.45f;
                }
                Enemy.Movement.SetDirection(canMove?Mathf.Sign(horizontal):0);
                aiWait -= Time.deltaTime;
                if (aiWait <= 0) FireAI();
            }
        }
        public bool Cast(Side side, Vector2 direction, float power)
            => CastSigil(side, side == Side.Player ? SelectedSlot : 0, direction, power, Vector2.zero);

        public bool CastSigil(Side side, int slot, Vector2 direction, float power, Vector2 target)
        {
            if (InMenu || Suspended || projectile || !Turns.CanAct(side) || direction.sqrMagnitude < 0.01f
                || float.IsNaN(power) || float.IsInfinity(power)
                || float.IsNaN(direction.x) || float.IsNaN(direction.y)
                || float.IsInfinity(direction.x) || float.IsInfinity(direction.y)
                || float.IsNaN(target.x) || float.IsNaN(target.y)
                || float.IsInfinity(target.x) || float.IsInfinity(target.y)) return false;
            var caster = side == Side.Player ? Player : Enemy;
            var sigil = caster.Loadout.Get(slot);
            if (!sigil || !caster.Loadout.Available(slot)) return false;
            var opponent = side == Side.Player ? Enemy : Player;
            Vector2 validTarget = target;
            Vector2 wallSize = sigil.wallSize * caster.Bonus(sigil,"terrain");
            if(sigil.form == SigilForm.Construct &&
                !Terrain.WallPosition(target,caster,sigil.targetingRange,wallSize,out validTarget)) return false;
            if(sigil.form == SigilForm.Shift &&
                !Terrain.Standing(target,caster,opponent,sigil.displacementDistance * caster.Bonus(sigil,"movement"),out validTarget)) return false;
            if (!Turns.BeginCast(side)) return false;
            caster.Loadout.Spend(slot);
            if(!sigil.usesProjectile)
            {
                if(sigil.form == SigilForm.Ward) caster.Health.GrantShield(Mathf.RoundToInt(sigil.shieldAmount*caster.Bonus(sigil,"shield")));
                else if(sigil.form == SigilForm.Shift) caster.Movement.ResetPosition(validTarget);
                else if(sigil.form == SigilForm.Construct) Terrain.CreateWall(validTarget,wallSize);
                QueueResolution(); StateChanged?.Invoke(); return true;
            }
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
                ResolveEffect(caster,sigil,victim,point);
                Impact?.Invoke(point);
                if (Player.Health.Defeated || Enemy.Health.Defeated)
                {
                    Result = Player.Health.Defeated ? "DEFEAT" : "VICTORY";
                    Turns.Finish();
                }
                else QueueResolution();
                StateChanged?.Invoke();
            });
            return true;
        }
        private void QueueResolution() { pendingResolution=true; settleTime=.45f; }
        private bool BothSettled() => Mathf.Abs(Player.GetComponent<Rigidbody2D>().linearVelocity.y)<.2f
            && Mathf.Abs(Enemy.GetComponent<Rigidbody2D>().linearVelocity.y)<.2f;
        private void CheckFall(SummonerCombat summoner)
        {
            var p=summoner.transform.position;
            if(p.y< battlefield.killY || p.x<Terrain.Left-.7f || p.x>Terrain.Right+.7f)
            {
                summoner.Health.Damage(summoner.Health.Current+summoner.Health.Shield);
                Result=summoner.Side==Side.Player?"DEFEAT":"VICTORY";
                if(projectile) projectile.Cancel();
                projectile=null; pendingResolution=false;
                Turns.Finish();
            }
        }
        private void ResolveEffect(SummonerCombat caster,SigilDefinition sigil,SummonerCombat direct,Vector2 point)
        {
            foreach(var victim in new[]{Player,Enemy})
            {
                float distance=Vector2.Distance(victim.transform.position,point);
                int damage=0;
                if(sigil.splashRadius>0 && distance<sigil.splashRadius)
                    damage=Mathf.RoundToInt(sigil.baseDamage*(sigil.damageFalloff?1-distance/sigil.splashRadius:1));
                if(victim==direct) damage=sigil.baseDamage;
                damage=Mathf.RoundToInt(damage*caster.Bonus(sigil,"damage"));
                if(damage>0)
                {
                    victim.Health.Damage(damage);
                    if(sigil.knockbackForce>0)
                        victim.Movement.ApplyKnockback(new Vector2(Mathf.Sign(victim.transform.position.x-point.x),.6f)*sigil.knockbackForce);
                }
            }
            if(sigil.destroysTerrain) Terrain.DestroyCircle(point,sigil.terrainDamageRadius*caster.Bonus(sigil,"terrain"));
        }
        private void FireAI()
        {
            var plan=SliceAI.Choose(this);
            if(plan.slot<0) { aiWait=.5f; return; }
            Vector2 direction=plan.direction;
            if(Enemy.Loadout.Get(plan.slot).usesProjectile)
            {
                direction=Quaternion.Euler(0,0,(float)(aiRandom.NextDouble()*1.2-.6))*direction;
                plan.power+=(float)(aiRandom.NextDouble()*.01-.005);
            }
            if(!CastSigil(Side.Enemy,plan.slot,direction,plan.power,plan.target))
            { aiWait=.4f; }
        }
        public void Rematch()
        {
            if(Terrain) Terrain.ResetTerrain();
            pendingResolution=false;
            aiRandom=new System.Random(1729);
            SelectedSlot=0;
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
