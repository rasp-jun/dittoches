using UnityEngine;

public static class EquipmentTargetFeedback
{
    public static void Draw(Rect r,bool allowed)
    {
        if(r.width<=0||r.height<=0)return;
        r=new Rect(r.x-5,r.y-5,r.width+10,r.height+10);
        Color previous=GUI.color;GUI.color=allowed?new Color(.35f,1f,.72f,.95f):new Color(1f,.38f,.3f,.95f);
        const float length=13,thickness=2;
        foreach(float x in new[]{r.x,r.xMax-length})foreach(float y in new[]{r.y,r.yMax-thickness})
            GUI.DrawTexture(new Rect(x,y,length,thickness),Texture2D.whiteTexture);
        foreach(float x in new[]{r.x,r.xMax-thickness})foreach(float y in new[]{r.y,r.yMax-length})
            GUI.DrawTexture(new Rect(x,y,thickness,length),Texture2D.whiteTexture);
        GUI.color=previous;
    }
}
