using System;
using System.Collections.Generic;
using System.Globalization;

public enum GameLanguage { Korean, English }

public static class GameLocalization
{
    public static GameLanguage CurrentLanguage { get; private set; } = GameLanguage.Korean;
    public static bool IsEnglish => CurrentLanguage == GameLanguage.English;
    public static string LanguageName => IsEnglish ? "English" : "한국어";
    public static event Action Changed;

    public static void SetLanguage(GameLanguage language)
    {
        if (!Enum.IsDefined(typeof(GameLanguage), language) || language == CurrentLanguage) return;
        CurrentLanguage = language;
        Changed?.Invoke();
    }

    public static string Text(string korean, params object[] args)
    {
        if (korean == null) return string.Empty;
        string value = korean;
        if (IsEnglish)
        {
            if (!English.TryGetValue(korean, out value) &&
                !UiTranslations.English.TryGetValue(korean, out value) &&
                !CardTranslations.English.TryGetValue(korean, out value)) value = korean;
        }
        return args == null || args.Length == 0 ? value : string.Format(CultureInfo.InvariantCulture, value, args);
    }

    static readonly Dictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "이어할 기록이 없습니다.", "No saved run found." },
        { "봉인되지 않은 칸에 실행할 카드를 편성해 주세요.", "Place a usable card in an unsealed slot." },
        { "가방이 가득 찼습니다. 합성·판매하거나 골드를 선택하세요.", "Bag full. Merge, sell, or take gold." },
        { "{0}회 합성했습니다.", "Merged {0} pair(s)." },
        { "같은 카드·같은 성급 2장이 필요합니다.", "Requires two matching cards of the same star rank." },
        { "{0} 판매  +{1} 골드", "Sold {0}  +{1} gold" },
        { "마지막 카드 또는 궁극기 카드는 판매할 수 없습니다.", "You cannot sell your last card or an ultimate card." },
        { "{0} {1}성 합성", "Merged {0} to {1} stars" },
        { "리롤할 골드가 부족합니다.", "Not enough gold to reroll." },
        { "대거합 획득! 이번 턴 안에 시퀀스에 넣어 사용하세요.", "Grand Iai acquired! Equip and use it this turn." },
        { "궁극기 카드가 이미 있거나 가방·장착 칸이 가득 찼습니다.", "Ultimate already held, or all card slots are full." },
        { "궁극기 게이지가 100 필요합니다.", "Requires 100 ultimate charge." },
        { "이번 턴에는 봉인된 칸입니다.", "This slot is sealed this turn." },
        { "가방이 가득 찼습니다. 먼저 판매하거나 합성하세요.", "Bag full. Sell or merge a card first." },
        { "저장하지 못했습니다. 디스크 공간을 확인해 주세요.", "Could not save. Check available disk space." },
        { "보호막 {0}", "Barrier {0}" },
        { "집중 {0}", "Focus {0}" }
    };
}
