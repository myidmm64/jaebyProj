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
}
