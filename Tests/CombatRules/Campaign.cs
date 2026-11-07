using System;
using System.Collections.Generic;
using System.Linq;

// Exercises the shipping rules using only earned rewards, gold, legal costs and legal merges.
public static class Campaign
{
    sealed class Plan
    {
        public RunEngine Engine;
        public TurnContext Context=new();
        public List<int> Cards=new();
        public int Cost;
        public double Score;
    }
    static RunState Copy(RunState s) => new RunState {
        Focus=s.Focus, Items=s.Items, Target=s.Target, Cards=s.Cards, Ultimate=s.Ultimate,
        Enemies=s.Enemies.Select(e=>new EnemyState{Name=e.Name,Kind=e.Kind,Health=e.Health,MaxHealth=e.MaxHealth}).ToList()
    };
    static TurnContext Copy(TurnContext t) => new TurnContext {
        Flat=t.Flat, Mark=t.Mark, Echo=t.Echo, Executed=t.Executed,
        Amplifiers=new Dictionary<int,double>(t.Amplifiers)
    };
    static List<int> Search(RunEngine game)
    {
        var original=game.State; double hp=original.Enemies.Sum(e=>(double)e.Health);
        var beam=new List<Plan>{new Plan{Engine=new RunEngine(Copy(original))}};
        Plan best=null;
        double ratio=original.Cards.Where(c=>c.Data.IsAttack).Select(c=>c.Data.Ratio(c.Stars)).DefaultIfEmpty(1).Max();
        for(int depth=0;depth<6;depth++)
        {
            var next=new List<Plan>();
            foreach(var p in beam)
            foreach(var c in original.Cards)
            {
                if(p.Cards.Contains(c.Uid) || p.Cost+c.Data.Cost>game.Budget)continue;
                var candidate=new Plan{Engine=new RunEngine(Copy(p.Engine.State)),Context=Copy(p.Context),Cards=new List<int>(p.Cards),Cost=p.Cost+c.Data.Cost};
                candidate.Cards.Add(c.Uid); candidate.Engine.Resolve(c,candidate.Context);
                double left=candidate.Engine.State.Enemies.Sum(e=>(double)e.Health);
                if(left==0)return candidate.Cards;
                double mult=Math.Pow(1.5,original.Items[1])*candidate.Context.Mark;
                foreach(var amp in candidate.Context.Amplifiers.Values)mult*=1+amp;
                double future=0;
                foreach(var attack in original.Cards.Where(x=>x.Data.IsAttack && !candidate.Cards.Contains(x.Uid)))
                {
                    var d=attack.Data;
                    double amount=(d.Value(attack.Stars)+candidate.Context.Flat+candidate.Engine.State.Focus*d.Ratio(attack.Stars))*d.Hits*(1+candidate.Context.Echo)*mult;
                    future=Math.Max(future,amount);
                }
                double carry=game.TurnLimit-original.Turn>1?.5:0;
                candidate.Score=hp-left+Math.Min(hp*2,candidate.Engine.State.Focus*ratio)*carry+Math.Min(hp,future)*.12-candidate.Cost*.001;
                next.Add(candidate);
                if(best==null || candidate.Score>best.Score)best=candidate;
            }
            beam=next.OrderByDescending(p=>p.Score).Take(70).ToList();
            if(beam.Count==0)break;
        }
        return best?.Cards??new List<int>();
    }
    static double Quality(RunEngine e,int id)
    {
        var d=CardCatalog.All[id];
        double value=d.Effect switch {
            CardEffect.MultiplyFocus=>100, CardEffect.Focus=>65+d.Power*.25,
            CardEffect.Amplify=>65, CardEffect.Mark=>35, CardEffect.Echo=>65,
            CardEffect.Forge=>25, CardEffect.Rhythm=>30,
            _=>d.Iai?40+d.Rarity*12:15+d.Rarity*8
        };
        if(e.State.Cards.Any(c=>c.Definition==id && c.Stars<4)) value+=35;
        if(d.Effect==CardEffect.MultiplyFocus && e.State.Cards.Count(c=>c.Data.Effect==CardEffect.MultiplyFocus)>=2)value-=50;
        return value;
    }
    public static void Run()
    {
        string folder=System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),"Tests","Results");
        System.IO.Directory.CreateDirectory(folder);
        var options=new System.Text.Json.JsonSerializerOptions{IncludeFields=true,IgnoreReadOnlyProperties=true,WriteIndented=true};
        for(int seed=1;seed<=3;seed++)
        {
            var e=RunEngine.New(seed); int actions=0;
            while(actions++<400 && e.State.Phase!=RunPhase.Victory && e.State.Phase!=RunPhase.Defeat)
            {
                switch(e.State.Phase)
                {
                    case RunPhase.Battle:
                        if(e.State.Ultimate>=100)e.Ultimate();
                        var plan=Search(e);
                        e.State.Sequence=Enumerable.Repeat(-1,6).ToArray();
                        for(int i=0;i<plan.Count;i++)e.Equip(plan[i],i);
                        if(!e.CanExecute)throw new Exception("Campaign found no legal turn.");
                        if(seed==1 && e.State.Floor==3 && e.IsBoss)
                            System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"FinalBoss.json"),System.Text.Json.JsonSerializer.Serialize(e.State,options));
                        var turn=new TurnContext();
                        foreach(int uid in e.State.Sequence) {var card=e.State.Find(uid);if(card!=null)e.Resolve(card,turn);if(e.Cleared)break;}
                        e.EndTurn(); break;
                    case RunPhase.CardReward:
                        e.MergeAll();
                        if(e.State.Cards.Count>=16)
                        {
                            var worst=e.State.Cards.OrderBy(c=>Quality(e,c.Definition)*Math.Pow(2,c.Stars-1)).First();
                            e.Sell(worst.Uid);
                        }
                        int choice=Enumerable.Range(0,3).OrderByDescending(i=>Quality(e,e.State.Offers[i])).First();
                        e.ChooseCard(choice);e.MergeAll();break;
                    case RunPhase.ItemReward:
                        int[] priority={0,3,1,2,4,5};
                        int item=Enumerable.Range(0,3).OrderBy(i=>Array.IndexOf(priority,e.State.Offers[i])).First();
                        e.ChooseItem(item);break;
                    case RunPhase.Shop:
                        if(seed==1 && e.State.Floor==1)
                            System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"Shop.json"),System.Text.Json.JsonSerializer.Serialize(e.State,options));
                        var focus=e.State.Cards.Where(c=>c.Stars<4).OrderByDescending(c=>Quality(e,c.Definition)*Math.Pow(1.7,c.Stars)).FirstOrDefault();
                        if(focus!=null)e.BuyUpgrade(focus.Uid);
                        foreach(int buy in Enumerable.Range(0,3).OrderByDescending(i=>Quality(e,e.State.Offers[i])))if(e.State.Cards.Count<16)e.Buy(buy);
                        e.MergeAll();e.LeaveShop();break;
                }
            }
            Console.WriteLine($"Campaign seed {seed}: {e.State.Phase}, waves={e.State.WavesCleared}, turns={e.State.TurnsPlayed}, best hit={e.State.BestHit:N0}");
            if(e.State.Phase!=RunPhase.Victory)throw new Exception($"Campaign seed {seed} was not completable by the legal-card policy. Floor {e.State.Floor}, node {e.State.Node}, wave {e.State.Wave}.");
            if(seed==1)System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"Victory.json"),System.Text.Json.JsonSerializer.Serialize(e.State,options));
        }
    }
}
