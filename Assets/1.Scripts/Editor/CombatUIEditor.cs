using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(StageCombatController))]
public sealed class CombatUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Play 없이 아래 미리보기를 선택하세요. Combat Canvas 하위 RectTransform의 위치·크기, 글자 크기·색상을 편집하고 씬을 저장하면 실행 시에도 유지됩니다. 게임 수치와 카드 내용은 실행 중 갱신됩니다.",MessageType.Info);
        using(new EditorGUI.DisabledScope(Application.isPlaying))
        {
            PreviewButton("기본 HUD",CombatUIPreviewMode.HUD);
            PreviewButton("학교 중앙 홀",CombatUIPreviewMode.Hall);
            PreviewButton("가방 펼치기",CombatUIPreviewMode.Bag);
            PreviewButton("턴 카드 선택창",CombatUIPreviewMode.TurnReward);
            PreviewButton("웨이브 보상창",CombatUIPreviewMode.WaveReward);
            PreviewButton("설정창",CombatUIPreviewMode.Settings);
        }
    }
    void PreviewButton(string label,CombatUIPreviewMode mode)
    {
        if(!GUILayout.Button(label))return;
        var controller=(StageCombatController)target;
        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject,"Preview combat UI");
        controller.PreviewUI(mode);EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);SceneView.RepaintAll();
    }
    [MenuItem("Tools/Explosive Wizard/Prepare TestStage UI")]
    public static void PrepareTestStage()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
        const string path="Assets/Scenes/TestStage.unity";
        if(EditorSceneManager.GetActiveScene().path!=path)
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(path);
        }
        var stage=Object.FindFirstObjectByType<Stage>();
        if(stage==null)throw new System.InvalidOperationException("TestStage requires Stage.");
        var controller=stage.GetComponent<StageCombatController>();if(controller==null)controller=Undo.AddComponent<StageCombatController>(stage.gameObject);
        foreach(var old in Object.FindObjectsByType<BagPanelUI>(FindObjectsInactive.Include,FindObjectsSortMode.None))old.gameObject.SetActive(false);
        foreach(var mode in new[]{CombatUIPreviewMode.Bag,CombatUIPreviewMode.TurnReward,CombatUIPreviewMode.WaveReward,CombatUIPreviewMode.Settings,CombatUIPreviewMode.HUD})controller.PreviewUI(mode);
        EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);EditorSceneManager.SaveScene(stage.gameObject.scene);
        Selection.activeGameObject=stage.gameObject;
        Debug.Log("TESTSTAGE UI SAVED: editable HUD, bag and reward panels without Play mode.");
    }
}
