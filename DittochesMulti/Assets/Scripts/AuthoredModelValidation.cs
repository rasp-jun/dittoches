using System;
using UnityEngine;

/// <summary>Playback regressions against the actual authored skeleton, available in Editor and player.</summary>
public static class AuthoredModelValidation
{
    sealed class PoseState
    {
        public Vector3[] positions;
        public Quaternion[] rotations;
        public PoseState(int count){positions=new Vector3[count];rotations=new Quaternion[count];}
    }
    static void Require(bool value,string message,ref int checks)
    {
        if(!value)throw new InvalidOperationException("Authored playback: "+message);
        checks++;
    }
    static PoseState Sample(AuthoredModelData.Clip clip,float time)
    {var state=new PoseState(clip.boneCount);clip.Sample(time,state.positions,state.rotations);return state;}
    static PoseState Capture(DigimonRig rig)
    {
        var state=new PoseState(rig.BoneCount);
        for(int i=0;i<state.positions.Length;i++)
        {state.positions[i]=rig.renderer.bones[i].localPosition;state.rotations[i]=rig.renderer.bones[i].localRotation;}
        return state;
    }
    static void SamePose(DigimonRig rig,PoseState wanted,string context,ref int checks)
    {
        for(int i=0;i<wanted.positions.Length;i++)
        {
            var bone=rig.renderer.bones[i];
            Require((bone.localPosition-wanted.positions[i]).sqrMagnitude<.00000001f
                &&Mathf.Abs(Quaternion.Dot(bone.localRotation,wanted.rotations[i]))>.99999f,
                context+" / "+bone.name,ref checks);
        }
    }
    static void Pose(DigimonRig rig,float time,float speed=0,DigimonSkillCatalog.Entry skill=null,
        float castAge=-1,float attack=0,float death=0)
    {rig.Pose(Vector3.zero,speed,time,skill,castAge,attack,death,1);}

