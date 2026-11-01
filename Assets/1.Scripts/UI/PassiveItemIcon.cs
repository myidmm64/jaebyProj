using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PassiveItemIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image icon;
    [SerializeField] Text fallback;
    PassiveItemHUD owner;
    string itemName;
    string description;

    public void Bind(PassiveItemHUD hud, Sprite sprite, string title, string detail)
    {
        owner = hud;
        itemName = string.IsNullOrWhiteSpace(title) ? "이름 없는 아이템" : title;
        description = detail;
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        fallback.gameObject.SetActive(sprite == null);
        fallback.text = itemName.Substring(0, 1);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null) owner.ShowTooltip(itemName, description);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null) owner.HideTooltip();
    }
}
