using System.Collections.Generic;
using UnityEngine;

// Resolution-independent equipment silhouettes, rendered once into cached icons.
// Uses the game's own GUI pipeline; no downloaded art or per-frame texture allocation.
public static class DigimonEquipmentArt
{
    static readonly Dictionary<int,Texture2D> cache=new Dictionary<int,Texture2D>();
    static readonly Color steel=new Color(.64f,.78f,.86f),gold=new Color(1,.72f,.28f);
    public static Color Tint(int id)
    {
        if(id==0||id==4||id==5)return new Color(1,.65f,.27f);
        if(id==1||id==8)return new Color(1,.34f,.31f);
        if(id==2||id==6||id==11)return new Color(.24f,.72f,1);
        if(id==9||id==12||id==7)return new Color(1,.85f,.42f);
        if(id==10)return new Color(.98f,.4f,.7f);
        if(id==14)return new Color(.27f,1,.78f);
        return new Color(.64f,.52f,1);
    }
    public sealed class Painter
    {
        const int size=160;readonly Color[] pixels=new Color[size*size];readonly Color accent;
        public Painter(Color tint)
        {
            accent=tint;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size,light=Mathf.Max(0,1-Vector2.Distance(new Vector2(u,v),new Vector2(.5f,.57f))*1.65f);
                Color c=Color.Lerp(new Color(.015f,.025f,.042f),tint,.025f+light*.19f);
                if((x%24==0||y%24==0)&&x>9&&x<150&&y>9&&y<150)c=Color.Lerp(c,tint,.045f);
                pixels[y*size+x]=c;
            }
        }
        void Put(int x,int y,Color c,float a){pixels[y*size+x]=Color.Lerp(pixels[y*size+x],c,a);}
        public void Poly(Color color,params float[] xy)
        {
            float left=100,right=0,bottom=100,top=0;
            for(int i=0;i<xy.Length;i+=2){left=Mathf.Min(left,xy[i]);right=Mathf.Max(right,xy[i]);bottom=Mathf.Min(bottom,xy[i+1]);top=Mathf.Max(top,xy[i+1]);}
            for(int iy=Mathf.Max(0,Mathf.FloorToInt(bottom*1.6f)-2);iy<Mathf.Min(size,Mathf.CeilToInt(top*1.6f)+2);iy++)
            for(int ix=Mathf.Max(0,Mathf.FloorToInt(left*1.6f)-2);ix<Mathf.Min(size,Mathf.CeilToInt(right*1.6f)+2);ix++)
            {
                float x=(ix+.5f)/1.6f,y=(iy+.5f)/1.6f,d=1000;bool inside=false;
                for(int i=0,j=xy.Length-2;i<xy.Length;j=i,i+=2)
                {
                    float ax=xy[j],ay=xy[j+1],bx=xy[i],by=xy[i+1];
                    if((ay>y)!=(by>y)&&x<(bx-ax)*(y-ay)/(by-ay)+ax)inside=!inside;
                    Vector2 a=new Vector2(ax,ay),b=new Vector2(bx,by),p=new Vector2(x,y);
                    d=Mathf.Min(d,Vector2.Distance(p,a+Vector2.ClampMagnitude(b-a,Vector2.Distance(a,b))*Mathf.Clamp01(Vector2.Dot(p-a,b-a)/Mathf.Max(.0001f,(b-a).sqrMagnitude))));
                }
                float alpha=Mathf.Clamp01(.5f+(inside?d:-d)*1.6f);
                if(alpha<=0)continue;
                Color lit=Color.Lerp(color*.55f,Color.Lerp(color,Color.white,.32f),Mathf.Clamp01((y-bottom)/(top-bottom+1)*.75f+(1-x/100)*.2f));lit.a=1;
                if(d<1.1f)lit=Color.Lerp(lit,Color.Lerp(color,Color.white,.65f),.6f);
                Put(ix,iy,lit,alpha);
            }
        }
        public void Line(Color c,float width,float ax,float ay,float bx,float by)
        {
            Vector2 a=new Vector2(ax,ay),b=new Vector2(bx,by),n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
            Poly(c,ax+n.x,ay+n.y,bx+n.x,by+n.y,bx-n.x,by-n.y,ax-n.x,ay-n.y);
        }
        public void Gem(Color c,float x,float y,float w,float h)
        {Poly(c,x-w,y,x,y+h,x+w,y,x,y-h);Poly(Color.Lerp(c,Color.white,.5f),x-w,y,x,y+h,x,y);}
        public void Ring(Color c,float x,float y,float radius,float thickness)
        {
            for(int i=0;i<32;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;Line(c,thickness,x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius);}
        }
        public Texture2D Finish(int id)
        {
            Line(accent,.7f,7,7,93,7);Line(accent,.7f,7,93,93,93);
            foreach(int x in new[]{7,93}){Line(accent,2,x,7,x,18);Line(accent,2,x,82,x,93);}
            var t=new Texture2D(size,size,TextureFormat.RGBA32,true){name="Digimon equipment "+id,filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};
            t.SetPixels(pixels);t.Apply(true,false);return t;
        }
    }
    public static Texture2D Icon(int id)
    {
        Texture2D found;if(cache.TryGetValue(id,out found))return found;
        Color tint=Tint(id);var p=new Painter(tint);
        if(id<3)
        {
            p.Poly(steel*.45f,19,30,62,22,84,42,75,62,33,70,16,49);
            p.Poly(id==0?steel:tint,19,49,34,70,74,62,62,44);
            p.Poly(id==0?steel*.8f:tint*.7f,19,49,62,44,62,23,20,31);
            p.Poly(id==0?gold:tint,62,44,74,62,84,42,62,23);
            for(int i=0;i<3;i++)p.Line(tint,2,29+i*10,41-i*1.5f,29+i*10,34-i*1.5f);
            p.Gem(Color.white,45,58,6,4);
        }
        else if(id==3)
        {p.Ring(steel,50,50,29,4);p.Ring(tint,50,50,36,1);p.Gem(tint,50,51,19,27);p.Line(Color.white,2,50,32,50,68);}
        else if(id==4)
        {
            p.Poly(gold,22,22,48,20,67,39,58,58,30,47);
            for(int i=0;i<3;i++){float x=29+i*15,y=40+i*3;p.Poly(steel,x,y,x+7,y+6,x+19,85-i*3,x+5,70-i*3);}
            p.Gem(tint,39,32,6,5);
        }
        else if(id==5||id==12)
        {
            p.Line(gold,8,26,22,41,38);p.Poly(steel,34,44,67,83,84,89,77,69,47,34);
            p.Poly(id==12?tint:new Color(1,.38f,.2f),43,40,78,81,68,60);
            p.Line(gold,7,25,49,54,26);p.Gem(tint,39,39,6,6);
            if(id==12){p.Line(tint,3,19,70,28,80);p.Line(tint,3,25,64,39,80);}
        }
        else if(id==6)
        {
            p.Poly(steel*.7f,19,35,42,23,66,40,82,65,65,82,34,60);
            p.Poly(steel,29,51,39,70,61,77,71,65,53,42);
            p.Poly(tint*.5f,57,43,78,54,85,70,65,77,51,60);
            p.Ring(steel,68,60,14,5);p.Ring(tint,68,60,8,4);p.Gem(Color.white,68,60,4,5);
            p.Poly(gold,24,51,25,63,39,56);p.Gem(new Color(1,.25f,.18f),42,62,4,3);
        }
        else if(id==7)
        {p.Line(steel,7,24,20,61,63);p.Poly(steel,46,59,69,89,83,86,72,61,55,47);p.Poly(tint,55,59,76,81,67,62);p.Line(gold,6,43,66,66,45);p.Gem(new Color(1,.25f,.26f),52,55,5,6);}
        else if(id==8||id==9)
        {
            p.Poly(gold,20,72,50,87,80,72,76,42,50,17,24,42);
            p.Poly(id==8?tint:new Color(.74f,.83f,.85f),26,68,50,80,74,68,70,44,50,24,30,44);
            if(id==8){p.Poly(gold,34,63,43,59,50,70,57,59,66,63,58,49,63,37,50,44,37,37,42,49);p.Line(steel,2,50,24,50,79);}
            else{p.Ring(gold,50,53,17,3);p.Line(gold,5,50,30,50,74);p.Line(gold,5,33,56,67,56);p.Gem(new Color(1,.25f,.27f),50,53,6,8);}
        }
        else if(id==10)
        {for(int i=0;i<6;i++){float a=i*Mathf.PI/3;p.Gem(tint,50+Mathf.Cos(a)*18,52+Mathf.Sin(a)*18,12,15);}p.Ring(gold,50,52,12,3);p.Gem(new Color(1,.9f,.65f),50,52,8,10);p.Poly(new Color(.3f,.84f,.6f),50,18,28,28,38,36,50,29,63,36,73,28);}
        else if(id==11)
        {
            foreach(int sign in new[]{-1,1})
            {float x=50+sign*16;p.Poly(steel,x-8,38,x-8,81,x+8,81,x+8,47,x+16,28,x+3,23,x-2,40);p.Line(tint,3,x-3,52,x-3,75);p.Line(gold,3,x-10,50,x+10,50);}
        }
        else if(id==13)
        {p.Ring(steel,50,48,27,10);p.Ring(tint,50,48,22,3);p.Poly(gold,22,70,38,75,50,50,64,78,79,71,56,29,45,29);p.Gem(tint,50,47,7,9);}
        else
        {p.Poly(steel,28,19,22,72,33,83,69,83,78,72,72,19);p.Poly(new Color(.03f,.13f,.14f),32,34,29,70,68,70,66,34);p.Gem(tint,49,54,12,14);p.Line(tint,4,40,24,59,24);p.Line(gold,5,78,35,86,43);p.Line(gold,3,86,43,85,59);}
        found=p.Finish(id);cache.Add(id,found);return found;
    }
    public static void Draw(Rect r,int id,bool selected=false)
    {
        GUI.DrawTexture(r,Icon(id),ScaleMode.ScaleToFit);
        if(selected){ArenaInterface.Fill(new Rect(r.x,r.y,r.width,2),Color.white);ArenaInterface.Fill(new Rect(r.x,r.yMax-2,r.width,2),Tint(id));}
    }
    public static string Tooltip(int id)
    {var item=DigimonBuildCatalog.Data.items[id];return item.name+" · "+item.role+"\n"+item.description+"\n우클릭: 디지털 무장 도감";}
    public static bool Button(Rect r,int id,bool selected=false,bool enabled=true,int partner=-1)
    {
        bool previous=GUI.enabled;GUI.enabled=previous&&enabled;Draw(r,id,selected);
        int result=DigimonBuildCatalog.Combine(partner,id);string tip=Tooltip(id);
        if(result>=0){ArenaInterface.Fill(new Rect(r.x+2,r.yMax-3,r.width-4,3),new Color(.3f,1,.64f));tip="클릭하여 융합 → "+DigimonBuildCatalog.Data.items[result].name+"\n"+DigimonBuildCatalog.Data.items[result].description;}
        bool clicked=GUI.Button(r,new GUIContent("",tip),GUIStyle.none);GUI.enabled=previous;return clicked;
    }
}
