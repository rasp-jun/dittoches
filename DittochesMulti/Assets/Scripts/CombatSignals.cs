using UnityEngine;

/// <summary>Presentation derived from combat time, so reconnects never restart effects.</summary>
public static class CombatSignals
{
    public struct Signal
    {
        public int kind;public float age;
        public string Caption { get {return kind==1?"위기 효과 발동":kind==2?"야수 최대 중첩":kind==3?"9야수 · 무리의 결집":kind==4?"9화염 · 대폭발":kind==5?"9성역 · 기적의 부활":"";} }
        public Color Color { get {return kind==4?new Color(1,.36f,.12f):kind==5?new Color(1,.92f,.6f):kind==3?new Color(.4f,1,.72f):kind==1?new Color(1,.79f,.34f):new Color(.4f,.82f,1);} }
    }
    public static float Duration(int kind){return kind>=3&&kind<=5?2.4f:kind==1?1.2f:kind==2?1.4f:0;}
    public static Signal Evaluate(float time,bool crisisUsed,float crisisAt,bool friendship,bool alive=true,int synergyKind=0,float synergyAt=-1,float battleTime=-1)
    {
        if(!alive)return new Signal();
        float age=(battleTime>=0?battleTime:time)-synergyAt;
        if(synergyKind>=3&&synergyKind<=5&&synergyAt>=0&&age>=0&&age<Duration(synergyKind))return new Signal{kind=synergyKind,age=age};
        float crisisAge=time-crisisAt;
        if(crisisUsed&&crisisAt>=0&&crisisAge>=0&&crisisAge<Duration(1)-.0001f)return new Signal{kind=1,age=crisisAge};
        float friendshipAge=time-15;
        if(friendship&&friendshipAge>=0&&friendshipAge<Duration(2)-.0001f)return new Signal{kind=2,age=friendshipAge};
        return new Signal();
    }
}
