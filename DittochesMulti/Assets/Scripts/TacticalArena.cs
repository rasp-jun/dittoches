using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared, procedural perspective arena. All interaction uses the same camera as rendering.</summary>
public sealed partial class TacticalArena : IDisposable
{
    public static readonly Rect SoloViewport = new Rect(282, 118, 1068, 710);
    public static readonly Rect WideViewport = new Rect(248, 96, 1420, 740);
    public static readonly Rect MultiViewport = new Rect(300, 125, 980, 660);
    const int Layer = 30;
    static int nextArenaId;
    readonly GameObject root;
    readonly Camera camera;
    readonly RenderTexture target;
    readonly List<UnityEngine.Object> resources = new List<UnityEngine.Object>();
    readonly Dictionary<object, Actor> actors = new Dictionary<object, Actor>();
    readonly Dictionary<Texture, Material> portraits = new Dictionary<Texture, Material>();
    readonly List<object> expired = new List<object>();
    readonly MeshRenderer[] tiles = new MeshRenderer[56], seats = new MeshRenderer[9];
    readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
    readonly Material stone, trim, grass, dark, glow, shadow;
    readonly Mesh hex, cube, quad, disc, ringMesh;
    Rect viewport;
    int generation;
    bool disposed;
    readonly bool refined;
    sealed class Actor
    {
        public GameObject root;
        public MeshRenderer portrait, contactShadow, teamBase, halo, destination, selection;
        public MeshRenderer[] sparkles;
        public DigimonRig rig;
        public Vector3 facing;
        public bool hasFacing;
        public int generation;
    }
    public RenderTexture Texture { get { return target; } }
    public int ActorCount { get { return actors.Count; } }

