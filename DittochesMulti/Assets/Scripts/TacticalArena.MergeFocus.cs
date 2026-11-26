using UnityEngine;

public sealed partial class TacticalArena
{
    public void DrawMergeFocus(Vector3 point,int star,bool recipient,float time)
    {
        if(star<2||star>3)return;
        Color tint=recipient?StarUpgradeUI.Tint(star):MergeFocusUI.MaterialTint;
        point.y+=.07f;float pulse=.5f+.5f*Mathf.Sin(time*3.5f);
        Halo(point,recipient?.48f+pulse*.05f:.37f,tint);
        if(recipient)Halo(point+Vector3.up*.06f,.61f+pulse*.04f,tint*.7f);
        for(int i=0;i<4;i++)
        {
            float angle=i*Mathf.PI*.5f+(recipient?time*.35f:0);Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            Stroke(point+direction*.75f,point+direction*.53f,.037f,tint);
        }
    }
}
