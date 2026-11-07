using System;
using UnityEngine;

public enum GameSound { Click, Slash, Focus, Coin, Reward, Win, Lose }
public class GameAudio : MonoBehaviour
{
    AudioSource effects, music;
    AudioClip[] clips;
    public bool Muted { get; private set; }
    void Awake()
    {
        effects=gameObject.AddComponent<AudioSource>(); music=gameObject.AddComponent<AudioSource>();
        clips=new AudioClip[7];
        float[] freq={550,180,740,1100,880,660,220};
        for(int i=0;i<7;i++) clips[i]=Tone("UI"+i,freq[i],i==0?.065f:.24f,i==1);
        int rate=22050; float seconds=12.8f; var samples=new float[(int)(rate*seconds)];
        int[] notes={57,64,69,72,71,64,59,64,57,60,64,69,67,64,59,55};
        for(int i=0;i<samples.Length;i++)
        {
            float time=i/(float)rate; int step=(int)(time/.8f); float local=time% .8f;
            double f=440*Math.Pow(2,(notes[step%notes.Length]-69)/12d);
            samples[i]=(float)(Math.Sin(2*Math.PI*f*time)*Math.Exp(-local*5)*.08+Math.Sin(2*Math.PI*f*.5*time)*Math.Exp(-local*3)*.05);
        }
        var bgm=AudioClip.Create("Courtyard",samples.Length,1,rate,false); bgm.SetData(samples,0); music.clip=bgm; music.loop=true; music.volume=.4f; music.Play();
        Muted=PlayerPrefs.GetInt("Muted",0)==1; Apply();
    }
    AudioClip Tone(string name,float frequency,float duration,bool noise)
    {
        int rate=22050; var data=new float[(int)(rate*duration)]; var random=new System.Random(15);
        for(int i=0;i<data.Length;i++) { double t=i/(double)rate; double wave=noise?(random.NextDouble()*2-1)*.45+Math.Sin(2*Math.PI*frequency*t*(1-t))*.55:Math.Sin(2*Math.PI*frequency*t)+Math.Sin(2*Math.PI*frequency*1.5*t)*.3; data[i]=(float)(wave*Math.Exp(-t*18)*.18); }
        var c=AudioClip.Create(name,data.Length,1,rate,false); c.SetData(data,0); return c;
    }
    public void Play(GameSound sound) { if(!Muted && effects!=null) effects.PlayOneShot(clips[(int)sound]); }
    public void ToggleMute() { Muted=!Muted; PlayerPrefs.SetInt("Muted",Muted?1:0); PlayerPrefs.Save(); Apply(); }
    void Apply() { music.mute=Muted; effects.mute=Muted; }
}
