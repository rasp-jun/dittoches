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
        if(v=="holyarrow"||v=="javelin")
        {
            d=Mathf.Min(Line(x,y,-.4f,-.48f,.38f,.48f),Mathf.Min(Line(x,y,.38f,.48f,.06f,.34f),Line(x,y,.38f,.48f,.32f,.14f)));
            if(v=="javelin")return Mathf.Min(d,Ring(x,y,-.15f,-.19f,.24f));
            return Mathf.Min(d,Mathf.Min(Line(x,y,-.4f,-.48f,-.42f,-.19f),Line(x,y,-.4f,-.48f,-.1f,-.43f)));
        }
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
    static GUIStyle detailBody,detailTitle,detailMuted,detailTiny;
    static readonly string[] statNames={"공격력","주문력","방어력","마법저항력"};
    static void DetailStyles()
    {
        if(detailBody!=null)return;
        detailBody=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};detailBody.normal.textColor=BattleHudTheme.Text;
        detailTitle=new GUIStyle(detailBody){fontSize=25,fontStyle=FontStyle.Bold};detailTitle.normal.textColor=BattleHudTheme.Text;
        detailMuted=new GUIStyle(detailBody){fontSize=13};detailMuted.normal.textColor=BattleHudTheme.Muted;
        detailTiny=new GUIStyle(detailMuted){fontSize=12};
    }
    public static Rect DetailCloseRect(Rect r){return new Rect(r.xMax-50,r.y+17,32,32);}
    public static bool DrawDetails(Rect r,DigimonSkillCatalog.Entry s,int star,DigimonSkillCatalog.Stats stats,string context,string collection="")
    {
        if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){Event.current.Use();return false;}
        DetailStyles();var body=detailBody;var title=detailTitle;var muted=detailMuted;
        BattleHudTheme.Surface(r,false,s.Tint);
        BattleHudTheme.Round(new Rect(r.x+20,r.y+21,78,78),Color.Lerp(BattleHudTheme.Raised,s.Tint,.17f));
        GUI.DrawTexture(new Rect(r.x+27,r.y+28,64,64),Icon(s));
        BattleHudTheme.Label(new Rect(r.x+114,r.y+19,r.width-170,19),"TECHNIQUE  /  "+s.DamageLabel.ToUpper(),10,BattleHudTheme.Teal,true);
        GUI.Label(new Rect(r.x+111,r.y+40,r.width-174,39),s.name,title);
        BattleHudTheme.Label(new Rect(r.x+114,r.y+79,r.width-172,21),s.ScalingStats(star)+" 계수  ·  "+new string('★',star),12,BattleHudTheme.Muted);
        Rect close=DetailCloseRect(r);BattleHudTheme.Round(close,BattleHudTheme.Raised);
        BattleHudTheme.Label(close,"×",20,BattleHudTheme.Text,false,TextAnchor.MiddleCenter);
        if(GUI.Button(close,GUIContent.none,GUIStyle.none))return false;
        GUI.Label(new Rect(r.x+22,r.y+118,r.width-44,48),s.description,body);
        GUI.Label(new Rect(r.x+22,r.y+174,r.width-44,27),context,muted);
        float tileWidth=(r.width-62)/4;
        for(int i=0;i<4;i++){float value=i==0?stats.attack:i==1?stats.abilityPower:i==2?stats.armor:stats.magicResist;BattleHudTheme.StatTile(new Rect(r.x+22+i*(tileWidth+6),r.y+206,tileWidth,65),statNames[i],value.ToString("0.#"),i<2?s.Tint:BattleHudTheme.Text);}
        BattleHudTheme.Round(new Rect(r.x+22,r.y+287,r.width-44,157),Color.Lerp(BattleHudTheme.Raised,s.Tint,.08f));
        BattleHudTheme.Label(new Rect(r.x+37,r.y+298,r.width-74,19),"현재 성급의 스킬 피해",11,BattleHudTheme.Muted,true);
        BattleHudTheme.Label(new Rect(r.x+37,r.y+324,r.width-74,37),s.Damage(star,stats.attack,stats.abilityPower).ToString("0.#")+"  "+s.DamageLabel+" / 대상",27,BattleHudTheme.Text,true);
        GUI.Label(new Rect(r.x+37,r.y+369,r.width-74,24),Formula(s,star),body);
        int starIndex=DigimonCombatMath.StarIndex(star);
        GUI.Label(new Rect(r.x+37,r.y+396,r.width-74,21),"공격력 기여 "+(stats.attack*s.adRatio[starIndex]).ToString("0.#")+" + 주문력 기여 "+(stats.abilityPower*s.apRatio[starIndex]).ToString("0.#"),muted);
        GUI.Label(new Rect(r.x+37,r.y+419,r.width-74,19),"대상당 "+s.shots+"회 합계 · 최대 "+s.targets+"명 · 방어·조건부 효과 적용 전",detailTiny);
        BattleHudTheme.Label(new Rect(r.x+22,r.y+455,r.width-44,18),"성급 강화  ·  같은 장비와 시너지 기준 예상치",11,BattleHudTheme.Muted,true);
        for(int i=0;i<3;i++)
        {
            float x=r.x+22+i*(r.width-44)/3,w=(r.width-44)/3-8;
            Color tint=StarUpgradeUI.Tint(i+1);
            BattleHudTheme.Round(new Rect(x,r.y+483,w,117),i==star-1?Color.Lerp(BattleHudTheme.Raised,tint,.15f):BattleHudTheme.Raised);
            Fill(new Rect(x+9,r.y+483,w-18,2),i==star-1?tint:BattleHudTheme.Line);
            BattleHudTheme.Label(new Rect(x+10,r.y+488,w-20,24),new string('★',i+1)+(i==star-1?"  현재":""),16,tint,true);
            float ad=StarUpgradeUI.AttackAt(stats,star,i+1),hp=StarUpgradeUI.HealthAt(stats,star,i+1);
            BattleHudTheme.Label(new Rect(x+10,r.y+515,w-20,20),"체력 "+hp.ToString("0")+" · 공격 "+ad.ToString("0.#"),12,BattleHudTheme.Text);
            BattleHudTheme.Label(new Rect(x+10,r.y+538,w-20,27),s.Damage(i+1,ad,stats.abilityPower).ToString("0.#")+" 스킬 피해",17,tint,true);
            BattleHudTheme.Label(new Rect(x+10,r.y+572,w-20,18),i==0?"기본 기물 1개":i==1?"★ 3개 → ★★":"★★ 3개 → ★★★ · 총 9개",11,BattleHudTheme.Muted);
        }
        BattleHudTheme.Label(new Rect(r.x+22,r.y+608,r.width-44,20),"성급당 체력 +80% · 공격력 +50% · 스킬은 성급별 계수 적용",12,BattleHudTheme.Teal);
        BattleHudTheme.Round(new Rect(r.x+22,r.y+637,r.width-44,62),BattleHudTheme.Raised);
        GUI.Label(new Rect(r.x+34,r.y+646,r.width-68,46),string.IsNullOrEmpty(collection)?"같은 종·같은 성급 3개로 자동 승급 · 최대 3성\n장비 최대 3개 유지 · 초과분 보관함 반환 · 전투 중 승급은 다음 전투 적용":collection,muted);
        GUI.Label(new Rect(r.x+22,r.y+710,r.width-44,21),"평타 "+stats.attack.ToString("0.#")+" 물리 · 공격력 100% · "+stats.range+"칸 · "+stats.speed.ToString("0.00")+"회/초 · "+s.attackStyle,detailTiny);
        GUI.Label(new Rect(r.x+22,r.y+735,r.width-44,25),"마나 "+stats.startMana.ToString("0")+" / "+stats.maxMana.ToString("0")+" · 준비 "+s.windup.ToString("0.00")+"초 · 기절 "+s.stun.ToString("0.#")+"초 · 방어/마저 적용 전 피해",detailTiny);
        return true;
    }
}
