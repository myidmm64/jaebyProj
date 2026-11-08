using System;
using System.IO;
using UnityEngine;

public static class RunSave
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    static extern void ImpactOrderSyncSave();
#endif
    static string PathName => Path.Combine(Application.persistentDataPath,"Run.json");
    static bool saveFailed;
    public static string LastError => saveFailed?GameLocalization.Text("저장하지 못했습니다. 디스크 공간을 확인해 주세요."):null;
    public static void Write(RunState state)
    {
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            string json=JsonUtility.ToJson(state,true);
            File.WriteAllText(PathName+".tmp",json);
            if(File.Exists(PathName)) File.Copy(PathName,PathName+".bak",true);
            File.Copy(PathName+".tmp",PathName,true);
            File.Delete(PathName+".tmp"); saveFailed=false;
#if UNITY_WEBGL && !UNITY_EDITOR
            ImpactOrderSyncSave();
#endif
        }
        catch(Exception e) { saveFailed=true; Debug.LogWarning(e.Message); }
    }
    public static RunState Read()
    {
        // The product was renamed; keep existing runs available without changing their source files.
        string parent=Path.GetDirectoryName(Application.persistentDataPath)??Application.persistentDataPath;
        string previous=Path.Combine(parent,"Imporder","Run.json");
        string original=Path.Combine(parent,"Zanshin","Run.json");
        foreach(string path in new[]{PathName,PathName+".bak",previous,previous+".bak",original,original+".bak"})
        {
            try
            {
                if(!File.Exists(path)) continue;
                var s=JsonUtility.FromJson<RunState>(File.ReadAllText(path));
                if(s==null || s.Version<1 || s.Version>5 || s.Floor<1 || s.Floor>3 || s.Node<0 || s.Node>4 || s.Sequence?.Length!=6 || s.Items==null || (s.Items.Length!=6 && s.Items.Length!=ItemCatalog.Count) || s.Cards==null || s.Enemies==null || s.Offers?.Length!=3 || s.Bought?.Length!=3) continue;
                if(s.Cards.Exists(c=>c==null) || s.Enemies.Exists(e=>e==null || e.Kind<0 || e.Kind>=EnemyCatalog.All.Length || e.Health<0 || e.MaxHealth<=0 || e.Health>e.MaxHealth || !Enum.IsDefined(typeof(EnemyIntent),e.Intent))) continue;
                RunEngine.MigrateSavedState(s);
                if(s.Cards.Count>RunEngine.BagCapacity+7 || s.Cards.Exists(c=>c.Definition<0 || c.Definition>=CardCatalog.All.Length || c.Stars<1 || c.Stars>4)) continue;
                var ids=new System.Collections.Generic.HashSet<int>();
                if(s.Cards.Exists(c=>c.Uid<=0 || !ids.Add(c.Uid))) continue;
                foreach(var card in s.Cards) s.NextUid=Math.Max(s.NextUid,card.Uid+1);
                if(s.Sequence.AnyInvalid(s.Cards) || s.Turn<1 || s.Wave<0 || s.Wave>2 || s.Target<0 || s.Gold<0 || !Enum.IsDefined(typeof(RunPhase),s.Phase)) continue;
                bool badOffer=false;
                foreach(int id in s.Offers) if(id<0 || id>=(s.Phase==RunPhase.ItemReward?ItemCatalog.Count:CardCatalog.All.Length)) badOffer=true;
                if(badOffer || s.Items.AnyNegative()) continue;
                return s;
            }
            catch(Exception e) { Debug.LogWarning("저장 파일을 읽지 못했습니다: "+e.Message); }
        }
        return null;
    }
    static bool AnyInvalid(this int[] slots,System.Collections.Generic.List<OwnedCard> cards)
    {
        var found=new System.Collections.Generic.HashSet<int>();
        foreach(int id in slots) if(id!=-1 && (!found.Add(id) || !cards.Exists(c=>c.Uid==id))) return true;
        return false;
    }
    static bool AnyNegative(this int[] values) { foreach(int value in values) if(value<0) return true; return false; }
}
