using UnityEngine;

public sealed partial class TacticalArena
{
    public bool BasicEmitter(object key,out Vector3 point,bool atRelease=true)
    {
        Actor actor;point=Vector3.zero;if(!actors.TryGetValue(key,out actor)||actor.generation!=generation)return false;
        point=actor.faithful!=null?root.transform.InverseTransformPoint(actor.faithful.BasicEmitter(atRelease)):actor.root.transform.localPosition+Vector3.up*.6f;
        return true;
    }
    // Uses the same bounded mesh pool as skills. Launch positions are fixed once per attack.
    public void DrawBasicAttack(object key,string id,Vector3 target,float age,int serial,int star=1,float targetSurface=0)
    {
        if(age<0||age>=BasicAttackPresentation.Lifetime(id))return;
        Actor actor;if(!actors.TryGetValue(key,out actor)||actor.generation!=generation)return;
        if(actor.basicSerial!=serial||age<actor.basicAge-.0001f||actor.basicGeneration<generation-1)
        {
            actor.basicFrom=actor.faithful!=null?root.transform.InverseTransformPoint(actor.faithful.BasicEmitter(true)):actor.root.transform.localPosition+Vector3.up*.6f;
            actor.basicTo=target;actor.basicSerial=serial;actor.basicSurface=targetSurface;
        }
        actor.basicAge=age;actor.basicGeneration=generation;
        DrawBasicShape(id,actor.basicFrom,actor.basicTo,age,serial,star,actor.basicSurface);
    }
    void BasicLine(Vector3 a,Vector3 b,float width,Color color)
    {
        Vector3 delta=b-a;if(delta.sqrMagnitude<.00001f)return;
        Effect(cube,(a+b)*.5f,new Vector3(width,width,delta.magnitude),color,Quaternion.LookRotation(delta),EnsureSkillFinish()?transparentSkillMaterial:null);
    }
    void BasicArc(Vector3 center,Vector3 along,float radius,float progress,Color color,int teeth)
    {
        if(EnsureSkillFinish())
        {
            Color edge=Color.Lerp(color,Color.white,.3f);edge.a=color.a;
            for(int tooth=0;tooth<teeth;tooth++)
                CombatSweep(center+Side(along)*(tooth-(teeth-1)*.5f)*.055f,along,radius*(1-tooth*.07f),-65+progress*130+tooth*8,edge,.85f);
            return;
        }
        Vector3 side=Side(along),up=Vector3.up;
        for(int tooth=0;tooth<teeth;tooth++)for(int j=0;j<7;j++)
        {
            float a=(-1.1f+progress*.7f)+j*.25f,b=a+.25f;
            Vector3 offset=along*(tooth-(teeth-1)*.5f)*.07f;
            Vector3 p=center+offset+(side*Mathf.Sin(a)+up*Mathf.Cos(a))*radius;
            Vector3 q=center+offset+(side*Mathf.Sin(b)+up*Mathf.Cos(b))*radius;
            BasicLine(p,q,.013f+tooth*.002f,color);
        }
    }
    public void DrawBasicShape(string id,Vector3 start,Vector3 end,float age,int serial,int star=1,float targetSurface=0)
    {
        if(age<0||age>=BasicAttackPresentation.Lifetime(id))return;
        EnsureSkillFinish();var kind=BasicAttackPresentation.For(id);bool contact=BasicAttackPresentation.Contact(kind);
        var skill=DigimonSkillCatalog.Find(id);Color color=skill==null?Color.white:skill.Tint;
        float travel=BasicAttackPresentation.Travel(id),t=travel>0?Mathf.Clamp01(age/travel):1;
        float size=Mathf.Clamp(DigimonVisualScale.Height(id,star)*.14f,.09f,.24f);
        Vector3 direction=(end-start).normalized;if(direction.sqrMagnitude<.001f)direction=Vector3.forward;
        Vector3 side=Side(direction),p=Vector3.Lerp(start,end,t);
        if(contact)
        {
            Vector3 contactPoint=ContactPoint(end,Mathf.Max(targetSurface,Mathf.Max(.28f,size*1.4f)));
            float u=age/.19f;color.a=(1-u)*(1-u)*.85f;
            if(DrawContactFinish(kind,id,start,end,contactPoint,direction,u,size,color,serial)){}
            else if(kind==BasicAttackPresentation.Kind.Whip)
            {
                Vector3 last=start;
                for(int i=1;i<=10;i++)
                {
                    float f=i/10f;Vector3 next=Vector3.Lerp(start,end,f)+side*Mathf.Sin(f*Mathf.PI)*Mathf.Sin(u*Mathf.PI*2)*(id=="devimon"?.13f:.32f);
                    BasicLine(last,next,.027f*(1-f*.65f),color);last=next;
                }
            }
            else if(kind==BasicAttackPresentation.Kind.Claw||kind==BasicAttackPresentation.Kind.Sword)
                BasicArc(contactPoint-direction*.08f,direction,size*1.8f,u,color,kind==BasicAttackPresentation.Kind.Claw?3:1);
            else if(kind==BasicAttackPresentation.Kind.Bite)
            {
                for(int jaw=-1;jaw<=1;jaw+=2)for(int i=-1;i<=1;i++)
                {
                    Vector3 a=contactPoint+side*i*size*.75f+Vector3.up*jaw*size*(1-u);
                    BasicLine(a,a-Vector3.up*jaw*size*.55f,.025f,color);
                }
            }
            else
            {
                float radius=size*(.5f+u*1.6f);Halo(contactPoint,radius,color,true);
                if(kind==BasicAttackPresentation.Kind.Horn)BasicLine(contactPoint-direction*size*1.8f,contactPoint+direction*size*.3f,.038f,color);
            }
        }
        else if(age<travel)
        {
            Quaternion along=Quaternion.FromToRotation(Vector3.up,direction);
            bool ribbon=DrawRibbonBasic(kind,start,p,direction,age,size,color);
            if(EnsureSkillFinish())
            {
                if(!ribbon&&kind!=BasicAttackPresentation.Kind.Electric&&kind!=BasicAttackPresentation.Kind.Seed)
                    CombatTrail(p,direction,Mathf.Min((p-start).magnitude,.40f),size*.3f,color);
            }
            if(!ribbon)switch(kind)
            {
                case BasicAttackPresentation.Kind.Bubble:
                    for(int i=0;i<3;i++)Halo(p-direction*i*.10f+side*(i-1)*.07f,size*(.45f+i*.12f),color,true);
                    break;
                case BasicAttackPresentation.Kind.Electric:
                    for(int i=0;i<6;i++)
                    {
                        Vector3 a=Vector3.Lerp(start,p,i/6f)+side*Mathf.Sin(i*2.4f+serial)*.10f;
                        Vector3 b=Vector3.Lerp(start,p,(i+1)/6f)+side*Mathf.Sin((i+1)*2.4f+serial)*.10f;
                        BasicLine(a,b,.023f,color);
                    }
                    break;
                case BasicAttackPresentation.Kind.Missile:
                    Effect(hex,p,new Vector3(.045f,.19f,.045f),new Color(.55f,.65f,.70f),along,stone);
                    Effect(SkillTip(),p+direction*.095f,new Vector3(.05f,.09f,.05f),Color.white,along,stone);
                    Effect(cube,p-direction*.06f,new Vector3(.16f,.012f,.07f),color,Quaternion.LookRotation(direction),stone);
                    SoftSkillGlow(p-direction*.14f,.1f,new Color(1,.5f,.12f),.8f);break;
                case BasicAttackPresentation.Kind.Ice:
                    for(int i=-1;i<=1;i++)Effect(SkillTip(),p+side*i*.07f-direction*Mathf.Abs(i)*.06f,new Vector3(.035f,.23f,.035f),Color.Lerp(color,Color.white,.5f),along,stone);
                    break;
                case BasicAttackPresentation.Kind.Feather:
                    BasicLine(p-direction*.19f,p+direction*.13f,.017f,color);
                    for(int i=0;i<4;i++)for(int s=-1;s<=1;s+=2)
                        BasicLine(p-direction*(i*.055f),p-direction*(i*.055f+.08f)+side*s*(.07f-i*.01f),.022f,color);
                    SoftSkillGlow(p,.1f,color,.3f);break;
                case BasicAttackPresentation.Kind.Fire:
                    Effect(SkillTip(),p,new Vector3(size*.5f,size*2,size*.5f),color,along,EnsureSkillFinish()?transparentSkillMaterial:null);
                    for(int i=0;i<4;i++)SoftSkillGlow(p-direction*i*.07f+Vector3.up*Mathf.Sin(age*35+i)*.025f,size*(1-i*.17f),Color.Lerp(color,Color.white,(3-i)*.12f),.5f);
                    break;
                case BasicAttackPresentation.Kind.Seed:
                    Effect(hex,p+Vector3.up*Mathf.Sin(t*Mathf.PI)*.18f,new Vector3(.045f,.065f,.045f),new Color(.52f,.8f,.26f),along,stone);break;
                case BasicAttackPresentation.Kind.Pollen:
                    for(int i=0;i<5;i++)SoftSkillGlow(p+side*Mathf.Sin(i*2.3f)*.13f+Vector3.up*Mathf.Cos(i*2.3f)*.1f,.035f,color,.7f);break;
                case BasicAttackPresentation.Kind.Flower:
                    for(int i=0;i<5;i++)
                    {float a=i*Mathf.PI*.4f+age*12;Vector3 petal=side*Mathf.Cos(a)+Vector3.up*Mathf.Sin(a);Effect(SkillTip(),p+petal*.055f,new Vector3(.04f,.12f,.028f),color,Quaternion.FromToRotation(Vector3.up,petal),stone);}
                    break;
                case BasicAttackPresentation.Kind.Music:
                    Halo(p,size,color,true);BasicLine(p,p+Vector3.up*.21f,.025f,color);BasicLine(p+Vector3.up*.21f,p+Vector3.up*.17f+side*.12f,.025f,color);break;
                case BasicAttackPresentation.Kind.Holy:
                    BasicLine(p-direction*.22f,p+direction*.13f,.03f,color);BasicLine(p-side*.09f,p+side*.09f,.025f,Color.white);SoftSkillGlow(p,.11f,color,.5f);break;
                case BasicAttackPresentation.Kind.Water:
                    for(int i=0;i<4;i++)BasicLine(p-direction*i*.09f,p-direction*(i*.09f+.07f),.05f-i*.009f,color);break;
                default:
                    for(int i=0;i<3;i++)Halo(p-direction*i*.12f,size*(.65f+i*.24f),color,true);break;
            }
        }
        if(!contact&&age<travel)CombatLaunch(CombatEffectStyle.Basic(kind,id),start,direction,age,size*.85f,color,false);
        float impactAge=age-travel;
        if(impactAge>=0)
        {
            if(EnsureSkillFinish())
            {
                CombatImpact(CombatEffectStyle.Basic(kind,id),end,direction,impactAge/.19f,size,serial,color,false,targetSurface);return;
            }
            float fade=1-impactAge/.19f;Color spark=Color.Lerp(color,Color.white,.55f);spark.a=fade*fade;
            SoftSkillGlow(end,size*.9f,spark,.5f*fade);
            for(int i=0;i<4;i++)
            {float a=i*Mathf.PI*.5f+serial*.7f;Vector3 d=side*Mathf.Cos(a)+Vector3.up*Mathf.Sin(a);BasicLine(end+d*(size*.2f+impactAge*.5f),end+d*(size*.55f+impactAge*.7f),.016f,spark);}
        }
    }
}
