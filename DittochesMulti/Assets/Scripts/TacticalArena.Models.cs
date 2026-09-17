using System.Collections.Generic;
using UnityEngine;

public sealed partial class TacticalArena
{
    readonly Dictionary<string,DigimonModelLibrary.Model> models=new Dictionary<string,DigimonModelLibrary.Model>();
    Material modelMaterial;
    bool PoseModel(Actor actor,string id,float speed,float time,DigimonSkillCatalog.Entry skill,float castAge,float attack,float death,int star)
    {
        if(!DigimonModelLibrary.HasModel(id))return false;
        if(actor.rig==null||actor.rig.model.id!=id)
        {
            if(actor.rig!=null)Release(actor.rig.root);
            DigimonModelLibrary.Model model;
            if(!models.TryGetValue(id,out model))
            {model=DigimonModelLibrary.Build(id);models.Add(id,model);resources.Add(model.mesh);}
            if(modelMaterial==null)
            {modelMaterial=Material(Color.white);if(modelMaterial.HasProperty("_VertexColor"))modelMaterial.SetFloat("_VertexColor",1);}
            actor.rig=new DigimonRig(model,actor.root.transform,modelMaterial,Layer);
        }
        actor.rig.root.SetActive(true);actor.portrait.enabled=false;
        if(actor.hasFacing)actor.rig.Face(actor.facing);
        actor.hasFacing=false;
        actor.rig.Pose(actor.root.transform.localPosition,speed,time,skill,castAge,attack,death,star);
        actor.contactShadow.transform.localScale=new Vector3(.36f,.01f,.29f);
        return true;
    }
    public void FaceActor(object key,Vector3 direction)
    {Actor actor;if(actors.TryGetValue(key,out actor)){actor.facing=direction;actor.hasFacing=true;}}
    Vector3 ModelMuzzle(string id,Vector3 origin,Vector3 fallback)
    {
        foreach(Actor actor in actors.Values)
            if(actor.generation==generation&&actor.rig!=null&&actor.rig.model.id==id&&actor.rig.root.activeSelf
                &&(actor.root.transform.localPosition-origin).sqrMagnitude<.09f)
                return root.transform.InverseTransformPoint(actor.rig.Mouth);
        return fallback;
    }
    public void SetView(Vector3 position,Vector3 focus,float fieldOfView)
    {camera.transform.localPosition=position;camera.transform.LookAt(root.transform.TransformPoint(focus));camera.fieldOfView=fieldOfView;}
}
