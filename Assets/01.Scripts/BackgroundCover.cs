using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundCover : MonoBehaviour
{
    [SerializeField, Min(1)] float coverage=1.06f;
    [SerializeField] float verticalOffset;
    float lastVerticalOffset;
    SpriteRenderer background;
    Camera view;
    Sprite lastSprite;
    Vector2 lastViewSize;
    Vector3 lastParentScale;
    float lastCoverage;
    SpriteRenderer lowerExtension;
    Sprite bottomStrip, extensionSource;

    void OnEnable() { lastSprite=null; Fit(); }
    void LateUpdate() { Fit(); }
    public void SetVerticalOffset(float offset) { verticalOffset=offset; Fit(); }

    void Fit()
    {
        if(background==null) background=GetComponent<SpriteRenderer>();
        if(view==null) view=Camera.main;
        if(view==null || !view.orthographic || background.sprite==null) return;
        var size=new Vector2(view.orthographicSize*2*view.aspect,view.orthographicSize*2);
        var parentScale=transform.parent!=null?transform.parent.lossyScale:Vector3.one;
        if(lastSprite==background.sprite && lastViewSize==size && lastParentScale==parentScale && lastCoverage==coverage && lastVerticalOffset==verticalOffset) { ExtendBottom(); return; }
        var bounds=background.sprite.bounds;
        float width=bounds.size.x*Mathf.Abs(parentScale.x);
        float height=bounds.size.y*Mathf.Abs(parentScale.y);
        if(width<=0 || height<=0) return;
        float scale=Mathf.Max(size.x/width,size.y/height)*Mathf.Max(1,coverage);
        transform.localScale=new Vector3(scale,scale,1);
        var center=new Vector3(view.transform.position.x,view.transform.position.y+verticalOffset,transform.position.z);
        transform.position=center-transform.TransformVector(bounds.center);
        lastSprite=background.sprite; lastViewSize=size; lastParentScale=parentScale; lastCoverage=coverage; lastVerticalOffset=verticalOffset;
        ExtendBottom();
    }
    void ExtendBottom()
    {
        float bottom=view.transform.position.y-view.orthographicSize;
        float gap=background.bounds.min.y-bottom;
        if(gap<=0) { if(lowerExtension!=null) lowerExtension.enabled=false; return; }
        if(bottomStrip==null || extensionSource!=background.sprite)
        {
            Release(bottomStrip); extensionSource=background.sprite;
            var rect=extensionSource.rect; rect.height=Mathf.Min(4,rect.height);
            bottomStrip=Sprite.Create(extensionSource.texture,rect,new Vector2(.5f,1),extensionSource.pixelsPerUnit,0,SpriteMeshType.FullRect);
            bottomStrip.hideFlags=HideFlags.DontSave;
        }
        if(lowerExtension==null)
        {
            var go=new GameObject("LowerBackdropExtension"); go.hideFlags=HideFlags.DontSave; go.layer=gameObject.layer; go.transform.SetParent(transform,false);
            lowerExtension=go.AddComponent<SpriteRenderer>();
        }
        lowerExtension.enabled=true; lowerExtension.sprite=bottomStrip;
        lowerExtension.sharedMaterial=background.sharedMaterial; lowerExtension.color=background.color;
        lowerExtension.sortingLayerID=background.sortingLayerID; lowerExtension.sortingOrder=background.sortingOrder-1;
        lowerExtension.transform.position=new Vector3(background.bounds.center.x,background.bounds.min.y+.02f,transform.position.z+.01f);
        lowerExtension.transform.localScale=new Vector3(1,(gap+.12f)/(Mathf.Abs(transform.lossyScale.y)*bottomStrip.bounds.size.y),1);
    }
    static void Release(Object value) { if(value==null)return; if(Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    void OnDisable() { if(lowerExtension!=null)Release(lowerExtension.gameObject); Release(bottomStrip); lowerExtension=null; bottomStrip=null; }
}
