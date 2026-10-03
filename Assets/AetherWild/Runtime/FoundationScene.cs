using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AetherWild
{
    // M0 composition root. No combat, match rules, or Summoner-specific abilities.
    public sealed class FoundationScene : MonoBehaviour
    {
        [SerializeField] private BattlefieldDefinition battlefield;
        private MovementController player;
        private MovementController enemy;
        private HoldControl left;
        private HoldControl right;
        private HoldControl hop;
        private RectTransform safeRoot;
        private Text status;
        private Camera arenaCamera;
        private Sprite placeholder;
        private Texture2D texture;
        private PhysicsMaterial2D material;
        private Rect lastSafe;
        private int lastWidth, lastHeight;
        private int resets;
        private bool landscape;

        private void Awake()
        {
            if (!battlefield || battlefield.heights == null || battlefield.heights.Length == 0)
                throw new System.InvalidOperationException("Missing authored battlefield definition.");
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            arenaCamera = new GameObject("Arena camera", typeof(Camera)).GetComponent<Camera>();
            arenaCamera.tag = "MainCamera";
            arenaCamera.orthographic = true;
            arenaCamera.transform.position = new Vector3(0, 2, -10);
            arenaCamera.clearFlags = CameraClearFlags.SolidColor;
            arenaCamera.backgroundColor = new Color(0.055f, 0.08f, 0.105f);
            new GameObject("Battlefield").AddComponent<TerrainSystem>().Initialize(battlefield);
            texture = new Texture2D(2, 2) { filterMode = FilterMode.Point };
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            placeholder = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f, 2);
            material = new PhysicsMaterial2D("Summoner friction") { friction = 0, bounciness = 0 };
            player = CreateSummoner("Mae - player", battlefield.playerSpawn, new Color(0.42f, 0.8f, 0.68f));
            enemy = CreateSummoner("Mae - opponent", battlefield.enemySpawn, new Color(0.93f, 0.57f, 0.31f));
            BuildUI();
            RefreshViewport();
        }

        private MovementController CreateSummoner(string label, Vector2 position, Color color)
        {
            var root = new GameObject(label, typeof(Rigidbody2D), typeof(CapsuleCollider2D));
            root.transform.position = position;
            var collider = root.GetComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.65f, 1.2f);
            collider.sharedMaterial = material;
            var visual = new GameObject("Replaceable placeholder", typeof(SpriteRenderer));
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(0.65f, 1.2f, 1);
            var renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = placeholder;
            renderer.color = color;
            renderer.sortingOrder = 2;
            var movement = root.AddComponent<MovementController>();
            movement.Initialize(battlefield.moveSpeed, battlefield.hopSpeed);
            return movement;
        }

        private void BuildUI()
        {
            var events = new GameObject("Event system", typeof(EventSystem), typeof(StandaloneInputModule));
            events.GetComponent<EventSystem>().sendNavigationEvents = false;
            var canvas = new GameObject("Foundation HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1;
            safeRoot = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvas.transform, false);
            var title = Label("AETHERWILD  /  GHOSTFRAME STUDIOS", safeRoot, 25);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -30), new Vector2(-32, 42));
            status = Label("", safeRoot, 22);
            Place(status.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -76), new Vector2(-32, 44));
            left = Control("LEFT", new Vector2(0, 0), new Vector2(90, 76));
            right = Control("RIGHT", new Vector2(0, 0), new Vector2(230, 76));
            hop = Control("HOP", new Vector2(1, 0), new Vector2(-90, 76));
            hop.Pressed = () => { if (landscape) player.RequestHop(); };
        }

        private HoldControl Control(string label, Vector2 anchor, Vector2 position)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(HoldControl));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(safeRoot, false);
            Place(rect, anchor, anchor, position, new Vector2(120, 100));
            go.GetComponent<Image>().color = new Color(0.16f, 0.23f, 0.25f, 0.95f);
            var text = Label(label, rect, 26);
            Place(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return go.GetComponent<HoldControl>();
        }

        private static Text Label(string text, Transform parent, int size)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var result = go.GetComponent<Text>();
            result.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            result.text = text;
            result.fontSize = size;
            result.alignment = TextAnchor.MiddleCenter;
            result.color = Color.white;
            result.raycastTarget = false;
            return result;
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight || Screen.safeArea != lastSafe)
                RefreshViewport();
            float keyboard = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            player.SetDirection(landscape ? Mathf.Clamp(keyboard + (right.Held ? 1 : 0) - (left.Held ? 1 : 0), -1, 1) : 0);
            if (landscape && Input.GetKeyDown(KeyCode.Space)) player.RequestHop();
            CheckBoundary(player, battlefield.playerSpawn);
            CheckBoundary(enemy, battlefield.enemySpawn);
            status.text = landscape
                ? $"M0 INPUT TEST  |  Mint: player   Orange: opponent  |  Boundary resets: {resets}"
                : "Rotate your phone to landscape";
        }

        private void CheckBoundary(MovementController summoner, Vector2 spawn)
        {
            var position = summoner.transform.position;
            float maxX = battlefield.origin.x + battlefield.heights.Length * battlefield.cellSize + 2;
            if (position.y < battlefield.killY || position.x < battlefield.origin.x - 2 || position.x > maxX)
            {
                // M0 diagnostic reset only. M1 replaces this with MatchManager defeat.
                summoner.ResetPosition(spawn);
                resets++;
            }
        }

        private void RefreshViewport()
        {
            lastWidth = Mathf.Max(1, Screen.width);
            lastHeight = Mathf.Max(1, Screen.height);
            lastSafe = Screen.safeArea;
            landscape = lastWidth > lastHeight;
            safeRoot.anchorMin = new Vector2(lastSafe.xMin / lastWidth, lastSafe.yMin / lastHeight);
            safeRoot.anchorMax = new Vector2(lastSafe.xMax / lastWidth, lastSafe.yMax / lastHeight);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            arenaCamera.orthographicSize = Mathf.Max(10.5f, 18f / arenaCamera.aspect);
            ClearInput();
        }

        private void ClearInput()
        {
            if (left) left.Clear();
            if (right) right.Clear();
            if (hop) hop.Clear();
            if (player) player.ClearInput();
        }
        private void OnApplicationFocus(bool focused) { if (!focused) ClearInput(); }
        private void OnApplicationPause(bool paused) { if (paused) ClearInput(); }
        private void OnDestroy()
        {
            if (placeholder) Destroy(placeholder);
            if (texture) Destroy(texture);
            if (material) Destroy(material);
        }
    }
}
