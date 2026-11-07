using System;
using UnityEngine;

public enum GameSound { Click, Slash, Focus, Coin, Reward, Win, Lose, Interference, Ultimate }

public class GameAudio : MonoBehaviour
{
    const int Rate=22050;
    const double Bpm=144;
    AudioSource effects, music;
    AudioClip[] clips;
    int variation;
    public bool Muted { get; private set; }

    void Awake()
    {
        effects=gameObject.AddComponent<AudioSource>();
        music=gameObject.AddComponent<AudioSource>();
        effects.playOnAwake=music.playOnAwake=false;
        effects.spatialBlend=music.spatialBlend=0;
        effects.volume=.8f; music.volume=.56f;
        effects.priority=48; music.priority=128;
        clips=new AudioClip[Enum.GetValues(typeof(GameSound)).Length];
        for(int i=0;i<clips.Length;i++)clips[i]=MakeEffect((GameSound)i);
        music.clip=MakeMusic(); music.loop=true;
        Muted=PlayerPrefs.GetInt("Muted",0)==1; Apply(); music.Play();
    }
    static double Frequency(int midi) => 440*Math.Pow(2,(midi-69)/12d);
    static double Envelope(double t,double length,double decay)
        =>Math.Min(1,t/.005)*Math.Min(1,Math.Max(0,(length-t)/.015))*Math.Exp(-t*decay);

