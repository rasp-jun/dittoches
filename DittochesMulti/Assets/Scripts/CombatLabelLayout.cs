using System.Collections.Generic;
using UnityEngine;

/// <summary>Packs unit status bars near their anchors, inside the field, without overlapping each other.</summary>
public sealed class CombatLabelLayout
{
    public sealed class Entry
    {
        public object key;public Vector2 anchor;public float width;public bool priority,caption;public Rect rect;public string text;
    }
    readonly List<Rect> occupied=new List<Rect>();
    public void Arrange(List<Entry> entries,Rect bounds)
    {
        occupied.Clear();
        // Stable combat identity preserves priority when units pass each other.
        for(int priority=1;priority>=0;priority--)foreach(var entry in entries)
        {
            if(entry.priority!=(priority==1))continue;
            float height=entry.caption?39:19,best=float.MaxValue;Rect chosen=new Rect();
            for(int column=0;column<7;column++)for(int row=0;row<17;row++)
            {
                float dx=column==0?0:((column+1)/2)*(entry.width+7)*(column%2==1?-1:1);
                float dy=row==0?0:((row+1)/2)*23*(row%2==1?-1:1);
                Rect candidate=new Rect(Mathf.Clamp(entry.anchor.x-entry.width*.5f+dx,bounds.x,bounds.xMax-entry.width),
                    Mathf.Clamp(entry.anchor.y-(entry.caption?20:0)+dy,bounds.y,bounds.yMax-height),entry.width,height);
                float overlap=0;
                foreach(var other in occupied)
                {
                    float w=Mathf.Min(candidate.xMax+3,other.xMax+3)-Mathf.Max(candidate.x-3,other.x-3);
                    float h=Mathf.Min(candidate.yMax+2,other.yMax+2)-Mathf.Max(candidate.y-2,other.y-2);
                    if(w>0&&h>0)overlap+=w*h;
                }
                float distance=(candidate.center-new Vector2(entry.anchor.x,entry.anchor.y+(entry.caption?-1:9))).sqrMagnitude;
                float score=overlap*100000+distance;
                if(score<best){best=score;chosen=candidate;}
            }
            entry.rect=chosen;occupied.Add(chosen);
        }
    }
    static GUIStyle captionStyle;
    public static void Draw(Entry entry,float hp,float maxHp,float mana,float maxMana,float shield,bool enemy,float trail=-1)
    {
        Rect r=entry.rect;float y=r.y+(entry.caption?20:0),health=Mathf.Clamp01(hp/Mathf.Max(1,maxHp));
        Color team=enemy?new Color(.96f,.34f,.30f):new Color(.36f,.9f,.62f);
        if(Vector2.Distance(new Vector2(r.center.x,y),entry.anchor)>12)
        {
            Color tether=new Color(team.r,team.g,team.b,.24f);
            ArenaInterface.Fill(new Rect(entry.anchor.x,Mathf.Min(y+16,entry.anchor.y),1,Mathf.Abs(entry.anchor.y-y-16)),tether);
            ArenaInterface.Fill(new Rect(Mathf.Min(entry.anchor.x,r.center.x),y+16,Mathf.Abs(entry.anchor.x-r.center.x),1),tether);
        }
        if(entry.caption)
        {
            if(captionStyle==null){captionStyle=new GUIStyle(GUI.skin.label){fontSize=12,alignment=TextAnchor.MiddleCenter};captionStyle.normal.textColor=new Color(.9f,.96f,.96f);}
            ArenaInterface.Fill(new Rect(r.x,r.y,r.width,19),new Color(.018f,.035f,.045f,.94f));
            GUI.Label(new Rect(r.x+3,r.y,r.width-6,19),entry.text,captionStyle);
        }
        ArenaInterface.Fill(new Rect(r.x,y,r.width,18),entry.priority?new Color(.49f,.64f,.57f):new Color(.008f,.018f,.025f,.96f));
        ArenaInterface.Fill(new Rect(r.x+2,y+2,r.width-4,14),new Color(.07f,.11f,.13f));
        float w=r.width-4;
        if(trail>health)ArenaInterface.Fill(new Rect(r.x+2,y+2,w*Mathf.Clamp01(trail),7),new Color(.91f,.68f,.35f));
        ArenaInterface.Fill(new Rect(r.x+2,y+2,w*health,7),team);
        int segments=Mathf.Clamp(Mathf.CeilToInt(maxHp/250f),1,12);
        for(int i=1;i<segments;i++)ArenaInterface.Fill(new Rect(r.x+2+w*i/segments,y+2,1,7),new Color(.015f,.025f,.03f,.7f));
        if(shield>0)ArenaInterface.Fill(new Rect(r.x+2,y,w*Mathf.Clamp01(shield/Mathf.Max(1,maxHp*.5f)),2),new Color(.70f,.90f,1));
        ArenaInterface.Fill(new Rect(r.x+2,y+12,w*Mathf.Clamp01(mana/Mathf.Max(1,maxMana)),3),new Color(.32f,.65f,.97f));
    }
}
