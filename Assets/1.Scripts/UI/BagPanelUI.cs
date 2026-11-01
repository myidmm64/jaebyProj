using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Tab-controlled uGUI drawer. Bag contents are prototype slots only.</summary>
public class BagPanelUI : MonoBehaviour
{
    [SerializeField] RectTransform panel;
    [SerializeField] CanvasGroup panelGroup;
    [SerializeField] Button toggleButton;
    [SerializeField] Button closeButton;
    [SerializeField, Min(0.01f)] float slideDuration = 0.25f;
    [SerializeField] float bottomMargin = 16f;

    float progress;
    public bool IsOpen { get; private set; }

    void OnEnable()
    {
        if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        IsOpen = false;
        progress = 0;
        UpdatePanel();
    }

    void OnDisable()
    {
        if (toggleButton != null) toggleButton.onClick.RemoveListener(Toggle);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) Toggle();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Tab)) Toggle();
#endif
        progress = Mathf.MoveTowards(progress, IsOpen ? 1f : 0f,
            Time.unscaledDeltaTime / Mathf.Max(0.01f, slideDuration));
        UpdatePanel();
    }

    public void Toggle() => SetOpen(!IsOpen);
    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    public void SetOpen(bool open)
    {
        IsOpen = open;
        UpdatePanel();
    }

    void UpdatePanel()
    {
        if (panel == null || panelGroup == null) return;
        float eased = Mathf.SmoothStep(0f, 1f, progress);
        panel.anchoredPosition = new Vector2(panel.anchoredPosition.x,
            Mathf.Lerp(-panel.rect.height - bottomMargin, bottomMargin, eased));
        panelGroup.alpha = progress > 0 ? 1f : 0f;
        panelGroup.interactable = IsOpen;
        panelGroup.blocksRaycasts = IsOpen;
    }
}
