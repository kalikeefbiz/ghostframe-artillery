using UnityEngine;
using UnityEngine.UI;

namespace AetherWild
{
    public sealed class CombatHUD : MonoBehaviour
    {
        private MatchManager match;
        private AimController aim;
        private ProductionArt art;

        private Text playerHP, enemyHP, playerShield, enemyShield, power;
        private readonly Button[] sigilButtons=new Button[6];
        private readonly Text[] sigilLabels=new Text[6];
        private readonly Image[] sigilSkins=new Image[6];
        private readonly Image[] sigilIcons=new Image[6];
        private Button fire;

        private CanvasGroup matchUI;
        private SpriteRenderer impact;
        private float impactSeconds;

        private GameObject overlay;
        private RectTransform overlayPanel;
        private RectTransform mainRoot, loadoutRoot, resultRoot;
        private Image menuTitle;
        private Text resultText, loadoutHeader, loadoutDetail, resonanceText, emptySchool;
        private Button playButton, loadoutButton, backButton, rematchButton, resultMenuButton;
        private readonly Button[] schoolButtons=new Button[8];
        private readonly Image[] schoolSkins=new Image[8];
        private Button[] libraryButtons;
        private Text[] libraryLabels;
        private readonly Button[] equippedButtons=new Button[6];
        private readonly Text[] equippedLabels=new Text[6];
        private readonly Image[] equippedSkins=new Image[6];

        private int editSlot;
        private int selectedLibraryIndex=-1;
        private SigilSchool selectedSchool=SigilSchool.Origin;
        private bool loadoutOpen;

        public AimController Aim => aim;

        public void Initialize(RectTransform parent, MatchManager session, Sprite sprite, ProductionArt assets)
        {
            match=session;
            art=assets;

            BuildMatchHUD(parent,sprite);
            BuildOverlay(parent);

            impact=new GameObject("Impact marker",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            impact.sprite=sprite;
            impact.color=new Color(1,.88f,.55f);
            impact.sortingOrder=6;
            impact.enabled=false;

            match.Impact+=OnImpact;
            match.StateChanged+=OnStateChanged;
            ShowMainMenu();
            RefreshLoadout();
        }

        private void BuildMatchHUD(RectTransform menuParent,Sprite sprite)
        {
            var group=new GameObject("In-match UI",typeof(RectTransform),typeof(CanvasGroup));
            var parent=group.GetComponent<RectTransform>();
            parent.SetParent(menuParent,false);
            parent.anchorMin=Vector2.zero;parent.anchorMax=Vector2.one;
            parent.offsetMin=parent.offsetMax=Vector2.zero;
            matchUI=group.GetComponent<CanvasGroup>();

            var aimArea=new GameObject("Drag to aim",typeof(RectTransform),typeof(Image),typeof(AimController));
            var area=aimArea.GetComponent<RectTransform>();
            area.SetParent(parent,false);
            area.anchorMin=Vector2.zero;area.anchorMax=Vector2.one;
            area.offsetMin=new Vector2(0,212);area.offsetMax=new Vector2(0,-126);
            area.SetAsFirstSibling();
            aimArea.GetComponent<Image>().color=Color.clear;
            aim=aimArea.GetComponent<AimController>();
            aim.Initialize(match,sprite);

            playerHP=Label(parent,18);
            Place(playerHP.rectTransform,new Vector2(0,1),new Vector2(94,-112),new Vector2(174,38));
            playerShield=Label(parent,18);
            Place(playerShield.rectTransform,new Vector2(0,1),new Vector2(266,-112),new Vector2(174,38));
            enemyHP=Label(parent,18);
            Place(enemyHP.rectTransform,new Vector2(1,1),new Vector2(-266,-112),new Vector2(174,38));
            enemyShield=Label(parent,18);
            Place(enemyShield.rectTransform,new Vector2(1,1),new Vector2(-94,-112),new Vector2(174,38));
            ProductionArt.Skin(playerHP,art.healthFrame);ProductionArt.Skin(enemyHP,art.healthFrame);
            ProductionArt.Skin(playerShield,art.shieldFrame);ProductionArt.Skin(enemyShield,art.shieldFrame);

            power=Label(parent,22);
            Place(power.rectTransform,new Vector2(.5f,0),new Vector2(0,145),new Vector2(480,75));

            fire=Button(parent,"FIRE",new Vector2(1,0),new Vector2(-230,150),new Vector2(130,104));
            SkinButton(fire,art.primaryButton);
            fire.onClick.AddListener(()=>match.CastSigil(Side.Player,match.SelectedSlot,aim.Direction,aim.Power,aim.Target));

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
                sigilIcons[i]=icon;
                Place(icon.rectTransform,new Vector2(0,.5f),new Vector2(35,0),new Vector2(56,56));

                sigilLabels[i]=sigilButtons[i].GetComponentInChildren<Text>();
                sigilLabels[i].fontSize=16;
                sigilLabels[i].rectTransform.offsetMin=new Vector2(64,3);
                sigilLabels[i].rectTransform.offsetMax=new Vector2(-5,-3);
                sigilButtons[i].onClick.AddListener(()=>match.Select(slot));
            }
        }

