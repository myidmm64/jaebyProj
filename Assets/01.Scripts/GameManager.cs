using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    [SerializeField] private Stage stagePrefab;

    public Stage CurrentStage { get; private set; }

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
    }
}
