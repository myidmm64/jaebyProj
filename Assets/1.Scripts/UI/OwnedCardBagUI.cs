using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Owned cards only; sequence placement and execution are separate systems.</summary>
public class OwnedCardBagUI : MonoBehaviour
{
    [SerializeField] RectTransform content;
    [SerializeField] CardUIView cardTemplate;
    [SerializeField] ScrollRect scroll;
    [SerializeField] Text countLabel;
    [SerializeField] GameObject emptyHint;
    [SerializeField] List<CardUIData> cards = new();
    readonly List<CardUIView> views = new();

    public IReadOnlyList<CardUIData> Cards => cards;
    public int Count => cards.Count;

    void OnEnable()
    {
        if (content == null || cardTemplate == null) return;
        foreach (var view in views)
        {
            if (view == null) continue;
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }
        views.Clear();
        foreach (var card in cards) if (card != null) CreateView(card);
        Refresh();
    }

    public void AddCard(CardUIData card)
    {
        if (card == null) throw new System.ArgumentNullException(nameof(card));
        cards.Add(card);
        CreateView(card);
        Refresh();
        Canvas.ForceUpdateCanvases();
        scroll.horizontalNormalizedPosition = 1f;
    }

    void CreateView(CardUIData card)
    {
        var view = Instantiate(cardTemplate, content);
        view.name = $"Card {views.Count + 1:00} - {card.name}";
        view.Bind(card);
        view.gameObject.SetActive(true);
        views.Add(view);
    }

    void Refresh()
    {
        countLabel.text = $"{cards.Count:00} 장";
        emptyHint.SetActive(cards.Count == 0);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    [ContextMenu("프로토타입/테스트 카드 추가")]
    public void AddTestCard()
    {
        if (!Application.isPlaying) return;
        AddCard(new CardUIData { name = "시험용 카드", description = "보유 카드 표시를 확인합니다." });
    }
}
