using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using WizardCombat;

public sealed class CardDragHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public StageCombatController Owner;
    public Card Card;
    GameObject ghost;
    CanvasGroup group;
    Vector2 offset;
    bool dragging;
    public void OnInitializePotentialDrag(PointerEventData e)=>e.useDragThreshold=false;
    public void OnPointerDown(PointerEventData e)
    {
        if(e.button!=PointerEventData.InputButton.Left||!Owner.CanEdit||Card==null)return;
        Lift(e);
    }
    void Lift(PointerEventData e)
    {
        if(ghost!=null)return;
        var source=(RectTransform)transform;var canvas=GetComponentInParent<Canvas>();
        // Copy visuals only, never the source drag handler or its lifecycle state.
        ghost=new GameObject("Dragged card",typeof(RectTransform),typeof(Image),typeof(CanvasGroup));
        var r=ghost.GetComponent<RectTransform>();r.SetParent(canvas.transform,false);r.pivot=source.pivot;r.sizeDelta=source.rect.size;
        r.localScale=source.lossyScale/canvas.transform.lossyScale.x*1.035f;
        var sourceImage=GetComponent<Image>();var ghostImage=ghost.GetComponent<Image>();
        ghostImage.color=sourceImage.color;ghostImage.sprite=sourceImage.sprite;ghostImage.type=sourceImage.type;
        ghostImage.pixelsPerUnitMultiplier=sourceImage.pixelsPerUnitMultiplier;ghostImage.preserveAspect=sourceImage.preserveAspect;ghostImage.raycastTarget=false;
        foreach(Transform child in transform)Instantiate(child,r,false);
        var cg=ghost.GetComponent<CanvasGroup>();cg.blocksRaycasts=false;cg.interactable=false;cg.alpha=.95f;
        group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();
        group.alpha=.3f;
        offset=((Vector2)source.position-e.position)*1.035f+Vector2.up*8*canvas.scaleFactor;
        ghost.transform.position=e.position+offset;
    }
    public void OnBeginDrag(PointerEventData e)
    {
        if(e.button!=PointerEventData.InputButton.Left||!Owner.CanEdit||Card==null)return;
        Lift(e);dragging=true;Owner.BeginDrag(Card);
        if(group!=null)group.blocksRaycasts=false;
    }
    public void OnDrag(PointerEventData e)
    {
        if(ghost!=null)ghost.transform.position=e.position+offset;
        if(dragging)Owner.HoverDrop(e.position);
    }
    public void OnPointerUp(PointerEventData e){if(!dragging)ClearVisual();}
    public void OnEndDrag(PointerEventData e)
    {
        if(!dragging)return;dragging=false;
        ClearVisual();Owner.CompleteDrop(e.position);
    }
    void ClearVisual()
    {
        if(ghost!=null){Destroy(ghost);ghost=null;}
        if(group!=null){group.alpha=1;group.blocksRaycasts=true;}
    }
    void OnDisable(){ClearVisual();if(dragging){dragging=false;if(Owner!=null)Owner.CancelDrag();}}
}
