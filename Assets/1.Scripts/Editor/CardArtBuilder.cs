using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CardArtBuilder
{
    [MenuItem("Tools/Explosive Wizard/Apply Custom Card Frame")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
        const string scene="Assets/Scenes/TestStage.unity";
        if(EditorSceneManager.GetActiveScene().path!=scene)
        {if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(scene);}
        var controller=UnityEngine.Object.FindFirstObjectByType<StageCombatController>();
        var art=controller.GetComponent<StageArt>();
        art.cardFrame=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4.UI/Cards/CardFrame.png");art.cardStar=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/4.UI/Cards/CardStar.png");
        if(art.cardFrame==null||art.cardStar==null)throw new InvalidOperationException("Import CardFrame.png and CardStar.png first.");
        foreach(var mode in new[]{CombatUIPreviewMode.HUD,CombatUIPreviewMode.Bag,CombatUIPreviewMode.TurnReward,CombatUIPreviewMode.WaveReward,CombatUIPreviewMode.Hall})controller.PreviewUI(mode);
        EditorUtility.SetDirty(art);EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);EditorSceneManager.SaveScene(controller.gameObject.scene);
        Debug.Log("CUSTOM CARD ART SAVED: supplied frame/star, semantic card labels, sparse centered range diagram.");
    }
}
