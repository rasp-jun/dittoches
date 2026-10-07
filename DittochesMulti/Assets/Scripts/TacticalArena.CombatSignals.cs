using UnityEngine;

public sealed partial class TacticalArena
{
    public void DrawCombatSignal(Vector3 point,CombatSignals.Signal signal,float height=1)
    {
        if(signal.kind==0||signal.age<0||signal.age>=CombatSignals.Duration(signal.kind))return;
        float t=signal.age/CombatSignals.Duration(signal.kind),fade=1-t;
        Color color=signal.Color;Vector3 p=point+Vector3.up*.08f;
        Halo(p,.3f+t*.65f,color*fade);
        if(signal.kind==1)
        {
            Vector3 center=p+Vector3.up*(height*.7f+t*.15f)+camera.transform.right*.62f-camera.transform.forward*.25f;
            Vector3 right=camera.transform.right*.32f*fade,up=camera.transform.up*.42f*fade;
            Vector3 top=center+up,left=center-right+up*.3f,bottom=center-up,rightPoint=center+right+up*.3f;
            Stroke(top,left,.04f*fade,color);Stroke(left,bottom,.04f*fade,color);
            Stroke(bottom,rightPoint,.04f*fade,color);Stroke(rightPoint,top,.04f*fade,color);
            Stroke(center-up*.3f,center+up*.3f,.055f*fade,color);
        }
        else
        {
            Halo(p+Vector3.up*(.18f+t*.35f),.35f+t*.3f,color*fade);
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5+t*.8f;
                Vector3 spark=p+new Vector3(Mathf.Cos(a)*.45f,.2f+t*.75f,Mathf.Sin(a)*.45f);
                Stroke(spark,spark+Vector3.up*.15f*fade,.045f*fade,color);
            }
        }
    }
}
