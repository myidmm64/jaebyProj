using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WizardCombat;

public static class CombatChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("Combat check: "+message);}
    static Battle Ready(){var b=new Battle();b.ChooseTurnCard(0);return b;}
    [MenuItem("Tools/Explosive Wizard/Verify Combat Rules")]
    public static void Verify()
    {
        var b=Ready();var p=b.Player; b.BeginMove();
        Check(b.Reachable().Contains(p+Vector2Int.up*2),"two-cell destination selectable");
        Check(b.Move(p+Vector2Int.up*2)&&b.Moves==0,"destination selected in one action");Check(!b.Move(p+Vector2Int.down),"one selection per turn");
        b.Back();Check(b.Player==p&&b.Phase==BattlePhase.Setup,"movement rollback");
        b=Ready();p=b.Player;b.Enabled[p.x,p.y+1]=false;b.BeginMove();Check(!b.Reachable().Contains(p+Vector2Int.up*2),"no jumping through walls");
        b=Ready();var a=b.Owned.First(c=>c.Id==5);a.Life=1;var c=b.Owned.Last(x=>x.Id==5);c.Life=2;
        Check(b.Merge(a,c).Life==6,"merge refills life");
        b=Ready();b.Enemies.Clear();b.Enemies.Add(new Foe{Cell=b.Player+Vector2Int.right*2,HP=500});b.Costumes.Add(1);b.Equipped=0;b.Gauge=100;b.Ultimate();var ult=b.Owned.Last();var ignite=new Card(8);b.Owned.Add(ignite);
        b.Turn=4;b.Slots=new[]{b.Owned.First(x=>x.Id==5),b.Owned.First(x=>x.Id==4),ignite,ult,null,null};
        Check(b.Preview()[3]==90&&b.Mana==0&&b.Enemies[0].HP==500,"preview non-mutating");
        b.BeginMove();b.Attack();Check(b.Enemies[0].HP==404&&b.Gauge==25&&!b.Owned.Contains(ult),"execution and expiry");
        b=Ready();b.Costumes.Add(1);b.Equipped=0;b.Gauge=100;b.Ultimate();b.Slots=new Card[6];b.BeginMove();b.Attack();Check(!b.Owned.Any(x=>x.Id==11),"unplaced ultimate expires");
        b=Ready();b.Turn=2;var move=b.Owned.First(x=>Rules.Movement(x.Id));b.Slots=new Card[6];b.Slots[0]=move;b.Slots[1]=b.Owned.First(x=>x.Id==4);
        Check(!Rules.Consumer(move.Id)&&Rules.MoveRange(move)==2&&move.Life==-1,"movement is common and unlimited");
        Check(b.Preview()[1]==-1,"damage after an unselected movement is uncertain");
        for(int star=1;star<=3;star++)Check(Rules.MoveRange(new Card(12,star))==2&&Rules.Pattern(12,star).Count==12&&Rules.Pattern(12,star).All(p=>Mathf.Abs(p.x)+Mathf.Abs(p.y)<=2),"two-cell movement at every star");
        Check(Rules.Pattern(0).Count==2&&Rules.Pattern(10).Count==3&&Rules.Pattern(9).Count==3&&Rules.Pattern(11).Count==9,"shorter card ranges");
        Check(Rules.Pattern(0).All(x=>x.x>0 && x.y==0) && Rules.Pattern(11).All(x=>x.x>0),"right-facing fixed patterns");
        b=Ready();var card=b.Owned.First(x=>x.Id==0);
        Check(b.SequenceCapacity==1&&b.Slots.Count(x=>x!=null)==1,"one starting sequence slot");
        b.Place(card,1);Check(b.Slots[1]==null,"locked placement rejected");
        b.Slots[1]=card;Check(b.BeginAttack().Length==1&&b.Preview()[1]==0,"locked slot excluded from execution and preview");b.Slots[1]=null;
        for(int turn=1;turn<=7;turn++){Check(b.SequenceCapacity==Mathf.Min(turn,6),"one slot unlock per turn");b.FinishAttack(new Card[0]);b.HP=60;}
        b.StartWave();Check(b.SequenceCapacity==6,"wave transition preserves capacity");
        b=Ready();b.Enemies.Clear();b.FinishAttack(new Card[0]);Check(b.Turn==2&&b.SequenceCapacity==2&&b.Phase==BattlePhase.Reward,"victory also advances turn");
        b=new Battle();int owned=b.Owned.Count;
        Check(b.Phase==BattlePhase.TurnReward&&b.Rewards.Count==3&&b.Rewards.Select(x=>x.Id).Distinct().Count()==3&&!b.Rewards.Any(x=>x.Id==11),"first turn offers three ordinary cards");
        var location=b.Player;b.BeginMove();b.Place(b.Owned[0],0);Check(b.Phase==BattlePhase.TurnReward&&b.Player==location&&b.BeginAttack().Length==0,"draft blocks setup and sequence");
        b.ChooseTurnCard(-1);Check(b.Owned.Count==owned&&b.Rewards.Count==3,"invalid draft selection ignored");
        var chosen=b.Rewards[1];b.ChooseTurnCard(1);b.ChooseTurnCard(1);Check(b.Phase==BattlePhase.Setup&&b.Owned.Count==owned+1&&b.Owned.Contains(chosen)&&b.Rewards.Count==0,"exactly one draft card awarded");
        b.BeginMove();b.FinishAttack(new Card[0]);Check(b.Turn==2&&b.Phase==BattlePhase.Setup&&b.Rewards.Count==0,"skip supply on the following turn");b.BeginMove();b.FinishAttack(new Card[0]);Check(b.Turn==3&&b.Phase==BattlePhase.TurnReward&&b.Rewards.Count==3,"supply every two turns");b.ChooseTurnCard(0);
        b.Enemies.Clear();b.BeginMove();b.FinishAttack(new Card[0]);Check(b.Phase==BattlePhase.Reward&&b.Rewards.Count==3,"wave reward comes first");
        owned=b.Owned.Count;int wave=b.Wave;b.Reward(0);Check(b.Wave==wave+1&&b.Owned.Count==owned+1&&b.Phase==BattlePhase.Setup&&b.Rewards.Count==0,"wave reward replaces immediate turn draft");
        b=Ready();b.HP=1;b.Enemies[0].Intent.Clear();b.Enemies[0].Intent.Add(b.Player);b.BeginMove();b.FinishAttack(new Card[0]);Check(b.Phase==BattlePhase.Defeat,"defeat does not offer cards");
        b=Ready();b.Owned.Clear();b.Slots=new Card[6];for(int i=0;i<4;i++)b.Owned.Add(new Card(5){Life=1});b.Slots[0]=b.Owned[0];
        b.Owned.Add(new Card(11));b.Owned.Add(new Card(11));b.Owned.Add(new Card(0,3));b.Owned.Add(new Card(0,3));
        Check(b.AutoMerge()==3&&b.Owned.Count==5&&b.Slots[0].Id==5&&b.Slots[0].Star==3&&b.Slots[0].Life==6,"automatic chained merge preserves equipped slot and resets life");
        Check(b.Owned.Count(x=>x.Id==11)==2&&b.Owned.Count(x=>x.Id==0&&x.Star==3)==2&&b.AutoMerge()==0,"ultimate and max stars excluded");
        var equipped=b.Slots[0];b.SortCards();Check(b.Owned[0].Id==0&&b.Slots[0]==equipped&&b.Owned.Contains(equipped),"sorting preserves slot references");
        b=new Battle();int before=b.Owned.Count;Check(b.AutoMerge()==0&&b.Owned.Count==before,"auto merge blocked during reward selection");
        Debug.Log("COMBAT RULE CHECKS PASSED");
    }
}


