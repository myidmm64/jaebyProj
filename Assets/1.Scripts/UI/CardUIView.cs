using UnityEngine;
using UnityEngine.UI;

public class CardUIView : MonoBehaviour
{
    [SerializeField] Text title;
    [SerializeField] Text classification;
    [SerializeField] Text description;
    [SerializeField] Text remainingTurns;
    [SerializeField] Image artwork;
    [SerializeField] Text fallback;
    [SerializeField] Image[] rangeCells;

    public void Bind(CardUIData data)
    {
        title.text = data.name;
        classification.text = $"{data.rarity} / {data.category}";
        description.text = data.description;
        remainingTurns.gameObject.SetActive(data.remainingTurns >= 0);
        remainingTurns.text = $"남은 {data.remainingTurns}턴";
        artwork.sprite = data.artwork;
        artwork.enabled = data.artwork != null;
        fallback.gameObject.SetActive(data.artwork == null);
        fallback.text = string.IsNullOrEmpty(data.name) ? "+" : data.name.Substring(0, 1);
        for (int i = 0; i < rangeCells.Length; i++)
        {
            var cell = new Vector2Int(i % 3 - 1, 1 - i / 3);
            bool targeted = data.range != null && System.Array.IndexOf(data.range, cell) >= 0;
            rangeCells[i].color = targeted ? new Color32(215, 246, 80, 255)
                : cell == Vector2Int.zero ? new Color32(242, 240, 225, 255) : new Color32(65, 68, 73, 255);
        }
    }
}
