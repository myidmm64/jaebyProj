using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using WizardCombat;

public sealed partial class StageCombatController
{
    RectTransform sequencePanel,bagPanel;
    CanvasGroup bagGroup;
    Text statusLine;
    float bagProgress;
    int completedCards;
    bool settingsOpen,rewardInfoOpen,costumeOpen;
    readonly System.Collections.Generic.List<(RectTransform bar,Transform unit,float height)> healthBars=new System.Collections.Generic.List<(RectTransform,Transform,float)>();
    readonly Color paper=new Color(.91f,.91f,.85f),muted=new Color(.55f,.61f,.61f),gold=new Color(.95f,.75f,.3f);
    void LateUpdate()
    {
        if(worldCamera==null||worldOverlay==null)return;
        foreach(var entry in healthBars)
        {
            if(entry.unit==null||entry.bar==null)continue;
            var screen=worldCamera.WorldToScreenPoint(entry.unit.position+Vector3.up*entry.height);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(worldOverlay,screen,null,out var point);
            entry.bar.anchoredPosition=point;
        }
    }
    void DrawUnitHealth(GridObject unit,int hp,int maxHP,Color color)
    {
        var bar=Box(worldOverlay,"Unit health",0,0,64,20,dark);bar.pivot=new Vector2(.5f,0);
        bar.GetComponent<Image>().raycastTarget=false;
        var fill=Box(bar,"Health fill",2,2,60*Mathf.Clamp01(hp/(float)Mathf.Max(1,maxHP)),16,color);fill.GetComponent<Image>().raycastTarget=false;
        var number=Text(bar,Mathf.Max(0,hp).ToString(),2,0,60,20,14);number.alignment=TextAnchor.MiddleCenter;number.color=Color.white;
        float height=stage.CellSize*.6f;
        foreach(var renderer in unit.GetComponentsInChildren<Renderer>())if(renderer.enabled)height=Mathf.Max(height,renderer.bounds.max.y-unit.transform.position.y+stage.CellSize*.12f);
        healthBars.Add((bar,unit.transform,height));
    }
    void AnimateHUD()
    {
        if(root==null)return;
        bagProgress=Mathf.MoveTowards(bagProgress,bagOpen?1:0,Time.unscaledDeltaTime*5);
        PositionDrawer();
    }
    void PositionDrawer(bool applyEdits=true)
    {
        float y=Mathf.Lerp(root.rect.height-242,root.rect.height-487,Mathf.SmoothStep(0,1,bagProgress));
        float x=(root.rect.width-925)*.5f;
        if(sequencePanel!=null)sequencePanel.anchoredPosition=new Vector2(x,-y);
        if(statusLine!=null)statusLine.rectTransform.anchoredPosition=new Vector2(x+5,-(y-30));
        if(bagPanel!=null){bagPanel.anchoredPosition=new Vector2(x,-(y+240));bagPanel.gameObject.SetActive(bagProgress>.001f);}
        if(bagGroup!=null){bagGroup.alpha=bagProgress;bagGroup.blocksRaycasts=bagOpen&&bagProgress>.8f;bagGroup.interactable=CanEdit;}
        if(applyEdits)
        {
            if(sequencePanel!=null)sequencePanel.GetComponent<CombatUINode>().ApplyPositionEdit();
            if(bagPanel!=null)bagPanel.GetComponent<CombatUINode>().ApplyPositionEdit();
            if(statusLine!=null)statusLine.GetComponent<CombatUINode>().ApplyPositionEdit();
        }
    }
    RectTransform Surface(Transform parent,string name,float x,float y,float w,float h)
    {
        var r=Box(parent,name,x,y,w,h,panel);
        Box(r,"Top rule",0,0,w,2,muted).GetComponent<Image>().raycastTarget=false;
        return r;
    }
    RectTransform TransparentHUD(Transform parent,string name,float x,float y,float w,float h)
    {
        var r=Box(parent,name,x,y,w,h,Color.clear);r.GetComponent<Image>().raycastTarget=false;return r;
    }
    void SmallTitle(Transform parent,string text,float x,float y,float width=200)
    {var label=Text(parent,text,x,y,width,25,14);label.color=muted;}
    void Fill(Transform parent,float x,float y,float w,float h,float value,Color color)
    {Box(parent,"Gauge background",x,y,w,h,dark);Box(parent,"Gauge fill",x,y,w*Mathf.Clamp01(value),h,color).GetComponent<Image>().raycastTarget=false;}
    RectTransform RoundButton(Transform parent,string label,float x,float y,float w,float h,Action action,bool enabled=true)
    {
        var r=Button(parent,label,x,y,w,h,action,enabled);r.GetComponent<Image>().enabled=false;
        var go=UINode(r,"Disc",typeof(RectTransform),typeof(CanvasRenderer),typeof(HudDisc));var shape=go.GetComponent<RectTransform>();shape.anchorMin=Vector2.zero;shape.anchorMax=Vector2.one;shape.offsetMin=shape.offsetMax=Vector2.zero;shape.SetAsFirstSibling();
        var disc=go.GetComponent<HudDisc>();disc.color=enabled?orange:panel;r.GetComponent<Button>().targetGraphic=disc;
        return r;
    }
    void DrawHUD()
    {
        healthBars.Clear();
        worldOverlay=Box(view,"World overlay",0,0,root.rect.width,root.rect.height,Color.clear);worldOverlay.GetComponent<Image>().raycastTarget=false;
        float w=root.rect.width,h=root.rect.height;
        var main=TransparentHUD(view,"Concept HUD",0,0,w,h);
        var wave=TransparentHUD(main,"Wave and turn",12,12,315,56);
        Text(wave,$"WAVE {battle.Wave:00}   /   TURN {battle.Turn:00}",16,15,290,30,21);
        var coins=TransparentHUD(main,"Gold",w-360,12,180,56);Text(coins,$"●  {battle.Gold:000} GOLD",15,16,160,30,19);
        RoundButton(main,"보상",w-156,12,56,56,()=>{rewardInfoOpen=!rewardInfoOpen;settingsOpen=false;});
        RoundButton(main,"설정",w-80,12,56,56,()=>{settingsOpen=!settingsOpen;rewardInfoOpen=false;},!busy);
        foreach(var e in battle.Enemies.Where(e=>e.HP>0))
        {
            foreach(var p in e.Intent)Marker(p,new Color(1,.12f,.15f,.2f),"",18);
        }
        foreach(var p in busy?attackArea:selected!=null?battle.Area(selected):new System.Collections.Generic.List<Vector2Int>())Marker(p,new Color(1,.55f,.1f,.4f),"",24);
        if(choosingMove)foreach(var p in battle.Reachable()){var dest=p;Marker(p,Color.yellow,"",11,()=>moveDestination=dest);}
        DrawUnitHealth(stage.Player,battle.HP,60,new Color(.2f,.65f,.38f));
        var living=battle.Enemies.Where(e=>e.HP>0).ToArray();
        for(int i=0;i<living.Length&&i<actors.Count;i++)DrawUnitHealth(actors[i],living[i].HP,living[i].MaxHP,new Color(.8f,.24f,.25f));
        var stats=TransparentHUD(main,"Player stats",12,100,175,355);SmallTitle(stats,"현재 스탯",14,14);
        Text(stats,$"체력\n{Mathf.Max(0,battle.HP)} / 60",14,52,150,60,22);Fill(stats,14,118,145,9,battle.HP/60f,new Color(.75f,.23f,.24f));
        Text(stats,$"마나 충전\n{battle.Mana} / 12",14,151,150,60,22);Fill(stats,14,218,145,9,battle.Mana/12f,blue);
        Text(stats,$"보유 카드   {battle.Owned.Count}\n장착 카드   {battle.Slots.Count(c=>c!=null)} / {battle.SequenceCapacity}\n코스튬 성급   {battle.ActiveStar}",14,254,150,85,16);
        var portrait=Surface(main,"Portrait",12,h-270,175,178);
        Text(portrait,"플레이어",14,10,145,24,16);
        var head=UINode(portrait,"Face",typeof(RectTransform),typeof(CanvasRenderer),typeof(HudDisc));
        var hr=head.GetComponent<RectTransform>();hr.anchorMin=hr.anchorMax=hr.pivot=new Vector2(0,1);hr.anchoredPosition=new Vector2(50,-42);hr.sizeDelta=new Vector2(75,75);head.GetComponent<HudDisc>().color=paper;
        Box(portrait,"Eye left",69,64,5,10,dark);Box(portrait,"Eye right",97,64,5,10,dark);Box(portrait,"Mouth",78,91,12,3,dark);
        Box(portrait,"Body",53,124,70,27,battle.ActiveStar>0?orange:blue);
        Button(main,battle.ActiveStar>0?$"폭렬술사 ★{battle.ActiveStar}":"기본 코스튬",12,h-85,175,32,()=>costumeOpen=!costumeOpen,CanEdit);
        var ultimate=Surface(main,"Ultimate",196,h-270,36,178);
        Box(ultimate,"Ultimate fill",4,174-170*battle.Gauge/100f,28,170*battle.Gauge/100f,gold).GetComponent<Image>().raycastTarget=false;
        Button(main,$"궁극기 {battle.Gauge}%",12,h-50,220,40,()=>{battle.Ultimate();bagOpen=true;page=Math.Max(0,(Stored().Count-1)/6);},CanEdit&&battle.Gauge>=100&&battle.ActiveStar>0);
        var items=TransparentHUD(main,"Owned items",w-202,100,190,240);SmallTitle(items,"보유 아이템",12,14);
        Text(items,"획득 아이템 없음",12,55,165,30,16);
        Text(items,"아이템 획득 시\n이 영역에 순서대로 표시",12,166,166,58,14).color=muted;
        SmallTitle(main,"시퀀스 시작",w-197,h-280,180);
        RoundButton(main,choosingMove?"이동 선택 중":busy?"실행 중":"시퀀스\n시작",w-170,h-244,150,150,()=>StartCoroutine(AttackSequence()),CanEdit);
        var progress=Surface(main,"Sequence progress",w-202,h-76,190,64);
        Text(progress,$"현재 시퀀스  {completedCards}/{battle.Slots.Count(c=>c!=null)}",10,7,175,23,14);
        for(int i=0;i<6;i++)Box(progress,"Step "+i,10+i*29,36,24,16,i<completedCards?gold:busy&&i==activeSlot?orange:dark);
        if(choosingMove)Button(main,"이동 건너뛰기",w-202,h-325,190,35,()=>moveDestination=battle.Player);
        sequencePanel=Surface(main,"Sequence",270,620,925,230);
        SmallTitle(sequencePanel,$"카드 시퀀스  →  {battle.SequenceCapacity} / 6칸",12,7,300);
        var bagButton=Button(sequencePanel,"가방 [Tab] / 드롭 보관",650,4,260,28,()=>bagOpen=!bagOpen,!busy);AddDrop(bagButton,-1,null);
        var preview=battle.Preview();
        for(int i=0;i<6;i++)
        {
            int index=i;bool locked=i>=battle.SequenceCapacity;var c=locked?null:battle.Slots[i];string caption=locked?$"{i+1}턴에 개방":c==null?$"{i+1} 빈 슬롯":busy&&activeSlot==i?"● 실행 중":Rules.Movement(c.Id)?$"{i+1} · 이동":preview[i]<0?$"{i+1} · 이동 후 결정":$"{i+1} · 예상 {preview[i]}";
            sequence[i]=DrawCard(sequencePanel,c,12+i*152,36,142,188,caption,()=>selected=c,locked);if(!locked)AddDrop(sequence[i],index,c);
        }
        bagPanel=Surface(main,"Owned bag",270,860,925,245);AddDrop(bagPanel,-1,null);
        bagGroup=Ensure<CanvasGroup>(bagPanel.gameObject);
        SmallTitle(bagPanel,"가방  /  Q · E 페이지 이동",12,7,305);
        Button(bagPanel,"정렬",330,4,100,28,()=>{battle.SortCards();page=0;},CanEdit&&dragCard==null);
        Button(bagPanel,"자동 합성",440,4,140,28,()=>{battle.AutoMerge();selected=null;page=0;},CanEdit&&dragCard==null);
        var stored=Stored();int pages=Mathf.Max(1,(stored.Count+5)/6);page=Mathf.Clamp(page,0,pages-1);
        Button(bagPanel,"◀ Q",650,4,64,28,()=>ChangeBagPage(-1),CanEdit&&page>0);Text(bagPanel,$"{page+1}/{pages}",737,9,65,24,14);Button(bagPanel,"E ▶",846,4,64,28,()=>ChangeBagPage(1),CanEdit&&page<pages-1);
        int n=0;foreach(var c in stored.Skip(page*6).Take(6)){var card=c;var r=DrawCard(bagPanel,c,12+n*152,36,142,188,"보관 중",()=>selected=card);AddDrop(r,-1,c);n++;}
        if(stored.Count==0)Text(bagPanel,"보관 중인 카드가 없습니다. 시퀀스 카드를 이곳에 놓으면 보관됩니다.",25,100,875,60,20);
        statusLine=Text(main,choosingMove?"노란 점을 선택하세요. 이동 후 다음 카드가 실행됩니다.":string.Join("  /  ",battle.Log.Skip(Math.Max(0,battle.Log.Count-1))),275,590,915,25,15);statusLine.color=paper;
        PositionDrawer(false);
        if(rewardInfoOpen)
        {var info=Surface(main,"Wave reward info",w-312,80,285,175);Text(info,"카드 수급: 2턴마다 1장\n웨이브 클리어: 추가 1장\n클리어 직후 턴 수급 생략\n폭렬 코스튬 ★1 · 골드 10",15,15,260,145,18);}
        if(settingsOpen)
        {
            var settings=Surface(main,"Settings",w-312,80,300,190);Text(settings,"설정",15,12,270,35,23);
            Button(settings,AudioListener.volume>0?"사운드 끄기":"사운드 켜기",15,58,270,36,()=>AudioListener.volume=AudioListener.volume>0?0:1);
            Button(settings,"새 런 시작",15,104,270,36,()=>{settingsOpen=false;ResetRun();},!busy);
            Button(settings,"닫기",15,148,270,30,()=>settingsOpen=false);
        }
        if(costumeOpen)
        {
            var costume=Surface(main,"Costumes",12,h-520,350,240);Text(costume,"코스튬 / 무기",15,14,320,34,22);
            Text(costume,battle.ActiveStar>0?$"잿불 지팡이 ★{battle.ActiveStar}\n첫 폭렬 충전에 마나 +{battle.ActiveStar}\n궁극기: 대폭렬":"기본 외형 · 전용 무기 없음",15,58,320,85,18);
            Button(costume,"코스튬 교체",15,158,155,34,()=>{battle.Equip(battle.Costumes.Count==0?-1:(battle.Equipped+2)%(battle.Costumes.Count+1)-1);SyncActors();},CanEdit);
            Button(costume,"성급 강화",180,158,155,34,()=>battle.MergeCostume(),CanEdit);Button(costume,"닫기",15,201,320,30,()=>costumeOpen=false);
        }
        if(battle.Phase==BattlePhase.Reward||battle.Phase==BattlePhase.TurnReward)
        {
            bool turnReward=battle.Phase==BattlePhase.TurnReward;
            Box(main,"Reward backdrop",0,0,w,h,new Color(0,0,0,.65f));
            var reward=Surface(main,"Rewards",(w-800)*.5f,(h-360)*.5f,800,360);
            Text(reward,turnReward?$"{battle.Turn}턴 시작 · 카드 3장 중 1장 선택":"웨이브 승리 · 추가 카드 1장 선택",20,12,760,40,24);
            for(int i=0;i<3;i++){int id=i;var offered=battle.Rewards[i];DrawCard(reward,offered,20+i*258,66,245,240,"선택하여 획득",()=>{
                if(battle.Phase!=(turnReward?BattlePhase.TurnReward:BattlePhase.Reward)||battle.Rewards.Count<=id||battle.Rewards[id]!=offered)return;
                if(turnReward){battle.ChooseTurnCard(id);selected=offered;bagOpen=true;page=Math.Max(0,(Stored().Count-1)/6);}
                else {battle.Reward(id);selected=null;bagOpen=false;SyncActors();}
            });}
            Text(reward,turnReward?"선택한 카드는 가방에 추가됩니다. 턴 카드는 2턴마다 획득합니다.":"보상 획득 후 다음 웨이브를 시작합니다. 직후 턴 카드 선택은 생략합니다.",20,323,760,26,16);
        }
        if(battle.Phase==BattlePhase.Defeat){var over=Surface(main,"Defeat",w-512,80,500,200);Text(over,"패배",20,20,450,50,30);Button(over,"새 런 시작",20,110,460,50,ResetRun,!busy);}
    }
}
