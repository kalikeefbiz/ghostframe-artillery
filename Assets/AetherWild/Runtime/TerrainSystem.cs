using UnityEngine;
using UnityEngine.Tilemaps;

namespace AetherWild
{
    // Cell occupancy and collision share one Tilemap. Future destruction/construction
    // must edit this map, allowing both authored and created terrain to behave alike.
    public sealed class TerrainSystem : MonoBehaviour
    {
        public Tilemap Cells { get; private set; }
        private Tile tile;
        private Sprite sprite;
        private Texture2D texture;

        public void Initialize(BattlefieldDefinition definition)
        {
            var grid = gameObject.AddComponent<Grid>();
            grid.cellSize = Vector3.one * definition.cellSize;
            transform.position = definition.origin;
            var map = new GameObject("Solid terrain", typeof(Tilemap), typeof(TilemapRenderer));
            map.transform.SetParent(transform, false);
            Cells = map.GetComponent<Tilemap>();
            texture = new Texture2D(2, 2) { filterMode = FilterMode.Point };
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f, 4);
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.Grid;
            tile.color = new Color(0.33f, 0.40f, 0.31f);
            for (int x = 0; x < definition.heights.Length; x++)
                for (int y = 0; y < definition.heights[x]; y++)
                    Cells.SetTile(new Vector3Int(x, y, 0), tile);
            var body = map.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var collider = map.AddComponent<TilemapCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            map.AddComponent<CompositeCollider2D>();
        }

        private void OnDestroy()
        {
            if (tile) Destroy(tile);
            if (sprite) Destroy(sprite);
            if (texture) Destroy(texture);
        }
    }
}
