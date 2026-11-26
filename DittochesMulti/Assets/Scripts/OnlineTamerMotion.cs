using UnityEngine;

// One timeline per global participant, always in that participant's home coordinates.
public sealed class OnlineTamerMotion
{
    public const float Speed=4;
    public Vector3 Position {get;private set;}
    public Vector3 Velocity {get;private set;}
    public Vector3 Target {get;private set;}
    public int Sequence {get;private set;}
    public double Stamp {get;private set;}=double.NegativeInfinity;
    Vector3 sample,authority,authorityTarget,velocity;
    float sampledAt,authorityAt,lastStep;
    bool predicting;
    public OnlineTamerMotion(Vector3 position,float now)
    {Position=sample=authority=Target=authorityTarget=position;sampledAt=authorityAt=lastStep=now;}
    public void Receive(Vector3 position,Vector3 target,int sequence,double stamp,float now,float transit,int pending)
    {
        if(stamp<=Stamp||sequence<Sequence)return;
        if(double.IsNegativeInfinity(Stamp)&&!predicting)Position=Vector3.MoveTowards(position,target,Mathf.Clamp(transit,0,.12f)*Speed);
        Stamp=stamp;Sequence=sequence;authority=position;authorityTarget=target;authorityAt=now-Mathf.Clamp(transit,0,.12f);
        if(pending>sequence)return;
        predicting=false;sample=authority;Target=authorityTarget;sampledAt=authorityAt;
    }
    public void Predict(Vector3 target,float now)
    {sample=Position;Target=target;sampledAt=now;predicting=true;}
    public void CancelPrediction()
    {if(!predicting)return;predicting=false;sample=authority;Target=authorityTarget;sampledAt=authorityAt;}
    public void Step(float now)
    {
        float dt=now-lastStep;if(dt<=0)return;lastStep=now;
        Vector3 expected=Vector3.MoveTowards(sample,Target,Mathf.Max(0,now-sampledAt)*Speed),before=Position;
        Position=Vector3.SmoothDamp(Position,expected,ref velocity,.07f,12,Mathf.Min(dt,.25f));
        Velocity=(Position-before)/dt;
    }
    public static Vector3 View(Vector3 home,bool rival)
    {return rival?new Vector3(-home.x,home.y,-home.z):home;}
}
