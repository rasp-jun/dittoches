using UnityEngine;

public static class LobbyVisuals
{
    static Texture2D rule,feature;
    public static void Rule(Rect r,Color color)
    {
        if(rule==null)
        {
            rule=new Texture2D(64,1,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
            for(int i=0;i<64;i++)rule.SetPixel(i,0,new Color(1,1,1,Mathf.Min(1,Mathf.Sin(i/63f*Mathf.PI)*3)));rule.Apply();
        }
        Color old=GUI.color;GUI.color=old*color;GUI.DrawTexture(r,rule);GUI.color=old;
    }
    public static bool Primary(Rect r,string label,bool available,bool queued)
    {
        return SeasonLobbyArt.Play(r,label,available,queued);
    }
    public static void FeatureCard(Rect r,int theme,bool focus)
    {
        if(feature==null)
        {
            feature=new Texture2D(64,64,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {float u=x/63f,v=y/63f;feature.SetPixel(x,y,new Color(1,1,1,.04f+.11f*Mathf.Exp(-((u-.9f)*(u-.9f)*7+(v-.7f)*(v-.7f)*5))));}feature.Apply();
        }
        BattleHudTheme.Round(new Rect(r.x,r.y+4,r.width,r.height),new Color(0,0,0,.17f));
        BattleHudTheme.Round(r,focus?SeasonLobbyArt.Mint:Edge);
        BattleHudTheme.Round(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),new Color(.029f,.055f,.083f));
        Color old=GUI.color;GUI.color=old*(focus?SeasonLobbyArt.Mint:TamerLoadout.FieldColor(theme));GUI.DrawTexture(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),feature);GUI.color=old;
    }
    static Texture2D shade,diamond,cinematic;
    public static void CinematicShade(Rect r)
    {
        if(cinematic==null)
        {
            cinematic=new Texture2D(1,64,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<64;y++)
            {float t=y/63f;float a=Mathf.Clamp01(Mathf.Pow(Mathf.Abs(t-.53f)*2,3))*.93f;cinematic.SetPixel(0,y,new Color(.015f,.027f,.042f,a));}
            cinematic.Apply();
        }
        GUI.DrawTexture(r,cinematic,ScaleMode.StretchToFill);
    }
    public static readonly Color Gold=new Color(.89f,.75f,.47f),Ink=new Color(.022f,.039f,.063f),Edge=new Color(.14f,.24f,.31f);
    public static void Backdrop(Rect r,Texture2D art,float strength=1)
    {
        if(art!=null){Color old=GUI.color;GUI.color=new Color(1,1,1,strength);GUI.DrawTexture(r,art,ScaleMode.ScaleAndCrop);GUI.color=old;}
        if(shade==null)
        {
            shade=new Texture2D(64,1,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
            for(int x=0;x<64;x++)shade.SetPixel(x,0,new Color(.01f,.025f,.046f,Mathf.Lerp(.97f,.08f,x/63f)));
            shade.Apply();
        }
        GUI.DrawTexture(r,shade,ScaleMode.StretchToFill);
    }
    public static void Frame(Rect r,Color? accent=null)
    {
        ArenaInterface.Fill(r,Edge);ArenaInterface.Fill(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),Ink);
        ArenaInterface.Fill(new Rect(r.x,r.y,Mathf.Min(54,r.width),2),accent??Gold);
        ArenaInterface.Fill(new Rect(r.xMax-22,r.yMax-1,22,1),accent??Gold);
    }
    public static void Crest(Rect r,string trait,Color color)
    {
        // Draw in the existing GUI coordinate space. Rotating the GUI matrix here
        // shifts the pivot and pointer when the entire lobby is scaled to fit.
        if(diamond==null)
        {
            diamond=new Texture2D(65,65,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<65;y++)for(int x=0;x<65;x++)
                diamond.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(32.5f-Mathf.Abs(x-32)-Mathf.Abs(y-32))));
            diamond.Apply();
        }
        Color previous=GUI.color;GUI.color=previous*color;
        GUI.DrawTexture(r,diamond);
        GUI.color=previous*Ink;GUI.DrawTexture(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),diamond);
        GUI.color=previous;
        GUI.DrawTexture(new Rect(r.x+r.width*.22f,r.y+r.height*.22f,r.width*.56f,r.height*.56f),DigimonTraitUI.Icon(trait));
    }
    public static void Eyebrow(Rect r,string label){BattleHudTheme.Label(r,label,12,SeasonLobbyArt.Mint,true);}
}
