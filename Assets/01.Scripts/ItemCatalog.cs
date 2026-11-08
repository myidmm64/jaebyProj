public static class ItemCatalog
{
    public const int Whetstone=6, OpeningWeight=7, TwinBlade=8, BossLens=9, PiercingNeedle=10,
        WarHorn=11, Incense=12, LuckyDie=13, RiftHammer=14, Hourglass=15, OrderSheet=16,
        MerchantScale=17, CoinPurse=18, RerollTicket=19, EchoBottle=20;
    public static readonly string[] Names={
        "심호흡의 서","여우의 가면","붉은 열매","졸업 부적","시간의 물약","새싹 매듭",
        "숫돌","무게추","쌍날 장식","붉은 돋보기","관통 바늘","전쟁 나팔","집중 향로",
        "행운의 주사위","균열 망치","작은 모래시계","순서표","상인의 저울","동전 지갑","리롤 티켓","잔향 병"
    };
    public static readonly string[] Descriptions={
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
        "집중 10% 보존 (최대 30%)."
    };
    public static int Count => Names.Length;
    public static int MaxCopies(int id) => id==4 || id==RerollTicket?2:id==CoinPurse?4:3;
}
