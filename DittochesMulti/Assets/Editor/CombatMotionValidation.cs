using System;

public static class CombatMotionValidation
{
    static int checks;
    static void Check(bool condition,string reason)
    {checks++;if(!condition)throw new Exception("Combat spacing: "+reason);}
    static float Distance(CombatSeparation.Body a,CombatSeparation.Body b)
    {float x=a.x-b.x,y=a.y-b.y;return (float)Math.Sqrt(x*x+y*y);}
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Dittoches Multi/Validate Combat Motion")]
#endif
    public static void Validate()
    {
        checks=0;
        var solver=new CombatSeparation(32);
        var pair=new[]{new CombatSeparation.Body(3,3,true),new CombatSeparation.Body(3,3,true)};
        solver.Step(pair,2,1f/60);
        Check(Distance(pair[0],new CombatSeparation.Body(3,3,true))<=2f/60+.00001f,"per-frame movement cap");
        for(int i=0;i<120;i++)solver.Step(pair,2,1f/60);
        Check(Distance(pair[0],pair[1])>.67f,"coincident units separate");
        var frozen=new[]{new CombatSeparation.Body(2,2,true,false),new CombatSeparation.Body(2,2,true)};
        for(int i=0;i<120;i++)solver.Step(frozen,2,1f/60);
        Check(frozen[0].x==2&&frozen[0].y==2,"stunned unit stays in place");
        Check(Distance(frozen[0],frozen[1])>.67f,"moving unit clears stunned unit");
        var dead=new[]{new CombatSeparation.Body(1,1,false),new CombatSeparation.Body(1,1,true)};
        solver.Step(dead,2,.1f);
        Check(dead[1].x==1&&dead[1].y==1,"dead units do not block movement");
        var zero=new[]{new CombatSeparation.Body(3,3,true),new CombatSeparation.Body(3,3,true)};
        solver.Step(zero,2,0);solver.Step(zero,2,float.NaN);
        Check(zero[0].x==3&&zero[1].y==3,"invalid delta cannot move units");
        var crowd=new CombatSeparation.Body[16];
        for(int i=0;i<crowd.Length;i++)crowd[i]=new CombatSeparation.Body(0,0,true);
        var repeat=(CombatSeparation.Body[])crowd.Clone();
        var replaySolver=new CombatSeparation(32);
        for(int frame=0;frame<600;frame++)
        {solver.Step(crowd,16,1f/60);replaySolver.Step(repeat,16,1f/60);}
        for(int i=0;i<crowd.Length;i++)
        {
            Check(crowd[i].x>=0&&crowd[i].x<=6&&crowd[i].y>=0&&crowd[i].y<=7,"board bounds");
            Check(crowd[i].x==repeat[i].x&&crowd[i].y==repeat[i].y,"deterministic replay");
            for(int j=i+1;j<crowd.Length;j++)Check(Distance(crowd[i],crowd[j])>.6f,"corner crowd separates");
        }
        Console.WriteLine("COMBAT MOTION: "+checks+" checks passed.");
    }
}
