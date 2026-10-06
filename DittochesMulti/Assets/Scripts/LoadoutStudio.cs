using System;
using UnityEngine;

public sealed class LoadoutStudio : IDisposable
{
    TacticalArena preview;
    readonly ArenaInterface ui=new ArenaInterface();
    TamerLoadout draft;
    public int Tab;
    float effectStarted=-10;
    bool walking;
    Vector3 position=new Vector3(0,.23f,0),velocity;
    GUIStyle title,body,small;
    public TamerLoadout Draft {get{return draft;}}
    public void Preview(TamerLoadout value,int tab){draft=value.Copy();Tab=tab;effectStarted=Time.unscaledTime;}
    public bool Draw(Rect r,ref TamerLoadout equipped,bool editable=true)
    {
        if(draft==null)draft=equipped.Copy();draft.Normalize();
        if(title==null){body=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true};body.normal.textColor=new Color(.87f,.93f,.95f);title=new GUIStyle(body){fontSize=26,fontStyle=FontStyle.Bold};small=new GUIStyle(body){fontSize=13};small.normal.textColor=new Color(.60f,.74f,.80f);}
        ArenaInterface.Fill(r,new Color(.018f,.035f,.055f,.97f));
        GUI.Label(new Rect(r.x+22,r.y+16,r.width-44,38),"테이머 설정",title);
        GUI.Label(new Rect(r.x+22,r.y+58,r.width-44,26),"나의 테이머와 전장을 꾸미고, 전투의 마지막 장면을 선택하세요",small);
        string[] tabs={"테이머","필드","처형 효과"};
        for(int i=0;i<3;i++)if(ui.Button(new Rect(r.x+20+i*108,r.y+105,102,38),new GUIContent(tabs[i]),true,Tab==i)){Tab=i;effectStarted=Time.unscaledTime;}
        string[] choices=Tab==0?TamerLoadout.Tamers:Tab==1?TamerLoadout.Fields:TamerLoadout.Finishers;
        int current=Tab==0?draft.tamer:Tab==1?draft.field:draft.finisher;
        for(int i=0;i<choices.Length;i++)
        {
            Rect card=new Rect(r.x+20,r.y+160+i*99,318,88);bool chosen=i==current;
            ArenaInterface.Fill(card,chosen?new Color(.07f,.18f,.22f):new Color(.032f,.065f,.09f));
            Color color=Tab==1?TamerLoadout.FieldColor(i):Tab==2?TamerLoadout.FinishColor(i):new Color(.42f,.86f,.88f);
            ArenaInterface.Fill(new Rect(card.x,card.y,3,card.height),chosen?color:new Color(.14f,.25f,.3f));
            if(Tab==0){var portrait=TamerLoadout.Portrait(i);if(portrait!=null)GUI.DrawTexture(new Rect(card.x+9,card.y+9,70,70),portrait,ScaleMode.ScaleToFit);}
            else
            {
                Rect swatch=new Rect(card.x+17,card.y+16,58,55);ArenaInterface.Fill(swatch,Color.Lerp(Color.black,color,.28f));
                for(int line=0;line<4;line++)ArenaInterface.Fill(new Rect(swatch.x+5,swatch.y+9+line*11,48-line*5,2),color);
            }
            GUI.Label(new Rect(card.x+89,card.y+14,220,27),choices[i],body);
            GUI.Label(new Rect(card.x+89,card.y+48,214,30),chosen?"미리보는 중":Tab==0?"전장에서 직접 이동":Tab==1?"전장 테마":"승리 후 마무리 연출",small);
            if(GUI.Button(card,GUIContent.none,GUIStyle.none)){if(Tab==0)draft.tamer=i;else if(Tab==1)draft.field=i;else draft.finisher=i;effectStarted=Time.unscaledTime;}
        }
        GUI.Label(new Rect(r.x+22,r.yMax-153,316,62),"외형 설정은 전투 능력치에 영향을 주지 않습니다.\n선택 후 적용하면 다음 게임에도 유지됩니다.",small);
        Rect view=new Rect(r.x+366,r.y+104,r.width-388,r.height-218);
        if(preview==null)preview=new TacticalArena(view,true);
        if(Event.current.type==EventType.Repaint)
        {
            preview.SetField(draft.field);preview.SetLoadoutCamera(Tab==0);preview.BeginFrame(-1,-1,-1,false);
            Vector3 target=Tab==0?(walking?new Vector3(Mathf.Sin(Time.unscaledTime*.8f)*1.15f,.23f,Mathf.Cos(Time.unscaledTime*.8f)*.4f):new Vector3(0,.23f,0)):new Vector3(0,.23f,-3.3f);
            position=Vector3.SmoothDamp(position,target,ref velocity,.22f,3,Time.unscaledDeltaTime);
            preview.SetTamer("preview",draft.tamer,position,target,Mathf.Clamp01(velocity.magnitude),Time.unscaledTime,.15f);
            if(Tab==2)preview.DrawFinisher(draft.finisher,position,new Vector3(0,.23f,1.8f),Time.unscaledTime-effectStarted);
            preview.Render();
        }
        GUI.DrawTexture(view,preview.Texture,ScaleMode.StretchToFill,false);
        if(ui.Button(new Rect(view.x+16,view.yMax-51,160,35),new GUIContent(Tab==2?"처형 효과 재생":walking?"이동 시연 정지":"테이머 이동 시연")))
        {if(Tab==2)effectStarted=Time.unscaledTime;else walking=!walking;}
        GUI.Label(new Rect(view.x,r.yMax-101,view.width,30),"미리보기  ·  "+draft.Summary,small);
        GUI.Label(new Rect(view.x,r.yMax-60,view.width-410,46),editable?"현재 적용: "+equipped.Summary:"매칭 중에는 외형을 바꿀 수 없습니다",small);
        if(ui.Button(new Rect(r.xMax-408,r.yMax-67,172,44),new GUIContent("선택 되돌리기")))draft=equipped.Copy();
        if(ui.Button(new Rect(r.xMax-222,r.yMax-67,200,44),new GUIContent("이 외형 적용"),editable,true))
        {equipped=draft.Copy();equipped.Save();return true;}
        return false;
    }
    public void Dispose(){if(preview!=null){preview.Dispose();preview=null;}}
}
