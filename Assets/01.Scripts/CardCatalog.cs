using System;
using System.Collections.Generic;

public enum CardEffect { Strike, Focus, MultiplyFocus, Amplify, Forge, Mark, Echo, Rhythm, Ultimate, MultiplyDamage, Delay=11 }

public sealed class CardDefinition
{
    public int Id, Rarity, Art, Hits;
    public string Name;
    public bool Iai, All, Consume;
    public bool Finisher, PierceGuard;
    public double FocusGain, FocusRetention;
    public CardEffect Effect;
    public double Power, FocusRatio;
    public bool IsAttack => Effect == CardEffect.Strike || Effect == CardEffect.Ultimate;
    public bool IsSurvival => Effect==CardEffect.Delay;
    public string KindName => IsAttack?"공격":IsSurvival?"생존":"서포트";
    public double Value(int stars)
    {
        if(Effect==CardEffect.Delay) return stars>=3?2:1;
        bool multiplier=Effect==CardEffect.Amplify || Effect==CardEffect.MultiplyDamage || Effect==CardEffect.MultiplyFocus || Effect==CardEffect.Mark;
        return Power*(multiplier?new[]{1d,1d,1.25d,1.6d,2d}:new[]{1d,1d,2d,3.5d,6d})[stars];
    }
    public double Ratio(int stars) => FocusRatio*new[]{1d,1d,1.6d,2.2d,3d}[stars];
    public int Repeats(int stars) => stars>=3?2:1;
    public string Description(int stars)
    {
        string v = GameMath.Number(Value(stars));
        switch (Effect)
        {
            case CardEffect.Focus: return "집중 +" + v + ".\n집중은 이번 턴에만 유지됩니다.";
            case CardEffect.MultiplyFocus: return "현재 집중 ×" + (1 + Value(stars) / 100).ToString("0.##") + ".\n같은 카드의 배율은 높은 성급 1장만 적용합니다.";
            case CardEffect.Amplify: return "이번 턴 피해 배율 +" + (Value(stars)/100).ToString("0.##") + ".\n다른 배율 증가와 더한 뒤 곱연산을 적용합니다.";
            case CardEffect.MultiplyDamage: return "이번 턴 최종 피해 ×"+(1+Value(stars)/100).ToString("0.##")+".\n같은 카드는 최고 배율만, 다른 카드는 곱합니다.";
            case CardEffect.Forge: return "이번 턴 모든 타격의 기본 피해 +" + v + ".";
            case CardEffect.Mark: return "다음 공격의 피해 " + (1 + Value(stars) / 100).ToString("0.##") + "배.\n공격 후 효과가 사라집니다.";
            case CardEffect.Echo: return "다음 공격 "+Repeats(stars)+"회 추가 발동.\n반복은 최대 2회. 집중은 한 번만 소모합니다.";
            case CardEffect.Rhythm: return "앞서 실행한 카드 1장당\n집중 +" + v + ".";
            case CardEffect.Delay: return "이번 웨이브 제한 턴 +"+v+".\n카드로 늘리는 턴은 웨이브당 최대 2턴입니다.";
            case CardEffect.Ultimate: return "모든 적에게 피해 " + v + ".\n집중 ×" + Ratio(stars).ToString("0.##") + " 추가.\n집중 전부 소모 · 이번 턴 한정.";
            default:
                string s = (All ? "모든 적에게" : "선택한 적에게") + " 피해 " + v + (Hits > 1 ? " × " + Hits : "") + ".";
                if (FocusRatio > 0) s += "\n집중 ×" + Ratio(stars).ToString("0.##") + " 추가.";
                if (Consume) s += FocusRetention>0?"\n공격 후 집중 절반을 남깁니다.":"\n집중을 전부 소모합니다.";
                else if(FocusRatio>0)s+="\n집중을 소모하지 않습니다.";
                if(FocusGain>0)s+="\n집중 +"+GameMath.Number(FocusGain*Value(stars)/Power)+".";
                if(Finisher)s+="\n체력이 절반 이하인 적에게 피해 ×2.";
                if(PierceGuard)s+="\n적의 경화를 무시합니다.";
                return s;
        }
    }
}

