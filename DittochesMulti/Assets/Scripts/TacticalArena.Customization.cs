using System.Collections.Generic;
using UnityEngine;

public sealed partial class TacticalArena
{
    struct FieldSurface {public MeshRenderer renderer;public Color original;}
    readonly List<FieldSurface> fieldSurfaces=new List<FieldSurface>();
    readonly GameObject[] fieldDecor=new GameObject[3];
    int fieldId=-1;
    public int FieldId {get{return Mathf.Max(0,fieldId);}}
    public void SetLoadoutCamera(bool close)
    {
        camera.transform.localPosition=close?new Vector3(0,2.2f,-4.4f):new Vector3(0,12.8f,-14.2f);
        camera.transform.LookAt(root.transform.TransformPoint(close?new Vector3(0,.45f,0):new Vector3(0,0,-.3f)));
        camera.fieldOfView=close?28:30;
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
        choice=Mathf.Clamp(choice,0,3);Color color=choice==1?new Color(1,.5f,.7f):choice==2?new Color(1,.85f,.43f):new Color(.4f,.84f,1);
        SetTactician(key,point,destination,TamerLoadout.Portrait(choice),color,movement,(destination.x-point.x)*100,time,celebration);
        if(choice>0)
        {
            Vector3 direction=destination-point;
            if(direction.sqrMagnitude>.005f)FaceActor(key,direction);
            else if(actors[key].faithful==null)FaceActor(key,Vector3.back);
            PoseDigimon(key,TamerLoadout.Models[choice],movement,0,time);
            var actor=actors[key];if(actor.faithful!=null)actor.faithful.SetSize(.72f);
        }
    }
    public void DrawFinisher(int choice,Vector3 source,Vector3 destination,float age)
    {
        if(age<0||age>2.2f)return;choice=Mathf.Clamp(choice,0,2);Color c=TamerLoadout.FinishColor(choice);
        float launch=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.65f));Vector3 p=Vector3.Lerp(source+Vector3.up*.55f,destination+Vector3.up*.6f,launch);
        p+=Vector3.up*Mathf.Sin(launch*Mathf.PI)*(choice==1?2:1);
        if(age<.65f){Orb(p,.17f+choice*.07f,c);Halo(source+Vector3.up*.06f,.45f+age,c);return;}
        float t=Mathf.Clamp01((age-.65f)/1.55f),fade=1-t;Vector3 impact=destination+Vector3.up*.08f;
        if(choice==0)
        {
            for(int i=0;i<3;i++)Halo(impact+Vector3.up*i*.12f,.2f+t*2+i*.17f,c*fade);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Vector3 ray=new Vector3(Mathf.Cos(a),.35f,Mathf.Sin(a));Stroke(impact+ray*t,impact+ray*(t+.45f)*1.9f,.045f*fade,c);}
        }
        else if(choice==1)
        {
            Orb(impact+Vector3.up*.65f,(.35f+Mathf.Sin(t*Mathf.PI)*.7f)*fade,c);
            Halo(impact,.3f+t*2.4f,c*fade);
            for(int i=0;i<10;i++){float a=i*Mathf.PI/5;Vector3 v=new Vector3(Mathf.Cos(a)*(t*1.9f+.2f),Mathf.Sin(t*Mathf.PI)*1.4f,Mathf.Sin(a)*(t*1.9f+.2f));Effect(cube,impact+v,Vector3.one*.12f*fade,c,Quaternion.Euler(i*31,t*180,45));}
        }
        else
        {
            Stroke(impact,impact+Vector3.up*4,.18f*fade,c);Halo(impact,.3f+t*1.4f,c*fade);
            for(int i=0;i<3;i++)Halo(impact+Vector3.up*(.35f+i*.75f),(.8f-t*.5f)*fade,c*fade);
            for(int i=-1;i<=1;i+=2){Stroke(impact+Vector3.up*.5f+Vector3.right*i*.75f,impact+Vector3.up*2.8f+Vector3.right*i*.75f,.09f*fade,c);}
        }
    }
}
