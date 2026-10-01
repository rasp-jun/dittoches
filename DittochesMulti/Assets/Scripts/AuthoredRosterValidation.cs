using System;
using UnityEngine;

/// <summary>Checks every roster file and its live bone transforms, not only Agumon.</summary>
public static class AuthoredRosterValidation
{
    public static int ValidateModel(string id)
    {
        DigimonModelLibrary.Model model=null;GameObject parent=null;
        string previous=DigimonModelLibrary.InspectionClip;int checks=0;
        try
        {
            model=AuthoredModelData.Load(id);
            if(model==null)throw new InvalidOperationException("Missing model: "+id);
            if(model.authored.clips.Count!=8||model.mesh.vertexCount<100)throw new InvalidOperationException("Incomplete model: "+id);
            checks+=2;
            parent=new GameObject("Roster validation"){hideFlags=HideFlags.HideAndDontSave};
            var rig=new DigimonRig(model,parent.transform,null,0);
            foreach(var pair in model.authored.clips)
            {
                DigimonModelLibrary.InspectionClip=pair.Key;
                foreach(float age in new[]{0f,pair.Value.duration*.25f,pair.Value.duration*.5f,pair.Value.duration})
                {
                    rig.ResetPose();rig.Pose(Vector3.zero,0,age,DigimonSkillCatalog.Find(id),-1,0,0,1);
                    foreach(var bone in rig.renderer.bones)
                    {
                        Vector3 p=bone.position;Quaternion q=bone.rotation;
                        if(float.IsNaN(p.x+p.y+p.z+q.x+q.y+q.z+q.w)||float.IsInfinity(p.x+p.y+p.z)
                            ||p.sqrMagnitude>1000)throw new InvalidOperationException(id+" invalid pose: "+pair.Key+" / "+bone.name);
                        checks++;
                    }
                }
            }
            return checks;
        }
        finally
        {
            DigimonModelLibrary.InspectionClip=previous;
            if(parent!=null)UnityEngine.Object.DestroyImmediate(parent);
            if(model!=null&&model.mesh!=null)UnityEngine.Object.Destroy(model.mesh);
        }
    }
}
