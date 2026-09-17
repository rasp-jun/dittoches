using UnityEngine;

/// <summary>Authored stylized models. Each definition owns its silhouette and anatomical rig.</summary>
public static class DigimonModelLibrary
{
    public sealed class Model
    {
        public string id;
        public Mesh mesh;
        public DigimonMeshBuilder.Skeleton skeleton;
        public float height,hipHeight,legLength,footSpread;
    }
    // Work in progress. Enabled only by the explicit model inspection scene until home-PC authoring resumes.
    public static bool PreviewEnabled;
    public static bool HasModel(string id){return PreviewEnabled&&id=="agumon";}
    public static Model Build(string id)
    {
        if(!HasModel(id))return null;
        var b=new DigimonMeshBuilder();var s=b.skeleton;
        int hips=s.Add("Hips",-1,new Vector3(0,.61f,-.02f));
        int chest=s.Add("Spine",hips,new Vector3(0,.91f,.005f));
        int head=s.Add("Head",chest,new Vector3(0,1.16f,.04f));
        int jaw=s.Add("Jaw",head,new Vector3(0,1.24f,.09f));
        int tail=s.Add("Tail",hips,new Vector3(0,.58f,-.22f));
        int tailMid=s.Add("TailMid",tail,new Vector3(0,.40f,-.57f));
        int tailTip=s.Add("TailTip",tailMid,new Vector3(0,.40f,-.86f));
        Color skin=new Color(1,.59f,.10f),belly=new Color(1,.82f,.36f),dark=new Color(.28f,.09f,.045f);
        Color claw=new Color(1,.96f,.79f),green=new Color(.27f,.70f,.24f),ink=new Color(.045f,.07f,.045f);
        b.Ball(new Vector3(0,.70f,0),new Vector3(.29f,.36f,.26f),skin,hips,chest,.28f);
        b.Ball(new Vector3(0,.83f,.15f),new Vector3(.234f,.29f,.15f),belly,chest,hips,.4f);
        b.Capsule(new Vector3(0,.92f,.03f),new Vector3(0,1.24f,.08f),.19f,.19f,skin,chest,head,.5f);
        // Brow, skull and long dinosaur muzzle are separate from the hinged lower jaw.
        b.Ball(new Vector3(0,1.39f,.085f),new Vector3(.32f,.275f,.295f),skin,head);
        b.Ball(new Vector3(0,1.34f,.30f),new Vector3(.345f,.16f,.28f),skin,head);
        b.Ball(new Vector3(0,1.203f,.34f),new Vector3(.294f,.019f,.23f),dark,head);
        b.Ball(new Vector3(0,1.18f,.30f),new Vector3(.303f,.091f,.25f),skin,jaw);
        b.Ball(new Vector3(0,1.258f,.31f),new Vector3(.266f,.008f,.21f),new Color(.67f,.24f,.25f),jaw);
        for(int side=-1;side<=1;side+=2)
        {
            // Eyes wrap around the sides of the brow, visible from an isometric camera.
            b.Ellipsoid(new Vector3(side*.264f,1.468f,.235f),new Vector3(.094f,.123f,.047f),Color.white,head,Quaternion.Euler(0,side*45,side*-10));
            b.Ellipsoid(new Vector3(side*.286f,1.469f,.268f),new Vector3(.050f,.088f,.022f),green,head,Quaternion.Euler(0,side*45,0));
            b.Ellipsoid(new Vector3(side*.294f,1.469f,.283f),new Vector3(.021f,.066f,.010f),ink,head,Quaternion.Euler(0,side*45,0));
            b.Ball(new Vector3(side*.296f,1.505f,.293f),new Vector3(.011f,.017f,.008f),Color.white,head);
            b.Capsule(new Vector3(side*.21f,1.567f,.27f),new Vector3(side*.32f,1.548f,.19f),.035f,.041f,skin,head);
            b.Ball(new Vector3(side*.145f,1.419f,.516f),new Vector3(.021f,.011f,.015f),dark,head);
            for(int tooth=0;tooth<3;tooth++)
            {
                float z=.22f+tooth*.10f;
                b.Horn(new Vector3(side*.255f,1.237f,z),new Vector3(side*.251f,1.216f,z+.006f),new Vector3(side*.245f,1.195f,z+.012f),.025f,claw,head);
            }
            string suffix=side<0?"L":"R";
            int upper=s.Add("UpperArm"+suffix,chest,new Vector3(side*.235f,1.02f,.025f));
            int fore=s.Add("Forearm"+suffix,upper,new Vector3(side*.39f,.84f,.06f));
            int hand=s.Add("Hand"+suffix,fore,new Vector3(side*.45f,.72f,.20f));
            b.Tube(new[]{s.positions[upper],Vector3.Lerp(s.positions[upper],s.positions[fore],.35f),s.positions[fore],Vector3.Lerp(s.positions[fore],s.positions[hand],.6f),s.positions[hand]},
                new[]{.075f,.11f,.082f,.085f,.071f},skin,new[]{upper,upper,fore,fore,hand},16);
            b.Ball(s.positions[hand],new Vector3(.115f,.085f,.11f),skin,hand);
            for(int finger=0;finger<3;finger++)
            {
                Vector3 p=new Vector3(side*(.38f+finger*.065f),.70f,.24f);
                b.Capsule(p,p+new Vector3(side*.016f,-.03f,.075f),.032f,.035f,skin,hand);
                b.Horn(p+new Vector3(0,-.02f,.065f),p+new Vector3(0,-.028f,.125f),p+new Vector3(0,-.06f,.16f),.027f,claw,hand);
            }
            int thigh=s.Add("Thigh"+suffix,hips,new Vector3(side*.195f,.60f,-.015f));
            int shin=s.Add("Shin"+suffix,thigh,new Vector3(side*.215f,.34f,.045f));
            int foot=s.Add("Foot"+suffix,shin,new Vector3(side*.235f,.11f,.025f));
            b.Tube(new[]{s.positions[thigh]+Vector3.up*.06f,s.positions[thigh]-Vector3.up*.08f,s.positions[shin],Vector3.Lerp(s.positions[shin],s.positions[foot],.6f),s.positions[foot]},
                new[]{.11f,.168f,.118f,.112f,.084f},skin,new[]{thigh,thigh,shin,shin,foot},16);
            b.Ball(new Vector3(side*.24f,.104f,.15f),new Vector3(.19f,.103f,.245f),skin,foot);
            for(int toe=0;toe<3;toe++)
            {
                float x=side*.24f+(toe-1)*.112f;
                b.Horn(new Vector3(x,.096f,.304f),new Vector3(x,.075f,.40f),new Vector3(x,.040f,.47f),.047f,claw,foot);
            }
        }
        b.Tube(new[]{new Vector3(0,.61f,-.13f),new Vector3(0,.51f,-.37f),new Vector3(0,.405f,-.59f),new Vector3(0,.40f,-.79f),new Vector3(0,.455f,-1.01f)},
            new[]{.20f,.17f,.112f,.064f,.005f},skin,new[]{hips,tail,tailMid,tailTip,tailTip},16);
        return new Model{id=id,mesh=b.Build("Agumon / authored skinned mesh"),skeleton=s,height=1.69f,hipHeight=.61f,legLength=.26f,footSpread=.215f};
    }
}
