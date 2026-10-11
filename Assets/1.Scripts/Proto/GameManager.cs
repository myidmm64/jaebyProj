using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    public GameObject card = null;
    public int cardCount = 10;
    public float xPadding = 1.8f;

    public void Start()
    {
        GameInit();
    }

    private void GameInit()
    {
        for(int i = 0; i < cardCount; i++)
        {
            Card c = Instantiate(card).GetComponent<Card>();
            c.Init(i + 1);

            Vector2 startPos = new Vector2(- (cardCount / 2) * xPadding, -3.8f);
            Vector2 newPos = new Vector2(startPos.x + (i * xPadding), startPos.y);
            if (cardCount % 2 == 0) newPos.x += xPadding / 2;
            c.transform.position = newPos;
        }
    }
}
