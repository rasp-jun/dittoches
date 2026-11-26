using UnityEngine;

public sealed partial class TacticalArena
{
    // Uses the existing bounded effect pool. No particles or materials are created per frame.
    public void DrawPromotion(object key,int star,float remaining,float height=1)
    {
        Actor actor;
        if(star<2||remaining<=0||remaining>1||!actors.TryGetValue(key,out actor))return;
        float age=1-remaining,fade=Mathf.SmoothStep(0,1,remaining*1.8f);
        Color tint=StarUpgradeUI.Tint(star);
        Vector3 point=actor.root.transform.localPosition+Vector3.up*.08f;
        Halo(point,.4f+age*.75f,tint*fade);
        if(star>=3)Halo(point+Vector3.up*.12f,.25f+age*1.1f,tint*fade);
        int count=star>=3?9:6;
        for(int i=0;i<count;i++)
        {
            float angle=i*Mathf.PI*2/count+age*.9f;
            Vector3 ray=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            Vector3 p=point+ray*(.32f+age*.45f)+Vector3.up*(.12f+age*(star>=3?1.65f:1.05f));
            Stroke(p,p+Vector3.up*(.16f*fade),.032f*fade,tint);
        }
        // A row of two or three five-point stars makes the achieved rank readable.
        Vector3 center=point+Vector3.up*(height+.35f)-camera.transform.forward*.15f;
        for(int i=0;i<star;i++)
        {
            Vector3 starCenter=center+camera.transform.right*((i-(star-1)*.5f)*.32f);
            for(int edge=0;edge<10;edge++)
            {
                float a=Mathf.PI*.5f+edge*Mathf.PI/5,b=a+Mathf.PI/5;
                float ra=(edge%2==0?.14f:.062f)*fade,rb=(edge%2==0?.062f:.14f)*fade;
                Vector3 start=starCenter+camera.transform.right*(Mathf.Cos(a)*ra)+camera.transform.up*(Mathf.Sin(a)*ra);
                Vector3 end=starCenter+camera.transform.right*(Mathf.Cos(b)*rb)+camera.transform.up*(Mathf.Sin(b)*rb);
                Stroke(start,end,.024f*fade,tint);
            }
        }
    }
}
