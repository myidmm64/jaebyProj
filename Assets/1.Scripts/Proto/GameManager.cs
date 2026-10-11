using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    public GameObject card = null;

    public void Start()
    {
        GameInit();
    }

    private void GameInit()
    {
        for(int i = 0; i < 10; i++)
        {
            Card c = Instantiate(card).GetComponent<Card>();
            c.Init(i + 1);
        }
    }
}
