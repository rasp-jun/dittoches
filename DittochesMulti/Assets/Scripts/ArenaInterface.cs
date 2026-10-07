using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared presentation feedback. Never changes gameplay or captures input outside a control.</summary>
public sealed class ArenaInterface
{
    sealed class Hover { public float amount,time; }
    readonly Dictionary<Rect,Hover> hovers=new Dictionary<Rect,Hover>();
    readonly Dictionary<int,GUIStyle> buttons=new Dictionary<int,GUIStyle>();
    string tooltip="";float tooltipSince;
    GUIStyle tooltipStyle;
    public static readonly Color Gold=new Color(.84f,.69f,.37f),Ink=new Color(.025f,.045f,.056f);
    public static void Fill(Rect rect,Color color)
    {Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
    public static Color Rarity(int cost)
    {return cost==1?new Color(.53f,.65f,.69f):cost==2?new Color(.25f,.74f,.47f):cost==3?new Color(.25f,.57f,.92f):cost==4?new Color(.72f,.38f,.94f):new Color(.96f,.72f,.28f);}
    public float HoverAmount(Rect rect,bool available)
    {
        Hover state;if(!hovers.TryGetValue(rect,out state)){state=new Hover{time=Time.unscaledTime};hovers.Add(rect,state);}
        if(Event.current.type==EventType.Repaint)
        {
            float dt=Mathf.Max(0,Time.unscaledTime-state.time);state.time=Time.unscaledTime;
            state.amount=Mathf.MoveTowards(state.amount,available&&rect.Contains(Event.current.mousePosition)?1:0,dt*8);
        }
        return state.amount;
    }
    public bool Button(Rect rect,GUIContent content,bool enabled=true,bool selected=false,int fontSize=14)
    {
        bool previous=GUI.enabled,available=previous&&enabled;float hover=HoverAmount(rect,available);
        Fill(rect,available?Color.Lerp(selected?Gold:new Color(.20f,.30f,.32f),new Color(.60f,.79f,.76f),hover*.8f):new Color(.12f,.19f,.21f));
        Color normal=selected?new Color(.17f,.14f,.07f):new Color(.035f,.07f,.085f);
        Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2),Color.Lerp(normal,new Color(.095f,.18f,.18f),hover));
        GUIStyle style;if(!buttons.TryGetValue(fontSize,out style))
        {style=new GUIStyle(GUIStyle.none){fontSize=fontSize,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};buttons.Add(fontSize,style);}
        style.normal.textColor=available?(selected?new Color(1,.86f,.53f):new Color(.86f,.92f,.92f)):new Color(.36f,.43f,.44f);
        style.hover.textColor=style.active.textColor=style.normal.textColor;
        GUI.enabled=available;bool clicked=GUI.Button(rect,content,style);GUI.enabled=previous;return clicked;
    }
    public void Tooltip(Rect canvas,bool suppress=false)
    {
        if(Event.current.type!=EventType.Repaint)return;
        string current=suppress||GUIUtility.hotControl!=0?"":GUI.tooltip??"";
        if(current!=tooltip){tooltip=current;tooltipSince=Time.unscaledTime;}
        float age=Time.unscaledTime-tooltipSince-.32f;if(string.IsNullOrEmpty(current)||age<0)return;
        if(tooltipStyle==null){tooltipStyle=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};tooltipStyle.normal.textColor=new Color(.85f,.9f,.92f);}
        float width=Mathf.Min(350,canvas.width-24),height=Mathf.Clamp(tooltipStyle.CalcHeight(new GUIContent(current),width-26)+26,52,260);
        Vector2 p=Event.current.mousePosition;float y=p.y+22;
        if(y+height>canvas.yMax-12)y=p.y-height-16;
        Rect r=new Rect(Mathf.Clamp(p.x+18,canvas.x+12,canvas.xMax-width-12),Mathf.Clamp(y,canvas.y+12,canvas.yMax-height-12),width,height);
        Color old=GUI.color;GUI.color=new Color(old.r,old.g,old.b,old.a*Mathf.Clamp01(age/.12f));
        GUI.DrawTexture(new Rect(r.x+4,r.y+5,r.width,r.height),Texture2D.blackTexture);
        Color fade=GUI.color;GUI.color=new Color(.025f,.045f,.056f,fade.a*.98f);GUI.DrawTexture(r,Texture2D.whiteTexture);
        GUI.color=new Color(.42f,.65f,.64f,fade.a);GUI.DrawTexture(new Rect(r.x,r.y,r.width,2),Texture2D.whiteTexture);
        GUI.color=fade;GUI.Label(new Rect(r.x+13,r.y+13,width-26,height-26),current,tooltipStyle);GUI.color=old;
    }
}