public static class CardCatalog
{
    public static readonly CardDefinition[] All = Build();
    static CardDefinition[] Build()
    {
        var a = new List<CardDefinition>();
        Action<string,int,CardEffect,double,bool,bool,double,int,int> add = (n,r,e,p,i,all,f,h,art) =>
            a.Add(new CardDefinition { Id=a.Count, Name=n, Rarity=r, Effect=e, Power=p,
                Iai=i, All=all, FocusRatio=f, Consume=i && e==CardEffect.Strike && f>0, Hits=h, Art=art });
        add("올려베기",0,CardEffect.Strike,54,false,false,0,1,0);
        add("연속찌르기",0,CardEffect.Strike,24,false,false,0,3,0);
        add("휩쓸기",0,CardEffect.Strike,48,false,true,0,1,0);
        add("재련",0,CardEffect.Forge,600,false,false,0,1,1);
        add("합동 공격!",1,CardEffect.MultiplyDamage,40,false,false,0,1,2);
        add("몰아치기",1,CardEffect.Strike,48,false,false,0,4,0);
        add("마력 파동",1,CardEffect.Strike,120,false,true,0,1,3);
        add("약점 간파",0,CardEffect.Mark,90,false,false,0,1,4);
        add("마무리 일격",1,CardEffect.Strike,390,false,false,0,1,0);
        add("정밀 타격",1,CardEffect.Strike,174,false,false,0,2,0);
        add("전투의 흐름",1,CardEffect.Rhythm,54,false,false,0,1,5);
        add("마력 폭풍",2,CardEffect.Strike,300,false,true,0,3,3);
        add("집중",0,CardEffect.Focus,36,true,false,0,1,5);
        add("칼집 치기",0,CardEffect.Strike,48,true,false,1.5,1,0);
        add("호흡 정돈",0,CardEffect.Rhythm,18,true,false,0,1,5);
        add("거합 · 일섬",0,CardEffect.Strike,96,true,false,15,1,0);
        add("기합",1,CardEffect.Amplify,90,true,false,0,1,1);
        add("검의 호흡",1,CardEffect.MultiplyFocus,140,true,false,0,1,5);
        add("발도",1,CardEffect.Strike,165,true,false,10,1,0);
        add("집중의 고리",1,CardEffect.Focus,165,true,false,0,1,4);
        add("준비 작업",1,CardEffect.Forge,3000,true,false,0,1,1);
        add("연속 공격",2,CardEffect.Echo,0,false,false,0,1,2);
        add("거합 · 횡월",1,CardEffect.Strike,180,true,true,12.5,1,0);
        add("거합 · 연섬",2,CardEffect.Strike,225,true,false,12.5,3,0);
        add("잔심",2,CardEffect.Focus,360,true,false,0,1,5);
        add("응축",2,CardEffect.MultiplyFocus,280,true,false,0,1,4);
        add("종막의 물결",2,CardEffect.MultiplyDamage,100,false,false,0,1,3);
        add("거합 · 추월",3,CardEffect.Strike,600,true,true,10,1,0);
        add("거합 · 무심",3,CardEffect.Strike,1200,true,true,50,2,0);
        add("대거합",3,CardEffect.Ultimate,3000,true,true,160,1,0);
        add("찰나의 틈",1,CardEffect.Delay,1,false,false,0,1,2);
        a[0].FocusGain=9; a[8].Finisher=true; a[9].PierceGuard=true;
        a[13].Consume=false; a[18].FocusRetention=.5; a[27].Consume=false;
        return a.ToArray();
    }
}

public static class GameMath
{
    public const double Max = 999999999999d;
    public static double Clamp(double n) => Math.Max(0, Math.Min(Max, n));
    public static long Damage(double n) => (long)Math.Ceiling(Clamp(n));
    public static string Number(double n)
    {
        if (n >= 100000000) return (n/100000000d).ToString("0.##") + "억";
        if (n >= 10000) return (n/10000d).ToString("0.##") + "만";
        return Math.Floor(n).ToString("N0");
    }
}

public static class ItemCatalog
{
    public static readonly string[] Names = { "심호흡의 서", "여우의 가면", "붉은 열매", "졸업 부적", "시간의 물약", "새싹 매듭" };
    public static readonly string[] Descriptions = {
        "집중 획득량 +30%. 중첩 시 더합니다.", "최종 피해 +35%. 중첩 시 더합니다.",
        "웨이브 시작 집중 +60 × 현재 층.", "모든 공격 피해 +20%. 중첩 시 더합니다.",
        "웨이브 제한 턴 +1. 아이템 연장은 최대 2턴.", "웨이브마다 궁극기 +20. 보상 골드 +25%." };
}
