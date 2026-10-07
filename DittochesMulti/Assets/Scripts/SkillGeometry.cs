using System;

/// <summary>Gameplay coordinates; independent of camera, frame rate and rendering.</summary>
public static class SkillGeometry
{
    public static bool Contains(string shape,float sx,float sy,float tx,float ty,float x,float y,float radius,float reach)
    {
        float dx=x-sx,dy=y-sy;
        if(shape=="radial")return dx*dx+dy*dy<=radius*radius;
        if(shape=="splash"){dx=x-tx;dy=y-ty;return dx*dx+dy*dy<=radius*radius;}
        float ax=tx-sx,ay=ty-sy,length=(float)Math.Sqrt(ax*ax+ay*ay);
        if(length<.0001f){ax=0;ay=1;length=1;}
        ax/=length;ay/=length;
        float forward=dx*ax+dy*ay,side=Math.Abs(dx*ay-dy*ax);
        if(forward<-.15f||forward>reach)return false;
        return side<=radius*(shape=="cone"?Math.Max(.2f,forward/reach):1);
    }
    public static int DueHits(float age,float windup,float travel,float interval,int shots)
    {
        if(age+0.00001f<windup+travel)return 0;
        if(interval<=0)return shots;
        return Math.Min(shots,1+(int)Math.Floor((age-windup-travel+.00001f)/interval));
    }
}
