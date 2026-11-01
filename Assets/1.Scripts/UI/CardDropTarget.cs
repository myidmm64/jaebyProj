using UnityEngine;
using UnityEngine.EventSystems;
using WizardCombat;

public sealed class CardDropTarget : MonoBehaviour, IDropHandler
{
    public StageCombatController Owner;
    public int Slot=-1;
    public Card Target;
    public void OnDrop(PointerEventData e)=>Owner.QueueDrop(Slot,Target);
}
