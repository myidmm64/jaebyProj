using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Acquired passives are appended in pickup order, outside the sequence drawer.</summary>
public class PassiveItemHUD : MonoBehaviour
{
    [SerializeField] RectTransform content;
    [SerializeField] PassiveItemIcon itemTemplate;
    [SerializeField] ScrollRect scroll;
    [SerializeField] Text countLabel;
    [SerializeField] GameObject emptyHint;
    [SerializeField] GameObject tooltip;
    [SerializeField] Text tooltipTitle;
    [SerializeField] Text tooltipDescription;

    readonly List<PassiveItemIcon> items = new();
    public int Count => items.Count;

    public void AddItem(Sprite icon, string itemName, string description = "")
    {
        var entry = Instantiate(itemTemplate, content);
        entry.name = $"Passive {items.Count + 1:00} - {itemName}";
        entry.Bind(this, icon, itemName, description);
        entry.gameObject.SetActive(true);
        items.Add(entry);
        countLabel.text = items.Count.ToString("00");
        emptyHint.SetActive(false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 0;
    }

    public void ShowTooltip(string itemName, string description)
    {
        tooltipTitle.text = itemName;
        tooltipDescription.text = description;
        tooltip.SetActive(true);
    }

    public void HideTooltip() => tooltip.SetActive(false);

    void OnDisable()
    {
        if (tooltip != null) tooltip.SetActive(false);
    }

    [ContextMenu("프로토타입/테스트 아이템 획득")]
    public void AddTestItem()
    {
        if (!Application.isPlaying) return;
        AddItem(null, "시험용 유물", "획득 순서대로 오른쪽 목록에 추가됩니다.");
    }
}
