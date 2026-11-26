#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.IO;
using UnityEngine;

public static class TamerMotionValidation
{
    public static void Validate(Action<bool,string> require)
    {
        int checks=0;Action<bool,string> counted=(ok,message)=>{require(ok,message);checks++;};
        using(var review=new TacticalArena(new Rect(0,0,1280,720)))review.ValidateTamerMotion(counted);
        Debug.Log("TAMER MOTION COMPLETE: "+checks+" checks / actual SetTamer / "+(TamerLoadout.Tamers.Length-1)+" models / both seats / frame rates / final display scale");
    }
    public static System.Collections.IEnumerator CaptureReview()
    {
        using(var review=new TacticalArena(new Rect(0,0,1280,720)))
            yield return review.CaptureTamerMotionReview();
    }
}

public sealed partial class TacticalArena
{
    static bool SameTamerPhase(float a,float b)
    {return Mathf.Abs(Mathf.DeltaAngle(a*360,b*360))<.05f;}
    void ValidateTamerScale(Actor actor,Action<bool,string> require,string context)
    {
        float height=actor.faithful.Data.restBounds.size.y*actor.faithful.transform.localScale.y;
        require(Mathf.Abs(height-.72f)<.0001f,"tamer samples at its final model height / "+context);
        require(Mathf.Abs(height*actor.root.transform.localScale.y-.7776f)<.0001f,"tamer retains the existing displayed world height / "+context);
        require((actor.root.transform.localScale-Vector3.one*1.08f).sqrMagnitude<.000001f,"tactician parent scale stays intact / "+context);
    }
    public void ValidateTamerMotion(Action<bool,string> require)
    {
        SetField(0);
        for(int choice=1;choice<TamerLoadout.Tamers.Length;choice++)foreach(int side in new[]{-1,1})foreach(int fps in new[]{30,60,144})
        {
            object key=new object();string context=TamerLoadout.Models[choice]+" / seat "+side+" / "+fps;
            Vector3 start=new Vector3(side*1.5f,.19f,0),velocity=new Vector3(side*.6f,0,.45f);
            // Deliberately aim the destination opposite travel: the body must follow
            // the displayed curve/position, not the old target or normalized input.
            Vector3 destination=start-velocity*4;
            BeginFrame(-1,-1,-1,false);SetTamer(key,choice,start,destination,1,0);
            var actor=actors[key];var model=actor.faithful;var tracker=actor.tamerMovement;
            require(model!=null,"tamer has its faithful model / "+context);
            ValidateTamerScale(actor,require,context);
            require((actor.root.transform.localPosition-start-Vector3.up*.10f).sqrMagnitude<.000001f,"tamer preserves its displayed anchor offset / "+context);
            float expected=0;Vector3 previous=start;
            for(int frame=1;frame<=fps;frame++)
            {
                float time=frame/(float)fps;Vector3 point=start+velocity*time;
                BeginFrame(-1,-1,-1,false);SetTamer(key,choice,point,destination,frame%2==0?0:5,time);
                expected=Mathf.Repeat(expected+Vector3.Distance(previous,point)/(.72f*1.08f*.42f),1);previous=point;
                require(SameTamerPhase(model.LocomotionPhase,expected),"tamer cadence follows world distance at final size, independent of legacy movement / "+context);
                require(model.Motion=="Walk"||model.Motion=="Run","displayed movement drives tamer gait / "+context);
                require(ReferenceEquals(actor.faithful,model)&&ReferenceEquals(actor.tamerMovement,tracker),"steady tamer frames reuse model and movement history / "+context);
                if(frame==fps/2)
                {
                    var positions=new Vector3[model.Nodes.Length];var rotations=new Quaternion[model.Nodes.Length];
                    for(int n=0;n<positions.Length;n++){positions[n]=model.Nodes[n].localPosition;rotations[n]=model.Nodes[n].localRotation;}
                    float phase=model.LocomotionPhase;Quaternion facing=model.transform.localRotation;
                    SetTamer(key,choice,point,destination,1,time);
                    require(model.LocomotionPhase==phase&&Quaternion.Angle(facing,model.transform.localRotation)<.01f,"duplicate tamer repaint cannot advance stride or turn / "+context);
                    for(int n=0;n<positions.Length;n++)
                        require((positions[n]-model.Nodes[n].localPosition).sqrMagnitude<.000001f&&Quaternion.Angle(rotations[n],model.Nodes[n].localRotation)<.09f,"duplicate tamer repaint preserves displayed skeleton / "+context+" / "+n);
                }
            }
            require(Vector3.Angle(-model.transform.forward,velocity)<1,"tamer turns along actual travel rather than the opposite destination / "+context);
            float held=model.LocomotionPhase;Quaternion stopped=model.transform.localRotation;
            for(int frame=1;frame<=fps/2;frame++)
            {
                SetTamer(key,choice,previous,destination,1,1+frame/(float)fps);
                require(model.Motion=="Idle"&&model.LocomotionPhase==held,"stationary tamer settles even if legacy movement stays positive / "+context);
                require(Quaternion.Angle(stopped,model.transform.localRotation)<.02f,"stopped tamer retains its last actual facing / "+context);
            }
            ValidateTamerScale(actor,require,context);
            Vector3 jump=previous+Vector3.right*5;
            SetTamer(key,choice,jump,destination,1,1.6f);
            require(model.Motion=="Idle"&&model.LocomotionPhase==held,"teleport does not create a false tamer stride / "+context);
            SetTamer(key,choice,jump+velocity*.02f,destination,0,1.62f);
            require(SameTamerPhase(model.LocomotionPhase,held+velocity.magnitude*.02f/(.72f*1.08f*.42f)),"tamer resumes measured travel immediately after teleport / "+context);
            held=model.LocomotionPhase;
            SetTamer(key,choice,jump+velocity*.04f,destination,1,1.62f);
            require(model.Motion=="Idle"&&model.LocomotionPhase==held,"same-time corrected position rebases tamer movement / "+context);
            SetTamer(key,choice,start,destination,1,.25f);
            require(model.Motion=="Idle"&&model.LocomotionPhase==0,"backward time clears tamer stride / "+context);
            SetTamer(key,choice,start+velocity*.3f,destination,1,.7f);
            require(model.Motion=="Idle"&&model.LocomotionPhase==0,"long presentation gap does not become a tamer run / "+context);
            // Quickly switching through 2D must not retain travel measured before it.
            SetTamer(key,0,start,destination,1,.71f);
            SetTamer(key,choice,start+velocity*.1f,destination,1,.72f);
            require(model.Motion=="Idle"&&model.LocomotionPhase==0,"2D-to-same-3D switch establishes a fresh tamer origin / "+context);
            int changed=choice%3+1;SetTamer(key,changed,start+velocity*.15f,destination,1,.74f);
            require(actor.faithful.Data.id==TamerLoadout.Models[changed]&&actor.faithful.Motion=="Idle"&&actor.faithful.LocomotionPhase==0,"appearance change cannot inherit previous tamer speed / "+context);
            ValidateTamerScale(actor,require,context+" changed");
            Release(actor.root);actors.Remove(key);
            SetTamer(key,choice,start,destination,1,.75f);
            require(!ReferenceEquals(actor,actors[key])&&!ReferenceEquals(tracker,actors[key].tamerMovement)&&actors[key].faithful.Motion=="Idle","removed tamer actor releases its own movement history / "+context);
            Release(actors[key].root);actors.Remove(key);
        }
        ValidateTamerSmoothedPaths(require);
        ValidateTamerLegacyPortrait(require);
        ValidateTamerCombatScale(require);
    }
    void ValidateTamerSmoothedPaths(Action<bool,string> require)
    {
        foreach(int choice in new[]{1,2,3})foreach(float maximumSpeed in new[]{3f,4f})
        {
            object key=new object();Vector3 point=new Vector3(-1,.19f,.3f),destination=new Vector3(1,.19f,-.3f),velocity=Vector3.zero;
            float time=10,phase=0;SetTamer(key,choice,point,destination,0,time);var model=actors[key].faithful;
            for(int frame=0;frame<32;frame++)
            {
                // The settings and online caller both use this SmoothDamp, with
                // different maximum speeds. Uneven render intervals must still
                // match the visible displacement, including deceleration.
                float dt=new[]{.011f,.029f,.067f,.097f}[frame%4];Vector3 previous=point;
                point=Vector3.SmoothDamp(point,destination,ref velocity,.22f,maximumSpeed,dt);time+=dt;
                float worldSpeed=Vector3.Distance(previous,point)/(time-(time-dt));
                if(worldSpeed/1.08f>.025f)phase=Mathf.Repeat(phase+Vector3.Distance(previous,point)/(.72f*1.08f*.42f),1);
                SetTamer(key,choice,point,destination,Mathf.Clamp01(velocity.magnitude),time);
                require(SameTamerPhase(model.LocomotionPhase,phase),"smoothed settings/online path follows actual distance through irregular render times / "+choice+" / "+maximumSpeed);
            }
            Release(actors[key].root);actors.Remove(key);
        }
    }
    void ValidateTamerLegacyPortrait(Action<bool,string> require)
    {
        object actualKey=new object(),referenceKey=new object();Vector3 point=new Vector3(1,.19f,-1),destination=new Vector3(-1,.19f,2);
        foreach(float movement in new[]{0f,.45f,2f})foreach(float time in new[]{0f,.13f,1.2f})
        {
            SetTamer(actualKey,0,point,destination,movement,time,.4f);
            SetTactician(referenceKey,point,destination,TamerLoadout.Portrait(0),new Color(.4f,.84f,1),movement,(destination.x-point.x)*100,time,.4f);
            var actual=actors[actualKey];var reference=actors[referenceKey];
            require(actual.faithful==null&&actual.tamerMovement==null,"Bitmon retains its 2D presentation without a motion tracker");
            require((actual.portrait.transform.localPosition-reference.portrait.transform.localPosition).sqrMagnitude<.000001f&&
                (actual.portrait.transform.localScale-reference.portrait.transform.localScale).sqrMagnitude<.000001f&&
                Quaternion.Angle(actual.portrait.transform.localRotation,reference.portrait.transform.localRotation)<.01f,"Bitmon bounce, stretch and turn inputs remain unchanged");
            require((actual.contactShadow.transform.localScale-reference.contactShadow.transform.localScale).sqrMagnitude<.000001f&&
                (actual.halo.transform.localScale-reference.halo.transform.localScale).sqrMagnitude<.000001f,"Bitmon shadow and halo remain unchanged");
        }
        Release(actors[actualKey].root);Release(actors[referenceKey].root);actors.Remove(actualKey);actors.Remove(referenceKey);
    }
    void ValidateTamerCombatScale(Action<bool,string> require)
    {
        foreach(string id in new[]{"koromon","agumon","wargreymon"})
        {
            object key=new object();SetActor(key,new Vector3(0,.19f,0),null,Color.cyan);
            PoseDigimon(key,id,.75f,0,0);var model=actors[key].faithful;
            PoseDigimon(key,id,.75f,0,.05f);float height=DigimonVisualScale.Height(id);
            require(Mathf.Abs(model.Data.restBounds.size.y*model.transform.localScale.y-height)<.0001f,"normal combat model keeps its original evolution height / "+id);
            require(SameTamerPhase(model.LocomotionPhase,.75f*.05f/Mathf.Max(.15f,height*.42f)),"normal combat cadence does not inherit the tamer height override / "+id);
            Release(actors[key].root);actors.Remove(key);
        }
    }
    public System.Collections.IEnumerator CaptureTamerMotionReview()
    {
        string folder=Path.Combine(Application.dataPath,"../TamerMotionCaptures");Directory.CreateDirectory(folder);
        SetField(0);camera.transform.localPosition=new Vector3(0,4.8f,-7.8f);camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,.35f,0)));camera.fieldOfView=36;
        var picture=new Texture2D(Texture.width,Texture.height,TextureFormat.RGB24,false);
        try
        {
            for(int frame=0;frame<150;frame++)
            {
                float time=frame/30f,progress=Mathf.Clamp01(time/3.5f);progress=progress*progress*(3-2*progress);
                BeginFrame(-1,-1,-1,false);
                for(int choice=0;choice<4;choice++)
                {
                    float x=(choice-1.5f)*1.45f,side=choice%2==0?1:-1;Vector3 origin=new Vector3(x,.19f,-side*.6f);
                    // Parallel curved paths fit the retained .7776 world height;
                    // final destination differs from the tangent to expose stop snaps.
                    Vector3 point=origin+new Vector3(.35f*Mathf.Sin(progress*Mathf.PI),0,side*progress*1.2f);
                    SetTamer(choice,choice,point,origin+new Vector3(-.4f,0,side*1.35f),progress<1?.65f:0,time);
                }
                yield return null;Render();var old=RenderTexture.active;
                try{RenderTexture.active=Texture;picture.ReadPixels(new Rect(0,0,picture.width,picture.height),0,0);picture.Apply();File.WriteAllBytes(Path.Combine(folder,frame.ToString("D4")+".png"),picture.EncodeToPNG());}
                finally{RenderTexture.active=old;}
            }
        }
        finally{UnityEngine.Object.Destroy(picture);}
    }
}
#endif
