using System.Text.Json;
using System.Text.RegularExpressions;

int checks=0;
void Check(bool value,string message)
{
    if(!value)throw new Exception(message);
    checks++;
}
void BagIsPacked(RunEngine game,string context)
{
    var expected=game.BagCards.Select(c=>c.Uid).ToArray();
    Check(game.State.BagSlots.Take(expected.Length).SequenceEqual(expected),context+": occupied slots preserve bag order.");
    Check(game.State.BagSlots.Skip(expected.Length).All(uid=>uid==-1),context+": empty cells are trailing.");
    Check(expected.Length==game.BagCount && expected.Distinct().Count()==expected.Length,context+": bag count and unique entries.");
    Check(!expected.Intersect(game.State.Sequence).Any(),context+": equipped cards do not occupy bag cells.");
}
RunEngine Empty(int seed=71)
{
    var game=RunEngine.New(seed);
    game.State.Cards.Clear(); game.State.Sequence=Enumerable.Repeat(-1,6).ToArray();
    game.State.BagSlots=Enumerable.Repeat(-1,RunEngine.BagCapacity).ToArray();
    game.State.SealedSlot=-1; game.State.SealedCardUid=0;
    return game;
}
string Snapshot(RunState state)=>JsonSerializer.Serialize(state,new JsonSerializerOptions{IncludeFields=true,IgnoreReadOnlyProperties=true});
void English(string value,string context)
{
    Check(!string.IsNullOrWhiteSpace(value),context+": nonempty translation.");
    Check(!Regex.IsMatch(value,"[가-힣]"),context+": English contains no untranslated Hangul: "+value);
}

var e=RunEngine.New(777);
Check(e.State.Cards.Count==3,"A new run grants exactly three starter cards.");
Check(e.State.Cards.Select(c=>c.Uid).Distinct().Count()==3,"Starter IDs are unique.");
Check(e.State.Sequence.Take(3).All(uid=>e.State.Find(uid)!=null),"The three starters are equipped.");
Check(e.BagCount==0 && e.CanExecute,"Equipped starters do not count against the bag and the turn can execute.");
BagIsPacked(e,"Starter");

var bagA=e.AddCard(2); var bagB=e.AddCard(4); var bagC=e.AddCard(6); var bagD=e.AddCard(7);
Check(e.BagCards.Select(c=>c.Uid).SequenceEqual(new[]{bagA.Uid,bagB.Uid,bagC.Uid,bagD.Uid}),"Acquired cards append in acquisition order.");
Check(e.MoveCard(bagD.Uid,bagB.Uid),"Management drag can reorder cards.");
Check(e.BagCards.Select(c=>c.Uid).SequenceEqual(new[]{bagA.Uid,bagD.Uid,bagB.Uid,bagC.Uid}),"Management order is the battle bag order.");
BagIsPacked(e,"Management reorder");
int returned=e.State.Sequence[0];
Check(e.PlaceInBag(returned,2),"A sequence card can return between bag cards.");
Check(e.State.Sequence[0]==-1 && e.BagCards.Select(c=>c.Uid).SequenceEqual(new[]{bagA.Uid,bagD.Uid,returned,bagB.Uid,bagC.Uid}),"Sequence return inserts precisely at the requested position.");
Check(e.PlaceInBag(bagC.Uid,0),"A bag card can move to the beginning.");
Check(e.BagCards.First().Uid==bagC.Uid,"Drag to the beginning preserves the requested order.");
Check(e.PlaceInBag(returned,15),"Drop into trailing empty space appends.");
Check(e.BagCards.Last().Uid==returned,"Trailing-space drop does not leave holes.");
BagIsPacked(e,"Sequence return and drag");
int displaced=e.State.Sequence[1];
int swapIndex=e.BagCards.FindIndex(c=>c.Uid==bagD.Uid);
Check(e.Equip(bagD.Uid,1),"Equipping over an existing card succeeds.");
Check(e.State.Sequence[1]==bagD.Uid && e.BagCards[swapIndex].Uid==displaced,"Displaced sequence card replaces the source bag position.");
Check(e.Unequip(1),"Clicking a sequence card can return it to the bag.");
BagIsPacked(e,"Equip and unequip");

e=Empty();
e.AddCard(12); var sequenceKeep=e.AddCard(12); e.AddCard(12);
e.Equip(sequenceKeep.Uid,4);
Check(e.MergeAll()==1,"Three matching one-star cards merge once.");
Check(e.State.Sequence[4]==sequenceKeep.Uid && sequenceKeep.Stars==2,"Auto-merge prioritizes the equipped card and keeps its exact sequence cell.");
Check(e.State.Cards.Count==2 && e.State.Cards.Single(c=>c.Uid!=sequenceKeep.Uid).Stars==1,"The third duplicate remains one star.");
e.AddCard(12);
Check(e.MergeAll()==2 && e.State.Cards.Count==1,"Four one-star copies combine into one three-star card through cascading merges.");
Check(e.State.Sequence[4]==sequenceKeep.Uid && sequenceKeep.Stars==3,"Cascading merges retain the equipped survivor UID and slot.");
BagIsPacked(e,"Cascading merges");
var sameStar=e.AddCard(12,3);
e.State.SealedCardUid=sameStar.Uid;
Check(!e.CanMerge(sameStar.Uid,sequenceKeep.Uid) && e.MergeAll()==0,"Sealed cards cannot be consumed by merging.");
e.State.SealedCardUid=0;
Check(e.MergeInto(sameStar.Uid,sequenceKeep.Uid) && sequenceKeep.Stars==4,"Direct merge can upgrade the equipped target to four stars.");
var maxStar=e.AddCard(12,4);
Check(!e.CanMerge(maxStar.Uid,sequenceKeep.Uid),"Four-star cards do not merge beyond the maximum rank.");

