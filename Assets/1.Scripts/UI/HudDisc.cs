using UnityEngine;
using UnityEngine.UI;

/// <summary>A flat UI circle made from triangles, without a texture asset.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class HudDisc : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var rect=rectTransform.rect;var center=rect.center;var size=rect.size*.5f;
        vh.AddVert(center,color,Vector2.zero);
        for(int i=0;i<=40;i++){float a=i*Mathf.PI*2/40;vh.AddVert(center+new Vector2(Mathf.Cos(a)*size.x,Mathf.Sin(a)*size.y),color,Vector2.zero);if(i>0)vh.AddTriangle(0,i,i+1);}
    }
}
