using System.Collections.Generic;
using UnityEngine;

/// <summary>Code-drawn technique emblems and one inspect panel for solo and online.</summary>
public static class DigimonSkillUI
{
    static readonly Dictionary<string,Texture2D> icons=new Dictionary<string,Texture2D>();
    static float Line(float x,float y,float ax,float ay,float bx,float by)
    {float dx=bx-ax,dy=by-ay,t=Mathf.Clamp01(((x-ax)*dx+(y-ay)*dy)/(dx*dx+dy*dy));return Mathf.Sqrt(Mathf.Pow(x-ax-t*dx,2)+Mathf.Pow(y-ay-t*dy,2));}
    static float Ring(float x,float y,float cx,float cy,float radius)
    {return Mathf.Abs(Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy))-radius);}
    static float Mark(string v,float x,float y)
    {
        float d=10;
        if(v=="bubble")return Mathf.Min(Ring(x,y,-.25f,-.18f,.22f),Mathf.Min(Ring(x,y,.24f,.2f,.29f),Ring(x,y,-.26f,.4f,.1f)));
        if(v=="claws"||v=="darkclaw"||v=="needles")
        {for(int i=-1;i<=1;i++)d=Mathf.Min(d,Line(x,y,-.28f+i*.23f,-.48f,.22f+i*.23f,.52f));return d;}
        if(v=="lightning"||v=="electricorb"||v=="thunderbeam")
        {d=Mathf.Min(Line(x,y,.23f,.58f,-.24f,.01f),Mathf.Min(Line(x,y,-.24f,.01f,.23f,.01f),Line(x,y,.23f,.01f,-.17f,-.58f)));return v=="electricorb"?Mathf.Min(d,Ring(x,y,0,0,.52f)):d;}
        if(v=="fire"||v=="bluefire"||v=="spiral")
        {d=Mathf.Min(Line(x,y,-.39f,-.34f,0,.57f),Mathf.Min(Line(x,y,0,.57f,.36f,-.34f),Line(x,y,-.39f,-.34f,.36f,-.34f)));return Mathf.Min(d,Line(x,y,-.12f,-.28f,.11f,.19f));}
        if(v=="missiles"||v=="meteors")
        {for(int i=0;i<2;i++){float o=i*.35f-.17f;d=Mathf.Min(d,Line(x,y,-.38f+o,-.36f,.27f+o,.38f));d=Mathf.Min(d,Ring(x,y,.27f+o,.38f,.09f));}return d;}
        if(v=="vine"||v=="whip")
        {for(int i=0;i<12;i++){float y0=-.55f+i*.09f,y1=y0+.09f;d=Mathf.Min(d,Line(x,y,Mathf.Sin(y0*7)*.32f,y0,Mathf.Sin(y1*7)*.32f,y1));}return d;}
        if(v=="flower")
        {d=Ring(x,y,0,0,.11f);for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;d=Mathf.Min(d,Ring(x,y,Mathf.Cos(a)*.3f,Mathf.Sin(a)*.3f,.18f));}return d;}
        if(v=="gate")return Mathf.Min(Ring(x*.75f,y,0,0,.48f),Line(x,y,0,-.45f,0,.45f));
        if(v=="phoenix"||v=="starlight")
        {d=Line(x,y,0,-.5f,0,.34f);for(int sign=-1;sign<=1;sign+=2)for(int i=0;i<3;i++)d=Mathf.Min(d,Line(x,y,0,-.2f+i*.16f,sign*(.58f-i*.07f),.35f+i*.08f));return d;}
        if(v=="ice")
        {for(int i=0;i<3;i++){float a=i*Mathf.PI/3;d=Mathf.Min(d,Line(x,y,-Mathf.Cos(a)*.56f,-Mathf.Sin(a)*.56f,Mathf.Cos(a)*.56f,Mathf.Sin(a)*.56f));}return d;}
        if(v=="seven")
        {d=Ring(x,y,0,0,.12f);for(int i=0;i<6;i++){float a=i*Mathf.PI/3;d=Mathf.Min(d,Ring(x,y,Mathf.Cos(a)*.43f,Mathf.Sin(a)*.43f,.09f));}return d;}
        if(v=="holy")return Mathf.Min(Line(x,y,0,-.55f,0,.58f),Line(x,y,-.36f,.14f,.36f,.14f));
        if(v=="horn")return Mathf.Min(Line(x,y,-.35f,-.45f,.12f,.58f),Line(x,y,.12f,.58f,.38f,-.45f));
        if(v=="air"||v=="water")
        {for(int i=-1;i<=1;i++)d=Mathf.Min(d,Mathf.Abs(y-i*.27f-Mathf.Sin(x*5)*.09f)+Mathf.Max(0,Mathf.Abs(x)-.57f));return d;}
        // Gaia Force / dark energy: an orb with orbiting rays.
        d=Ring(x,y,0,0,.31f);
        for(int i=0;i<8;i++){float a=i*Mathf.PI/4;d=Mathf.Min(d,Line(x,y,Mathf.Cos(a)*.43f,Mathf.Sin(a)*.43f,Mathf.Cos(a)*.61f,Mathf.Sin(a)*.61f));}
        return d;
    }
    public static Texture2D Icon(DigimonSkillCatalog.Entry skill)
    {
        Texture2D texture;if(icons.TryGetValue(skill.id,out texture))return texture;
        const int size=96;texture=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        Color tint=skill.Tint;var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float px=(x+.5f)*2/size-1,py=(y+.5f)*2/size-1;
            float d=Mark(skill.visual,px,py),light=Mathf.Clamp01((.055f-d)/.027f),glow=Mathf.Exp(-d*12)*.4f;
            Color c=Color.Lerp(new Color(.018f,.033f,.05f),tint,glow+Mathf.Max(0,1-px*px-py*py)*.1f);
            c=Color.Lerp(c,Color.Lerp(tint,Color.white,.72f),light);
            if(x<2||y<2||x>=size-2||y>=size-2)c=Color.Lerp(tint,Color.white,.15f);
            else if(x==5||y==5||x==size-6||y==size-6)c=Color.Lerp(c,tint,.35f);
            pixels[y*size+x]=c;
        }
        texture.SetPixels(pixels);texture.Apply();icons.Add(skill.id,texture);return texture;
    }
    public static bool WantsDetails(Rect r,Event e)
    {return e!=null&&e.type==EventType.MouseDown&&e.button==1&&r.Contains(e.mousePosition);}
    public static bool DrawIcon(Rect r,DigimonSkillCatalog.Entry skill)
    {
        GUI.DrawTexture(r,Icon(skill));GUI.Label(r,new GUIContent("",skill.name+" · 우클릭으로 스킬 정보"));
        if(GUI.enabled&&WantsDetails(r,Event.current)){Event.current.Use();return true;}return false;
    }
    static void Fill(Rect r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    public static string Formula(DigimonSkillCatalog.Entry s,int star)
    {int i=DigimonCombatMath.StarIndex(star);return (s.adRatio[i]>0?"공격력 "+(s.adRatio[i]*100).ToString("0.#")+"%":"")+(s.adRatio[i]>0&&s.apRatio[i]>0?" + ":"")+(s.apRatio[i]>0?"주문력 "+(s.apRatio[i]*100).ToString("0.#")+"%":"");}
    public static bool DrawDetails(Rect r,DigimonSkillCatalog.Entry s,int star,DigimonSkillCatalog.Stats stats,string context)
    {
        if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){Event.current.Use();return false;}
        Fill(r,new Color(.02f,.037f,.058f,.99f));Fill(new Rect(r.x,r.y,r.width,2),s.Tint);
        var body=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true};body.normal.textColor=new Color(.79f,.86f,.9f);
        var title=new GUIStyle(body){fontSize=24,fontStyle=FontStyle.Bold};title.normal.textColor=Color.white;
        var muted=new GUIStyle(body){fontSize=14};muted.normal.textColor=new Color(.55f,.68f,.75f);
        GUI.DrawTexture(new Rect(r.x+22,r.y+22,66,66),Icon(s));
        GUI.Label(new Rect(r.x+104,r.y+21,r.width-164,38),s.name,title);
        GUI.Label(new Rect(r.x+104,r.y+61,r.width-145,32),s.ScalingRole+" · "+s.DamageLabel+" 피해 · "+new string('★',star),body);
        if(GUI.Button(new Rect(r.xMax-50,r.y+17,32,32),"×"))return false;
        GUI.Label(new Rect(r.x+22,r.y+105,r.width-44,55),s.description,body);
        GUI.Label(new Rect(r.x+22,r.y+166,r.width-44,25),context,muted);
        Fill(new Rect(r.x+18,r.y+199,r.width-36,77),new Color(.06f,.09f,.14f));
        string[] names={"공격력","주문력","방어력","마법저항력"};float[] values={stats.attack,stats.abilityPower,stats.armor,stats.magicResist};
        for(int i=0;i<4;i++){float x=r.x+30+i*(r.width-60)/4;GUI.Label(new Rect(x,r.y+207,130,26),names[i],muted);GUI.Label(new Rect(x,r.y+234,130,34),values[i].ToString("0.#"),title);}
        GUI.Label(new Rect(r.x+22,r.y+290,r.width-44,49),"기본 공격: 공격력 100%의 물리 피해 · "+stats.range+"칸\n"+s.attackStyle+" · 공격 속도 "+stats.speed.ToString("0.00")+"회/초",body);
        GUI.Label(new Rect(r.x+22,r.y+352,r.width-44,28),Formula(s,star),body);
        GUI.Label(new Rect(r.x+22,r.y+387,r.width-44,36),"총 "+s.Damage(star,stats.attack,stats.abilityPower).ToString("0.#")+" "+s.DamageLabel+" 피해 / 대상",title);
        GUI.Label(new Rect(r.x+22,r.y+429,r.width-44,44),s.shots+"회 명중 · 최대 "+s.targets+"명 · 대상당 총합 / 타수\n방어·추가 피해 적용 전 · 물리: 방어력 / 마법: 마법저항력",muted);
        for(int i=0;i<3;i++)
        {
            float x=r.x+22+i*(r.width-44)/3,w=(r.width-44)/3-8;
            Fill(new Rect(x,r.y+484,w,75),i==star-1?new Color(.16f,.15f,.09f):new Color(.045f,.07f,.1f));
            GUI.Label(new Rect(x+9,r.y+490,w-18,34),new string('★',i+1)+"  "+Formula(s,i+1),new GUIStyle(muted){fontSize=12});
            float ad=stats.attack/Mathf.Pow(1.5f,star-1)*Mathf.Pow(1.5f,i);
            GUI.Label(new Rect(x+9,r.y+524,w-18,27),s.Damage(i+1,ad,stats.abilityPower).ToString("0.#")+" 피해",body);
        }
        GUI.Label(new Rect(r.x+22,r.y+574,r.width-44,47),"성급 비교: 같은 장비·시너지 유지 · 기본 주문력 100\n마나 "+stats.startMana.ToString("0")+" / "+stats.maxMana.ToString("0")+" · 준비 "+s.windup.ToString("0.00")+"초 · 기절 "+s.stun.ToString("0.#")+"초",muted);
        return true;
    }
}
