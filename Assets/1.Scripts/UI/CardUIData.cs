using System;
using UnityEngine;

[Serializable]
public class CardUIData
{
    public string name = "이름 없는 카드";
    public string rarity = "일반";
    public string category = "실행";
    public Sprite artwork;
    [TextArea] public string description;
    public Vector2Int[] range = { Vector2Int.right };
    [Tooltip("-1이면 남은 턴을 표시하지 않습니다.")] public int remainingTurns = -1;
}
