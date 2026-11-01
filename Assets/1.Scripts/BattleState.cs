using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WizardCombat
{
    public enum BattlePhase { Setup, Move, Reward, Defeat, TurnReward }
    public sealed class Card
    {
        public int Id, Star = 1, Life;
        public Card(int id, int star = 1) { Id = id; Star = star; Life = Rules.Lifetime(id); }
        public Card Copy() => new Card(Id, Star) { Life = Life };
    }
    public sealed class Foe
    {
        public Vector2Int Cell;
        public int HP, MaxHP;
        public bool Ranged;
        public List<Vector2Int> Intent = new List<Vector2Int>();
        public Foe Copy() => new Foe { Cell = Cell, HP = HP, MaxHP = MaxHP, Ranged = Ranged, Intent = new List<Vector2Int>(Intent) };
    }
    public static class Rules
    {
        public static readonly string[] Names = { "견제 베기", "예리함", "찌르기", "연속 베기", "칼집 치기", "정심", "반월 베기", "스쳐 베기", "검의 호흡", "거합·횡월", "거합·일섬", "비기·단공", "보법" };
        public static readonly string[] Descriptions = {
            "앞 1칸 베기", "다음 공격 첫 타격 강화", "직선 2칸 찌르기", "앞 1칸 2회 베기",
            "앞 1칸 명중 시 집중 획득", "적 없이 집중 획득", "전방 3칸 베기 · 명중 시 집중 획득", "직선 2칸 첫 적 · 명중 시 집중 획득",
            "다음 거합 강화", "집중 전량 소모 · 전방 3칸 횡베기", "집중 전량 소모 · 직선 3칸 베기", "집중 전량 소모 · 부채꼴 9칸 베기\n미사용이어도 이번 턴 소멸", "카드 차례에 목적지를 선택해 이동" };
        static readonly int[][] Damages = { new[]{8,12,18}, new[]{8,12,18}, new[]{6,9,14}, new[]{4,6,9}, new[]{6,9,14}, new[]{0,0,0}, new[]{4,6,9}, new[]{10,15,22}, new[]{0,0,0}, new[]{8,12,18}, new[]{10,15,22}, new[]{40,60,90} };
        public static int Damage(Card c) => Movement(c.Id)?0:Damages[c.Id][c.Star-1];
        public static int Lifetime(int id) => new[]{-1,4,6,6,-1,6,-1,5,4,6,4,1,-1}[id];
        public static bool Movement(int id)=>id==12;
        public static int MoveRange(Card c)=>2;
        public static int Charge(Card c) => c.Id == 5 ? c.Star+1 : c.Id == 7 ? (c.Star == 3 ? 3 : 2) : c.Id == 4 || c.Id == 6 ? (c.Star == 3 ? 2 : 1) : 0;
        public static int Coefficient(Card c) => c.Id == 9 ? c.Star+3 : c.Id == 10 ? c.Star+2 : c.Id == 11 ? c.Star*2+6 : 0;
        public static float Boost(Card c) => new[]{.25f,.35f,.5f}[c.Star-1];
        public static bool Support(int id) => id == 1 || id == 5 || id == 8;
        public static bool Consumer(int id) => id >= 9 && id<=11;
        public static bool FirstOnly(int id) => id == 0 || id == 3 || id == 4 || id == 7;
        public static string EffectText(Card c)
        {
            if(Movement(c.Id))return $"최대 {MoveRange(c)}칸 이동\n노란 점 선택\n새 위치에서 다음 카드";
            if(c.Id==1)return $"다음 공격 첫 타격\n피해 +{Damage(c)}";
            if(c.Id==5)return $"집중 +{Charge(c)}\n적이 없어도 사용";
            if(c.Id==8)return $"다음 거합 공격\n피해 +{Mathf.RoundToInt(Boost(c)*100)}%";
            if(Consumer(c.Id))return $"집중 전부 소모\n피해 {Damage(c)} + 소비량 × {Coefficient(c)}\n"+(c.Id==11?"집중 0도 사용 가능":"집중 1 이상 필요");
            if(c.Id==3)return $"피해 {Damage(c)} × 2회\n바로 앞 1칸 베기";
            if(Charge(c)>0)return $"피해 {Damage(c)}\n명중 시 집중 +{Charge(c)}";
            return $"피해 {Damage(c)}\n"+(c.Id==2?"직선 2칸 모두 찌르기":"바로 앞 1칸 베기");
        }
        public static List<Vector2Int> Pattern(int id,int star=1)
        {
            var cells = new List<Vector2Int>();
            if(Movement(id)){int range=MoveRange(new Card(id,star));for(int x=-range;x<=range;x++)for(int y=-range;y<=range;y++)if(Mathf.Abs(x)+Mathf.Abs(y)<=range&&(x!=0||y!=0))cells.Add(new Vector2Int(x,y));return cells;}
            if (Support(id)) return cells;
            if (id == 6 || id == 9) cells.AddRange(new[]{new Vector2Int(1,-1),new Vector2Int(1,0),new Vector2Int(1,1)});
            else if (id == 11) { for(int x=1;x<=3;x++) for(int y=1-x;y<x;y++) cells.Add(new Vector2Int(x,y)); }
            else for(int x=1;x<=(id == 10 ? 3 : id == 2 || id == 7 ? 2 : 1);x++) cells.Add(new Vector2Int(x,0));
            return cells;
        }
    }
    // Small deterministic state model, shared by execution, preview and editor smoke checks.
    public sealed class Battle
    {
        public int Size = 8, HP = 60, Concentration, Gauge, Gold, Turn = 1, Wave = 1, Moves = 2;
        public bool[,] Enabled;
        public Vector2Int Player, Origin;
        public BattlePhase Phase;
        public List<Card> Owned = new List<Card>();
        public Card[] Slots = new Card[6];
        public int SequenceCapacity => Mathf.Clamp(Turn,1,Slots.Length);
        public const int TurnCardInterval=2;
        public int LastCardSupplyTurn=-1;
        public List<Foe> Enemies = new List<Foe>();
        public List<int> Costumes = new List<int>();
        public int Equipped = -1;
        public List<Card> Rewards = new List<Card>();
        public List<string> Log = new List<string>();
        public int ActiveStar => Equipped >= 0 && Equipped < Costumes.Count ? Costumes[Equipped] : 0;
        public bool WeaponUsed, NormalHit;
        public int Sharpness;
        public float BreathBoost;
        public Battle(int size = 8)
        {
            Size = size; Enabled = new bool[size,size];
            for(int x=0;x<size;x++) for(int y=0;y<size;y++) Enabled[x,y]=true;
            foreach(int id in new[]{5,4,9,0,5}) Owned.Add(new Card(id));
            var move=new Card(12);Owned.Add(move);Slots[0]=move;
            StartWave();
        }
        public bool Valid(Vector2Int p) => p.x>=0 && p.y>=0 && p.x<Size && p.y<Size && Enabled[p.x,p.y];
        public Foe At(Vector2Int p) => Enemies.FirstOrDefault(e=>e.HP>0 && e.Cell==p);
        public void Say(string s) { Log.Add(s); if(Log.Count>14) Log.RemoveAt(0); }
        public void StartWave()
        {
            Concentration=0; Player=new Vector2Int(0,Size/2);
            // Enabled grids remain authored by Stage; select valid unoccupied cells.
            var cells = new List<Vector2Int>();
            for(int x=0;x<Size;x++) for(int y=0;y<Size;y++) if(Valid(new Vector2Int(x,y))) cells.Add(new Vector2Int(x,y));
            Player=cells.OrderBy(p=>p.x).ThenBy(p=>Mathf.Abs(p.y-Size/2)).First();
            Enemies.Clear();
            foreach(var p in cells.Where(p=>p!=Player).OrderByDescending(p=>p.x).ThenBy(p=>Mathf.Abs(p.y-Player.y)).Take(Mathf.Min(2+Wave/2,5)))
                Enemies.Add(new Foe { Cell=p,HP=18+Wave*6,MaxHP=18+Wave*6,Ranged=Enemies.Count%2==1 });
            Moves=2; Plan(); Say($"웨이브 {Wave} 시작");PrepareTurnReward();
        }
        public void Plan()
        {
            foreach(var e in Enemies.Where(e=>e.HP>0))
            {
                e.Intent.Clear();
                if(e.Ranged)
                {
                    var delta=Player-e.Cell;
                    var d=Mathf.Abs(delta.x)>=Mathf.Abs(delta.y)?new Vector2Int(delta.x>=0?1:-1,0):new Vector2Int(0,delta.y>=0?1:-1);
                    for(int i=1;i<=3;i++) { var p=e.Cell+d*i; if(!Valid(p))break; e.Intent.Add(p); }
                }
                else foreach(var d in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}) if(Valid(e.Cell+d)) e.Intent.Add(e.Cell+d);
            }
        }
        public void BeginMove() { if(Phase!=BattlePhase.Setup)return; Origin=Player; Moves=2; Phase=BattlePhase.Move; }
        public void Back() { if(Phase!=BattlePhase.Move)return; Player=Origin; Phase=BattlePhase.Setup; Moves=2; }
        public bool Move(Vector2Int p)
        {
            if(Phase!=BattlePhase.Move || !Reachable().Contains(p))return false;
            Player=p; Moves=0; return true;
        }
        public List<Vector2Int> Reachable()
        {
            var result=new List<Vector2Int>();if(Phase!=BattlePhase.Move||Moves<=0)return result;
            var queue=new Queue<Vector2Int>();var distance=new Dictionary<Vector2Int,int>();distance[Player]=0;queue.Enqueue(Player);
            while(queue.Count>0){var p=queue.Dequeue();if(distance[p]>=Moves)continue;
                foreach(var d in new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left})
                {var n=p+d;if(!Valid(n)||At(n)!=null||distance.ContainsKey(n))continue;distance[n]=distance[p]+1;result.Add(n);queue.Enqueue(n);}}
            return result;
        }
        public List<Vector2Int> PathTo(Vector2Int target)
        {
            var path=new List<Vector2Int>();if(!Reachable().Contains(target))return path;
            var q=new Queue<Vector2Int>();var parent=new Dictionary<Vector2Int,Vector2Int>();q.Enqueue(Player);parent[Player]=Player;
            while(q.Count>0){var p=q.Dequeue();if(p==target)break;foreach(var d in new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left})
            {var n=p+d;if(!Valid(n)||At(n)!=null||parent.ContainsKey(n))continue;parent[n]=p;q.Enqueue(n);}}
            for(var p=target;p!=Player;p=parent[p])path.Add(p);path.Reverse();return path;
        }
        public void Place(Card card,int slot)
        {
            if(Phase!=BattlePhase.Setup || !Owned.Contains(card) || slot<0 || slot>=SequenceCapacity)return;
            int prior=Array.IndexOf(Slots,card); var displaced=Slots[slot];
            Slots[slot]=card; if(prior>=0 && prior!=slot) Slots[prior]=displaced;
        }
        public Card Merge(Card a,Card b)
        {
            if(Phase!=BattlePhase.Setup || a==b || !Owned.Contains(a)||!Owned.Contains(b)||a.Id!=b.Id||a.Star!=b.Star||a.Star>=3||a.Id==11)return null;
            int slot=Array.IndexOf(Slots,a); if(slot<0)slot=Array.IndexOf(Slots,b);
            var c=new Card(a.Id,a.Star+1); Remove(a); Remove(b); Owned.Add(c); if(slot>=0)Slots[slot]=c;
            Say($"{Rules.Names[c.Id]} ★{c.Star} 합체 · 수명 초기화");return c;
        }
        public void SortCards()
        {
            if(Phase!=BattlePhase.Setup)return;
            Owned=Owned.OrderBy(c=>c.Id<4||Rules.Movement(c.Id)?0:1).ThenBy(c=>c.Id).ThenByDescending(c=>c.Star).ThenBy(c=>c.Life<0?int.MaxValue:c.Life).ToList();
        }
        public int AutoMerge()
        {
            if(Phase!=BattlePhase.Setup)return 0;
            int count=0;
            while(true)
            {
                var pair=Owned.Where(c=>c.Id!=11&&c.Star<3).GroupBy(c=>(c.Id,c.Star)).FirstOrDefault(g=>g.Count()>=2);
                if(pair==null)break;
                // Prefer bag cards; when equipped cards merge, retain the earliest slot.
                var cards=pair.OrderBy(c=>Array.IndexOf(Slots,c)).Take(2).ToArray();
                if(Merge(cards[0],cards[1])==null)break;
                count++;
            }
            SortCards();Say(count>0?$"자동 합성 {count}회 완료":"합성 가능한 카드가 없습니다");return count;
        }
        public void Remove(Card c) { Owned.Remove(c); for(int i=0;i<6;i++)if(Slots[i]==c)Slots[i]=null; }
        public void Equip(int index) { if(Phase==BattlePhase.Setup && index>=-1 && index<Costumes.Count)Equipped=index; }
        public void MergeCostume()
        {
            if(Phase!=BattlePhase.Setup)return;
            for(int i=0;i<Costumes.Count;i++)for(int j=i+1;j<Costumes.Count;j++)if(Costumes[i]==Costumes[j] && Costumes[i]<3)
            { int active=Equipped; Costumes[i]++; Costumes.RemoveAt(j); if(active==j)Equipped=i; else if(active>j)Equipped--; Say("코스튬 성급 강화");return; }
        }
        public void Ultimate()
        {
            if(Phase!=BattlePhase.Setup || Gauge<100 || ActiveStar==0)return;
            Gauge-=100; Owned.Add(new Card(11,ActiveStar));Say("비기·단공 획득! 이번 턴 미사용 시에도 소멸");
        }
        public List<Vector2Int> Area(Card c)
        {
            var result=new List<Vector2Int>();
            if(Rules.Movement(c.Id))return result;
            if(c.Id==9 && !Valid(Player+Vector2Int.right))return result;
            foreach(var offset in Rules.Pattern(c.Id))
            {
                var p=Player+offset;
                if(!Valid(p)){ if(c.Id!=6 && c.Id!=9 && c.Id!=11)break; continue; }
                result.Add(p);
                if(Rules.FirstOnly(c.Id)&&At(p)!=null)break;
            }
            return result;
        }
        void Gain(int amount)
        {
            if(amount<=0)return;
            if(!WeaponUsed && ActiveStar>0) { amount+=ActiveStar; WeaponUsed=true; }
            Concentration=Mathf.Min(12,Concentration+amount);
        }
        public int Execute(Card c)
        {
            if(Rules.Movement(c.Id))return 0;
            if(c.Id==1){Sharpness+=Rules.Damage(c);Say("예리함: 다음 공격 강화");return 0;}
            if(c.Id==5){Gain(Rules.Charge(c));Say($"정심: 집중 {Concentration}");return 0;}
            if(c.Id==8){BreathBoost+=Rules.Boost(c);Say("검의 호흡: 다음 거합 강화");return 0;}
            var area=Area(c); var targets=Enemies.Where(e=>e.HP>0 && area.Contains(e.Cell)).ToList();
            if(targets.Count==0 || (Rules.Consumer(c.Id)&&c.Id!=11&&Concentration==0)){Say(Rules.Names[c.Id]+": 조건 미충족");return 0;}
            int spent=Rules.Consumer(c.Id)?Concentration:0; if(Rules.Consumer(c.Id))Concentration=0;
            int first=Mathf.FloorToInt((Rules.Damage(c)+Rules.Coefficient(c)*spent+Sharpness)*(1+(Rules.Consumer(c.Id)?BreathBoost:0)));
            foreach(var e in targets) {e.HP-=first; if(c.Id==3 && e.HP>0)e.HP-=Rules.Damage(c);}
            Sharpness=0;if(Rules.Consumer(c.Id))BreathBoost=0;
            Gain(Rules.Charge(c)); if(c.Id!=11)NormalHit=true;
            Say($"{Rules.Names[c.Id]}: {first}"+(c.Id==3?" + 추가 타격":"")+$" / {targets.Count}명 · 집중 {Concentration}");
            if(c.Id==11)Remove(c);
            return first;
        }
        public void Attack()
        {
            if(Phase!=BattlePhase.Move)return;
            var execution=BeginAttack();
            foreach(var c in execution){ if(Enemies.All(e=>e.HP<=0))break; Execute(c); }
            FinishAttack(execution);
        }
        public Card[] BeginAttack()
        {
            if(Phase==BattlePhase.TurnReward||Phase==BattlePhase.Reward||Phase==BattlePhase.Defeat)return new Card[0];
            WeaponUsed=false;NormalHit=false;Sharpness=0;BreathBoost=0;
            return Slots.Take(SequenceCapacity).Where(c=>c!=null).ToArray();
        }
        public void FinishAttack(Card[] execution)
        {
            foreach(var c in execution)if(c.Id!=11 && c.Life>0 && --c.Life==0)Remove(c);
            foreach(var c in Owned.Where(c=>c.Id==11).ToArray())Remove(c);
            Sharpness=0;BreathBoost=0; if(NormalHit)Gauge=Mathf.Min(100,Gauge+25);
            Turn++;
            if(Enemies.All(e=>e.HP<=0)){Concentration=0;Phase=BattlePhase.Reward;PrepareRewards();Say("승리! 보상 하나를 선택하세요");return;}
            foreach(var e in Enemies.Where(e=>e.HP>0))
            {
                if(e.Intent.Contains(Player)){HP-=e.Ranged?8:6;Say(e.Ranged?"직선 공격 -8 HP":"근접 공격 -6 HP");}
                if(!e.Ranged && (e.Cell-Player).sqrMagnitude>1)
                {
                    var next=StepToward(e.Cell,Player);if(next!=e.Cell)e.Cell=next;
                }
            }
            Moves=2;Phase=HP<=0?BattlePhase.Defeat:BattlePhase.Setup;Plan();
            if(Phase!=BattlePhase.Defeat)PrepareTurnReward();
        }
        Vector2Int StepToward(Vector2Int start,Vector2Int goal)
        {
            var q=new Queue<Vector2Int>();var first=new Dictionary<Vector2Int,Vector2Int>();q.Enqueue(start);first[start]=start;
            while(q.Count>0){var p=q.Dequeue();foreach(var d in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
            {var n=p+d;if(n==goal)return p==start?start:first[p];if(!Valid(n)||At(n)!=null||first.ContainsKey(n))continue;first[n]=p==start?n:first[p];q.Enqueue(n);}}
            return start;
        }
        void PrepareTurnReward()
        {
            if(Turn-LastCardSupplyTurn<TurnCardInterval){Phase=BattlePhase.Setup;Rewards.Clear();return;}
            Phase=BattlePhase.TurnReward;Rewards.Clear();
            var pool=Enumerable.Range(0,13).Where(id=>id!=11).ToList();
            for(int i=0;i<3;i++){int index=UnityEngine.Random.Range(0,pool.Count);Rewards.Add(new Card(pool[index]));pool.RemoveAt(index);}
            Say($"{Turn}턴 시작 · 카드 3장 중 1장을 선택하세요");
        }
        public void ChooseTurnCard(int index)
        {
            if(Phase!=BattlePhase.TurnReward||index<0||index>=Rewards.Count)return;
            var card=Rewards[index];Owned.Add(card);LastCardSupplyTurn=Turn;Rewards.Clear();Phase=BattlePhase.Setup;
            Say($"턴 카드 획득: {Rules.Names[card.Id]}");
        }
        void PrepareRewards()
        {
            Gold+=10;
            Rewards.Clear();var duplicate=Owned.FirstOrDefault(c=>c.Life>0&&c.Star<3);
            Rewards.Add(duplicate!=null?new Card(duplicate.Id,duplicate.Star):new Card(5));
            Rewards.Add(new Card((Wave*3)%11));Rewards.Add(new Card(Wave%3==0?12:(Wave*3+4)%11));
            Costumes.Add(1); if(Equipped<0 && Costumes.Count==1)Equipped=0;
            Say("코스튬 획득: 검술부 수련복 ★1");
        }
        public void Reward(int index) {if(Phase!=BattlePhase.Reward||index<0||index>=Rewards.Count)return;Owned.Add(Rewards[index]);LastCardSupplyTurn=Turn;Rewards.Clear();Wave++;StartWave();}
        public Battle Copy()
        {
            var b=(Battle)MemberwiseClone();b.Owned=Owned.Select(c=>c.Copy()).ToList();
            b.Slots=Slots.Select(c=>c==null?null:b.Owned[Owned.IndexOf(c)]).ToArray();
            b.Enemies=Enemies.Select(e=>e.Copy()).ToList();b.Costumes=new List<int>(Costumes);b.Log=new List<string>();b.Rewards=new List<Card>();return b;
        }
        public int[] Preview()
        {
            var b=Copy();b.WeaponUsed=false;b.Sharpness=0;b.BreathBoost=0;var values=new int[6];
            bool uncertain=false;
            var cards=b.Slots.ToArray();for(int i=0;i<b.SequenceCapacity;i++)if(cards[i]!=null && b.Enemies.Any(e=>e.HP>0))
            {if(Rules.Movement(cards[i].Id)){uncertain=true;values[i]=-1;}else values[i]=uncertain?-1:b.Execute(cards[i]);}return values;
        }
        public int PreviewCard(Card card)
        {
            if(card==null||Rules.Support(card.Id)||Rules.Movement(card.Id))return 0;
            return Copy().Execute(card.Copy());
        }
    }
}

