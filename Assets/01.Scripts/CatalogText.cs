using System;
using System.Collections.Generic;
using System.Globalization;

// Catalog IDs and saved data stay language independent; only their presentation changes.
public static class CatalogText
{
    static readonly Dictionary<string,string> EnglishNames = new Dictionary<string,string>
    {
        {"올려베기","Rising Slash"}, {"연속찌르기","Rapid Thrusts"}, {"휩쓸기","Sweep"},
        {"재련","Forge"}, {"합동 공격!","Combined Assault!"}, {"몰아치기","Flurry"},
        {"마력 파동","Arcane Wave"}, {"약점 간파","Expose Weakness"}, {"마무리 일격","Finishing Blow"},
        {"정밀 타격","Precision Strike"}, {"전투의 흐름","Battle Flow"}, {"마력 폭풍","Arcane Storm"},
        {"집중","Focus"}, {"칼집 치기","Scabbard Strike"}, {"호흡 정돈","Steady Breath"},
        {"거합 · 일섬","Iai: Flash"}, {"기합","Fighting Spirit"}, {"검의 호흡","Sword Breathing"},
        {"발도","Quickdraw"}, {"행운의 고리","Lucky Ring"}, {"준비 작업","Preparation"},
        {"연속 공격","Follow-up"}, {"거합 · 횡월","Iai: Crescent"}, {"거합 · 연섬","Iai: Twin Flash"},
        {"잔심","Zanshin"}, {"응축","Condense"}, {"종막의 물결","Final Wave"},
        {"거합 · 추월","Iai: Moonchaser"}, {"거합 · 무심","Iai: No Mind"}, {"대거합","Grand Iai"},
        {"찰나의 틈","Brief Opening"}, {"승부수","All In"}, {"변덕의 검","Fickle Blade"},
        {"메아리의 동전","Echo Coin"},
        {"슬라임","Slime"}, {"돌 정령","Stone Spirit"}, {"가시 슬라임","Thorn Slime"},
        {"포자 슬라임","Spore Slime"}, {"방울 슬라임","Bubble Slime"}, {"철갑 조약돌","Iron Pebble"},
        {"균열 정령","Rift Spirit"}, {"마력 반딧불","Arcane Firefly"}, {"새싹 정령","Sprout Spirit"},
        {"밤그늘 버섯","Nightshade Shroom"}, {"공허 나방","Void Moth"}, {"수정 고슴도치","Crystal Hedgehog"},
        {"초원의 심장","Heart of the Meadow"}, {"균열의 파수꾼","Rift Warden"}, {"공허의 학장","Void Headmaster"},
        {"카드 1장","one card"}
    };
    static readonly Dictionary<string,string> KoreanNames = BuildReverseNames();
    static Dictionary<string,string> BuildReverseNames()
    {
        var result=new Dictionary<string,string>();
        foreach(var pair in EnglishNames)result[pair.Value]=pair.Key;
        return result;
    }
    public static string Name(string value)
    {
        if(string.IsNullOrEmpty(value))return value;
        string translated;
        if(GameLocalization.IsEnglish)return EnglishNames.TryGetValue(value,out translated)?translated:value;
        return KoreanNames.TryGetValue(value,out translated)?translated:value;
    }
    public static string Text(string korean,string english,params object[] args)
    {
        string value=GameLocalization.IsEnglish?english:korean;
        return args.Length==0?value:string.Format(CultureInfo.InvariantCulture,value,args);
    }
}
