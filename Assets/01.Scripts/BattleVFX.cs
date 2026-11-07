using System.Collections;
using UnityEngine;

public class BattleVFX : MonoBehaviour
{
    public static readonly Color Steel=new Color(.94f,.88f,.76f);
    public static readonly Color Ember=new Color(1,.48f,.42f);
    public static readonly Color Focus=new Color(.72f,.86f,.64f);
    static readonly int FlashId=Shader.PropertyToID("_Flash");
    static readonly int ChargeId=Shader.PropertyToID("_Charge");
    static readonly int DissolveId=Shader.PropertyToID("_Dissolve");
    MaterialPropertyBlock properties;
    Material particlesMaterial;
    ParticleSystem sparks, motes;
    Camera battleCamera;
    Vector3 cameraHome;
    float shakeUntil, shakeStrength;
    bool shaking;

    public void Init(GameAssets assets)
    {
        if(properties==null) properties=new MaterialPropertyBlock();
        battleCamera=Camera.main;
        if(battleCamera!=null) cameraHome=battleCamera.transform.position;
        if(assets.CombatMaterial==null) return;
        particlesMaterial=new Material(assets.CombatMaterial);
        particlesMaterial.SetFloat("_SpriteMode",0);
        sparks=CreateParticles("ImpactSparks",false);
        motes=CreateParticles("VoidMotes",true);
    }
    ParticleSystem CreateParticles(string name,bool ambient)
    {
        var go=new GameObject(name); go.transform.SetParent(transform,false);
        var ps=go.AddComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main; main.playOnAwake=false; main.loop=ambient; main.duration=8;
        main.simulationSpace=ParticleSystemSimulationSpace.World; main.maxParticles=ambient?80:256;
        main.startLifetime=ambient?new ParticleSystem.MinMaxCurve(5,8):new ParticleSystem.MinMaxCurve(.18f,.4f);
        main.startSize=ambient?new ParticleSystem.MinMaxCurve(.018f,.045f):new ParticleSystem.MinMaxCurve(.03f,.09f);
        main.startSpeed=ambient?.06f:0;
        main.startColor=ambient?new Color(.82f,.65f,.88f,.07f):Color.white;
        var emission=ps.emission; emission.enabled=ambient; emission.rateOverTime=ambient?2:0;
        var shape=ps.shape; shape.enabled=ambient;
        if(ambient) { shape.shapeType=ParticleSystemShapeType.Box; shape.scale=new Vector3(14,3,0); go.transform.position=new Vector3(0,2,0); }
        var color=ps.colorOverLifetime; color.enabled=true;
        var gradient=new Gradient(); gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(.6f,.55f),new GradientAlphaKey(0,1)});
        color.color=gradient;
        var size=ps.sizeOverLifetime; size.enabled=true;
        size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
        var renderer=go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=particlesMaterial;
        renderer.renderMode=ParticleSystemRenderMode.Billboard; renderer.sortingOrder=ambient?-5:40;
        if(ambient) ps.Play();
        return ps;
    }
    public void SetVisual(SpriteRenderer renderer,float flash=0,float charge=0,float dissolve=0)
    {
        if(renderer==null) return;
        // Unity can clear nonserialized fields during a script reload. Recreate on the main thread.
        if(properties==null) properties=new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        properties.SetFloat(FlashId,flash); properties.SetFloat(ChargeId,charge); properties.SetFloat(DissolveId,dissolve);
        renderer.SetPropertyBlock(properties);
    }
    public void Burst(Vector3 position,Color color,int count=16,float power=1)
    {
        if(sparks==null) return;
        for(int i=0;i<count;i++)
        {
            float angle=Random.value*Mathf.PI*2;
            float speed=Random.Range(.6f,2.6f)*power;
            var emit=new ParticleSystem.EmitParams {
                position=position+new Vector3(Random.Range(-.2f,.2f),Random.Range(-.2f,.2f),-.1f),
                velocity=new Vector3(Mathf.Cos(angle)*speed,Mathf.Sin(angle)*speed,0),
                startColor=color, startSize=Random.Range(.025f,.085f), startLifetime=Random.Range(.15f,.42f)
            };
            sparks.Emit(emit,1);
        }
    }
    public void Slash(Vector3 position,Color color,bool heavy,int variant=0)
    {
        if(particlesMaterial==null) return;
        StartCoroutine(SlashTrail(position,color,heavy,variant));
    }
    public IEnumerator Interference(Vector3 source,Vector3 target,Color color,bool guard,float speed)
    {
        if(particlesMaterial==null) { Burst(target,color,10,.45f); yield break; }
        var go=new GameObject("InterferenceRune"); go.transform.SetParent(transform,false);
        var line=go.AddComponent<LineRenderer>(); line.sharedMaterial=particlesMaterial;
        line.useWorldSpace=true; line.loop=true; line.positionCount=16; line.sortingOrder=38; line.widthMultiplier=.055f;
        Burst(source,color,8,.4f);
        for(float t=0;t<.32f;t+=Time.deltaTime*speed)
        {
            float p=t/.32f;
            var center=guard?source:Vector3.Lerp(source,target,p)+Vector3.up*(Mathf.Sin(p*Mathf.PI)*.35f);
            float radius=guard?Mathf.Lerp(.25f,.65f,p):.1f;
            for(int i=0;i<16;i++)
            {
                float angle=(i/16f+p*.3f)*Mathf.PI*2;
                line.SetPosition(i,center+new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,-.2f));
            }
            var tint=color; tint.a=guard?1-p*.7f:1; line.startColor=line.endColor=tint;
            yield return null;
        }
        Burst(target,color,guard?14:12,guard?.55f:.45f);
        Destroy(go);
    }
    IEnumerator SlashTrail(Vector3 center,Color color,bool heavy,int variant)
    {
        var go=new GameObject("BladeTrail"); go.transform.SetParent(transform,false);
        var line=go.AddComponent<LineRenderer>(); line.sharedMaterial=particlesMaterial;
        line.useWorldSpace=true; line.positionCount=9; line.sortingOrder=35;
        line.widthMultiplier=heavy?.13f:.065f;
        line.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.35f,1),new Keyframe(.7f,.75f),new Keyframe(1,0));
        float size=heavy?1.7f:1.1f;
        for(int i=0;i<9;i++)
        {
            float x=(i/8f-.5f)*2;
            float y=Mathf.Sin(i/8f*Mathf.PI)*.35f+x*(variant%2==0?.4f:-.4f);
            line.SetPosition(i,center+new Vector3(x*size,y*size,-.2f));
        }
        for(float t=0;t<.18f;t+=Time.deltaTime)
        {
            Color tint=Color.Lerp(Color.white,color,t/.18f); tint.a=1-t/.18f;
            line.startColor=line.endColor=tint; yield return null;
        }
        Destroy(go);
    }
    public void Shake(float strength=.045f)
    {
        shakeStrength=Mathf.Max(shakeStrength,strength); shakeUntil=Time.unscaledTime+.14f; shaking=true;
    }
    void LateUpdate()
    {
        if(!shaking || battleCamera==null) return;
        float remaining=shakeUntil-Time.unscaledTime;
        if(remaining<=0) { battleCamera.transform.position=cameraHome; shaking=false; shakeStrength=0; return; }
        var offset=Random.insideUnitCircle*shakeStrength*(remaining/.14f);
        battleCamera.transform.position=cameraHome+new Vector3(offset.x,offset.y,0);
    }
    void OnDestroy()
    {
        if(shaking && battleCamera!=null) battleCamera.transform.position=cameraHome;
        if(particlesMaterial!=null) Destroy(particlesMaterial);
    }
}
