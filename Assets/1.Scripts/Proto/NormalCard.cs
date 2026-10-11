using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class NormalCard : Card
{
    public List<Sprite> numberSprites = new List<Sprite>();
    public SpriteRenderer numSprite = null;

    public SpriteRenderer cardSpriteRenderer = null;
    public Sprite unselectedSprite = null;
    public Sprite selectedSprite = null;
    public bool selected = false;

    protected override void ChildInit(int num)
    {
        if(numSprite == null) numSprite = GetComponentInChildren<SpriteRenderer>();

        numSprite.sprite = numberSprites[num - 1];
    }

    private void OnMouseDown()
    {
        selected = !selected;
        if (selected)
        {
            cardSpriteRenderer.sprite = selectedSprite;
        }
        else
        {
            cardSpriteRenderer.sprite = unselectedSprite;
        }
    }
}
