using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class CardUI : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public const float Width=288, Height=520, BagWidth=224, BagHeight=192;
    public OwnedCard Card { get; private set; }
    public int Slot { get; private set; }
    GameUI ui;
    Action clicked;
    GameObject ghost;
    Vector3 ghostStart;
    bool dragged, dragBack;
    CanvasGroup group;
    bool hovered;
    float layoutScale=1, pulseTime=-10;
    Vector2 layoutPosition;
    Image selection;
    Image artwork, stateBackground;
    Text stateText;
    bool sealedCard;
    string sealedReason="이번 턴 봉인";
    bool compactView, showingBack, flipTarget, flipChanged;
    float flipStarted=-10;
    Image cardBack;
    Text backName, backDescription;
    Text slotBonusLabel;
    static readonly System.Collections.Generic.Dictionary<Sprite,Rect> artBounds=new System.Collections.Generic.Dictionary<Sprite,Rect>();
    static string L(string text,params object[] args) => GameLocalization.Text(text,args);
    T Find<T>(string child) where T:Component
    {
        foreach(var t in GetComponentsInChildren<Transform>(true)) if(t.name==child) return t.GetComponent<T>();
        return null;
    }
    public void Bind(OwnedCard card,GameUI owner,int slot,bool compact,Action click=null)
    {
        Card=card; ui=owner; Slot=slot; clicked=click;
        compactView=compact;
        if(!compact || slot!=-1) { showingBack=false; flipTarget=false; flipChanged=true; flipStarted=-10; }
        var a=ui.Assets; var d=card.Data;
        foreach(var text in GetComponentsInChildren<Text>(true))
        {
            GameTypography.Apply(text,a,text.name!="DescriptionText" && text.name!="BackDescription");
            if(text.color.r<.5f && text.color.g<.5f) text.color=GameTypography.Ink;
        }
        ApplyFrame(GetComponent<Image>(),a,d.Rarity,compact);
        GetComponent<RectTransform>().sizeDelta=new Vector2(compact?BagWidth:Width,compact?BagHeight:Height);
        foreach(string part in new[]{"NameText","FamilyBadge","TypeBadge","DescriptionText"})
        {
            var t=Find<RectTransform>(part); if(t!=null) t.gameObject.SetActive(!compact);
        }
        foreach(var img in GetComponentsInChildren<Image>(true)) img.raycastTarget=false;
        GetComponent<Image>().raycastTarget=true;
        SetText("NameText",d.Name); SetText("DescriptionText",d.Description(card.Stars));
        var oldCost=Find<RectTransform>("CostBadge"); if(oldCost!=null) oldCost.gameObject.SetActive(false);
        SetText("FamilyText",L(d.Iai?"거합":"공통")); SetText("TypeText",d.KindName);
        var desc=Find<Text>("DescriptionText"); if(desc!=null) { desc.verticalOverflow=VerticalWrapMode.Truncate; desc.lineSpacing=1.1f; desc.resizeTextForBestFit=true; desc.resizeTextMinSize=18; desc.resizeTextMaxSize=24; }
        var nameText=Find<Text>("NameText"); if(nameText!=null) { nameText.horizontalOverflow=HorizontalWrapMode.Wrap; nameText.verticalOverflow=VerticalWrapMode.Truncate; nameText.resizeTextForBestFit=true; nameText.resizeTextMinSize=16; nameText.resizeTextMaxSize=24; }
        foreach(string part in new[]{"FamilyText","TypeText"})
        {
            var label=Find<Text>(part); if(label==null)continue;
            label.horizontalOverflow=HorizontalWrapMode.Wrap; label.verticalOverflow=VerticalWrapMode.Truncate;
            label.resizeTextForBestFit=true; label.resizeTextMinSize=14; label.resizeTextMaxSize=18;
        }
        artwork=Find<Image>("Artwork"); artwork.sprite=ui.Art(d); artwork.preserveAspect=true; artwork.color=Color.white;
        LayoutCard(compact);
        PrepareBack(); RefreshFace();
        foreach(string part in new[]{"NameText","DescriptionText"}) { var text=Find<Text>(part); if(text!=null) text.color=DescriptionColor(d.Rarity); }
        for(int i=1;i<=4;i++) { var s=Find<Image>("Star"+i); if(s!=null) { s.sprite=a.Star; s.gameObject.SetActive(i<=card.Stars); } }
        var family=Find<Image>("FamilyBadge"); if(family!=null) family.color=FamilyColor(d.Iai);
        var type=Find<Image>("TypeBadge"); if(type!=null) type.color=TypeColor(d);
        var damage=Find<Image>("DamageBadge"); if(damage!=null)
        {
            damage.gameObject.SetActive(!compact && d.IsAttack && !(ui.Manager.Run!=null && (ui.Manager.Run.IsCardSealed(card.Uid) || (slot>=0 && slot==ui.Manager.Run.State.SealedSlot))));
            double expected=slot>=-1 && ui.Manager.Run!=null && ui.Manager.Run.State.Phase==RunPhase.Battle
                ?ui.Manager.Run.Preview(card):d.Value(card.Stars)*d.Hits;
            if(expected<=0)damage.gameObject.SetActive(false);
            bool average=ui.Manager.Run!=null && ui.Manager.Run.HasChanceBefore(card);
            SetText("DamageText",(average?"~":"")+GameMath.Number(expected));
        }
        var border=Find<Image>("SelectionBorder");
        selection=border;
        if(border!=null)
        {
            border.sprite=a.SelectionCorners!=null?a.SelectionCorners:a.Frame; border.type=Image.Type.Sliced; border.color=Color.white;
            border.rectTransform.anchorMin=Vector2.zero; border.rectTransform.anchorMax=Vector2.one;
            border.rectTransform.offsetMin=new Vector2(3,3); border.rectTransform.offsetMax=new Vector2(-3,-3);
        }
        group=GetComponent<CanvasGroup>(); if(group==null) group=gameObject.AddComponent<CanvasGroup>();
        group.alpha=1; group.blocksRaycasts=true;
        layoutPosition=GetComponent<RectTransform>().anchoredPosition;
        RefreshSlotBonus();
    }
    void Box(string name,float x,float y,float width,float height)
    {
        var rect=Find<RectTransform>(name); if(rect==null) return;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,y); rect.sizeDelta=new Vector2(width,height);
    }
    void LayoutCard(bool compact)
    {
        Box("Stars",compact?108:164,compact?-14:-16,104,compact?22:24);
        for(int i=1;i<=4;i++)
        {
            var star=Find<RectTransform>("Star"+i); if(star==null) continue;
            star.anchorMin=star.anchorMax=star.pivot=new Vector2(1,1);
            star.anchoredPosition=new Vector2(-(i-1)*26,0); star.sizeDelta=Vector2.one*(compact?22:24);
        }
        Box("NameText",22,-178,244,32);
        Box("FamilyBadge",18,-216,122,28); Box("TypeBadge",148,-216,122,28);
        Box("FamilyText",7,-2,108,24); Box("TypeText",7,-2,108,24);
        Box("DescriptionText",22,-258,244,238);
        var rect=artwork.rectTransform;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        var bounds=VisibleBounds(artwork.sprite);
        float scale=Mathf.Min((compact?132:158)/bounds.width,(compact?120:108)/bounds.height);
        rect.sizeDelta=artwork.sprite.rect.size*scale;
        rect.anchoredPosition=new Vector2(0,compact?-8:Height*.5f-104)+(artwork.sprite.rect.size*.5f-bounds.center)*scale;
        if(stateBackground==null)
        {
            var go=new GameObject("CardState",typeof(RectTransform),typeof(Image)); go.transform.SetParent(transform,false);
            stateBackground=go.GetComponent<Image>(); stateBackground.color=new Color(.07f,.04f,.10f,.92f); stateBackground.raycastTarget=false;
            var label=new GameObject("Label",typeof(RectTransform),typeof(Text)); label.transform.SetParent(go.transform,false);
            stateText=label.GetComponent<Text>(); stateText.font=ui.Assets.HeadingFont; stateText.alignment=TextAnchor.MiddleCenter; stateText.color=GameTypography.Paper; stateText.raycastTarget=false;
            var textRect=stateText.rectTransform; textRect.anchorMin=Vector2.zero; textRect.anchorMax=Vector2.one; textRect.offsetMin=new Vector2(10,6); textRect.offsetMax=new Vector2(-10,-6);
            stateText.resizeTextForBestFit=true; stateText.resizeTextMinSize=14;
            stateText.horizontalOverflow=HorizontalWrapMode.Wrap; stateText.verticalOverflow=VerticalWrapMode.Truncate;
            go.SetActive(false);
        }
        Box("CardState",compact?10:18,compact?-128:-318,compact?204:252,compact?46:62);
        stateText.fontSize=compact?18:24; stateText.resizeTextMaxSize=stateText.fontSize;
    }
    void PrepareBack()
    {
        if(cardBack==null)
        {
            var go=new GameObject("CardBack",typeof(RectTransform),typeof(Image)); go.transform.SetParent(transform,false);
            cardBack=go.GetComponent<Image>(); cardBack.raycastTarget=false;
            backName=BackLabel("BackName",true); backDescription=BackLabel("BackDescription",false);
        }
        Box("CardBack",20,-20,BagWidth-40,BagHeight-40);
        cardBack.color=new[]{new Color(.93f,.9f,.85f),new Color(.83f,.93f,.98f),new Color(.98f,.9f,.7f),new Color(.92f,.85f,.98f)}[Card.Data.Rarity];
        backName.text=Card.Data.Name; backDescription.text=Card.Data.Description(Card.Stars);
        backName.color=backDescription.color=DescriptionColor(Card.Data.Rarity);
        backName.resizeTextMinSize=14; backName.resizeTextMaxSize=20;
        backDescription.resizeTextMinSize=14; backDescription.resizeTextMaxSize=18;
        Box("BackName",8,-4,BagWidth-56,26);
        Box("BackDescription",8,-35,BagWidth-56,BagHeight-84);
    }
    Text BackLabel(string name,bool title)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(cardBack.transform,false);
        var text=go.GetComponent<Text>(); GameTypography.Apply(text,ui.Assets,title);
        text.alignment=title?TextAnchor.MiddleLeft:TextAnchor.UpperLeft; text.raycastTarget=false;
        text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate;
        text.resizeTextForBestFit=true; text.resizeTextMinSize=title?18:16; text.resizeTextMaxSize=title?22:20; text.lineSpacing=1.05f;
        return text;
    }
    void RefreshFace()
    {
        bool back=compactView && showingBack;
        if(cardBack!=null) cardBack.gameObject.SetActive(back);
        if(artwork!=null) artwork.gameObject.SetActive(!back);
        var stars=Find<RectTransform>("Stars"); if(stars!=null) stars.gameObject.SetActive(!back);
        if(slotBonusLabel!=null)slotBonusLabel.gameObject.SetActive(!compactView && Card.Data.HasSlotBonus);
    }
    void RefreshSlotBonus()
    {
        if(slotBonusLabel==null && Card.Data.HasSlotBonus && !compactView)
        {
            var go=new GameObject("SlotBonusLabel",typeof(RectTransform),typeof(Text),typeof(Shadow));
            go.transform.SetParent(transform,false); slotBonusLabel=go.GetComponent<Text>();
            GameTypography.Apply(slotBonusLabel,ui.Assets,true);
            slotBonusLabel.alignment=TextAnchor.MiddleLeft; slotBonusLabel.raycastTarget=false;
            slotBonusLabel.resizeTextForBestFit=true; slotBonusLabel.resizeTextMinSize=14; slotBonusLabel.resizeTextMaxSize=18;
            go.GetComponent<Shadow>().effectColor=new Color(.04f,.02f,.06f,.9f);
            go.GetComponent<Shadow>().effectDistance=new Vector2(1,-1);
        }
        if(slotBonusLabel==null)return;
        bool active=Card.Data.IsSlotBonusActive(Slot) && !sealedCard && ui.Manager.Run!=null && Slot!=ui.Manager.Run.State.SealedSlot && !ui.Manager.Run.IsCardSealed(Card.Uid);
        double bonus=active?ui.Manager.Run.SlotScale(Card.Data,Slot)-1:Card.Data.SlotBonus-1;
        slotBonusLabel.text=active?L("{0}번 칸  +{1}%",Card.Data.BonusSlot,(bonus*100).ToString("0")):L("{0}번 칸 보너스",Card.Data.BonusSlot);
        slotBonusLabel.color=active?new Color(.65f,1,.65f):new Color(.95f,.9f,.78f);
        Box("SlotBonusLabel",compactView?16:22,compactView?-14:-144,compactView?88:244,24);
        slotBonusLabel.gameObject.SetActive(Card.Data.HasSlotBonus && !compactView);
    }
    public void SetBackFace(bool value,bool animate=false)
    {
        if(!compactView || Slot!=-1) return;
        if(flipTarget==value && (showingBack==value || Time.unscaledTime-flipStarted<.2f)) return;
        if(animate)
        {
            flipTarget=value; flipChanged=false; flipStarted=Time.unscaledTime;
        }
        else
        {
            showingBack=flipTarget=value; flipChanged=true; flipStarted=-10; RefreshFace();
        }
    }
    public void SetSealed(bool value,string reason="이번 턴 봉인")
    {
        sealedCard=value; sealedReason=L(reason); group.alpha=value?.58f:1;
        stateText.text=value?sealedReason:""; stateBackground.gameObject.SetActive(value);
        RefreshSlotBonus();
        if(value)stateBackground.transform.SetAsLastSibling();
    }
    public void SetMergePreview(int stars)
    {
        if(Card==null || artwork==null) return;
        bool active=stars>0;
        if(cardBack!=null) cardBack.gameObject.SetActive(!active && compactView && showingBack);
        artwork.gameObject.SetActive(active || !(compactView && showingBack));
        var starHolder=Find<RectTransform>("Stars"); if(starHolder!=null) starHolder.gameObject.SetActive(active || !(compactView && showingBack));
        artwork.color=active?new Color(.12f,.08f,.16f):Color.white;
        for(int i=1;i<=4;i++) { var star=Find<Image>("Star"+i); if(star!=null) star.gameObject.SetActive(i<=(active?stars:Card.Stars)); }
        stateText.text=active?L("합성 → {0}성",stars):sealedCard?sealedReason:"";
        stateBackground.gameObject.SetActive(active || sealedCard);
        if(active) stateBackground.transform.SetAsLastSibling();
    }
    public static Color GradeColor(int rarity)
    {
        return new[]{new Color(.69f,.70f,.74f),new Color(.55f,.82f,1),new Color(1,.78f,.3f),new Color(.87f,.73f,1)}[Mathf.Clamp(rarity,0,3)];
    }
    public static void ApplyFrame(Image frame,GameAssets assets,int rarity,bool compact)
    {
        rarity=Mathf.Clamp(rarity,0,3);
        var frames=compact?assets.BagFrames:assets.CardFrames;
        var sprite=frames!=null && rarity<frames.Length && frames[rarity]!=null?frames[rarity]:(compact?assets.BagFrame:assets.CardFrame);
        if(frame.sprite!=sprite) frame.sprite=sprite;
        frame.type=Image.Type.Simple; frame.color=Color.white;
        var material=assets.CardFrameMaterials!=null && rarity<assets.CardFrameMaterials.Length?assets.CardFrameMaterials[rarity]:null;
        if(frame.material!=material) frame.material=material;
    }
    public static Color DescriptionColor(int rarity) => new[]{new Color(.22f,.18f,.24f),new Color(.10f,.22f,.31f),new Color(.32f,.19f,.06f),new Color(.26f,.12f,.34f)}[Mathf.Clamp(rarity,0,3)];
    public static Color FamilyColor(bool iai) => iai?new Color(.85f,.75f,.9f):new Color(1,.91f,.71f);
    public static Color TypeColor(bool attack) => attack?new Color(1,.74f,.71f):new Color(.72f,.9f,.77f);
    public static Color TypeColor(CardDefinition card) => card.IsSurvival?new Color(.68f,.83f,1):TypeColor(card.IsAttack);
    static Rect VisibleBounds(Sprite sprite)
    {
        if(artBounds.TryGetValue(sprite,out var bounds)) return bounds;
        var rect=sprite.rect;
        bounds=new Rect(Vector2.zero,rect.size);
        if(sprite.texture.isReadable)
        {
            var pixels=sprite.texture.GetPixels32();
            int minX=(int)rect.width,minY=(int)rect.height,maxX=-1,maxY=-1;
            for(int y=0;y<(int)rect.height;y++) for(int x=0;x<(int)rect.width;x++)
            {
                if(pixels[((int)rect.y+y)*sprite.texture.width+(int)rect.x+x].a==0) continue;
                minX=Math.Min(minX,x); minY=Math.Min(minY,y); maxX=Math.Max(maxX,x); maxY=Math.Max(maxY,y);
            }
            if(maxX>=minX) bounds=new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
        }
        artBounds[sprite]=bounds;
        return bounds;
    }
    void SetText(string child,string value) { var t=Find<Text>(child); if(t!=null) {t.text=value;t.verticalOverflow=VerticalWrapMode.Overflow;} }
    public void SetSelected(bool value) { var s=Find<Image>("SelectionBorder"); if(s!=null) s.gameObject.SetActive(value); }
    public void SetLayoutScale(float scale)
    {
        layoutScale=scale;
        layoutPosition=GetComponent<RectTransform>().anchoredPosition;
        transform.localScale=Vector3.one*scale;
    }
    public void PlayPulse() { pulseTime=Time.unscaledTime; }
    public void OnPointerEnter(PointerEventData e) { hovered=true; }
    public void OnPointerExit(PointerEventData e) { hovered=false; }
    void Update()
    {
        if(ui==null) return;
        float pulse=Mathf.Clamp01(1-(Time.unscaledTime-pulseTime)/.35f);
        bool lift=hovered && !ui.Manager.Busy && !dragged;
        float target=layoutScale*(1+(lift?.025f:0)+pulse*.06f);
        float flip=Mathf.Clamp01((Time.unscaledTime-flipStarted)/.2f);
        if(!flipChanged && flip>=.5f) { flipChanged=true; showingBack=flipTarget; RefreshFace(); }
        float scale=Mathf.Lerp(transform.localScale.y,target,1-Mathf.Exp(-Time.unscaledDeltaTime*20));
        transform.localScale=new Vector3(scale*Mathf.Max(.06f,Mathf.Abs(1-flip*2)),scale,scale);
        var rt=GetComponent<RectTransform>();
        rt.anchoredPosition=Vector2.Lerp(rt.anchoredPosition,layoutPosition+Vector2.up*((lift?5:0)+pulse*10),1-Mathf.Exp(-Time.unscaledDeltaTime*20));
        if(selection!=null && selection.gameObject.activeSelf)
        {
            var color=selection.color; color.a=.8f+.2f*Mathf.Sin(Time.unscaledTime*4); selection.color=color;
        }
    }
    public void OnPointerDown(PointerEventData e) { dragged=false; }
    public void OnPointerClick(PointerEventData e)
    {
        if(dragged || ui==null) return;
        if(e.button==PointerEventData.InputButton.Right && Slot==-1 && compactView && Card!=null && Card.Uid>0)
        {
            if(!ui.Manager.Busy && !ui.ModalOpen) { ui.Select(Card.Uid); ui.FlipCard(Card.Uid); } return;
        }
        if(e.button==PointerEventData.InputButton.Right && Slot==-2 && Card!=null && Card.Uid>0)
        {
            ui.Manager.Sell(Card.Uid); return;
        }
        if(ui.Manager.Busy) return;
        if(clicked!=null) { clicked(); return; }
        if(e.button==PointerEventData.InputButton.Right && Slot>=0) ui.Manager.Unequip(Slot);
        else { ui.Select(Card.Uid); if(e.clickCount>=2 && Slot<0) ui.QuickEquip(Card.Uid); }
    }
    public void OnBeginDrag(PointerEventData e)
    {
        if(e.button!=PointerEventData.InputButton.Left) return;
        if(ui==null || ui.Manager.Busy) return;
        bool inventory=ui.InventoryOpen && Slot==-2 && Card.Uid>0;
        if(!inventory && (ui.ModalOpen || clicked!=null || ui.Manager.Run.State.Phase!=RunPhase.Battle)) return;
        dragBack=flipTarget;
        showingBack=false; flipTarget=false; flipChanged=true; flipStarted=-10; RefreshFace();
        transform.localScale=Vector3.one*layoutScale;
        dragged=true; ui.Select(Card.Uid);
        ghost=Instantiate(gameObject,ui.CanvasTransform); ghost.name="DraggedCard";
        var g=ghost.GetComponent<CanvasGroup>(); if(g==null) g=ghost.AddComponent<CanvasGroup>();
        g.blocksRaycasts=false; g.alpha=.9f;
        var rt=ghost.GetComponent<RectTransform>(); rt.position=transform.position+Vector3.up*12;
        rt.localScale=transform.lossyScale/ui.CanvasTransform.lossyScale.x;
        ghostStart=rt.position;
        ghost.GetComponent<CardUI>().enabled=false;
        group.alpha=.4f;
    }
    public void OnDrag(PointerEventData e)
    {
        if(ghost!=null) { ghost.transform.position=ghostStart+(Vector3)(e.position-e.pressPosition); ui.PreviewDrop(Card.Uid,e.position); }
    }
    public void OnEndDrag(PointerEventData e)
    {
        if(ghost==null) return;
        Destroy(ghost); group.alpha=sealedCard?.58f:1; SetBackFace(dragBack); ui.ClearDropPreview(); ui.Drop(Card.Uid,Slot,e.position);
    }
    void OnDisable() { hovered=false; if(ghost!=null) Destroy(ghost); }
}
