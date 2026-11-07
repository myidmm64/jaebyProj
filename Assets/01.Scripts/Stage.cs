using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Stage : MonoBehaviour
{
    readonly List<Enemy> enemies=new List<Enemy>();
    readonly List<Vector3> home=new List<Vector3>();
    readonly List<SpriteRenderer> visuals=new List<SpriteRenderer>();
    readonly List<Vector3> visualScale=new List<Vector3>();
    readonly List<Vector3> visualHome=new List<Vector3>();
    readonly List<SpriteRenderer> shadows=new List<SpriteRenderer>();
    const float BaseGroundY=.70f;
    float battleLift;
    float GroundY => BaseGroundY+battleLift;
    Player player;
    SpriteRenderer playerVisual;
    Vector3 playerVisualScale;
    Vector3 playerVisualHome;
    BattleVFX vfx;
    Vector3 playerHome;
    float playerHeight=2.3f;
    RunEngine run;
    bool animating;
    Coroutine spawnRoutine;
    public Vector3 PlayerBuffAnchor => (player!=null?player.transform.position:playerHome)+new Vector3(-.8f,playerHeight*.55f,0);
    Vector3 PlayerCenter => playerHome+Vector3.up*(playerHeight*.5f);

    public void Init()
    {
        player=FindAnyObjectByType<Player>();
        var assets=GameManager.Instance.Assets;
        if(assets.PlayerHeight>0)playerHeight=assets.PlayerHeight;
        vfx=gameObject.AddComponent<BattleVFX>(); vfx.Init(assets);
        if(player!=null)
        {
            playerHome=new Vector3(-3.8f,GroundY,0); player.transform.position=playerHome;
            playerVisual=player.GetComponentInChildren<SpriteRenderer>();
            if(playerVisual!=null)
            {
                if(assets.PlayerArt!=null)playerVisual.sprite=assets.PlayerArt;
                playerVisual.transform.localScale=Vector3.one; playerVisual.transform.localPosition=Vector3.zero;
                player.transform.localScale=Vector3.one;
                playerVisualScale=playerVisual.transform.localScale; playerVisualHome=playerVisual.transform.localPosition;
                if(assets.CombatMaterial!=null)playerVisual.sharedMaterial=assets.CombatMaterial;
                playerVisual.color=Color.white;
            }
            var shadow=GameObject.Find("PlayerGroundShadow");
            if(shadow==null) CreateShadow(playerHome.x,1.3f).name="PlayerGroundShadow";
            else shadow.transform.position=new Vector3(playerHome.x,GroundY,0);
        }
        foreach(var e in FindObjectsByType<Enemy>()) { e.gameObject.SetActive(false); Destroy(e.gameObject); }
    }
    public void Show(RunEngine engine)
    {
        run=engine;
        StopAllCoroutines(); spawnRoutine=null; animating=false;
        if(player!=null) player.transform.position=playerHome;
        foreach(var e in enemies) if(e!=null) { e.gameObject.SetActive(false); Destroy(e.gameObject); }
        foreach(var shadow in shadows) if(shadow!=null) Destroy(shadow.gameObject);
        enemies.Clear(); home.Clear(); visuals.Clear(); visualScale.Clear(); visualHome.Clear(); shadows.Clear();
        var assets=GameManager.Instance.Assets;
        for(int i=0;i<engine.State.Enemies.Count;i++)
        {
            var state=engine.State.Enemies[i];
            var definition=EnemyCatalog.Get(state.Kind);
            var e=Instantiate(definition.Body==0?assets.SlimePrefab:assets.StonePrefab,transform);
            float height=GroundY+(state.Rear?.6f:0)+(definition.Floating?.14f:0);
            float x=state.Boss?2.55f:engine.IsBoss?(i==1?4.8f:.8f):engine.State.Enemies.Count<=2?1.15f+i*2.3f:new[]{1.05f,3.65f,2.25f,5.1f}[i%4];
            Vector3 pos=new Vector3(x,height,0);
            e.transform.position=pos;
            var visual=e.GetComponentInChildren<SpriteRenderer>();
            if(visual!=null)
            {
                if(assets.EnemyArt!=null && state.Kind<assets.EnemyArt.Length && assets.EnemyArt[state.Kind]!=null)visual.sprite=assets.EnemyArt[state.Kind];
                if(state.Boss && assets.BossArt!=null && engine.State.Floor<=assets.BossArt.Length && assets.BossArt[engine.State.Floor-1]!=null)visual.sprite=assets.BossArt[engine.State.Floor-1];
                visual.transform.localScale=Vector3.one; visual.transform.localPosition=Vector3.zero;
                visual.sortingOrder=state.Rear?0:2;
            }
            if(visual!=null && assets.CombatMaterial!=null) visual.sharedMaterial=assets.CombatMaterial;
            if(visual!=null) visual.color=assets.EnemyArt!=null && state.Kind<assets.EnemyArt.Length && assets.EnemyArt[state.Kind]!=null?Color.white:new Color(definition.Tint[0],definition.Tint[1],definition.Tint[2]);
            if(state.Boss && visual!=null)
            {
                visual.color=Color.white;
            }
            e.Configure(state,state.Boss);
            e.gameObject.SetActive(state.Health>0);
            enemies.Add(e); home.Add(pos); visuals.Add(visual); visualScale.Add(visual!=null?visual.transform.localScale:Vector3.one);
            visualHome.Add(visual!=null?visual.transform.localPosition:Vector3.zero);
            var groundShadow=CreateShadow(pos.x,state.Boss?1.8f:definition.Body==0?1.35f:1.1f);
            groundShadow.transform.position+=Vector3.up*(state.Rear?.6f:0);
            groundShadow.gameObject.SetActive(state.Health>0); shadows.Add(groundShadow);
        }
        var bg=GameObject.Find("BattleBackground");
        if(bg!=null) bg.GetComponent<SpriteRenderer>().color=engine.State.Floor==1?Color.white:engine.State.Floor==2?new Color(.94f,.92f,1):new Color(1,.88f,.94f);
        if(engine.State.Phase==RunPhase.Battle) spawnRoutine=StartCoroutine(Spawn());
    }
    SpriteRenderer CreateShadow(float x,float width)
    {
        var go=new GameObject("GroundShadow"); go.transform.SetParent(transform,false);
        go.transform.position=new Vector3(x,GroundY,0); go.transform.localScale=new Vector3(width,1,1);
        var renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=GameManager.Instance.Assets.GroundShadow;
        renderer.sortingOrder=-2; renderer.color=new Color(1,1,1,.7f);
        if(GameManager.Instance.Assets.CombatMaterial!=null) renderer.sharedMaterial=GameManager.Instance.Assets.CombatMaterial;
        return renderer;
    }
    IEnumerator Spawn()
    {
        for(int i=0;i<visuals.Count;i++) if(enemies[i].gameObject.activeSelf) vfx.Burst(home[i],BattleVFX.Steel,8,.4f);
        for(float t=0;t<.3f;t+=Time.deltaTime)
        {
            float scale=Mathf.SmoothStep(.8f,1,t/.3f);
            for(int i=0;i<visuals.Count;i++) if(visuals[i]!=null) visuals[i].transform.localScale=visualScale[i]*scale;
            yield return null;
        }
        for(int i=0;i<visuals.Count;i++) if(visuals[i]!=null) visuals[i].transform.localScale=visualScale[i];
    }
    public Vector3 Position(int index)
    {
        if(index<0 || index>=home.Count)return new Vector3(2,2,0);
        float height=GameManager.Instance.Assets.UnitHeight(run.State.Enemies[index].Kind,run.State.Floor,run.State.Enemies[index].Boss);
        return home[index]+Vector3.up*(height*.5f);
    }
    public Vector3 HeartPosition(int index) => index>=0 && index<enemies.Count?enemies[index].HeartPosition:Position(index);
    public Vector3 BuffPosition(int index) => index>=0 && index<enemies.Count?enemies[index].BuffPosition:Position(index);
    public void SetTarget(int index) { for(int i=0;i<enemies.Count;i++) if(enemies[i]!=null) enemies[i].SetTargeted(i==index); }
    public void SyncHealth() { if(run!=null)for(int i=0;i<enemies.Count;i++)if(enemies[i]!=null)enemies[i].Health=run.State.Enemies[i].Health; }
    public void SetBattleLayout(float extraHeight,float canvasHeight)
    {
        var camera=Camera.main; if(camera==null || canvasHeight<=0) return;
        float lift=extraHeight/canvasHeight*camera.orthographicSize*2;
        if(Mathf.Approximately(lift,battleLift)) return;
        var offset=Vector3.up*(lift-battleLift); battleLift=lift;
        playerHome+=offset; if(player!=null) player.transform.position+=offset;
        for(int i=0;i<home.Count;i++) { home[i]+=offset; if(enemies[i]!=null) enemies[i].transform.position+=offset; }
        foreach(var shadow in shadows) if(shadow!=null) shadow.transform.position+=offset;
        var playerShadow=GameObject.Find("PlayerGroundShadow"); if(playerShadow!=null) playerShadow.transform.position+=offset;
        var background=GameObject.Find("BattleBackground");
        if(background!=null) background.GetComponent<BackgroundCover>()?.SetVerticalOffset(lift);
    }
    public IEnumerator AnimateEnemyIntents(float speed)
    {
        animating=true;
        for(int i=0;i<run.State.Enemies.Count;i++)
        {
            var state=run.State.Enemies[i]; if(state.Health<=0) continue;
            bool guard=state.Intent==EnemyIntent.Guard;
            bool healing=state.Intent==EnemyIntent.Regenerate || state.Intent==EnemyIntent.MendAllies;
            bool fortifying=state.Intent==EnemyIntent.Fortify;
            bool self=guard || healing || fortifying;
            var tint=healing?BattleVFX.Focus:guard || fortifying?new Color(.5f,.84f,1):new Color(.88f,.58f,1);
            GameManager.Instance.Audio.Play(GameSound.Interference);
            for(float t=0;t<.22f;t+=Time.deltaTime*speed)
            {
                float pulse=Mathf.Sin(t/.22f*Mathf.PI);
                enemies[i].transform.position=home[i]+(guard?Vector3.up:Vector3.left)*(.13f*pulse);
                if(visuals[i]!=null) visuals[i].transform.localScale=Vector3.Scale(visualScale[i],new Vector3(1+.08f*pulse,1-.05f*pulse,1));
                vfx.SetVisual(visuals[i],pulse*.18f,pulse*.9f);
                yield return null;
            }
            enemies[i].transform.position=home[i];
            if(visuals[i]!=null) { visuals[i].transform.localScale=visualScale[i]; visuals[i].transform.localPosition=visualHome[i]; }
            yield return vfx.Interference(Position(i),self?Position(i):PlayerCenter,tint,self,speed);
            if(fortifying || state.Intent==EnemyIntent.MendAllies)
                for(int target=0;target<enemies.Count;target++)if(run.State.Enemies[target].Health>0)vfx.Burst(Position(target),tint,6,.32f);
            if(!self) { vfx.SetVisual(playerVisual,.2f,.65f); yield return new WaitForSeconds(.08f/speed); }
            vfx.SetVisual(visuals[i],0,state.Boss?.35f:0);
        }
        vfx.SetVisual(playerVisual); animating=false;
    }
    public IEnumerator Animate(CardResult result,float speed)
    {
        if(spawnRoutine!=null)
        {
            StopCoroutine(spawnRoutine); spawnRoutine=null;
            for(int i=0;i<visuals.Count;i++) if(visuals[i]!=null) visuals[i].transform.localScale=visualScale[i];
        }
        animating=true;
        var ui=GameManager.Instance.UI;
        if(result.Card.Data.IsAttack)
        {
            bool heavy=result.Card.Data.Iai || result.Card.Data.Effect==CardEffect.Ultimate;
            Color color=heavy?BattleVFX.Ember:BattleVFX.Steel;
            if(player!=null) player.transform.position=playerHome+Vector3.right*(heavy?.65f:.35f);
            foreach(int i in result.Targets)
            {
                vfx.Slash(Position(i),color,heavy,i);
                vfx.Burst(Position(i),color,heavy?24:12,heavy?1.3f:1);
                if(i<enemies.Count) enemies[i].Health=run.State.Enemies[i].Health;
            }
            vfx.Shake(heavy?.075f:.03f);
            Vector3 target=Position(result.Targets.Count>0?result.Targets[0]:0);
            string damageText=GameMath.Number(result.Damage)+(string.IsNullOrEmpty(result.ChanceText)?"":"\n"+result.ChanceText);
            if(result.BarrierDamage>0)damageText+="\n보호막 "+GameMath.Number(result.BarrierDamage);
            ui.Floating(target+Vector3.up*.8f,damageText,result.ChanceSuccess?new Color(1,.87f,.4f):heavy?new Color(1,.75f,.4f):Color.white,heavy?48:38);
            yield return new WaitForSecondsRealtime(heavy?.045f:.02f);
            for(float t=0;t<.32f;t+=Time.deltaTime*speed)
            {
                float p=t/.32f;
                foreach(int i in result.Targets) if(i<enemies.Count && enemies[i]!=null)
                {
                    bool dead=run.State.Enemies[i].Health<=0;
                    vfx.SetVisual(visuals[i],Mathf.Max(0,1-p*4),run.State.Enemies[i].Boss?.35f:0,dead?Mathf.Clamp01((p-.3f)/.7f):0);
                    enemies[i].transform.position=home[i]+Vector3.right*Mathf.Sin(p*Mathf.PI)*.18f;
                    if(visuals[i]!=null) visuals[i].transform.localScale=Vector3.Scale(visualScale[i],new Vector3(1+.1f*Mathf.Sin(p*Mathf.PI*2),1-.08f*Mathf.Sin(p*Mathf.PI*2),1));
                }
                if(player!=null) player.transform.position=Vector3.Lerp(player.transform.position,playerHome,Time.deltaTime*speed*18);
                yield return null;
            }
            foreach(int i in result.Targets) if(run.State.Enemies[i].Health<=0) vfx.Burst(Position(i),new Color(.75f,.4f,1),20,.7f);
        }
        else
        {
            string message=result.FocusAfter!=result.FocusBefore?"집중 "+GameMath.Number(result.FocusAfter):result.Card.Data.Name;
            if(!string.IsNullOrEmpty(result.ChanceText))message+="\n"+result.ChanceText;
            ui.Floating(playerHome+Vector3.up,message,new Color(.65f,1,.88f),32);
            vfx.Burst(PlayerCenter,BattleVFX.Focus,10,.5f);
            vfx.Slash(PlayerCenter,BattleVFX.Focus,false,1);
            vfx.SetVisual(playerVisual,.25f,.8f);
            yield return new WaitForSeconds(.18f/speed);
        }
        for(int i=0;i<enemies.Count;i++)
        {
            enemies[i].Health=run.State.Enemies[i].Health;
            enemies[i].gameObject.SetActive(run.State.Enemies[i].Health>0);
            shadows[i].gameObject.SetActive(run.State.Enemies[i].Health>0);
            enemies[i].transform.position=home[i];
            if(visuals[i]!=null) { visuals[i].transform.localScale=visualScale[i]; visuals[i].transform.localPosition=visualHome[i]; vfx.SetVisual(visuals[i],0,run.State.Enemies[i].Boss?.35f:0); }
        }
        if(player!=null) player.transform.position=playerHome;
        animating=false;
    }
    void LateUpdate()
    {
        if(animating) return;
        for(int i=0;i<enemies.Count;i++) if(enemies[i]!=null)
        {
            bool floating=EnemyCatalog.Get(run.State.Enemies[i].Kind).Floating;
            enemies[i].transform.position=home[i]+Vector3.up*(floating?Mathf.Sin(Time.time*2+i)*.035f:0);
            if(visuals[i]!=null)
            {
                float breath=Mathf.Sin(Time.time*2+i);
                var scale=Vector3.Scale(visualScale[i],new Vector3(1-breath*.008f,1+breath*.012f,1));
                visuals[i].transform.localScale=scale;
                visuals[i].transform.localPosition=visualHome[i]+Vector3.up*((visualScale[i].y-scale.y)*visuals[i].sprite.bounds.min.y);
                visuals[i].transform.localRotation=Quaternion.Euler(0,0,floating?Mathf.Sin(Time.time*1.5f+i)*.7f:0);
            }
        }
        if(playerVisual!=null)
        {
            playerVisual.transform.localScale=Vector3.Scale(playerVisualScale,new Vector3(1,1+Mathf.Sin(Time.time*2.3f)*.008f,1));
            playerVisual.transform.localPosition=playerVisualHome+Vector3.up*((playerVisualScale.y-playerVisual.transform.localScale.y)*playerVisual.sprite.bounds.min.y);
            float charge=run!=null?Mathf.Clamp01(Mathf.Log10(1+(float)run.State.Focus)/5):0;
            vfx.SetVisual(playerVisual,0,charge);
        }
    }
}
