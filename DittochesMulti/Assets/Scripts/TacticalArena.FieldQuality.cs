using System.Collections.Generic;
using UnityEngine;

public sealed partial class TacticalArena
{
    readonly List<MeshRenderer> detailedGround=new List<MeshRenderer>();
    readonly List<MeshRenderer> islandVegetation=new List<MeshRenderer>(),islandAltar=new List<MeshRenderer>(),islandLanterns=new List<MeshRenderer>();
    readonly Material[] terrainMaterials=new Material[3];
    readonly List<Transform>[] fieldMotes={new List<Transform>(),new List<Transform>(),new List<Transform>()};
    readonly List<Transform>[] fieldSeals={new List<Transform>(),new List<Transform>(),new List<Transform>()};
    bool fieldQualityReady;
    void EnsureFieldQuality()
    {
        if(fieldQualityReady||!presentationQuality)return;fieldQualityReady=true;
        foreach(var surface in fieldSurfaces)
        {
            var renderer=surface.renderer;
            if(renderer.name=="Edge fern"||renderer.name=="Moss patch")islandVegetation.Add(renderer);
            if(renderer.name=="Far altar"||renderer.name=="Far altar inlay")islandAltar.Add(renderer);
            if(renderer.name=="Crystal lantern"||renderer.name=="Beacon collar")islandLanterns.Add(renderer);
            if(!renderer.name.StartsWith("Turf")&&!renderer.name.Contains("playing surface"))continue;
            var filter=renderer.GetComponent<MeshFilter>();var original=filter.sharedMesh;
            var vertices=original.vertices;var uv=new Vector2[vertices.Length];
            for(int i=0;i<uv.Length;i++)
            {
                Vector3 p=renderer.transform.localPosition+Vector3.Scale(vertices[i],renderer.transform.localScale);
                uv[i]=new Vector2((p.x+5.5f)/11,(p.z+5.5f)/11);
            }
            filter.sharedMesh=Own(new Mesh{name="Continuous terrain UV",vertices=vertices,triangles=original.triangles,uv=uv});
            detailedGround.Add(renderer);
        }
    }
    Material TerrainMaterial(int id)
    {
        if(terrainMaterials[id]!=null)return terrainMaterials[id];
        const int size=512;var pixels=new Color32[size*size];
        var iceSites=new Vector2[64];var random=new System.Random(4187);
        for(int i=0;i<iceSites.Length;i++)iceSites[i]=new Vector2(i%8+.2f+(float)random.NextDouble()*.6f,i/8+.2f+(float)random.NextDouble()*.6f);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=x/(float)size,v=y/(float)size;
            // Strong decoration belongs beyond the playable hex footprint, not across its borders.
            float edge=Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Max(Mathf.Abs(u*11-5.5f)/4.9f,Mathf.Abs(v*11-5.5f)/4.72f)-1)/.14f));
            float broad=Mathf.PerlinNoise(u*7+id*13,v*7+4),grain=Mathf.PerlinNoise(u*110+7,v*110+id*17);
            Color c;
            if(id==0)
            {
                c=Color.Lerp(new Color(.105f,.195f,.18f),new Color(.25f,.34f,.29f),broad);
                c*=.94f+grain*.12f;
                float paverX=Mathf.Repeat(u*12+Mathf.Floor(v*14)%2*.5f,1),paverY=Mathf.Repeat(v*14,1);
                if(paverX<.014f||paverY<.012f)c*=.87f;
            }
            else if(id==1)
            {
                Vector2 p=new Vector2(u*8,v*8);int cellX=(int)p.x,cellY=(int)p.y;
                float nearest=100,second=100;int facet=0;
                for(int sy=Mathf.Max(0,cellY-1);sy<=Mathf.Min(7,cellY+1);sy++)for(int sx=Mathf.Max(0,cellX-1);sx<=Mathf.Min(7,cellX+1);sx++)
                {
                    int index=sy*8+sx;float d=(p-iceSites[index]).sqrMagnitude;
                    if(d<nearest){second=nearest;nearest=d;facet=index;}else if(d<second)second=d;
                }
                float fracture=Mathf.Sqrt(second)-Mathf.Sqrt(nearest);
                c=Color.Lerp(new Color(.15f,.29f,.40f),new Color(.40f,.58f,.69f),broad*.8f+(facet%7)*.027f);
                c*=.93f+grain*.12f;
                if(fracture<.025f)c=Color.Lerp(c,new Color(.64f,.79f,.83f),Mathf.Lerp(.14f,.36f,edge)*(1-fracture/.025f));
            }
            else
            {
                c=Color.Lerp(new Color(.075f,.075f,.13f),new Color(.19f,.16f,.29f),broad)*(.94f+grain*.10f);
                float gx=Mathf.Repeat(u*42,1),gy=Mathf.Repeat(v*42,1);
                if((gx<.04f||gy<.04f)&&broad>.48f)c=Color.Lerp(c,new Color(.34f,.26f,.51f),Mathf.Lerp(.20f,.36f,edge));
            }
            float wx=u*11-5.5f,wz=v*11-5.5f,radius=Mathf.Sqrt(wx*wx+wz*wz);
            float angle=Mathf.Atan2(wz,wx);
            float ring=Mathf.Max(1-Mathf.Abs(radius-1.85f)/.035f,1-Mathf.Abs(radius-2.03f)/.018f);
            float tick=radius>2.09f&&radius<2.28f&&Mathf.Abs(Mathf.Sin(angle*12))<.11f?1:0;
            float diamond=Mathf.Abs(Mathf.Abs(wx)+Mathf.Abs(wz)-.54f)<.025f?1:0;
            Color engraving=id==0?new Color(.49f,.53f,.35f):id==1?new Color(.47f,.68f,.76f):new Color(.49f,.36f,.68f);
            c=Color.Lerp(c,engraving,Mathf.Clamp01(Mathf.Max(ring,Mathf.Max(tick,diamond)))*.20f);
            // A quiet, broad center light keeps unit silhouettes readable without actual scene lights.
            float center=Mathf.Clamp01(1-new Vector2((u-.5f)*1.5f,(v-.5f)*1.5f).sqrMagnitude);
            pixels[y*size+x]=new Color(c.r*(.84f+.20f*center),c.g*(.84f+.20f*center),c.b*(.84f+.20f*center),1);
        }
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,true){name="Terrain grain "+id,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=4};
        texture.SetPixels32(pixels);texture.Apply(true,true);resources.Add(texture);
        var material=Material(Color.white,false,texture);material.name="Terrain theme "+id;
        // Imported sprite shader needs opaque ground depth, unlike transparent spell glows.
        if(material.HasProperty("_ZWrite"))material.SetInt("_ZWrite",1);material.renderQueue=2450;
        terrainMaterials[id]=material;return material;
    }
    void ApplyFieldQuality(int id)
    {
        if(!presentationQuality)return;EnsureFieldQuality();SetAtmosphere(id);
        // CaptureField retains these renderers for later tinting, so hide them without destroying their nodes.
        foreach(var renderer in islandVegetation)renderer.enabled=false;
        foreach(var renderer in islandAltar)renderer.enabled=false;
        foreach(var renderer in islandLanterns)renderer.enabled=false;
        var material=TerrainMaterial(id);
        foreach(var ground in detailedGround){ground.sharedMaterial=material;Tint(ground,Color.white);}
        if(fieldDecor[id]==null)BuildDetailedField(id);
        for(int i=0;i<3;i++)if(fieldDecor[i]!=null)fieldDecor[i].SetActive(i==id);
    }
    void BuildDetailedField(int id)
    {
        var parent=new GameObject("Detailed field "+id){layer=Layer,hideFlags=HideFlags.HideAndDontSave};parent.transform.SetParent(root.transform,false);fieldDecor[id]=parent;
        Color accent=TamerLoadout.FieldColor(id);
        var masonry=FieldCraftMaterial(id,0,id==0?new Color(.43f,.48f,.40f):id==1?new Color(.37f,.52f,.63f):new Color(.29f,.26f,.39f));
        var metal=FieldCraftMaterial(id,1,id==2?new Color(.56f,.43f,.66f):new Color(.64f,.54f,.34f));
        var luminous=Material(accent,true);luminous.name="Field luminous";
        if(luminous.HasProperty("_VertexColor"))luminous.SetFloat("_VertexColor",1);
        var foliage=FieldCraftMaterial(id,3,id==0?new Color(.27f,.49f,.29f):id==1?new Color(.59f,.80f,.86f):new Color(.39f,.29f,.56f));
        Transform p=parent.transform;
        BuildRenewedField(id,p,masonry,metal,foliage,luminous);
        BuildSculptedDetails(id,p,masonry,metal,foliage,luminous);
        CombineFieldArchitecture(parent);
        for(int i=0;i<12;i++)
        {
            var mote=Shape("Ambient mote",id==2?cube:SkillTip(),luminous,Vector3.zero,Vector3.one*(id==1?.032f:.025f),p);
            fieldMotes[id].Add(mote.transform);
        }
        if(id==2)for(int side=-1;side<=1;side+=2)
        {
            var seal=Shape("Orbiting data seal",ringMesh,luminous,new Vector3(side*6.07f,1.08f,.25f),Vector3.one*.44f,p);
            fieldSeals[id].Add(seal.transform);
        }
    }
    // These landmarks stay beyond the last playable row; the island keeps its ruined stone gate.
    void BuildIslandGate(Transform p,Material masonry,Material metal,Material luminous)
    {
        for(int side=-1;side<=1;side+=2)
        {
            Shape("Gate footing",hex,masonry,new Vector3(side*1.92f,.27f,5.79f),new Vector3(.49f,.42f,.42f),p);
            Shape("Gate column",cube,masonry,new Vector3(side*1.92f,.88f,5.83f),new Vector3(.37f,1.14f,.42f),p);
            Shape("Gate inset",cube,luminous,new Vector3(side*1.92f,.96f,5.60f),new Vector3(.07f,.70f,.025f),p);
            Shape("Gate capital",hex,metal,new Vector3(side*1.92f,1.49f,5.83f),new Vector3(.35f,.13f,.34f),p);
        }
        Shape("Gate lintel",cube,masonry,new Vector3(0,1.62f,5.84f),new Vector3(4.38f,.26f,.50f),p);
        Shape("Gate upper trim",cube,metal,new Vector3(0,1.78f,5.84f),new Vector3(4.48f,.065f,.54f),p);
        var crest=Shape("Gate engraved seal",ringMesh,luminous,new Vector3(0,1.13f,5.57f),Vector3.one*.42f,p);crest.transform.localRotation=Quaternion.Euler(90,0,0);
        for(int i=0;i<5;i++)Shape("Gate data inscription",cube,metal,new Vector3((i-2)*.11f,1.13f,5.53f),new Vector3(.055f,.10f+(i%2)*.12f,.025f),p);
    }
    void BuildFrostArch(Transform p,Material stone,Material ice,Material light)
    {
        Shape("Frozen arch footing",hex,stone,new Vector3(0,.19f,5.88f),new Vector3(2.70f,.18f,.44f),p);
        for(int side=-1;side<=1;side+=2)
        {
            for(int j=0;j<3;j++)
            {
                var shard=Shape("Frozen gate buttress",SkillTip(),ice,new Vector3(side*(1.85f+j*.28f),.23f,5.82f+j*.06f),new Vector3(.33f-j*.035f,1.10f+j*.20f,.30f),p);
                shard.transform.localRotation=Quaternion.Euler(0,j*24,side*(10+j*3));
            }
            var arm=Shape("Open ice arch arm",cube,ice,new Vector3(side*1.30f,1.43f,5.87f),new Vector3(1.13f,.21f,.34f),p);
            arm.transform.localRotation=Quaternion.Euler(0,0,-side*16);
            var tip=Shape("Open arch icicle",SkillTip(),light,new Vector3(side*.79f,1.52f,5.77f),new Vector3(.12f,.31f,.11f),p);
            tip.transform.localRotation=Quaternion.Euler(0,0,180-side*18);
            Shape("Frozen foot ledge",hex,ice,new Vector3(side*2.13f,.31f,5.74f),new Vector3(.56f,.12f,.42f),p);
        }
    }
    void BuildDataPortal(Transform p,Material casing,Material metal,Material light)
    {
        Shape("Portal circuit plinth",cube,casing,new Vector3(0,.23f,5.85f),new Vector3(4.62f,.22f,.69f),p);
        Shape("Portal lower circuit bus",cube,light,new Vector3(0,.36f,5.55f),new Vector3(3.28f,.025f,.045f),p);
        for(int side=-1;side<=1;side+=2)
        {
            Shape("Portal segmented column",cube,casing,new Vector3(side*1.92f,.87f,5.85f),new Vector3(.40f,1.10f,.39f),p);
            Shape("Portal corner socket",cube,metal,new Vector3(side*1.92f,1.47f,5.84f),new Vector3(.49f,.22f,.44f),p);
            var upper=Shape("Portal separated upper bracket",cube,metal,new Vector3(side*1.30f,1.60f,5.84f),new Vector3(.91f,.18f,.36f),p);
            upper.transform.localRotation=Quaternion.Euler(0,0,side*10);
            Shape("Portal bracket light",cube,light,new Vector3(side*1.92f,1.01f,5.62f),new Vector3(.065f,.57f,.025f),p);
            Shape("Portal lower corner",cube,metal,new Vector3(side*1.61f,.47f,5.84f),new Vector3(.93f,.18f,.39f),p);
            Shape("Portal circuit branch",cube,light,new Vector3(side*.78f,.36f,5.69f),new Vector3(.035f,.025f,.28f),p);
        }
        var bezel=Shape("Portal diamond socket",cube,metal,new Vector3(0,1.10f,5.78f),new Vector3(.60f,.60f,.20f),p);bezel.transform.localRotation=Quaternion.Euler(0,0,45);
        var core=Shape("Portal diamond core",cube,light,new Vector3(0,1.10f,5.66f),new Vector3(.37f,.37f,.065f),p);core.transform.localRotation=Quaternion.Euler(0,0,45);
        for(int i=0;i<3;i++)Shape("Portal suspended circuit pin",cube,light,new Vector3((i-1)*.13f,.58f,5.68f),new Vector3(.045f,.12f+(i%2)*.08f,.025f),p);
    }
    void BuildThemeLanterns(int id,Transform p,Material metal,Material ice,Material light)
    {
        for(int side=-1;side<=1;side+=2)for(int end=0;end<2;end++)
        {
            float z=end==0?-3.8f:4.5f;
            if(id==1)
            {
                var crystal=Shape("Frost corner crystal",SkillTip(),ice,new Vector3(side*5.62f,.62f,z),new Vector3(.17f,.44f,.15f),p);
                crystal.transform.localRotation=Quaternion.Euler(0,30,side*9);
                Shape("Frost corner shard",SkillTip(),light,new Vector3(side*5.77f,.62f,z+.10f),new Vector3(.08f,.25f,.08f),p);
            }
            else
            {
                Shape("Data corner socket",cube,metal,new Vector3(side*5.62f,.67f,z),new Vector3(.34f,.16f,.34f),p);
                var crystal=Shape("Data corner core",cube,light,new Vector3(side*5.62f,.88f,z),new Vector3(.14f,.28f,.14f),p);
                crystal.transform.localRotation=Quaternion.Euler(0,45,0);
            }
        }
    }
    void CombineFieldArchitecture(GameObject parent)
    {
        var groups=new Dictionary<Material,List<CombineInstance>>();
        var renderers=parent.GetComponentsInChildren<MeshRenderer>();
        foreach(var renderer in renderers)
        {
            List<CombineInstance> group;if(!groups.TryGetValue(renderer.sharedMaterial,out group)){group=new List<CombineInstance>();groups.Add(renderer.sharedMaterial,group);}
            group.Add(new CombineInstance{mesh=renderer.GetComponent<MeshFilter>().sharedMesh,transform=parent.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix});
        }
        foreach(var group in groups)
        {
            var mesh=new Mesh{name="Batched field architecture",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(group.Value.ToArray());ShadeFieldMesh(mesh,group.Key.name=="Field luminous");resources.Add(mesh);
            Shape("Field architecture batch",mesh,group.Key,Vector3.zero,Vector3.one,parent.transform);
        }
        foreach(var renderer in renderers)Release(renderer.gameObject);
    }
    void AnimateField()
    {
        if(!fieldQualityReady)return;float time=Time.unscaledTime;
        for(int i=0;i<fieldMotes[FieldId].Count;i++)
        {
            float phase=time*(FieldId==1?.15f:.09f)+i*.618f;
            fieldMotes[FieldId][i].localPosition=new Vector3((i%2==0?-1:1)*(5.48f+Mathf.Sin(phase*3)*.19f),.30f+Mathf.Repeat(phase,1)*.8f,-4.2f+(i/2)*1.66f+Mathf.Sin(phase)*.2f);
            fieldMotes[FieldId][i].localRotation=Quaternion.Euler(phase*31,phase*63,45);
        }
        foreach(var seal in fieldSeals[FieldId])seal.localRotation=Quaternion.Euler(16,time*16,12);
    }
}
