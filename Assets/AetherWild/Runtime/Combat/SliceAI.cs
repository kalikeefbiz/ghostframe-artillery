using UnityEngine;

namespace AetherWild
{
    public static class SliceAI
    {
        public struct Plan { public int slot; public Vector2 direction,target; public float power,score; }
        public static Plan Choose(MatchManager match)
        {
            var ai=match.Enemy; var player=match.Player; var terrain=match.Terrain;
            Vector2 position=ai.transform.position, target=player.transform.position;
            var best=new Plan{slot=-1,score=-100,direction=Vector2.up,power=.85f};
            bool grounded=terrain.Grounded(ai);
            bool danger=grounded && (position.x<terrain.Left+1.5f || position.x>terrain.Right-1.5f);
            bool repositionUseful=grounded &&
                (danger || Mathf.Abs(target.x-position.x)>9f || target.y>position.y+1.25f);
            for(int slot=0;slot<ai.Loadout.Count;slot++)
            {
                if(!ai.Loadout.Available(slot)) continue;
                var sigil=ai.Loadout.Get(slot);
                var plan=new Plan{slot=slot,direction=Vector2.up,power=.85f,target=target,score=-100};
                if(sigil.usesProjectile)
                {
                    Vector2 delta=target-ai.LaunchOrigin;
                    float speed=sigil.Speed(plan.power);
                    if(!Ballistics.TryHighArc(delta,speed,-sigil.Gravity.y,out plan.direction))
                    { plan.power=1; Ballistics.TryHighArc(delta,sigil.Speed(1),-sigil.Gravity.y,out plan.direction); }
                    plan.score=sigil.baseDamage*.65f;
                    if(sigil.splashRadius>0) plan.score+=5;
                    // Fault is valuable when a supported enemy occupies higher ground or cover blocks a direct line.
                    var obstruction=Physics2D.Raycast(ai.LaunchOrigin+plan.direction*.8f,delta.normalized,delta.magnitude-.8f);
                    if(sigil.form==SigilForm.TerrainManipulation)
                        plan.score=terrain.Supported(target) && (target.y>position.y+.7f ||
                            (obstruction.collider && obstruction.collider.GetComponent<UnityEngine.Tilemaps.Tilemap>()))?23:4;
                    if(!sigil.unlimitedUses && ai.Loadout.Uses(slot)==1) plan.score-=4;
                }
                else if(sigil.form==SigilForm.Ward)
                    plan.score=ai.Health.Shield<5 && ai.Health.Current<=65?35: -100;
                else if(sigil.form==SigilForm.Shift && repositionUseful)
                {
                    float bestUtility=float.NegativeInfinity;
                    for(float dx=-6;dx<=6;dx+=1)
                    {
                        if(Mathf.Abs(dx)<1f) continue;
                        Vector2 request=position+new Vector2(dx,0);
                        if(!terrain.Standing(request,ai,player,sigil.displacementDistance,out var destination)) continue;

                        float towardPlayer=Mathf.Abs(target.x-position.x)-Mathf.Abs(target.x-destination.x);
                        float heightGain=destination.y-position.y;
                        float edgeSafety=Mathf.Min(destination.x-terrain.Left,terrain.Right-destination.x);
                        float utility=towardPlayer*.8f+heightGain*1.8f+Mathf.Min(edgeSafety,3f)*.15f;
                        if(danger) utility+=8f;
                        if(utility>bestUtility)
                        {
                            bestUtility=utility;
                            plan.target=request;
                            plan.score=18f+utility;
                        }
                    }
                }
                else if(sigil.form==SigilForm.Construct && ai.Health.Current<=70 && ai.Health.Shield<15)
                {
                    float sign=Mathf.Sign(target.x-position.x);
                    foreach(float dx in new[]{sign*2.5f,sign*1.75f,-sign*1.75f})
                    {
                        Vector2 request=position+new Vector2(dx,0);
                        if(terrain.WallPosition(request,ai,sigil.targetingRange,sigil.wallSize,out _))
                        {
                            plan.target=request;
                            plan.score=ai.Health.Current<=45?31:24;
                            break;
                        }
                    }
                }
                if(plan.score>best.score) best=plan;
            }
            return best;
        }
    }
}
