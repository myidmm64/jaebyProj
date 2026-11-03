using UnityEngine;
using UnityEngine.UI;

public abstract class Enemy : CombatUnit
{
    [SerializeField] private long health = 100;
    [SerializeField] private long maxHealth = 100;
    [SerializeField] private Image healthFill;
    [SerializeField] private Text healthText;

    public long MaxHealth => maxHealth;

    public long Health
    {
        get => health;
        set
        {
            health = System.Math.Max(0, value);
            RefreshHealthUI();
        }
    }

    protected virtual void Awake()
    {
        RefreshHealthUI();
    }

    private void RefreshHealthUI()
    {
        if (healthFill != null)
            healthFill.fillAmount = maxHealth > 0 ? (float)((double)health / maxHealth) : 0f;
        if (healthText != null)
            healthText.text = health.ToString();
    }
}
