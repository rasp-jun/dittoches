using System;

public static class SkillTimingValidation
{
    static int checks;
    static void Check(bool value,string reason)
    {checks++;if(!value)throw new Exception("Skill timing: "+reason);}
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Dittoches Multi/Validate Skill Timing")]
#endif
    public static void Validate()
    {
        checks=0;
        Check(SkillGeometry.DueHits(1.039f,.68f,.36f,.07f,7)==0,"no damage before first sphere arrives");
        for(int i=0;i<7;i++)Check(SkillGeometry.DueHits(1.04f+i*.07f,.68f,.36f,.07f,7)==i+1,"seven separate impacts");
        Check(SkillGeometry.DueHits(10,.68f,.36f,.07f,7)==7,"long frame cannot add an eighth hit");
        foreach(float dt in new[]{1f/30,1f/60,1f/144,.5f})
        {
            int applied=0;
            for(float age=0;age<3;age+=dt)
            {
                int due=SkillGeometry.DueHits(age,.68f,.36f,.07f,7);
                Check(due>=applied,"monotonic impact count");applied+=due-applied;
            }
            Check(applied==7,"frame-rate independent total");
        }
        foreach(string shape in new[]{"line","cone"})
        {
            Check(SkillGeometry.Contains(shape,3,4,3,2,3,2,1,4),"forward target");
            Check(!SkillGeometry.Contains(shape,3,4,3,2,3,5,1,4),"behind caster");
            Check(!SkillGeometry.Contains(shape,3,4,3,2,5,3,1,4),"outside width");
            Check(!SkillGeometry.Contains(shape,3,4,3,2,3,-1,1,4),"outside reach");
            Check(SkillGeometry.Contains(shape,3,4,3,4,3,4,1,4),"overlapping source and aim stays finite");
        }
        Check(SkillGeometry.Contains("radial",3,4,0,0,3,5,1,0),"radial includes rear");
        Check(SkillGeometry.Contains("splash",0,0,3,4,3,5,1,0),"splash centered on impact");
        Console.WriteLine("SKILL TIMING: "+checks+" checks passed.");
    }
}
