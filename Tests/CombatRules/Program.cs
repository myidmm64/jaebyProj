using System.Text.Json;
int checks=0;
void Check(bool value,string message) { if(!value) throw new Exception(message); checks++; }
var e=RunEngine.New(777);
Check(e.CanExecute,"Starter must fit turn-one budget.");
Check(e.State.Cards.Select(c=>c.Uid).Distinct().Count()==7,"Unique card ids.");
e.Equip(3,1);
Check(e.State.Sequence.Count(id=>id==3)==1,"Move swaps without duplicating cards.");
e.MergeAll();
Check(e.State.Cards.Count==5 && e.State.Cards.Count(c=>c.Stars==2)==2,"Merge two equal cards.");
Check(e.State.Sequence.All(id=>id<0 || e.State.Find(id)!=null),"Merge does not leave invalid slots.");
var focus=e.State.Cards.First(c=>c.Definition==12); var slash=e.State.Cards.First(c=>c.Definition==15);
e.State.Focus=0; e.Resolve(focus,new TurnContext());
Check(e.State.Focus==20,"Star-two focus value.");
var previewFocus=e.State.Focus; var total=e.State.TotalDamage; var played=e.State.CardsPlayed;
e.Preview(slash);
Check(e.State.Focus==previewFocus && e.State.TotalDamage==total && e.State.CardsPlayed==played,"Preview is side-effect free.");
e.Resolve(slash,new TurnContext()); Check(e.State.Focus==0,"Iai consumes focus.");
e.State.Ultimate=100; Check(e.Ultimate(),"Ultimate becomes a temporary card.");
e.EndTurn(); Check(e.State.Cards.All(c=>!c.Temporary),"Unused ultimate expires at end of turn.");
e=RunEngine.New(1); e.State.Turn=e.TurnLimit; e.EndTurn(); Check(e.State.Phase==RunPhase.Defeat,"Timeout loses.");
e=RunEngine.New(2); e.State.Turn=e.TurnLimit; foreach(var enemy in e.State.Enemies) enemy.Health=0; e.EndTurn();
Check(e.State.Phase==RunPhase.CardReward,"A kill on last allowed turn wins.");
e.State.Cards.Clear(); for(int i=0;i<16;i++)e.AddCard(i);
Check(!e.ChooseCard(0),"Full bag blocks rewards without consuming them."); Check(e.ChooseCard(-1),"Full bag can skip for gold.");
e=RunEngine.New(3); e.State.Focus=GameMath.Max; var result=e.Resolve(e.AddCard(28,4),new TurnContext());
Check(result.Damage>0 && e.State.Enemies.All(x=>x.Health>=0),"Huge damage never overflows.");
// Walk every stage transition, including shops and the final boss, through production rules.
e=RunEngine.New(456); int waves=0, shops=0, items=0;
for(int safety=0;safety<300 && e.State.Phase!=RunPhase.Victory;safety++)
{
    switch(e.State.Phase)
    {
        case RunPhase.Battle: foreach(var enemy in e.State.Enemies)enemy.Health=0; e.EndTurn(); waves++; break;
        case RunPhase.CardReward: e.ChooseCard(-1); break;
        case RunPhase.ItemReward: e.ChooseItem(0); items++; break;
        case RunPhase.Shop: var gold=e.State.Gold; if(e.Buy(0)){Check(!e.Buy(0),"Shop purchase cannot repeat.");Check(e.State.Gold<gold,"Purchase spends gold.");} e.LeaveShop(); shops++; break;
        default: throw new Exception("Unexpected progression state.");
    }
}
Check(e.State.Phase==RunPhase.Victory && waves==30 && shops==3 && items==12,"Full campaign has 30 waves, three shops and twelve items.");
// A JSON round trip preserves mid-encounter data and all six slots.
var options=new JsonSerializerOptions{IncludeFields=true,IgnoreReadOnlyProperties=true};
var saved=JsonSerializer.Deserialize<RunState>(JsonSerializer.Serialize(e.State,options),options);
Check(saved.WavesCleared==30 && saved.Items.Sum()==12 && saved.Sequence.Length==6,"Save round trip.");
Console.WriteLine($"PASS: {checks} combat/progression invariants.");
Campaign.Run();
