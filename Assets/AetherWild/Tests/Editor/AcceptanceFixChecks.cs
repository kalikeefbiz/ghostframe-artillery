using System;
using UnityEditor;
using UnityEngine;

namespace AetherWild.Editor
{
    // Explicit opt-in Play Mode smoke test. Not run during a build or an ordinary match.
    public static class AcceptanceFixChecks
    {
        [MenuItem("AetherWild/Tests/M2.1 placement and terrain smoke test (Play Mode)")]
        public static void Run()
        {
            if(!Application.isPlaying) throw new InvalidOperationException("Open Foundation and enter Play Mode first.");
            var match=UnityEngine.Object.FindFirstObjectByType<MatchManager>();
            if(!match) throw new InvalidOperationException("Foundation match not found.");
            if(!EditorUtility.DisplayDialog("M2.1 smoke test", "This starts and resets the current test match.","Run","Cancel")) return;
            match.StartMatch();
            try
            {
                var terrain=match.Terrain;
                var player=match.Player;
                var sigil=player.Loadout.Get(3);
                Vector2 target=new Vector2(-16.75f,1);
                Require(terrain.WallPosition(target,player,sigil.targetingRange,sigil.wallSize,out var bottom),
                    "Wall must accept a free part of the left spawn shelf.");
                Require(!terrain.WallPosition(player.transform.position,player,sigil.targetingRange,sigil.wallSize,out _),
                    "Wall must reject overlap with its caster.");
                Require(!terrain.WallPosition(match.Enemy.transform.position,player,100,sigil.wallSize,out _),
                    "Wall must reject overlap with the opponent.");
                Require(!terrain.WallPosition(new Vector2(100,1),player,100,sigil.wallSize,out _),"Bounds rejected.");
                Require(!terrain.WallPosition(target,player,.1f,sigil.wallSize,out _),"Range rejected.");
                Vector2 before=match.Enemy.GetComponent<Rigidbody2D>().position;
                terrain.CreateWall(bottom,sigil.wallSize);
                var hit=Physics2D.Raycast(bottom+Vector2.up*(sigil.wallSize.y+1),Vector2.down,2);
                Require(hit.collider && hit.point.y>bottom.y+sigil.wallSize.y-.25f,"Wall top must have solid collision.");
                var sideHit=Physics2D.CircleCast(bottom+new Vector2(-2,1),.12f,Vector2.right,4);
                Require(sideHit.collider,"Wall must block projectile-sized sweeps.");
                Require(terrain.DestroyCircle(bottom+Vector2.up*1.3f,2)>0,"Wall must be destructible.");
                Require(match.Enemy.GetComponent<Rigidbody2D>().position==before,"Terrain refresh must not teleport AI.");
                Vector2 enemy=match.Enemy.GetComponent<Rigidbody2D>().position;
                terrain.DestroyCircle(enemy+Vector2.down*.6f,2);
                Require(!terrain.Grounded(match.Enemy),"Excavated AI must no longer report ground support.");
                Require(match.Enemy.GetComponent<Rigidbody2D>().position==enemy,"Excavation must leave AI position to gravity.");
                Require(SliceAI.Choose(match).slot!=4,"Unsupported AI must not automatically Step out of trench.");
                Debug.Log("M2.1 placement/collision checks passed. Still test fall settling, touch, visuals and full matches on iPhone.");
            }
            finally { match.Rematch(); }
        }
        private static void Require(bool value,string message)
        {
            if(!value) throw new InvalidOperationException("M2.1: "+message);
        }
    }
}
