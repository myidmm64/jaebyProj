using UnityEngine;
using UnityEngine.UI;

public abstract class Enemy : CombatUnit
{
    [SerializeField] private long health = 100;
    [SerializeField] private long maxHealth = 100;
    [SerializeField] private Text healthText;
    Outline targetOutline;
    Transform buffArea;

    public long MaxHealth => maxHealth;
    public Vector3 HeartPosition => healthText!=null?healthText.transform.parent.position:transform.position+Vector3.up;
    public Vector3 BuffPosition => buffArea!=null?buffArea.position:transform.position+new Vector3(.75f,.25f,0);

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

    public void Configure(EnemyState state,bool boss=false)
    {
        name=state.Name;
        maxHealth=state.MaxHealth;
        Health=state.Health;
        if(healthText!=null)
        {
            if(GameManager.Instance!=null) GameTypography.Apply(healthText,GameManager.Instance.Assets,true);
            healthText.fontSize=24;
            healthText.resizeTextForBestFit=true;
            healthText.resizeTextMinSize=12;
            healthText.resizeTextMaxSize=24;
            healthText.rectTransform.anchoredPosition=new Vector2(0,5);
            healthText.rectTransform.sizeDelta=new Vector2(60,38);
            var heart=healthText.transform.parent.GetComponent<Image>();
            heart.rectTransform.anchoredPosition=Vector2.zero;
            var canvas=heart.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            var visual=GetComponentInChildren<SpriteRenderer>();
            float top=visual!=null?visual.bounds.max.y-transform.position.y:1.3f;
            canvas.localPosition=new Vector3(0,top+.34f,0);
            targetOutline=heart.GetComponent<Outline>(); if(targetOutline==null) targetOutline=heart.gameObject.AddComponent<Outline>();
            targetOutline.effectColor=new Color(.68f,.82f,.68f,.9f); targetOutline.effectDistance=new Vector2(2,-2); targetOutline.enabled=false;
        }
        var buff=GetComponentsInChildren<Transform>(true);
        foreach(var t in buff)
        {
            if(t.name=="BuffSlot") t.gameObject.SetActive(false);
            if(t.name=="BuffArea") { buffArea=t; var rect=t.GetComponent<RectTransform>(); rect.anchoredPosition=new Vector2(48,-22); }
        }
    }

    private void RefreshHealthUI()
    {
        if (healthText != null)
            healthText.text = GameMath.Number(health);
    }
    public void SetTargeted(bool targeted) { if(targetOutline!=null) targetOutline.enabled=targeted; }
}