e=Empty(19);
var focus=e.AddCard(12); var slash=e.AddCard(15);
e.State.Enemies.ForEach(enemy=>{enemy.Health=enemy.MaxHealth=1000000; enemy.Intent=EnemyIntent.None;});
e.Resolve(focus,new TurnContext());
Check(e.State.Focus==30,"One-star Focus grants its authored value without a slot bonus.");
e.Equip(focus.Uid,0); e.Equip(slash.Uid,2);
string before=Snapshot(e.State);
Check(e.Preview(slash)>0,"Damage preview produces a positive expected hit.");
Check(Snapshot(e.State)==before,"Preview leaves all production state unchanged, including enemies, statistics and RNG.");
e.Resolve(slash,new TurnContext(),2);
Check(e.State.Focus==0,"Consuming Iai expends Focus.");
e.State.Focus=123; e.State.Items[ItemCatalog.EchoBottle]=3;
e.EndTurn();
Check(e.State.Focus==0,"Focus expires at turn end, including with retention items.");

e=RunEngine.New(31); e.State.Ultimate=99;
Check(!e.Ultimate(),"Ultimate requires a full gauge.");
e.State.Ultimate=100;
Check(e.Ultimate(),"Full gauge grants the temporary ultimate card.");
var ultimate=e.State.Cards.Single(c=>c.Temporary);
Check(ultimate.Definition==29 && e.State.Ultimate==0,"Ultimate consumes charge and grants Grand Iai.");
Check(e.TargetsFor(ultimate.Data).Length<=2,"Ultimate targets at most two enemies.");
e.EndTurn();
Check(e.State.Find(ultimate.Uid)==null && !e.State.Sequence.Contains(ultimate.Uid),"Unused ultimate expires from both ownership and sequence at turn end.");
e=RunEngine.New(1); e.State.Turn=e.TurnLimit; e.EndTurn();
Check(e.State.Phase==RunPhase.Defeat,"Running out of turns loses.");
e=RunEngine.New(2); e.State.Turn=e.TurnLimit; e.State.Enemies.ForEach(enemy=>enemy.Health=0); e.EndTurn();
Check(e.State.Phase==RunPhase.CardReward,"A kill on the last allowed turn wins the wave.");
e.State.Cards.Clear(); e.State.Sequence=Enumerable.Repeat(-1,6).ToArray(); e.PackBag();
for(int i=0;i<RunEngine.BagCapacity;i++)e.AddCard(i);
before=Snapshot(e.State);
Check(!e.ChooseCard(0) && Snapshot(e.State)==before,"A full bag rejects a card reward without consuming or changing the offer.");
Check(e.ChooseCard(-1),"A full bag can choose the gold alternative.");
e=RunEngine.New(3); e.State.Focus=GameMath.Max;
var huge=e.Resolve(e.AddCard(28,4),new TurnContext());
Check(huge.Damage>0 && huge.Damage<=GameMath.Max && e.State.Enemies.All(enemy=>enemy.Health>=0),"Extreme Focus produces bounded damage without overflowing or negative HP.");

checks+=Campaign.Run();

GameLocalization.SetLanguage(GameLanguage.Korean);
string koreanCard=CardCatalog.All[0].Name, koreanEnemy=EnemyCatalog.All[0].Name, koreanItem=ItemCatalog.Names[0];
Check(GameMath.Number(10000).Contains("만"),"Korean uses Korean large-number units.");
int languageChanges=0; void Changed()=>languageChanges++;
GameLocalization.Changed+=Changed;
GameLocalization.SetLanguage(GameLanguage.English);
GameLocalization.SetLanguage(GameLanguage.English);
Check(languageChanges==1,"A language change emits one notification and an unchanged language emits none.");
foreach(var definition in CardCatalog.All)
{
    English(definition.Name,"Card "+definition.Id+" name"); English(definition.KindName,"Card "+definition.Id+" type");
    English(definition.TargetName,"Card "+definition.Id+" target");
    for(int stars=1;stars<=4;stars++)English(definition.Description(stars),$"Card {definition.Id} at {stars} stars");
}
foreach(var definition in EnemyCatalog.All)English(definition.Name,"Enemy "+definition.Id);
for(int id=0;id<ItemCatalog.Count;id++)
{
    English(ItemCatalog.Names[id],"Item "+id+" name");
    English(ItemCatalog.Descriptions[id],"Item "+id+" description");
}
foreach(EnemyIntent intent in Enum.GetValues<EnemyIntent>())
{
    var enemy=new EnemyState{Intent=intent,IntentSlot=2,IntentCardName=koreanCard,Enraged=true};
    English(RunEngine.IntentName(enemy),intent+" title");
    English(RunEngine.IntentDescription(enemy),intent+" description");
}
Check(CardCatalog.All[0].Name!=koreanCard && EnemyCatalog.All[0].Name!=koreanEnemy && ItemCatalog.Names[0]!=koreanItem,"Language switches update previously initialized catalogs.");
Check(GameMath.Number(10000).Contains("K") && GameMath.Number(1000000).Contains("M") && GameMath.Number(1000000000).Contains("B"),"English uses K/M/B large-number units.");
foreach(var key in CardTranslations.English.Keys)
    English(GameLocalization.Text(key,1,25),"Card UI translation "+key);
GameLocalization.SetLanguage(GameLanguage.Korean);
GameLocalization.Changed-=Changed;
Check(languageChanges==2 && CardCatalog.All[0].Name==koreanCard && EnemyCatalog.All[0].Name==koreanEnemy && ItemCatalog.Names[0]==koreanItem,"Switching back restores Korean names without recreating catalogs.");
Console.WriteLine($"PASS: {checks} current combat, bag, merge, progression and localization regression checks.");
