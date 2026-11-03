using System.Collections.Generic;
using UnityEngine;

public abstract class CombatUnit : MonoBehaviour
{
    [SerializeField] private List<Buff> buffs = new List<Buff>();

    public List<Buff> Buffs => buffs;
}
