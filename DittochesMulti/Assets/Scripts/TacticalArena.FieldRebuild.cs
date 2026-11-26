using UnityEngine;

public sealed partial class TacticalArena
{
    void BuildRenewedField(int id,Transform p,Material stoneSurface,Material metal,Material accent,Material light)
    {
        // All scenery remains outside the shared 56-cell board and reserve deck.
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<7;i++)
            {
                float z=-4.4f+i*1.52f;
                Shape("Carved perimeter foundation",hex,stoneSurface,new Vector3(side*5.72f,.20f,z),new Vector3(.58f,.30f,.81f),p);
                Shape("Perimeter brass seam",FieldBlock(),metal,new Vector3(side*5.33f,.35f,z),new Vector3(.04f,.06f,1.30f),p);
                if(id==0)
                {
                    Shape("Jade water basin",hex,metal,new Vector3(side*5.86f,.36f,z),new Vector3(.39f,.07f,.58f),p);
                    Shape("Basin water",BasinWater(),light,new Vector3(side*5.86f,.41f,z),new Vector3(.30f,.013f,.48f),p);
                    for(int leaf=0;leaf<3;leaf++)
                    {
                        var petal=Shape("Broad sanctuary leaf",SkillTip(),accent,new Vector3(side*(6.13f+leaf*.1f),.35f,z+.15f),new Vector3(.23f,.60f+leaf*.13f,.14f),p);
                        petal.transform.localRotation=Quaternion.Euler(22+leaf*25,i*39,side*(25+leaf*14));
                    }
                }
                else if(id==1)
                {
                    Shape("Snow cornice",hex,accent,new Vector3(side*5.88f,.40f,z),new Vector3(.49f,.06f,.75f),p);
                    for(int shard=0;shard<3;shard++)
                    {
                        var crystal=Shape("Glacial fracture",GlacierCrystal(),shard==1?light:accent,new Vector3(side*(6.08f+shard*.16f),.24f,z+.2f*shard),new Vector3(.21f,1.1f+shard*.25f,.26f),p);
                        crystal.transform.localRotation=Quaternion.Euler(8,i*27,side*(9+shard*12));
                    }
                }
                else
                {
                    Shape("Reactor rail",FieldBlock(),metal,new Vector3(side*5.82f,.43f,z),new Vector3(.30f,.10f,1.16f),p);
                    Shape("Rail plasma conduit",FieldBlock(),light,new Vector3(side*5.64f,.47f,z),new Vector3(.06f,.07f,.88f),p);
                    Shape("Reactor cooling fin",FieldBlock(),stoneSurface,new Vector3(side*6.18f,.62f,z),new Vector3(.36f,.86f,.31f),p);
                    for(int fin=0;fin<3;fin++)Shape("Cooling copper rib",FieldBlock(),metal,new Vector3(side*6.18f,.48f+fin*.19f,z),new Vector3(.52f,.035f,.41f),p);
                }
            }
            for(int i=0;i<3;i++)
            {
                float z=-3.6f+i*3.4f;
                var island=Shape("Detached themed island",hex,stoneSurface,new Vector3(side*7.15f,-.53f,z),new Vector3(1.02f,.48f,1.1f),p);
                island.transform.localRotation=Quaternion.Euler(0,i*21,side*5);
                var rootRock=Shape("Suspended island keel",SkillTip(),stoneSurface,new Vector3(side*7.15f,-.64f,z),new Vector3(.8f,1.6f,.85f),p);rootRock.transform.localRotation=Quaternion.Euler(0,i*35,180);
                if(id==0)
                {
                    Shape("Garden crown",hex,accent,new Vector3(side*7.1f,-.26f,z),new Vector3(.87f,.10f,.95f),p);
                    for(int j=0;j<5;j++)Shape("Cascading water ribbon",FieldBlock(),light,new Vector3(side*(7.0f+j*.085f),-1.05f,z-.7f),new Vector3(.04f,1.6f-j*.09f,.035f),p);
                }
                else if(id==1)
                {
                    var glacier=Shape("Distant glacier pinnacle",GlacierCrystal(),accent,new Vector3(side*7.1f,-.25f,z),new Vector3(.65f,2.1f,.70f),p);glacier.transform.localRotation=Quaternion.Euler(0,i*43,side*13);
                }
                else
                {
                    var crystal=Shape("Suspended energy prism",FieldBlock(),light,new Vector3(side*7.1f,.4f,z),Vector3.one*.34f,p);crystal.transform.localRotation=Quaternion.Euler(35,45,35);
                    Shape("Energy satellite ring",ringMesh,metal,new Vector3(side*7.1f,.1f,z),Vector3.one*.65f,p);
                }
            }
        }
        var landmark=new GameObject("Far landmark framing");landmark.transform.SetParent(p,false);
        landmark.transform.localPosition=new Vector3(0,0,-.32f);landmark.transform.localScale=new Vector3(1,.72f,1);
        if(id==0)BuildSanctuaryGate(landmark.transform,stoneSurface,metal,accent,light);
        else if(id==1)BuildGlacierCrown(landmark.transform,stoneSurface,accent,light);
        else BuildReactorGate(landmark.transform,stoneSurface,metal,light);
    }
    void BuildSanctuaryGate(Transform p,Material stoneSurface,Material metal,Material leaves,Material water)
    {
        for(int i=0;i<11;i++)
        {
            float a=i*Mathf.PI/10;Vector3 position=new Vector3(Mathf.Cos(a)*2.45f,.40f+Mathf.Sin(a)*1.65f,6.55f);
            var arch=Shape("Sanctuary arch segment",FieldBlock(),stoneSurface,position,new Vector3(.71f,.35f,.64f),p);arch.transform.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg-90);
            var trimPart=Shape("Sanctuary arch engraving",FieldBlock(),metal,position+Vector3.back*.33f,new Vector3(.51f,.032f,.022f),p);trimPart.transform.localRotation=arch.transform.localRotation;
        }
        for(int side=-1;side<=1;side+=2)
        {
            Shape("Shrine stepped pedestal",hex,stoneSurface,new Vector3(side*2.38f,.25f,6.48f),new Vector3(.76f,.42f,.67f),p);
            for(int i=0;i<4;i++)
            {
                var branch=Shape("Ancient root",hex,stoneSurface,new Vector3(side*(6.9f+i*.05f),.17f+i*.30f,3.7f),new Vector3(.21f-i*.026f,.5f,.23f),p);branch.transform.localRotation=Quaternion.Euler(0,17*i,side*(12-i*9));
                Shape("Canopy cluster",Canopy(),leaves,new Vector3(side*(6.65f+i*.38f),1.18f+(i%2)*.2f,3.7f+(i%2)*.4f),new Vector3(.8f,.25f,.72f),p);
            }
            Shape("Gate basin",BasinWater(),water,new Vector3(side*3.65f,.15f,6.20f),new Vector3(.68f,.025f,.51f),p);
        }
        var emblem=Shape("Sanctuary crest",ringMesh,water,new Vector3(0,1.1f,6.53f),Vector3.one*.65f,p);emblem.transform.localRotation=Quaternion.Euler(90,0,0);
    }
    void BuildGlacierCrown(Transform p,Material stoneSurface,Material ice,Material light)
    {
        Shape("Glacier bridge",hex,stoneSurface,new Vector3(0,.25f,6.43f),new Vector3(3.3f,.25f,.74f),p);
        for(int i=0;i<13;i++)
        {
            float x=(i-6)*.48f;float height=.9f+Mathf.Abs(i-6)*.12f+(i%3)*.23f;
            var shard=Shape("Glacier crown tooth",GlacierCrystal(),i%3==0?light:ice,new Vector3(x,.31f,6.64f+Mathf.Abs(i-6)*.07f),new Vector3(.30f,height,.35f),p);
            shard.transform.localRotation=Quaternion.Euler(5,i*31,(i-6)*-3);
        }
        for(int side=-1;side<=1;side+=2)
        {
            var arc=Shape("Frozen crescent",ringMesh,ice,new Vector3(side*1.19f,1.05f,6.35f),Vector3.one*.73f,p);arc.transform.localRotation=Quaternion.Euler(90,0,side*30);
            var heart=Shape("Frost heart",hex,light,new Vector3(side*1.19f,1.05f,6.26f),new Vector3(.15f,.44f,.15f),p);heart.transform.localRotation=Quaternion.Euler(0,0,side*25);
        }
    }
    void BuildReactorGate(Transform p,Material casing,Material metal,Material light)
    {
        Shape("Reactor dais",FieldBlock(),casing,new Vector3(0,.26f,6.35f),new Vector3(5.2f,.27f,.9f),p);
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;Vector3 point=new Vector3(Mathf.Sin(a)*1.15f,1.32f+Mathf.Cos(a)*1.08f,6.45f);
            var section=Shape("Reactor torus segment",FieldBlock(),metal,point,new Vector3(.40f,.16f,.41f),p);section.transform.localRotation=Quaternion.Euler(0,0,-a*Mathf.Rad2Deg);
            var emitter=Shape("Reactor torus light",FieldBlock(),light,point+Vector3.back*.23f,new Vector3(.22f,.055f,.025f),p);emitter.transform.localRotation=section.transform.localRotation;
        }
        var core=Shape("Reactor singularity",FieldBlock(),light,new Vector3(0,1.32f,6.4f),Vector3.one*.43f,p);core.transform.localRotation=Quaternion.Euler(20,30,45);
        for(int side=-1;side<=1;side+=2)
        {
            Shape("Reactor brace",FieldBlock(),casing,new Vector3(side*2.08f,1,6.45f),new Vector3(.43f,1.45f,.62f),p);
            for(int i=0;i<4;i++)Shape("Reactor brace inlay",FieldBlock(),light,new Vector3(side*2.08f,.49f+i*.29f,6.12f),new Vector3(.27f,.07f,.025f),p);
            var arm=Shape("Reactor angled arm",FieldBlock(),metal,new Vector3(side*1.77f,1.84f,6.45f),new Vector3(.91f,.16f,.5f),p);arm.transform.localRotation=Quaternion.Euler(0,0,side*24);
        }
    }
}
