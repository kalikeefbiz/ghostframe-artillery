using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AetherWild
{
    // Extends the validated M0 composition with independent combat and presentation components.
    public sealed class FoundationScene : MonoBehaviour
    {
        [SerializeField] private BattlefieldDefinition battlefield;
        [SerializeField] private SummonerDefinition mae;
        [SerializeField] private ProductionArt productionArt;
        private MatchManager match;
        private CombatHUD combatHUD;
        private bool focused = true;
        private bool paused;
        private MovementController player;
        private MovementController enemy;
        private HoldControl left;
        private HoldControl right;
        private HoldControl hop;
        private RectTransform safeRoot;
        private Text status;
        private Image statusSkin;
        private Camera arenaCamera;
        private Sprite placeholder;
        private Texture2D texture;
        private PhysicsMaterial2D material;
        private Rect lastSafe;
        private int lastWidth, lastHeight;
        private bool landscape;

        private void Awake()
        {
            if (!battlefield || battlefield.surface == null || battlefield.surface.Length == 0)
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
            if (!mae || mae.startingLoadout == null || mae.startingLoadout.Length == 0)
                throw new System.InvalidOperationException("Missing Mae/Sigil data.");
            var playerCombat = player.gameObject.AddComponent<SummonerCombat>();
            var enemyCombat = enemy.gameObject.AddComponent<SummonerCombat>();
            playerCombat.Initialize(Side.Player, mae);
            enemyCombat.Initialize(Side.Enemy, mae);
            match = gameObject.AddComponent<MatchManager>();
            match.Initialize(playerCombat, enemyCombat, battlefield, placeholder, productionArt);
            player.gameObject.AddComponent<MaePresentation>().Initialize(playerCombat,match,productionArt,
                player.GetComponentInChildren<SpriteRenderer>());
            enemy.gameObject.AddComponent<MaePresentation>().Initialize(enemyCombat,match,productionArt,
                enemy.GetComponentInChildren<SpriteRenderer>());
            match.StateChanged += ClearInput;
            combatHUD = gameObject.AddComponent<CombatHUD>();
            combatHUD.Initialize(safeRoot, match, placeholder, productionArt);
            SetMenuVisibility();
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
            var title = new GameObject("AetherWild title",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            title.transform.SetParent(safeRoot,false);
            title.sprite=productionArt.title;title.preserveAspect=true;title.raycastTarget=false;
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -30), new Vector2(-32, 42));
            status = Label("", safeRoot, 22);
            Place(status.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -76), new Vector2(-32, 44));
            // Use a compact frame behind the existing turn/timer text, not across the arena.
            statusSkin=ProductionArt.Skin(status,productionArt.hudPanel);
            statusSkin.rectTransform.anchorMin=statusSkin.rectTransform.anchorMax=new Vector2(.5f,1);
            statusSkin.rectTransform.anchoredPosition=new Vector2(0,-76);
            statusSkin.rectTransform.sizeDelta=new Vector2(520,44);
            ProductionArt.TextOutline(status);
            left = Control("LEFT", new Vector2(0, 0), new Vector2(90, 150));
            right = Control("RIGHT", new Vector2(0, 0), new Vector2(230, 150));
            hop = Control("HOP", new Vector2(1, 0), new Vector2(-90, 150));
            hop.Pressed = () => { if (landscape && match && match.HopAllowed()) player.RequestHop(); };
        }

        private HoldControl Control(string label, Vector2 anchor, Vector2 position)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(HoldControl));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(safeRoot, false);
            Place(rect, anchor, anchor, position, new Vector2(120, 100));
            go.GetComponent<Image>().color = Color.clear;
            ProductionArt.Skin(go.GetComponent<Image>(),productionArt.secondaryButton);
            var text = Label(label, rect, 26);
            ProductionArt.TextOutline(text);
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
            SetMenuVisibility();
            if (Screen.width != lastWidth || Screen.height != lastHeight || Screen.safeArea != lastSafe)
                RefreshViewport();
            float keyboard = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            player.SetDirection(landscape && match.PlayerCanMove ? Mathf.Clamp(keyboard + (right.Held ? 1 : 0) - (left.Held ? 1 : 0), -1, 1) : 0);
            if (landscape && Input.GetKeyDown(KeyCode.Space) && match.HopAllowed()) player.RequestHop();
            string turn = match.Turns.Phase == TurnPhase.Finished ? match.Result
                : match.Turns.Phase == TurnPhase.Resolving ? "BOLT IN FLIGHT"
                : match.Turns.ActiveSide == Side.Player ? "YOUR TURN" : "AI TURN";
            status.text = landscape
                ? $"{turn}  |  {Mathf.CeilToInt(match.Turns.SecondsRemaining)}s  |  Turn {match.Turns.TurnNumber}"
                : "Rotate your phone to landscape";
        }
        private void SetMenuVisibility()
        {
            bool visible=!match.InMenu;
            status.gameObject.SetActive(visible);
            statusSkin.gameObject.SetActive(visible);
            left.gameObject.SetActive(visible);
            right.gameObject.SetActive(visible);
            hop.gameObject.SetActive(visible);
            player.GetComponentInChildren<SpriteRenderer>().enabled=visible;
            enemy.GetComponentInChildren<SpriteRenderer>().enabled=visible;
        }

        private void LateUpdate()
        {
            if(!player || !enemy || !arenaCamera) return;
            // A simple fit only when excavation puts a Summoner below the starting view.
            float bottom=Mathf.Min(-10,Mathf.Min(player.transform.position.y,enemy.transform.position.y)-3);
            float size=Mathf.Max(12,Mathf.Max(25/arenaCamera.aspect,(14-bottom)/2));
            arenaCamera.orthographicSize=size;
            arenaCamera.transform.position=new Vector3(0,(14+bottom)/2,-10);
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
            arenaCamera.orthographicSize = Mathf.Max(12f, 25f / arenaCamera.aspect);
            UpdateSuspension();
            ClearInput();
        }

        private void ClearInput()
        {
            if (left) left.Clear();
            if (right) right.Clear();
            if (hop) hop.Clear();
            if (player) player.ClearInput();
            if (combatHUD && combatHUD.Aim) combatHUD.Aim.CancelDrag();
        }
        private void UpdateSuspension()
        {
            if (match) match.Suspended = !landscape || !focused || paused;
        }
        private void OnApplicationFocus(bool value)
        {
            focused = value;
            UpdateSuspension();
            if (!value) ClearInput();
        }
        private void OnApplicationPause(bool value)
        {
            paused = value;
            UpdateSuspension();
            if (value) ClearInput();
        }
        private void OnDestroy()
        {
            if (match) match.StateChanged -= ClearInput;
            if (placeholder) Destroy(placeholder);
            if (texture) Destroy(texture);
            if (material) Destroy(material);
        }
    }
}
