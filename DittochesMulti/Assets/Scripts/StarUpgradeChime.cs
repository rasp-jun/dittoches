using System;

// Original, short additive chimes; generated once per rank and cached by the player.
public static class StarUpgradeChime
{
    public const int SampleRate=48000;
    public static float[] Samples(int star)
    {
        if(star!=2&&star!=3)return new float[0];
        double duration=star==3?1.28:.78;var samples=new float[(int)(duration*SampleRate)];
        double[] notes=star==3?new[]{523.251,659.255,783.991,1046.502}:new[]{659.255,783.991,1046.502};
        double step=star==3?.105:.075,peak=0;
        for(int i=0;i<samples.Length;i++)
        {
            double t=(double)i/SampleRate,value=0;
            for(int n=0;n<notes.Length;n++)
            {
                double age=t-n*step;if(age<0)continue;
                double envelope=Math.Min(1,age/.012)*Math.Exp(-age*(star==3?5.3:8.5));
                double phase=2*Math.PI*notes[n]*age;
                value+=envelope*(Math.Sin(phase)+.22*Math.Sin(phase*2)+.07*Math.Sin(phase*3))* .18;
            }
            if(star==3&&t>.30)
            {
                double age=t-.30,envelope=Math.Min(1,age/.035)*Math.Exp(-age*5);
                value+=envelope*(Math.Sin(2*Math.PI*261.626*age)+.45*Math.Sin(2*Math.PI*391.995*age))*.11;
            }
            value*=Math.Min(1,t/.007)*Math.Min(1,(duration-t)/.085);
            samples[i]=(float)value;peak=Math.Max(peak,Math.Abs(value));
        }
        if(peak>0)for(int i=0;i<samples.Length;i++)samples[i]*=(float)(.62/peak);
        samples[0]=samples[samples.Length-1]=0;return samples;
    }
}

// Both steps of a chain merge occur in one transaction. Emit only the final rank.
public sealed class StarUpgradeSoundQueue
{
    public int Star {get;private set;}
    public float Due {get;private set;}
    public bool Add(int star,float now)
    {
        if(star!=2&&star!=3)return false;
        if(Star==0)Due=now+.04f;
        Star=Math.Max(Star,star);return true;
    }
    public int Take(float now)
    {if(Star==0||now<Due)return 0;int star=Star;Clear();return star;}
    public void Clear(){Star=0;Due=0;}
}
