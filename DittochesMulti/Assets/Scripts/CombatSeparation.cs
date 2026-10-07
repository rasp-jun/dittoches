using System;

// Small deterministic separation pass, independent of rendering and frame rate.
public sealed class CombatSeparation
{
    public struct Body
    {
        public float x,y;
        public bool active,movable;
        public Body(float x,float y,bool active,bool movable=true)
        {this.x=x;this.y=y;this.active=active;this.movable=movable;}
    }
    readonly float[] dx,dy,startX,startY;
    public CombatSeparation(int capacity)
    {
        if(capacity<1)throw new ArgumentOutOfRangeException(nameof(capacity));
        dx=new float[capacity];dy=new float[capacity];startX=new float[capacity];startY=new float[capacity];
    }
    public void Step(Body[] bodies,int count,float deltaTime)
    {
        if(bodies==null)throw new ArgumentNullException(nameof(bodies));
        if(count<0||count>bodies.Length||count>dx.Length)throw new ArgumentOutOfRangeException(nameof(count));
        if(deltaTime<=0||float.IsNaN(deltaTime)||float.IsInfinity(deltaTime))return;
        float maxMove=Math.Min(deltaTime,.05f)*2f;
        for(int i=0;i<count;i++){startX[i]=bodies[i].x;startY[i]=bodies[i].y;}
        for(int pass=0;pass<4;pass++)
        {
            Array.Clear(dx,0,count);Array.Clear(dy,0,count);
            for(int i=0;i<count;i++)
            {
                if(!bodies[i].active)continue;
                for(int j=i+1;j<count;j++)
                {
                    if(!bodies[j].active||(!bodies[i].movable&&!bodies[j].movable))continue;
                    float x=bodies[j].x-bodies[i].x,y=bodies[j].y-bodies[i].y;
                    float distance=(float)Math.Sqrt(x*x+y*y);
                    const float spacing=.68f;
                    if(distance>=spacing)continue;
                    if(distance<.0001f)
                    {
                        double angle=(i*17+j*31)*2.39996323;
                        x=(float)Math.Cos(angle);y=(float)Math.Sin(angle);
                    }
                    else {x/=distance;y/=distance;}
                    float amount=(spacing-distance)*(bodies[i].movable&&bodies[j].movable?.5f:1f);
                    if(bodies[i].movable){dx[i]-=x*amount;dy[i]-=y*amount;}
                    if(bodies[j].movable){dx[j]+=x*amount;dy[j]+=y*amount;}
                }
            }
            for(int i=0;i<count;i++)
            {
                if(!bodies[i].active||!bodies[i].movable)continue;
                float x=bodies[i].x+dx[i]-startX[i],y=bodies[i].y+dy[i]-startY[i];
                float length=(float)Math.Sqrt(x*x+y*y);
                if(length>maxMove){x*=maxMove/length;y*=maxMove/length;}
                bodies[i].x=Math.Max(0,Math.Min(6,startX[i]+x));
                bodies[i].y=Math.Max(0,Math.Min(7,startY[i]+y));
            }
        }
    }
}
