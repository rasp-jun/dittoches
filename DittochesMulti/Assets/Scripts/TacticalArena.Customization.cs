using System.Collections.Generic;
using UnityEngine;

public sealed partial class TacticalArena
{
    struct FieldSurface {public MeshRenderer renderer;public Color original;}
    readonly List<FieldSurface> fieldSurfaces=new List<FieldSurface>();
    readonly GameObject[] fieldDecor=new GameObject[3];
    const float TamerModelHeight=.72f;
    int fieldId=-1;
    public int FieldId {get{return Mathf.Max(0,fieldId);}}
    public void SetLobbyCamera()
    {
        camera.transform.localPosition=new Vector3(0,1.9f,-3.9f);
        camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,.85f,0)));camera.fieldOfView=34;
        foreach(var tile in tiles)tile.enabled=false;
        foreach(var seat in seats)seat.enabled=false;
    }
    public void SetLoadoutCamera(bool close)
    {
        camera.transform.localPosition=close?new Vector3(0,2.2f,-4.4f):new Vector3(0,12.8f,-14.2f);
        camera.transform.LookAt(root.transform.TransformPoint(close?new Vector3(0,.72f,0):new Vector3(0,0,-.3f)));
        camera.fieldOfView=close?34:30;
    }
    void CaptureField()
    {
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            fieldSurfaces.Add(new FieldSurface{renderer=renderer,original=block.isEmpty?renderer.sharedMaterial.color:block.GetColor("_Color")});
        }
    }
    public void SetField(int id)
    {
        id=Mathf.Clamp(id,0,2);if(fieldId==id)return;fieldId=id;
        System.Array.Clear(tileTintValid,0,tileTintValid.Length);System.Array.Clear(seatTintValid,0,seatTintValid.Length);
        Color accent=TamerLoadout.FieldColor(id);
        foreach(var s in fieldSurfaces)
        {
            string name=s.renderer.name;Color c=s.original;
            if(id>0)
            {
                bool floor=name.StartsWith("Turf")||name.Contains("playing surface");
                c=floor?(id==1?new Color(.21f,.32f,.40f):new Color(.17f,.12f,.27f)):Color.Lerp(s.original,accent,.25f);
                if(name.Contains("channel")||name.Contains("lantern")||name.Contains("inlay"))c=accent;
            }
            Tint(s.renderer,c);
        }
        camera.backgroundColor=id==1?new Color(.035f,.075f,.12f):id==2?new Color(.045f,.025f,.085f):new Color(.028f,.055f,.075f);
        if(presentationQuality){ApplyFieldQuality(id);return;}
        for(int i=1;i<3;i++)if(fieldDecor[i]!=null)fieldDecor[i].SetActive(i==id);
        if(id==0||fieldDecor[id]!=null)return;
        var parent=new GameObject("Field ornaments "+id){layer=Layer,hideFlags=HideFlags.HideAndDontSave};parent.transform.SetParent(root.transform,false);fieldDecor[id]=parent;
        var material=Material(accent);
        for(int side=-1;side<=1;side+=2)for(int i=0;i<4;i++)
        {
            var ornament=Shape(id==1?"Ice spire":"Data obelisk",id==1?hex:cube,material,new Vector3(side*6.05f,.65f,3.6f-i*2.2f),new Vector3(.17f,.9f+i%2*.3f,.17f),parent.transform);
            ornament.transform.localRotation=Quaternion.Euler(0,45,id==1?side*14:0);
            if(id==2)Shape("Orbit seal",ringMesh,glow,new Vector3(side*6.05f,1.1f,3.6f-i*2.2f),Vector3.one*.3f,parent.transform);
        }
    }
    public void SetTamer(object key,int choice,Vector3 point,Vector3 destination,float movement,float time,float celebration=0)
    {
        choice=Mathf.Clamp(choice,0,TamerLoadout.Tamers.Length-1);Color color=choice==1?new Color(1,.5f,.7f):choice==2?new Color(1,.85f,.43f):new Color(.4f,.84f,1);
        SetTactician(key,point,destination,TamerLoadout.Portrait(choice),color,movement,(destination.x-point.x)*100,time,celebration);
        var actor=actors[key];
        if(choice>0)
        {
            string id=TamerLoadout.Models[choice];
            if(actor.tamerMovement==null)actor.tamerMovement=new WorldMotionTracker<int>();
            actor.tamerMovement.EnsureContext(actor,choice);
            var travel=actor.tamerMovement.At(0,point.x,point.z,time);
            if(travel.speed>.025f)FaceActor(key,new Vector3(travel.x,0,travel.z));
            else if(actor.faithful==null||actor.faithful.Data.id!=id)
            {
                Vector3 direction=destination-point;
                FaceActor(key,direction.sqrMagnitude>.005f?direction:Vector3.back);
            }
            // Tactician roots retain their existing 1.08 scale. Sample at the final
            // .72 model height, with velocity expressed in that parent's units.
            float localSpeed=travel.speed/Mathf.Max(.001f,actor.root.transform.localScale.x);
            if(!PoseModel(actor,id,localSpeed,time,DigimonSkillCatalog.Find(id),-1,0,0,1,TamerModelHeight))
                PoseDigimon(key,id,localSpeed,travel.x,time);
        }
        else if(actor.tamerMovement!=null)actor.tamerMovement.EnsureContext(actor,0);
    }
}
