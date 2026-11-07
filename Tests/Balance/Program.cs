using System.Text.Json;

// Numerical simulation against the same engine used by Unity. Never loads a scene or a save.
int seeds=args.Length>0?int.Parse(args[0]):24;
string label=args.Length>1?args[1]:"Current";
int beamWidth=args.Length>2?int.Parse(args[2]):14;
int firstSeed=args.Length>3?int.Parse(args[3]):1;
var results=new List<Outcome>();
for(int seed=firstSeed;seed<firstSeed+seeds;seed++)
{
    var e=RunEngine.New(seed); var turns=new List<int>(); var floors=new int[3]; double bestTurn=0;
    var effects=new Dictionary<string,int>();
    for(int guard=0;guard<500 && e.State.Phase!=RunPhase.Victory && e.State.Phase!=RunPhase.Defeat;guard++)
    {
        var s=e.State;
        switch(s.Phase)
        {
            case RunPhase.Battle:
                if(s.Ultimate>=100) e.Ultimate();
                var plan=Planner.Find(e,beamWidth);
                s.Sequence=Enumerable.Repeat(-1,6).ToArray(); int slot=0;
                foreach(int uid in plan.Cards) { while(slot==s.SealedSlot)slot++; if(slot<6)e.Equip(uid,slot++); }
                s.Target=plan.Target; double damage=s.TotalDamage; var context=new TurnContext();
                for(int i=0;i<6;i++) { if(i==s.SealedSlot)continue; var card=s.Find(s.Sequence[i]); if(card!=null){e.Resolve(card,context); string effect=card.Data.Effect.ToString();effects[effect]=effects.GetValueOrDefault(effect)+1;} if(e.Cleared)break; }
                bestTurn=Math.Max(bestTurn,s.TotalDamage-damage);
                if(e.Cleared) { turns.Add(s.Turn); floors[s.Floor-1]++; }
                e.EndTurn(); break;
            case RunPhase.CardReward:
                e.MergeAll();
                int pick=Enumerable.Range(0,3).OrderByDescending(i=>Planner.Quality(e,s.Offers[i],s.OfferStars[i])).First();
                while(s.Cards.Count+e.RewardCopies>16) { var worst=s.Cards.Where(c=>!c.Temporary).OrderBy(c=>Planner.Quality(e,c.Definition,c.Stars)).First(); e.Sell(worst.Uid); }
                if(!e.ChooseCard(pick))e.ChooseCard(-1); e.MergeAll(); break;
            case RunPhase.ItemReward:
                int[] preference={0,1,3,2,4,5};
                int item=Enumerable.Range(0,3).OrderBy(i=>Array.IndexOf(preference,s.Offers[i]) + s.Items[s.Offers[i]]*1.3).First();
                e.ChooseItem(item); break;
            case RunPhase.Shop:
                var upgrade=s.Cards.Where(c=>c.Stars<4 && !c.Temporary).OrderByDescending(c=>Planner.Quality(e,c.Definition,c.Stars)).FirstOrDefault();
                if(upgrade!=null)e.BuyUpgrade(upgrade.Uid);
                foreach(int i in Enumerable.Range(0,3).OrderByDescending(i=>Planner.Quality(e,s.Offers[i],s.OfferStars[i])))
                    if(s.Cards.Count<16)e.Buy(i);
                e.MergeAll();
                if(s.Gold>=e.RerollPrice+50 && e.RerollShop())
                { int i=Enumerable.Range(0,3).OrderByDescending(n=>Planner.Quality(e,s.Offers[n],s.OfferStars[n])).First(); if(s.Cards.Count<16)e.Buy(i); e.MergeAll(); }
                e.LeaveShop(); break;
        }
    }
    results.Add(new Outcome(seed,e.State.Phase.ToString(),e.State.Floor,e.State.Node,e.State.WavesCleared,e.State.TurnsPlayed,e.State.BestHit,bestTurn,turns.ToArray(),floors,effects));
    if(results.Count%8==0)Console.WriteLine($"{label}: {results.Count}/{seeds}, wins {results.Count(r=>r.Phase=="Victory")}");
}
Directory.CreateDirectory("Tests/Balance/Results");
File.WriteAllText($"Tests/Balance/Results/{label}.json",JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"{label}: wins={results.Count(r=>r.Phase=="Victory")}/{seeds}, mean waves={results.Average(r=>r.Waves):F1}, mean turns/clear={results.SelectMany(r=>r.ClearTurns).DefaultIfEmpty().Average():F2}, max hit={results.Max(r=>r.BestHit):N0}, max turn={results.Max(r=>r.BestTurn):N0}");
for(int f=0;f<3;f++)Console.WriteLine($"Floor {f+1}: clears={results.Count(r=>r.FloorClears[f]==10)}/{seeds}, median fail waves={results.Where(r=>r.Floor==f+1 && r.Phase=="Defeat").Select(r=>r.Waves).DefaultIfEmpty(-1).Order().ElementAt(results.Count(r=>r.Floor==f+1 && r.Phase=="Defeat")/2)}");
if(label.StartsWith("Final"))Scenarios.Run();

