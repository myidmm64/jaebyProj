using System;
using System.Collections.Generic;

public static class CardTranslations
{
    public static readonly Dictionary<string,string> English=new Dictionary<string,string>(StringComparer.Ordinal)
    {
        {"거합","Iai"},
        {"공통","Common"},
        {"이번 턴 봉인","Sealed this turn"},
        {"카드 봉인","Card sealed"},
        {"칸 봉인","Slot sealed"},
        {"{0}번 칸  +{1}%","Slot {0}  +{1}%"},
        {"{0}번 칸 보너스","Slot {0} bonus"},
        {"합성 → {0}성","Merge → {0}★"}
    };
}
