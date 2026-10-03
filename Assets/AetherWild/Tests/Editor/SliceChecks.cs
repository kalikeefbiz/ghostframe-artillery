using System;
using UnityEditor;
using UnityEngine;

namespace AetherWild.Editor
{
    public static class SliceChecks
    {
        private static void Check(bool condition,string message)
        {if(!condition) throw new InvalidOperationException("M2 check failed: "+message);}
        [MenuItem("AetherWild/Run M2 checks")]
        public static void Run()
        {
            var health=new HealthState(100);
            health.GrantShield(25);health.Damage(30);
            Check(health.Shield==0 && health.Current==95,"shield overflow");
            health.Reset();health.GrantShield(25);health.Damage(20);
            Check(health.Shield==5 && health.Current==100,"shield absorbs first");
            health.Reset();Check(health.Shield==0 && health.Current==100,"full health reset");
            var mae=AssetDatabase.LoadAssetAtPath<SummonerDefinition>("Assets/AetherWild/Data/Mae.asset");
            Check(mae && mae.startingLoadout.Length==6,"loadout");
            var loadout=new SigilLoadout(mae.startingLoadout);
            Check(loadout.Matches(SummonerClass.Conduit)==1,"starter resonance inactive");
            int[] uses={0,3,2,2,2,2};
            for(int slot=1;slot<6;slot++)
            {
                Check(loadout.Uses(slot)==uses[slot],"initial charges");
                for(int n=0;n<uses[slot];n++) Check(loadout.Spend(slot),"spend");
                Check(!loadout.Spend(slot),"exhausted rejected");
            }
            loadout.Reset();Check(loadout.Uses(1)==3 && loadout.Uses(5)==2,"resource reset");
            var go=new GameObject("Resonance test",typeof(Rigidbody2D),typeof(CapsuleCollider2D),typeof(MovementController));
            var definition=ScriptableObject.CreateInstance<SummonerDefinition>();
            var aligned=ScriptableObject.CreateInstance<SigilDefinition>();
            var offClass=ScriptableObject.CreateInstance<SigilDefinition>();
            try
            {
                aligned.classAffinity=SummonerClass.Conduit;aligned.resonanceEligible=true;
                aligned.resonanceModifiedStat="shield";aligned.resonanceBonusPercent=5;
                offClass.classAffinity=SummonerClass.Expellant;offClass.resonanceModifiedStat="damage";
                offClass.resonanceBonusPercent=5;offClass.resonanceEligible=true;
                definition.summonerClass=SummonerClass.Conduit;
                var combat=go.AddComponent<SummonerCombat>();
                for(int count=4;count<=6;count++)
                {
                    definition.startingLoadout=new SigilDefinition[6];
                    for(int i=0;i<6;i++) definition.startingLoadout[i]=i<count?aligned:offClass;
                    combat.Initialize(Side.Player,definition);
                    Check(combat.Resonant==(count>=5),"75 percent threshold");
                    Check(Mathf.Approximately(combat.Bonus(aligned,"shield"),count>=5?1.05f:1),"matching primary");
                    Check(combat.Bonus(aligned,"damage")==1 && combat.Bonus(offClass,"damage")==1,"no generic/off-class bonus");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(aligned);UnityEngine.Object.DestroyImmediate(offClass);
            }
            var map=AssetDatabase.LoadAssetAtPath<BattlefieldDefinition>("Assets/AetherWild/Data/Battlefield.asset");
            Check(map && map.mapArt && map.mapShader && map.surface.Length==14,"map assets");
            Check(map.surface[6].y>map.surface[3].y+1.1f,"center blocks direct shot");
            Check(mae.startingLoadout[2].terrainDamageRadius>mae.startingLoadout[1].terrainDamageRadius*1.5f,"Fault terrain strength");
            Debug.Log("M2 data/rules checks passed. Grid collision, rendering and physical iPhone tests remain required.");
        }
    }
}
