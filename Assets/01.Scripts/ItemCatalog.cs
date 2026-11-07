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
        "집중 획득량 +20%. 중첩 시 더합니다.",
        "모든 최종 피해 +18%. 중첩 시 더합니다.",
        "웨이브 시작 집중 +40 × 현재 층.",
        "거합 계열 공격 피해 +18%. 중첩 시 더합니다.",
        "웨이브 제한 턴 +1. 아이템 연장은 최대 2턴.",
        "웨이브 시작 궁극기 +12. 웨이브 보상 골드 +15%.",
        "각 타격 기본 피해 +40 × 현재 층. 재련 배수에는 포함되지 않습니다.",
        "매 턴 첫 공격의 피해 +25%. 중첩 시 더합니다.",
        "기본 타격 횟수가 2회 이상인 카드 피해 +18%.",
        "보스 본체에게 주는 피해 +20%. 소환 부하에게는 적용하지 않습니다.",
        "적의 경화 피해 감소를 15%p 낮춥니다. 최대 40%p.",
        "웨이브 첫 턴의 공격 피해 +20%.",
        "공격 후 처치한 적 1마리당 집중 +8 × 현재 층. 소모 처리 후 얻습니다.",
        "확률형 카드의 성공 확률 +5%p. 아이템 보정 최대 15%p, 최종 확률 최대 95%.",
        "보호막에 주는 피해 +75%. 적 체력에 주는 피해는 늘어나지 않습니다.",
        "남은 턴이 2 이하일 때 공격 피해 +25%.",
        "지정 칸 추가 효과의 증가분 +50%. 중첩 보정 최대 +150%.",
        "상점 카드 가격 -10%. 할인 최대 30%.",
        "카드 판매 골드 +25%. 증가량 최대 100%.",
        "상점마다 첫 리롤 무료. 2개면 첫 두 번 무료입니다.",
        "집중 소모 공격 후 집중 10%를 남깁니다. 최대 30%. 원래 보존율이 높으면 원래 수치를 사용합니다."
    };
    public static int Count => Names.Length;
    public static int MaxCopies(int id) => id==4 || id==RerollTicket?2:id==CoinPurse?4:3;
}
