using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class GameUI : MonoBehaviour
{
    public GameManager Manager { get; private set; }
    public GameAssets Assets => Manager.Assets;
    public RectTransform CanvasTransform { get; private set; }
    public bool ModalOpen => overlay!=null;
    public bool InventoryOpen => overlayKind=="Inventory";
    static readonly Color Ink=GameTypography.Ink, Cream=GameTypography.Paper, Mint=new Color(.68f,.82f,.68f);
    RectTransform bag, sequence, panel, header, info, items, nodes, rightHud, playerBuffs;
    RectTransform[] slots=new RectTransform[6];
    const int BagColumns=7;
    const float BagHeaderHeight=52, BattlePanelHeight=628;
    CardUI[] bagCards=new CardUI[BagColumns], sequenceCards=new CardUI[6];
    readonly Dictionary<int,CardUI> cardViews=new Dictionary<int,CardUI>();
    readonly HashSet<int> flippedBagCards=new HashSet<int>();
    RunState flipState;
    readonly List<GameObject> targetButtons=new List<GameObject>();
    readonly Dictionary<int,RectTransform> enemyIntentIcons=new Dictionary<int,RectTransform>();
    Text turnsValue, focusText, speedText, toast, damageText, interferenceText;
    RectTransform turnPanel, focusBuff, toastPanel;
    readonly List<Image> turnPips=new List<Image>();
    readonly List<CardUI> inventoryCards=new List<CardUI>();
    Button execute, ultimate;
    GameObject overlay;
    GameObject buffTooltip;
    string overlayKind;
    int selected=-1, page;
    int debugDefinition, debugStars=1;
    float toastUntil;
    float goldPulseUntil;
    Transform goldTransform;
    Vector2 lastCanvas;
    Transform F(string name,Transform root=null)
    {
        root=root==null?CanvasTransform:root;
        foreach(var t in root.GetComponentsInChildren<Transform>(true)) if(t.name==name) return t;
        return null;
    }
    T C<T>(string name,Transform root=null) where T:Component => F(name,root)?.GetComponent<T>();
    void Put(string name,string value,Transform root=null) { var t=C<Text>(name,root); if(t!=null) t.text=value; }
    public void Init(GameManager manager)
    {
        Manager=manager;
        CanvasTransform=GameObject.Find("MainCanvas").GetComponent<RectTransform>();
        foreach(var text in CanvasTransform.GetComponentsInChildren<Text>(true))
            GameTypography.Apply(text,Assets,text.name.Contains("Name") || text.name=="ExecuteText" || text.name=="FloorTurnText" || text.name=="GoldText" || text.name=="UltimateText");
        var scaler=CanvasTransform.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        if(EventSystem.current==null)
        {
            var es=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        EventSystem.current.pixelDragThreshold=7;
        bag=C<RectTransform>("Bag"); sequence=C<RectTransform>("Sequence"); panel=C<RectTransform>("Panel");
        header=C<RectTransform>("BagHeader"); info=C<RectTransform>("CardInfoUI"); items=C<RectTransform>("ItemArea"); nodes=C<RectTransform>("StageNodes");
        foreach(Transform t in bag) t.gameObject.SetActive(t.name=="PreviousPageButton" || t.name=="NextPageButton");
        foreach(Transform t in sequence) t.gameObject.SetActive(false);
        foreach(Transform t in header) t.gameObject.SetActive(false);
        var headerBackground=header.GetComponent<Image>();
        if(headerBackground!=null) headerBackground.enabled=false;
        header.SetParent(panel,false); header.anchorMin=header.anchorMax=header.pivot=new Vector2(0,1);
        header.anchoredPosition=new Vector2(20,BagHeaderHeight); header.sizeDelta=new Vector2(832,BagHeaderHeight);
        var inventoryTab=Picture(header,"InventoryTab",0,0,244,52,Assets.HeaderTab); inventoryTab.type=Image.Type.Sliced;
        MakeHeaderButton(inventoryTab.transform,"Inventory",14,214,"Tab  가방 관리",ShowInventory);
        var toolsTab=Picture(header,"ToolsTab",244,0,588,52,Assets.HeaderTab); toolsTab.type=Image.Type.Sliced;
        var bagCount=Label(toolsTab.transform,"BagCount",20,-10,88,36,"",22,Cream);
        GameTypography.Apply(bagCount,Assets,true);
        MakeHeaderButton(toolsTab.transform,"Sort",120,128,"Q 정리",Sort);
        MakeHeaderButton(toolsTab.transform,"Merge",270,134,"E 합성",()=>{Manager.Merge();});
        MakeHeaderButton(toolsTab.transform,"Flip",420,144,"W 뒤집기",FlipBag);
        Hook("PreviousPageButton",()=>Page(-1)); Hook("NextPageButton",()=>Page(1));
        Hook("SettingsButton",TogglePause);
        PreparePanels();
        rightHud=Rect(CanvasTransform,"RightHud",0,0,352,242);
        rightHud.anchorMin=rightHud.anchorMax=rightHud.pivot=new Vector2(1,1); rightHud.anchoredPosition=new Vector2(-24,-20);
        LayoutRightHud();
        var speed=MakeButton(rightHud,"SpeedButton",244,-76,108,44,"×2",Manager.SetSpeed,18);
        speedText=speed.GetComponentInChildren<Text>();
        MakeButton(rightHud,"DebugCardsButton",0,-76,232,44,"F2 카드 추가",ShowDebugCards,18);
        damageText=Label(rightHud,"RunDamage",0,-132,352,36,"누적 피해 0",18,Cream); Outline(damageText);
        interferenceText=Label(rightHud,"Interference",0,-178,352,60,"",18,new Color(1,.71f,.57f)); Outline(interferenceText);
        execute=C<Button>("ExecuteButton"); Hook("ExecuteButton",Manager.Execute);
        ultimate=F("UltimateUI").gameObject.AddComponent<Button>(); ultimate.targetGraphic=C<Image>("UltimateUI");
        ultimate.targetGraphic.raycastTarget=true; ultimate.onClick.AddListener(Manager.Ultimate);
        C<Text>("UltimateText").fontSize=24;
        for(int i=0;i<6;i++)
        {
            int slot=i;
            var b=MakeButton(sequence,"Slot"+(i+1),0,0,224,392,"+",()=>{if(selected>=0) Manager.Equip(selected,slot);},48);
            b.GetComponent<Image>().sprite=Assets.EmptySlot;
            slots[i]=b.GetComponent<RectTransform>();
        }
        turnPanel=StatusPanel(CanvasTransform,"RemainingTurnsUI",30,-106,252,88);
        Label(turnPanel,"Caption",24,-14,196,32,"남은 턴",18,Cream,TextAnchor.MiddleLeft);
        turnsValue=Label(turnPanel,"Value",172,-12,56,40,"",36,Cream);
        for(int i=0;i<8;i++) turnPips.Add(Picture(turnPanel,"TurnPip"+i,24+i*26,-64,20,6,null));
        var buffs=C<RectTransform>("PlayerBuffArea"); buffs.gameObject.SetActive(true);
        foreach(Transform child in buffs) child.gameObject.SetActive(false);
        var buffLayout=buffs.GetComponent<HorizontalLayoutGroup>(); if(buffLayout!=null) buffLayout.enabled=false;
        playerBuffs=buffs; buffs.SetParent(CanvasTransform,false); buffs.anchorMin=buffs.anchorMax=new Vector2(.5f,.5f); buffs.pivot=new Vector2(1,.5f); buffs.sizeDelta=new Vector2(60,84);
        focusBuff=Rect(buffs,"FocusBuff",0,0,60,84);
        var buffHitArea=focusBuff.gameObject.AddComponent<Image>(); buffHitArea.color=Color.clear; buffHitArea.raycastTarget=true;
        var focusIcon=Picture(focusBuff,"Icon",6,-6,48,48,Assets.Focus); focusIcon.preserveAspect=true;
        focusText=Label(focusBuff,"Stacks",0,-56,60,26,"0",18,Mint); GameTypography.Apply(focusText,Assets,true);
        Outline(focusText);
        focusText.resizeTextForBestFit=true; focusText.resizeTextMinSize=12; focusText.resizeTextMaxSize=18;
        var hover=focusBuff.gameObject.AddComponent<EventTrigger>();
        var enter=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter}; enter.callback.AddListener(_=>ShowFocusTooltip()); hover.triggers.Add(enter);
        var exit=new EventTrigger.Entry{eventID=EventTriggerType.PointerExit}; exit.callback.AddListener(_=>HideBuffTooltip()); hover.triggers.Add(exit);
        var toastBackground=Picture(CanvasTransform,"ToastBackground",0,0,800,76,null);
        toastBackground.color=new Color(.06f,.04f,.08f,.94f);
        var toastShadow=toastBackground.gameObject.AddComponent<Shadow>(); toastShadow.effectColor=new Color(0,0,0,.75f); toastShadow.effectDistance=new Vector2(0,-6);
        toastPanel=toastBackground.rectTransform;
        toastPanel.anchorMin=toastPanel.anchorMax=toastPanel.pivot=new Vector2(.5f,1); toastPanel.anchoredPosition=new Vector2(0,-156);
        toast=Label(toastPanel,"Toast",28,-14,744,48,"",27,Cream);
        toast.rectTransform.anchorMin=Vector2.zero; toast.rectTransform.anchorMax=Vector2.one;
        toast.rectTransform.offsetMin=new Vector2(28,14); toast.rectTransform.offsetMax=new Vector2(-28,-14);
        toast.resizeTextForBestFit=true; toast.resizeTextMinSize=18; toast.resizeTextMaxSize=30; Outline(toast);
        toastPanel.gameObject.SetActive(false);
        goldTransform=F("GoldUI");
        Layout();
    }
    RectTransform StatusPanel(Transform parent,string name,float x,float y,float w,float h)
    {
        var image=Picture(parent,name,x,y,w,h,Assets.EmptySlot); image.type=Image.Type.Sliced; return image.rectTransform;
    }
    static void Place(RectTransform rect,Transform parent,float x,float y,float width,float height)
    {
        rect.SetParent(parent,false); rect.localScale=Vector3.one;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,y); rect.sizeDelta=new Vector2(width,height);
    }
    void LayoutRightHud()
    {
        var row=C<RectTransform>("RightInfoUI"); Place(row,rightHud,0,0,352,52);
        var floor=C<RectTransform>("FloorTurnUI"); Place(floor,row,0,0,134,52);
        var floorText=C<Text>("FloorTurnText"); Place(floorText.rectTransform,floor,4,-8,126,36); floorText.fontSize=24;
        var gold=C<RectTransform>("GoldUI"); Place(gold,row,142,0,124,52);
        Place(C<RectTransform>("GoldIcon"),gold,4,-12,28,28);
        var goldText=C<Text>("GoldText"); Place(goldText.rectTransform,gold,42,-8,78,36); goldText.fontSize=24; goldText.alignment=TextAnchor.MiddleLeft;
        goldText.resizeTextForBestFit=true; goldText.resizeTextMinSize=18; goldText.resizeTextMaxSize=24;
        Place(C<RectTransform>("SettingsButton"),row,292,-2,48,48);
        nodes.SetParent(CanvasTransform,false); nodes.anchorMin=nodes.anchorMax=nodes.pivot=new Vector2(.5f,1);
        nodes.anchoredPosition=new Vector2(0,-24); nodes.sizeDelta=new Vector2(420,54);
        foreach(Transform child in nodes) child.gameObject.SetActive(false);
    }
    void ShowFocusTooltip()
    {
        if(ModalOpen || Manager.InMenu || Manager.Run==null) return;
        HideBuffTooltip();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasTransform,RectTransformUtility.WorldToScreenPoint(null,focusBuff.position),null,out var point);
        var rect=StatusPanel(CanvasTransform,"BuffTooltip",0,0,380,196); buffTooltip=rect.gameObject;
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f); rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(Mathf.Clamp(point.x+80,-CanvasTransform.rect.width/2+20,CanvasTransform.rect.width/2-400),Mathf.Clamp(point.y+20,-CanvasTransform.rect.height/2+216,CanvasTransform.rect.height/2-20));
        Label(rect,"Title",26,-20,328,36,"집중 · 캐릭터 특수 버프",24,Mint);
        Label(rect,"Description",26,-68,328,104,"현재 "+GameMath.Number(Manager.Run.State.Focus)+"중첩\n턴 종료 시 모두 사라집니다.\n거합의 피해를 강화하며, 카드마다 소모량이 다릅니다.",18,Cream,TextAnchor.UpperLeft);
    }
    void HideBuffTooltip() { if(buffTooltip!=null) Destroy(buffTooltip); buffTooltip=null; }
    void AttachTooltip(Transform anchor,Func<string> title,Func<string> description)
    {
        var hit=anchor.GetComponent<Graphic>(); if(hit!=null) hit.raycastTarget=true;
        var trigger=anchor.gameObject.AddComponent<EventTrigger>();
        var enter=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};
        enter.callback.AddListener(_=>ShowTooltip(anchor,title(),description())); trigger.triggers.Add(enter);
        var exit=new EventTrigger.Entry{eventID=EventTriggerType.PointerExit}; exit.callback.AddListener(_=>HideBuffTooltip()); trigger.triggers.Add(exit);
    }
    void ShowTooltip(Transform anchor,string title,string description)
    {
        if(ModalOpen || Manager.InMenu) return;
        HideBuffTooltip();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasTransform,RectTransformUtility.WorldToScreenPoint(null,anchor.position),null,out var point);
        var box=StatusPanel(CanvasTransform,"HoverTooltip",0,0,390,204); buffTooltip=box.gameObject;
        box.anchorMin=box.anchorMax=new Vector2(.5f,.5f); box.pivot=new Vector2(0,1);
        box.anchoredPosition=new Vector2(Mathf.Clamp(point.x+28,-CanvasTransform.rect.width/2+16,CanvasTransform.rect.width/2-406),Mathf.Clamp(point.y-38,-CanvasTransform.rect.height/2+220,CanvasTransform.rect.height/2-16));
        Label(box,"Title",24,-18,342,36,title,24,Mint);
        Label(box,"Description",24,-66,342,116,description,18,Cream,TextAnchor.UpperLeft);
    }
    void PreparePanels()
    {
        panel.anchorMin=new Vector2(0,0); panel.anchorMax=new Vector2(1,0); panel.pivot=new Vector2(.5f,0);
        panel.anchoredPosition=Vector2.zero; panel.sizeDelta=new Vector2(0,BattlePanelHeight);
        bag.anchorMin=new Vector2(0,1); bag.anchorMax=new Vector2(1,1); bag.pivot=new Vector2(.5f,1);
        bag.anchoredPosition=new Vector2(-176,-12); bag.sizeDelta=new Vector2(-384,168);
        sequence.anchorMin=new Vector2(0,0); sequence.anchorMax=new Vector2(1,0); sequence.pivot=new Vector2(.5f,0);
        sequence.anchoredPosition=new Vector2(-188,16); sequence.sizeDelta=new Vector2(-408,420);
        var background=panel.GetComponent<Image>(); background.sprite=Assets.EmptySlot; background.type=Image.Type.Sliced;
        var oldFrame=C<Image>("PanelFrame",panel); if(oldFrame!=null) oldFrame.enabled=false;
        foreach(string name in new[]{"PreviousPageButton","NextPageButton"})
        {
            var button=C<Image>(name,bag); var arrow=button.sprite; button.sprite=Assets.EmptySlot; button.type=Image.Type.Sliced;
            var icon=Picture(button.transform,"Arrow",12,-14,28,32,arrow); icon.preserveAspect=true;
        }
        info.sizeDelta=new Vector2(info.sizeDelta.x,336);
        var cost=C<Text>("CostText",info); if(cost!=null) cost.gameObject.SetActive(false);
        var desc=C<Text>("DescriptionText",info); desc.rectTransform.anchoredPosition=new Vector2(28,-118); desc.rectTransform.sizeDelta=new Vector2(280,184);
        foreach(var badgeName in new[]{"FamilyBadge","TypeBadge"})
        {
            var badge=C<RectTransform>(badgeName,info); badge.sizeDelta=new Vector2(100,32);
            var text=badge.GetComponentInChildren<Text>(); Place(text.rectTransform,badge,8,-4,84,24); text.fontSize=18;
        }
        var cardName=C<Text>("NameText",info); cardName.rectTransform.anchoredPosition=new Vector2(104,-22); cardName.rectTransform.sizeDelta=new Vector2(172,36);
        var executeText=C<Text>("ExecuteText"); executeText.rectTransform.anchorMin=Vector2.zero; executeText.rectTransform.anchorMax=Vector2.one;
        executeText.rectTransform.offsetMin=new Vector2(20,20); executeText.rectTransform.offsetMax=new Vector2(-20,-20); executeText.fontSize=42;
        var ultimateLabel=C<Text>("UltimateText"); ultimateLabel.rectTransform.anchoredPosition=new Vector2(24,-20); ultimateLabel.rectTransform.sizeDelta=new Vector2(288,34);
        var fill=C<Image>("UltimateBarFill"); var track=(RectTransform)fill.transform.parent;
        track.anchoredPosition=new Vector2(24,-66); track.sizeDelta=new Vector2(288,22);
        fill.rectTransform.anchorMin=Vector2.zero; fill.rectTransform.anchorMax=Vector2.one; fill.rectTransform.offsetMin=new Vector2(6,5); fill.rectTransform.offsetMax=new Vector2(-6,-5);
    }
    void Hook(string name,Action action)
    {
        var b=C<Button>(name); if(b==null) return;
        var graphic=b.targetGraphic; if(graphic!=null) graphic.raycastTarget=true;
        b.onClick.RemoveAllListeners(); b.onClick.AddListener(()=>action());
    }
    public Sprite Art(CardDefinition d)
    {
        if(Assets.CardArt!=null && d.Id<Assets.CardArt.Length && Assets.CardArt[d.Id]!=null) return Assets.CardArt[d.Id];
        return d.Art==0?Assets.Slash:Assets.ItemIcons[d.Art%6];
    }
    public void Refresh()
    {
        if(Manager.Run==null) return;
        var s=Manager.Run.State;
        if(s.Find(selected)==null) selected=s.Cards.Count>0?s.Cards[0].Uid:-1;
        RefreshCards(); RefreshBattle(); RefreshInfo(); RefreshItems(); RefreshNodes();
        RefreshBagCount();
        speedText.text="×"+Manager.Speed.ToString("0");
        Put("GoldText",s.Gold.ToString());
        Put("FloorTurnText",s.Floor+"층 · "+s.Turn+"턴");
        Put("ExecuteText",Manager.Busy?"진행 중":"실행");
        execute.interactable=!Manager.Busy && s.Phase==RunPhase.Battle;
        ultimate.interactable=!Manager.Busy && s.Phase==RunPhase.Battle && s.Ultimate>=100;
        Put("UltimateText","궁극기  "+s.Ultimate+"/100  [U]");
        var fill=C<Image>("UltimateBarFill"); if(fill!=null) { fill.type=Image.Type.Filled; fill.fillMethod=Image.FillMethod.Horizontal; fill.fillAmount=s.Ultimate/100f; }
        if(!Manager.InMenu && !Manager.Busy)
        {
            if(s.Phase!=RunPhase.Battle && (overlay==null || overlayKind=="Phase")) ShowPhase();
            else if(s.Phase==RunPhase.Battle && overlayKind=="Phase") CloseOverlay();
        }
        if(RunSave.LastError!=null) Toast(RunSave.LastError);
    }
    List<OwnedCard> Ordered() => Manager.Run.BagCards;
    int BagPageCount => Math.Max(1,(Ordered().Count+BagColumns-1)/BagColumns);
    void RefreshBagCount() { Put("BagCount",Manager.Run.State.Cards.Count+"/16",header); }
    void RefreshCards()
    {
        var s=Manager.Run.State; var ordered=Ordered();
        if(flipState!=s) { flipState=s; flippedBagCards.Clear(); }
        flippedBagCards.RemoveWhere(uid=>s.Find(uid)==null);
        page=Mathf.Clamp(page,0,BagPageCount-1);
        var visible=new HashSet<int>();
        for(int i=0;i<BagColumns;i++)
        {
            var c=ordered.Skip(page*BagColumns+i).FirstOrDefault();
            bagCards[i]=c==null?null:CardView(c,bag,-1,true); if(c!=null) visible.Add(c.Uid);
        }
        for(int i=0;i<6;i++)
        {
            var seq=s.Find(s.Sequence[i]);
            slots[i].GetComponent<Image>().enabled=seq==null;
            var slotLabel=slots[i].GetComponentInChildren<Text>(); slotLabel.text=i==s.SealedSlot?"봉인":"+"; slotLabel.fontSize=i==s.SealedSlot?24:48;
            sequenceCards[i]=null;
            if(seq!=null)
            {
                var view=CardView(seq,slots[i],i,false); sequenceCards[i]=view; visible.Add(seq.Uid);
                var rt=view.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f); rt.anchoredPosition=Vector2.zero;
            }
        }
        foreach(var pair in cardViews.ToArray())
        {
            if(s.Find(pair.Key)==null) { Destroy(pair.Value.gameObject); cardViews.Remove(pair.Key); }
            else pair.Value.gameObject.SetActive(visible.Contains(pair.Key));
        }
        Layout();
    }
    CardUI CardView(OwnedCard card,Transform parent,int slot,bool compact)
    {
        if(!cardViews.TryGetValue(card.Uid,out var view)) { view=Instantiate(Assets.CardPrefab,parent); cardViews.Add(card.Uid,view); }
        if(view.transform.parent!=parent) view.transform.SetParent(parent,false);
        view.transform.SetAsLastSibling();
        view.gameObject.SetActive(true); view.Bind(card,this,slot,compact); view.SetSelected(card.Uid==selected);
        view.SetSealed(slot>=0 && slot==Manager.Run.State.SealedSlot);
        if(compact && slot==-1) view.SetBackFace(flippedBagCards.Contains(card.Uid));
        return view;
    }
    public void RefreshBattle()
    {
        var r=Manager.Run; if(r==null) return; var s=r.State;
        damageText.text="누적 피해  "+GameMath.Number(s.TotalDamage);
        interferenceText.text=InterferenceText();
        int remaining=Math.Max(0,r.TurnLimit-s.Turn+1);
        turnsValue.text=remaining.ToString(); turnsValue.color=remaining<=2?new Color(1,.58f,.5f):Cream;
        int activePips=Mathf.CeilToInt(remaining*Math.Min(8,r.TurnLimit)/(float)r.TurnLimit);
        for(int i=0;i<turnPips.Count;i++) { turnPips[i].gameObject.SetActive(i<Math.Min(8,r.TurnLimit)); turnPips[i].color=i<activePips?(remaining<=2?new Color(1,.58f,.5f):Mint):new Color(.3f,.25f,.32f); }
        focusText.text=GameMath.Number(s.Focus);
        foreach(var obj in targetButtons) { obj.SetActive(false); Destroy(obj); } targetButtons.Clear(); enemyIntentIcons.Clear();
        Manager.CurrentStage.SetTarget(s.Target);
        Manager.CurrentStage.SyncHealth();
        for(int i=0;i<s.Enemies.Count;i++)
        {
            if(s.Enemies[i].Health<=0) continue;
            int index=i;
            Vector3 screen=Camera.main.WorldToScreenPoint(Manager.CurrentStage.HeartPosition(i));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasTransform,screen,null,out var local);
            var hit=Picture(CanvasTransform,"Target"+i,0,0,88,88,null); hit.color=Color.clear; hit.raycastTarget=true;
            var b=hit.gameObject.AddComponent<Button>(); b.targetGraphic=hit; b.navigation=new Navigation{mode=Navigation.Mode.None};
            b.onClick.AddListener(()=>{if(!Manager.Busy){s.Target=index;Manager.Commit();}});
            var rt=b.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f); rt.anchoredPosition=local;
            targetButtons.Add(b.gameObject);
            var state=s.Enemies[i];
            var sprite=state.Intent==EnemyIntent.Guard?Assets.Buff:state.Intent==EnemyIntent.DisruptFocus?Assets.Focus:state.Intent==EnemyIntent.Seal?Art(CardCatalog.All[7]):
                state.Intent==EnemyIntent.SiphonUltimate?Assets.ItemIcons[5]:state.Intent==EnemyIntent.Pressure?Assets.ItemIcons[4]:state.Intent==EnemyIntent.Regenerate?Assets.ItemIcons[2]:Assets.Slash;
            var intent=Picture(CanvasTransform,"EnemyIntent"+i,0,0,44,44,sprite); intent.preserveAspect=true;
            var iconRect=intent.rectTransform; iconRect.anchorMin=iconRect.anchorMax=iconRect.pivot=new Vector2(.5f,.5f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasTransform,Camera.main.WorldToScreenPoint(Manager.CurrentStage.BuffPosition(i)),null,out var buffPoint);
            iconRect.anchoredPosition=buffPoint;
            enemyIntentIcons.Add(i,iconRect);
            AttachTooltip(iconRect,()=>"다음 행동 · "+RunEngine.IntentName(s.Enemies[index]),()=>
                (s.Enemies[index].Guarding?"현재 경화: 받는 피해 -40%\n":"")+RunEngine.IntentDescription(s.Enemies[index]));
            if(state.Guarding) intent.color=new Color(.56f,.86f,1);
            targetButtons.Add(intent.gameObject);
        }
    }
    public string InterferenceText()
    {
        if(Manager.Run==null) return "";
        var s=Manager.Run.State; var effects=new List<string>();
        if(s.Weakened) effects.Add("피해 -25%"); if(s.FocusDisrupted) effects.Add("집중 획득 -50%");
        if(s.SealedSlot>=0) effects.Add((s.SealedSlot+1)+"번 칸 봉인");
        if(s.LostTurns>0)effects.Add("기한 -"+s.LostTurns+"턴");
        return effects.Count==0?"방해 효과 없음":"이번 턴 · "+string.Join(" / ",effects);
    }
    void RefreshInfo()
    {
        var c=Manager.Run.State.Find(selected); if(c==null) return;
        foreach(var text in info.GetComponentsInChildren<Text>(true)) text.verticalOverflow=VerticalWrapMode.Overflow;
        Put("NameText",c.Data.Name,info); Put("DescriptionText",c.Data.Description(c.Stars),info);
        Put("FamilyText",c.Data.Iai?"거합":"공통",info); Put("TypeText",c.Data.KindName,info);
        var desc=C<Text>("DescriptionText",info); desc.verticalOverflow=VerticalWrapMode.Truncate; desc.resizeTextForBestFit=true; desc.resizeTextMinSize=18; desc.resizeTextMaxSize=24;
        C<Image>("Artwork",info).sprite=Art(c.Data);
        C<Image>("Artwork",info).color=new Color(1,.96f,.94f);
        C<Text>("DescriptionText",info).color=CardUI.DescriptionColor(c.Data.Rarity);
        var infoImage=info.GetComponent<Image>(); infoImage.color=new[]{Color.white,new Color(.87f,.95f,1),new Color(1,.93f,.74f),new Color(.94f,.86f,1)}[c.Data.Rarity];
        C<Image>("FamilyBadge",info).color=CardUI.FamilyColor(c.Data.Iai);
        C<Image>("TypeBadge",info).color=CardUI.TypeColor(c.Data);
        for(int i=1;i<=4;i++) { C<Image>("Star"+i,info).sprite=Assets.Star; F("Star"+i,info).gameObject.SetActive(i<=c.Stars); }
    }
    void RefreshItems()
    {
        foreach(Transform t in items) { t.gameObject.SetActive(false); Destroy(t.gameObject); }
        var s=Manager.Run.State;
        for(int i=0;i<6;i++) if(s.Items[i]>0)
        {
            int id=i;
            var b=MakeButton(items,"Item"+i,0,0,62,72,"",()=>ShowMessage(ItemCatalog.Names[id],ItemCatalog.Descriptions[id]+"\n\n보유 "+s.Items[id]+"개"),20);
            b.GetComponent<Image>().sprite=Assets.ItemIcons[i]; b.GetComponent<Image>().type=Image.Type.Simple; b.GetComponent<Image>().preserveAspect=true;
            AttachTooltip(b.transform,()=>ItemCatalog.Names[id],()=>ItemCatalog.Descriptions[id]+"\n보유 "+s.Items[id]+"개");
            var label=b.GetComponentInChildren<Text>(); label.text=s.Items[i]>1?s.Items[i].ToString():""; label.alignment=TextAnchor.LowerRight; label.color=Cream; Outline(label);
        }
    }
    void RefreshNodes()
    {
        foreach(Transform t in nodes) { t.gameObject.SetActive(false); Destroy(t.gameObject); }
        float width=nodes.rect.width;
        float start=(width-298)/2;
        for(int i=0;i<4;i++)
        {
            var line=Picture(nodes,"NodeLink"+i,start+36+i*64,-19,34,4,null);
            line.color=i<Manager.Run.State.Node?Mint:new Color(.5f,.4f,.43f);
        }
        for(int i=0;i<5;i++)
        {
            var icon=Picture(nodes,"Route"+i,start+i*64,0,42,42,i==2?Assets.Shop:i==4?Assets.Boss:Assets.Battle);
            icon.color=i<Manager.Run.State.Node?new Color(.55f,.55f,.55f):Color.white;
            if(i==Manager.Run.State.Node) Picture(icon.transform,"Current",-6,6,54,54,Assets.CurrentNode);
        }
        var waveValue=Label(nodes,"CurrentWaveValue",110,-55,200,26,"웨이브 "+(Manager.Run.State.Wave+1)+"/"+(Manager.Run.IsBoss?1:3),18,Cream);
        GameTypography.Apply(waveValue,Assets,true); Outline(waveValue);
    }
    void Layout()
    {
        if(sequence==null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRightPanel();
        Manager.CurrentStage.SetBattleLayout(BattlePanelHeight+BagHeaderHeight-600,CanvasTransform.rect.height);
        float width=sequence.rect.width;
        float cell=(width-32)/6;
        float scale=Math.Min((cell-12)/CardUI.Width,(sequence.rect.height-16)/CardUI.Height);
        var cardSize=new Vector2(CardUI.Width*scale,CardUI.Height*scale);
        for(int i=0;i<6;i++)
        {
            var rt=slots[i]; rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);
            rt.anchoredPosition=new Vector2((i-2.5f)*cell,0); rt.sizeDelta=cardSize;
            if(sequenceCards[i]!=null) sequenceCards[i].SetLayoutScale(scale);
        }
        for(int i=0;i<BagColumns;i++)
        {
            if(bagCards[i]!=null)
            {
                var br=bagCards[i].GetComponent<RectTransform>(); br.anchorMin=br.anchorMax=new Vector2(0,.5f); br.pivot=new Vector2(.5f,.5f);
                br.anchoredPosition=new Vector2(66+(i+.5f)*(bag.rect.width-132)/BagColumns,0);
                bagCards[i].SetLayoutScale(Math.Min((bag.rect.width-150)/BagColumns/CardUI.BagWidth,(bag.rect.height-16)/CardUI.BagHeight));
            }
        }
        var previous=C<RectTransform>("PreviousPageButton",bag); previous.anchorMin=previous.anchorMax=new Vector2(0,.5f); previous.pivot=new Vector2(0,.5f); previous.anchoredPosition=new Vector2(14,0);
        var next=C<RectTransform>("NextPageButton",bag); next.anchorMin=next.anchorMax=new Vector2(1,.5f); next.pivot=new Vector2(1,.5f); next.anchoredPosition=new Vector2(-14,0);
        lastCanvas=CanvasTransform.rect.size;
    }
    void LayoutRightPanel()
    {
        var right=C<RectTransform>("BottomRightUI"); float height=right.rect.height;
        info.sizeDelta=new Vector2(0,height-236);
        Place(C<RectTransform>("Artwork",info),info,28,-24,64,64); C<Image>("Artwork",info).preserveAspect=true;
        var name=C<Text>("NameText",info); Place(name.rectTransform,info,104,-24,128,34);
        name.resizeTextForBestFit=true; name.resizeTextMinSize=16; name.resizeTextMaxSize=26;
        Place(C<RectTransform>("FamilyBadge",info),info,108,-72,92,30);
        Place(C<RectTransform>("TypeBadge",info),info,208,-72,100,30);
        Place(C<RectTransform>("FamilyText",info),F("FamilyBadge",info),6,-3,80,24);
        Place(C<RectTransform>("TypeText",info),F("TypeBadge",info),6,-3,88,24);
        Place(C<RectTransform>("DescriptionText",info),info,28,-124,280,height-384);
        for(int i=1;i<=4;i++)
        {
            var star=C<RectTransform>("Star"+i,info); star.SetParent(info,false);
            star.anchorMin=star.anchorMax=star.pivot=new Vector2(1,1); star.anchoredPosition=new Vector2(-20-(i-1)*20,-24); star.sizeDelta=Vector2.one*18;
        }
        var ult=C<RectTransform>("UltimateUI"); ult.anchoredPosition=new Vector2(0,116); ult.sizeDelta=new Vector2(0,104);
        var run=C<RectTransform>("ExecuteButton"); run.anchoredPosition=Vector2.zero; run.sizeDelta=new Vector2(0,100);
    }
    public void Page(int delta) { if(Manager.Run==null || Manager.Busy) return; page=(page+delta+BagPageCount)%BagPageCount; RefreshCards(); RefreshBagCount(); }
    public void FlipCard(int uid)
    {
        if(Manager.Run==null || Manager.Busy || Manager.InMenu || ModalOpen) return;
        if(!Ordered().Any(c=>c.Uid==uid)) return;
        SetBagFace(uid,!flippedBagCards.Contains(uid));
    }
    public void FlipBag()
    {
        if(Manager.Run==null || Manager.Busy || Manager.InMenu || ModalOpen) return;
        var cards=Ordered(); if(cards.Count==0) return;
        bool back=cards.Any(c=>!flippedBagCards.Contains(c.Uid));
        foreach(var card in cards) SetBagFace(card.Uid,back);
    }
    void SetBagFace(int uid,bool back)
    {
        if(back) flippedBagCards.Add(uid); else flippedBagCards.Remove(uid);
        if(cardViews.TryGetValue(uid,out var view)) view.SetBackFace(back,view.gameObject.activeInHierarchy);
    }
    public void Sort()
    {
        if(Manager.Run==null || Manager.Busy) return;
        Manager.Run.State.Cards=Manager.Run.State.Cards.OrderByDescending(c=>c.Data.Rarity).ThenBy(c=>c.Definition).ThenByDescending(c=>c.Stars).ToList();
        page=0; Manager.Commit(); if(InventoryOpen) ShowInventory();
    }
    public void Select(int uid) { selected=uid; RefreshInfo(); foreach(var v in bagCards) if(v!=null) v.SetSelected(v.Card.Uid==uid); foreach(var v in sequenceCards) if(v!=null) v.SetSelected(v.Card.Uid==uid); }
    public void QuickEquip(int uid)
    {
        int slot=-1;
        for(int i=0;i<6;i++) if(Manager.Run.State.Sequence[i]==-1 && i!=Manager.Run.State.SealedSlot) { slot=i; break; }
        if(slot>=0) Manager.Equip(uid,slot); else Toast("사용 가능한 칸이 없습니다. 카드를 끌어 교체하세요.");
    }
    CardUI DropCard(int uid,Vector2 screen)
    {
        var candidates=InventoryOpen?inventoryCards:bagCards.Concat(sequenceCards).ToList();
        return candidates.FirstOrDefault(c=>c!=null && c.gameObject.activeInHierarchy && c.Card.Uid!=uid && RectTransformUtility.RectangleContainsScreenPoint(c.GetComponent<RectTransform>(),screen,null));
    }
    public void ClearDropPreview()
    {
        foreach(var card in cardViews.Values.Concat(inventoryCards)) if(card!=null) { card.SetMergePreview(0); card.SetSelected(card.Card.Uid==selected); }
    }
    public void PreviewDrop(int uid,Vector2 screen)
    {
        ClearDropPreview();
        var target=DropCard(uid,screen);
        if(target!=null && Manager.Run.CanMerge(uid,target.Card.Uid)) { target.SetMergePreview(target.Card.Stars+1); target.SetSelected(true); }
    }
    public void Drop(int uid,int fromSlot,Vector2 screen)
    {
        var mergeTarget=DropCard(uid,screen);
        if(mergeTarget!=null && Manager.Run.CanMerge(uid,mergeTarget.Card.Uid)) { Manager.MergeCards(uid,mergeTarget.Card.Uid); return; }
        if(InventoryOpen)
        {
            var target=inventoryCards.FirstOrDefault(c=>c!=null && c.Card.Uid!=uid && RectTransformUtility.RectangleContainsScreenPoint(c.GetComponent<RectTransform>(),screen,null));
            if(target!=null && Manager.Run.MoveCard(uid,target.Card.Uid)) { Manager.Commit(); ShowInventory(); }
            return;
        }
        for(int i=0;i<6;i++) if(RectTransformUtility.RectangleContainsScreenPoint(slots[i],screen,null)) { Manager.Equip(uid,i); return; }
        if(RectTransformUtility.RectangleContainsScreenPoint(bag,screen,null))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(bag,screen,null,out var point);
            float x=point.x-bag.rect.xMin-66;
            float cell=(bag.rect.width-132)/BagColumns;
            if(x>=0 && x<cell*BagColumns) Manager.PlaceInBag(uid,page*BagColumns+Mathf.FloorToInt(x/cell));
        }
    }
    public void ToggleInventory() { if(Manager.Busy) return; if(InventoryOpen) CloseInventory(); else if(!ModalOpen) ShowInventory(); }
    void CloseInventory() { page=0; CloseOverlay(); Refresh(); }
    public void BeginCombat() { bag.gameObject.SetActive(false); header.gameObject.SetActive(false); execute.interactable=false; }
    public void EndCombat() { bag.gameObject.SetActive(true); header.gameObject.SetActive(true); Highlight(-1); }
    public void Highlight(int slot) { for(int i=0;i<6;i++) if(sequenceCards[i]!=null) {sequenceCards[i].SetSelected(i==slot);if(i==slot)sequenceCards[i].PlayPulse();} }
    public void AfterSale()
    {
        goldPulseUntil=Time.unscaledTime+.4f;
        if(overlayKind=="Inventory") ShowInventory();
    }
    public void Toast(string message)
    {
        toast.text=message; toastUntil=Time.unscaledTime+4;
        toastPanel.sizeDelta=new Vector2(Mathf.Clamp(toast.preferredWidth+64,340,1280),76);
        toastPanel.gameObject.SetActive(true); toastPanel.SetAsLastSibling();
    }
    void LateUpdate()
    {
        if(playerBuffs==null || Manager.CurrentStage==null || Camera.main==null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasTransform,Camera.main.WorldToScreenPoint(Manager.CurrentStage.PlayerBuffAnchor),null,out var point);
        playerBuffs.anchoredPosition=point;
        foreach(var pair in enemyIntentIcons) if(pair.Value!=null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasTransform,Camera.main.WorldToScreenPoint(Manager.CurrentStage.BuffPosition(pair.Key)),null,out var buffPoint);
            pair.Value.anchoredPosition=buffPoint;
        }
    }
    void Update()
    {
        if(CanvasTransform==null) return;
        Shader.SetGlobalFloat("_CardTime",Time.unscaledTime);
        if(speedText!=null) speedText.text="×"+Manager.Speed.ToString("0");
        if(toastPanel.gameObject.activeSelf && Time.unscaledTime>toastUntil) { toast.text=""; toastPanel.gameObject.SetActive(false); }
        if(lastCanvas!=CanvasTransform.rect.size) { Layout(); if(Manager.Run!=null) RefreshBattle(); }
        if(execute!=null && Manager.Run!=null)
        {
            float pulse=!Manager.InMenu && !Manager.Busy && Manager.Run.CanExecute?1+.012f*Mathf.Sin(Time.unscaledTime*4):1;
            execute.transform.localScale=Vector3.one*pulse;
            ultimate.transform.localScale=Vector3.one*(Manager.Run.State.Ultimate>=100?1+.015f*Mathf.Sin(Time.unscaledTime*5):1);
        }
        if(goldTransform!=null) goldTransform.localScale=Vector3.one*(1+Mathf.Max(0,goldPulseUntil-Time.unscaledTime)*.2f);
    }
    public void Floating(Vector3 world,string value,Color color,int size)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(CanvasTransform,Camera.main.WorldToScreenPoint(world),null,out var local);
        var t=Label(CanvasTransform,"CombatText",0,0,700,90,value,size,color); Outline(t);
        t.rectTransform.anchorMin=t.rectTransform.anchorMax=t.rectTransform.pivot=new Vector2(.5f,.5f); t.rectTransform.anchoredPosition=local;
        StartCoroutine(FloatText(t));
    }
    IEnumerator FloatText(Text t)
    {
        var start=t.rectTransform.anchoredPosition;
        for(float time=0;time<.9f;time+=Time.deltaTime)
        {
            if(t==null) yield break;
            t.rectTransform.anchoredPosition=start+Vector2.up*(time*70);
            t.transform.localScale=Vector3.one*(1+Mathf.Max(0,.18f-time)*1.2f);
            var c=t.color; c.a=1-time/.9f; t.color=c; yield return null;
        }
        if(t!=null) Destroy(t.gameObject);
    }
    public void CloseOverlay()
    {
        if(InventoryOpen) page=0;
        HideBuffTooltip();
        if(overlay!=null) { overlay.SetActive(false); Destroy(overlay); }
        overlay=null; overlayKind=null;
    }
    RectTransform Overlay(string kind,float width,float height)
    {
        CloseOverlay(); overlayKind=kind;
        var shade=Picture(CanvasTransform,"ModalOverlay",0,0,0,0,null);
        var rt=shade.rectTransform; rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero;
        shade.color=new Color(.06f,.035f,.08f,.86f); shade.raycastTarget=true; overlay=shade.gameObject;
        var content=Picture(rt,"Window",0,0,width,height,Assets.Panel);
        content.type=Image.Type.Sliced; content.color=Color.white; content.raycastTarget=true;
        var r=content.rectTransform; r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=Vector2.zero;
        if(toastPanel!=null && toastPanel.gameObject.activeSelf) toastPanel.SetAsLastSibling();
        return r;
    }
    public void ShowTitle()
    {
        var w=Overlay("Title",960,660);
        Label(w,"Title",70,-72,820,100,"프로토타입",60,Ink);
        MakeButton(w,"NewRun",110,-240,350,76,"게임 시작",()=>{var s=RunSave.Read(); if(s!=null && s.Phase!=RunPhase.Victory && s.Phase!=RunPhase.Defeat) Confirm("진행 중인 탐험을 새 탐험으로 바꿀까요?",Manager.NewRun); else Manager.NewRun();},30);
        var saved=RunSave.Read();
        var cont=MakeButton(w,"Continue",500,-240,350,76,"이어하기",Manager.ContinueRun,30);
        cont.interactable=saved!=null && saved.Phase!=RunPhase.Victory && saved.Phase!=RunPhase.Defeat;
        MakeButton(w,"Help",110,-394,220,64,"방법",()=>ShowHelp(false),24);
        MakeButton(w,"Settings",370,-394,220,64,"설정",ShowSettings,24);
        MakeButton(w,"Quit",630,-394,220,64,"종료",()=>Application.Quit(),24);
    }
    public void ShowHelp(bool first)
    {
        var w=Overlay("Help",1080,760);
        Label(w,"Title",50,-38,980,60,"숨을 고르고, 순서를 만든다",38,Ink);
        Label(w,"Instructions",85,-126,910,460,
            "1   집중 → 강화 → 거합 순으로 시퀀스를 편성하세요.\n\n"+
            "2   가방 카드를 드래그하거나 두 번 눌러 장착합니다.\n     가방 우클릭은 설명 뒷면, 시퀀스 우클릭은 장착 해제입니다.\n\n"+
            "3   코스트 없이 6칸을 편성합니다. 집중은 턴 종료 시 사라집니다.\n     적의 다음 행동을 보고 약화·집중 방해·봉인에 대비하세요.\n\n"+
            "4   제한 턴 안에 적을 전부 처치하세요. 하트를 눌러 대상을 고릅니다.\n     플레이어 체력은 없고, 시간이 끝나면 탐험이 끝납니다.\n\n"+
            "5   같은 카드·같은 성급 2장을 합성하면 최대 4성이 됩니다.\n     웨이브마다 카드, 스테이지마다 아이템을 얻습니다.",25,Ink,TextAnchor.UpperLeft);
        Label(w,"Keys",70,-610,940,36,"Space 실행 · Tab 가방 · A/D 페이지 · W 뒤집기 · Q 정리 · E 합성 · U 궁극기 · F2 카드",18,Ink);
        MakeButton(w,"Start",340,-675,400,56,first?"첫 번째 수업 시작":"알겠어",()=>{PlayerPrefs.SetInt("ReadIntro",1);PlayerPrefs.Save();CloseOverlay();if(Manager.InMenu)ShowTitle();else Refresh();},26);
    }
    public void TogglePause()
    {
        if(Manager.InMenu) { ShowTitle(); return; }
        if(overlayKind=="Pause" || overlayKind=="Settings" || overlayKind=="Help") { CloseOverlay(); Refresh(); return; }
        if(ModalOpen) { if(overlayKind=="Phase") ShowPause(); else {CloseOverlay();Refresh();} return; }
        ShowPause();
    }
    void ShowPause()
    {
        var w=Overlay("Pause",640,600);
        Label(w,"Title",30,-50,580,70,"잠깐 숨 고르기",38,Ink);
        MakeButton(w,"Resume",120,-160,400,60,"계속하기",()=>{CloseOverlay();Refresh();},27);
        MakeButton(w,"Help",120,-245,400,60,"플레이 방법",()=>ShowHelp(false),27);
        MakeButton(w,"Settings",120,-330,400,60,"설정",ShowSettings,27);
        MakeButton(w,"TitleScreen",120,-415,400,60,"저장하고 시작 화면으로",Manager.ReturnToTitle,27);
        Label(w,"AutoSave",40,-520,560,35,"편성·보상·턴 종료 때 자동 저장됩니다.",21,Ink);
    }
    void ShowSettings()
    {
        var w=Overlay("Settings",720,560);
        Label(w,"Title",40,-45,640,65,"설정",40,Ink);
        MakeButton(w,"Volume",100,-160,520,65,"사운드  "+(Manager.Audio.Muted?"끔":"켬"),()=>{Manager.Audio.ToggleMute();ShowSettings();},27);
        MakeButton(w,"BattleSpeed",100,-255,520,65,"전투 속도  ×"+Manager.Speed,()=>{Manager.SetSpeed();ShowSettings();},27);
        MakeButton(w,"Fullscreen",100,-350,520,65,"전체 화면  "+(Screen.fullScreen?"켬":"끔"),()=>{Screen.fullScreen=!Screen.fullScreen;},27);
        MakeButton(w,"Back",220,-450,280,58,"돌아가기",()=>{CloseOverlay();if(Manager.InMenu)ShowTitle();else ShowPause();},25);
    }
    public void ShowMessage(string title,string message)
    {
        var w=Overlay("Message",780,470);
        Label(w,"Title",45,-45,690,70,title,36,Ink);
        Label(w,"Message",80,-140,620,190,message,27,Ink);
        MakeButton(w,"Back",240,-365,300,60,"확인",()=>{CloseOverlay();Refresh();},26);
    }
    void Confirm(string message,Action yes)
    {
        var w=Overlay("Confirm",800,360);
        Label(w,"Question",65,-55,670,130,message,30,Ink);
        MakeButton(w,"Yes",100,-235,260,60,"확인",()=>{CloseOverlay();yes();},27);
        MakeButton(w,"No",440,-235,260,60,"취소",()=>{CloseOverlay();if(Manager.InMenu)ShowTitle();else Refresh();},27);
    }
    void ShowPhase()
    {
        var s=Manager.Run.State;
        if(s.Phase==RunPhase.Victory || s.Phase==RunPhase.Defeat) { ShowEnding(); return; }
        if(s.Phase==RunPhase.Shop) { ShowShop(); return; }
        bool item=s.Phase==RunPhase.ItemReward;
        var w=Overlay("Phase",1360,790);
        Label(w,"Title",40,-28,1280,65,item?"한 걸음 더 깊이":"웨이브 돌파",40,Ink);
        Label(w,"Subtitle",60,-103,1240,40,item?"탐험 내내 함께할 아이템을 고르세요.":"카드 "+Manager.Run.RewardCopies+"장을 선택하세요. 표시된 성급으로 획득합니다.",24,Ink);
        for(int i=0;i<3;i++)
        {
            int index=i; int id=s.Offers[i];
            if(item)
            {
                var p=Picture(w,"ItemChoice"+i,80+i*410,-175,380,420,Assets.EmptySlot); p.type=Image.Type.Sliced;
                Picture(p.transform,"Art",126,-30,128,128,Assets.ItemIcons[id]);
                Label(p.transform,"ItemName",20,-180,340,50,ItemCatalog.Names[id],30,Cream);
                Label(p.transform,"ItemDescription",30,-250,320,120,ItemCatalog.Descriptions[id],25,Cream);
                MakeButton(w,"Pick"+i,100+i*410,-625,340,60,"가져가기",()=>Manager.ChooseItem(index),25);
            }
            else
            {
                var card=Instantiate(Assets.CardPrefab,w); card.gameObject.SetActive(true);
                card.Bind(new OwnedCard(-100-i,id,s.OfferStars[i]),this,-2,false,()=>Manager.ChooseCard(index));
                PlaceCard(card,w,205+i*410,-175,.82f);
                MakeButton(w,"Pick"+i,140+i*410,-625,340,60,Manager.Run.RewardCopies+"장 획득",()=>Manager.ChooseCard(index),25);
            }
        }
        if(!item)
        {
            MakeButton(w,"Skip",870,-715,360,48,"카드 대신 골드 +"+Manager.Run.RewardGold,()=>Manager.ChooseCard(-1),23);
            MakeButton(w,"Manage",100,-715,360,48,"가방 "+s.Cards.Count+"/16 · 합성 / 판매",ShowInventory,23);
        }
    }
    void ShowShop()
    {
        var s=Manager.Run.State; var w=Overlay("Phase",1360,820);
        Label(w,"Title",40,-25,1280,65,"틈새의 상점     "+s.Gold+" 골드",39,Ink);
        Label(w,"Subtitle",50,-100,1260,40,"카드 구매 · 판매 · 카드 1장 성급 강화",24,Ink);
        for(int i=0;i<3;i++)
        {
            int index=i; var card=Instantiate(Assets.CardPrefab,w); card.gameObject.SetActive(true);
            card.Bind(new OwnedCard(-100-i,s.Offers[i],s.OfferStars[i]),this,-2,false,()=>{});
            PlaceCard(card,w,205+i*410,-175,.82f);
            var buy=MakeButton(w,"Buy"+i,140+i*410,-625,340,60,s.Bought[i]?"구매 완료":Manager.Run.ShopPrice(i)+" 골드 · 구매",()=>{if(Manager.Run.Buy(index)){Manager.Audio.Play(GameSound.Coin);Manager.Commit();}else Toast("골드나 가방 공간이 부족합니다.");},25);
            buy.interactable=!s.Bought[i];
        }
        MakeButton(w,"Manage",100,-735,400,55,"가방 관리 / 성급 강화",ShowInventory,25);
        MakeButton(w,"Leave",860,-735,400,55,"다음 스테이지로",Manager.LeaveShop,25);
        var reroll=MakeButton(w,"Reroll",530,-735,300,55,"리롤  "+Manager.Run.RerollPrice+" 골드",Manager.RerollShop,24);
        reroll.interactable=s.Gold>=Manager.Run.RerollPrice;
    }
    public void ShowInventory()
    {
        if(Manager.Run==null || Manager.Busy) return;
        var s=Manager.Run.State;
        var bagOrder=Ordered();
        var managedOrder=bagOrder.Concat(s.Cards.Where(c=>Array.IndexOf(s.Sequence,c.Uid)>=0)).ToList();
        if(!managedOrder.Any(c=>c.Uid==selected)) selected=managedOrder.Count>0?managedOrder[0].Uid:-1;
        var w=Overlay("Inventory",1500,900); inventoryCards.Clear();
        Label(w,"Title",40,-25,1420,60,"가방 관리   "+s.Cards.Count+" / 16",36,Ink);
        var inventoryGold=Rect(w,"InventoryGold",1240,-32,200,44);
        var goldIcon=Picture(inventoryGold,"Icon",0,-4,36,36,Assets.Gold); goldIcon.preserveAspect=true;
        var goldValue=Label(inventoryGold,"Value",48,0,152,44,s.Gold.ToString(),30,Ink,TextAnchor.MiddleLeft);
        goldValue.resizeTextForBestFit=true; goldValue.resizeTextMinSize=18; goldValue.resizeTextMaxSize=30;
        Label(w,"ReorderHint",64,-86,1080,30,"전투 가방 순서 · 장착 카드는 뒤쪽에 표시 · 드래그 정렬/합성 · 우클릭 판매",18,Ink,TextAnchor.MiddleLeft);
        int n=0;
        foreach(var c in managedOrder)
        {
            var card=c;
            var view=Instantiate(Assets.CardPrefab,w); view.gameObject.SetActive(true);
            view.Bind(card,this,-2,true,()=>{selected=card.Uid;ShowInventory();}); view.SetSelected(card.Uid==selected);
            PlaceCard(view,w,72+(n%5)*180,-136-(n/5)*160,.68f); inventoryCards.Add(view);
            if(Array.IndexOf(s.Sequence,card.Uid)>=0)
            {
                var tag=Picture(view.transform,"EquippedTag",12,-150,200,30,null); tag.color=new Color(.08f,.06f,.11f,.92f);
                Label(tag.transform,"Caption",4,-3,192,24,"시퀀스 장착",18,Cream);
            }
            Label(w,"Name"+n,62+(n%5)*180,-270-(n/5)*160,174,24,card.Data.Name,18,Ink);
            n++;
        }
        var selectedCard=s.Find(selected);
        var details=Picture(w,"CardDescriptionPanel",990,-118,450,636,Assets.EmptySlot); details.type=Image.Type.Sliced;
        if(selectedCard!=null)
        {
            Label(details.transform,"CardName",28,-24,394,52,selectedCard.Data.Name,30,Cream);
            Label(details.transform,"Stars",28,-90,394,36,new string('★',selectedCard.Stars),24,new Color(1,.86f,.57f));
            Label(details.transform,"Description",30,-156,390,230,selectedCard.Data.Description(selectedCard.Stars),24,Cream,TextAnchor.UpperLeft);
            Label(details.transform,"CardStats",30,-394,390,38,new[]{"회색","하늘색","금색","프리즘"}[selectedCard.Data.Rarity]+" · "+selectedCard.Stars+"성",18,Cream);
            MakeButton(w,"Sell",1030,-550,370,58,"판매  +"+Manager.Run.SellPrice(selectedCard)+" 골드",()=>Manager.Sell(selectedCard.Uid),25);
            if(s.Phase==RunPhase.Shop)
            {
                var upgrade=MakeButton(w,"Upgrade",1030,-635,370,58,s.ShopMerged?"이번 상점 강화 완료":"성급 +1  ·  "+(40+selectedCard.Stars*20)+" 골드",()=>{if(Manager.Run.BuyUpgrade(selectedCard.Uid)){Manager.Commit();ShowInventory();}else Toast("골드가 부족하거나 이미 강화했습니다.");},24);
                upgrade.interactable=!s.ShopMerged && selectedCard.Stars<4;
            }
        }
        MakeButton(w,"Merge",90,-805,380,58,"자동 합성",()=>{Manager.Merge();ShowInventory();},26);
        MakeButton(w,"Sort",510,-805,310,58,"Q 정리",Sort,24);
        MakeButton(w,"Back",1030,-805,370,58,"돌아가기",CloseInventory,26);
    }
    public void ShowDebugCards()
    {
        if(Manager.InMenu || Manager.Run==null || Manager.Busy) return;
        var w=Overlay("DebugCards",1540,920);
        Label(w,"Title",40,-28,1460,60,"디버그 · 카드 추가",36,Ink);
        Label(w,"Hint",50,-90,1420,32,"카드 선택 → 성급 설정 → 가방에 추가",18,Ink);
        for(int i=0;i<CardCatalog.All.Length;i++)
        {
            int id=i; var d=CardCatalog.All[i];
            var button=MakeButton(w,"CardChoice"+i,44+(i%5)*184,-150-(i/5)*88,172,76,"",()=>{debugDefinition=id;ShowDebugCards();},18);
            CardUI.ApplyFrame(button.GetComponent<Image>(),Assets,d.Rarity,true);
            var art=Picture(button.transform,"Art",12,-16,48,48,Art(d)); art.preserveAspect=true;
            Label(button.transform,"CardName",66,-12,94,58,d.Name,18,id==debugDefinition?Mint:Cream);
            if(id==debugDefinition)
            {
                var selectedBorder=Picture(button.transform,"Selected",0,0,172,84,Assets.SelectionCorners);
                selectedBorder.type=Image.Type.Sliced; selectedBorder.color=Color.white;
            }
        }
        var detail=StatusPanel(w,"DebugCardDetails",994,-150,490,594);
        var definition=CardCatalog.All[debugDefinition];
        var icon=Picture(detail,"Art",30,-28,86,86,Art(definition)); icon.preserveAspect=true;
        Label(detail,"CardName",130,-30,330,72,definition.Name,30,Cream);
        Label(detail,"Description",30,-142,430,176,definition.Description(debugStars),24,Cream,TextAnchor.UpperLeft);
        Label(detail,"StarCaption",30,-340,150,46,"성급",24,Cream,TextAnchor.MiddleLeft);
        MakeButton(detail,"StarMinus",194,-340,56,46,"−",()=>{debugStars=Math.Max(1,debugStars-1);ShowDebugCards();},24);
        Label(detail,"StarValue",258,-340,118,46,debugStars+"성",24,Cream);
        MakeButton(detail,"StarPlus",388,-340,56,46,"+",()=>{debugStars=Math.Min(4,debugStars+1);ShowDebugCards();},24);
        MakeButton(detail,"AddToBag",30,-510,430,58,"가방에 추가",()=>{Manager.AddDebugCard(debugDefinition,debugStars);Put("Capacity","가방 "+Manager.Run.State.Cards.Count+" / 16",w);},24);
        MakeButton(w,"Back",1124,-810,360,60,"돌아가기",()=>{CloseOverlay();Refresh();},24);
        Label(w,"Capacity",54,-814,820,52,"가방 "+Manager.Run.State.Cards.Count+" / 16",24,Ink,TextAnchor.MiddleLeft);
    }
    void ShowEnding()
    {
        var s=Manager.Run.State; bool win=s.Phase==RunPhase.Victory;
        if(!win)
        {
            var gameOver=Overlay("Phase",800,410);
            Label(gameOver,"Title",50,-48,700,90,"게임 오버",48,Ink);
            MakeButton(gameOver,"Retry",75,-190,300,68,"다시하기",Manager.NewRun,30);
            MakeButton(gameOver,"Main",425,-190,300,68,"메인으로",Manager.ReturnToTitle,30);
            MakeButton(gameOver,"Info",275,-295,250,58,"정보",()=>ShowMessage("진행 정보",
                "돌파 웨이브   "+s.WavesCleared+" / 30\n최대 단일 타격   "+GameMath.Number(s.BestHit)+
                "\n누적 피해량   "+GameMath.Number(s.TotalDamage)+"\n실행한 카드   "+s.CardsPlayed+"장"),24);
            return;
        }
        var w=Overlay("Phase",1050,750);
        Label(w,"Title",50,-55,950,90,win?"공허를 베다":"아직, 닿지 못한 일섬",48,Ink);
        Label(w,"Story",100,-170,850,110,win?"마지막 균열이 닫히고, 중앙 홀에 아침빛이 스민다.\n오늘의 수업은 여기까지다.":"공허가 닫히기 전에 빠져나왔다.\n순서를 바꾸면, 다음 일격은 달라질 것이다.",27,Ink);
        Label(w,"Stats",130,-325,790,200,"돌파한 웨이브   "+s.WavesCleared+" / 30\n최대 단일 타격   "+GameMath.Number(s.BestHit)+"\n누적 피해량   "+GameMath.Number(s.TotalDamage)+"\n실행한 카드   "+s.CardsPlayed+"장",29,Ink);
        MakeButton(w,"Retry",140,-585,350,65,"다시 공허로",Manager.NewRun,27);
        MakeButton(w,"Hall",560,-585,350,65,"중앙 홀로",Manager.ReturnToTitle,27);
    }
    static void PlaceCard(CardUI card,Transform parent,float x,float y,float scale)
    {
        var rt=card.GetComponent<RectTransform>(); rt.anchorMin=rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(0,1); rt.anchoredPosition=new Vector2(x,y); card.SetLayoutScale(scale);
    }
    RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
    {
        var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent,false); rt.gameObject.layer=5;
        rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1); rt.anchoredPosition=new Vector2(x,y); rt.sizeDelta=new Vector2(width,height); return rt;
    }
    Image Picture(Transform parent,string name,float x,float y,float w,float h,Sprite sprite)
    {
        var rt=Rect(parent,name,x,y,w,h); var im=rt.gameObject.AddComponent<Image>(); im.sprite=sprite; im.raycastTarget=false; return im;
    }
    Text Label(Transform parent,string name,float x,float y,float w,float h,string value,int size,Color color,TextAnchor align=TextAnchor.MiddleCenter)
    {
        var rt=Rect(parent,name,x,y,w,h); var t=rt.gameObject.AddComponent<Text>(); t.font=Assets.Font; t.fontSize=size; t.color=color; t.text=value; t.alignment=align; t.raycastTarget=false;
        t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Overflow;
        GameTypography.Apply(t,Assets,size>=30 || name.Contains("Name") || name=="Title");
        return t;
    }
    Button MakeButton(Transform parent,string name,float x,float y,float w,float h,string value,Action clicked,int size)
    {
        var im=Picture(parent,name,x,y,w,h,Assets.EmptySlot); im.type=Image.Type.Sliced; im.raycastTarget=true;
        var b=im.gameObject.AddComponent<Button>(); b.targetGraphic=im;
        var colors=b.colors; colors.highlightedColor=new Color(1,.94f,.83f); colors.pressedColor=new Color(.86f,.78f,.82f); b.colors=colors;
        b.navigation=new Navigation{mode=Navigation.Mode.None};
        b.onClick.AddListener(()=>{Manager.Audio.Play(GameSound.Click);clicked();});
        var t=Label(im.transform,"Label",16,-8,w-32,h-16,value,size,Cream);
        t.rectTransform.anchorMin=Vector2.zero; t.rectTransform.anchorMax=Vector2.one;
        t.rectTransform.offsetMin=new Vector2(16,8); t.rectTransform.offsetMax=new Vector2(-16,-8);
        t.verticalOverflow=VerticalWrapMode.Truncate;
        GameTypography.Apply(t,Assets,true); return b;
    }
    Button MakeHeaderButton(Transform parent,string name,float x,float width,string value,Action clicked)
    {
        var rt=Rect(parent,name,x,-10,width,36);
        var label=Label(rt,"Label",8,-2,width-16,32,value,21,Cream);
        label.verticalOverflow=VerticalWrapMode.Truncate;
        GameTypography.Apply(label,Assets,true); label.raycastTarget=true; Outline(label);
        var button=rt.gameObject.AddComponent<Button>(); button.targetGraphic=label;
        button.navigation=new Navigation{mode=Navigation.Mode.None};
        var colors=button.colors; colors.highlightedColor=new Color(1,.8f,.45f); colors.pressedColor=Mint;
        button.colors=colors;
        button.onClick.AddListener(()=>{Manager.Audio.Play(GameSound.Click);clicked();});
        return button;
    }
    void Outline(Text t) { var o=t.gameObject.AddComponent<Outline>(); o.effectColor=new Color(.12f,.08f,.14f,.9f); o.effectDistance=new Vector2(1,-1); }
}
