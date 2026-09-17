using UnityEngine;

public sealed partial class TacticalArena
{
    void BuildIsland()
    {
        var slate=Material(new Color(.24f,.31f,.32f));
        var edging=Material(new Color(.42f,.44f,.35f));
        var earth=Material(new Color(.10f,.16f,.16f));
        var moss=Material(new Color(.20f,.34f,.24f));
        var leaf=Material(new Color(.12f,.26f,.20f));
        var water=Material(new Color(.09f,.39f,.43f),true);
        Mesh terrace=TerraceMesh();
        Shape("Island bedrock",terrace,earth,new Vector3(0,-.85f,0),new Vector3(12.2f,1.5f,12.6f));
        Shape("Chiselled terrace",terrace,slate,new Vector3(0,-.18f,0),new Vector3(12f,.30f,12.4f));
        Shape("Brass terrace seam",terrace,trim,new Vector3(0,-.012f,0),new Vector3(11.96f,.045f,12.36f));
        Shape("Moss stone lip",terrace,edging,new Vector3(0,.015f,0),new Vector3(11.8f,.11f,12.2f));
        Shape("Unbroken playing surface",terrace,grass,new Vector3(0,.11f,.15f),new Vector3(10.52f,.09f,10.1f));
        for(int row=0;row<8;row++)for(int col=0;col<7;col++)
        {
            Vector3 p=CellWorld(col,row);
            tiles[row*7+col]=Shape("Formation outline "+row+":"+col,hex,stone,p,new Vector3(.735f,.08f,.735f));
            var turf=Shape("Turf "+row+":"+col,hex,grass,p+Vector3.up*.044f,new Vector3(.718f,.01f,.718f));
            float shade=((row*17+col*7)%5)*.006f;
            Tint(turf,new Color(.165f+shade,.28f+shade,.215f+shade));
        }
        for(int side=-1;side<=1;side+=2)
        {
            Shape("Shallow side channel",cube,water,new Vector3(side*5.27f,.06f,.10f),new Vector3(.24f,.025f,9.75f));
            for(int i=0;i<9;i++)
            {
                float z=-4.6f+i*1.12f;
                var block=Shape("Terrace paving",cube,slate,new Vector3(side*5.65f,.16f,z),new Vector3(.48f,.22f,1.055f));
                block.transform.localRotation=Quaternion.Euler(0,side*(i%3-1)*2,0);
            }
            for(int i=0;i<2;i++)
            {
                float z=i==0?-3.8f:4.5f;
                Shape("Beacon plinth",hex,slate,new Vector3(side*5.62f,.36f,z),new Vector3(.43f,.45f,.43f));
                Shape("Beacon collar",hex,trim,new Vector3(side*5.62f,.61f,z),new Vector3(.37f,.065f,.37f));
                var beacon=Shape("Crystal lantern",hex,glow,new Vector3(side*5.62f,.85f,z),new Vector3(.115f,.39f,.115f));
                beacon.transform.localRotation=Quaternion.Euler(0,30,side*10);
            }
            for(int i=0;i<5;i++)
            {
                float z=4.8f-i*2.3f;
                var rock=Shape("Fractured island edge",hex,earth,new Vector3(side*(5.5f+i%2*.3f),-.72f-i%3*.2f,z),new Vector3(.73f,1.15f,.85f));
                rock.transform.localRotation=Quaternion.Euler(12+i*7,31*i,side*18);
                var crown=Shape("Moss patch",hex,moss,new Vector3(side*5.95f,.19f,z),new Vector3(.36f,.08f,.47f));
                crown.transform.localRotation=Quaternion.Euler(0,27*i,0);
                for(int blade=0;blade<3;blade++)
                {
                    var sprig=Shape("Edge fern",cube,leaf,new Vector3(side*(6.08f+blade*.07f),.27f,z+blade*.10f),new Vector3(.065f,.32f,.08f));
                    sprig.transform.localRotation=Quaternion.Euler(blade*18,15*i,side*(20+blade*15));
                }
            }
        }
        Shape("Far stone steps",terrace,slate,new Vector3(0,.18f,5.55f),new Vector3(4.5f,.27f,.70f));
        Shape("Far altar",terrace,earth,new Vector3(0,.49f,5.78f),new Vector3(3.2f,.48f,.52f));
        Shape("Far altar inlay",cube,trim,new Vector3(0,.74f,5.78f),new Vector3(3.20f,.055f,.56f));
        Shape("Reserve deck",terrace,slate,new Vector3(0,.12f,-5.3f),new Vector3(10.8f,.30f,1.22f));
        for(int i=0;i<9;i++)
        {
            seats[i]=Shape("Reserve seat "+i,hex,trim,BenchWorld(i),new Vector3(.53f,.13f,.53f));
            Shape("Reserve cushion "+i,hex,dark,BenchWorld(i)+Vector3.up*.07f,new Vector3(.48f,.02f,.48f));
        }
        // Sparse midpoint inlays leave the combat silhouettes and spell areas unobstructed.
        for(int side=-1;side<=1;side+=2)
            Shape("Midline marker",cube,trim,new Vector3(side*4.92f,.245f,0),new Vector3(.11f,.01f,.64f));
    }
    Mesh TerraceMesh()
    {
        Vector2[] corners={new Vector2(-.43f,-.5f),new Vector2(.43f,-.5f),new Vector2(.5f,-.43f),new Vector2(.5f,.43f),new Vector2(.43f,.5f),new Vector2(-.43f,.5f),new Vector2(-.5f,.43f),new Vector2(-.5f,-.43f)};
        var v=new System.Collections.Generic.List<Vector3>();var t=new System.Collections.Generic.List<int>();
        for(int i=0;i<8;i++)
        {
            Vector3 a=new Vector3(corners[i].x,.5f,corners[i].y),b=new Vector3(corners[(i+1)%8].x,.5f,corners[(i+1)%8].y);
            int n=v.Count;v.Add(Vector3.up*.5f);v.Add(b);v.Add(a);t.Add(n);t.Add(n+1);t.Add(n+2);
            n=v.Count;v.Add(a);v.Add(b);v.Add(b-Vector3.up);v.Add(a-Vector3.up);
            t.Add(n);t.Add(n+1);t.Add(n+2);t.Add(n);t.Add(n+2);t.Add(n+3);
        }
        return Own(new Mesh{name="Bevelled island terrace",vertices=v.ToArray(),triangles=t.ToArray()});
    }
}
