using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared portrait stage and rarity treatment for recruitment, codex and inspection.</summary>
public static class CharacterCardArt
{
    static readonly Dictionary<int,Texture2D> backgrounds=new Dictionary<int,Texture2D>();
    static Texture2D Background(int cost)
    {
        cost=Mathf.Clamp(cost,1,5);Texture2D image;if(backgrounds.TryGetValue(cost,out image))return image;
        const int w=192,h=128;image=new Texture2D(w,h,TextureFormat.RGB24,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        Color accent=ArenaInterface.Rarity(cost);var pixels=new Color[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float u=x/(float)w,v=y/(float)h;
            float glow=Mathf.Exp(-((u-.4f)*(u-.4f)*7+(v-.55f)*(v-.55f)*3));
            float slash=Mathf.Abs((u+v*.58f)*8-Mathf.Round((u+v*.58f)*8))<.014f?.035f:0;
            pixels[y*w+x]=Color.Lerp(new Color(.018f,.033f,.052f),accent,.05f+.25f*glow+slash);
        }
        image.SetPixels(pixels);image.Apply(false,true);backgrounds[cost]=image;return image;
    }
    public static void Stage(Rect rect,int cost)
    {GUI.DrawTexture(rect,Background(cost),ScaleMode.StretchToFill);}
    public static void Portrait(Rect rect,string id,int cost,bool bust)
    {
        Stage(rect,cost);FaithfulPortraits.Draw(rect,id,bust);
        ArenaInterface.Fill(new Rect(rect.x,rect.yMax-2,rect.width,2),ArenaInterface.Rarity(cost));
    }
    public static void Border(Rect rect,int cost,bool merge,float hover)
    {
        Color edge=merge?ArenaInterface.Gold:Color.Lerp(ArenaInterface.Rarity(cost)*.5f,ArenaInterface.Rarity(cost),.5f+hover*.5f);
        edge.a=1;float width=merge||hover>.1f?2:1;
        ArenaInterface.Fill(new Rect(rect.x,rect.y,rect.width,3),edge);
        ArenaInterface.Fill(new Rect(rect.x,rect.y,width,rect.height),edge);
        ArenaInterface.Fill(new Rect(rect.xMax-width,rect.y,width,rect.height),edge);
        ArenaInterface.Fill(new Rect(rect.x,rect.yMax-width,rect.width,width),edge);
    }
}
