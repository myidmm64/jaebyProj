using UnityEngine;
using UnityEngine.UI;

/// <summary>Identifies saved HUD nodes and preserves Inspector layout/style edits.</summary>
[DisallowMultipleComponent]
public sealed class CombatUINode : MonoBehaviour
{
    [HideInInspector] public string key;
    [HideInInspector] public int occurrence;
    [SerializeField, HideInInspector] bool hasDefaults;
    [SerializeField, HideInInspector] Vector2 defaultPosition,defaultSize,defaultAnchorMin,defaultAnchorMax,defaultPivot;
    [SerializeField, HideInInspector] Vector3 defaultScale;
    [SerializeField, HideInInspector] Color defaultColor;
    [SerializeField, HideInInspector] int defaultFontSize;
    [SerializeField, HideInInspector] Font defaultFont;
    Vector2 positionOffset,sizeOffset,anchorMin,anchorMax,pivot;
    Vector3 scaleRatio=Vector3.one;
    Color color;
    Font font;
    int fontSize;
    bool captured,anchorsEdited,pivotEdited,colorEdited,fontSizeEdited,fontEdited;
    public void CaptureEdits()
    {
        if(captured&&Application.isPlaying)return;captured=true;
        if(!hasDefaults)return;
        var r=(RectTransform)transform;
        positionOffset=r.anchoredPosition-defaultPosition;sizeOffset=r.sizeDelta-defaultSize;
        scaleRatio=new Vector3(Ratio(r.localScale.x,defaultScale.x),Ratio(r.localScale.y,defaultScale.y),Ratio(r.localScale.z,defaultScale.z));
        anchorMin=r.anchorMin;anchorMax=r.anchorMax;pivot=r.pivot;
        anchorsEdited=anchorMin!=defaultAnchorMin||anchorMax!=defaultAnchorMax;pivotEdited=pivot!=defaultPivot;
        var graphic=GetComponent<Graphic>();if(graphic!=null){color=graphic.color;colorEdited=color!=defaultColor;}
        var text=GetComponent<Text>();if(text!=null){fontSize=text.fontSize;fontSizeEdited=fontSize!=defaultFontSize;font=text.font;fontEdited=font!=defaultFont;}
    }
    static float Ratio(float value,float baseline)=>Mathf.Approximately(baseline,0)?1:value/baseline;
    void RememberDefaults()
    {
        var r=(RectTransform)transform;defaultPosition=r.anchoredPosition;defaultSize=r.sizeDelta;defaultScale=r.localScale;
        defaultAnchorMin=r.anchorMin;defaultAnchorMax=r.anchorMax;defaultPivot=r.pivot;
        var graphic=GetComponent<Graphic>();if(graphic!=null)defaultColor=graphic.color;
        var text=GetComponent<Text>();if(text!=null){defaultFontSize=text.fontSize;defaultFont=text.font;}
        hasDefaults=true;
    }
    public void ApplyEdits()
    {
        if(!hasDefaults||!Application.isPlaying)RememberDefaults();
        var r=(RectTransform)transform;
        if(anchorsEdited){r.anchorMin=anchorMin;r.anchorMax=anchorMax;}if(pivotEdited)r.pivot=pivot;
        r.anchoredPosition+=positionOffset;r.sizeDelta+=sizeOffset;r.localScale=Vector3.Scale(r.localScale,scaleRatio);
        var graphic=GetComponent<Graphic>();if(colorEdited&&graphic!=null)graphic.color=color;
        var text=GetComponent<Text>();if(text!=null){if(fontSizeEdited)text.fontSize=fontSize;if(fontEdited)text.font=font;}
    }
    public void ApplyPositionEdit(){((RectTransform)transform).anchoredPosition+=positionOffset;}
}
