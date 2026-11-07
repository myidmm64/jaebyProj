using UnityEngine;

public class GameAssets : ScriptableObject
{
    public CardUI CardPrefab;
    public Enemy SlimePrefab, StonePrefab;
    public Font Font;
    public Font HeadingFont;
    public Sprite Panel, Frame, EmptySlot, Execute, CardFrame, BagFrame, Star, Slash;
    public Sprite Focus, Buff, Gold, Boss, Battle, Shop, CurrentNode;
    public Sprite[] ItemIcons;
    public Sprite[] CardArt;
    public Sprite[] CardFrames, BagFrames;
    public Material[] CardFrameMaterials;
    public Material CombatMaterial;
    public Sprite GroundShadow;
    public Sprite HeaderTab;
    public Sprite SelectionCorners;
    public Sprite PlayerArt;
    public Sprite[] EnemyArt;
    public float PlayerHeight;
    public float[] EnemyHeights;
    public Sprite[] BossArt;
    public float[] BossHeights;
    public float UnitHeight(int kind,int floor,bool boss)
    {
        if(boss && BossHeights!=null && floor>0 && floor<=BossHeights.Length)return BossHeights[floor-1];
        return EnemyHeights!=null && kind>=0 && kind<EnemyHeights.Length?EnemyHeights[kind]:1.2f;
    }
}
