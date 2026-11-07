using System.Text.Json;
static class Scenarios
{
    public static void Run()
    {
        var outcomes=new List<object>();
        var builds=new Dictionary<string,int[]> {
            ["FocusBurst"]=new[]{24,25,16,26,28,21},
            ["MultiHit"]=new[]{3,20,4,26,5,21},
            ["Tempo"]=new[]{24,25,28,3,30,16},
            ["Unsynergized"]=new[]{0,1,2,5,9,13}
        };
        foreach(var build in builds)for(int stars=1;stars<=4;stars++)
        {
            var state=new RunState{Floor=3,Node=4,Items=new[]{2,2,1,2,0,0}};
            var e=new RunEngine(state); foreach(int id in build.Value)e.AddCard(id,stars);
            e.BeginWave(); double peak=0;
            while(state.Phase==RunPhase.Battle)
            {
                var plan=Planner.Find(e,24); state.Target=plan.Target; state.Sequence=Enumerable.Repeat(-1,6).ToArray();
                int slot=0;foreach(int uid in plan.Cards){while(slot==state.SealedSlot)slot++;if(slot<6)e.Equip(uid,slot++);}
                var turn=new TurnContext(); double damage=state.TotalDamage;
                for(int i=0;i<6;i++){if(i==state.SealedSlot)continue;var c=state.Find(state.Sequence[i]);if(c!=null)e.Resolve(c,turn);if(e.Cleared)break;}
                peak=Math.Max(peak,state.TotalDamage-damage); e.EndTurn();
            }
            outcomes.Add(new {Build=build.Key,Stars=stars,Clear=e.Cleared,Turns=state.Turn,PeakTurn=peak,Damage=state.TotalDamage});
        }
        var timer=new RunEngine(new RunState());timer.BeginWave();var time=timer.AddCard(30,4);
        for(int i=0;i<20;i++)timer.Resolve(time,new TurnContext());
        if(timer.State.BonusTurns!=2)throw new Exception("Delay allows unbounded extra turns.");
        var dup=new RunEngine(new RunState());dup.BeginWave();dup.State.Focus=100;
        var focus=dup.AddCard(25,4);var multiplier=dup.AddCard(26,4);var context=new TurnContext();
        dup.Resolve(focus,context);dup.Resolve(multiplier,context);double f=dup.State.Focus,m=context.Multiplier;
        for(int i=0;i<5;i++){dup.Resolve(focus,context);dup.Resolve(multiplier,context);}
        if(dup.State.Focus!=f || context.Multiplier!=m)throw new Exception("Duplicate multipliers stack exponentially.");
        File.WriteAllText("Tests/Balance/Results/BuildsV2.json",JsonSerializer.Serialize(outcomes,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("Build, turn-cap and duplicate-growth scenarios saved.");
    }
}