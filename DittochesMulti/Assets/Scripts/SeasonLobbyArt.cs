using System.IO;
using UnityEngine;

public static class SeasonLobbyArt
{
    static Texture2D scene;
    static GUIStyle inputField;
    static readonly Texture2D[] gradients=new Texture2D[4];
    static bool attempted;
    public static readonly Color Mint=new Color(.47f,.94f,.88f),Cream=new Color(.93f,.97f,1f);
    public static Texture2D Scene
    {
        get
        {
            if(attempted)return scene;attempted=true;
            string path=Path.Combine(Application.streamingAssetsPath,"Lobby/file-island-observatory-v2.png");
            if(!File.Exists(path))return null;
            scene=new Texture2D(2,2,TextureFormat.RGBA32,false){name="File Island observatory lobby",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            if(!ImageConversion.LoadImage(scene,File.ReadAllBytes(path),true)){Object.Destroy(scene);scene=null;}
            return scene;
        }
    }
    public static void Background(Rect r,Texture2D fallback=null,float darken=0)
    {
        var art=Scene??fallback;if(art!=null)GUI.DrawTexture(r,art,ScaleMode.ScaleAndCrop);
        ArenaInterface.Fill(r,new Color(.012f,.026f,.048f,darken));
    }
    public static void Shade(Rect r,Color color,bool vertical=false,bool reverse=false)
    {
        int index=(vertical?2:0)+(reverse?1:0);
        if(gradients[index]==null)
        {
            var texture=new Texture2D(vertical?1:256,vertical?256:1,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            for(int i=0;i<256;i++)
            {
                float t=i/255f;float alpha=vertical?(reverse?1-t:t):(reverse?t:1-t);
                texture.SetPixel(vertical?0:i,vertical?i:0,new Color(1,1,1,alpha*alpha));
            }
            texture.Apply();gradients[index]=texture;
        }
        Color old=GUI.color;GUI.color=old*color;GUI.DrawTexture(r,gradients[index]);GUI.color=old;
    }
    public static void Panel(Rect r,Color color)
    {
        BattleHudTheme.Round(r,color);
    }
    public static string Input(Rect r,string control,string value,int limit,GUIStyle template,string placeholder="")
    {
        if(inputField==null)
        {
            inputField=new GUIStyle(template){border=new RectOffset(0,0,0,0),padding=new RectOffset(14,14,8,8)};
            inputField.normal.background=null;inputField.hover.background=null;inputField.active.background=null;inputField.focused.background=null;
            inputField.onNormal.background=null;inputField.onHover.background=null;inputField.onActive.background=null;inputField.onFocused.background=null;
            inputField.normal.textColor=Cream;inputField.hover.textColor=Cream;inputField.active.textColor=Cream;inputField.focused.textColor=Cream;
        }
        bool focused=GUI.enabled&&GUI.GetNameOfFocusedControl()==control;
        Panel(r,focused?Mint:GUI.enabled?new Color(.23f,.36f,.43f):new Color(.13f,.21f,.27f));
        float inset=focused?2:1;
        Panel(new Rect(r.x+inset,r.y+inset,r.width-inset*2,r.height-inset*2),new Color(.017f,.039f,.064f));
        GUI.SetNextControlName(control);
        string result=GUI.TextField(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),value,limit,inputField);
        if(result.Length==0&&!focused&&placeholder.Length>0)BattleHudTheme.Label(new Rect(r.x+16,r.y+5,r.width-32,r.height-10),placeholder,13,BattleHudTheme.Muted);
        return result;
    }
    public static bool Play(Rect r,string label,bool available,bool queued)
    {
        bool enabled=GUI.enabled;GUI.enabled=enabled&&available;bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
        Panel(new Rect(r.x,r.y+4,r.width,r.height),new Color(0,0,0,.24f));
        Color edge=queued?new Color(.69f,.40f,.45f):Mint;
        Panel(r,available?new Color(edge.r,edge.g,edge.b,hover?1:.8f):new Color(.14f,.23f,.29f));
        Panel(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),available?(queued?new Color(.23f,.11f,.16f):hover?new Color(.12f,.46f,.47f):new Color(.07f,.31f,.35f)):new Color(.055f,.10f,.14f));
        BattleHudTheme.Label(new Rect(r.x+9,r.y,r.width-18,r.height),label,r.width<240?18:22,available?Cream:BattleHudTheme.Muted,true,TextAnchor.MiddleCenter);
        bool clicked=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.enabled=enabled;return clicked;
    }
    public static bool Tab(Rect r,string label,bool selected,bool available=true,int size=16)
    {
        bool previous=GUI.enabled;GUI.enabled=previous&&available;
        bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
        Panel(r,selected?new Color(.12f,.40f,.42f):hover?new Color(.16f,.27f,.33f):new Color(.13f,.21f,.28f));
        Panel(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),selected?new Color(.05f,.18f,.21f):hover?new Color(.06f,.13f,.18f):new Color(.036f,.074f,.11f));
        BattleHudTheme.Label(r,label,size,GUI.enabled?(selected?Cream:hover?Mint:BattleHudTheme.Text):BattleHudTheme.Muted,true,TextAnchor.MiddleCenter);
        bool clicked=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.enabled=previous;return clicked;
    }
    public static void Motes(float time)
    {
        for(int i=0;i<19;i++)
        {
            float x=550+Mathf.Repeat(i*113.7f+Mathf.Sin(time*.12f+i)*16,930),y=200+Mathf.Repeat(i*57.3f-time*(3+i%3),440);
            float size=2+i%3;float alpha=.16f+.3f*(.5f+.5f*Mathf.Sin(time*.8f+i));
            ArenaInterface.Fill(new Rect(x,y,size,size),new Color(.58f,1,.86f,alpha));
        }
    }
}
