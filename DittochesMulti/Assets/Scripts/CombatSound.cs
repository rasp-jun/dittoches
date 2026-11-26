using UnityEngine;

// Original procedural PCM. Presentation only; no simulation state or timing is changed.
public static class CombatSound
{
    public const int SampleRate=48000;
    public enum Kind { Impact,Slash,Bite,Whip,Fire,Electric,Ice,Water,Air,Seed,Metal,Holy,Shadow,Music }
    public static Kind Basic(string id)
    {
        switch(BasicAttackPresentation.For(id))
        {
            case BasicAttackPresentation.Kind.Claw:case BasicAttackPresentation.Kind.Sword:return Kind.Slash;
            case BasicAttackPresentation.Kind.Bite:return Kind.Bite;
            case BasicAttackPresentation.Kind.Whip:return Kind.Whip;
            case BasicAttackPresentation.Kind.Fire:case BasicAttackPresentation.Kind.Feather:return Kind.Fire;
            case BasicAttackPresentation.Kind.Electric:return Kind.Electric;
            case BasicAttackPresentation.Kind.Ice:return Kind.Ice;
            case BasicAttackPresentation.Kind.Bubble:case BasicAttackPresentation.Kind.Water:return Kind.Water;
            case BasicAttackPresentation.Kind.Air:return Kind.Air;
            case BasicAttackPresentation.Kind.Seed:case BasicAttackPresentation.Kind.Pollen:case BasicAttackPresentation.Kind.Flower:return Kind.Seed;
            case BasicAttackPresentation.Kind.Missile:return Kind.Metal;
            case BasicAttackPresentation.Kind.Holy:return Kind.Holy;
            case BasicAttackPresentation.Kind.Music:return Kind.Music;
            default:return Kind.Impact;
        }
    }
    public static Kind Skill(string visual)
    {
        switch(visual)
        {
            case "claw":case "claws":case "sword":case "swords":case "cross":return Kind.Slash;
            case "vine":case "whip":return Kind.Whip;
            case "missile":case "missiles":return Kind.Metal;
            case "bubble":case "water":return Kind.Water;
            case "air":return Kind.Air;
            case "music":return Kind.Music;
        }
        switch(CombatEffectStyle.Skill(visual))
        {
            case CombatEffectStyle.Element.Flame:return Kind.Fire;
            case CombatEffectStyle.Element.Storm:return Kind.Electric;
            case CombatEffectStyle.Element.Frost:return Kind.Ice;
            case CombatEffectStyle.Element.Nature:return Kind.Seed;
            case CombatEffectStyle.Element.Light:return Kind.Holy;
            case CombatEffectStyle.Element.Shadow:return Kind.Shadow;
            default:return Kind.Impact;
        }
    }
    static float Sin(float phase){return Mathf.Sin(phase*Mathf.PI*2);}
    public static float[] Samples(Kind kind,bool skill)
    {
        if((int)kind<0||(int)kind>=(int)Kind.Music+1)return new float[0];
        float duration=(skill?.34f:.18f)+(kind==Kind.Holy?.14f:kind==Kind.Fire||kind==Kind.Water||kind==Kind.Shadow?.07f:0);
        var data=new float[Mathf.RoundToInt(duration*SampleRate)];uint random=(uint)(731+(int)kind*197+(skill?31:0));
        float low=0,soft=0,phase=0,scale=skill?.78f:1;
        for(int i=0;i<data.Length;i++)
        {
            float t=i/(float)SampleRate,u=t/duration;
            random^=random<<13;random^=random>>17;random^=random<<5;
            float noise=(random&65535)/32767.5f-1;
            low+=(noise-low)*.085f;soft+=(noise-soft)*.31f;
            float thud=Sin(scale*(112*t-160*t*t))*Mathf.Exp(-22*t),v=0;
            switch(kind)
            {
                case Kind.Impact:v=.80f*thud+.42f*low*Mathf.Exp(-14*t);break;
                case Kind.Slash:v=2.0f*soft*Mathf.Sin(Mathf.PI*Mathf.Pow(u,.55f))+.17f*thud;break;
                case Kind.Bite:v=.5f*thud+1.25f*soft*(.3f+.7f*Mathf.Pow(Mathf.Sin(t*92),2))*Mathf.Exp(-10*t);break;
                case Kind.Whip:v=1.2f*soft*Mathf.Exp(-18*t)+.3f*Sin(scale*(980*t-1400*t*t))*Mathf.Exp(-25*t);break;
                case Kind.Fire:v=2.2f*low*(.65f+.35f*Mathf.Sin(t*47))+.35f*thud;break;
                case Kind.Electric:phase+=(340+220*Mathf.Sin(t*92))*scale/SampleRate;v=.32f*Sin(phase)+.18f*Sin(phase*2.97f)+.55f*soft*Mathf.Sin(t*180);break;
                case Kind.Ice:v=.26f*Sin(1400*scale*t)*Mathf.Exp(-13*t)+.20f*Sin(2137*scale*t)*Mathf.Exp(-19*t)+.5f*soft*Mathf.Exp(-9*t);break;
                case Kind.Water:phase+=(420+170*Mathf.Sin(t*35))*scale/SampleRate;v=1.7f*low+.24f*Sin(phase)*Mathf.Exp(-7*t);break;
                case Kind.Air:v=2.0f*low*Mathf.Sin(Mathf.PI*u)+.45f*soft*Mathf.Sin(Mathf.PI*u);break;
                case Kind.Seed:v=.30f*Sin(scale*(490*t-650*t*t))*Mathf.Exp(-18*t)+1.2f*soft*Mathf.Exp(-9*t);break;
                case Kind.Metal:v=.48f*thud+.19f*Sin(720*scale*t)*Mathf.Exp(-17*t)+.15f*Sin(1093*scale*t)*Mathf.Exp(-23*t)+.8f*low;break;
                case Kind.Holy:v=.23f*Sin(784*scale*t)*Mathf.Exp(-5*t)+.15f*Sin(1176*scale*t)*Mathf.Exp(-7*t)+.10f*Sin(1568*scale*t)*Mathf.Exp(-9*t);break;
                case Kind.Shadow:v=.42f*Sin(scale*(190*t-150*t*t))+.28f*Sin(71*scale*t)+.8f*low;break;
                case Kind.Music:v=.24f*Sin(196*scale*t)+.18f*Sin(247*scale*t)+.14f*Sin(294*scale*t);break;
            }
            float envelope=Mathf.Clamp01(t/.004f)*Mathf.Pow(1-u,1.65f)*Mathf.Clamp01((duration-t)/.014f);
            data[i]=.52f*(float)System.Math.Tanh(v*1.25f)*envelope;
        }
        // Remove tiny DC drift without introducing endpoint clicks.
        double sum=0,weight=0;for(int i=0;i<data.Length;i++){sum+=data[i];weight+=Mathf.Sin(Mathf.PI*i/(data.Length-1));}
        for(int i=0;i<data.Length;i++)data[i]-=(float)(sum/weight)*Mathf.Sin(Mathf.PI*i/(data.Length-1));
        data[0]=data[data.Length-1]=0;return data;
    }
}
