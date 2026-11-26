using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared visual language for the lobby, solo HUD and online league.</summary>
public static class BattleHudTheme
{
    public static readonly Color Background=new Color(.027f,.043f,.070f),Panel=new Color(.043f,.070f,.105f),
        Raised=new Color(.070f,.109f,.153f),Line=new Color(.15f,.224f,.286f),Text=new Color(.933f,.949f,.929f),
        Muted=new Color(.62f,.72f,.78f),Gold=new Color(.941f,.761f,.482f),Teal=new Color(.408f,.882f,.839f),Danger=new Color(.98f,.43f,.48f);
    static readonly Dictionary<int,GUIStyle> styles=new Dictionary<int,GUIStyle>();
    static Texture2D sheen;
    static GUIStyle rounded;
    static Texture2D contactShadow;
    public static void GroundShadow(Rect r,float opacity=.38f)
    {
        if(Event.current.type!=EventType.Repaint)return;
        if(contactShadow==null)
        {
            contactShadow=new Texture2D(64,64,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {float dx=(x-31.5f)/31.5f,dy=(y-31.5f)/31.5f;float a=Mathf.Clamp01(1-dx*dx-dy*dy);contactShadow.SetPixel(x,y,new Color(.015f,.025f,.04f,a*a));}
            contactShadow.Apply(false,true);
        }
        Color old=GUI.color;GUI.color=new Color(1,1,1,opacity);GUI.DrawTexture(r,contactShadow);GUI.color=old;
    }
    /// <summary>One shared nine-slice mask; animated tints never allocate textures.</summary>
    public static void Round(Rect r,Color color)
    {
        if(Event.current.type!=EventType.Repaint||r.width<=0||r.height<=0)return;
        if(r.width<20||r.height<20){ArenaInterface.Fill(r,color);return;}
        if(rounded==null)
        {
            var tex=new Texture2D(24,24,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            for(int y=0;y<24;y++)for(int x=0;x<24;x++)
            {float dx=Mathf.Max(8.5f-x,x-14.5f),dy=Mathf.Max(8.5f-y,y-14.5f);float distance=Mathf.Sqrt(Mathf.Max(0,dx)*Mathf.Max(0,dx)+Mathf.Max(0,dy)*Mathf.Max(0,dy));tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(8.5f-distance)));}
            tex.Apply(false,true);rounded=new GUIStyle(GUIStyle.none){border=new RectOffset(10,10,10,10)};rounded.normal.background=tex;
        }
        Color old=GUI.color;GUI.color=color;rounded.Draw(r,GUIContent.none,false,false,false,false);GUI.color=old;
    }
    public static void Section(Rect r,string kicker,string title,string detail="")
    {
        Label(new Rect(r.x,r.y,r.width,20),kicker,11,Teal,true);
        Label(new Rect(r.x,r.y+24,r.width,40),title,28,Text,true);
        if(!string.IsNullOrEmpty(detail))Label(new Rect(r.x,r.y+70,r.width,24),detail,13,Muted);
    }
    public static void Sheen(Rect r,float opacity=.07f)
    {
        if(sheen==null)
        {
            sheen=new Texture2D(1,32,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<32;y++)sheen.SetPixel(0,y,new Color(1,1,1,y/31f));sheen.Apply(false,true);
        }
        Color old=GUI.color;GUI.color=new Color(1,1,1,opacity);GUI.DrawTexture(r,sheen);GUI.color=old;
    }
    public static GUIStyle Style(int size=14,bool bold=false,TextAnchor anchor=TextAnchor.MiddleLeft)
    {
        int key=size*100+(bold?50:0)+(int)anchor;GUIStyle value;
        if(!styles.TryGetValue(key,out value))
        {
            value=new GUIStyle(GUI.skin.label){fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,
                alignment=anchor,clipping=TextClipping.Clip,wordWrap=false,padding=new RectOffset(0,0,0,0)};
            value.normal.textColor=Text;styles.Add(key,value);
        }
        return value;
    }
    public static void Label(Rect r,string text,int size=14,Color? color=null,bool bold=false,TextAnchor anchor=TextAnchor.MiddleLeft)
    {
        var style=Style(size,bold,anchor);Color saved=style.normal.textColor;style.normal.textColor=color??Text;
        GUI.Label(r,text,style);style.normal.textColor=saved;
    }
    public static void FitLabel(Rect r,string text,int size=17,Color? color=null,bool bold=true)
    {
        while(size>11&&Style(size,bold).CalcSize(new GUIContent(text)).x>r.width)size--;
        Label(r,text,size,color,bold);
    }
    public static void Surface(Rect r,bool raised=false,Color? accent=null)
    {
        Round(new Rect(r.x,r.y+4,r.width,r.height),new Color(0,0,0,.19f));
        Round(r,accent.HasValue?Color.Lerp(Line,accent.Value,.45f):Line);
        Round(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),raised?Raised:Panel);
        if(accent.HasValue)Round(new Rect(r.x+12,r.y+1,Mathf.Min(38,r.width-24),3),accent.Value);
    }
    public static void Bar(Rect r,float value,Color color)
    {
        ArenaInterface.Fill(r,Raised);ArenaInterface.Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(value),r.height),color);
        if(r.height>=4)ArenaInterface.Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(value),1),Color.Lerp(color,Color.white,.25f));
    }
    public static void Badge(Rect r,string text,Color color,int size=12)
    {
        Round(r,Color.Lerp(Panel,color,.16f));
        Label(r,text,size,color,true,TextAnchor.MiddleCenter);
    }
    public static bool TraitRow(Rect r,DigimonTraitHudSnapshot.Row row,bool compact=false)
    {
        bool active=row.tier>0;Color tint=DigimonTraitUI.ColorFor(row.trait.id);
        bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
        ArenaInterface.Fill(r,Color.Lerp(Panel,tint,hover?.20f:active?.10f:.025f));
        ArenaInterface.Fill(new Rect(r.x,r.y,2,r.height),active?tint:Line);
        float icon=compact?24:29;GUI.DrawTexture(new Rect(r.x+7,r.y+5,icon,icon),DigimonTraitUI.Icon(row.trait.id));
        float x=r.x+icon+15;
        Label(new Rect(x,r.y+2,r.width-icon-72,21),row.trait.name,compact?13:14,active?Text:Muted,true);
        Label(new Rect(r.xMax-49,r.y+2,42,21),row.progress,13,active?tint:Muted,true,TextAnchor.MiddleRight);
        float width=r.width-(x-r.x)-12;
        for(int i=0;i<row.trait.tiers.Length;i++)
        {
            float w=width/row.trait.tiers.Length-3;
            ArenaInterface.Fill(new Rect(x+i*(w+3),r.yMax-7,w,3),i<row.tier?tint:Line);
        }
        string tip=row.trait.name+" · "+(active?row.tier+"단계 활성":"미활성")+"\n"+DigimonTraitUI.Next(row.trait,row.count)+"\n클릭: 기여 유닛 · 대기석 후보 · 효과";
        return GUI.Button(r,new GUIContent("",tip),GUIStyle.none);
    }
    public static void ShopArtwork(Rect r,string id,string name,int cost,string status,int singles,int doubles,bool merge,float hover,bool compact=false,DigimonTraitHudSnapshot.Row breakpoint=null)
    {
        Color rarity=ArenaInterface.Rarity(cost),edge=merge?StarUpgradeUI.Tint(doubles>=2?3:2):rarity;
        Round(r,Color.Lerp(Line,edge,merge?.85f:.45f+hover*.55f));
        Round(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),Panel);
        float footer=compact?35:48,artHeight=r.height-footer;
        Rect art=new Rect(r.x+3,r.y+3,r.width-6,artHeight-3);
        CharacterCardArt.Stage(art,cost);
        FaithfulPortraits.Draw(new Rect(r.x+6,r.y+2,r.width*(compact?.64f:.70f),artHeight-2),id,true);
        // A quiet, separate trait rail keeps the silhouette and purchase information legible.
        float rail=r.x+r.width*.66f;
        ArenaInterface.Fill(new Rect(rail,r.y+3,r.xMax-rail-3,artHeight-3),new Color(.025f,.044f,.068f,.88f));
        Badge(new Rect(r.xMax-(compact?40:52),r.y+7,compact?33:43,compact?22:27),cost+" G",Gold,compact?13:17);
        var traits=DigimonBuildCatalog.ForUnit(id);float y=r.y+(compact?31:41);
        foreach(var t in traits)
        {
            float size=compact?14:17;
            GUI.DrawTexture(new Rect(rail+5,y,size,size),DigimonTraitUI.Icon(t.id));
            FitLabel(new Rect(rail+size+9,y,r.xMax-rail-size-15,size),t.name,compact?10:12,Text,false);
            y+=compact?17:23;
        }
        if(breakpoint!=null)
        {
            Rect hint=new Rect(r.x+3,r.y+artHeight-(compact?17:23),rail-r.x-3,compact?17:23);
            ArenaInterface.Fill(hint,new Color(.015f,.055f,.07f,.95f));
            FitLabel(new Rect(hint.x+4,hint.y,hint.width-8,hint.height),"배치 → "+breakpoint.trait.name+" "+breakpoint.target,compact?10:12,Teal);
            GUI.Label(hint,new GUIContent("","다른 기여종을 빼지 않고 전장에 배치하면 "+breakpoint.trait.name+" "+(breakpoint.tier+1)+"단계\n모집만으로 전장 시너지는 바뀌지 않습니다"),GUIStyle.none);
        }
        ArenaInterface.Fill(new Rect(r.x+3,r.yMax-footer,r.width-6,1),Color.Lerp(Panel,edge,.5f));
        FitLabel(new Rect(r.x+10,r.yMax-footer+2,r.width-20,compact?19:26),name,compact?14:20);
        FitLabel(new Rect(r.x+10,r.yMax-(compact?17:22),r.width-20,18),status,compact?10:12,merge?edge:Muted,false);
        int target=StarUpgradeUI.Target(doubles),owned=StarUpgradeUI.Copies(singles,doubles);
        if(owned>0)
        {
            Badge(new Rect(r.x+8,r.y+(compact?29:35),compact?51:61,compact?19:22),owned+" / "+target,edge,compact?11:13);
            float step=(r.width-16)/target;
            for(int i=0;i<target;i++)ArenaInterface.Fill(new Rect(r.x+8+i*step,r.yMax-footer-3,step-3,3),i<owned?edge:Line);
        }
    }
    public static void StatTile(Rect r,string name,string value,Color? tint=null)
    {
        Round(r,Raised);
        Label(new Rect(r.x+10,r.y+5,r.width-20,20),name,11,Muted);
        FitLabel(new Rect(r.x+10,r.y+28,r.width-20,r.height-32),value,20,tint??Text);
    }
}
