using UnityEngine;

namespace AetherWild
{
    [CreateAssetMenu(menuName = "AetherWild/Summoner")]
    public sealed class SummonerDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public SummonerClass summonerClass;
        public int startingHP = 100;
        public SigilDefinition[] startingLoadout;
    }
}
