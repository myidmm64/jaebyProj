using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.InputSystem;

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private Stage stagePrefab;
    [SerializeField] private GameAssets assets;

    public Stage CurrentStage { get; private set; }
    public RunEngine Run { get; private set; }
    public GameUI UI { get; private set; }
    public GameAssets Assets => assets;
    public GameAudio Audio { get; private set; }
    public bool Busy { get; private set; }
    public bool InMenu { get; private set; } = true;
    public float Speed { get; private set; }
    float lastShortcut=-1;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        if (stagePrefab == null)
        {
            Debug.LogError("Stage prefab is not assigned.", this);
            return;
        }

        CurrentStage = Instantiate(stagePrefab);
        CurrentStage.Init();
        Speed=PlayerPrefs.GetFloat("BattleSpeedV2",2);
        Audio=gameObject.AddComponent<GameAudio>();
        UI=gameObject.AddComponent<GameUI>();
        UI.Init(this);
        UI.ShowTitle();
    }

    public void NewRun()
    {
        Run=RunEngine.New(System.Environment.TickCount);
        InMenu=false; Busy=false; CurrentStage.Show(Run); UI.CloseOverlay(); Commit();
        if(PlayerPrefs.GetInt("ReadIntro",0)==0) UI.ShowHelp(true);
    }
    public void ContinueRun()
    {
        var saved=RunSave.Read(); if(saved==null) { UI.Toast("이어할 기록이 없습니다."); return; }
        Run=new RunEngine(saved); InMenu=false; Busy=false; CurrentStage.Show(Run); UI.CloseOverlay(); UI.Refresh();
    }
    public void ReturnToTitle()
    {
        if(Busy) return;
        InMenu=true; UI.ShowTitle();
    }
    public void SetSpeed()
    {
        Speed=Speed<1.5f?2:Speed<2.5f?3:1;
        PlayerPrefs.SetFloat("BattleSpeedV2",Speed); PlayerPrefs.Save(); UI.Refresh();
    }
    public void Commit()
    {
        if(!Busy) RunSave.Write(Run.State);
        UI.Refresh();
    }
    public void Execute()
    {
        if(Busy || InMenu || UI.ModalOpen || Run==null) return;
        if(!Run.CanExecute) { UI.Toast("봉인되지 않은 칸에 실행할 카드를 편성해 주세요."); return; }
        StartCoroutine(PlaySequence());
    }
    IEnumerator PlaySequence()
    {
        Busy=true; UI.BeginCombat();
        var turn=new TurnContext();
        var sequence=(int[])Run.State.Sequence.Clone();
        for(int i=0;i<6;i++)
        {
            if(i==Run.State.SealedSlot) continue;
            var card=Run.State.Find(sequence[i]); if(card==null || Run.IsCardSealed(card.Uid)) continue;
            UI.Highlight(i); Audio.Play(card.Data.Effect==CardEffect.Ultimate?GameSound.Ultimate:card.Data.IsAttack?GameSound.Slash:GameSound.Focus);
            yield return new WaitForSeconds(.22f/Speed);
            var result=Run.Resolve(card,turn,i);
            yield return CurrentStage.Animate(result,Speed);
            UI.RefreshBattle();
            yield return new WaitForSeconds(.15f/Speed);
            if(Run.Cleared) break;
        }
        if(!Run.Cleared && Run.State.Turn<Run.TurnLimit) yield return CurrentStage.AnimateEnemyIntents(Speed);
        Run.EndTurn(); Busy=false; UI.EndCombat();
        if(Run.State.Phase==RunPhase.Defeat) Audio.Play(GameSound.Lose);
        if(Run.State.Phase==RunPhase.CardReward || Run.State.Phase==RunPhase.Victory) Audio.Play(GameSound.Win);
        Commit();
    }
    public void ChooseCard(int index)
    {
        if(Busy || Run==null) return;
        if(!Run.ChooseCard(index)) { UI.Toast("가방이 가득 찼습니다. 합성·판매하거나 골드를 선택하세요."); return; }
        ChangedEncounter();
    }
    public void ChooseItem(int index) { Run.ChooseItem(index); ChangedEncounter(); }
    public void LeaveShop() { Run.LeaveShop(); ChangedEncounter(); }
    void ChangedEncounter()
    {
        UI.CloseOverlay(); CurrentStage.Show(Run); Audio.Play(GameSound.Reward); Commit();
    }
    public void Merge()
    {
        if(Busy || InMenu) return;
        int count=Run.MergeAll(); UI.Toast(count>0?count+"회 합성했습니다.":"같은 카드·같은 성급 2장이 필요합니다.");
        if(count>0) Audio.Play(GameSound.Reward); Commit();
    }
    public void Sell(int uid)
    {
        if(InMenu || Run==null) return;
        var card=Run.State.Find(uid); if(card==null) return;
        int price=Run.SellPrice(card);
        string cardName=card.Data.Name;
        if(Run.Sell(uid))
        {
            Audio.Play(GameSound.Coin); Commit(); UI.AfterSale();
            UI.Toast(cardName+" 판매  +"+price+" 골드");
        }
        else UI.Toast("마지막 카드 또는 궁극기 카드는 판매할 수 없습니다.");
    }
    public void MergeCards(int sourceUid,int targetUid)
    {
        if(Busy || InMenu || Run==null || !Run.MergeInto(sourceUid,targetUid)) return;
        Audio.Play(GameSound.Reward); Commit();
        UI.Select(targetUid); UI.Toast(Run.State.Find(targetUid).Data.Name+" "+Run.State.Find(targetUid).Stars+"성 합성");
        if(UI.InventoryOpen) UI.ShowInventory();
    }
    public void RerollShop()
    {
        if(Busy || InMenu || Run==null) return;
        if(!Run.RerollShop()) { UI.Toast("리롤할 골드가 부족합니다."); return; }
        Audio.Play(GameSound.Coin); Commit();
    }
    public void Ultimate()
    {
        if(Busy || InMenu || Run==null) return;
        if(Run.Ultimate()) { Audio.Play(GameSound.Reward); UI.Toast("대거합 획득! 이번 턴 안에 시퀀스에 넣어 사용하세요."); Commit(); }
        else UI.Toast(Run.State.Ultimate>=100?"궁극기 카드가 이미 있거나 가방·장착 칸이 가득 찼습니다.":"궁극기 게이지가 100 필요합니다.");
    }
    public void Equip(int uid,int slot)
    {
        if(Busy || InMenu || Run==null || UI.ModalOpen) return;
        if(slot==Run.State.SealedSlot) { UI.Toast("이번 턴에는 봉인된 칸입니다."); return; }
        if(Run.Equip(uid,slot)) { Audio.Play(GameSound.Click); Commit(); }
    }
    public void Unequip(int slot)
    {
        if(Busy || InMenu || Run==null || UI.ModalOpen) return;
        if(!Run.Unequip(slot)) { UI.Toast("가방이 가득 찼습니다. 먼저 판매하거나 합성하세요."); return; }
        Commit();
    }
    public void PlaceInBag(int uid,int slot)
    {
        if(Busy || InMenu || Run==null || UI.ModalOpen) return;
        if(Run.PlaceInBag(uid,slot)) { Audio.Play(GameSound.Click); Commit(); }
        else UI.Toast("가방이 가득 찼습니다. 먼저 판매하거나 합성하세요.");
    }
    public void AddDebugCard(int definition,int stars)
    {
        if(Busy || InMenu || Run==null || definition<0 || definition>=CardCatalog.All.Length) return;
        if(Run.BagCount>=RunEngine.BagCapacity) { UI.Toast("가방이 가득 찼습니다. 카드를 판매하거나 합성하세요."); return; }
        var card=Run.AddCard(definition,Mathf.Clamp(stars,1,4));
        Commit(); UI.Toast(card.Data.Name+" 추가");
    }
    void Update()
    {
        var k=Keyboard.current; if(k==null || UI==null) return;
        if(k.escapeKey.wasPressedThisFrame) Shortcut(KeyCode.Escape);
        if(k.spaceKey.wasPressedThisFrame) Shortcut(KeyCode.Space);
        if(k.qKey.wasPressedThisFrame) Shortcut(KeyCode.Q);
        if(k.eKey.wasPressedThisFrame) Shortcut(KeyCode.E);
        if(k.tKey.wasPressedThisFrame) Shortcut(KeyCode.T);
        if(k.tabKey.wasPressedThisFrame) Shortcut(KeyCode.Tab);
        if(k.uKey.wasPressedThisFrame) Shortcut(KeyCode.U);
        if(k.f2Key.wasPressedThisFrame) Shortcut(KeyCode.F2);
        if(k.aKey.wasPressedThisFrame) Shortcut(KeyCode.A);
        if(k.dKey.wasPressedThisFrame) Shortcut(KeyCode.D);
        if(k.wKey.wasPressedThisFrame) Shortcut(KeyCode.W);
    }
    void Shortcut(KeyCode key)
    {
        if(UI==null || Time.unscaledTime-lastShortcut<.12f) return;
        lastShortcut=Time.unscaledTime;
        if(key==KeyCode.Escape) { if(!Busy) UI.TogglePause(); return; }
        if(InMenu || Busy || Run==null) return;
        if(key==KeyCode.Tab) { UI.ToggleInventory(); return; }
        if(key==KeyCode.F2) { UI.ShowDebugCards(); return; }
        if(UI.ModalOpen && (!UI.InventoryOpen || (key!=KeyCode.Q && key!=KeyCode.E && key!=KeyCode.T))) return;
        switch(key)
        {
            case KeyCode.Space: Execute(); break;
            case KeyCode.Q: UI.Sort(); break;
            case KeyCode.E: Merge(); if(UI.InventoryOpen) UI.ShowInventory(); break;
            case KeyCode.T: UI.Sort(); break;
            case KeyCode.U: Ultimate(); break;
            case KeyCode.A: UI.Page(-1); break;
            case KeyCode.D: UI.Page(1); break;
            case KeyCode.W: UI.FlipBag(); break;
        }
    }
#if UNITY_EDITOR
    // Editor-embedded and remote keyboard events can arrive through IMGUI rather than the input device.
    void OnGUI()
    {
        var e=Event.current;
        if(e.type==EventType.KeyDown && !e.control && !e.alt && e.keyCode!=KeyCode.None) Shortcut(e.keyCode);
    }
#endif
}
