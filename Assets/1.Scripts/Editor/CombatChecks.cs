using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WizardCombat;

public static class CombatChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("Combat check: "+message);}
    [MenuItem("Tools/Explosive Wizard/Verify Combat Rules")]
    public static void Verify()
    {
        var b=new Battle();var p=b.Player; b.BeginMove();
        Check(b.Reachable().Contains(p+Vector2Int.up*2),"two-cell destination selectable");
        Check(b.Move(p+Vector2Int.up*2)&&b.Moves==0,"destination selected in one action");Check(!b.Move(p+Vector2Int.down),"one selection per turn");
        b.Back();Check(b.Player==p&&b.Phase==BattlePhase.Setup,"movement rollback");
        b=new Battle();p=b.Player;b.Enabled[p.x,p.y+1]=false;b.BeginMove();Check(!b.Reachable().Contains(p+Vector2Int.up*2),"no jumping through walls");
        b=new Battle();var a=b.Owned.First(c=>c.Id==5);a.Life=1;var c=b.Owned.Last(x=>x.Id==5);c.Life=2;
        Check(b.Merge(a,c).Life==6,"merge refills life");
        b=new Battle();b.Enemies.Clear();b.Enemies.Add(new Foe{Cell=b.Player+Vector2Int.right*2,HP=500});b.Costumes.Add(1);b.Equipped=0;b.Gauge=100;b.Ultimate();var ult=b.Owned.Last();var ignite=new Card(8);b.Owned.Add(ignite);
        b.Turn=4;b.Slots=new[]{b.Owned.First(x=>x.Id==5),b.Owned.First(x=>x.Id==4),ignite,ult,null,null};
        Check(b.Preview()[3]==90&&b.Mana==0&&b.Enemies[0].HP==500,"preview non-mutating");
        b.BeginMove();b.Attack();Check(b.Enemies[0].HP==404&&b.Gauge==25&&!b.Owned.Contains(ult),"execution and expiry");
        b=new Battle();b.Costumes.Add(1);b.Equipped=0;b.Gauge=100;b.Ultimate();b.Slots=new Card[6];b.BeginMove();b.Attack();Check(!b.Owned.Any(x=>x.Id==11),"unplaced ultimate expires");
        b=new Battle();b.Turn=2;var move=b.Owned.First(x=>Rules.Movement(x.Id));b.Slots=new Card[6];b.Slots[0]=move;b.Slots[1]=b.Owned.First(x=>x.Id==4);
        Check(!Rules.Consumer(move.Id)&&Rules.MoveRange(move)==2&&move.Life==-1,"movement is common and unlimited");
        Check(b.Preview()[1]==-1,"damage after an unselected movement is uncertain");
        for(int star=1;star<=3;star++)Check(Rules.MoveRange(new Card(12,star))==2&&Rules.Pattern(12,star).Count==12&&Rules.Pattern(12,star).All(p=>Mathf.Abs(p.x)+Mathf.Abs(p.y)<=2),"two-cell movement at every star");
        Check(Rules.Pattern(0).Count==2&&Rules.Pattern(10).Count==3&&Rules.Pattern(9).Count==3&&Rules.Pattern(11).Count==9,"shorter card ranges");
        Check(Rules.Pattern(0).All(x=>x.x>0 && x.y==0) && Rules.Pattern(11).All(x=>x.x>0),"right-facing fixed patterns");
        b=new Battle();var card=b.Owned.First(x=>x.Id==0);
        Check(b.SequenceCapacity==1&&b.Slots.Count(x=>x!=null)==1,"one starting sequence slot");
        b.Place(card,1);Check(b.Slots[1]==null,"locked placement rejected");
        b.Slots[1]=card;Check(b.BeginAttack().Length==1&&b.Preview()[1]==0,"locked slot excluded from execution and preview");b.Slots[1]=null;
        for(int turn=1;turn<=7;turn++){Check(b.SequenceCapacity==Mathf.Min(turn,6),"one slot unlock per turn");b.FinishAttack(new Card[0]);b.HP=60;}
        b.StartWave();Check(b.SequenceCapacity==6,"wave transition preserves capacity");
        b=new Battle();b.Enemies.Clear();b.FinishAttack(new Card[0]);Check(b.Turn==2&&b.SequenceCapacity==2&&b.Phase==BattlePhase.Reward,"victory also advances turn");
        Debug.Log("COMBAT RULE CHECKS PASSED");
    }
}


