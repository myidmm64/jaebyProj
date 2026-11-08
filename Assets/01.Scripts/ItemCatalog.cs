public static class ItemCatalog
{
    public const int Whetstone=6, OpeningWeight=7, TwinBlade=8, BossLens=9, PiercingNeedle=10,
        WarHorn=11, Incense=12, LuckyDie=13, RiftHammer=14, Hourglass=15, OrderSheet=16,
        MerchantScale=17, CoinPurse=18, RerollTicket=19, EchoBottle=20;
    static readonly string[] KoreanNames={
        "심호흡의 서","여우의 가면","붉은 열매","졸업 부적","시간의 물약","새싹 매듭",
        "숫돌","무게추","쌍날 장식","붉은 돋보기","관통 바늘","전쟁 나팔","집중 향로",
        "행운의 주사위","균열 망치","작은 모래시계","순서표","상인의 저울","동전 지갑","리롤 티켓","잔향 병"
    };
    static readonly string[] KoreanDescriptions={
        "집중 획득 +20%.",
        "피해 +18%.",
        "웨이브 시작 집중 +40 × 현재 층.",
        "거합 피해 +18%.",
        "제한 턴 +1 (최대 +2).",
        "웨이브 시작 궁극기 +12. 보상 골드 +15%.",
        "타격 피해 +40 × 층.",
        "매 턴 첫 공격 +25%.",
        "다단 공격 피해 +18%.",
        "보스에게 피해 +20%.",
        "경화 피해 감소 -10%p (최대 -25%p).",
        "첫 턴 피해 +20%.",
        "처치 후 집중 +8 × 층.",
        "성공 확률 +5%p.",
        "보호막 피해 +75%.",
        "남은 턴 2 이하: 피해 +25%.",
        "지정 칸 보너스 +50%.",
        "상점 가격 -10%.",
        "판매 골드 +25%.",
        "상점 무료 리롤 +1 (최대 2회).",
        "집중 소모 시 10% 보존 (최대 30%)."
    };
    static readonly string[] EnglishNames={
        "Book of Breathing","Fox Mask","Red Berry","Graduation Charm","Time Potion","Sprout Knot",
        "Whetstone","Training Weight","Twin-blade Charm","Red Lens","Piercing Needle","War Horn","Focus Incense",
        "Lucky Die","Rift Hammer","Small Hourglass","Order Sheet","Merchant's Scale","Coin Purse","Reroll Ticket","Echo Bottle"
    };
    static readonly string[] EnglishDescriptions={
        "Focus gained +20%.",
        "Damage +18%.",
        "Wave start: Focus +40 × floor.",
        "Iai damage +18%.",
        "Turn limit +1 (max +2).",
        "Wave start: ultimate +12. Gold rewards +15%.",
        "Damage per hit +40 × floor.",
        "First attack each turn +25% damage.",
        "Multi-hit damage +18%.",
        "Damage against bosses +20%.",
        "Guard reduction -10 percentage points (max -25).",
        "First-turn damage +20%.",
        "On kill: Focus +8 × floor.",
        "Success chance +5 percentage points.",
        "Damage to barriers +75%.",
        "2 turns left or fewer: damage +25%.",
        "Slot bonuses +50%.",
        "Shop prices -10%.",
        "Sell value +25%.",
        "+1 free shop reroll (max 2).",
        "Keep 10% of consumed Focus (max 30%)."
    };
    public static string[] Names => GameLocalization.IsEnglish?EnglishNames:KoreanNames;
    public static string[] Descriptions => GameLocalization.IsEnglish?EnglishDescriptions:KoreanDescriptions;
    public static int Count => KoreanNames.Length;
    public static int MaxCopies(int id) => id==4 || id==RerollTicket?2:id==CoinPurse?4:3;
}
