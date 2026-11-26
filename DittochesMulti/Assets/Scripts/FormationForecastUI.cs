using UnityEngine;

public static class FormationForecastUI
{
    static GUISkin sourceSkin;
    static Font sourceFont;
    static GUIStyle body,title,noteWarning,valueUp,valueDown,valueNeutral;
    static GUIStyle ValueStyle(Color color)
    {var result=new GUIStyle(body){wordWrap=false};result.normal.textColor=color;return result;}
    static void EnsureStyles()
    {
        var skin=GUI.skin;
        if(body!=null&&ReferenceEquals(sourceSkin,skin)&&ReferenceEquals(sourceFont,skin.font))return;
        sourceSkin=skin;sourceFont=skin.font;
        body=new GUIStyle(skin.label){fontSize=12,wordWrap=true};body.normal.textColor=new Color(.77f,.85f,.88f);
        title=new GUIStyle(body){fontSize=16,fontStyle=FontStyle.Bold};
        noteWarning=new GUIStyle(body);noteWarning.normal.textColor=new Color(1,.57f,.4f);
        valueUp=ValueStyle(new Color(.42f,.94f,.69f));valueDown=ValueStyle(new Color(1,.55f,.35f));valueNeutral=ValueStyle(new Color(.64f,.74f,.79f));
    }
    static void Fill(Rect r,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    public static void Draw(Rect r,FormationForecast.Plan plan)
    {
        Fill(r,new Color(.025f,.045f,.056f,.99f));Fill(new Rect(r.x,r.y,r.width,2),new Color(.35f,.66f,.64f));
        EnsureStyles();
        GUI.Label(new Rect(r.x+13,r.y+12,r.width-26,27),"시너지 미리보기",title);
        var note=plan.allowed?body:noteWarning;
        GUI.Label(new Rect(r.x+13,r.y+45,r.width-26,plan.allowed?44:80),plan.message,note);
        if(!plan.allowed)return;
        for(int i=0;i<plan.traits.Length;i++)
        {
            var change=plan.traits[i];float y=r.y+98+i*24;
            var value=change.AfterTier>change.BeforeTier?valueUp:change.AfterTier<change.BeforeTier?valueDown:valueNeutral;
            if(change.before!=change.after)Fill(new Rect(r.x+10,y,r.width-20,22),new Color(.08f,.14f,.16f));
            GUI.Label(new Rect(r.x+14,y,69,22),change.trait.name,body);
            string counts=change.before==change.after?change.after+"/"+change.trait.Target(change.after):change.before+"→"+change.after+"/"+change.trait.Target(change.after);
            GUI.Label(new Rect(r.xMax-112,y,67,22),counts,value);
            string state=change.AfterTier>change.BeforeTier?(change.BeforeTier==0?"활성":"강화"):change.AfterTier<change.BeforeTier?(change.AfterTier==0?"해제":"약화"):change.AfterTier>0?"유지":"대기";
            GUI.Label(new Rect(r.xMax-47,y,38,22),state,value);
        }
        GUI.Label(new Rect(r.x+13,r.yMax-35,r.width-26,28),"미리보기 · 클릭하거나 놓으면 적용",body);
    }
}
