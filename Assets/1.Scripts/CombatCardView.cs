using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using WizardCombat;

public sealed partial class StageCombatController
{
    RectTransform DrawCard(Transform parent,Card card,float x,float y,float w,float h,string caption,Action click,bool locked=false,int? expectedDamage=null)
    {
        var rect=Box(parent,"Card",x,y,w,h,Color.clear);
        var hit=rect.GetComponent<Image>();hit.sprite=null;hit.type=Image.Type.Simple;hit.preserveAspect=false;hit.raycastTarget=true;
        float scale=Mathf.Min(w/80f,h/110f),left=(w-80*scale)*.5f,top=(h-110*scale)*.5f;
        var visual=Box(rect,"Card frame",left,top,80,110,locked?new Color(.48f,.48f,.48f):card!=null&&card==selected?new Color(1,.92f,.72f):Color.white);
        visual.localScale=new Vector3(scale,scale,1);
        var image=visual.GetComponent<Image>();image.sprite=art!=null?art.cardFrame:null;image.type=Image.Type.Simple;image.preserveAspect=false;image.raycastTarget=true;
        if(image.sprite==null)image.color=dark;
        var button=Ensure<Button>(rect.gameObject);button.interactable=!locked;button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>{if(busy)return;click();Refresh();});
        button.targetGraphic=image;
        // Positions are authored against the supplied 80x110 PNG, including its transparent margin.
        Text Label(string name,string value,float px,float py,float pw,float ph,float fontSize,TextAnchor alignment=TextAnchor.MiddleCenter)
        {
            // Keep glyph rendering at 2x screen resolution even though the frame uses native-art coordinates.
            var label=Text(visual,value,px,py,pw*scale,ph*scale,Mathf.Max(9,Mathf.RoundToInt(fontSize*scale)),name);
            label.rectTransform.localScale/=scale;label.alignment=alignment;return label;
        }
        if(card==null)
        {
            var oldDrag=rect.GetComponent<CardDragHandle>();if(oldDrag!=null)oldDrag.Card=null;
            Label("Card empty title",locked?"잠긴 슬롯":"빈 슬롯",10,59,60,12,7);
            Label("Card empty description",locked?caption:"카드를 놓아 장착",11,74,58,18,6);return rect;
        }
        int prediction=Rules.Movement(card.Id)||Rules.Support(card.Id)?0:expectedDamage??battle.PreviewCard(card);
        Label("Expected damage",prediction<0?"?":prediction.ToString(),10,11,22,12,8);
        for(int i=0;i<card.Star;i++)
        {
            var star=Box(visual,"Card star",70-card.Star*7+i*7,13,6,6,Color.white).GetComponent<Image>();
            star.sprite=art!=null?art.cardStar:null;star.type=Image.Type.Simple;star.preserveAspect=true;star.raycastTarget=false;
        }
        string family=card.Id>=4&&!Rules.Movement(card.Id)?"거합":"공통";
        string kind=card.Id==11?"궁극기":Rules.Movement(card.Id)?"이동":Rules.Support(card.Id)?"서포트":"공격";
        Label("Card classification",family+" / "+kind,15,47,50,8,5.8f);
        Label("Card name",Rules.Names[card.Id],10,59,60,11,7.5f);
        var description=Label("Card description",Rules.EffectText(card),11,73,58,19,5.8f,TextAnchor.UpperLeft);description.lineSpacing=.88f;
        Label("Card lifetime",card.Life.ToString(),10,94,20,9,8);
        var rangeGo=UINode(visual,"Range diagram",typeof(RectTransform));var range=(RectTransform)rangeGo.transform;
        const float pitch=2.3f;range.anchorMin=range.anchorMax=range.pivot=new Vector2(0,1);
        range.anchoredPosition=new Vector2(65-3.5f*pitch,-(94-3.5f*pitch));range.sizeDelta=Vector2.one*(7*pitch);
        foreach(var cell in Rules.Pattern(card.Id,card.Star).Append(Vector2Int.zero).Distinct())
        {
            var node=Box(range,cell==Vector2Int.zero?"Range origin":"Range cell",(3+cell.x)*pitch+.5f/scale,(3-cell.y)*pitch+.5f/scale,pitch-1/scale,pitch-1/scale,cell==Vector2Int.zero?blue:orange);
            node.GetComponent<Image>().raycastTarget=false;
        }
        var drag=Ensure<CardDragHandle>(rect.gameObject);drag.Owner=this;drag.Card=card;
        return rect;
    }
}