record Outcome(int Seed,string Phase,int Floor,int Node,int Waves,int Turns,double BestHit,double BestTurn,int[] ClearTurns,int[] FloorClears,Dictionary<string,int> Effects);
sealed class Plan
{
    public RunEngine Engine; public TurnContext Context=new(); public List<int> Cards=new(); public int Target; public double Score;
}
static class Planner
{
    static RunState Copy(RunState s) => new() {
        Floor=s.Floor,Node=s.Node,Wave=s.Wave,Turn=s.Turn,Focus=s.Focus,Items=s.Items,Target=s.Target,Cards=s.Cards,Ultimate=s.Ultimate,
        Weakened=s.Weakened,FocusDisrupted=s.FocusDisrupted,SealedSlot=s.SealedSlot,BonusTurns=s.BonusTurns,LostTurns=s.LostTurns,
        Enemies=s.Enemies.Select(e=>new EnemyState{Name=e.Name,Kind=e.Kind,Health=e.Health,MaxHealth=e.MaxHealth,Intent=e.Intent,IntentSlot=e.IntentSlot,Guarding=e.Guarding}).ToList()
    };
    static TurnContext Copy(TurnContext t) => new() {Flat=t.Flat,Mark=t.Mark,Echo=t.Echo,Executed=t.Executed,Additive=t.Additive,Multiplier=t.Multiplier,FocusMultipliers=new(t.FocusMultipliers),DamageMultipliers=new(t.DamageMultipliers)};
    public static Plan Find(RunEngine game,int width)
    {
        var s=game.State; double hp=s.Enemies.Sum(e=>(double)e.Health); var beam=new List<Plan>();
        for(int i=0;i<s.Enemies.Count;i++)if(s.Enemies[i].Health>0){var copy=Copy(s);copy.Target=i;beam.Add(new Plan{Engine=new(copy),Target=i});}
        Plan best=null; int slots=6-(s.SealedSlot>=0?1:0);
        for(int depth=0;depth<slots;depth++)
        {
            var next=new List<Plan>();
            foreach(var p in beam)foreach(var c in s.Cards)
            {
                if(p.Cards.Contains(c.Uid))continue;
                var candidate=new Plan{Engine=new(Copy(p.Engine.State)),Context=Copy(p.Context),Cards=new(p.Cards),Target=p.Target};
                candidate.Cards.Add(c.Uid); candidate.Engine.Resolve(c,candidate.Context);
                double left=candidate.Engine.State.Enemies.Sum(e=>(double)e.Health);
                if(left==0)return candidate;
                double mult=(1+candidate.Context.Additive)*candidate.Context.Multiplier*candidate.Context.Mark*(1+s.Items[1]*.35)*(1+s.Items[3]*.2);
                double future=0;
                foreach(var atk in s.Cards.Where(a=>a.Data.IsAttack && !candidate.Cards.Contains(a.Uid)))
                {
                    double amount=(atk.Data.Value(atk.Stars)+candidate.Context.Flat+candidate.Engine.State.Focus*atk.Data.Ratio(atk.Stars))*mult*atk.Data.Hits*(1+candidate.Context.Echo);
                    future=Math.Max(future,amount*(atk.Data.All?candidate.Engine.State.Enemies.Count(e=>e.Health>0):1));
                }
                double survival=(candidate.Engine.State.BonusTurns-s.BonusTurns)*hp/(Math.Max(1,game.TurnLimit-s.Turn+1))* .65;
                candidate.Score=hp-left+Math.Min(hp,future)*.12+survival+candidate.Engine.State.Enemies.Count(e=>e.Health==0)*hp*.04;
                if(best==null || candidate.Score>best.Score)best=candidate;
                next.Add(candidate);
            }
            beam=next.OrderByDescending(p=>p.Score).Take(width).ToList();if(beam.Count==0)break;
        }
        return best;
    }
    public static double Quality(RunEngine e,int id,int stars)
    {
        var d=CardCatalog.All[id]; string effect=d.Effect.ToString();
        double score=effect switch {
            "Focus"=>70+d.Power*.6,"MultiplyFocus"=>110,"Amplify"=>85,"MultiplyDamage"=>115,
            "Mark"=>60,"Echo"=>100,"Forge"=>85,"Rhythm"=>65,"Delay"=>52,
            _=>d.Iai?75+d.Rarity*22:35+d.Rarity*14
        };
        score*=Math.Pow(1.65,stars-1);
        if(e.State.Cards.Any(c=>c.Definition==id && c.Stars==stars && stars<4))score*=1.8;
        int count=e.State.Cards.Count(c=>c.Data.Effect==d.Effect);
        if(count>1 && !d.IsAttack)score/=1+(count-1)*.5;
        return score;
    }
}
