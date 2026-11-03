using UnityEngine;

public abstract class Enemy : CombatUnit
{
    [SerializeField] private long health = 100;

    public long Health
    {
        get => health;
        set => health = value;
    }
}
