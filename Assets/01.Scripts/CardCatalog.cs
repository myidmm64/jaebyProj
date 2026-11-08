using System;
using System.Collections.Generic;

public enum CardEffect { Strike, Focus, MultiplyFocus, Amplify, Forge, Mark, Echo, Rhythm, Ultimate, MultiplyDamage, Delay=11, Fortune, Gamble, ChanceEcho }

public enum CardTarget { Selected, Two, Rearmost }

public sealed class CardDefinition
{
    public int Id, Rarity, Art, Hits;
    public int BonusSlot;
    public double SlotBonus=1;
    public double Chance, ChanceGrowth=.05, LuckyScale=1, UnluckyScale=1;
    string sourceName;
    public string Name { get => CatalogText.Name(sourceName); set => sourceName=value; }
    public bool Iai, Consume;
    public CardTarget Target;
    public bool Finisher, PierceGuard, ForgeFinisher;
    public double FocusGain, FocusRetention;
    public CardEffect Effect;
    public double Power, FocusRatio;
    public bool IsAttack => Effect == CardEffect.Strike || Effect == CardEffect.Ultimate;
    public bool IsSurvival => Effect==CardEffect.Delay;
    public bool IsChance => Chance>0;
    public double ChanceAt(int stars) => Math.Min(.95,Chance+ChanceGrowth*(stars-1));
    public string KindName => IsAttack?CatalogText.Text("공격","Attack"):IsSurvival?CatalogText.Text("생존","Survival"):CatalogText.Text("서포트","Support");
    public double Value(int stars)
    {
        if(Effect==CardEffect.Delay) return stars>=3?2:1;
        bool multiplier=Effect==CardEffect.Amplify || Effect==CardEffect.MultiplyDamage || Effect==CardEffect.MultiplyFocus || Effect==CardEffect.Mark || Effect==CardEffect.Fortune || Effect==CardEffect.Gamble;
        return Power*(multiplier?new[]{1d,1d,1.2d,1.45d,1.75d}:new[]{1d,1d,1.8d,3d,5d})[stars];
    }
    public double Ratio(int stars) => FocusRatio*new[]{1d,1d,1.45d,1.9d,2.5d}[stars];
    public int Repeats(int stars) => stars>=3?2:1;
    public double RepeatPower(int stars) => new[]{0d,.6d,.8d,.75d,1d}[stars];
    public double ForgeScale(int stars) => ForgeFinisher?new[]{1d,2d,3d,4d,5d}[stars]:1;
    public bool HasSlotBonus => BonusSlot>0;
    public bool IsSlotBonusActive(int slot) => HasSlotBonus && slot==BonusSlot-1;
    public double SlotScale(int slot) => IsSlotBonusActive(slot)?SlotBonus:1;
    public string Description(int stars)
    {
        string text=BaseDescription(stars);
        if(IsAttack && IsChance)text+=CatalogText.Text("\n성공 {0:0}% ×{1:0.##} / 실패 ×{2:0.##}.","\n{0:0}%: ×{1:0.##}; otherwise ×{2:0.##}.",ChanceAt(stars)*100,LuckyScale,UnluckyScale);
        if(HasSlotBonus)text+=CatalogText.Text("\n[{0}번 칸] {1} +{2:0}%.","\n[Slot {0}] {1} +{2:0}%.",BonusSlot,IsAttack?CatalogText.Text("피해","damage"):CatalogText.Text("효과","effect"),(SlotBonus-1)*100);
        return text;
    }
    public string TargetName => Target==CardTarget.Two?CatalogText.Text("적 2명","2 enemies"):Target==CardTarget.Rearmost?CatalogText.Text("맨 뒤 적","the rearmost enemy"):CatalogText.Text("적 1명","1 enemy");
    string BaseDescription(int stars)
    {
        string v=GameMath.Number(Value(stars));
        switch(Effect)
        {
            case CardEffect.Focus: return CatalogText.Text("집중 +{0}.","Focus +{0}.",v);
            case CardEffect.MultiplyFocus: return CatalogText.Text("집중 ×{0:0.##} (동일 카드 중첩 불가).","Focus ×{0:0.##} (matching copies do not stack).",1+Value(stars)/100);
            case CardEffect.Amplify: return CatalogText.Text("이번 턴 피해 +{0:0.##}%.","Damage +{0:0.##}% this turn.",Value(stars));
            case CardEffect.MultiplyDamage: return CatalogText.Text("이번 턴 피해 ×{0:0.##} (동일 카드 중첩 불가).","Damage ×{0:0.##} this turn (matching copies do not stack).",1+Value(stars)/100);
            case CardEffect.Forge: return CatalogText.Text("이번 턴 타격 피해 +{0}.","Damage per hit +{0} this turn.",v);
            case CardEffect.Mark: return CatalogText.Text("다음 공격 ×{0:0.##}.","Next attack ×{0:0.##}.",1+Value(stars)/100);
            case CardEffect.Echo: return CatalogText.Text("다음 공격 {0}회 추가 ({1:0}%).","Repeat next attack {0} time(s) at {1:0}% damage.",Repeats(stars),RepeatPower(stars)*100);
            case CardEffect.Fortune: return CatalogText.Text("다음 공격 {0:0}% 확률로 ×{1:0.##}.","Next attack: {0:0}% chance of ×{1:0.##}.",ChanceAt(stars)*100,1+Value(stars)/100);
            case CardEffect.Gamble: return CatalogText.Text("이번 턴 {0:0}% 확률로 ×{1:0.##}.\n실패 ×{2:0.##} · 턴당 1회.","{0:0}%: damage ×{1:0.##} this turn.\nOtherwise ×{2:0.##}. Once per turn.",ChanceAt(stars)*100,1+Value(stars)/100,UnluckyScale);
            case CardEffect.ChanceEcho: return CatalogText.Text("집중 +{0}.\n{1:0}% 확률로 다음 공격 1회 추가.","Focus +{0}.\n{1:0}% chance to repeat the next attack.",20*stars,ChanceAt(stars)*100);
            case CardEffect.Rhythm: return CatalogText.Text("앞선 카드당 집중 +{0}.","Focus +{0} per preceding card.",v);
            case CardEffect.Delay: return CatalogText.Text("남은 턴 +{0} (최대 +2).\n집중 +{1}.","Turns +{0} (max +2).\nFocus +{1}.",v,20*stars);
            default:
                string text=CatalogText.Text("{0}에게 {1}{2} 피해.","Deal {1}{2} damage to {0}.",TargetName,v,Hits>1?" ×"+Hits:"");
                if(FocusRatio>0)text+=CatalogText.Text("\n집중 ×{0:0.##}","\nAdd Focus ×{0:0.##}",Ratio(stars))+
                    (Effect==CardEffect.Ultimate || Consume?(FocusRetention>0?CatalogText.Text(" 추가·절반 보존.","; keep half."):CatalogText.Text(" 추가·모두 소모.","; consume all.")):CatalogText.Text(" 추가·소모 없음.","; keep Focus."));
                if(FocusGain>0)text+=CatalogText.Text("\n집중 +{0}.","\nFocus +{0}.",GameMath.Number(FocusGain*Value(stars)/Power));
                if(Finisher)text+=CatalogText.Text("\n체력 절반 이하에게 ×2.","\n×2 against enemies at half HP or less.");
                if(PierceGuard)text+=CatalogText.Text("\n경화 무시.","\nIgnore Guard.");
                if(ForgeFinisher)text+=CatalogText.Text("\n재련 효과 ×{0:0}.","\nForge effect ×{0:0}.",ForgeScale(stars));
                if(Effect==CardEffect.Ultimate)text+=CatalogText.Text("\n이번 턴 한정.","\nExpires this turn.");
                return text;
        }
    }
}

