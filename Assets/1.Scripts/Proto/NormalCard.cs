using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NormalCard : Card
{
    public List<Sprite> numberSprites = new List<Sprite>();
    public SpriteRenderer numSprite = null;

    protected override void ChildInit(int num)
    {
        if(numSprite == null) numSprite = GetComponentInChildren<SpriteRenderer>();

        numSprite.sprite = numberSprites[num - 1];
    }
    
    private void OnMouseEnter()
    {

    }
}
