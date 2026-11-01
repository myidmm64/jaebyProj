using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using WizardCombat;

/// <summary>Stage scene presentation. Battle rules are shared with the damage preview.</summary>
public sealed partial class StageCombatController : MonoBehaviour
{
    Stage stage;
    Camera worldCamera;
    Font font;
    Canvas canvas;
    [SerializeField] Canvas savedCanvas;
    RectTransform root,view,worldOverlay;
    readonly RectTransform[] sequence=new RectTransform[6];
    readonly List<Enemy> actors=new List<Enemy>();
    Battle battle;
    Card selected,dragCard;
    int activeSlot=-1,page;
    bool bagOpen,busy;
    bool choosingMove;
    Vector2Int? moveDestination;
    readonly List<CardDropTarget> dropTargets=new List<CardDropTarget>();
    readonly Dictionary<Image,Color> dropColors=new Dictionary<Image,Color>();
    Action pendingDrop;
    List<Vector2Int> attackArea=new List<Vector2Int>();
    readonly Color dark=new Color(.065f,.08f,.105f),panel=new Color(.12f,.15f,.19f),orange=new Color(.9f,.35f,.1f),blue=new Color(.15f,.5f,.7f);
    public Battle State=>battle;
    public bool CanEdit=>!busy&&battle!=null&&battle.Phase==BattlePhase.Setup;
    void Start()
    {
        stage=GetComponent<Stage>();worldCamera=Camera.main;
        if(!stage.IsInitialized||worldCamera==null){Debug.LogError("Stage combat requires GameManager initialization and Main Camera.");enabled=false;return;}
        InitializeCanvas();
        foreach(var old in FindObjectsByType<BagPanelUI>(FindObjectsInactive.Include,FindObjectsSortMode.None))old.gameObject.SetActive(false);
        actors.Clear();actors.Add(stage.Enemy);
        bagOpen=false;bagProgress=0;settingsOpen=false;rewardInfoOpen=false;costumeOpen=false;
        ResetRun();
    }
    void InitializeCanvas()
    {
        font=Resources.Load<Font>("UI/NanumGothic");
        if(font==null)font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",16);
        canvas=savedCanvas;
        if(canvas==null)
        {
        var go=new GameObject("Combat Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);
        canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;canvas.pixelPerfect=true;
        var scale=go.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1440,900);scale.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        savedCanvas=canvas;
        }
        root=canvas.GetComponent<RectTransform>();RegisterSavedUI();
    }
    void Update()
    {
        AnimateHUD();
        if(battle!=null&&!busy&&dragCard==null&&Keyboard.current!=null&&Keyboard.current.tabKey.wasPressedThisFrame){bagOpen=!bagOpen;Refresh();}
        if(battle!=null&&CanEdit&&bagOpen&&dragCard==null&&Keyboard.current!=null)
        {
            if(Keyboard.current.qKey.wasPressedThisFrame)ChangeBagPage(-1);
            else if(Keyboard.current.eKey.wasPressedThisFrame)ChangeBagPage(1);
        }
    }
    public void ResetRun()
    {
        if(busy)return;
        battle=new Battle(stage.MapSize);
        for(int x=0;x<battle.Size;x++)for(int y=0;y<battle.Size;y++)battle.Enabled[x,y]=stage.IsCellEnabled(new Vector2Int(x,y));
        battle.StartWave();selected=null;page=0;completedCards=0;SyncActors();Refresh();
    }
    RectTransform Box(Transform parent,string name,float x,float y,float w,float h,Color color)
    {
        var go=UINode(parent,name,typeof(RectTransform),typeof(Image));var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);go.GetComponent<Image>().color=color;return r;
    }
    Text Text(Transform parent,string value,float x,float y,float w,float h,int size=16)
    {
        var go=UINode(parent,"Text",typeof(RectTransform),typeof(Text));var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
        var text=go.GetComponent<Text>();
        text.font=font;text.fontSize=size*2;text.resizeTextForBestFit=false;text.supportRichText=false;
        // Render glyphs at twice the resolution without changing the logical HUD layout.
        r.sizeDelta=new Vector2(w*2,h*2);r.localScale=Vector3.one*.5f;
        text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;
        text.text=value;text.color=Color.white;text.raycastTarget=false;return text;
    }
    RectTransform Button(Transform parent,string label,float x,float y,float w,float h,Action action,bool enabled=true)
    {
        var r=Box(parent,label,x,y,w,h,blue);var b=Ensure<Button>(r.gameObject);b.onClick.RemoveAllListeners();b.interactable=enabled;b.onClick.AddListener(()=>{action();Refresh();});var t=Text(r,label,4,3,w-8,h-6,16);t.alignment=TextAnchor.MiddleCenter;return r;
    }
    Vector2 ScreenCell(Vector2Int p)
    {
        var screen=worldCamera.WorldToScreenPoint(stage.GetCellCenterWorld(p));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var local);
        return new Vector2(local.x+root.rect.width*.5f,root.rect.height*.5f-local.y);
    }
    void Marker(Vector2Int p,Color color,string label,float size,Action click=null)
    {
        if(!battle.Valid(p))return;var at=ScreenCell(p);var r=Box(worldOverlay,"Cell marker",at.x-size/2,at.y-size/2,size,size,color);
        r.GetComponent<Image>().raycastTarget=click!=null;
        var existingButton=r.GetComponent<Button>();if(existingButton!=null){existingButton.onClick.RemoveAllListeners();existingButton.interactable=click!=null;}
        if(click!=null){r.sizeDelta=new Vector2(36,36);r.anchoredPosition=new Vector2(at.x-18,-at.y+18);r.GetComponent<Image>().color=Color.clear;var dot=Box(r,"Yellow dot",12.5f,12.5f,11,11,color);dot.GetComponent<Image>().raycastTarget=false;var b=Ensure<Button>(r.gameObject);b.interactable=true;b.onClick.AddListener(()=>click());}
        if(!string.IsNullOrEmpty(label)){var t=Text(r,label,-15,-19,size+30,20,13);t.alignment=TextAnchor.MiddleCenter;}
    }
    public void Refresh()
    {
        if(canvas==null||battle==null)return;
        uiCounts.Clear();uiUsed.Clear();
        dropTargets.Clear();dropColors.Clear();Canvas.ForceUpdateCanvases();
        view=Box(root,"Battle HUD",0,0,root.rect.width,root.rect.height,Color.clear);view.GetComponent<Image>().raycastTarget=false;
        DrawHUD();FinishUIUpdate();
    }
    List<Card> Stored()=>battle.Owned.Where(c=>!battle.Slots.Contains(c)).ToList();
    public void ChangeBagPage(int direction)
    {
        if(!CanEdit||!bagOpen||dragCard!=null)return;
        int next=Mathf.Clamp(page+direction,0,Mathf.Max(0,(Stored().Count-1)/6));
        if(next==page)return;page=next;Refresh();
    }
    RectTransform DrawCard(Transform parent,Card c,float x,float y,float w,float h,string caption,Action click,bool locked=false)
    {
        var r=Box(parent,"Card",x,y,w,h,c==selected?new Color(.32f,.25f,.13f):dark);
        var button=Ensure<Button>(r.gameObject);button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>{if(busy)return;click();Refresh();});
        button.interactable=!locked;
        bool compact=w<180;
        Text(r,caption,8,5,w-16,22,compact?12:14);
        if(c==null){var oldDrag=r.GetComponent<CardDragHandle>();if(oldDrag!=null)oldDrag.Card=null;Text(r,locked?"잠긴 슬롯":"드롭하여 장착",12,80,w-24,45,22);return r;}
        Text(r,Rules.Names[c.Id]+" "+new string('★',c.Star),8,compact?29:32,w-16,26,compact?15:19);
        Text(r,(c.Id>=4&&!Rules.Movement(c.Id)?"폭렬":"공통")+" / "+(Rules.Movement(c.Id)?"이동":Rules.Support(c.Id)?"서포트":"공격"),8,compact?57:65,w-16,23,compact?12:14);
        Text(r,Rules.EffectText(c),8,compact?83:96,w-16,compact?61:63,compact?12:15);
        Text(r,c.Id==11?"이번 턴\n1회용":c.Life<0?"무제한":$"{c.Life}턴",8,h-34,w-58,30,compact?12:14);
        var pattern=Rules.Pattern(c.Id,c.Star);int count=7,center=count/2;float pitch=5;
        for(int row=0;row<count;row++)for(int col=0;col<count;col++)
        {var p=new Vector2Int(col-center,center-row);var cell=Box(r,"Range",w-43+col*pitch,h-40+row*pitch,pitch-1,pitch-1,p==Vector2Int.zero?blue:pattern.Contains(p)?orange:panel);cell.GetComponent<Image>().raycastTarget=false;}
        var drag=Ensure<CardDragHandle>(r.gameObject);drag.Owner=this;drag.Card=c;
        return r;
    }
    void AddDrop(RectTransform r,int slot,Card target){var d=Ensure<CardDropTarget>(r.gameObject);d.Owner=this;d.Slot=slot;d.Target=target;dropTargets.Add(d);dropColors[r.GetComponent<Image>()]=r.GetComponent<Image>().color;}
    CardDropTarget NearestDrop(Vector2 screen)
    {
        CardDropTarget best=null;float distance=48*48;
        foreach(var d in dropTargets.AsEnumerable().Reverse())
        {
            if(d==null||!d.gameObject.activeInHierarchy||d.Target==dragCard)continue;
            var rect=(RectTransform)d.transform;RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,screen,null,out var p);
            var box=rect.rect;float dx=Mathf.Max(box.xMin-p.x,0,p.x-box.xMax),dy=Mathf.Max(box.yMin-p.y,0,p.y-box.yMax);float sq=dx*dx+dy*dy;
            if(sq<distance){distance=sq;best=d;}
        }return best;
    }
    public void HoverDrop(Vector2 screen)
    {
        foreach(var pair in dropColors)if(pair.Key!=null)pair.Key.color=pair.Value;
        var d=NearestDrop(screen);if(d!=null){var image=d.GetComponent<Image>();image.color=Color.Lerp(dropColors[image],Color.yellow,.35f);}
    }
    public void CompleteDrop(Vector2 screen){if(pendingDrop==null){var d=NearestDrop(screen);if(d!=null)QueueDrop(d.Slot,d.Target);}EndDrag();}
    public void CancelDrag(){pendingDrop=null;dragCard=null;}
    public void BeginDrag(Card card){dragCard=CanEdit?card:null;pendingDrop=null;}
    public void QueueDrop(int slot,Card target)
    {
        if(!CanEdit||dragCard==null||slot>=battle.SequenceCapacity)return;var card=dragCard;
        pendingDrop=()=>{if(target!=null&&target!=card&&target.Id==card.Id&&target.Star==card.Star&&card.Star<3&&card.Id!=11)selected=battle.Merge(target,card);
            else if(slot>=0)battle.Place(card,slot);else for(int i=0;i<6;i++)if(battle.Slots[i]==card)battle.Slots[i]=null;};
    }
    public void EndDrag(){pendingDrop?.Invoke();pendingDrop=null;dragCard=null;Refresh();}
    void SyncActors()
    {
        stage.Player.SetCell(battle.Player);
        var alive=battle.Enemies.Where(e=>e.HP>0).ToArray();
        while(actors.Count<alive.Length){var actor=Instantiate(stage.Enemy,stage.transform);actor.name="Enemy "+actors.Count;actors.Add(actor);}
        for(int i=0;i<actors.Count;i++){actors[i].gameObject.SetActive(i<alive.Length);if(i<alive.Length)actors[i].Init(stage,alive[i].Cell);}
    }
    IEnumerator MoveTo(Vector2Int target)
    {
        if(!battle.Reachable().Contains(target))yield break;
        var path=battle.PathTo(target);battle.Move(target);Refresh();
        foreach(var step in path){var start=stage.Player.transform.position;var end=stage.GetCellCenterWorld(step);
        for(float t=0;t<1;t+=Time.deltaTime/.14f){stage.Player.transform.position=Vector3.Lerp(start,end,Mathf.SmoothStep(0,1,t));yield return null;}stage.Player.transform.position=end;}
        stage.Player.SetCell(target);Refresh();
    }
    IEnumerator AttackSequence()
    {
        if(!CanEdit)yield break;
        battle.BeginMove();busy=true;bagOpen=false;completedCards=0;var cards=battle.BeginAttack();
        foreach(var card in cards)
        {
            if(battle.Enemies.All(e=>e.HP<=0))break;
            activeSlot=Array.IndexOf(battle.Slots,card);attackArea=battle.Area(card);Refresh();
            var r=activeSlot>=0?sequence[activeSlot]:null;
            var restScale=r!=null?r.localScale:Vector3.one;
            for(float t=0;t<1;t+=Time.deltaTime/.18f){if(r!=null)r.localScale=restScale*(1+.07f*Mathf.Sin(t*Mathf.PI));yield return null;}
            if(Rules.Movement(card.Id))
            {
                battle.Moves=Rules.MoveRange(card);choosingMove=true;moveDestination=null;attackArea.Clear();Refresh();
                if(battle.Reachable().Count==0)moveDestination=battle.Player;
                while(!moveDestination.HasValue)yield return null;
                choosingMove=false;if(moveDestination.Value!=battle.Player)yield return MoveTo(moveDestination.Value);
                battle.Say("이동 카드 완료");
            }
            else battle.Execute(card);
            completedCards++;SyncActors();Refresh();yield return new WaitForSeconds(.22f);
        }
        battle.FinishAttack(cards);selected=null;activeSlot=-1;attackArea.Clear();SyncActors();busy=false;Refresh();
    }
}
