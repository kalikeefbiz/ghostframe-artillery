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
            // Being lower than the player (or falling into a new crater) is not an escape trigger.
            // Step remains available for deliberate boundary safety once actually grounded.
            bool danger=terrain.Grounded(ai) &&
                (position.x<terrain.Left+1.5f || position.x>terrain.Right-1.5f);
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
                else if(sigil.form==SigilForm.Shift && danger)
                {
                    for(float dx=-6;dx<=6;dx+=1)
                    {
                        Vector2 request=position+new Vector2(dx,2);
                        if(terrain.Standing(request,ai,player,sigil.displacementDistance,out var destination)
                            && destination.y>position.y+.4f)
                        { plan.target=request; plan.score=45+destination.y-position.y; }
                    }
                }
                else if(sigil.form==SigilForm.Construct && ai.Health.Current<50 && ai.Health.Shield<10)
                {
                    Vector2 request=position+new Vector2(Mathf.Sign(target.x-position.x)*2.5f,-.6f);
                    if(terrain.WallPosition(request,ai,sigil.targetingRange,sigil.wallSize,out var bottom))
                    {plan.target=request;plan.score=28;}
                }
                if(plan.score>best.score) best=plan;
            }
            return best;
        }
    }
}
