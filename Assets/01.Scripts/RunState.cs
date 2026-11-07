using System;
using System.Collections.Generic;

public enum RunPhase { Battle, CardReward, ItemReward, Shop, Victory, Defeat }
public enum EnemyIntent { None, Weaken, Guard, Seal, DisruptFocus, SiphonUltimate, Pressure, Regenerate, CardSeal, Fortify, MendAllies, Suppress }

[Serializable]
public class OwnedCard
{
    public int Uid, Definition, Stars = 1;
    public bool Temporary;
    public OwnedCard(int uid, int definition, int stars = 1) { Uid=uid; Definition=definition; Stars=stars; }
    public CardDefinition Data => CardCatalog.All[Definition];
}

[Serializable]
public class EnemyState
{
    public string Name;
    public int Kind;
    public long Health, MaxHealth;
    public EnemyIntent Intent;
    public int IntentSlot;
    public int IntentCardUid;
    public string IntentCardName;
    public bool Guarding;
    public bool Boss, Rear, Enraged;
    public long Barrier;
}

[Serializable]
public class RunState
{
    public int Version=3, Seed, RandomState, NextUid=1;
    public int Floor=1, Node, Wave, Turn=1, Gold=50, Ultimate, Target;
    public int WavesCleared, TurnsPlayed, CardsPlayed;
    public double Focus, TotalDamage, BestHit;
    public RunPhase Phase;
    public List<OwnedCard> Cards = new List<OwnedCard>();
    public int[] Sequence = { -1,-1,-1,-1,-1,-1 };
    public int[] Items = new int[ItemCatalog.Count];
    public List<EnemyState> Enemies = new List<EnemyState>();
    public int[] Offers = new int[3];
    public int[] OfferStars = {1,1,1};
    public bool[] Bought = new bool[3];
    public bool ShopMerged;
    public int ShopRerolls;
    public bool Weakened, FocusDisrupted;
    public bool SupportSuppressed;
    public double WeakenStrength;
    public int SealedSlot=-1;
    public int SealedCardUid;
    public int BonusTurns, LostTurns;
    public OwnedCard Find(int uid) => Cards.Find(c=>c.Uid==uid);
}

public sealed class TurnContext
{
    public Dictionary<int,double> FocusMultipliers = new Dictionary<int,double>();
    public Dictionary<int,double> DamageMultipliers = new Dictionary<int,double>();
    public double Flat, Mark=1;
    public double Additive, Multiplier=1;
    public int Echo, EchoStars, Executed, Attacks;
    public double EchoPower;
    public double CriticalChance, CriticalScale=1;
    public HashSet<int> Gambles = new HashSet<int>();
    public bool Preview;
}

public sealed class CardResult
{
    public OwnedCard Card;
    public long Damage;
    public List<int> Targets = new List<int>();
    public double FocusBefore, FocusAfter;
    public string ChanceText;
    public bool ChanceSuccess;
    public int Kills;
    public long BarrierDamage;
}
