using UnityEngine;
using WizardCombat;

public enum CombatUIPreviewMode { HUD, Bag, TurnReward, WaveReward, Settings }

public sealed partial class StageCombatController
{
    /// <summary>Builds saved UI without starting a game or changing scene actors.</summary>
    public void PreviewUI(CombatUIPreviewMode mode)
    {
        if(Application.isPlaying)return;
        stage=GetComponent<Stage>();worldCamera=Camera.main;
        if(stage==null||stage.Player==null||stage.Enemy==null||worldCamera==null)
            throw new System.InvalidOperationException("Assign Stage actors and Main Camera before previewing the HUD.");
        InitializeCanvas();
        var randomState=Random.state;
        try{battle=new Battle(stage.MapSize);}finally{Random.state=randomState;}
        actors.Clear();actors.Add(stage.Enemy);
        busy=false;choosingMove=false;selected=null;activeSlot=-1;page=0;completedCards=0;attackArea.Clear();
        bagOpen=mode==CombatUIPreviewMode.Bag;bagProgress=bagOpen?1:0;
        settingsOpen=mode==CombatUIPreviewMode.Settings;rewardInfoOpen=false;costumeOpen=false;
        battle.Phase=mode==CombatUIPreviewMode.TurnReward?BattlePhase.TurnReward:mode==CombatUIPreviewMode.WaveReward?BattlePhase.Reward:BattlePhase.Setup;
        Refresh();LateUpdate();
    }
}
