using System.Collections.Generic;
using UnityEngine;

public sealed partial class TacticalArena
{
    readonly Dictionary<string,DigimonModelLibrary.Model> models=new Dictionary<string,DigimonModelLibrary.Model>();
    Material modelMaterial;
    bool modelStudio;
    Material studioShadow;
    public System.Collections.Generic.IEnumerable<string> FaithfulMotions()
    {foreach(Actor actor in actors.Values)if(actor.faithful!=null)yield return actor.faithful.Motion;}
    public void UseModelStudio(Vector3 origin)
    {
        if(modelStudio)return;
        modelStudio=true;
        foreach(Transform child in root.transform)
            if(child.gameObject!=camera.gameObject)child.gameObject.SetActive(false);
        camera.backgroundColor=new Color(.105f,.13f,.17f);
        var floor=Material(new Color(.18f,.22f,.28f),true);
        Shape("Model review floor",cube,floor,origin+Vector3.up*.065f,new Vector3(60,.065f,60));
        studioShadow=Material(new Color(.105f,.13f,.17f),true);
    }
    bool PoseModel(Actor actor,string id,float speed,float time,DigimonSkillCatalog.Entry skill,float castAge,float attack,float death,int star)
    {
        if(!DigimonModelLibrary.PreviewEnabled&&FaithfulModelData.Available(id))
        {
            if(actor.faithful==null||actor.faithful.Data.id!=id)
            {
                if(actor.faithful!=null)Release(actor.faithful.gameObject);
                var node=new GameObject(id+" / faithful model"){layer=Layer};node.transform.SetParent(actor.root.transform,false);
                actor.faithful=node.AddComponent<FaithfulModelActor>();actor.faithful.Initialize(id,Layer);
            }
            actor.faithfulGeneration=generation;
            if(!actor.faithful.gameObject.activeSelf)actor.faithful.gameObject.SetActive(true);
            actor.portrait.enabled=false;
            Vector3 facing=actor.hasFacing?actor.facing:-actor.faithful.transform.forward;actor.hasFacing=false;
            actor.faithful.SetSize(DigimonVisualScale.Height(id,star));
            float shadow=DigimonVisualScale.Shadow(id);
            actor.contactShadow.transform.localScale=new Vector3(shadow,.01f,shadow*.72f);
            actor.faithful.Pose(time,speed,facing,skill,castAge,attack,death,actor.attackSerial,actor.attackCooldown,actor.attackInRange,actor.hitAmount,actor.hitTime);
            return true;
        }
        if(!DigimonModelLibrary.HasModel(id))return false;
        if(actor.rig==null||actor.rig.model.id!=id)
        {
            if(actor.rig!=null)Release(actor.rig.root);
            DigimonModelLibrary.Model model;
            if(!models.TryGetValue(id,out model))
            {model=DigimonModelLibrary.Build(id);if(model==null)return false;models.Add(id,model);resources.Add(model.mesh);}
            if(modelMaterial==null)
            {
                var shader=Resources.Load<Shader>("CharacterToon");
                if(shader!=null&&model.authored!=null)
                {modelMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave};resources.Add(modelMaterial);}
                else {modelMaterial=Material(Color.white);if(modelMaterial.HasProperty("_VertexColor"))modelMaterial.SetFloat("_VertexColor",1);}
            }
            actor.rig=new DigimonRig(model,actor.root.transform,modelMaterial,Layer);
        }
        actor.rig.root.SetActive(true);actor.portrait.enabled=false;
        if(actor.hasFacing)actor.rig.Face(actor.facing);
        actor.hasFacing=false;
        actor.rig.Pose(actor.root.transform.localPosition,speed,time,skill,castAge,attack,death,star);
        actor.contactShadow.transform.localScale=new Vector3(.36f,.01f,.29f);
        if(modelStudio)
        {
            actor.teamBase.enabled=false;
            actor.contactShadow.sharedMaterial=contactMaterial!=null?contactMaterial:studioShadow;
        }
        return true;
    }
    public void FaceActor(object key,Vector3 direction)
    {Actor actor;if(actors.TryGetValue(key,out actor)){actor.facing=direction;actor.hasFacing=true;}}
    public void CombatMotion(object key,int attackSerial,float cooldown,bool inRange,float hitTime=float.NaN)
    {Actor actor;if(actors.TryGetValue(key,out actor)){actor.attackSerial=attackSerial;actor.attackCooldown=cooldown;actor.attackInRange=inRange;actor.hitTime=hitTime;}}
    public void ResetModelPose(object key)
    {Actor actor;if(actors.TryGetValue(key,out actor)&&actor.rig!=null)actor.rig.ResetPose();}
    public bool ModelBounds(object key,out Bounds bounds)
    {
        Actor actor;bounds=new Bounds();
        if(!actors.TryGetValue(key,out actor)||actor.rig==null)return false;
        bounds=actor.rig.model.mesh.bounds;return true;
    }
    public AuthoredModelData.Clip ModelClip(object key,string name)
    {
        Actor actor;AuthoredModelData.Clip clip;
        return actors.TryGetValue(key,out actor)&&actor.rig!=null&&actor.rig.model.authored!=null
            &&actor.rig.model.authored.clips.TryGetValue(name,out clip)?clip:null;
    }
    Vector3 ModelMuzzle(string id,Vector3 origin,Vector3 fallback)
    {
        foreach(Actor actor in actors.Values)
            if(actor.generation==generation&&actor.faithful!=null&&actor.faithful.Data.id==id&&actor.faithful.Mouth!=null
                &&(actor.root.transform.localPosition-origin).sqrMagnitude<.09f)
                return root.transform.InverseTransformPoint(actor.faithful.Mouth.position);
        foreach(Actor actor in actors.Values)
            if(actor.generation==generation&&actor.rig!=null&&actor.rig.model.id==id&&actor.rig.root.activeSelf
                &&(actor.root.transform.localPosition-origin).sqrMagnitude<.09f)
                return root.transform.InverseTransformPoint(actor.rig.Mouth);
        return fallback;
    }
    bool ModelSkillEmitter(string id,Vector3 origin,bool atRelease,out Vector3 point)
    {
        Actor nearest=null;float distance=.64f;point=Vector3.zero;
        foreach(Actor actor in actors.Values)
        {
            if(actor.generation!=generation||actor.faithful==null||actor.faithful.Data.id!=id)continue;
            float candidate=(actor.root.transform.localPosition-origin).sqrMagnitude;
            if(candidate<distance){nearest=actor;distance=candidate;}
        }
        if(nearest==null)return false;
        point=root.transform.InverseTransformPoint(nearest.faithful.SkillEmitter(atRelease));return true;
    }
    public void SetView(Vector3 position,Vector3 focus,float fieldOfView)
    {camera.transform.localPosition=position;camera.transform.LookAt(root.transform.TransformPoint(focus));camera.fieldOfView=fieldOfView;}
}
