using System;
using System.Collections.Generic;

public enum CardEffect { Strike, Focus, MultiplyFocus, Amplify, Forge, Mark, Echo, Rhythm, Ultimate, MultiplyDamage, Delay=11, Fortune, Gamble, ChanceEcho }

public sealed class CardDefinition
{
    public int Id, Rarity, Art, Hits;
    public int BonusSlot;
    public double SlotBonus=1;
    public double Chance, ChanceGrowth=.05, LuckyScale=1, UnluckyScale=1;
    public string Name;
    public bool Iai, All, Consume;
    public bool Finisher, PierceGuard, ForgeFinisher;
    public double FocusGain, FocusRetention;
    public CardEffect Effect;
    public double Power, FocusRatio;
    public bool IsAttack => Effect == CardEffect.Strike || Effect == CardEffect.Ultimate;
    public bool IsSurvival => Effect==CardEffect.Delay;
    public bool IsChance => Chance>0;
    public double ChanceAt(int stars) => Math.Min(.95,Chance+ChanceGrowth*(stars-1));
    public string KindName => IsAttack?"공격":IsSurvival?"생존":"서포트";
    public double Value(int stars)
    {
        if(Effect==CardEffect.Delay) return stars>=3?2:1;
        bool multiplier=Effect==CardEffect.Amplify || Effect==CardEffect.MultiplyDamage || Effect==CardEffect.MultiplyFocus || Effect==CardEffect.Mark || Effect==CardEffect.Fortune || Effect==CardEffect.Gamble;
        return Power*(multiplier?new[]{1d,1d,1.2d,1.45d,1.75d}:new[]{1d,1d,1.8d,3d,5d})[stars];
    }
    public double Ratio(int stars) => FocusRatio*new[]{1d,1d,1.45d,1.9d,2.5d}[stars];
    public int Repeats(int stars) => stars>=3?2:1;
    public double RepeatPower(int stars) => new[]{0d,.6d,.8d,.75d,1d}[stars];
    public double ForgeScale(int stars) => ForgeFinisher?new[]{1d,4d,7d,10d,15d}[stars]:1;
    public bool HasSlotBonus => BonusSlot>0;
    public bool IsSlotBonusActive(int slot) => HasSlotBonus && slot==BonusSlot-1;
    public double SlotScale(int slot) => IsSlotBonusActive(slot)?SlotBonus:1;
    public string Description(int stars)
    {
        string text=BaseDescription(stars);
        if(IsAttack && IsChance)text+="\n성공 "+(ChanceAt(stars)*100).ToString("0")+"% ×"+LuckyScale.ToString("0.##")+" / 실패 ×"+UnluckyScale.ToString("0.##")+".";
        if(HasSlotBonus)text+="\n["+BonusSlot+"번 칸] "+(IsAttack?"피해":"효과")+" +"+((SlotBonus-1)*100).ToString("0")+"%.";
        return text;
    }
    string BaseDescription(int stars)
    {
        string v = GameMath.Number(Value(stars));
        switch (Effect)
        {
            case CardEffect.Focus: return "이번 턴 집중 +" + v + ".";
            case CardEffect.MultiplyFocus: return "집중 ×" + (1 + Value(stars) / 100).ToString("0.##") + ".\n동일 카드 중첩 불가.";
            case CardEffect.Amplify: return "이번 턴 피해 배율 +" + (Value(stars)/100).ToString("0.##") + ".";
            case CardEffect.MultiplyDamage: return "이번 턴 최종 피해 ×"+(1+Value(stars)/100).ToString("0.##")+".\n동일 카드 중첩 불가.";
            case CardEffect.Forge: return "이번 턴 타격 피해 +" + v + ".";
            case CardEffect.Mark: return "다음 공격 피해 ×" + (1 + Value(stars) / 100).ToString("0.##") + ".";
            case CardEffect.Echo: return "다음 공격 "+Repeats(stars)+"회 추가.\n추가 피해 "+(RepeatPower(stars)*100).ToString("0")+"%.";
            case CardEffect.Fortune: return "다음 공격 "+(ChanceAt(stars)*100).ToString("0")+"% 확률로 ×"+(1+Value(stars)/100).ToString("0.##")+".";
            case CardEffect.Gamble: return "이번 턴 "+(ChanceAt(stars)*100).ToString("0")+"% 확률로 ×"+(1+Value(stars)/100).ToString("0.##")+".\n실패 ×"+UnluckyScale.ToString("0.##")+". 턴당 1회.";
            case CardEffect.ChanceEcho: return "집중 +"+(20*stars)+".\n"+(ChanceAt(stars)*100).ToString("0")+"% 확률로 다음 공격 1회 추가.";
            case CardEffect.Rhythm: return "앞선 카드당 집중 +" + v + ".";
            case CardEffect.Delay: return "제한 턴 +"+v+". 집중 +"+(20*stars)+".\n웨이브당 최대 +2턴.";
            case CardEffect.Ultimate: return "모든 적에게 피해 " + v + ".\n집중 ×" + Ratio(stars).ToString("0.##") + " 추가·전부 소모.\n이번 턴 한정.";
            default:
                string s = (All ? "모든 적에게" : "적 1명에게") + " 피해 " + v + (Hits > 1 ? " × " + Hits : "") + ".";
                if (FocusRatio > 0) s += "\n집중 ×" + Ratio(stars).ToString("0.##") + " 추가·"+(Consume?(FocusRetention>0?"절반 보존.":"전부 소모."):"소모 없음.");
                if(FocusGain>0)s+="\n집중 +"+GameMath.Number(FocusGain*Value(stars)/Power)+".";
                if(Finisher)s+="\n체력 50% 이하에게 피해 ×2.";
                if(PierceGuard)s+="\n경화·후열 엄호 무시.";
                if(ForgeFinisher)s+="\n재련 추가 피해 ×"+ForgeScale(stars).ToString("0")+".";
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
        add("마력 파동",1,CardEffect.Strike,240,false,false,0,1,3);
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
        add("행운의 고리",1,CardEffect.Fortune,200,true,false,0,1,4);
        add("준비 작업",1,CardEffect.Forge,3000,true,false,0,1,1);
        add("연속 공격",2,CardEffect.Echo,0,false,false,0,1,2);
        add("거합 · 횡월",1,CardEffect.Strike,180,true,false,12.5,1,0);
        add("거합 · 연섬",2,CardEffect.Strike,225,true,false,12.5,3,0);
        add("잔심",2,CardEffect.Focus,360,true,false,0,1,5);
        add("응축",2,CardEffect.MultiplyFocus,280,true,false,0,1,4);
        add("종막의 물결",2,CardEffect.MultiplyDamage,100,false,false,0,1,3);
        add("거합 · 추월",3,CardEffect.Strike,600,true,false,10,1,0);
        add("거합 · 무심",3,CardEffect.Strike,1200,true,false,50,2,0);
        add("대거합",3,CardEffect.Ultimate,1800,true,true,90,1,0);
        add("찰나의 틈",1,CardEffect.Delay,1,false,false,0,1,2);
        add("승부수",2,CardEffect.Gamble,100,false,false,0,1,2);
        add("변덕의 검",1,CardEffect.Strike,240,false,false,0,1,0);
        add("메아리의 동전",1,CardEffect.ChanceEcho,0,false,false,0,1,2);
        a[0].FocusGain=9; a[8].Finisher=true; a[9].PierceGuard=true;
        a[11].ForgeFinisher=true;
        a[13].Consume=false; a[18].FocusRetention=.5; a[27].Consume=false;
        a[12].BonusSlot=1; a[12].SlotBonus=1.25;
        a[3].BonusSlot=2; a[3].SlotBonus=1.25;
        a[14].BonusSlot=3; a[14].SlotBonus=1.5;
        a[15].BonusSlot=3; a[15].SlotBonus=1.25;
        a[7].BonusSlot=4; a[7].SlotBonus=1.3;
        a[16].BonusSlot=5; a[16].SlotBonus=1.25;
        a[8].BonusSlot=6; a[8].SlotBonus=1.35;
        a[11].BonusSlot=6; a[11].SlotBonus=1.25;
        a[19].Chance=.3;
        a[31].Chance=.5; a[31].UnluckyScale=.8;
        a[32].Chance=.4; a[32].LuckyScale=3; a[32].UnluckyScale=.5;
        a[33].Chance=.4; a[33].ChanceGrowth=.1;
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