public static class CardCatalog
{
    public static readonly CardDefinition[] All = Build();
    static CardDefinition[] Build()
    {
        var a = new List<CardDefinition>();
        Action<string,int,CardEffect,double,bool,CardTarget,double,int,int> add = (n,r,e,p,i,target,f,h,art) =>
            a.Add(new CardDefinition { Id=a.Count, Name=n, Rarity=r, Effect=e, Power=p,
                Iai=i, Target=target, FocusRatio=f, Consume=i && e==CardEffect.Strike && f>0, Hits=h, Art=art });
        add("올려베기",0,CardEffect.Strike,140,false,CardTarget.Selected,0,1,0);
        add("연속찌르기",0,CardEffect.Strike,60,false,CardTarget.Selected,0,3,0);
        add("휩쓸기",0,CardEffect.Strike,100,false,CardTarget.Two,0,1,0);
        add("재련",0,CardEffect.Forge,140,false,CardTarget.Selected,0,1,1);
        add("합동 공격!",1,CardEffect.MultiplyDamage,40,false,CardTarget.Selected,0,1,2);
        add("몰아치기",1,CardEffect.Strike,90,false,CardTarget.Selected,0,4,0);
        add("마력 파동",1,CardEffect.Strike,360,false,CardTarget.Rearmost,0,1,3);
        add("약점 간파",0,CardEffect.Mark,60,false,CardTarget.Selected,0,1,4);
        add("마무리 일격",1,CardEffect.Strike,650,false,CardTarget.Selected,0,1,0);
        add("정밀 타격",1,CardEffect.Strike,190,false,CardTarget.Rearmost,0,2,0);
        add("전투의 흐름",1,CardEffect.Rhythm,40,false,CardTarget.Selected,0,1,5);
        add("마력 폭풍",2,CardEffect.Strike,180,false,CardTarget.Two,0,2,3);
        add("집중",0,CardEffect.Focus,30,true,CardTarget.Selected,0,1,5);
        add("칼집 치기",0,CardEffect.Strike,130,true,CardTarget.Selected,2.5,1,0);
        add("호흡 정돈",0,CardEffect.Rhythm,16,true,CardTarget.Selected,0,1,5);
        add("거합 · 일섬",0,CardEffect.Strike,220,true,CardTarget.Selected,8,1,0);
        add("기합",1,CardEffect.Amplify,50,true,CardTarget.Selected,0,1,1);
        add("검의 호흡",1,CardEffect.MultiplyFocus,80,true,CardTarget.Selected,0,1,5);
        add("발도",1,CardEffect.Strike,320,true,CardTarget.Selected,6,1,0);
        add("행운의 고리",1,CardEffect.Fortune,200,true,CardTarget.Selected,0,1,4);
        add("준비 작업",1,CardEffect.Forge,480,true,CardTarget.Selected,0,1,1);
        add("연속 공격",2,CardEffect.Echo,0,false,CardTarget.Selected,0,1,2);
        add("거합 · 횡월",1,CardEffect.Strike,220,true,CardTarget.Two,5,1,0);
        add("거합 · 연섬",2,CardEffect.Strike,160,true,CardTarget.Selected,4,3,0);
        add("잔심",2,CardEffect.Focus,140,true,CardTarget.Selected,0,1,5);
        add("응축",2,CardEffect.MultiplyFocus,180,true,CardTarget.Selected,0,1,4);
        add("종막의 물결",2,CardEffect.MultiplyDamage,65,false,CardTarget.Selected,0,1,3);
        add("거합 · 추월",3,CardEffect.Strike,620,true,CardTarget.Rearmost,5,1,0);
        add("거합 · 무심",3,CardEffect.Strike,650,true,CardTarget.Selected,14,2,0);
        add("대거합",3,CardEffect.Ultimate,900,true,CardTarget.Two,20,1,0);
        add("찰나의 틈",1,CardEffect.Delay,1,false,CardTarget.Selected,0,1,2);
        add("승부수",2,CardEffect.Gamble,100,false,CardTarget.Selected,0,1,2);
        add("변덕의 검",1,CardEffect.Strike,300,false,CardTarget.Selected,0,1,0);
        add("메아리의 동전",1,CardEffect.ChanceEcho,0,false,CardTarget.Selected,0,1,2);
        a[0].FocusGain=12; a[8].Finisher=true; a[9].PierceGuard=true;
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
        if(GameLocalization.IsEnglish)
        {
            var culture=System.Globalization.CultureInfo.InvariantCulture;
            if(n>=1000000000)return (n/1000000000d).ToString("0.##",culture)+"B";
            if(n>=1000000)return (n/1000000d).ToString("0.##",culture)+"M";
            if(n>=1000)return (n/1000d).ToString("0.##",culture)+"K";
            return Math.Floor(n).ToString("N0",culture);
        }
        if (n >= 100000000) return (n/100000000d).ToString("0.##") + "억";
        if (n >= 10000) return (n/10000d).ToString("0.##") + "만";
        return Math.Floor(n).ToString("N0");
    }
}