    public static int Validate()
    {
        string previousInspection=DigimonModelLibrary.InspectionClip;
        GameObject parent=null;DigimonModelLibrary.Model model=null;
        int checks=0;
        try
        {
            model=AuthoredModelData.LoadAgumon();
            Require(model!=null&&model.authored!=null,"Authored Agumon asset is available",ref checks);
            parent=new GameObject("Authored playback validation"){hideFlags=HideFlags.HideAndDontSave};
            var rig=new DigimonRig(model,parent.transform,null,0);
            var skill=DigimonSkillCatalog.Find("agumon");
            Require(skill!=null,"Agumon skill definition is available",ref checks);

            // Explicit seeks sample the selected Action exactly, including holds beyond its end.
            foreach(var pair in model.authored.clips)
            {
                DigimonModelLibrary.InspectionClip=pair.Key;
                foreach(float time in new[]{0f,.13f,pair.Value.duration*.5f,pair.Value.duration,pair.Value.duration+1f})
                {
                    rig.ResetPose();Pose(rig,time,skill:skill,castAge:.1f);
                    SamePose(rig,Sample(pair.Value,time),"Seek "+pair.Key+" at "+time,ref checks);
                }
            }

            // Seeking at an unchanged clock must work while paused; castAge is a separate game timeline.
            DigimonModelLibrary.InspectionClip="PepperBreath";rig.ResetPose();Pose(rig,.7f,skill:skill,castAge:.1f);
            SamePose(rig,Sample(model.authored.clips["PepperBreath"],.7f),"Inspection uses the requested clip time",ref checks);
            DigimonModelLibrary.InspectionClip="Hit";rig.ResetPose();Pose(rig,.7f);
            SamePose(rig,Sample(model.authored.clips["Hit"],.7f),"Paused mode selection discards old pose",ref checks);
            rig.Face(Vector3.right);Pose(rig,.7f);
            Require(Vector3.Dot(rig.root.transform.forward,Vector3.right)>.9999f,"Paused inspection rotation is immediate",ref checks);
            rig.Face(Vector3.back);Pose(rig,.7f);
            Require(Vector3.Dot(rig.root.transform.forward,Vector3.back)>.9999f,"Repeated paused rotation is immediate",ref checks);

            // A playing clip switch starts at the displayed pose and reaches the target after 0.12 seconds.
            DigimonModelLibrary.InspectionClip="Idle";rig.ResetPose();Pose(rig,.3f);
            var before=Capture(rig);DigimonModelLibrary.InspectionClip="Attack";Pose(rig,.31f);
            SamePose(rig,before,"Transition begins without snapping",ref checks);
            Pose(rig,.37f);var target=Sample(model.authored.clips["Attack"],.37f);
            for(int i=0;i<target.positions.Length;i++)
            {
                target.positions[i]=Vector3.Lerp(before.positions[i],target.positions[i],.5f);
                target.rotations[i]=Quaternion.Slerp(before.rotations[i],target.rotations[i],.5f);
            }
            SamePose(rig,target,"Transition midpoint",ref checks);
            Pose(rig,.44f);SamePose(rig,Sample(model.authored.clips["Attack"],.44f),"Transition completes",ref checks);

            // Ordinary combat must retain its priority and timer contracts without inspection overrides.
            DigimonModelLibrary.InspectionClip=null;rig.ResetPose();Pose(rig,2,1,skill,.4f,.175f,.5f);
            SamePose(rig,Sample(model.authored.clips["Defeat"],.6f),"Death interrupts casting and attacking",ref checks);
            rig.ResetPose();Pose(rig,2,1,skill,.4f,.175f);
            SamePose(rig,Sample(model.authored.clips["PepperBreath"],.4f),"Casting overrides attack",ref checks);
            rig.ResetPose();Pose(rig,2,1,skill,-1,.175f);
            SamePose(rig,Sample(model.authored.clips["Attack"],.4f),"Attack flash maps to authored Action",ref checks);

            // A newly observed attack keeps its recovery after the .35s flash.
            rig.ResetPose();Pose(rig,0);Pose(rig,.01f,attack:.35f);
            for(int frame=1;frame<=8;frame++)
                Pose(rig,.01f+frame*.05f,attack:Mathf.Max(0,.35f-frame*.05f));
            SamePose(rig,Sample(model.authored.clips["Attack"],.4f),"Attack recovery is not compressed into the flash",ref checks);
            Pose(rig,.46f,skill:skill,castAge:.1f);
            Pose(rig,.56f,skill:skill,castAge:.2f);
            Pose(rig,.61f,skill:skill,castAge:.25f);
            SamePose(rig,Sample(model.authored.clips["PepperBreath"],.25f),"Casting interrupts extended attack recovery",ref checks);

            // World movement and model scale determine cadence, not a speed hint.
            rig.ResetPose();Pose(rig,0,1);
            float cycleDistance=(.30f/.62f)*.66f;
            for(int frame=1;frame<=6;frame++)
                rig.Pose(Vector3.forward*(cycleDistance*frame/12f),1,frame*.05f,null,-1,0,0,1);
            SamePose(rig,Sample(model.authored.clips["Walk"],.45f),"Half a stride distance gives half a gait cycle",ref checks);
            rig.ResetPose();rig.Pose(Vector3.zero,1,0,null,-1,0,0,3);
            for(int frame=1;frame<=6;frame++)
                rig.Pose(Vector3.forward*(cycleDistance*1.11f*frame/12f),1,frame*.05f,null,-1,0,0,3);
            SamePose(rig,Sample(model.authored.clips["Walk"],.45f),"Larger units retain planted-foot cadence",ref checks);

            // A rewind outside Locomotion clears the old gait phase before walking resumes.
            rig.ResetPose();Pose(rig,1,1);Pose(rig,1.08f,1);Pose(rig,1.16f,1);
            DigimonModelLibrary.InspectionClip="Attack";Pose(rig,.02f);
            var fresh=new DigimonRig(model,parent.transform,null,0);Pose(fresh,.02f);
            DigimonModelLibrary.InspectionClip=null;Pose(rig,.15f,1);Pose(rig,.30f,1);
            Pose(fresh,.15f,1);Pose(fresh,.30f,1);
            SamePose(rig,Capture(fresh),"Rewind clears gait phase in every state",ref checks);

            // Explicit reset produces the same initial walk regardless of the previously displayed clip.
            rig.ResetPose();Pose(rig,.30f,1);
            SamePose(rig,Sample(model.authored.clips["Walk"],0),"Reset clears gait and transition history",ref checks);
            Debug.Log("AUTHORED PLAYBACK PASS: "+checks+" checks; explicit seeks, paused rotation, transitions, combat priority and rewind.");
            return checks;
        }
        finally
        {
            DigimonModelLibrary.InspectionClip=previousInspection;
            if(parent!=null)UnityEngine.Object.DestroyImmediate(parent);
            if(model!=null&&model.mesh!=null)UnityEngine.Object.DestroyImmediate(model.mesh);
        }
    }
}