        private void BuildOverlay(RectTransform parent)
        {
            overlay=new GameObject("Menu and results",typeof(RectTransform),typeof(Image));
            overlayPanel=overlay.GetComponent<RectTransform>();
            overlayPanel.SetParent(parent,false);
            Place(overlayPanel,Vector2.one*.5f,Vector2.zero,new Vector2(1040,620));
            overlay.GetComponent<Image>().color=new Color(.035f,.055f,.07f,.97f);

            menuTitle=new GameObject("Production title",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
            menuTitle.transform.SetParent(overlayPanel,false);
            menuTitle.sprite=art.title;menuTitle.preserveAspect=true;menuTitle.raycastTarget=false;
            Place(menuTitle.rectTransform,Vector2.one*.5f,new Vector2(0,245),new Vector2(380,95));

            mainRoot=Group(overlayPanel,"Main menu");
            var map=Label(mainRoot,20);map.text="THE WILDS — DEPTH 1";
            Place(map.rectTransform,Vector2.one*.5f,new Vector2(0,130),new Vector2(500,40));

            playButton=Button(mainRoot,"PLAY",Vector2.one*.5f,new Vector2(0,25),new Vector2(260,82));
            SkinButton(playButton,art.primaryButton);
            playButton.onClick.AddListener(()=>{loadoutOpen=false;aim.ResetAim();match.StartMatch();});

            loadoutButton=Button(mainRoot,"LOADOUT",Vector2.one*.5f,new Vector2(0,-80),new Vector2(260,72));
            SkinButton(loadoutButton,art.secondaryButton);
            loadoutButton.onClick.AddListener(ShowLoadout);

            var note=Label(mainRoot,15);
            note.text="Your saved six Sigils are carried into every battle.";
            Place(note.rectTransform,Vector2.one*.5f,new Vector2(0,-155),new Vector2(560,34));

            loadoutRoot=Group(overlayPanel,"Loadout menu");
            BuildLoadout(loadoutRoot);

            resultRoot=Group(overlayPanel,"Result menu");
            resultText=Label(resultRoot,38);
            Place(resultText.rectTransform,Vector2.one*.5f,new Vector2(0,45),new Vector2(380,90));
            rematchButton=Button(resultRoot,"REMATCH",Vector2.one*.5f,new Vector2(-118,-55),new Vector2(205,72));
            SkinButton(rematchButton,art.primaryButton);
            rematchButton.onClick.AddListener(()=>{aim.ResetAim();match.Rematch();});
            resultMenuButton=Button(resultRoot,"MAIN MENU",Vector2.one*.5f,new Vector2(118,-55),new Vector2(205,72));
            SkinButton(resultMenuButton,art.secondaryButton);
            resultMenuButton.onClick.AddListener(()=>{aim.ResetAim();match.ReturnToMenu();ShowMainMenu();});

            mainRoot.gameObject.SetActive(false);
            loadoutRoot.gameObject.SetActive(false);
            resultRoot.gameObject.SetActive(false);
        }

        private void BuildLoadout(RectTransform parent)
        {
            loadoutHeader=Label(parent,20);
            loadoutHeader.text="LOADOUT — SELECT A SCHOOL, THEN EQUIP SIX SIGILS";
            Place(loadoutHeader.rectTransform,Vector2.one*.5f,new Vector2(0,185),new Vector2(820,34));

            resonanceText=Label(parent,16);
            Place(resonanceText.rectTransform,Vector2.one*.5f,new Vector2(0,150),new Vector2(720,30));

            for(int i=0;i<8;i++)
            {
                int schoolIndex=i;
                var button=Button(parent,((SigilSchool)i).ToString(),Vector2.one*.5f,
                    new Vector2(-420+i*120,95),new Vector2(108,70));
                schoolSkins[i]=SkinButton(button,art.sigilSlot);
                var icon=new GameObject("School icon",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(button.transform,false);
                icon.sprite=SchoolIcon((SigilSchool)i);icon.preserveAspect=true;icon.raycastTarget=false;
                Place(icon.rectTransform,new Vector2(.5f,.68f),new Vector2(0,0),new Vector2(34,34));
                var label=button.GetComponentInChildren<Text>();
                label.fontSize=12;label.alignment=TextAnchor.LowerCenter;
                label.rectTransform.offsetMin=new Vector2(2,2);label.rectTransform.offsetMax=new Vector2(-2,-38);
                button.onClick.AddListener(()=>{selectedSchool=(SigilSchool)schoolIndex;selectedLibraryIndex=-1;RefreshLoadout();});
                schoolButtons[i]=button;
            }

            var library=match.Library;
            libraryButtons=new Button[library.Length];
            libraryLabels=new Text[library.Length];
            for(int i=0;i<library.Length;i++)
            {
                int index=i;
                var button=Button(parent,"",Vector2.one*.5f,Vector2.zero,new Vector2(190,76));
                SkinButton(button,art.sigilSlot);
                var icon=new GameObject("Library icon",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(button.transform,false);
                icon.sprite=library[i].icon;icon.preserveAspect=true;icon.raycastTarget=false;
                Place(icon.rectTransform,new Vector2(0,.5f),new Vector2(34,0),new Vector2(58,58));
                var label=button.GetComponentInChildren<Text>();
                label.fontSize=13;label.alignment=TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin=new Vector2(68,4);label.rectTransform.offsetMax=new Vector2(-5,-4);
                button.onClick.AddListener(()=>{
                    selectedLibraryIndex=index;
                    match.EquipPlayerSlot(editSlot,index);
                    RefreshLoadout();
                });
                libraryButtons[i]=button;libraryLabels[i]=label;
            }

            emptySchool=Label(parent,17);
            Place(emptySchool.rectTransform,Vector2.one*.5f,new Vector2(-200,-5),new Vector2(500,50));

            loadoutDetail=Label(parent,15);
            loadoutDetail.alignment=TextAnchor.UpperLeft;
            Place(loadoutDetail.rectTransform,Vector2.one*.5f,new Vector2(325,-15),new Vector2(330,180));

            for(int i=0;i<6;i++)
            {
                int slot=i;
                var button=Button(parent,"",Vector2.one*.5f,new Vector2(-355+i*142,-170),new Vector2(132,78));
                equippedSkins[i]=SkinButton(button,art.sigilSlot);
                button.onClick.AddListener(()=>{editSlot=slot;RefreshLoadout();});
                equippedButtons[i]=button;
                equippedLabels[i]=button.GetComponentInChildren<Text>();
                equippedLabels[i].fontSize=12;
            }

            backButton=Button(parent,"BACK",Vector2.one*.5f,new Vector2(0,-255),new Vector2(180,62));
            SkinButton(backButton,art.secondaryButton);
            backButton.onClick.AddListener(ShowMainMenu);
        }

        private void Update()
        {
            if(!match) return;
            SetMenuVisibility();

            playerHP.text=$"MAE {match.Player.Health.Current} HP";
            enemyHP.text=$"AI {match.Enemy.Health.Current} HP";
            playerShield.text=$"{match.Player.Health.Shield} Shield";
            enemyShield.text=$"{match.Enemy.Health.Shield} Shield";

            var selected=match.Player.Loadout.Get(match.SelectedSlot);
            bool recall=match.IsRecallReady(Side.Player,match.SelectedSlot);
            bool ballistic=selected && selected.usesProjectile;
            string feedback=recall?"RECALL READY":ballistic?$"Power {Mathf.RoundToInt(aim.Power*100)}%":
                selected && selected.form==SigilForm.Ward?"Shield ready":aim.TargetValid?"Valid target":"Tap a valid target";
            power.text=selected?$"{(recall?"RECALL":selected.displayName)}  |  {feedback}\nMove {match.MovementLeft:0.0}  |  Resonance {(match.Player.Resonant?"ON":"OFF")}":"";
            fire.interactable=selected && match.PlayerCanAct && match.CanUseSlot(Side.Player,match.SelectedSlot) && aim.TargetValid;

            for(int i=0;i<6;i++)
            {
                var s=match.Player.Loadout.Get(i);
                if(sigilIcons[i]) sigilIcons[i].sprite=s?s.icon:null;
                bool slotRecall=match.IsRecallReady(Side.Player,i);
                sigilLabels[i].text=s?(slotRecall?"RECALL":s.displayName)+"\n"+(slotRecall?"Ready":s.unlimitedUses?"Unlimited":match.Player.Loadout.Uses(i)+" left"):"EMPTY";
                sigilButtons[i].interactable=s && match.PlayerCanAct && match.CanUseSlot(Side.Player,i);
                sigilSkins[i].sprite=i==match.SelectedSlot?art.sigilSelected:art.sigilSlot;
            }

            if(match.InMenu)
            {
                overlay.SetActive(true);
                overlayPanel.sizeDelta=new Vector2(1040,620);
                menuTitle.gameObject.SetActive(true);
                resultRoot.gameObject.SetActive(false);
                if(loadoutOpen) RefreshLoadout();
            }
            else if(match.Turns.Phase==TurnPhase.Finished)
            {
                overlay.SetActive(true);
                overlayPanel.sizeDelta=new Vector2(420,240);
                menuTitle.gameObject.SetActive(false);
                mainRoot.gameObject.SetActive(false);
                loadoutRoot.gameObject.SetActive(false);
                resultRoot.gameObject.SetActive(true);
                resultText.text=match.Result;
            }
            else overlay.SetActive(false);

            if(impactSeconds>0) impactSeconds-=Time.deltaTime;
            if(impactSeconds<=0) impact.enabled=false;
        }

        private void ShowMainMenu()
        {
            loadoutOpen=false;
            if(mainRoot) mainRoot.gameObject.SetActive(true);
            if(loadoutRoot) loadoutRoot.gameObject.SetActive(false);
            if(resultRoot) resultRoot.gameObject.SetActive(false);
        }

        private void ShowLoadout()
        {
            loadoutOpen=true;
            mainRoot.gameObject.SetActive(false);
            resultRoot.gameObject.SetActive(false);
            loadoutRoot.gameObject.SetActive(true);
            RefreshLoadout();
        }

        private void RefreshLoadout()
        {
            if(!loadoutRoot || !loadoutRoot.gameObject.activeSelf) return;
            resonanceText.text=$"MAE — {match.Player.Definition.summonerClass}   |   Resonance {(match.Player.Resonant?"ON":"OFF")}   |   5/6 matching affinity required";

            for(int i=0;i<schoolButtons.Length;i++)
                schoolSkins[i].sprite=(SigilSchool)i==selectedSchool?art.sigilSelected:art.sigilSlot;

            int visible=0;
            var library=match.Library;
            for(int i=0;i<libraryButtons.Length;i++)
            {
                var s=library[i];
                bool show=s && s.school==selectedSchool;
                libraryButtons[i].gameObject.SetActive(show);
                if(!show) continue;
                int col=visible%2,row=visible/2;
                var r=libraryButtons[i].GetComponent<RectTransform>();
                r.anchoredPosition=new Vector2(-330+col*205,35-row*88);
                libraryLabels[i].text=$"{s.displayName}\n{s.form} • {s.classAffinity}";
                visible++;
            }
            emptySchool.gameObject.SetActive(visible==0);
            emptySchool.text=visible==0?"No documented Sigils available yet.":"";

            for(int i=0;i<6;i++)
            {
                var s=match.Player.Loadout.Get(i);
                equippedLabels[i].text=$"{i+1}\n{(s?s.displayName:"EMPTY")}";
                equippedSkins[i].sprite=i==editSlot?art.sigilSelected:art.sigilSlot;
            }

            SigilDefinition detail=null;
            if(selectedLibraryIndex>=0 && selectedLibraryIndex<library.Length) detail=library[selectedLibraryIndex];
            if(!detail)
            {
                for(int i=0;i<library.Length;i++) if(library[i] && library[i].school==selectedSchool){detail=library[i];break;}
            }
            if(detail)
            {
                string uses=detail.unlimitedUses?"Unlimited":detail.maxUses+" uses";
                loadoutDetail.text=$"{detail.displayName}\n\nSchool: {detail.school}\nForm: {detail.form}\nAffinity: {detail.classAffinity}\nCharges: {uses}\n\n{Description(detail.id)}";
            }
            else loadoutDetail.text=$"{selectedSchool}\n\nNo documented Sigils available yet.";
        }

        private string Description(string id)
        {
            switch(id)
            {
                case "origin.aether-bolt": return "Direct Aether projectile. Reliable damage with light terrain cutting.";
                case "starter.lilbomb": return "Ballistic bomb that bounces and rolls after terrain contact before detonating.";
                case "starter.fault": return "Fractures and removes a large section of terrain.";
                case "starter.wall": return "Raises a permanent destructible wall from valid ground.";
                case "starter.summoners-step": return "Short reposition to a valid standing location.";
                case "starter.brace": return "Creates a temporary protective Aether shield.";
                case "m3.mirrorsigil": return "Places a free-floating Seeker plane that redirects the first projectile crossing it.";
                case "m3.rootcaller": return "Launches a living seed that creates permanent destructible root terrain on impact.";
                case "m3.resorecall": return "Embeds a Reso blade, then recalls it on a later turn through its return path.";
                case "m3.bulwarkrise": return "Raises two permanent stone ridges with a deliberate defensive gap.";
                default:return "";
            }
        }

        private Sprite SchoolIcon(SigilSchool school)
        {
            switch(school)
            {
                case SigilSchool.Origin:return art.schoolOrigin;
                case SigilSchool.Aerth:return art.schoolAerth;
                case SigilSchool.Ash:return art.schoolAsh;
                case SigilSchool.Aurora:return art.schoolAurora;
                case SigilSchool.Tempest:return art.schoolTempest;
                case SigilSchool.Lunar:return art.schoolLunar;
                case SigilSchool.XO:return art.schoolXO;
                case SigilSchool.Seeker:return art.schoolSeeker;
                default:return null;
            }
        }

        private void OnStateChanged()
        {
            aim.CancelDrag();
            if(match.Turns.TurnNumber==1 && match.Turns.Phase==TurnPhase.Acting)
            {impactSeconds=0;impact.enabled=false;}
        }

        private void OnImpact(Vector2 point)
        {
            impact.transform.position=point;
            impact.transform.localScale=Vector3.one*.45f;
            impact.enabled=true;
            impactSeconds=.18f;
        }

        private void SetMenuVisibility()
        {
            bool visible=!match.InMenu && match.Turns.Phase!=TurnPhase.Finished;
            matchUI.alpha=visible?1:0;
            matchUI.interactable=visible;
            matchUI.blocksRaycasts=visible;
        }

        private static RectTransform Group(Transform parent,string name)
        {
            var root=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent,false);
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;
            root.offsetMin=root.offsetMax=Vector2.zero;
            return root;
        }

        private static Text Label(Transform parent,int size)
        {
            var text=new GameObject("Text",typeof(RectTransform),typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent,false);
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=size;text.alignment=TextAnchor.MiddleCenter;
            text.color=Color.white;text.raycastTarget=false;
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

        private static Button Button(Transform parent,string text,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var go=new GameObject(text,typeof(RectTransform),typeof(Image),typeof(Button));
            go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),anchor,position,size);
            var image=go.GetComponent<Image>();image.color=new Color(.22f,.37f,.38f);
            var button=go.GetComponent<Button>();
            button.targetGraphic=image;
            button.navigation=new Navigation{mode=Navigation.Mode.None};
            var label=Label(go.transform,26);
            label.text=text;label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
            label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            return button;
        }

        private static void Place(RectTransform rect,Vector2 anchor,Vector2 position,Vector2 size)
        {
            rect.anchorMin=rect.anchorMax=anchor;
            rect.pivot=Vector2.one*.5f;
            rect.anchoredPosition=position;
            rect.sizeDelta=size;
        }

        private void OnDestroy()
        {
            if(match){match.Impact-=OnImpact;match.StateChanged-=OnStateChanged;}
            if(impact) Destroy(impact.gameObject);
        }
    }
}