    // Stereo voices are mixed once at startup; no per-frame music synthesis.
    static void Voice(float[] data,double start,double length,int midi,double gain,int shape=0,double pan=0,double decay=7)
    {
        if(midi<0)return;
        int from=(int)(start*Rate), count=(int)(length*Rate);
        double phase=0, increment=Frequency(midi)/Rate, filtered=0;
        for(int i=0;i<count && from+i<data.Length/2;i++)
        {
            phase+=increment; phase-=Math.Floor(phase);
            double wave=shape==0?1-4*Math.Abs(phase-.5):shape==1?(phase<.3?1:-.43):Math.Sin(phase*Math.PI*2);
            filtered+=(wave-filtered)*(shape==1?.23:.62);
            float value=(float)(filtered*Envelope(i/(double)Rate,length,decay)*gain);
            data[(from+i)*2]+=value*(float)(1-pan);
            data[(from+i)*2+1]+=value*(float)(1+pan);
        }
    }
    static void Drum(float[] data,double start,int kind,double gain,System.Random random)
    {
        double length=kind==0?.24:kind==1?.16:kind==3?.12:.045;
        int from=(int)(start*Rate), count=(int)(length*Rate); double previous=0;
        for(int i=0;i<count && from+i<data.Length/2;i++)
        {
            double t=i/(double)Rate, noise=random.NextDouble()*2-1;
            double high=noise-previous; previous=noise;
            double sound=kind==0
                ?Math.Sin(2*Math.PI*(48*t+140*(1-Math.Exp(-t*40))/40))*Math.Exp(-t*18)
                :kind==1
                    ?(noise*.72+Math.Sin(2*Math.PI*185*t)*.28)*Math.Exp(-t*27)
                    :high*Math.Exp(-t*(kind==3?24:85))*.45;
            float value=(float)(sound*gain*Math.Min(1,t/.0015)*Math.Min(1,(length-t)/.008));
            data[(from+i)*2]+=value; data[(from+i)*2+1]+=value;
        }
    }
    AudioClip MakeMusic()
    {
        // Original D-minor electro/chiptune score: 16 bars, syncopated bass, A/B lead.
        double beat=60/Bpm, bar=beat*4;
        var data=new float[(int)(Rate*bar*16)*2];
        var random=new System.Random(73019);
        int[] roots={50,46,53,48};
        int[][] melody={
            new[]{74,77,81,-1,79,77,76,72},new[]{74,-1,77,79,77,74,72,-1},
            new[]{77,81,84,81,79,-1,77,76},new[]{72,76,79,-1,81,79,76,72},
            new[]{74,77,81,86,84,81,79,77},new[]{77,-1,74,77,79,77,74,72},
            new[]{81,84,88,84,81,79,77,-1},new[]{79,76,72,76,74,-1,72,73}
        };
        for(int b=0;b<16;b++)
        {
            double time=b*bar; int chord=b%4, root=roots[chord];
            bool minor=chord==0, breakdown=b==7;
            for(int step=0;step<8;step++)
            {
                double at=time+step*beat*.5;
                int bass=root-12+(step==3 || step==7?7:step==5?12:0);
                Voice(data,at,.15,bass,breakdown?.10:.15,1,-.08,9);
                int note=melody[b%8][step];
                Voice(data,at,step==7?.19:.22,note,breakdown?.045:.075,0,-.15,6);
                if(b>=8 && note>=0 && step%2==0)Voice(data,at+beat*.25,.12,note+12,.018,2,.55,12);
                Drum(data,at,step==7?3:2,step%2==0?.065:.1,random);
            }
            if(!breakdown)
            {
                Drum(data,time,0,.42,random); Drum(data,time+beat*2,0,.38,random);
                if(b%2==1)Drum(data,time+beat*2.75,0,.25,random);
                Drum(data,time+beat,1,.23,random); Drum(data,time+beat*3,1,.25,random);
            }
            for(int stab=0;stab<2;stab++)
                foreach(int interval in new[]{0,minor?3:4,7})
                    Voice(data,time+beat*(stab==0?.5:2.5),.22,root+12+interval,.035,1,stab==0?-.36:.36,12);
            if(b%4==3)
                for(int fill=0;fill<3;fill++)Drum(data,time+beat*(3.5+fill*.125),1,.085+fill*.018,random);
        }
        int delay=(int)(beat*.75*Rate);
        for(int frame=delay;frame<data.Length/2;frame++)
        {
            data[frame*2]+=data[(frame-delay)*2+1]*.13f;
            data[frame*2+1]+=data[(frame-delay)*2]*.13f;
        }
        for(int frame=0;frame<data.Length/2;frame++)
        {
            double fade=Math.Min(1,Math.Min(frame,(data.Length/2-1)-frame)/(Rate*.008));
            data[frame*2]=(float)(Math.Tanh(data[frame*2]*1.12)*.82*fade);
            data[frame*2+1]=(float)(Math.Tanh(data[frame*2+1]*1.12)*.82*fade);
        }
        return Clip("VoidRush144Bpm",data);
    }
    AudioClip MakeEffect(GameSound sound)
    {
        double length=sound==GameSound.Click?.055:sound==GameSound.Slash?.17:sound==GameSound.Coin?.18:
            sound==GameSound.Win?1.05:sound==GameSound.Lose?.75:sound==GameSound.Ultimate?.55:.32;
        var data=new float[(int)(Rate*length)*2];
        switch(sound)
        {
            case GameSound.Click: Voice(data,0,length,86,.20,1,0,55); break;
            case GameSound.Focus:
                for(int i=0;i<3;i++)Voice(data,i*.055,.18,new[]{74,77,81}[i],.15,0,(i-1)*.25,15);
                break;
            case GameSound.Coin:
                Voice(data,0,.12,95,.18,0,-.2,24); Voice(data,.055,.12,102,.16,2,.2,22); break;
            case GameSound.Reward:
                for(int i=0;i<4;i++)Voice(data,i*.055,.14,new[]{74,77,81,86}[i],.15,0,(i-1.5)*.18,13);
                break;
            case GameSound.Win:
                for(int i=0;i<6;i++)Voice(data,i*.11,.35,new[]{74,77,81,86,89,93}[i],.12,0,(i%2==0?-.2:.2),9);
                break;
            case GameSound.Lose:
                for(int i=0;i<4;i++)Voice(data,i*.11,.38,new[]{69,65,62,50}[i],.12,0,0,7);
                break;
            case GameSound.Interference:
                Voice(data,0,.24,45,.19,1,-.15,13); Voice(data,.055,.18,57,.09,0,.15,18); break;
            case GameSound.Slash:
            case GameSound.Ultimate:
                var random=new System.Random(sound==GameSound.Ultimate?72:39);
                Drum(data,sound==GameSound.Ultimate?.07:.025,0,sound==GameSound.Ultimate?.45:.2,random);
                double previous=0;
                for(int frame=0;frame<data.Length/2;frame++)
                {
                    double t=frame/(double)Rate, noise=random.NextDouble()*2-1;
                    double high=noise-previous; previous=noise;
                    double envelope=Math.Sin(Math.Min(1,t/(length*.55))*Math.PI*.5)*Math.Exp(-t*(sound==GameSound.Ultimate?9:24));
                    float whoosh=(float)(high*envelope*.17);
                    data[frame*2]+=whoosh; data[frame*2+1]+=whoosh*.86f;
                }
                Voice(data,.035,length*.7,sound==GameSound.Ultimate?74:86,.11,2,.1,18);
                break;
        }
        for(int i=0;i<data.Length;i++)data[i]=(float)Math.Tanh(data[i]);
        return Clip(sound.ToString(),data);
    }
    static AudioClip Clip(string name,float[] data)
    {
        var clip=AudioClip.Create(name,data.Length/2,2,Rate,false); clip.SetData(data,0); return clip;
    }
    public void Play(GameSound sound)
    {
        if(Muted || effects==null)return;
        effects.pitch=sound==GameSound.Slash?new[]{.97f,1f,1.035f}[variation++%3]:1;
        effects.PlayOneShot(clips[(int)sound]);
    }
    public void ToggleMute() { Muted=!Muted; PlayerPrefs.SetInt("Muted",Muted?1:0); PlayerPrefs.Save(); Apply(); }
    void Apply() { music.mute=Muted; effects.mute=Muted; }
    void OnDestroy()
    {
        if(music!=null && music.clip!=null)Destroy(music.clip);
        if(clips!=null)foreach(var clip in clips)if(clip!=null)Destroy(clip);
    }
}
