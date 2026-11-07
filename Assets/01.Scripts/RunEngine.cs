using System;
using System.Collections.Generic;
using System.Linq;

// All progression and combat rules live here; the scene only presents their results.
public sealed class RunEngine
{
    public RunState State { get; private set; }
    public bool IsBoss => State.Node==4;
    public int CombatIndex => State.Node < 2 ? State.Node : 2;
    public int TurnLimit => (IsBoss ? 10 : 7) + Math.Min(2,State.Items[4])+State.BonusTurns-State.LostTurns;
    public bool Cleared => State.Enemies.All(e=>e.Health<=0);
    public List<OwnedCard> BagCards => State.Cards.Where(c=>Array.IndexOf(State.Sequence,c.Uid)<0).ToList();
    public bool CanExecute => State.Phase==RunPhase.Battle && State.Sequence.Where((id,slot)=>slot!=State.SealedSlot).Any(id=>State.Find(id)!=null);
    public RunEngine(RunState saved)
    {
        MigrateSavedState(saved); State=saved;
        if(State.OfferStars==null || State.OfferStars.Length!=3)State.OfferStars=new[]{1,1,1};
        for(int i=0;i<3;i++)State.OfferStars[i]=Math.Max(1,Math.Min(4,State.OfferStars[i]));
        if(State.Phase==RunPhase.Battle && State.Enemies.Any(e=>e.Intent==EnemyIntent.None)) { State.SealedSlot=-1; PlanIntents(); }
    }
    public static RunEngine New(int seed)
    {
        var e=new RunEngine(new RunState { Seed=seed, RandomState=seed });
        foreach(int id in new[]{0,0,12,12,15,2,7,3}) e.AddCard(id);
        e.State.Sequence=new[]{3,5,1,-1,-1,-1};
        e.BeginWave();
        return e;
    }
    int Roll(int max)
    {
        State.RandomState=unchecked(State.RandomState*1664525+1013904223);
        return (int)((uint)State.RandomState%(uint)max);
    }
    public static void MigrateSavedState(RunState s)
    {
        if(s.Version>=2)return;
        var removed=s.Cards.Where(c=>c.Definition==30 || c.Definition==32).ToArray();
        foreach(var card in removed)
        {
            s.Gold+=(card.Definition==30?6:12)*(1<<(card.Stars-1));
            for(int i=0;i<s.Sequence.Length;i++)if(s.Sequence[i]==card.Uid)s.Sequence[i]=-1;
            s.Cards.Remove(card);
        }
        foreach(var card in s.Cards)if(card.Definition==31)card.Definition=30;
        if(s.Phase==RunPhase.CardReward || s.Phase==RunPhase.Shop)
            for(int i=0;i<s.Offers.Length;i++)s.Offers[i]=s.Offers[i]==31?30:(s.Offers[i]==30 || s.Offers[i]==32?12:s.Offers[i]);
        if(s.Cards.Count==0)s.Cards.Add(new OwnedCard(s.NextUid++,0));
        foreach(var enemy in s.Enemies)
        {
            double fraction=enemy.MaxHealth>0?enemy.Health/(double)enemy.MaxHealth:1;
            enemy.MaxHealth=EnemyHealth(s,enemy.Kind); enemy.Health=GameMath.Damage(enemy.MaxHealth*fraction);
        }
        s.Version=2;
    }
    static long EnemyHealth(RunState s,int kind)
    {
        if(s.Node==4)return GameMath.Damage(new[]{0d,220000d,18000000d,240000000d}[s.Floor]);
        int combat=s.Node<2?s.Node:2;
        return GameMath.Damage(EnemyCatalog.Get(kind).Health*new[]{1d,70d,2500d}[s.Floor-1]*Math.Pow(1.5,combat)*Math.Pow(1.35,s.Wave));
    }
    static double StrongestMultiplier(System.Collections.Generic.Dictionary<int,double> values,int id,double amount)
    {
        double previous=values.TryGetValue(id,out var old)?old:0;
        double next=Math.Max(previous,amount); values[id]=next;
        return (1+next)/(1+previous);
    }
    public OwnedCard AddCard(int id, int stars=1)
    {
        var card=new OwnedCard(State.NextUid++,id,stars); State.Cards.Add(card); return card;
    }
    public bool Equip(int uid,int slot)
    {
        var card=State.Find(uid);
        if(State.Phase!=RunPhase.Battle || slot<0 || slot>=6 || slot==State.SealedSlot || card==null) return false;
        int old=Array.IndexOf(State.Sequence,uid);
        if(old>=0) State.Sequence[old]=State.Sequence[slot];
        State.Sequence[slot]=uid; return true;
    }
    public void Unequip(int slot)
    {
        if(slot<0 || slot>=6) return;
        State.Sequence[slot]=-1;
    }
    public bool PlaceInBag(int uid,int slot)
    {
        if(State.Phase!=RunPhase.Battle || slot<0) return false;
        var card=State.Find(uid); if(card==null) return false;
        for(int i=0;i<State.Sequence.Length;i++) if(State.Sequence[i]==uid) State.Sequence[i]=-1;
        State.Cards.Remove(card);
        var bag=State.Cards.Where(c=>Array.IndexOf(State.Sequence,c.Uid)<0).ToList();
        slot=Math.Min(slot,bag.Count);
        int index=slot<bag.Count?State.Cards.IndexOf(bag[slot]):bag.Count>0?State.Cards.IndexOf(bag[bag.Count-1])+1:State.Cards.Count;
        State.Cards.Insert(index,card);
        return true;
    }
    public bool MoveCard(int uid,int targetUid)
    {
        if(Array.IndexOf(State.Sequence,uid)>=0 || Array.IndexOf(State.Sequence,targetUid)>=0) return false;
        int from=State.Cards.FindIndex(c=>c.Uid==uid), to=State.Cards.FindIndex(c=>c.Uid==targetUid);
        if(from<0 || to<0 || from==to) return false;
        var card=State.Cards[from]; State.Cards.RemoveAt(from); State.Cards.Insert(to,card);
        return true;
    }
    public bool CanMerge(int sourceUid,int targetUid)
    {
        var source=State.Find(sourceUid); var target=State.Find(targetUid);
        return source!=null && target!=null && sourceUid!=targetUid && !source.Temporary && !target.Temporary && source.Definition==target.Definition && source.Stars==target.Stars && target.Stars<4;
    }
    public bool MergeInto(int sourceUid,int targetUid)
    {
        if(!CanMerge(sourceUid,targetUid)) return false;
        State.Find(targetUid).Stars++;
        for(int i=0;i<6;i++) if(State.Sequence[i]==sourceUid) State.Sequence[i]=-1;
        State.Cards.Remove(State.Find(sourceUid));
        return true;
    }
    public int MergeAll()
    {
        int count=0;
        while(true)
        {
            var pair=State.Cards.Where(c=>!c.Temporary && c.Stars<4)
                .GroupBy(c=>c.Definition+":"+c.Stars).FirstOrDefault(g=>g.Count()>=2);
            if(pair==null) break;
            var keep=pair.First(); var remove=pair.Skip(1).First();
            MergeInto(remove.Uid,keep.Uid); count++;
        }
        return count;
    }
    public int SellPrice(OwnedCard c) => (6+c.Data.Rarity*6)*(1<<(c.Stars-1));
    public bool Sell(int uid)
    {
        var c=State.Find(uid);
        if(c==null || c.Temporary || State.Cards.Count(x=>!x.Temporary)<=1) return false;
        State.Gold+=SellPrice(c);
        for(int i=0;i<6;i++) if(State.Sequence[i]==uid) State.Sequence[i]=-1;
        State.Cards.Remove(c); return true;
    }
    public bool Ultimate()
    {
        if(State.Phase!=RunPhase.Battle || State.Ultimate<100 || State.Cards.Any(c=>c.Temporary)) return false;
        State.Ultimate=0;
        var c=AddCard(29,Math.Min(4,State.Floor)); c.Temporary=true;
        for(int slot=0;slot<State.Sequence.Length;slot++)
            if(State.Sequence[slot]<0 && slot!=State.SealedSlot) { Equip(c.Uid,slot); break; }
        return true;
    }
    public CardResult Resolve(OwnedCard card,TurnContext turn)
    {
        var s=State; var d=card.Data;
        var result=new CardResult { Card=card, FocusBefore=s.Focus };
        double value=d.Value(card.Stars);
        switch(d.Effect)
        {
            case CardEffect.Focus: GainFocus(value); break;
            case CardEffect.Rhythm: GainFocus(value*turn.Executed); break;
            case CardEffect.MultiplyFocus: s.Focus=GameMath.Clamp(s.Focus*StrongestMultiplier(turn.FocusMultipliers,d.Id,value/100)); break;
            case CardEffect.Amplify:
                turn.Additive+=value/100; break;
            case CardEffect.MultiplyDamage: turn.Multiplier*=StrongestMultiplier(turn.DamageMultipliers,d.Id,value/100); break;
            case CardEffect.Delay: s.BonusTurns=Math.Min(2,s.BonusTurns+(int)value); break;
            case CardEffect.Forge: turn.Flat=GameMath.Clamp(turn.Flat+value); break;
            case CardEffect.Mark: turn.Mark+=value/100; break;
            case CardEffect.Echo: turn.Echo=Math.Max(turn.Echo,d.Repeats(card.Stars)); break;
            default:
                double mult=(1+turn.Additive)*turn.Multiplier*(1+s.Items[1]*.35)*(1+s.Items[3]*.2)*turn.Mark*(s.Weakened?.75:1);
                long damage=GameMath.Damage((value+turn.Flat+s.Focus*d.Ratio(card.Stars))*mult);
                for(int hit=0;hit<d.Hits*(1+turn.Echo);hit++)
                {
                    if(d.All)
                    {
                        for(int i=0;i<s.Enemies.Count;i++) Hit(i,damage,result,d);
                    }
                    else
                    {
                        int target=s.Target;
                        if(target<0 || target>=s.Enemies.Count || s.Enemies[target].Health<=0) target=s.Enemies.FindIndex(e=>e.Health>0);
                        if(target>=0) Hit(target,damage,result,d);
                    }
                }
                if(d.Consume || d.Effect==CardEffect.Ultimate) s.Focus*=d.FocusRetention;
                if(d.FocusGain>0)GainFocus(d.FocusGain*value/d.Power);
                turn.Echo=0; turn.Mark=1;
                break;
        }
        turn.Executed++; s.CardsPlayed++;
        s.Ultimate=Math.Min(100,s.Ultimate+4+(d.Iai?1:0));
        result.FocusAfter=s.Focus;
        return result;
    }
    void GainFocus(double n) { State.Focus=GameMath.Clamp(State.Focus+n*(1+State.Items[0]*.3)*(State.FocusDisrupted?.5:1)); }
    void Hit(int index,long damage,CardResult result,CardDefinition card)
    {
        var enemy=State.Enemies[index]; if(enemy.Health<=0) return;
        if(card.Finisher && enemy.Health<=enemy.MaxHealth*.5) damage=GameMath.Damage(damage*2d);
        if(enemy.Guarding && !card.PierceGuard) damage=GameMath.Damage(damage*.6);
        enemy.Health=Math.Max(0,enemy.Health-damage);
        result.Damage=(long)Math.Min(GameMath.Max,result.Damage+(double)damage);
        if(!result.Targets.Contains(index)) result.Targets.Add(index);
        State.TotalDamage=GameMath.Clamp(State.TotalDamage+damage);
        State.BestHit=Math.Max(State.BestHit,damage);
    }
    public void EndTurn()
    {
        var s=State; s.TurnsPlayed++; s.Focus=0;
        foreach(var c in s.Cards.Where(c=>c.Temporary).ToArray())
        {
            for(int i=0;i<6;i++) if(s.Sequence[i]==c.Uid) s.Sequence[i]=-1;
            s.Cards.Remove(c);
        }
        if(Cleared)
        {
            s.WavesCleared++; s.Gold+=(int)((15+s.Floor*8)*(1+s.Items[5]*.25));
            s.Phase=RunPhase.CardReward; s.Offers=CardOffers();
        }
        else if(s.Turn>=TurnLimit) s.Phase=RunPhase.Defeat;
        else
        {
            s.Weakened=false; s.FocusDisrupted=false; s.SealedSlot=-1;
            foreach(var enemy in s.Enemies) enemy.Guarding=false;
            foreach(var enemy in s.Enemies.Where(e=>e.Health>0))
            {
                switch(enemy.Intent)
                {
                    case EnemyIntent.Weaken: s.Weakened=true; break;
                    case EnemyIntent.Guard: enemy.Guarding=true; break;
                    case EnemyIntent.Seal: s.SealedSlot=enemy.IntentSlot; break;
                    case EnemyIntent.DisruptFocus: s.FocusDisrupted=true; break;
                    case EnemyIntent.SiphonUltimate: s.Ultimate=Math.Max(0,s.Ultimate-15); break;
                    case EnemyIntent.Pressure: s.LostTurns=Math.Min(2,s.LostTurns+1); break;
                    case EnemyIntent.Regenerate: enemy.Health=Math.Min(enemy.MaxHealth,enemy.Health+GameMath.Damage(enemy.MaxHealth*(IsBoss?.02:.04))); break;
                }
            }
            if(s.Turn>=TurnLimit)s.Phase=RunPhase.Defeat;
            else { s.Turn++; PlanIntents(); }
            if(s.Target>=s.Enemies.Count || s.Enemies[s.Target].Health<=0) s.Target=s.Enemies.FindIndex(e=>e.Health>0);
        }
    }
    public long Preview(OwnedCard card)
    {
        // A real dry-run includes every preceding support card, in sequence order.
        var backup=State;
        var clone=new RunState { Focus=backup.Focus, Items=(int[])backup.Items.Clone(), Target=backup.Target, Weakened=backup.Weakened, FocusDisrupted=backup.FocusDisrupted, SealedSlot=backup.SealedSlot, BonusTurns=backup.BonusTurns };
        foreach(var e in backup.Enemies) clone.Enemies.Add(new EnemyState {Health=e.Health, MaxHealth=e.MaxHealth, Guarding=e.Guarding});
        State=clone;
        try
        {
            var t=new TurnContext();
            for(int i=0;i<backup.Sequence.Length;i++)
            {
                if(i==State.SealedSlot) continue;
                var c=backup.Find(backup.Sequence[i]); if(c==null) continue;
                var r=Resolve(c,t); if(c.Uid==card.Uid) return r.Damage;
            }
            return Resolve(card,t).Damage;
        }
        finally { State=backup; }
    }
    int[] CardOffers()
    {
        var result=new List<int>();
        State.OfferStars=new[]{1,1,1};
        int tier=IsBoss?Math.Min(3,State.Floor+1):State.Floor;
        var duplicate=State.Cards.Where(c=>!c.Temporary && c.Stars<4 && c.Stars<=Math.Min(2,tier)).ToArray();
        if(duplicate.Length>0)
        {
            var card=duplicate[Roll(duplicate.Length)]; result.Add(card.Definition); State.OfferStars[0]=card.Stars;
        }
        int[][] weights={new[]{60,30,9,1},new[]{25,45,26,4},new[]{5,35,45,15}};
        while(result.Count<3)
        {
            int id=DrawCard(weights[tier-1],result);
            State.OfferStars[result.Count]=tier==1?1:tier==2?(Roll(100)<60?2:1):(Roll(100)<12?3:2);
            result.Add(id);
        }
        return result.ToArray();
    }
    int DrawCard(int[] weights,List<int> excluded)
    {
        int roll=Roll(100), grade=0;
        for(;grade<3;grade++) { if(roll<weights[grade])break; roll-=weights[grade]; }
        var pool=CardCatalog.All.Where(c=>c.Id!=29 && c.Rarity==grade && !excluded.Contains(c.Id)).ToArray();
        if(pool.Length==0)pool=CardCatalog.All.Where(c=>c.Id!=29 && !excluded.Contains(c.Id)).ToArray();
        return pool[Roll(pool.Length)].Id;
    }
    public int RewardCopies => 1;
    public int RewardGold => (12+State.Floor*4)*RewardCopies;
    public bool ChooseCard(int index)
    {
        if(State.Phase!=RunPhase.CardReward) return false;
        if(index>=0)
        {
            if(index>=3 || State.Cards.Count+RewardCopies>16) return false;
            for(int n=0;n<RewardCopies;n++)AddCard(State.Offers[index],State.OfferStars[index]);
        }
        else State.Gold+=RewardGold;
        if(IsBoss || State.Wave==2)
        {
            State.Phase=RunPhase.ItemReward;
            var ids=new List<int>(); while(ids.Count<3) { int id=Roll(6); if(!ids.Contains(id)) ids.Add(id); }
            State.Offers=ids.ToArray();
        }
        else { State.Wave++; BeginWave(); }
        return true;
    }
    public void ChooseItem(int index)
    {
        if(State.Phase!=RunPhase.ItemReward || index<0 || index>=3) return;
        State.Items[State.Offers[index]]++;
        if(IsBoss)
        {
            if(State.Floor==3) { State.Phase=RunPhase.Victory; return; }
            State.Floor++; State.Node=0;
        }
        else State.Node++;
        State.Wave=0;
        if(State.Node==2) EnterShop(); else BeginWave();
    }
    void EnterShop()
    {
        State.Phase=RunPhase.Shop; State.Offers=ShopOffers(); State.Bought=new bool[3]; State.ShopMerged=false; State.ShopRerolls=0;
    }
    int[] ShopOffers()
    {
        int[][] weights={new[]{10,55,30,5},new[]{5,45,40,10},new[]{0,30,50,20}};
        var offers=new List<int>();
        int stars=Math.Min(2,State.Floor); State.OfferStars=new[]{stars,stars,stars};
        while(offers.Count<3)
        {
            offers.Add(DrawCard(weights[State.Floor-1],offers));
        }
        if(offers.All(id=>CardCatalog.All[id].Rarity<2))
        {
            var gold=CardCatalog.All.Where(c=>c.Rarity==2 && !offers.Contains(c.Id)).ToArray();
            offers[2]=gold[Roll(gold.Length)].Id;
        }
        return offers.ToArray();
    }
    public int RerollPrice => 10+State.ShopRerolls*5;
    public bool RerollShop()
    {
        if(State.Phase!=RunPhase.Shop || State.Gold<RerollPrice) return false;
        State.Gold-=RerollPrice; State.ShopRerolls++;
        State.Offers=ShopOffers(); State.Bought=new bool[3]; return true;
    }
    public int ShopPrice(int index) => (int)((25+CardCatalog.All[State.Offers[index]].Rarity*20)*(1+(State.OfferStars[index]-1)*.65));
    public bool Buy(int index)
    {
        if(State.Phase!=RunPhase.Shop || index<0 || index>=3 || State.Bought[index] || State.Cards.Count>=16 || State.Gold<ShopPrice(index)) return false;
        State.Gold-=ShopPrice(index); AddCard(State.Offers[index],State.OfferStars[index]); State.Bought[index]=true; return true;
    }
    public bool BuyUpgrade(int uid)
    {
        var c=State.Find(uid); int price=40+(c?.Stars??1)*20;
        if(State.Phase!=RunPhase.Shop || State.ShopMerged || c==null || c.Stars>=4 || State.Gold<price) return false;
        State.Gold-=price; c.Stars++; State.ShopMerged=true; return true;
    }
    public void LeaveShop() { if(State.Phase==RunPhase.Shop) { State.Node=3; BeginWave(); } }
    public void BeginWave()
    {
        var s=State; s.Phase=RunPhase.Battle; s.Turn=1; s.Focus=State.Items[2]*60*s.Floor; s.Target=0;
        s.Weakened=false; s.FocusDisrupted=false; s.SealedSlot=-1; s.BonusTurns=0; s.LostTurns=0;
        s.Ultimate=Math.Min(100,s.Ultimate+s.Items[5]*20);
        s.Enemies.Clear();
        if(IsBoss)
        {
            int kind=new[]{0,5,9,11}[s.Floor]; long hp=EnemyHealth(s,kind);
            s.Enemies.Add(new EnemyState { Name=new[]{"","초원의 심장","균열의 파수꾼","공허의 학장"}[s.Floor],Kind=kind,Health=hp,MaxHealth=hp });
        }
        else
        {
            int count=s.Wave==2?3:2;
            for(int i=0;i<count;i++)
            {
                var pool=EnemyCatalog.Pools[s.Floor-1];
                int kind=s.Floor==1 && s.Node==0 && s.Wave==0?i:pool[(int)(((uint)s.Seed+(uint)(s.Node*3+s.Wave*2+i))%(uint)pool.Length)];
                long hp=EnemyHealth(s,kind);
                s.Enemies.Add(new EnemyState { Name=EnemyCatalog.Get(kind).Name, Kind=kind, Health=hp, MaxHealth=hp });
            }
        }
        PlanIntents();
    }
    void PlanIntents()
    {
        for(int i=0;i<State.Enemies.Count;i++)
        {
            var enemy=State.Enemies[i];
            var pattern=IsBoss?new[]{EnemyIntent.Weaken,EnemyIntent.Guard,EnemyIntent.Seal,EnemyIntent.SiphonUltimate,EnemyIntent.Regenerate}:EnemyCatalog.Get(enemy.Kind).Pattern;
            enemy.Intent=pattern[(State.Turn-1+i)%pattern.Length];
            if(enemy.Intent==EnemyIntent.Pressure && State.LostTurns>=2)enemy.Intent=EnemyIntent.Weaken;
            enemy.IntentSlot=(State.Turn+i)%6;
        }
    }
    public static string IntentName(EnemyState enemy)
    {
        switch(enemy.Intent)
        {
            case EnemyIntent.Weaken: return "공격 약화";
            case EnemyIntent.Guard: return "경화";
            case EnemyIntent.Seal: return (enemy.IntentSlot+1)+"번 칸 봉인";
            case EnemyIntent.DisruptFocus: return "집중 방해";
            case EnemyIntent.SiphonUltimate: return "마력 흡수";
            case EnemyIntent.Pressure: return "시간 압박";
            case EnemyIntent.Regenerate: return "재생";
            default: return "대기";
        }
    }
    public static string IntentDescription(EnemyState enemy)
    {
        switch(enemy.Intent)
        {
            case EnemyIntent.Weaken: return "생존하면 다음 턴 아군의 공격 피해가 25% 감소합니다. 중첩되지 않습니다.";
            case EnemyIntent.Guard: return "생존하면 다음 턴 이 적이 받는 피해가 40% 감소합니다.";
            case EnemyIntent.Seal: return "생존하면 다음 턴 "+(enemy.IntentSlot+1)+"번 시퀀스 칸을 사용할 수 없습니다. 카드는 소멸하지 않습니다.";
            case EnemyIntent.DisruptFocus: return "생존하면 다음 턴 집중 획득량이 50% 감소합니다. 중첩되지 않습니다.";
            case EnemyIntent.SiphonUltimate: return "생존하면 궁극기 게이지를 15 감소시킵니다.";
            case EnemyIntent.Pressure: return "생존하면 제한 턴을 1 줄입니다. 웨이브당 최대 2턴 감소합니다.";
            case EnemyIntent.Regenerate: return "생존하면 자신의 최대 체력 4%를 회복합니다. 보스는 2% 회복합니다.";
            default: return "이번 턴 행동하지 않습니다.";
        }
    }
}
