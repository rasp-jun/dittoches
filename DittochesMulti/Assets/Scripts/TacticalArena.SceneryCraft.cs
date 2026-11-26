using UnityEngine;

public sealed partial class TacticalArena
{
    Mesh carvedBlock,canopyMesh,crystalMesh,basinWater;
    Mesh BasinWater()
    {
        if(basinWater!=null)return basinWater;
        const int sectors=48,rings=8;var vertices=new Vector3[(sectors+1)*(rings+1)];var colors=new Color[vertices.Length];var triangles=new int[sectors*rings*6];int cursor=0;
        for(int row=0;row<=rings;row++)for(int col=0;col<=sectors;col++)
        {
            float angle=col*360f/sectors,a=angle*Mathf.Deg2Rad,t=row/(float)rings;
            float edge=.8660254f/Mathf.Cos(Mathf.DeltaAngle(angle,Mathf.Round(angle/60)*60)*Mathf.Deg2Rad);
            int n=row*(sectors+1)+col;vertices[n]=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*edge*t;
            float ripple=.5f+.5f*Mathf.Sin(t*25+Mathf.Sin(a*3)*.7f);
            colors[n]=Color.Lerp(new Color(.13f,.36f,.48f),new Color(.75f,1,.88f),.18f+t*.27f+ripple*.18f);
            if(row==rings||col==sectors)continue;
            triangles[cursor++]=n;triangles[cursor++]=n+1;triangles[cursor++]=n+sectors+1;
            triangles[cursor++]=n+1;triangles[cursor++]=n+sectors+2;triangles[cursor++]=n+sectors+1;
        }
        basinWater=Own(new Mesh{name="Jade water with inset ripples",vertices=vertices,triangles=triangles});basinWater.colors=colors;return basinWater;
    }
    Mesh FieldBlock()
    {
        if(carvedBlock!=null)return carvedBlock;
        const int steps=4;var vertices=new Vector3[6*25];var triangles=new int[6*steps*steps*6];int cursor=0;
        Vector3[] normals={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
        for(int face=0;face<6;face++)
        {
            Vector3 n=normals[face],u=face<2?Vector3.forward:Vector3.right,v=Vector3.Cross(n,u);
            for(int y=0;y<=steps;y++)for(int x=0;x<=steps;x++)
            {
                Vector3 p=n*.5f+u*(x/(float)steps-.5f)+v*(y/(float)steps-.5f);
                Vector3 core=new Vector3(Mathf.Clamp(p.x,-.40f,.40f),Mathf.Clamp(p.y,-.40f,.40f),Mathf.Clamp(p.z,-.40f,.40f));
                vertices[face*25+y*5+x]=core+(p-core).normalized*.10f;
                if(x==steps||y==steps)continue;int a=face*25+y*5+x;
                triangles[cursor++]=a;triangles[cursor++]=a+1;triangles[cursor++]=a+5;
                triangles[cursor++]=a+1;triangles[cursor++]=a+6;triangles[cursor++]=a+5;
            }
        }
        carvedBlock=Own(new Mesh{name="Dressed stone with rounded bevels",vertices=vertices,triangles=triangles});carvedBlock.RecalculateNormals();return carvedBlock;
    }
    Mesh Canopy()
    {
        if(canopyMesh!=null)return canopyMesh;
        const int rows=8,columns=16;var vertices=new Vector3[(rows+1)*(columns+1)];var colors=new Color[vertices.Length];var triangles=new int[rows*columns*6];int cursor=0;
        for(int row=0;row<=rows;row++)for(int col=0;col<=columns;col++)
        {
            float a=col*Mathf.PI*2/columns,b=row*Mathf.PI/rows;
            float r=1+.05f*Mathf.Sin(a*5)*Mathf.Sin(b*3);
            int n=row*(columns+1)+col;vertices[n]=new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a))*r;
            colors[n]=Color.Lerp(new Color(.40f,.64f,.49f),new Color(1.10f,1.12f,.81f),Mathf.Clamp01(.48f+vertices[n].y*.40f-vertices[n].x*.12f));
            if(row==rows||col==columns)continue;
            triangles[cursor++]=n;triangles[cursor++]=n+1;triangles[cursor++]=n+columns+1;
            triangles[cursor++]=n+1;triangles[cursor++]=n+columns+2;triangles[cursor++]=n+columns+1;
        }
        canopyMesh=Own(new Mesh{name="Sculpted leafy crown",vertices=vertices,triangles=triangles});canopyMesh.colors=colors;return canopyMesh;
    }
    Mesh GlacierCrystal()
    {
        if(crystalMesh!=null)return crystalMesh;
        const int faces=6;var vertices=new Vector3[faces*12];var colors=new Color[vertices.Length];var triangles=new int[vertices.Length];int cursor=0;
        for(int i=0;i<faces;i++)
        {
            float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;
            Vector3 low=new Vector3(Mathf.Cos(a)*.45f,0,Mathf.Sin(a)*.45f),next=new Vector3(Mathf.Cos(b)*.45f,0,Mathf.Sin(b)*.45f);
            Vector3 shoulder=new Vector3(low.x,.65f,low.z),other=new Vector3(next.x,.65f,next.z),tip=new Vector3(.13f,1,-.07f);
            Vector3[] part={low,next,shoulder,next,other,shoulder,shoulder,other,tip,next,low,Vector3.zero};
            for(int j=0;j<part.Length;j++){vertices[cursor]=part[j];float light=.59f+(i%3)*.18f+(j>=6?.10f:0);colors[cursor]=new Color(light,light,light,1);triangles[cursor]=cursor;cursor++;}
        }
        for(int i=0;i<triangles.Length;i+=3){int first=triangles[i];triangles[i]=triangles[i+1];triangles[i+1]=first;}
        crystalMesh=Own(new Mesh{name="Six sided crystal with cut shoulders",vertices=vertices,triangles=triangles});crystalMesh.colors=colors;return crystalMesh;
    }
    Material FieldCraftMaterial(int id,int kind,Color tint)
    {
        const int size=128;var pixels=new Color32[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=x/(float)size,v=y/(float)size;
            float broad=Mathf.PerlinNoise(u*7+id*19,v*7+kind*13),grain=Mathf.PerlinNoise(u*64+7,v*64+17);
            float value=.84f+broad*.24f+(grain-.5f)*.13f;
            if(kind==1)value=.87f+grain*.10f+Mathf.PerlinNoise(u*3,v*100)*.12f;
            else if(id==1)
            {
                float vein=Mathf.Abs(Mathf.Sin((u+v*.53f+(broad-.5f)*.34f)*22));
                value+=Mathf.Pow(Mathf.Max(0,1-vein*8),2)*.16f;
            }
            else if(id==2)
            {
                float seam=Mathf.Min(Mathf.Repeat(u*4,1),Mathf.Repeat(v*4,1));
                if(seam<.026f)value*=.68f;
            }
            else if(kind==3)value=.70f+broad*.41f+grain*.09f;
            pixels[y*size+x]=new Color(value,value,value,1);
        }
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,true){name="Crafted field surface "+id+"/"+kind,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
        texture.SetPixels32(pixels);texture.Apply(true,true);resources.Add(texture);
        var material=Material(tint,true,texture);material.name="Crafted architecture "+id+"/"+kind;
        if(material.HasProperty("_VertexColor"))material.SetFloat("_VertexColor",1);
        if(material.HasProperty("_ZWrite"))material.SetInt("_ZWrite",1);
        material.renderQueue=2450;return material;
    }
    void ShadeFieldMesh(Mesh mesh,bool luminous)
    {
        mesh.RecalculateNormals();
        var vertices=mesh.vertices;var normals=mesh.normals;var original=mesh.colors;var colors=new Color[vertices.Length];var uv=new Vector2[vertices.Length];
        Vector3 sun=new Vector3(-.5f,1,-.65f).normalized;
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 n=normals[i],p=vertices[i];
            uv[i]=(Mathf.Abs(n.y)>Mathf.Max(Mathf.Abs(n.x),Mathf.Abs(n.z))?new Vector2(p.x,p.z):Mathf.Abs(n.x)>Mathf.Abs(n.z)?new Vector2(p.z,p.y):new Vector2(p.x,p.y))*.8f;
            if(luminous){colors[i]=original.Length==vertices.Length?original[i]:Color.white;colors[i].a=1;continue;}
            float light=.63f+.47f*Mathf.Max(0,Vector3.Dot(n,sun));
            float patina=.94f+.06f*Mathf.PerlinNoise(vertices[i].x*7.1f,vertices[i].z*5.3f);
            float occlusion=Mathf.Lerp(.82f,1,Mathf.Clamp01((vertices[i].y+.1f)/.8f));
            colors[i]=new Color(light*patina*occlusion,light*patina*occlusion,light*patina*occlusion,1);
        }
        mesh.colors=colors;mesh.uv=uv;
    }
    void BuildSculptedDetails(int id,Transform p,Material stoneSurface,Material metal,Material foliage,Material light)
    {
        for(int side=-1;side<=1;side+=2)for(int i=0;i<4;i++)
        {
            float z=-3.8f+i*2.7f,x=side*5.87f;
            if(id==0)
            {
                // Terraced planters and brass lanterns frame the board, never its cells.
                Shape("Terracotta sanctuary planter",FieldBlock(),stoneSurface,new Vector3(x,.34f,z),new Vector3(.57f,.30f,.70f),p);
                Shape("Rounded moss crown",Canopy(),foliage,new Vector3(x,.54f,z),new Vector3(.36f,.18f,.43f),p);
                Shape("Lantern brass foot",hex,metal,new Vector3(side*6.35f,.40f,z+.51f),new Vector3(.12f,.06f,.12f),p);
                Shape("Lantern warm heart",GlacierCrystal(),light,new Vector3(side*6.35f,.44f,z+.51f),new Vector3(.15f,.31f,.15f),p);
                Shape("Lantern finial",hex,metal,new Vector3(side*6.35f,.76f,z+.51f),new Vector3(.13f,.04f,.13f),p);
            }
            else if(id==1)
            {
                Shape("Layered frost ledge",FieldBlock(),foliage,new Vector3(side*5.92f,.40f,z),new Vector3(.8f,.12f,.93f),p);
                for(int j=0;j<3;j++)
                {
                    var shard=Shape("Hanging glacial icicle",GlacierCrystal(),light,new Vector3(side*(6.2f+j*.12f),.36f,z+.4f),new Vector3(.09f,.42f+j*.12f,.10f),p);
                    shard.transform.localRotation=Quaternion.Euler(180,0,side*8);
                }
            }
            else
            {
                Shape("Reactor induction drum",disc,stoneSurface,new Vector3(x,.55f,z),new Vector3(.42f,.24f,.42f),p);
                for(int j=0;j<3;j++)Shape("Induction winding",ringMesh,metal,new Vector3(x,.38f+j*.15f,z),Vector3.one*.39f,p);
                var core=Shape("Contained plasma spindle",GlacierCrystal(),light,new Vector3(x,.48f,z),new Vector3(.20f,.52f,.20f),p);core.transform.localRotation=Quaternion.Euler(0,i*38,0);
            }
        }
    }
}