    public TacticalArena(Rect viewport,bool refined=true)
    {
        this.viewport = viewport;
        this.refined=refined;
        root = new GameObject("Tactical Arena (runtime)") { hideFlags = HideFlags.HideAndDontSave };
        // Isolate from scene lights/cameras without changing global render settings.
        root.transform.position = new Vector3(200f * nextArenaId++, -1000, 0);
        hex = Prism(6, 30); cube = Box(); quad = Billboard(); disc = Prism(32, 0); ringMesh = Ring();
        stone = Material(new Color(.28f,.37f,.36f));
        trim = Material(new Color(.57f,.43f,.22f));
        grass = Material(new Color(.16f,.27f,.22f));
        dark = Material(new Color(.055f,.10f,.13f));
        glow = Material(new Color(.20f,.78f,.80f), true);
        shadow = Material(new Color(.04f,.065f,.055f), true);
        var cameraObject = new GameObject("Arena Camera") { hideFlags = HideFlags.HideAndDontSave };
        cameraObject.transform.SetParent(root.transform, false);
        camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.transform.localPosition = new Vector3(0, 12.8f, -14.2f);
        camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0, 0, -.3f)));
        camera.fieldOfView = 30;
        camera.nearClipPlane = .1f; camera.farClipPlane = 65;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.028f,.055f,.075f);
        camera.cullingMask = 1 << Layer;
        camera.allowHDR = false; camera.allowMSAA = true;
        target = new RenderTexture(1440, Mathf.RoundToInt(1440 * viewport.height / viewport.width), 24, RenderTextureFormat.ARGB32);
        target.name = "Tactical arena view"; target.antiAliasing = 2; target.Create();
        camera.targetTexture = target; camera.aspect = viewport.width / viewport.height;
        if(refined)BuildIsland();else BuildEnvironment();
    }

    Material Material(Color color, bool unlit = false, Texture texture = null)
    {
        Shader shader = Resources.Load<Shader>("ArenaSurface");
#if DITTOCHES_PORTABLE_PREVIEW
        if(shader==null)shader=PortablePreview.ArenaShader(texture!=null);
#endif
        if (shader == null) throw new InvalidOperationException("Missing Resources/ArenaSurface.shader");
        var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
#if DITTOCHES_PORTABLE_PREVIEW
        PortablePreview.ConfigureMaterial(material,texture!=null);
#endif
        material.color = color; if(material.HasProperty("_Unlit"))material.SetFloat("_Unlit", unlit ? 1 : 0);
        material.mainTexture = texture != null ? texture : Texture2D.whiteTexture;
        resources.Add(material); return material;
    }

    MeshRenderer Shape(string name, Mesh mesh, Material material, Vector3 position, Vector3 scale, Transform parent = null)
    {
        var node = new GameObject(name) { layer = Layer, hideFlags = HideFlags.HideAndDontSave };
        node.transform.SetParent(parent != null ? parent : root.transform, false);
        node.transform.localPosition = position; node.transform.localScale = scale;
        node.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = node.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    void BuildEnvironment()
    {
        Shape("Floating foundation", cube, dark, new Vector3(0,-.65f,0), new Vector3(11.8f,1.15f,12.3f));
        Shape("Gold foundation inlay", cube, trim, new Vector3(0,-.12f,0), new Vector3(11.9f,.14f,12.4f));
        Shape("Arena stone terrace", cube, stone, new Vector3(0,-.015f,0), new Vector3(11.65f,.18f,12.15f));
        Shape("Battlefield turf", cube, grass, new Vector3(0,.09f,.2f), new Vector3(10.3f,.09f,9.5f));
        for (int row=0; row<8; row++) for (int col=0; col<7; col++)
        {
            Vector3 point = CellWorld(col,row);
            tiles[row*7+col] = Shape("Hex " + row + ":" + col, hex, stone, point, new Vector3(.735f,.09f,.735f));
            Shape("Inset " + row + ":" + col, hex, grass, point+Vector3.up*.051f, new Vector3(.693f,.014f,.693f));
        }
        for (int side=-1; side<=1; side+=2)
        {
            Shape("Raised stone rail",cube,stone,new Vector3(side*5.5f,.27f,.1f),new Vector3(.40f,.55f,10.9f));
            Shape("Rail gold edge",cube,trim,new Vector3(side*5.5f,.56f,.1f),new Vector3(.43f,.06f,10.9f));
            for (int i=0;i<4;i++)
            {
                float z=-4.25f+i*2.9f;
                Shape("Pillar",cube,dark,new Vector3(side*5.5f,.69f,z),new Vector3(.70f,1.25f,.70f));
                Shape("Pillar cap",cube,trim,new Vector3(side*5.5f,1.34f,z),new Vector3(.80f,.12f,.80f));
                var crystal=Shape("Beacon crystal",hex,glow,new Vector3(side*5.5f,1.68f,z),new Vector3(.20f,.56f,.20f));
                crystal.transform.localRotation=Quaternion.Euler(0,30,15);
            }
            for(int i=0;i<5;i++)
            {
                var rock=Shape("Floating outcrop",cube,dark,new Vector3(side*(6.4f+i*.28f),-1.5f-i*.32f,4.8f-i*2.6f),new Vector3(.65f,.7f,.85f));
                rock.transform.localRotation=Quaternion.Euler(12*i,25*i,15);
            }
        }
        Shape("Enemy gate",cube,dark,new Vector3(0,.6f,5.65f),new Vector3(4.8f,1.1f,.45f));
        Shape("Gate crown",cube,trim,new Vector3(0,1.2f,5.65f),new Vector3(5.1f,.13f,.55f));
        Shape("Gate light",cube,glow,new Vector3(0,.70f,5.39f),new Vector3(3.9f,.07f,.035f));
        Shape("Front reserve terrace",cube,dark,new Vector3(0,.12f,-5.28f),new Vector3(10.6f,.28f,1.25f));
        for(int i=0;i<9;i++)
        {
            seats[i]=Shape("Reserve pedestal "+i,hex,trim,BenchWorld(i),new Vector3(.54f,.14f,.54f));
            Shape("Reserve inset "+i,hex,stone,BenchWorld(i)+Vector3.up*.08f,new Vector3(.48f,.025f,.48f));
        }
        Shape("Center crest",disc,trim,new Vector3(0,.15f,0),new Vector3(.34f,.015f,.34f));
    }

    public static Vector3 CellWorld(float col, float row)
    {
        int r0=Mathf.Clamp(Mathf.FloorToInt(row),0,7),r1=Mathf.Min(7,r0+1);
        float offset=Mathf.Lerp((r0%2)*.65f,(r1%2)*.65f,Mathf.Clamp01(row-r0));
        return new Vector3((col-3)*1.30f+offset-.325f,.19f,(3.5f-row)*1.125f);
    }
    public static Vector3 BenchWorld(int index) { return new Vector3((index-4)*1.13f,.36f,-5.28f); }
    public Vector2 Project(Vector3 point)
    {
        Vector3 p=camera.WorldToViewportPoint(root.transform.TransformPoint(point));
        return new Vector2(viewport.x+p.x*viewport.width,viewport.y+(1-p.y)*viewport.height);
    }
    public bool GroundPoint(Vector2 guiPoint, out Vector3 point)
    {
        point=Vector3.zero;if(!viewport.Contains(guiPoint))return false;
        Ray ray=camera.ViewportPointToRay(new Vector3((guiPoint.x-viewport.x)/viewport.width,1-(guiPoint.y-viewport.y)/viewport.height,0));
        var plane=new Plane(Vector3.up,root.transform.TransformPoint(new Vector3(0,.19f,0)));
        float distance;if(!plane.Raycast(ray,out distance))return false;
        point=root.transform.InverseTransformPoint(ray.GetPoint(distance));return true;
    }
    public int HitCell(Vector2 point)
    {
        Vector3 world;if(!GroundPoint(point,out world))return -1;
        return HitWorld(world);
    }
    public static int HitWorld(Vector3 world)
    {
        for(int row=0;row<8;row++)for(int col=0;col<7;col++)
        {
            Vector3 delta=world-CellWorld(col,row);
            float x=Mathf.Abs(delta.x),z=Mathf.Abs(delta.z);
            if(x<=.6365f&&z<=.735f&&z+x*.5773503f<=.735f)return row*7+col;
        }
        return -1;
    }
    public int HitBench(Vector2 point)
    {
        if(!viewport.Contains(point))return -1;
        Ray ray=camera.ViewportPointToRay(new Vector3((point.x-viewport.x)/viewport.width,1-(point.y-viewport.y)/viewport.height,0));
        var plane=new Plane(Vector3.up,root.transform.TransformPoint(new Vector3(0,.36f,0)));
        float distance;if(!plane.Raycast(ray,out distance))return -1;
        Vector3 world=root.transform.InverseTransformPoint(ray.GetPoint(distance));
        for(int i=0;i<9;i++)
        {
            Vector3 delta=world-BenchWorld(i);float x=Mathf.Abs(delta.x),z=Mathf.Abs(delta.z);
            if(x<=.4677f&&z<=.54f&&z+x*.5773503f<=.54f)return i;
        }
        return -1;
    }
    public Rect CellRect(int row,int col) { return Bounds(CellWorld(col,row),.67f,.57f); }
    public Rect BenchRect(int index) { return Bounds(BenchWorld(index),.54f,.46f); }
    Rect Bounds(Vector3 center,float width,float depth)
    {
        Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
        {
            Vector2 p=Project(center+new Vector3(x*width,0,z*depth));min=Vector2.Min(min,p);max=Vector2.Max(max,p);
        }
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }

    public Rect LabelRect(Vector3 point,float yOffset=0)
    {
        Vector2 p=Project(point);return new Rect(p.x-48,p.y+yOffset,96,20);
    }
    void Tint(MeshRenderer renderer,Color color)
    {
        properties.Clear();properties.SetColor("_Color",color);renderer.SetPropertyBlock(properties);
    }
    public void BeginFrame(int selectedCell,int hoveredCell,int selectedBench,bool placing)
    {
        generation++;BeginEffects();
        for(int i=0;i<tiles.Length;i++)
        {
            bool own=i>=28;
            Color color=refined?new Color(.19f,.30f,.26f):own?new Color(.33f,.46f,.39f):new Color(.42f,.34f,.30f);
            if(placing&&own)color=new Color(.31f,.55f,.48f);
            if(i==selectedCell)color=new Color(1,.76f,.24f);
            if(i==hoveredCell&&own)color=new Color(.45f,1,.86f);
            Tint(tiles[i],color);
        }
        for(int i=0;i<seats.Length;i++)Tint(seats[i],i==selectedBench?new Color(1,.8f,.3f):new Color(.57f,.43f,.22f));
    }
    public void HighlightDestination(int cell,int seat,bool valid)
    {
        Color color=valid?new Color(.35f,1f,.75f):new Color(1f,.25f,.18f);
        if(cell>=0&&cell<tiles.Length)Tint(tiles[cell],color);
        if(seat>=0&&seat<seats.Length)Tint(seats[seat],color);
    }
    public void SetActor(object key,Vector3 point,Texture texture,Color team,float scale=1,float flash=0)
    {
        Actor actor;
        if(!actors.TryGetValue(key,out actor))
        {
            var node=new GameObject("Arena piece"){layer=Layer,hideFlags=HideFlags.HideAndDontSave};
            node.transform.SetParent(root.transform,false);
            actor=new Actor{root=node};
            actor.contactShadow=Shape("Contact shadow",disc,shadow,new Vector3(0,.012f,0),new Vector3(.43f,.01f,.30f),node.transform);
            var ring=Shape("Team base",refined?ringMesh:disc,glow,new Vector3(0,.025f,0),new Vector3(.34f,.012f,.28f),node.transform);
            actor.teamBase=ring;
            Tint(ring,team);
            actor.portrait=Shape("Character",quad,stone,new Vector3(0,.67f,0),Vector3.one,node.transform);
            actor.portrait.transform.rotation=camera.transform.rotation;
            actor.portrait.transform.localPosition=camera.transform.up*.65f;
            actors.Add(key,actor);
        }
        actor.generation=generation;
        actor.portrait.enabled=true;
        if(actor.rig!=null)actor.rig.root.SetActive(false);
        Tint(actor.teamBase,refined?Color.Lerp(new Color(.12f,.22f,.20f),team,.66f):team);
        actor.root.transform.localPosition=point+Vector3.up*.10f;
        actor.root.transform.localScale=Vector3.one*scale;
        Material material;
        if(texture!=null)
        {
            if(!portraits.TryGetValue(texture,out material)){material=Material(Color.white,true,texture);portraits.Add(texture,material);}
            actor.portrait.sharedMaterial=material;
        }
        else actor.portrait.sharedMaterial=glow;
        Tint(actor.portrait,texture==null?team:Color.Lerp(Color.white,new Color(1,.4f,.3f),Mathf.Clamp01(flash)));
    }
    public void PoseCombatActor(object key,float speed,float direction,float time,float death)
    {
        Actor actor;if(!actors.TryGetValue(key,out actor))return;
        speed=Mathf.Clamp01(speed);
        float bob=Mathf.Abs(Mathf.Sin(time*10f))*.045f*speed;
        actor.portrait.transform.localPosition=camera.transform.up*.65f+Vector3.up*bob;
        actor.portrait.transform.rotation=camera.transform.rotation*Quaternion.Euler(0,0,death>0?-55f*death:Mathf.Sin(time*10f)*3f*speed*Mathf.Sign(direction));
        actor.contactShadow.transform.localScale=new Vector3(.43f,.01f,.30f)*(1-bob*2);
    }
    public void HighlightAttackRange(int cell,float range)
    {
        if(cell<0||cell>=tiles.Length)return;
        int col=cell%7,row=cell/7;
        for(int i=0;i<tiles.Length;i++)
        {
            if(i==cell)continue;
            float x=i%7-col,y=i/7-row;
            if(x*x+y*y<=range*range)Tint(tiles[i],i>=28?new Color(.25f,.55f,.64f):new Color(.48f,.4f,.66f));
        }
    }
    public void HighlightHexAttackRange(float col,float row,int range)
    {
        for(int i=0;i<tiles.Length;i++)
            if(DigimonCombatMath.InAttackRange(col,row,i%7,i/7,range))
                Tint(tiles[i],new Color(.12f,.75f,1f));
        int x=Mathf.RoundToInt(col),y=Mathf.RoundToInt(row);
        if(x>=0&&x<7&&y>=0&&y<8)Tint(tiles[y*7+x],new Color(.72f,.57f,.26f));
    }
    public void DecorateActor(object key,bool selected,float promotion,float hit=0,float healing=0,float shielding=0,bool invalid=false)
    {
        Actor actor;if(!actors.TryGetValue(key,out actor))return;
        bool visible=selected||promotion>0||healing>0||shielding>0;
        if(visible&&actor.selection==null)
            actor.selection=Shape("Unit selection",ringMesh,glow,new Vector3(0,.035f,0),Vector3.one*.52f,actor.root.transform);
        Color color=invalid?new Color(1f,.3f,.25f):promotion>0?new Color(1f,.82f,.3f):healing>0?new Color(.35f,1f,.6f):shielding>0?new Color(.4f,.8f,1f):new Color(1f,.78f,.3f);
        if(actor.selection!=null)
        {
            actor.selection.enabled=visible;
            actor.selection.transform.localScale=Vector3.one*(.5f+Mathf.Sin(Time.unscaledTime*5f)*.025f+promotion*.16f);
            Tint(actor.selection,color);
        }
        properties.Clear();
        Color tint=Color.Lerp(Color.white,new Color(1f,.4f,.3f),Mathf.Clamp01(hit));
        if(healing>hit)tint=Color.Lerp(Color.white,new Color(.5f,1f,.68f),Mathf.Clamp01(healing)*.5f);
        properties.SetColor("_Color",tint);
        properties.SetColor("_OutlineColor",color);
        properties.SetFloat("_OutlineWidth",visible?2f:0f);
        actor.portrait.SetPropertyBlock(properties);
        if(actor.rig!=null)actor.rig.renderer.SetPropertyBlock(properties);
    }
    public void SetTactician(object key, Vector3 point, Vector3 destination, Texture texture,
        Color color, float movement, float horizontalSpeed, float time, float celebration)
    {
        SetActor(key,point,texture,color,1.08f);
        Actor actor=actors[key];
        if(actor.halo==null)
        {
            actor.teamBase.enabled=false;
            actor.halo=Shape("Tactician halo",ringMesh,glow,new Vector3(0,.035f,0),Vector3.one*.53f,actor.root.transform);
            actor.destination=Shape("Move destination",ringMesh,glow,Vector3.zero,Vector3.one*.35f,actor.root.transform);
            actor.sparkles=new MeshRenderer[6];
            for(int i=0;i<actor.sparkles.Length;i++)
                actor.sparkles[i]=Shape("Tactician sparkle",cube,glow,Vector3.zero,Vector3.one*.055f,actor.root.transform);
        }
        float speed=Mathf.Clamp01(movement),reward=Mathf.Clamp01(celebration);
        float hop=Mathf.Abs(Mathf.Sin(time*9f))*.19f*speed;
        float breath=Mathf.Sin(time*2.8f)*.025f;
        float cheer=Mathf.Abs(Mathf.Sin(time*7f))*.28f*reward;
        float stretch=Mathf.Sin(time*18f)*.045f*speed;
        actor.portrait.transform.localScale=new Vector3(1-stretch,1+stretch,1);
        actor.portrait.transform.localPosition=camera.transform.up*(.65f*(1+stretch))+Vector3.up*(.045f+hop+breath+cheer);
        actor.portrait.transform.rotation=camera.transform.rotation*Quaternion.Euler(0,0,-Mathf.Clamp(horizontalSpeed/100f,-1,1)*7f*speed+Mathf.Sin(time*3f)*2f);
        actor.contactShadow.transform.localScale=new Vector3(.46f,.01f,.32f)*(1-hop*.65f-cheer*.35f);
        actor.halo.transform.localScale=Vector3.one*(.53f+breath+reward*.1f);
        Tint(actor.halo,Color.Lerp(color,new Color(1,.82f,.35f),reward));
        properties.Clear();properties.SetColor("_Color",Color.white);
        properties.SetColor("_OutlineColor",Color.Lerp(color,Color.white,.45f));
        properties.SetFloat("_OutlineWidth",2.5f);actor.portrait.SetPropertyBlock(properties);
        bool moving=(destination-point).sqrMagnitude>.035f;
        actor.destination.enabled=moving;
        if(moving)
        {
            actor.destination.transform.position=root.transform.TransformPoint(destination+Vector3.up*.025f);
            actor.destination.transform.localScale=Vector3.one*(.26f+.07f*Mathf.Sin(time*6f));
            Tint(actor.destination,color);
        }
        for(int i=0;i<actor.sparkles.Length;i++)
        {
            var sparkle=actor.sparkles[i];sparkle.enabled=reward>0||speed>.15f;
            float angle=time*(1.1f+reward)+i*Mathf.PI/3;
            sparkle.transform.localPosition=new Vector3(Mathf.Cos(angle)*(.48f+reward*.22f),.18f+Mathf.Repeat(time*.55f+i*.17f,1)*(.5f+reward),Mathf.Sin(angle)*.38f);
            sparkle.transform.localRotation=Quaternion.Euler(45,time*80+i*30,45);
            sparkle.transform.localScale=Vector3.one*(.035f+reward*.045f);
            Tint(sparkle,Color.Lerp(color,new Color(1,.87f,.45f),i%2));
        }
    }
    public void Render()
    {
        expired.Clear();
        foreach(var pair in actors)if(pair.Value.generation!=generation)expired.Add(pair.Key);
        foreach(object key in expired){Release(actors[key].root);actors.Remove(key);}
        EndEffects();camera.Render();
    }
    Mesh Own(Mesh mesh)
    {
        mesh.RecalculateNormals();mesh.RecalculateBounds();
#if DITTOCHES_PORTABLE_PREVIEW
        // Imported scene shaders are unavailable in the legacy player; give opaque geometry readable depth.
        var normals=mesh.normals;var colors=new Color[normals.Length];Vector3 sun=new Vector3(-.4f,1,-.3f).normalized;
        for(int i=0;i<colors.Length;i++)colors[i]=Color.white*(.56f+.44f*Mathf.Max(0,Vector3.Dot(normals[i],sun)));
        mesh.colors=colors;
#endif
        resources.Add(mesh);return mesh;
    }
    Mesh Prism(int sides,float angle)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int i=0;i<sides;i++)
        {
            float a=(angle+i*360f/sides)*Mathf.Deg2Rad,b=(angle+(i+1)*360f/sides)*Mathf.Deg2Rad;
            Vector3 p=new Vector3(Mathf.Cos(a),.5f,Mathf.Sin(a)),q=new Vector3(Mathf.Cos(b),.5f,Mathf.Sin(b));
            int n=vertices.Count;vertices.Add(Vector3.up*.5f);vertices.Add(q);vertices.Add(p);
            triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
            n=vertices.Count;vertices.Add(p);vertices.Add(q);vertices.Add(q-Vector3.up);vertices.Add(p-Vector3.up);
            triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
        }
        var mesh=new Mesh{name="Arena prism"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);return Own(mesh);
    }
    Mesh Box()
    {
        Vector3[] corners={new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};
        int[] faces={0,3,2,1,5,6,7,4,4,7,3,0,1,2,6,5,3,7,6,2,4,0,1,5};
        var vertices=new Vector3[24];var triangles=new int[36];
        for(int f=0;f<6;f++){for(int i=0;i<4;i++)vertices[f*4+i]=corners[faces[f*4+i]];int n=f*4,t=f*6;triangles[t]=n;triangles[t+1]=n+1;triangles[t+2]=n+2;triangles[t+3]=n;triangles[t+4]=n+2;triangles[t+5]=n+3;}
        return Own(new Mesh{name="Arena block",vertices=vertices,triangles=triangles});
    }
    Mesh Billboard()
    {
        return Own(new Mesh{name="Arena billboard",vertices=new[]{new Vector3(-.65f,-.65f,0),new Vector3(.65f,-.65f,0),new Vector3(.65f,.65f,0),new Vector3(-.65f,.65f,0)},uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)},triangles=new[]{0,2,1,0,3,2}});
    }
    Mesh Ring()
    {
        const int segments=48;
        var vertices=new Vector3[segments*2];var triangles=new int[segments*6];
        for(int i=0;i<segments;i++)
        {
            float angle=i*Mathf.PI*2/segments;Vector3 p=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            vertices[i*2]=p;vertices[i*2+1]=p*.84f;
            int n=i*2,next=((i+1)%segments)*2,t=i*6;
            triangles[t]=n;triangles[t+1]=next;triangles[t+2]=n+1;
            triangles[t+3]=n+1;triangles[t+4]=next;triangles[t+5]=next+1;
        }
        return Own(new Mesh{name="Tactician ring",vertices=vertices,triangles=triangles});
    }
    static void Release(UnityEngine.Object value){if(value==null)return;GameObject node=value as GameObject;if(node!=null)node.SetActive(false);if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
    public void Dispose()
    {
        if(disposed)return;disposed=true;camera.targetTexture=null;target.Release();Release(target);Release(root);
        foreach(var resource in resources)Release(resource);actors.Clear();portraits.Clear();
    }
}

/// <summary>A release is a click only when its press began on the same target.</summary>
public sealed class ArenaPointer
{
    int pressed=-1;
    public bool HasPress { get { return pressed>=0; } }
    public int Released { get; private set; } = -1;
    public void Reset(){pressed=-1;Released=-1;}
    public void Update(int target,bool down,bool up,bool dragging,bool blocked)
    {
        Released=-1;
        if(blocked||dragging){pressed=-1;return;}
        if(down)pressed=target;
        if(up){if(target>=0&&pressed==target)Released=target;pressed=-1;}
    }
}
