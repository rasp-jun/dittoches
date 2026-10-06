using UnityEngine;

/// <summary>Presentation derived from combat time, so reconnects never restart effects.</summary>
public static class CombatSignals
{
    public struct Signal
    {
        public int kind;public float age;
        public string Caption { get {return kind==1?"위기 효과 발동":kind==2?"우정 최대 중첩":"";} }
        public Color Color { get {return kind==1?new Color(1,.79f,.34f):new Color(.4f,.82f,1);} }
    }
    public static float Duration(int kind){return kind==1?1.2f:kind==2?1.4f:0;}
    public static Signal Evaluate(float time,bool crisisUsed,float crisisAt,bool friendship,bool alive=true)
    {
        if(!alive)return new Signal();
        float crisisAge=time-crisisAt;
        if(crisisUsed&&crisisAt>=0&&crisisAge>=0&&crisisAge<Duration(1)-.0001f)return new Signal{kind=1,age=crisisAge};
        float friendshipAge=time-15;
        if(friendship&&friendshipAge>=0&&friendshipAge<Duration(2)-.0001f)return new Signal{kind=2,age=friendshipAge};
        return new Signal();
    }
}
