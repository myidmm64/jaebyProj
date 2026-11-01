using UnityEngine;

/// <summary>Scene-authored art references shared by the hub and combat presentation.</summary>
public sealed class StageArt : MonoBehaviour
{
    public Sprite player,slime,pebble,panel;
    public Sprite cardFrame,cardStar;
    public GameObject meadow,hall;
    public SpriteRenderer voidEntrance;
    public GameObject[] legacyMaps;
    public void ShowLocation(bool atHall)
    {
        if(meadow!=null)meadow.SetActive(!atHall);if(hall!=null)hall.SetActive(atHall);
        if(legacyMaps!=null)foreach(var old in legacyMaps)if(old!=null)old.SetActive(false);
    }
    public void Dress(GridObject unit,bool ranged=false)
    {
        var sprite=unit is Player?player:ranged?pebble:slime;if(sprite==null)return;
        foreach(var animator in unit.GetComponentsInChildren<Animator>(true))animator.enabled=false;
        var renderer=unit.GetComponent<SpriteRenderer>();if(renderer==null)renderer=unit.gameObject.AddComponent<SpriteRenderer>();
        foreach(var other in unit.GetComponentsInChildren<SpriteRenderer>(true))other.enabled=other==renderer;
        renderer.enabled=true;renderer.sprite=sprite;renderer.color=Color.white;renderer.flipX=false;renderer.flipY=false;renderer.sortingOrder=20;
        var stage=GetComponent<Stage>();unit.transform.localScale=Vector3.one*(stage.CellSize*.9f);unit.transform.localRotation=Quaternion.identity;
    }
}
