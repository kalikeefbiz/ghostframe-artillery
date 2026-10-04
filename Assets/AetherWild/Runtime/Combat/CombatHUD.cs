using UnityEngine;
using UnityEngine.UI;

namespace AetherWild
{
    public sealed class CombatHUD : MonoBehaviour
    {
        private MatchManager match;
        private AimController aim;
        private Text playerHP, enemyHP, power, result;
        private Text playerShield,enemyShield;
        private Image menuTitle;
        private ProductionArt art;
        private readonly Image[] sigilSkins=new Image[6];
        private Button fire, rematch;
        private GameObject resultPanel;
        private CanvasGroup matchUI;
        private SpriteRenderer impact;
        private float impactSeconds;
        private readonly Button[] sigilButtons=new Button[6];
        private readonly Text[] sigilLabels=new Text[6];
        public AimController Aim => aim;

        public void Initialize(RectTransform parent, MatchManager session, Sprite sprite, ProductionArt assets)
        {
            match = session;
            art=assets;
            var menuParent=parent;
            var group=new GameObject("In-match UI",typeof(RectTransform),typeof(CanvasGroup));
            parent=group.GetComponent<RectTransform>();
            parent.SetParent(menuParent,false);
            parent.anchorMin=Vector2.zero;parent.anchorMax=Vector2.one;
            parent.offsetMin=parent.offsetMax=Vector2.zero;
            matchUI=group.GetComponent<CanvasGroup>();
            SetMenuVisibility();
            var aimArea = new GameObject("Drag to aim", typeof(RectTransform), typeof(Image), typeof(AimController));
            var area = aimArea.GetComponent<RectTransform>();
            area.SetParent(parent, false);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(0, 212);
            area.offsetMax = new Vector2(0, -126);
            area.SetAsFirstSibling();
            aimArea.GetComponent<Image>().color = Color.clear;
            aim = aimArea.GetComponent<AimController>();
            aim.Initialize(match, sprite);
            playerHP = Label(parent, 18);
            Place(playerHP.rectTransform, new Vector2(0, 1), new Vector2(94, -112), new Vector2(174, 38));
            playerShield=Label(parent,18);
            Place(playerShield.rectTransform,new Vector2(0,1),new Vector2(266,-112),new Vector2(174,38));
            enemyHP = Label(parent, 18);
            Place(enemyHP.rectTransform, new Vector2(1, 1), new Vector2(-266, -112), new Vector2(174, 38));
            enemyShield=Label(parent,18);
            Place(enemyShield.rectTransform,new Vector2(1,1),new Vector2(-94,-112),new Vector2(174,38));
            ProductionArt.Skin(playerHP,art.healthFrame);ProductionArt.Skin(enemyHP,art.healthFrame);
            ProductionArt.Skin(playerShield,art.shieldFrame);ProductionArt.Skin(enemyShield,art.shieldFrame);
            power = Label(parent, 22);
            Place(power.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 145), new Vector2(480, 75));
            fire = Button(parent, "FIRE", new Vector2(1, 0), new Vector2(-230, 150), new Vector2(130, 104));
            SkinButton(fire,art.primaryButton);
            fire.onClick.AddListener(() => match.CastSigil(Side.Player,match.SelectedSlot,aim.Direction,aim.Power,aim.Target));
            for(int i=0;i<6;i++)
            {
                int slot=i;
                sigilButtons[i]=Button(parent,"Sigil",Vector2.zero,Vector2.zero,new Vector2(1,86));
                var r=sigilButtons[i].GetComponent<RectTransform>();
                r.anchorMin=new Vector2(i/6f,0);r.anchorMax=new Vector2((i+1)/6f,0);
                r.anchoredPosition=new Vector2(0,46);r.sizeDelta=new Vector2(-8,86);
                sigilSkins[i]=SkinButton(sigilButtons[i],art.sigilSlot);
                var icon=new GameObject("Sigil icon",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(r,false);icon.raycastTarget=false;icon.preserveAspect=true;
                icon.sprite=match.Player.Loadout.Get(i).icon;
                Place(icon.rectTransform,new Vector2(0,.5f),new Vector2(35,0),new Vector2(56,56));
                sigilLabels[i]=sigilButtons[i].GetComponentInChildren<Text>();sigilLabels[i].fontSize=16;
                sigilLabels[i].rectTransform.offsetMin=new Vector2(64,3);
                sigilLabels[i].rectTransform.offsetMax=new Vector2(-5,-3);
                sigilButtons[i].onClick.AddListener(()=>match.Select(slot));
            }
            resultPanel = new GameObject("Match result", typeof(RectTransform), typeof(Image));
            var panel = resultPanel.GetComponent<RectTransform>();
            panel.SetParent(menuParent, false);
            Place(panel, Vector2.one * 0.5f, Vector2.zero, new Vector2(420, 240));
            resultPanel.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.09f, 0.97f);
            result = Label(panel, 38);
            Place(result.rectTransform, Vector2.one * 0.5f, new Vector2(0, 50), new Vector2(380, 90));
            menuTitle=new GameObject("Production title",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            menuTitle.transform.SetParent(panel,false);menuTitle.sprite=art.title;
            menuTitle.preserveAspect=true;menuTitle.raycastTarget=false;
            Place(menuTitle.rectTransform,Vector2.one*.5f,new Vector2(0,70),new Vector2(360,100));
            rematch = Button(panel, "REMATCH", Vector2.one * 0.5f, new Vector2(0, -45), new Vector2(220, 80));
            SkinButton(rematch,art.primaryButton);
            rematch.onClick.AddListener(() => { aim.ResetAim(); if(match.InMenu) match.StartMatch(); else match.Rematch(); });
            resultPanel.SetActive(match.InMenu);
            result.text="THE WILDS — DEPTH 1";
            rematch.GetComponentInChildren<Text>().text="PLAY";
            impact = new GameObject("Impact marker", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            impact.sprite = sprite;
            impact.color = new Color(1, 0.88f, 0.55f);
            impact.sortingOrder = 6;
            impact.enabled = false;
            match.Impact += OnImpact;
            match.StateChanged += OnStateChanged;
        }
        private void OnStateChanged()
        {
            aim.CancelDrag();
            if (match.Turns.TurnNumber == 1 && match.Turns.Phase == TurnPhase.Acting)
            { impactSeconds = 0; impact.enabled = false; }
        }
        private void OnImpact(Vector2 point)
        {
            impact.transform.position = point;
            impact.transform.localScale = Vector3.one * 0.45f;
            impact.enabled = true;
            impactSeconds = 0.18f;
        }
        private void Update()
        {
            if (!match) return;
            SetMenuVisibility();
            playerHP.text = $"MAE {match.Player.Health.Current} HP";
            enemyHP.text = $"AI {match.Enemy.Health.Current} HP";
            playerShield.text=$"{match.Player.Health.Shield} Shield";
            enemyShield.text=$"{match.Enemy.Health.Shield} Shield";
            var selected=match.Player.Loadout.Get(match.SelectedSlot);
            string feedback=selected.usesProjectile?$"Power {Mathf.RoundToInt(aim.Power*100)}%":selected.form==SigilForm.Ward?
                "Shield ready":aim.TargetValid?"Valid target":"Tap a valid surface in range";
            power.text=$"{selected.displayName}  |  {feedback}\nMove {match.MovementLeft:0.0}  |  Resonance {(match.Player.Resonant?"ON":"OFF")}";
            fire.interactable = match.PlayerCanAct && match.Player.Loadout.Available(match.SelectedSlot) && aim.TargetValid;
            for(int i=0;i<6;i++)
            {
                var s=match.Player.Loadout.Get(i);
                sigilLabels[i].text=s.displayName+"\n"+(s.unlimitedUses?"Unlimited":match.Player.Loadout.Uses(i)+" left");
                sigilButtons[i].interactable=match.PlayerCanAct && match.Player.Loadout.Available(i);
                sigilSkins[i].sprite=i==match.SelectedSlot?art.sigilSelected:art.sigilSlot;
            }
            bool finished = match.InMenu || match.Turns.Phase == TurnPhase.Finished;
            resultPanel.SetActive(finished);
            menuTitle.gameObject.SetActive(match.InMenu);
            result.text = match.InMenu?"THE WILDS — DEPTH 1":match.Result;
            result.fontSize=match.InMenu?20:38;
            result.rectTransform.anchoredPosition=new Vector2(0,match.InMenu?12:50);
            rematch.GetComponentInChildren<Text>().text=match.InMenu?"PLAY":"REMATCH";
            if (impactSeconds > 0) impactSeconds -= Time.deltaTime;
            if (impactSeconds <= 0) impact.enabled = false;
        }
        private void SetMenuVisibility()
        {
            matchUI.alpha=match.InMenu?0:1;
            matchUI.interactable=!match.InMenu;
            matchUI.blocksRaycasts=!match.InMenu;
        }
        private static Text Label(Transform parent, int size)
        {
            var text = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            ProductionArt.TextOutline(text);
            return text;
        }
        private static Image SkinButton(Button button,Sprite sprite)
        {
            button.GetComponent<Image>().color=Color.clear;
            var skin=ProductionArt.Skin(button.GetComponent<Image>(),sprite);
            button.targetGraphic=skin;
            return skin;
        }
        private static Button Button(Transform parent, string text, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), anchor, position, size);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.37f, 0.38f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = Label(go.transform, 26);
            label.text = text;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
        private void OnDestroy()
        {
            if (match) { match.Impact -= OnImpact; match.StateChanged -= OnStateChanged; }
            if (impact) Destroy(impact.gameObject);
        }
    }
}
