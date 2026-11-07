using System;
using System.Collections.Generic;
using System.Linq;

// All progression and combat rules live here; the scene only presents their results.
public sealed class RunEngine
{
    public RunState State { get; private set; }
    public bool IsBoss => State.Node==4;
    public int CombatIndex => State.Node < 2 ? State.Node : 2;
    public const int BagCapacity=16;
    public int TurnLimit => (IsBoss ? 8 : State.Floor==1?6:5) + Math.Min(2,State.Items[4])+State.BonusTurns-State.LostTurns;
    public int BagCount => State.Cards.Count(c=>Array.IndexOf(State.Sequence,c.Uid)<0);
    public double WeakenAmount => State.Weakened?Math.Max(.25,State.WeakenStrength):0;
    public double SlotScale(CardDefinition card,int slot) => card.IsSlotBonusActive(slot)?1+(card.SlotBonus-1)*(1+Math.Min(1.5,State.Items[ItemCatalog.OrderSheet]*.5)):1;
    public bool Cleared => State.Enemies.All(e=>e.Health<=0);
    public List<OwnedCard> BagCards => State.Cards.Where(c=>Array.IndexOf(State.Sequence,c.Uid)<0).ToList();
    public bool IsCardSealed(int uid) => uid>0 && State.SealedCardUid==uid;
    public bool HasExecutableCard => State.Sequence.Where((id,slot)=>slot!=State.SealedSlot && !IsCardSealed(id)).Any(id=>State.Find(id)!=null);
    public bool CanExecute => State.Phase==RunPhase.Battle;
    public RunEngine(RunState saved)
    {
        MigrateSavedState(saved); State=saved;
        if(State.OfferStars==null || State.OfferStars.Length!=3)State.OfferStars=new[]{1,1,1};
        for(int i=0;i<3;i++)State.OfferStars[i]=Math.Max(1,Math.Min(4,State.OfferStars[i]));
        if(State.Phase==RunPhase.Battle && State.Enemies.Any(e=>e.Intent==EnemyIntent.None)) { State.SealedSlot=-1; PlanIntents(); }
        RefreshCardSealTargets();
    }
    public static RunEngine New(int seed)
    {
        var e=new RunEngine(new RunState { Seed=seed, RandomState=seed });
        foreach(int id in new[]{3,0,15}) e.AddCard(id);
        e.State.Sequence=new[]{1,2,3,-1,-1,-1};
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
        if(s.Items==null)s.Items=new int[ItemCatalog.Count];
        else if(s.Items.Length!=ItemCatalog.Count)Array.Resize(ref s.Items,ItemCatalog.Count);
        if(s.Version>=3)return;
        if(s.Version<2)
        {
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
        }
        for(int i=0;i<s.Enemies.Count;i++)
        {
            var enemy=s.Enemies[i]; enemy.Boss=s.Node==4 && i==0; enemy.Rear=s.Node!=4 && i>=2;
            double fraction=enemy.MaxHealth>0?enemy.Health/(double)enemy.MaxHealth:1;
            enemy.MaxHealth=GameMath.Damage(EnemyHealth(s,enemy.Kind,enemy.Boss)*(enemy.Rear?.85:1)); enemy.Health=GameMath.Damage(enemy.MaxHealth*fraction);
        }
        s.Version=3;
    }
    static long EnemyHealth(RunState s,int kind,bool boss=false)
    {
        if(boss)return GameMath.Damage(new[]{0d,330000d,28000000d,380000000d}[s.Floor]);
        int combat=s.Node<2?s.Node:2;
        if(s.Node==4)combat=3;
        return GameMath.Damage(EnemyCatalog.Get(kind).Health*new[]{1.2,98d,4125d}[s.Floor-1]*Math.Pow(1.6,combat)*Math.Pow(1.45,s.Wave));
    }
    static double StrongestMultiplier(System.Collections.Generic.Dictionary<int,double> values,int id,double amount)
    {
        double previous=values.TryGetValue(id,out var old)?old:0;
        double next=Math.Max(previous,amount); values[id]=next;
        return (1+next)/(1+previous);
    }
    bool ChanceSucceeds(int uid,int salt,double chance)
    {
        // Stable per-card/per-turn combat rolls, independent of reward RNG and previews.
        unchecked
        {
            uint hash=(uint)State.Seed^(uint)uid*0x9e3779b9u^(uint)State.Turn*0x85ebca6bu;
            hash^=(uint)(State.Floor*65536+State.Node*256+State.Wave)*0xc2b2ae35u^(uint)salt;
            hash^=hash>>16; hash*=0x7feb352du; hash^=hash>>15; hash*=0x846ca68bu; hash^=hash>>16;
            return (hash>>8)/16777216d<chance;
        }
    }
    static void SetEcho(TurnContext turn,int repeats,double power,int stars)
    {
        if(repeats*power<=turn.Echo*turn.EchoPower)return;
        turn.Echo=repeats; turn.EchoPower=power; turn.EchoStars=stars;
    }
    public OwnedCard AddCard(int id, int stars=1)
    {
        var card=new OwnedCard(State.NextUid++,id,stars); State.Cards.Add(card); RefreshCardSealTargets(); return card;
    }
    public bool Equip(int uid,int slot)
    {
        var card=State.Find(uid);
        if(State.Phase!=RunPhase.Battle || slot<0 || slot>=6 || slot==State.SealedSlot || card==null) return false;
        int old=Array.IndexOf(State.Sequence,uid);
        if(old>=0) State.Sequence[old]=State.Sequence[slot];
        State.Sequence[slot]=uid; return true;
    }
    public bool Unequip(int slot)
    {
        if(slot<0 || slot>=6 || (State.Find(State.Sequence[slot])!=null && BagCount>=BagCapacity)) return false;
        State.Sequence[slot]=-1; return true;
    }
    public bool PlaceInBag(int uid,int slot)
    {
        if(State.Phase!=RunPhase.Battle || slot<0) return false;
        var card=State.Find(uid); if(card==null) return false;
        if(Array.IndexOf(State.Sequence,uid)>=0 && BagCount>=BagCapacity)return false;
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
        return source!=null && target!=null && sourceUid!=targetUid && !source.Temporary && !target.Temporary && !IsCardSealed(sourceUid) && !IsCardSealed(targetUid) && source.Definition==target.Definition && source.Stars==target.Stars && target.Stars<4;
    }
    public bool MergeInto(int sourceUid,int targetUid)
    {
        if(!CanMerge(sourceUid,targetUid)) return false;
        State.Find(targetUid).Stars++;
        for(int i=0;i<6;i++) if(State.Sequence[i]==sourceUid) State.Sequence[i]=-1;
        State.Cards.Remove(State.Find(sourceUid));
        RefreshCardSealTargets();
        return true;
    }
    public int MergeAll()
    {
        int count=0;
        while(true)
        {
            var pair=State.Cards.Where(c=>!c.Temporary && c.Stars<4 && !IsCardSealed(c.Uid))
                .GroupBy(c=>c.Definition+":"+c.Stars).FirstOrDefault(g=>g.Count()>=2);
            if(pair==null) break;
            var keep=pair.First(); var remove=pair.Skip(1).First();
            MergeInto(remove.Uid,keep.Uid); count++;
        }
        return count;
    }
    public int SellPrice(OwnedCard c) => (int)((6+c.Data.Rarity*6)*(1<<(c.Stars-1))*(1+Math.Min(1,State.Items[ItemCatalog.CoinPurse]*.25)));
    public bool Sell(int uid)
    {
        var c=State.Find(uid);
        if(c==null || c.Temporary || State.Cards.Count(x=>!x.Temporary)<=1) return false;
        State.Gold+=SellPrice(c);
        for(int i=0;i<6;i++) if(State.Sequence[i]==uid) State.Sequence[i]=-1;
        State.Cards.Remove(c);
        if(State.SealedCardUid==uid)State.SealedCardUid=0;
        RefreshCardSealTargets(); return true;
    }
    public bool Ultimate()
    {
        if(State.Phase!=RunPhase.Battle || State.Ultimate<100 || State.Cards.Any(c=>c.Temporary)) return false;
        if(BagCount>=BagCapacity && !State.Sequence.Where((uid,slot)=>slot!=State.SealedSlot).Any(uid=>State.Find(uid)==null))return false;
        State.Ultimate=0;
        var c=AddCard(29,Math.Min(4,State.Floor)); c.Temporary=true;
        for(int slot=0;slot<State.Sequence.Length;slot++)
            if(State.Sequence[slot]<0 && slot!=State.SealedSlot) { Equip(c.Uid,slot); break; }
        return true;
    }
    public CardResult Resolve(OwnedCard card,TurnContext turn,int slot=-1)
    {
        var s=State; var d=card.Data;
        var result=new CardResult { Card=card, FocusBefore=s.Focus };
        if(IsCardSealed(card.Uid) || (slot>=0 && slot==s.SealedSlot)) { result.FocusAfter=s.Focus; return result; }
        // Use the actual cell index: empty/sealed cells still retain their number.
        double slotScale=SlotScale(d,slot);
        double value=d.Value(card.Stars)*(d.IsAttack?1:slotScale);
        if(s.SupportSuppressed && d.Effect!=CardEffect.Focus && d.Effect!=CardEffect.Rhythm && !d.IsAttack && d.Effect!=CardEffect.Delay)value*=.75;
        switch(d.Effect)
        {
            case CardEffect.Focus: GainFocus(value); break;
            case CardEffect.Rhythm: GainFocus(value*turn.Executed); break;
            case CardEffect.MultiplyFocus: s.Focus=GameMath.Clamp(s.Focus*StrongestMultiplier(turn.FocusMultipliers,d.Id,value/100)); break;
            case CardEffect.Amplify:
                turn.Additive+=value/100; break;
            case CardEffect.MultiplyDamage: turn.Multiplier*=StrongestMultiplier(turn.DamageMultipliers,d.Id,value/100); break;
            case CardEffect.Delay: s.BonusTurns=Math.Min(2,s.BonusTurns+(int)value); GainFocus(20*card.Stars); break;
            case CardEffect.Forge: turn.Flat=GameMath.Clamp(turn.Flat+value); break;
            case CardEffect.Mark: turn.Mark+=value/100; break;
            case CardEffect.Echo:
                SetEcho(turn,d.Repeats(card.Stars),d.RepeatPower(card.Stars),card.Stars); break;
            case CardEffect.Fortune:
                turn.CriticalChance=Math.Max(turn.CriticalChance,Chance(d,card.Stars));
                turn.CriticalScale=Math.Max(turn.CriticalScale,1+value/100); break;
            case CardEffect.Gamble:
                if(turn.Gambles.Add(d.Id))
                {
                    double chance=Chance(d,card.Stars), success=1+value/100;
                    bool won=!turn.Preview && ChanceSucceeds(card.Uid,31,chance);
                    turn.Multiplier*=turn.Preview?chance*success+(1-chance)*d.UnluckyScale:won?success:d.UnluckyScale;
                    if(!turn.Preview) { result.ChanceSuccess=won; result.ChanceText=won?"승부 성공 ×"+success.ToString("0.##"):"승부 실패 ×"+d.UnluckyScale.ToString("0.##"); }
                }
                break;
            case CardEffect.ChanceEcho:
                GainFocus(20*card.Stars);
                double echoChance=Chance(d,card.Stars);
                bool echoWon=!turn.Preview && ChanceSucceeds(card.Uid,33,echoChance);
                if(turn.Preview || echoWon)SetEcho(turn,1,turn.Preview?echoChance:1,card.Stars);
                if(!turn.Preview) { result.ChanceSuccess=echoWon; result.ChanceText=echoWon?"동전 성공 · 추가 공격":"동전 실패"; }
                break;
            default:
                double itemBonus=s.Items[1]*.18+(d.Iai?s.Items[3]*.18:0);
                if(turn.Attacks==0)itemBonus+=s.Items[ItemCatalog.OpeningWeight]*.25;
                if(d.Hits>1)itemBonus+=s.Items[ItemCatalog.TwinBlade]*.18;
                if(s.Turn==1)itemBonus+=s.Items[ItemCatalog.WarHorn]*.2;
                if(TurnLimit-s.Turn+1<=2)itemBonus+=s.Items[ItemCatalog.Hourglass]*.25;
                double mult=(1+turn.Additive)*turn.Multiplier*(1+itemBonus)*turn.Mark*(1-WeakenAmount);
                if(d.IsChance)
                {
                    double chance=Chance(d,card.Stars); bool lucky=!turn.Preview && ChanceSucceeds(card.Uid,32,chance);
                    mult*=turn.Preview?chance*d.LuckyScale+(1-chance)*d.UnluckyScale:lucky?d.LuckyScale:d.UnluckyScale;
                    if(!turn.Preview) { result.ChanceSuccess=lucky; result.ChanceText=lucky?"강타 ×"+d.LuckyScale.ToString("0.##"):"약타 ×"+d.UnluckyScale.ToString("0.##"); }
                }
                if(turn.CriticalChance>0)
                {
                    bool critical=!turn.Preview && ChanceSucceeds(card.Uid,19,turn.CriticalChance);
                    mult*=turn.Preview?1+turn.CriticalChance*(turn.CriticalScale-1):critical?turn.CriticalScale:1;
                    if(critical) { result.ChanceSuccess=true; result.ChanceText=string.IsNullOrEmpty(result.ChanceText)?"행운 ×"+turn.CriticalScale.ToString("0.##"):result.ChanceText+" · 행운 ×"+turn.CriticalScale.ToString("0.##"); }
                }
                long damage=GameMath.Damage((value+turn.Flat*d.ForgeScale(card.Stars)+s.Items[ItemCatalog.Whetstone]*40*s.Floor+s.Focus*d.Ratio(card.Stars))*mult*slotScale);
                for(int hit=0;hit<d.Hits*(1+turn.Echo);hit++)
                {
                    long hitDamage=hit<d.Hits?damage:GameMath.Damage(damage*turn.EchoPower);
                    if(d.All)
                    {
                        for(int i=0;i<s.Enemies.Count;i++) Hit(i,hitDamage,result,d);
                    }
                    else
                    {
                        int target=s.Target;
                        if(target<0 || target>=s.Enemies.Count || s.Enemies[target].Health<=0) target=s.Enemies.FindIndex(e=>e.Health>0);
                        if(target>=0) Hit(target,hitDamage,result,d);
                    }
                }
                if(d.Consume || d.Effect==CardEffect.Ultimate) s.Focus*=Math.Max(d.FocusRetention,Math.Min(.3,s.Items[ItemCatalog.EchoBottle]*.1));
                if(d.FocusGain>0)GainFocus(d.FocusGain*value/d.Power);
                if(result.Kills>0)GainFocus(result.Kills*s.Items[ItemCatalog.Incense]*8*s.Floor);
                turn.Attacks++;
                turn.Echo=0; turn.EchoStars=0; turn.EchoPower=0; turn.Mark=1;
                turn.CriticalChance=0; turn.CriticalScale=1;
                break;
        }
        turn.Executed++; s.CardsPlayed++;
        s.Ultimate=Math.Min(100,s.Ultimate+3+(d.Iai?1:0));
        result.FocusAfter=s.Focus;
        return result;
    }
    double Chance(CardDefinition card,int stars) => Math.Min(.95,card.ChanceAt(stars)+Math.Min(.15,State.Items[ItemCatalog.LuckyDie]*.05));
    void GainFocus(double n) { State.Focus=GameMath.Clamp(State.Focus+n*(1+State.Items[0]*.2)*(State.FocusDisrupted?.5:1)*(State.SupportSuppressed?.75:1)); }
    void Hit(int index,long damage,CardResult result,CardDefinition card)
    {
        var enemy=State.Enemies[index]; if(enemy.Health<=0) return;
        if(card.Finisher && enemy.Health<=enemy.MaxHealth*.5) damage=GameMath.Damage(damage*2d);
        if(enemy.Boss)damage=GameMath.Damage(damage*(1+State.Items[ItemCatalog.BossLens]*.2));
        if(enemy.Guarding && !card.PierceGuard)damage=GameMath.Damage(damage*(1-Math.Max(.1,.5-Math.Min(.4,State.Items[ItemCatalog.PiercingNeedle]*.15))));
        if(enemy.Rear && !card.All && !card.PierceGuard && State.Enemies.Any(e=>e.Health>0 && !e.Rear && e.Guarding))damage=GameMath.Damage(damage*.8);
        double shieldPower=1+State.Items[ItemCatalog.RiftHammer]*.75;
        long absorbed=Math.Min(enemy.Barrier,GameMath.Damage(damage*shieldPower)); enemy.Barrier-=absorbed;
        result.BarrierDamage=(long)Math.Min(GameMath.Max,result.BarrierDamage+(double)absorbed);
        long healthDamage=Math.Max(0,damage-(long)Math.Ceiling(absorbed/shieldPower));
        enemy.Health=Math.Max(0,enemy.Health-healthDamage);
        if(enemy.Health==0)result.Kills++;
        result.Damage=(long)Math.Min(GameMath.Max,result.Damage+(double)damage);
        if(!result.Targets.Contains(index)) result.Targets.Add(index);
        State.TotalDamage=GameMath.Clamp(State.TotalDamage+damage);
        State.BestHit=Math.Max(State.BestHit,damage);
    }
    public void EndTurn()
    {
        var s=State; s.TurnsPlayed++; s.Focus=0; s.SealedSlot=-1; s.SealedCardUid=0;
        foreach(var c in s.Cards.Where(c=>c.Temporary).ToArray())
        {
            for(int i=0;i<6;i++) if(s.Sequence[i]==c.Uid) s.Sequence[i]=-1;
            s.Cards.Remove(c);
        }
        if(Cleared)
        {
            s.WavesCleared++; s.Gold+=(int)((15+s.Floor*8)*(1+s.Items[5]*.15));
            s.Phase=RunPhase.CardReward; s.Offers=CardOffers();
        }
        else if(s.Turn>=TurnLimit) s.Phase=RunPhase.Defeat;
        else
        {
            s.Weakened=false; s.WeakenStrength=0; s.FocusDisrupted=false; s.SupportSuppressed=false; s.SealedSlot=-1; s.SealedCardUid=0;
            foreach(var enemy in s.Enemies) enemy.Guarding=false;
            foreach(var enemy in s.Enemies.Where(e=>e.Health>0))
            {
                switch(enemy.Intent)
                {
                    case EnemyIntent.Weaken: s.Weakened=true; s.WeakenStrength=Math.Max(s.WeakenStrength,enemy.Enraged?.35:.25); break;
                    case EnemyIntent.Guard: enemy.Guarding=true; break;
                    case EnemyIntent.Seal: s.SealedSlot=enemy.IntentSlot; break;
                    case EnemyIntent.CardSeal:
                        if(s.Find(enemy.IntentCardUid)!=null)s.SealedCardUid=enemy.IntentCardUid;
                        break;
                    case EnemyIntent.DisruptFocus: s.FocusDisrupted=true; break;
                    case EnemyIntent.SiphonUltimate: s.Ultimate=Math.Max(0,s.Ultimate-(enemy.Enraged?25:15)); break;
                    case EnemyIntent.Pressure: s.LostTurns=Math.Min(2,s.LostTurns+1); break;
                    case EnemyIntent.Regenerate: enemy.Health=Math.Min(enemy.MaxHealth,enemy.Health+GameMath.Damage(enemy.MaxHealth*(enemy.Boss?.02:.05))); break;
                    case EnemyIntent.Fortify:
                        foreach(var ally in s.Enemies.Where(e=>e.Health>0))
                            ally.Barrier=Math.Min(GameMath.Damage(ally.MaxHealth*.15),ally.Barrier+GameMath.Damage(ally.MaxHealth*(ally.Boss?.025:.06)));
                        break;
                    case EnemyIntent.MendAllies:
                        foreach(var ally in s.Enemies.Where(e=>e.Health>0))
                            ally.Health=Math.Min(ally.MaxHealth,ally.Health+GameMath.Damage(ally.MaxHealth*(ally.Boss?.015:.04)));
                        break;
                    case EnemyIntent.Suppress: s.SupportSuppressed=true; break;
                }
            }
            if(s.Turn>=TurnLimit)s.Phase=RunPhase.Defeat;
            else { s.Turn++; PlanIntents(); }
            if(s.Target>=s.Enemies.Count || s.Enemies[s.Target].Health<=0) s.Target=s.Enemies.FindIndex(e=>e.Health>0);
        }
    }
    public bool HasChanceBefore(OwnedCard card)
    {
        bool turnChance=false,nextChance=false;
        for(int slot=0;slot<State.Sequence.Length;slot++)
        {
            int uid=State.Sequence[slot];
            if(slot==State.SealedSlot || IsCardSealed(uid))continue;
            var previous=State.Find(uid); if(previous==null)continue;
            if(previous.Uid==card.Uid)return turnChance || nextChance || previous.Data.IsChance;
            if(previous.Data.Effect==CardEffect.Gamble)turnChance=true;
            if(previous.Data.Effect==CardEffect.Fortune || previous.Data.Effect==CardEffect.ChanceEcho)nextChance=true;
            if(previous.Data.IsAttack)nextChance=false;
        }
        return turnChance || nextChance || card.Data.IsChance;
    }
    public long Preview(OwnedCard card)
    {
        // A real dry-run includes every preceding support card, in sequence order.
        var backup=State;
        var clone=new RunState { Focus=backup.Focus, Items=(int[])backup.Items.Clone(), Target=backup.Target, Weakened=backup.Weakened, FocusDisrupted=backup.FocusDisrupted, SealedSlot=backup.SealedSlot, SealedCardUid=backup.SealedCardUid, BonusTurns=backup.BonusTurns };
        clone.Floor=backup.Floor; clone.Node=backup.Node; clone.Wave=backup.Wave; clone.Turn=backup.Turn; clone.Seed=backup.Seed;
        clone.WeakenStrength=backup.WeakenStrength; clone.SupportSuppressed=backup.SupportSuppressed; clone.LostTurns=backup.LostTurns;
        foreach(var e in backup.Enemies) clone.Enemies.Add(new EnemyState {Health=e.Health, MaxHealth=e.MaxHealth, Guarding=e.Guarding, Boss=e.Boss, Rear=e.Rear, Enraged=e.Enraged, Barrier=e.Barrier});
        State=clone;
        try
        {
            var t=new TurnContext {Preview=true};
            if(IsCardSealed(card.Uid))return 0;
            for(int i=0;i<backup.Sequence.Length;i++)
            {
                if(i==State.SealedSlot) continue;
                var c=backup.Find(backup.Sequence[i]); if(c==null) continue;
                if(IsCardSealed(c.Uid))continue;
                var r=Resolve(c,t,i); if(c.Uid==card.Uid) return r.Damage;
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
            if(index>=3 || BagCount+RewardCopies>BagCapacity) return false;
            for(int n=0;n<RewardCopies;n++)AddCard(State.Offers[index],State.OfferStars[index]);
        }
        else State.Gold+=RewardGold;
        if(IsBoss || State.Wave==2)
        {
            State.Phase=RunPhase.ItemReward;
            var available=Enumerable.Range(0,ItemCatalog.Count).Where(id=>State.Items[id]<ItemCatalog.MaxCopies(id)).ToList();
            if(available.Count<3)available=Enumerable.Range(0,ItemCatalog.Count).ToList();
            var ids=new List<int>(); while(ids.Count<3) { int pick=Roll(available.Count); ids.Add(available[pick]); available.RemoveAt(pick); }
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
    public int RerollPrice => State.ShopRerolls<Math.Min(2,State.Items[ItemCatalog.RerollTicket])?0:10+Math.Max(0,State.ShopRerolls-Math.Min(2,State.Items[ItemCatalog.RerollTicket]))*5;
    public bool RerollShop()
    {
        if(State.Phase!=RunPhase.Shop || State.Gold<RerollPrice) return false;
        State.Gold-=RerollPrice; State.ShopRerolls++;
        State.Offers=ShopOffers(); State.Bought=new bool[3]; return true;
    }
    public int ShopPrice(int index) => Math.Max(1,(int)((25+CardCatalog.All[State.Offers[index]].Rarity*20)*(1+(State.OfferStars[index]-1)*.65)*(1-Math.Min(.3,State.Items[ItemCatalog.MerchantScale]*.1))));
    public bool Buy(int index)
    {
        if(State.Phase!=RunPhase.Shop || index<0 || index>=3 || State.Bought[index] || BagCount>=BagCapacity || State.Gold<ShopPrice(index)) return false;
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
        var s=State; s.Phase=RunPhase.Battle; s.Turn=1; s.Focus=State.Items[2]*40*s.Floor; s.Target=0;
        s.Weakened=false; s.WeakenStrength=0; s.FocusDisrupted=false; s.SupportSuppressed=false; s.SealedSlot=-1; s.SealedCardUid=0; s.BonusTurns=0; s.LostTurns=0;
        s.Ultimate=Math.Min(100,s.Ultimate+s.Items[5]*12);
        s.Enemies.Clear();
        if(IsBoss)
        {
            int kind=new[]{0,5,9,11}[s.Floor]; long hp=EnemyHealth(s,kind,true);
            s.Enemies.Add(new EnemyState { Name=new[]{"","초원의 심장","균열의 파수꾼","공허의 학장"}[s.Floor],Kind=kind,Health=hp,MaxHealth=hp,Boss=true });
            int[] helpers=s.Floor==1?new[]{3}:s.Floor==2?new[]{5,8}:new[]{6,9};
            foreach(int helper in helpers)
            {
                long helperHp=EnemyHealth(s,helper);
                s.Enemies.Add(new EnemyState{Name=EnemyCatalog.Get(helper).Name,Kind=helper,Health=helperHp,MaxHealth=helperHp,Rear=true});
            }
        }
        else
        {
            bool first=s.Floor==1 && s.Node==0 && s.Wave==0;
            int count=first?2:s.Floor==1?3:s.Floor==3 || s.Wave==2 || s.Node==3?4:3;
            var formations=EnemyCatalog.Formations[s.Floor-1];
            var formation=formations[(int)(((uint)s.Seed+(uint)(s.Node*3+s.Wave))%(uint)formations.Length)];
            for(int i=0;i<count;i++)
            {
                int kind=first?i:formation[i%formation.Length];
                bool rear=i>=2;
                long hp=GameMath.Damage(EnemyHealth(s,kind)*(rear?.85:1));
                s.Enemies.Add(new EnemyState { Name=EnemyCatalog.Get(kind).Name, Kind=kind, Health=hp, MaxHealth=hp,Rear=rear });
            }
        }
        PlanIntents();
    }
    void PlanIntents()
    {
        for(int i=0;i<State.Enemies.Count;i++)
        {
            var enemy=State.Enemies[i];
            if(enemy.Boss && (State.Turn>=4 || enemy.Health<=enemy.MaxHealth*.5))enemy.Enraged=true;
            var pattern=enemy.Boss?EnemyCatalog.BossPatterns[State.Floor-1]:EnemyCatalog.Get(enemy.Kind).Pattern;
            enemy.Intent=pattern[(State.Turn-1+i)%pattern.Length];
            if(enemy.Intent==EnemyIntent.Pressure && State.LostTurns>=2)enemy.Intent=EnemyIntent.Weaken;
            enemy.IntentSlot=(State.Turn+i)%6;
            if(enemy.Intent==EnemyIntent.CardSeal)SelectCardSealTarget(enemy,i);
        }
    }
    void SelectCardSealTarget(EnemyState enemy,int index)
    {
        var cards=State.Sequence.Select(uid=>State.Find(uid)).Where(c=>c!=null).Concat(State.Cards)
            .Where(c=>!c.Temporary && !IsCardSealed(c.Uid)).GroupBy(c=>c.Uid).Select(g=>g.First()).ToArray();
        var target=cards.Length>0?cards[(State.Turn+index)%cards.Length]:null;
        enemy.IntentCardUid=target?.Uid??0; enemy.IntentCardName=target?.Data.Name??"카드 1장";
    }
    void RefreshCardSealTargets()
    {
        for(int i=0;i<State.Enemies.Count;i++)
        {
            var enemy=State.Enemies[i];
            if(enemy.Intent==EnemyIntent.CardSeal && State.Find(enemy.IntentCardUid)==null)SelectCardSealTarget(enemy,i);
        }
    }
    public static string IntentName(EnemyState enemy)
    {
        switch(enemy.Intent)
        {
            case EnemyIntent.Weaken: return "공격 약화";
            case EnemyIntent.Guard: return "경화";
            case EnemyIntent.Fortify: return "진형 보호막";
            case EnemyIntent.MendAllies: return "아군 회복";
            case EnemyIntent.Suppress: return "지원 억제";
            case EnemyIntent.Seal: return (enemy.IntentSlot+1)+"번 칸 봉인";
            case EnemyIntent.CardSeal: return "카드 봉인";
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
            case EnemyIntent.Weaken: return "생존하면 다음 턴 아군 공격 피해 -"+(enemy.Enraged?35:25)+"%. 중첩되지 않습니다.";
            case EnemyIntent.Guard: return "생존하면 다음 턴 자신이 받는 피해 -50%. 앞줄의 경화는 후열이 받는 단일 피해도 20% 줄입니다. 광역·관통 공격은 후열 엄호를 무시합니다.";
            case EnemyIntent.Fortify: return "생존하면 살아 있는 모든 적에게 최대 체력의 6% 보호막을 추가합니다. 보스 본체는 2.5%, 누적 보호막은 최대 체력의 15%까지입니다.";
            case EnemyIntent.MendAllies: return "생존하면 모든 살아 있는 적의 최대 체력 4%를 회복합니다. 보스 본체는 1.5% 회복하며 부활은 하지 않습니다.";
            case EnemyIntent.Suppress: return "생존하면 다음 턴 아군 집중 획득·재련·배율 증가량 -25%. 반복·턴 연장에는 영향을 주지 않으며 중첩되지 않습니다.";
            case EnemyIntent.Seal: return "생존하면 다음 턴 "+(enemy.IntentSlot+1)+"번 시퀀스 칸을 사용할 수 없습니다. 카드는 소멸하지 않습니다.";
            case EnemyIntent.CardSeal: return "생존하면 다음 턴 '"+(enemy.IntentCardName??"카드 1장")+"'를 봉인합니다. 칸을 옮겨도 실행·합성할 수 없으며, 해당 턴 종료 시 해제됩니다.";
            case EnemyIntent.DisruptFocus: return "생존하면 다음 턴 집중 획득량이 50% 감소합니다. 중첩되지 않습니다.";
            case EnemyIntent.SiphonUltimate: return "생존하면 궁극기 게이지를 "+(enemy.Enraged?25:15)+" 감소시킵니다.";
            case EnemyIntent.Pressure: return "생존하면 제한 턴을 1 줄입니다. 웨이브당 최대 2턴 감소합니다.";
            case EnemyIntent.Regenerate: return "생존하면 자신의 최대 체력 5%를 회복합니다. 보스 본체는 2% 회복합니다.";
            default: return "이번 턴 행동하지 않습니다.";
        }
    }
}
