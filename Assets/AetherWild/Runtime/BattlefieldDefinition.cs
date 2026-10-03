using UnityEngine;

namespace AetherWild
{
    [CreateAssetMenu(menuName = "AetherWild/Battlefield")]
    public sealed class BattlefieldDefinition : ScriptableObject
    {
        // Authored column heights; each cell is 0.5 world units. No procedural map generation.
        public int[] heights;
        public float cellSize = 0.5f;
        public Vector2 origin = new Vector2(-16, -5);
        public Vector2 playerSpawn = new Vector2(-12, 0);
        public Vector2 enemySpawn = new Vector2(12, 0);
        public float killY = -8;
        public float moveSpeed = 3;
        public float hopSpeed = 5;
        public Vector2[] surface;
        public int gridWidth = 192;
        public int gridHeight = 128;
        public Texture2D mapArt;
        public Shader mapShader;
        public float movementPerTurn = 5;
    }
}
