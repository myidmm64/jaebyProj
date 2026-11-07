using UnityEngine;
using UnityEngine.UI;

public static class GameTypography
{
    public static readonly Color Ink=new Color(.19f,.14f,.20f);
    public static readonly Color Paper=new Color(.95f,.88f,.77f);

    public static void Apply(Text text,GameAssets assets,bool heading=false)
    {
        if(text==null || assets==null) return;
        text.font=heading && assets.HeadingFont!=null?assets.HeadingFont:assets.Font;
        text.fontStyle=FontStyle.Normal;
        text.alignByGeometry=text.alignment==TextAnchor.MiddleLeft || text.alignment==TextAnchor.MiddleCenter || text.alignment==TextAnchor.MiddleRight;
        text.fontSize=Mathf.Clamp(Mathf.RoundToInt(text.fontSize/6f)*6,18,84);
        text.lineSpacing=text.name.Contains("Description")?1.18f:1;
        if(text.resizeTextForBestFit) { text.resizeTextMinSize=18; text.resizeTextMaxSize=Mathf.Max(18,text.fontSize); }
        foreach(var outline in text.GetComponents<Outline>())
        {
            outline.effectDistance=new Vector2(1,-1); outline.effectColor=new Color(.12f,.08f,.14f,.9f);
        }
    }
}
