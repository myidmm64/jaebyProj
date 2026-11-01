using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] Stage stage;

    public Stage CurrentStage => stage;
    public bool IsGameStarted => stage != null && stage.IsInitialized;

    void Start()
    {
        if (stage == null)
        {
            Debug.LogError("GameManager: assign a Stage in the Inspector before starting the game.", this);
            return;
        }

        StartGame(stage);
    }

    public void StartGame(Stage nextStage)
    {
        if (nextStage == null) throw new System.ArgumentNullException(nameof(nextStage));
        nextStage.Init();
        stage = nextStage;
        if (!stage.TryGetComponent<StageCombatController>(out _))
            stage.gameObject.AddComponent<StageCombatController>();
    }
}
