using System.Text.Json;
static class Scenarios
{
    public static void Run()
    {
        var outcomes=new List<object>();
        var builds=new Dictionary<string,int[]> {
            ["FocusBurst"]=new[]{24,25,16,26,21,28},
            ["MultiHit"]=new[]{3,20,4,26,21,11},
            ["Tempo"]=new[]{24,25,28,3,30,16},
            ["Unsynergized"]=new[]{0,1,2,5,9,13}
        };
        foreach(var build in builds)for(int stars=1;stars<=4;stars++)
        {
            var laboratory=new RunEngine(new RunState{Floor=3,Node=4,Items=new[]{2,2,1,2,0,0}});laboratory.BeginWave();
            laboratory.State.Enemies[0].Health=laboratory.State.Enemies[0].MaxHealth=(long)GameMath.Max;
            var laboratoryTurn=new TurnContext();int laboratorySlot=0;foreach(int id in build.Value)laboratory.Resolve(laboratory.AddCard(id,stars),laboratoryTurn,laboratorySlot++);
            double rawDamage=laboratory.State.TotalDamage;
            var state=new RunState{Floor=3,Node=4,Items=new[]{2,2,1,2,0,0}};
            var e=new RunEngine(state); foreach(int id in build.Value)e.AddCard(id,stars);
            e.BeginWave(); double peak=0;
            while(state.Phase==RunPhase.Battle)
            {
                var plan=Planner.Find(e,24); state.Target=plan.Target; state.Sequence=Enumerable.Repeat(-1,6).ToArray();
                int slot=0;foreach(int uid in plan.Cards){while(slot==state.SealedSlot)slot++;if(slot<6)e.Equip(uid,slot++);}
                var turn=new TurnContext(); double damage=state.TotalDamage;
                for(int i=0;i<6;i++){if(i==state.SealedSlot || e.IsCardSealed(state.Sequence[i]))continue;var c=state.Find(state.Sequence[i]);if(c!=null)e.Resolve(c,turn,i);if(e.Cleared)break;}
                peak=Math.Max(peak,state.TotalDamage-damage); e.EndTurn();
            }
            outcomes.Add(new {Build=build.Key,Stars=stars,Clear=e.Cleared,Turns=state.Turn,PeakTurn=peak,Damage=state.TotalDamage,RawComboDamage=rawDamage});
        }
        var timer=new RunEngine(new RunState());timer.BeginWave();var time=timer.AddCard(30,4);
        for(int i=0;i<20;i++)timer.Resolve(time,new TurnContext());
        if(timer.State.BonusTurns!=2)throw new Exception("Delay allows unbounded extra turns.");
        var dup=new RunEngine(new RunState());dup.BeginWave();dup.State.Focus=100;
        var focus=dup.AddCard(25,4);var multiplier=dup.AddCard(26,4);var context=new TurnContext();
        dup.Resolve(focus,context);dup.Resolve(multiplier,context);double f=dup.State.Focus,m=context.Multiplier;
        for(int i=0;i<5;i++){dup.Resolve(focus,context);dup.Resolve(multiplier,context);}
        if(dup.State.Focus!=f || context.Multiplier!=m)throw new Exception("Duplicate multipliers stack exponentially.");
        var kinds=new HashSet<int>();
        for(int floor=1;floor<=3;floor++)foreach(int node in new[]{0,1,3})for(int wave=0;wave<3;wave++)
        {var run=new RunEngine(new RunState{Floor=floor,Node=node,Wave=wave,Seed=17});run.BeginWave();foreach(var enemy in run.State.Enemies)kinds.Add(enemy.Kind);}
        if(kinds.Count!=12)throw new Exception("Enemy roster is not fully used by encounter pools.");
        var legacy=new RunState{Version=1,NextUid=4,Cards=new(){new(1,30),new(2,31),new(3,32)},Sequence=new[]{1,2,3,-1,-1,-1}};
        var migrated=new RunEngine(legacy);
        if(legacy.Cards.Count!=1 || legacy.Cards[0].Definition!=30 || legacy.Sequence[0]!=-1 || legacy.Sequence[2]!=-1)throw new Exception("Removed-card save migration failed.");
        var sealing=RunEngine.New(1); var locked=sealing.State.Cards.First(c=>c.Data.IsAttack);
        sealing.State.SealedCardUid=locked.Uid; double hp=sealing.State.Enemies.Sum(e=>(double)e.Health);
        sealing.Equip(locked.Uid,5); sealing.Resolve(locked,new TurnContext());
        if(sealing.State.Enemies.Sum(e=>(double)e.Health)!=hp || sealing.Preview(locked)!=0)throw new Exception("Moving a sealed card bypassed its seal.");
        var copy=sealing.AddCard(locked.Definition,locked.Stars);
        if(sealing.CanMerge(locked.Uid,copy.Uid))throw new Exception("A sealed card can be merged.");
        sealing.EndTurn(); if(sealing.IsCardSealed(locked.Uid))throw new Exception("Card seal did not expire.");
        sealing.State.Sequence=Enumerable.Repeat(-1,6).ToArray();if(!sealing.CanExecute || sealing.HasExecutableCard)throw new Exception("Sealed/empty turns cannot wait.");
        File.WriteAllText("Tests/Balance/Results/BuildsV2.json",JsonSerializer.Serialize(outcomes,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("Build, turn-cap and duplicate-growth scenarios saved.");
    }
}
