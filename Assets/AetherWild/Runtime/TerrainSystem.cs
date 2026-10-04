using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AetherWild
{
    // Grid occupancy with marching-square tiles. No arbitrary mesh cutting or collapse simulation.
    public sealed class TerrainSystem : MonoBehaviour
    {
        public Tilemap Cells { get; private set; }
        public BattlefieldDefinition Map { get; private set; }
        private bool[,] solid, initial;
        private Tile[] tiles;
        private Texture2D tileTexture, mask, originalMask, sourceHeight;
        private readonly List<Sprite> sprites = new List<Sprite>();
        private TilemapCollider2D tileCollider;
        private CompositeCollider2D composite;
        private Material artMaterial;
        private Mesh artMesh;
        private Material backgroundMaterial;
        private Mesh backgroundMesh;
        public event System.Action Changed;
        private int W => Map.gridWidth;
        private int H => Map.gridHeight;
        private float S => Map.cellSize;
        public float Left => Map.origin.x;
        public float Right => Left + W * S;
        public float Bottom => Map.origin.y;
        public Vector2 Center(int x, int y) => Map.origin + new Vector2(x * S, y * S);
        private readonly Collider2D[] overlaps = new Collider2D[16];

        public void Initialize(BattlefieldDefinition definition)
        {
            Map = definition;
            transform.position = Map.origin;
            gameObject.AddComponent<Grid>().cellSize = Vector3.one * S;
            var go = new GameObject("Mutable collision grid", typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(transform, false);
            Cells = go.GetComponent<Tilemap>();
            go.GetComponent<TilemapRenderer>().enabled = false;
            var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Static;
            tileCollider = go.AddComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType=CompositeCollider2D.GeometryType.Polygons;
            composite.generationType=CompositeCollider2D.GenerationType.Manual;
            tileTexture = new Texture2D(2, 2);
            tileTexture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); tileTexture.Apply();
            BuildTiles();
            solid = new bool[W + 1, H + 1];
            initial = new bool[W + 1, H + 1];
            for (int x = 0; x <= W; x++)
                for (int y = 0; y <= H; y++)
                    initial[x,y] = Center(x,y).y <= Surface(Center(x,y).x);
            mask = MakeMask(); originalMask = MakeMask();
            BuildArt();
            ResetTerrain();
        }

        public float Surface(float x)
        {
            var points = Map.surface;
            for (int i = 1; i < points.Length; i++)
                if (x <= points[i].x)
                    return Mathf.Lerp(points[i-1].y, points[i].y,
                        Mathf.InverseLerp(points[i-1].x, points[i].x, x));
            return points[points.Length-1].y;
        }
        private Texture2D MakeMask() => new Texture2D(W+1, H+1, TextureFormat.RGBA32, false, true)
            { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };

        private void BuildTiles()
        {
            tiles = new Tile[16];
            Vector2 a = new Vector2(-.5f,-.5f), b = new Vector2(.5f,-.5f);
            Vector2 c = new Vector2(.5f,.5f), d = new Vector2(-.5f,.5f);
            Vector2 ab = new Vector2(0,-.5f), bc = new Vector2(.5f,0);
            Vector2 cd = new Vector2(0,.5f), da = new Vector2(-.5f,0);
            Vector2[][][] shapes = {
                new Vector2[0][], new[]{new[]{a,ab,da}}, new[]{new[]{b,bc,ab}}, new[]{new[]{a,b,bc,da}},
                new[]{new[]{c,cd,bc}}, new[]{new[]{a,ab,da},new[]{c,cd,bc}}, new[]{new[]{ab,b,c,cd}},
                new[]{new[]{a,b,c,cd,da}}, new[]{new[]{d,da,cd}}, new[]{new[]{a,ab,cd,d}},
                new[]{new[]{b,bc,ab},new[]{d,da,cd}}, new[]{new[]{a,b,bc,cd,d}},
                new[]{new[]{da,bc,c,d}}, new[]{new[]{a,ab,bc,c,d}}, new[]{new[]{ab,b,c,d,da}},
                new[]{new[]{a,b,c,d}}
            };
            for (int i=1; i<16; i++)
            {
                // Sprite coordinates are one unit wide; scale to the cell via pixels-per-unit.
                var paths = new List<Vector2[]>();
                foreach (var polygon in shapes[i])
                {
                    var scaled = new Vector2[polygon.Length];
                    for (int v=0;v<polygon.Length;v++) scaled[v]=polygon[v]*S;
                    paths.Add(scaled);
                }
                // A sprite whose world bounds match the cell supplies those local physics paths.
                var sprite = Sprite.Create(tileTexture,new Rect(0,0,2,2),Vector2.one*.5f,2/S);
                sprite.OverridePhysicsShape(paths);
                sprites.Add(sprite);
                tiles[i]=ScriptableObject.CreateInstance<Tile>();
                tiles[i].sprite=sprite; tiles[i].colliderType=Tile.ColliderType.Sprite;
            }
        }
        public void ResetTerrain()
        {
            System.Array.Copy(initial,solid,initial.Length);
            Cells.ClearAllTiles();
            Refresh();
            originalMask.SetPixels(mask.GetPixels()); originalMask.Apply();
        }
        private void Refresh()
        {
            var positions = new Vector3Int[W*H]; var values = new TileBase[W*H];
            int k=0;
            for (int y=0;y<H;y++) for (int x=0;x<W;x++)
            {
                int bits=(solid[x,y]?1:0)|(solid[x+1,y]?2:0)|(solid[x+1,y+1]?4:0)|(solid[x,y+1]?8:0);
                positions[k]=new Vector3Int(x,y,0); values[k++]=tiles[bits];
            }
            Cells.SetTiles(positions,values);
            var colors=new Color[(W+1)*(H+1)];
            for(int y=0;y<=H;y++) for(int x=0;x<=W;x++) colors[y*(W+1)+x]=solid[x,y]?Color.white:Color.black;
            mask.SetPixels(colors); mask.Apply(false);
            tileCollider.ProcessTilemapChanges();
            composite.GenerateGeometry();
            Physics2D.SyncTransforms();
            Changed?.Invoke();
        }
        public int DestroyCircle(Vector2 center,float radius)
        {
            int removed=0;
            for(int x=0;x<=W;x++) for(int y=0;y<=H;y++)
                if(solid[x,y] && (Center(x,y)-center).sqrMagnitude<=radius*radius)
                { solid[x,y]=false; removed++; }
            if(removed>0) Refresh();
            return removed;
        }
        public bool Standing(Vector2 requested, SummonerCombat caster, SummonerCombat opponent,
            float range, out Vector2 destination)
        {
            destination=default;
            if(!caster || requested.x<Left+.5f || requested.x>Right-.5f) return false;

            // Mobile targeting is horizontal-first: the tap chooses an X position and the
            // current terrain resolves the actual standing surface below it. Requiring the
            // finger to land within a tiny vertical band made Step effectively unusable.
            float x=Left+Mathf.Round((requested.x-Left)/S)*S;
            if(!TopSurface(x,out var hit) || hit.normal.y<.45f) return false;

            destination=hit.point+Vector2.up*.68f;
            if(destination.y<Bottom+.65f || destination.y+.61f>Bottom+H*S ||
                Vector2.Distance(caster.transform.position,destination)>range) return false;

            int n=Physics2D.OverlapCapsule(destination,new Vector2(.68f,1.18f),
                CapsuleDirection2D.Vertical,0,new ContactFilter2D{useTriggers=false},overlaps);
            for(int i=0;i<n;i++)
            {
                var overlap=overlaps[i];
                if(!overlap || overlap.gameObject==caster.gameObject || overlap==composite || overlap==tileCollider) continue;
                return false;
            }

            // Keep Step from resolving directly on top of the opposing Summoner even when
            // collider setup changes later.
            if(opponent && Vector2.Distance(destination,opponent.transform.position)<.9f) return false;
            return true;
        }
        public bool WallPosition(Vector2 requested,SummonerCombat caster,float range,Vector2 size,out Vector2 bottom)
        {
            bottom=default;
            if(!caster || size.x<=0 || size.y<=0) return false;

            // Like Step, Wall targeting is horizontal-first for touch screens. The tap selects
            // the column; the wall then snaps to the CURRENT top terrain surface at that X.
            float x=Left+Mathf.Round((requested.x-Left)/S)*S;
            float half=size.x/2+S/2;
            if(x-half<Left || x+half>Right) return false;

            float highest=float.NegativeInfinity,lowest=float.PositiveInfinity;
            for(int i=0;i<3;i++)
            {
                float sampleX=x+Mathf.Lerp(-size.x*.35f,size.x*.35f,i/2f);
                if(!TopSurface(sampleX,out var hit) || hit.normal.y<.35f) return false;
                highest=Mathf.Max(highest,hit.point.y);
                lowest=Mathf.Min(lowest,hit.point.y);
            }

            // Permit ordinary slopes and crater lips, while still preventing a wall from
            // bridging a large gap with no meaningful foundation.
            if(highest-lowest>Mathf.Max(S*3,size.y*.65f)) return false;

            bottom=new Vector2(x,Bottom+Mathf.Ceil((highest-Bottom)/S)*S);
            if(Vector2.Distance(caster.transform.position,bottom)>range ||
                bottom.y+size.y+S/2>Bottom+H*S) return false;

            var filter=new ContactFilter2D{useTriggers=false};
            int n=Physics2D.OverlapBox(bottom+Vector2.up*(size.y*.5f+S*.35f),
                new Vector2(size.x+S*.5f,Mathf.Max(.1f,size.y-S*.3f)),0,filter,overlaps);
            if(n==overlaps.Length) return false;
            for(int i=0;i<n;i++)
            {
                var overlap=overlaps[i];
                if(!overlap || overlap==composite || overlap==tileCollider) continue;
                return false;
            }
            return true;
        }
        private bool TopSurface(float x,out RaycastHit2D surface)
        {
            surface=default;
            foreach(var hit in Physics2D.RaycastAll(new Vector2(x,Bottom+H*S+1),Vector2.down,H*S+2))
                if(hit.collider==composite && hit.fraction>0)
                { surface=hit; return true; }
            return false;
        }
        public bool Grounded(SummonerCombat summoner)
        {
            var shape=summoner.GetComponent<CapsuleCollider2D>();
            int count=shape.Cast(Vector2.down,new ContactFilter2D{useTriggers=false},supportHits,.08f);
            for(int i=0;i<count;i++)
                if(supportHits[i].collider==composite && supportHits[i].normal.y>.5f) return true;
            return false;
        }
        private readonly RaycastHit2D[] supportHits=new RaycastHit2D[8];
        public void CreateWall(Vector2 bottom,Vector2 size)
        {
            for(int x=0;x<=W;x++) for(int y=0;y<=H;y++)
            {
                var p=Center(x,y);
                if(Mathf.Abs(p.x-bottom.x)<=size.x/2 && p.y>=bottom.y-.15f && p.y<=bottom.y+size.y)
                    solid[x,y]=true;
            }
            Refresh();
        }
        public bool Supported(Vector2 position)
        {
            var h=Physics2D.Raycast(position+Vector2.down*.62f,Vector2.down,.6f);
            return h.collider && h.collider.GetComponent<Tilemap>();
        }
        private void BuildArt()
        {
            // Scenic image is an independent, non-colliding backdrop. Never sampled for terrain.
            var background=new GameObject("Wilds scenic background",typeof(MeshFilter),typeof(MeshRenderer));
            background.transform.SetParent(transform,false);
            backgroundMesh=new Mesh();
            // Enlarge the full, unchanged environment image behind the arena so excavations
            // reveal distant scenery rather than the old foreground rocks at identical UVs.
            float bh=H*S*3, bw=bh*Map.mapArt.width/Map.mapArt.height;
            float bx=(W*S-bw)/2,by=H*S+4-bh;
            backgroundMesh.vertices=new[]{new Vector3(bx,by,3),new Vector3(bx+bw,by,3),
                new Vector3(bx+bw,by+bh,3),new Vector3(bx,by+bh,3)};
            backgroundMesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
            backgroundMesh.triangles=new[]{0,2,1,0,3,2};backgroundMesh.RecalculateBounds();
            background.GetComponent<MeshFilter>().sharedMesh=backgroundMesh;
            backgroundMaterial=new Material(Map.mapShader);
            backgroundMaterial.SetFloat("_BackgroundOnly",1);
            backgroundMaterial.mainTexture=Map.mapArt;
            background.GetComponent<MeshRenderer>().sharedMaterial=backgroundMaterial;
            background.GetComponent<MeshRenderer>().sortingOrder=-20;
            var go=new GameObject("Original Wilds art and terrain state",typeof(MeshFilter),typeof(MeshRenderer));
            // The source JPEG is sampled unchanged. Only the occupancy/deformation overlay changes.
            go.transform.SetParent(transform,false);
            artMesh=new Mesh();
            float w=W*S,h=H*S;
            artMesh.vertices=new[]{new Vector3(0,0,2),new Vector3(w,0,2),new Vector3(w,h,2),new Vector3(0,h,2)};
            artMesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
            artMesh.triangles=new[]{0,2,1,0,3,2}; artMesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh=artMesh;
            artMaterial=new Material(Map.mapShader);
            artMaterial.SetTexture("_MainTex",Map.mapArt);
            artMaterial.SetTexture("_Mask",mask); artMaterial.SetTexture("_Initial",originalMask);
            // Coarse visual foreground boundary from the supplied image, not collision tracing.
            sourceHeight=new Texture2D(256,1,TextureFormat.RGBA32,false,true);
            sourceHeight.wrapMode=TextureWrapMode.Clamp; sourceHeight.filterMode=FilterMode.Bilinear;
            var skyline=new Vector2[]{new Vector2(0,.598f),new Vector2(.10f,.63f),new Vector2(.22f,.62f),
                new Vector2(.29f,.54f),new Vector2(.35f,.48f),new Vector2(.40f,.56f),
                new Vector2(.45f,.59f),new Vector2(.47f,.637f),new Vector2(.51f,.63f),
                new Vector2(.59f,.54f),new Vector2(.65f,.47f),new Vector2(.70f,.52f),
                new Vector2(.77f,.63f),new Vector2(.88f,.63f),new Vector2(.94f,.59f),new Vector2(1,.60f)};
            var pixels=new Color[256];
            for(int x=0;x<256;x++)
            {
                float u=x/255f,v=.6f;
                for(int j=1;j<skyline.Length;j++) if(u<=skyline[j].x)
                {v=Mathf.Lerp(skyline[j-1].y,skyline[j].y,Mathf.InverseLerp(skyline[j-1].x,skyline[j].x,u));break;}
                pixels[x]=new Color(v,v,v,1);
            }
            sourceHeight.SetPixels(pixels);sourceHeight.Apply();
            artMaterial.SetTexture("_SourceHeight",sourceHeight);
            artMaterial.SetVector("_Grid",new Vector4(W,H,0,0));
            go.GetComponent<MeshRenderer>().sharedMaterial=artMaterial;
            go.GetComponent<MeshRenderer>().sortingOrder=-10;
        }
        private void OnDestroy()
        {
            if(tiles!=null) foreach(var t in tiles) if(t) Destroy(t);
            foreach(var s in sprites) if(s) Destroy(s);
            if(tileTexture) Destroy(tileTexture); if(mask) Destroy(mask); if(originalMask) Destroy(originalMask);
            if(artMaterial) Destroy(artMaterial); if(artMesh) Destroy(artMesh);
            if(sourceHeight) Destroy(sourceHeight);
            if(backgroundMaterial) Destroy(backgroundMaterial); if(backgroundMesh) Destroy(backgroundMesh);
        }
    }
}
