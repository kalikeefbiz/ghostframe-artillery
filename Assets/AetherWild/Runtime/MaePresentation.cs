using UnityEngine;

namespace AetherWild
{
    // Read-only view of the existing match. This component never drives physics or combat.
    public sealed class MaePresentation : MonoBehaviour
    {
        private SummonerCombat summoner;
        private MatchManager match;
        private ProductionArt art;
        private SpriteRenderer visual;
        private Rigidbody2D body;
        private int previousHP,previousShield;
        private float castTime,hitTime,walkTime;

        public void Initialize(SummonerCombat actor,MatchManager session,ProductionArt assets,SpriteRenderer renderer)
        {
            summoner=actor;match=session;art=assets;visual=renderer;
            body=actor.GetComponent<Rigidbody2D>();
            visual.transform.localScale=Vector3.one;
            visual.transform.localPosition=Vector3.down*.6f; // Existing capsule foot, not a new collision shape.
            visual.color=Color.white;
            match.SigilCast += OnCast;
            match.StateChanged += OnMatchState;
            ResetView();
        }
        private void ResetView()
        {
            castTime=hitTime=walkTime=0;
            previousHP=summoner.Health.Current;previousShield=summoner.Health.Shield;
            visual.sprite=art.maeIdle;visual.flipX=summoner.Side==Side.Enemy;
            visual.enabled=!match.InMenu;
        }
        private void OnMatchState()
        {
            if(match.Turns.TurnNumber==1 && match.Turns.Phase==TurnPhase.Acting) ResetView();
        }
        private void OnCast(SummonerCombat actor,Vector2 direction)
        {
            if(actor!=summoner) return;
            castTime=.4f;
            if(Mathf.Abs(direction.x)>.01f) visual.flipX=direction.x<0;
        }
        private void LateUpdate()
        {
            if(!match) return;
            visual.enabled=!match.InMenu;
            if(match.InMenu) return;
            if(summoner.Health.Current<previousHP || summoner.Health.Shield<previousShield) hitTime=.22f;
            previousHP=summoner.Health.Current;previousShield=summoner.Health.Shield;
            if(!match.Suspended) {castTime=Mathf.Max(0,castTime-Time.deltaTime);hitTime=Mathf.Max(0,hitTime-Time.deltaTime);}
            if(summoner.Health.Defeated) {visual.sprite=art.maeDefeat;return;}
            if(hitTime>0) {visual.sprite=art.maeHit;return;}
            if(castTime>0) {visual.sprite=art.maeCast;return;}
            if(Mathf.Abs(body.linearVelocity.x)>.1f && Mathf.Abs(body.linearVelocity.y)<.3f)
            {
                if(!match.Suspended) walkTime+=Time.deltaTime;
                visual.flipX=body.linearVelocity.x<0;
                // Exactly the supplied A / B / A loop, no interpolated or generated frames.
                visual.sprite=(Mathf.FloorToInt(walkTime/.16f)%3)==1?art.maeWalkB:art.maeWalkA;
            }
            else {walkTime=0;visual.sprite=art.maeIdle;}
        }
        private void OnDestroy()
        {
            if(match) {match.SigilCast-=OnCast;match.StateChanged-=OnMatchState;}
        }
    }
}
