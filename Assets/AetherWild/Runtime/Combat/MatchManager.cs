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
        private ResoAnchor playerReso,enemyReso;
        public SigilDefinition[] Library => Player && Player.Definition.sigilLibrary!=null
            ? Player.Definition.sigilLibrary : System.Array.Empty<SigilDefinition>();
        public void Select(int slot)
        {
            if(PlayerCanAct && CanUseSlot(Side.Player,slot)) { SelectedSlot=slot; StateChanged?.Invoke(); }
        }
        public bool EquipPlayerSlot(int slot,int libraryIndex)
        {
            if(!InMenu || slot<0 || slot>=Player.Loadout.Count || libraryIndex<0 || libraryIndex>=Library.Length) return false;
            Player.EquipSlot(slot,Library[libraryIndex]);
            SelectedSlot=Mathf.Clamp(SelectedSlot,0,Player.Loadout.Count-1);
            StateChanged?.Invoke();
            return true;
        }
        public bool IsRecallReady(Side side,int slot)
        {
            var caster=side==Side.Player?Player:Enemy;
            var anchor=side==Side.Player?playerReso:enemyReso;
            return anchor && anchor.Owner==caster && anchor.Slot==slot && Turns.TurnNumber<=anchor.ExpireTurn;
        }
        public bool CanUseSlot(Side side,int slot)
        {
            var caster=side==Side.Player?Player:Enemy;
            return caster && (caster.Loadout.Available(slot) || IsRecallReady(side,slot));
        }
        public bool HopAllowed()
        {
            if(!PlayerCanMove || MovementLeft < .8f) return false;
            MovementLeft-=.8f; return true;
        }
        public void StartMatch() { InMenu=false; Rematch(); }
        public event Action StateChanged;
        public event Action<Vector2> Impact;
        public event Action<SummonerCombat,Vector2> SigilCast;
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
            Terrain.Changed += OnTerrainChanged;
            Turns.Changed += OnTurnChanged;
            Rematch();
        }
        private void OnTurnChanged()
        {
            MirrorField.RemoveExpired(Turns.TurnNumber);
            ExpireResoAnchors();
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
                // Let current-terrain physics finish the fall before selecting any action.
                if(!Terrain.Grounded(Enemy) || Mathf.Abs(Enemy.GetComponent<Rigidbody2D>().linearVelocity.y)>.2f)
                { Enemy.Movement.ClearInput(); aiWait=1.1f; return; }
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
            if (!sigil || !CanUseSlot(side,slot)) return false;
            var opponent = side == Side.Player ? Enemy : Player;

            if(sigil.behavior==SigilBehavior.ResoRecall && IsRecallReady(side,slot))
            {
                if(!Turns.BeginCast(side)) return false;
                SigilCast?.Invoke(caster,direction);
                RecallReso(side,sigil);
                QueueResolution(); StateChanged?.Invoke(); return true;
            }

            Vector2 validTarget = target;
            Vector2 wallSize = sigil.wallSize * caster.Bonus(sigil,"terrain");
            if(sigil.behavior==SigilBehavior.Mirror)
            {
                if(direction.sqrMagnitude<.01f ||
                    !Terrain.AnchorPosition(target,caster,sigil.targetingRange,out validTarget)) return false;
            }
            else if(sigil.behavior==SigilBehavior.BulwarkRise)
            {
                if(!Terrain.WallPosition(target,caster,sigil.targetingRange,wallSize,out validTarget)) return false;
            }
            else if(sigil.form == SigilForm.Construct &&
                !Terrain.WallPosition(target,caster,sigil.targetingRange,wallSize,out validTarget)) return false;

            if(sigil.form == SigilForm.Shift && sigil.behavior!=SigilBehavior.EmberStep &&
                !Terrain.Standing(target,caster,opponent,sigil.displacementDistance * caster.Bonus(sigil,"movement"),out validTarget)) return false;

            if (!Turns.BeginCast(side)) return false;
            caster.Loadout.Spend(slot);
            SigilCast?.Invoke(caster,direction);
            if(!sigil.usesProjectile)
            {
                if(sigil.behavior==SigilBehavior.Mirror)
                    MirrorField.Create(validTarget,direction,Turns.TurnNumber,Mathf.Max(1,sigil.persistentTurns));
                else if(sigil.behavior==SigilBehavior.BulwarkRise)
                    Terrain.CreateBulwarks(validTarget,wallSize);
                else if(sigil.behavior==SigilBehavior.EmberStep)
                {
                    caster.gameObject.AddComponent<EmberStepImpact>().Initialize(caster,opponent,Terrain,sigil);
                    caster.Movement.Launch(direction.normalized*sigil.Speed(power));
                }
                else if(sigil.form == SigilForm.Ward)
                    caster.Health.GrantShield(Mathf.RoundToInt(sigil.shieldAmount*caster.Bonus(sigil,"shield")));
                else if(sigil.form == SigilForm.Shift)
                    caster.Movement.ResetPosition(validTarget);
                else if(sigil.form == SigilForm.Construct)
                    Terrain.CreateWall(validTarget,wallSize);
                QueueResolution(); StateChanged?.Invoke(); return true;
            }
            int castGeneration = generation;
            var go = new GameObject("Sigil projectile", typeof(SpriteRenderer), typeof(ProjectileController));
            var visual = go.GetComponent<SpriteRenderer>();
            visual.sprite = sigil.icon ? sigil.icon : projectileSprite;
            visual.color = Color.white;
            visual.sortingOrder = 5;
            if(sigil.icon)
            {
                float longest=Mathf.Max(.01f,Mathf.Max(sigil.icon.bounds.size.x,sigil.icon.bounds.size.y));
                go.transform.localScale=Vector3.one*(.72f/longest);
            }
            else go.transform.localScale = Vector3.one * sigil.collisionRadius * 2;
            projectile = go.GetComponent<ProjectileController>();
            projectile.Initialize(caster, sigil, direction, power, (hit, point) =>
            {
                if (castGeneration != generation || Turns.Phase != TurnPhase.Resolving) return;
                projectile = null;
                var victim = hit ? hit.GetComponentInParent<SummonerCombat>() : null;
                ResolveEffect(caster,sigil,victim,point);
                if(hit && sigil.behavior==SigilBehavior.Rootcaller)
                    Terrain.CreateRootMound(point,sigil.wallSize.x,sigil.wallSize.y);
                if(hit && sigil.behavior==SigilBehavior.ResoRecall)
                    SetResoAnchor(side,slot,point,sigil);
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
            && Mathf.Abs(Enemy.GetComponent<Rigidbody2D>().linearVelocity.y)<.2f
            && Terrain.Grounded(Player) && Terrain.Grounded(Enemy);
        private void OnTerrainChanged()
        {
            // Rebuilding a static composite must not leave an unsupported sleeping body aloft.
            // Never reset a transform here: both Summoners settle with the existing gravity.
            Player.GetComponent<Rigidbody2D>().WakeUp();
            Enemy.GetComponent<Rigidbody2D>().WakeUp();
            aiWait=1.1f;
        }
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
            MirrorField.ClearAll();
            ClearResoAnchors();
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
        private void SetResoAnchor(Side side,int slot,Vector2 point,SigilDefinition sigil)
        {
            var existing=side==Side.Player?playerReso:enemyReso;
            if(existing) Destroy(existing.gameObject);
            var caster=side==Side.Player?Player:Enemy;
            var anchor=ResoAnchor.Create(caster,slot,point,Turns.TurnNumber+Mathf.Max(2,sigil.persistentTurns*2),sigil.icon);
            if(side==Side.Player) playerReso=anchor; else enemyReso=anchor;
        }
        private void RecallReso(Side side,SigilDefinition sigil)
        {
            var anchor=side==Side.Player?playerReso:enemyReso;
            var caster=side==Side.Player?Player:Enemy;
            var opponent=side==Side.Player?Enemy:Player;
            if(!anchor) return;
            Vector2 start=anchor.Point;
            Vector2 end=caster.transform.position;
            Vector2 delta=end-start;
            float distance=delta.magnitude;
            if(distance>.01f)
            {
                var hit=Physics2D.Raycast(start+delta.normalized*.18f,delta.normalized,Mathf.Max(0,distance-.2f));
                if(hit.collider && hit.collider.GetComponent<UnityEngine.Tilemaps.Tilemap>()) end=hit.point;
                if(DistanceToSegment(opponent.transform.position,start,end)<.55f)
                {
                    opponent.Health.Damage(Mathf.Max(0,sigil.secondaryDamage));
                    opponent.Movement.ApplyKnockback((end-start).normalized*2.2f+Vector2.up*.35f);
                }
                var line=new GameObject("Reso recall path",typeof(LineRenderer)).GetComponent<LineRenderer>();
                line.positionCount=2;line.SetPositions(new[]{(Vector3)start,(Vector3)end});
                line.startWidth=line.endWidth=.11f;
                line.startColor=line.endColor=new Color(.4f,.9f,1,1);
                line.material=new Material(Shader.Find("Sprites/Default"));
                line.sortingOrder=7;
                Destroy(line.gameObject,.22f);
            }
            Destroy(anchor.gameObject);
            if(side==Side.Player) playerReso=null; else enemyReso=null;
        }
        private static float DistanceToSegment(Vector2 p,Vector2 a,Vector2 b)
        {
            Vector2 ab=b-a;
            if(ab.sqrMagnitude<.0001f) return Vector2.Distance(p,a);
            float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);
            return Vector2.Distance(p,a+ab*t);
        }
        private void ExpireResoAnchors()
        {
            if(playerReso && Turns.TurnNumber>playerReso.ExpireTurn){Destroy(playerReso.gameObject);playerReso=null;}
            if(enemyReso && Turns.TurnNumber>enemyReso.ExpireTurn){Destroy(enemyReso.gameObject);enemyReso=null;}
        }
        private void ClearResoAnchors()
        {
            if(playerReso) Destroy(playerReso.gameObject);
            if(enemyReso) Destroy(enemyReso.gameObject);
            playerReso=enemyReso=null;
        }

        private void OnDestroy()
        {
            generation++;
            if (projectile) projectile.Cancel();
            Turns.Changed -= OnTurnChanged;
            if(Terrain) Terrain.Changed -= OnTerrainChanged;
        }
    }
}
