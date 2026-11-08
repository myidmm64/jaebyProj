using System;

public sealed class EnemyDefinition
{
    public int Id, Body;
    string sourceName;
    public string Name { get => CatalogText.Name(sourceName); set => sourceName=value; }
    public double Health;
    public bool Floating;
    public float[] Tint;
    public EnemyIntent[] Pattern;
}

public static class EnemyCatalog
{
    static readonly EnemyIntent W=EnemyIntent.Weaken,G=EnemyIntent.Guard,S=EnemyIntent.Seal,F=EnemyIntent.DisruptFocus,
        U=EnemyIntent.SiphonUltimate,P=EnemyIntent.Pressure,R=EnemyIntent.Regenerate,C=EnemyIntent.CardSeal,
        B=EnemyIntent.Fortify,H=EnemyIntent.MendAllies,Q=EnemyIntent.Suppress;
    public static readonly EnemyDefinition[] All={
        Make(0,"슬라임",0,1600,false,new[]{1f,.83f,.94f},W,F),
        Make(1,"돌 정령",1,2400,true,new[]{.9f,.9f,1f},G,S),
        Make(2,"가시 슬라임",0,2000,false,new[]{.72f,.96f,.63f},W,G,Q),
        Make(3,"포자 슬라임",0,1500,false,new[]{.83f,.65f,1f},F,Q,U),
        Make(4,"방울 슬라임",0,1700,false,new[]{.58f,.89f,1f},P,F),
        Make(5,"철갑 조약돌",1,3400,false,new[]{.65f,.72f,.84f},G,B,W),
        Make(6,"균열 정령",1,2300,true,new[]{.73f,.59f,1f},S,C,U),
        Make(7,"마력 반딧불",1,1300,true,new[]{1f,.87f,.48f},U,W,U),
        Make(8,"새싹 정령",1,2100,true,new[]{.57f,.91f,.67f},H,G,R),
        Make(9,"밤그늘 버섯",0,1800,false,new[]{.68f,.52f,.87f},Q,C,H),
        Make(10,"공허 나방",1,1600,true,new[]{.67f,.84f,1f},W,P,U),
        Make(11,"수정 고슴도치",1,2800,false,new[]{.86f,.73f,1f},B,R,S,C)
    };
    static EnemyDefinition Make(int id,string name,int body,double health,bool floating,float[] tint,params EnemyIntent[] pattern)
        =>new EnemyDefinition{Id=id,Name=name,Body=body,Health=health,Floating=floating,Tint=tint,Pattern=pattern};
    public static EnemyDefinition Get(int kind) => All[Math.Max(0,Math.Min(All.Length-1,kind))];
    public static readonly int[][] Pools={new[]{0,1,2,3,4,5},new[]{2,3,4,5,6,7,8,9},new[]{6,7,8,9,10,11,0,1}};
    public static readonly int[][][] Formations={
        new[]{new[]{1,0,3,4},new[]{2,0,4,1},new[]{5,3,0,2},new[]{1,4,2,3}},
        new[]{new[]{5,2,8,7},new[]{2,3,6,9},new[]{5,9,7,6},new[]{6,3,8,9}},
        new[]{new[]{11,6,9,10},new[]{5,11,8,7},new[]{2,6,10,9},new[]{11,1,8,6}}
    };
    public static readonly EnemyIntent[][] BossPatterns={
        new[]{B,W,G,P,C,R},new[]{S,B,F,C,P,U},new[]{C,B,P,S,Q,U}
    };
}
