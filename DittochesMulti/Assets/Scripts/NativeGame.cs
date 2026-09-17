using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame : MonoBehaviour
{
    [Serializable] private sealed class UnitSave { public string id; public int star; public int[] items; }
    [Serializable] private sealed class LootOrbSave { public float x, y; public int rarity, rewardType, amount; }
    [Serializable] private sealed class SoloSave
    {
        public int version, difficulty, legend, gold, hp, level, xp, round;
        public bool shopLocked;
        public UnitSave[] board, bench;
        public string[] shop;
        public int[] inventory;
        public LootOrbSave[] lootOrbs;
    }

    private sealed class UnitDef
    {
        public string id, name, sprite, role; public int cost;
        public UnitDef(string id, string name, int cost, string sprite, string role) { this.id=id; this.name=name; this.cost=cost; this.sprite=sprite; this.role=role; }
    }
    private sealed class Unit { public UnitDef def; public int star=1; public readonly List<int> items=new List<int>(); public Unit(UnitDef def) { this.def=def; } }
    private sealed class UnitRef { public bool boardArea; public int index; public Unit unit; }
    private sealed class UnitMeta { public string attr,family;public float hp,atk,speed;public int range;public UnitMeta(string a,string f,float h,float at,float s,int r){attr=a;family=f;hp=h;atk=at;speed=s;range=r;} }
    private sealed class TraitEntry { public string category,key,name; public int count; public TraitEntry(string category,string key,int count){this.category=category;this.key=key;this.name=category+" · "+key;this.count=count;} }
    private sealed class SkillMeta { public float startMana,maxMana,power;public SkillMeta(float start,float max,float ratio){startMana=start;maxMana=max;power=ratio;} }
    private sealed class Fighter
    {
        public Unit unit; public bool enemy, dead; public Vector2 pos, renderPos, renderVelocity; public float hp, maxHp, shield, mana, maxMana=100, cooldown, stun, hitFlash, healFlash, shieldFlash, attackFlash, skillFlash, attackScale=1f, damageDone, healingDone, shieldingDone; public int casts;
    }
    private sealed class LootOrb { public Vector2 pos; public int rarity, rewardType, amount; }
    private sealed class CombatPopup { public Vector2 pos; public string text; public Color color; public float life; }

    private static readonly UnitDef[] Roster = {
        new UnitDef("koromon","코로몬",1,"Koromon","전사"),new UnitDef("tsunomon","뿔몬",1,"Tunomon","사수"),new UnitDef("mochimon","모티몬",1,"Mochimon","탱커"),new UnitDef("tanemon","시드몬",1,"Tanemon","지원"),new UnitDef("pyocomon","어니몬",1,"Pyocomon","마법사"),new UnitDef("tokomon","토코몬",1,"Tokomon","지원"),
        new UnitDef("agumon","아구몬",2,"Agumon","전사"),new UnitDef("gabumon","파피몬",2,"Gabumon","사수"),new UnitDef("tentomon","텐타몬",2,"Tentomon","탱커"),new UnitDef("palmon","팔몬",2,"Palmon","지원"),new UnitDef("piyomon","피요몬",2,"Piyomon","마법사"),new UnitDef("patamon","파닥몬",2,"Patamon","지원"),
        new UnitDef("togemon","니드몬",3,"Togemon","탱커"),new UnitDef("garurumon","가루몬",3,"Garurumon","전사"),new UnitDef("greymon","그레이몬",3,"Greymon","탱커"),new UnitDef("kabuterimon","캅테리몬",3,"Kabuterimon","마법사"),new UnitDef("angemon","엔젤몬",3,"Angemon","전사"),new UnitDef("birdramon","버드라몬",3,"Birdramon","사수"),
        new UnitDef("metalgreymon","메탈그레이몬",4,"Metal_Greymon","사수"),new UnitDef("weregarurumon","워가루몬",4,"Were_Garurumon","전사"),new UnitDef("lilimon","릴리몬",4,"Lilimon","지원"),new UnitDef("holyangemon","홀리엔젤몬",4,"Holy_Angemon","마법사"),new UnitDef("atlur","아트라캅테리몬",4,"Atlur_Kabuterimon","탱커"),new UnitDef("garudamon","가루다몬",4,"Garudamon","사수"),
        new UnitDef("herakle","헤라클레스캅테리몬",5,"Herakle_Kabuterimon","탱커"),new UnitDef("hououmon","페닉스몬",5,"Hououmon","마법사"),new UnitDef("wargreymon","워그레이몬",5,"War_Greymon","전사"),new UnitDef("metalgarurumon","메탈가루몬",5,"Metal_Garurumon","사수"),new UnitDef("rosemon","로제몬",5,"Rosemon","지원"),new UnitDef("seraphimon","세라피몬",5,"Seraphimon","마법사")
    };
    private static readonly Dictionary<string,UnitDef> RosterById=Roster.ToDictionary(unit=>unit.id);
    private static readonly Dictionary<string,string> OriginalNames=new Dictionary<string,string>{{"koromon","비트버드"},{"tsunomon","프리즈마이트"},{"mochimon","모스바이트"}};
    private static readonly Dictionary<string,string> OriginalSprites=new Dictionary<string,string>{{"koromon","ArtVariants/Original/Bitbud-v1"},{"tsunomon","ArtVariants/Original/Prismite-v1"},{"mochimon","ArtVariants/Original/Mossbyte-v1"}};
    private static readonly Dictionary<string,string> FanUnitSprites=new Dictionary<string,string>{
        {"koromon","ArtVariants/LicensedFanArt/Koromon-unit-v2"},{"tsunomon","ArtVariants/LicensedFanArt/Tsunomon-unit-v2"},{"mochimon","ArtVariants/LicensedFanArt/Mochimon-unit-v2"},
        {"tanemon","ArtVariants/LicensedFanArt/Tanemon-unit-v2"},{"pyocomon","ArtVariants/LicensedFanArt/Pyocomon-unit-v2"},{"tokomon","ArtVariants/LicensedFanArt/Tokomon-unit-v2"},
        {"agumon","ArtVariants/LicensedFanArt/Agumon-unit-v2"},{"gabumon","ArtVariants/LicensedFanArt/Gabumon-unit-v2"}
    };
    private static readonly Dictionary<string,string> SkillNames=new Dictionary<string,string>{
        {"koromon","용기 펄스"},{"tsunomon","프리즘 볼트"},{"mochimon","데이터 장벽"},{"tanemon","생장 신호"},{"pyocomon","버스트 깃털"},{"tokomon","희망 파동"},
        {"agumon","화염 압축"},{"gabumon","빙결 탄환"},{"tentomon","전도성 갑각"},{"palmon","회복 덩굴"},{"piyomon","나선 화염"},{"patamon","상승 기류"},
        {"togemon","가시 요새"},{"garurumon","극속 돌진"},{"greymon","대지 강타"},{"kabuterimon","전자 포격"},{"angemon","빛의 단죄"},{"birdramon","홍염 연사"},
        {"metalgreymon","기가 포격"},{"weregarurumon","월광 난무"},{"lilimon","꽃가루 재생"},{"holyangemon","천공 폭발"},{"atlur","초중량 방벽"},{"garudamon","진홍 폭풍"},
        {"herakle","절대 갑주"},{"hououmon","불멸의 태양"},{"wargreymon","용왕 연격"},{"metalgarurumon","빙하 탄막"},{"rosemon","생명의 정원"},{"seraphimon","세라프 심판"}
    };
    private const string SaveKey="multiSoloStateV1";
    private static readonly string[] Legends={"비트몬","코로몬","토코몬","어니몬"};
    private static readonly string[] LegendSprites={"ArtVariants/Legends/Bitmon-v1","ArtVariants/LicensedFanArt/Koromon-v1","ArtVariants/LicensedFanArt/Tokomon-unit-v2","ArtVariants/LicensedFanArt/Pyocomon-unit-v2"};
    private static readonly string[] Difficulties={"쉬움","보통","어려움"};
    private static readonly string[] RivalNames={"태일","매튜","소라","미나","리키","한솔","나리"};
    private static readonly float[] DifficultyScale={.88f,1f,1.12f};
    private static readonly int[,] ShopOdds={{100,0,0,0,0},{100,0,0,0,0},{75,25,0,0,0},{55,30,15,0,0},{40,35,23,2,0},{25,40,28,7,0},{18,30,35,16,1},{15,20,32,28,5},{10,15,25,35,15}};
    private static readonly string[] ItemNames={"공격 데이터","크롬디지조이드 조각","순수 에너지","생명 파편","용기의 문장","우정의 문장","사랑의 문장","성실의 문장","지식의 문장","희망의 문장","순수의 문장","빛의 문장","기적의 캡슐","운명의 캡슐","자석 제거기"};
    private static readonly string[] ItemIcons={"✦","⬡","✧","❋","☀","∞","♥","✚","◈","✴","❀","✵","⬢","◆","↩"};
    private static readonly string[] ItemDescriptions={"공격력 +12% · 다른 재료와 장착 시 자동 합성","체력 +120 · 다른 재료와 장착 시 자동 합성","공격 속도 +12% · 다른 재료와 장착 시 자동 합성","체력 +150 · 다른 재료와 장착 시 자동 합성","공격력 +40%","공격력 +22% · 공격 속도 +25% · 체력 +80","공격력 +18% · 공격 속도 +18%","공격력 +20% · 체력 +120","체력 +250 · 방어 강화","체력 +180 · 공격 속도 +15%","체력 +500 · 재생 강화","공격 속도 +35% · 스킬 강화","체력 +200 · 공격 속도 +20%","체력 +650 · 시작 보호막","유닛의 모든 장비를 보관함으로 회수하는 1회용 소모품"};
    private static readonly int[,] ItemRecipes={{4,5,6,7},{5,8,9,10},{6,9,11,12},{7,10,12,13}};
    private readonly Unit[] board=new Unit[28], bench=new Unit[9];
    private readonly UnitDef[] shop=new UnitDef[5];
    private readonly Dictionary<string,int> pool=new Dictionary<string,int>();
    private readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
    private Texture2D arenaBackground, lobbyBackground, hexTexture;
    private bool lobby=true, battling, win;
    private int difficulty=1, legend, artPack, gold, hp=100, level=1, xp, round=1, selectedBench=-1, selectedBoard=-1, selectedItem=-1;
    private float battleProgress;
    private float battleStartedAt, battleTimeRemaining;
    private float resultNoticeUntil;
    private string battleText="전투 준비", lastReward="", lastCombatSummary="";
    private string currentOpponent="";
    private readonly List<Fighter> fighters=new List<Fighter>();
    private readonly List<LootOrb> lootOrbs=new List<LootOrb>();
    private readonly List<CombatPopup> combatPopups=new List<CombatPopup>();
    private readonly List<int> inventory=new List<int>();
    private Unit inspectedUnit;
    private Vector2 legendPos=new Vector2(785,690), legendTarget=new Vector2(785,690), legendVelocity;
    private bool shopLocked, showCarousel, showRecipeGuide;
    private int recipeFocus=-1;
    private float recipeGuideUntil;
    private string activeTooltip="";
    private float tooltipUntil, guiScale=1f;
    private int scoutedRival=-1;
    private readonly Unit[] scoutBoard=new Unit[28];
    private TraitEntry traitFocus;
    private float traitGuideUntil;
    private bool observedLobby=true;
    private float screenTransitionUntil;
    private readonly float[] shopHover=new float[5], benchHover=new float[9];
    private int dragSource=-1;
    private bool dragFromBoard, draggingUnit;
    private Vector2 dragStart;
    private UnitDef[] carouselUnits=new UnitDef[3]; private int[] carouselItems=new int[3];
    private GUIStyle title, header, label, small, center, button, card, selectedStyle;
    private readonly Color bg=new Color(.012f,.022f,.04f), panel=new Color(.025f,.052f,.082f), accent=new Color(.93f,.72f,.28f);

    // The copied solo game is launched explicitly by MultiLauncher.
    private static void Boot()
    {
        if (FindAnyObjectByType<NativeGame>() != null) return;
        GameObject root=new GameObject("File Island Native Game");
        DontDestroyOnLoad(root); root.AddComponent<NativeGame>();
    }

    private void Awake()
    {
        Application.targetFrameRate=60; Screen.sleepTimeout=SleepTimeout.NeverSleep;
        arenaBackground=Resources.Load<Texture2D>("UI/file-island-arena-v1");
        lobbyBackground=Resources.Load<Texture2D>("UI/file-island-lobby-v2");
        difficulty=PlayerPrefs.GetInt("multiSoloDifficulty",1); legend=PlayerPrefs.GetInt("multiSoloLegend",0); artPack=PlayerPrefs.GetInt("multiSoloArtPack",0);
        if(!Load())ResetGame();
        observedLobby=lobby;
    }

    private void Update()
    {
        if(observedLobby!=lobby){observedLobby=lobby;screenTransitionUntil=Time.unscaledTime+.34f;}
        if(lobby||battling||Input.touchCount==0)return;Touch touch=Input.GetTouch(0);if(touch.phase!=TouchPhase.Began&&touch.phase!=TouchPhase.Moved)return;
        Vector2 point=new Vector2(touch.position.x/guiScale,(Screen.height-touch.position.y)/guiScale);TryMoveLegend(point);
    }

    private void InitPool() { int[] sizes={0,39,26,21,13,10}; pool.Clear(); foreach(UnitDef d in Roster) pool[d.id]=sizes[d.cost]; pool["koromon"]--; }
    private Texture2D MakeTexture(Color color) { Texture2D t=new Texture2D(1,1); t.SetPixel(0,0,color); t.Apply(); return t; }
    private Texture2D MakeHexTexture()
    {
        Texture2D texture=new Texture2D(128,80,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear};for(int y=0;y<80;y++)for(int x=0;x<128;x++){float nx=Mathf.Abs((x+0.5f)/128f-.5f)*2,ny=Mathf.Abs((y+0.5f)/80f-.5f)*2;float alpha=nx<=1f-ny*.48f?1f:0f;texture.SetPixel(x,y,new Color(1,1,1,alpha));}texture.Apply();return texture;
    }
    private void Styles()
    {
        if (title!=null) return;
        title=new GUIStyle(GUI.skin.label){fontSize=32,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter}; title.normal.textColor=accent;
        header=new GUIStyle(GUI.skin.label){fontSize=19,fontStyle=FontStyle.Bold}; header.normal.textColor=Color.white;
        label=new GUIStyle(GUI.skin.label){fontSize=15}; label.normal.textColor=Color.white;
        small=new GUIStyle(label){fontSize=11}; small.normal.textColor=new Color(.62f,.71f,.69f);
        center=new GUIStyle(label){alignment=TextAnchor.MiddleCenter}; button=new GUIStyle(GUI.skin.button){fontSize=14,fontStyle=FontStyle.Bold}; button.normal.textColor=Color.white;
        card=new GUIStyle(GUI.skin.box); card.normal.background=MakeTexture(new Color(.035f,.075f,.115f)); selectedStyle=new GUIStyle(card); selectedStyle.normal.background=MakeTexture(new Color(.25f,.20f,.08f));hexTexture=MakeHexTexture();
    }
    private Texture2D Tex(string name) { if(string.IsNullOrEmpty(name)) return null; Texture2D t; if(!textures.TryGetValue(name,out t)){t=Resources.Load<Texture2D>(name.Contains("/")?name:"Sprites/"+name); textures[name]=t;} return t; }
    private string UnitName(UnitDef d){string value;return artPack==1&&OriginalNames.TryGetValue(d.id,out value)?value:d.name;}
    private string UnitSprite(UnitDef d){string value;if(artPack==1&&OriginalSprites.TryGetValue(d.id,out value))return value;if(artPack==0&&FanUnitSprites.TryGetValue(d.id,out value)&&Tex(value)!=null)return value;return d.sprite;}
    private string SkillName(UnitDef d){string value;return SkillNames.TryGetValue(d.id,out value)?value:"데이터 방출";}
    private UnitMeta Meta(string id){switch(id){
        case "koromon":return new UnitMeta("백신","용형",550,43,.8f,1);case "tsunomon":return new UnitMeta("데이터","야수형",430,43,.85f,3);case "mochimon":return new UnitMeta("백신","곤충형",690,32,.65f,1);case "tanemon":return new UnitMeta("데이터","식물형",460,29,.7f,3);case "pyocomon":return new UnitMeta("백신","조류형",440,36,.75f,3);case "tokomon":return new UnitMeta("백신","천사형",490,28,.7f,3);
        case "agumon":return new UnitMeta("백신","용형",600,48,.8f,1);case "gabumon":return new UnitMeta("데이터","야수형",460,49,.85f,4);case "tentomon":return new UnitMeta("백신","곤충형",760,36,.65f,1);case "palmon":return new UnitMeta("데이터","식물형",490,32,.7f,3);case "piyomon":return new UnitMeta("백신","조류형",530,43,.75f,3);case "patamon":return new UnitMeta("백신","천사형",570,35,.7f,3);
        case "togemon":return new UnitMeta("데이터","식물형",900,46,.7f,1);case "garurumon":return new UnitMeta("데이터","야수형",760,59,.85f,1);case "greymon":return new UnitMeta("백신","용형",1080,60,.7f,1);case "kabuterimon":return new UnitMeta("바이러스","곤충형",690,52,.8f,3);case "angemon":return new UnitMeta("백신","천사형",890,70,.9f,1);case "birdramon":return new UnitMeta("데이터","조류형",670,68,.9f,4);
        case "metalgreymon":return new UnitMeta("바이러스","용형",966,105,.85f,4);case "weregarurumon":return new UnitMeta("데이터","야수형",1323,106,1,1);case "lilimon":return new UnitMeta("데이터","식물형",920,62,.8f,3);case "holyangemon":return new UnitMeta("백신","천사형",989,79,.8f,3);case "atlur":return new UnitMeta("바이러스","곤충형",1370,78,.8f,1);case "garudamon":return new UnitMeta("백신","조류형",1030,97,.95f,4);
        case "herakle":return new UnitMeta("바이러스","곤충형",1782,100,.8f,1);case "hououmon":return new UnitMeta("백신","조류형",1185,107,.9f,4);case "wargreymon":return new UnitMeta("백신","용형",1550,128,1,1);case "metalgarurumon":return new UnitMeta("데이터","야수형",1240,120,1.05f,4);case "rosemon":return new UnitMeta("데이터","식물형",1250,89,.9f,3);case "seraphimon":return new UnitMeta("백신","천사형",1300,118,.9f,3);default:return new UnitMeta("바이러스","악당",700,55,.75f,1);}}
    private SkillMeta Skill(string id){switch(id){
        case "koromon":return new SkillMeta(10,70,.85f);case "tsunomon":return new SkillMeta(15,85,.92f);case "mochimon":return new SkillMeta(20,105,.82f);case "tanemon":return new SkillMeta(30,90,.88f);case "pyocomon":return new SkillMeta(25,75,1.02f);case "tokomon":return new SkillMeta(35,100,.90f);
        case "agumon":return new SkillMeta(15,80,1.00f);case "gabumon":return new SkillMeta(20,95,1.06f);case "tentomon":return new SkillMeta(25,110,.94f);case "palmon":return new SkillMeta(40,105,1.00f);case "piyomon":return new SkillMeta(30,85,1.16f);case "patamon":return new SkillMeta(45,115,1.04f);
        case "togemon":return new SkillMeta(30,120,1.05f);case "garurumon":return new SkillMeta(20,75,1.18f);case "greymon":return new SkillMeta(35,125,1.12f);case "kabuterimon":return new SkillMeta(25,90,1.28f);case "angemon":return new SkillMeta(40,100,1.22f);case "birdramon":return new SkillMeta(30,70,1.24f);
        case "metalgreymon":return new SkillMeta(35,95,1.38f);case "weregarurumon":return new SkillMeta(25,65,1.34f);case "lilimon":return new SkillMeta(50,110,1.30f);case "holyangemon":return new SkillMeta(45,80,1.46f);case "atlur":return new SkillMeta(40,130,1.18f);case "garudamon":return new SkillMeta(35,75,1.42f);
        case "herakle":return new SkillMeta(50,140,1.42f);case "hououmon":return new SkillMeta(60,90,1.62f);case "wargreymon":return new SkillMeta(40,70,1.58f);case "metalgarurumon":return new SkillMeta(45,80,1.55f);case "rosemon":return new SkillMeta(65,120,1.48f);case "seraphimon":return new SkillMeta(55,100,1.70f);default:return new SkillMeta(0,100,1f);}}
    private void OnGUI()
    {
        Styles(); float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);guiScale=Mathf.Max(.01f,scale); Matrix4x4 old=GUI.matrix;
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1)); DrawRect(new Rect(0,0,1920,1080),bg);
        if(lobby) DrawLobby(); else DrawGame();DrawTransientTooltip();if(Time.unscaledTime<screenTransitionUntil){float fade=Mathf.Clamp01((screenTransitionUntil-Time.unscaledTime)/.34f);DrawRect(new Rect(0,0,1920,1080),new Color(.005f,.012f,.025f,fade));}GUI.matrix=old;
    }
    private void DrawTransientTooltip()
    {
        string current=GUI.tooltip??"";if(string.IsNullOrEmpty(current)){activeTooltip="";return;}if(current!=activeTooltip){activeTooltip=current;tooltipUntil=Time.unscaledTime+2.2f;}if(Time.unscaledTime>=tooltipUntil)return;float alpha=Mathf.Clamp01((tooltipUntil-Time.unscaledTime)*3f);Vector2 p=Event.current.mousePosition;Color old=GUI.color;GUI.color=new Color(1,1,1,alpha);GUI.Box(new Rect(Mathf.Min(p.x+18,1510),Mathf.Min(p.y+18,980),370,68),current,card);GUI.color=old;
    }
    private void DrawRect(Rect r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    private bool Btn(Rect r,string text,bool enabled=true){GUI.enabled=enabled;bool hit=GUI.Button(r,text,button);GUI.enabled=true;return hit;}
    private void Portrait(Rect r,string sprite,string glyph=""){Texture2D t=Tex(sprite);if(t)GUI.DrawTexture(r,t,ScaleMode.ScaleToFit,true);else GUI.Label(r,glyph,title);}

    private void DrawLobby()
    {
        if(lobbyBackground!=null){GUI.DrawTexture(new Rect(0,0,1920,1080),lobbyBackground,ScaleMode.ScaleAndCrop,true);DrawRect(new Rect(0,0,1920,1080),new Color(.005f,.018f,.038f,.62f));}else DrawRect(new Rect(0,0,1920,1080),bg);
        DrawRect(new Rect(0,0,1920,94),new Color(.01f,.025f,.05f,.96f));DrawRect(new Rect(0,90,1920,4),accent);GUI.Label(new Rect(54,14,560,28),"FILE ISLAND / SOLO LEAGUE",small);GUI.Label(new Rect(54,38,560,42),"디지몬 오토 아레나",header);if(Btn(new Rect(1625,22,245,48),"← 모드 선택으로")){FindAnyObjectByType<MultiLauncher>().ReturnToMulti();return;}
        DrawRect(new Rect(62,140,470,820),new Color(panel.r,panel.g,panel.b,.96f));DrawRect(new Rect(62,140,5,820),accent);GUI.Label(new Rect(95,174,400,28),"MATCH SETTINGS",header);GUI.Label(new Rect(95,214,400,52),"나만의 속도로 즐기는\n1인 파일 아일랜드 리그",label);
        GUI.Label(new Rect(95,298,400,28),"난이도",header);for(int i=0;i<3;i++){Rect r=new Rect(95+i*132,338,118,58);GUI.Box(r,GUIContent.none,i==difficulty?selectedStyle:card);if(Btn(new Rect(r.x+4,r.y+4,r.width-8,r.height-8),Difficulties[i]))difficulty=i;}GUI.Label(new Rect(95,410,400,44),difficulty==0?"AI 전투력 88% · 처음 플레이 추천":difficulty==1?"표준 전투력 · 기본 난이도":"AI 전투력 112% · 숙련자 추천",small);
        GUI.Label(new Rect(95,490,400,28),"캐릭터 버전",header);if(Btn(new Rect(95,530,190,52),artPack==0?"◆ 디지몬 버전":"◇ 디지몬 버전")){artPack=0;PlayerPrefs.SetInt("multiSoloArtPack",artPack);PlayerPrefs.Save();}if(Btn(new Rect(300,530,190,52),artPack==1?"◆ 오리지널":"◇ 오리지널")){artPack=1;PlayerPrefs.SetInt("multiSoloArtPack",artPack);PlayerPrefs.Save();}
        GUI.Label(new Rect(95,630,400,28),"현재 설정",header);GUI.Box(new Rect(95,670,395,92),GUIContent.none,selectedStyle);GUI.Label(new Rect(115,682,355,30),Difficulties[difficulty]+" · "+(artPack==0?"디지몬":"오리지널"),label);GUI.Label(new Rect(115,717,355,28),"플레이어 1명 + AI 테이머 7명",small);
        if(Btn(new Rect(95,812,395,74),"솔로 리그 입장")){lobby=false;Save();}if(Btn(new Rect(95,900,395,38),"새 게임으로 초기화")){ResetGame();lobby=false;}
        GUI.Label(new Rect(585,150,1240,35),"LITTLE LEGEND",header);GUI.Label(new Rect(585,188,1240,32),"전리품을 회수하고 전장을 함께 누빌 파트너를 선택하세요.",small);
        for(int i=0;i<4;i++){Rect r=new Rect(585+i*310,252,280,488);GUI.Box(r,GUIContent.none,i==legend?selectedStyle:card);DrawRect(new Rect(r.x,r.y,r.width,5),i==legend?accent:new Color(.16f,.30f,.38f));Portrait(new Rect(r.x+34,r.y+36,212,245),LegendSprites[i],"⌁");GUI.Label(new Rect(r.x+15,r.y+300,250,38),Legends[i],header);GUI.Label(new Rect(r.x+20,r.y+350,240,52),i==legend?"선택된 전설이":"전장 파트너",center);if(Btn(new Rect(r.x+35,r.y+418,210,48),i==legend?"✓ 선택됨":"선택"))legend=i;}
        GUI.Box(new Rect(585,780,1210,125),GUIContent.none,card);GUI.Label(new Rect(615,797,1150,30),"전설이는 능력치에 영향을 주지 않습니다.",header);GUI.Label(new Rect(615,837,1150,42),"자유롭게 전장을 이동하고 전리품 구슬을 회수하며, 승리 연출을 함께합니다.",small);
    }

    private void DrawGame(){EnsureArena();HandleArenaPointer();HandleUnitDrag();HandleHotkeys();DrawTop();DrawLeft();DrawBoard();DrawRight();DrawBench();DrawShop();DrawSelectionGhost();if(showCarousel)DrawCarousel();if(showRecipeGuide&&Time.unscaledTime<recipeGuideUntil)DrawRecipeGuide();else if(showRecipeGuide)showRecipeGuide=false;if(traitFocus!=null&&Time.unscaledTime<traitGuideUntil)DrawTraitGuide();else if(traitFocus!=null)traitFocus=null;if(hp<=0&&!battling)DrawGameOver();}
    private Rect BenchRect(int index){EnsureArena();return arena.BenchRect(index);}
    private void HandleUnitDrag()
    {
        Event e=Event.current;if(e==null||battling||showCarousel||hp<=0||scoutedRival>=0)return;Vector2 p=e.mousePosition;
        if(e.type==EventType.MouseDown&&e.button==0){dragSource=-1;for(int i=0;i<board.Length;i++){int row=i/7+4,col=i%7;if(board[i]!=null&&arena.HitCell(p)==i+28){dragSource=i;dragFromBoard=true;break;}}if(dragSource<0)for(int i=0;i<bench.Length;i++)if(bench[i]!=null&&arena.HitBench(p)==i){dragSource=i;dragFromBoard=false;break;}dragStart=p;draggingUnit=false;if(dragSource>=0)e.Use();}
        if(e.type==EventType.MouseDrag&&dragSource>=0&&Vector2.Distance(dragStart,p)>8f){draggingUnit=true;e.Use();}
        if(e.type==EventType.MouseUp&&e.button==0){if(draggingUnit&&dragSource>=0){DropDraggedUnit(p);e.Use();}dragSource=-1;draggingUnit=false;}
    }
    private void DropDraggedUnit(Vector2 p)
    {
        for(int i=0;i<board.Length;i++){if(arena.HitCell(p)!=i+28)continue;if(!dragFromBoard&&board[i]==null&&board.Count(u=>u!=null)>=level)return;Unit moving=dragFromBoard?board[dragSource]:bench[dragSource],other=board[i];board[i]=moving;if(dragFromBoard)board[dragSource]=other;else bench[dragSource]=other;selectedBoard=selectedBench=-1;inspectedUnit=moving;Save();return;}
        for(int i=0;i<bench.Length;i++){if(arena.HitBench(p)!=i)continue;Unit moving=dragFromBoard?board[dragSource]:bench[dragSource],other=bench[i];bench[i]=moving;if(dragFromBoard)board[dragSource]=other;else bench[dragSource]=other;selectedBoard=selectedBench=-1;inspectedUnit=moving;Save();return;}
    }
    private void HandleHotkeys()
    {
        Event e=Event.current;if(e==null||e.type!=EventType.KeyDown||showCarousel||hp<=0)return;
        if(e.keyCode==KeyCode.D&&gold>=2){gold-=2;RollShop();e.Use();return;}if(e.keyCode==KeyCode.F&&gold>=4&&level<9){gold-=4;AddXp(4);Save();e.Use();return;}if(e.keyCode==KeyCode.Space&&!battling&&board.Any(u=>u!=null)){StartRoundAction();e.Use();return;}if(e.keyCode==KeyCode.Escape){selectedBench=selectedBoard=selectedItem=-1;inspectedUnit=null;e.Use();}
    }
    private void StartRoundAction(){if(battling)return;if(RoundType()=="초밥집")OpenCarousel();else if(board.Any(u=>u!=null))StartCoroutine(Battle());}
    private void DrawGameOver()
    {
        DrawRect(new Rect(0,0,1920,1080),new Color(.01f,.025f,.04f,.88f));GUI.Box(new Rect(540,235,840,600),GUIContent.none,card);GUI.Label(new Rect(650,285,620,60),"리그 도전 종료",title);GUI.Label(new Rect(650,365,620,42),$"최종 기록 · {RoundLabel()} 라운드",header);GUI.Label(new Rect(650,420,620,72),"전장을 정비하고 새 리그에 다시 도전할 수 있습니다.\n캐릭터 버전과 난이도 선택은 그대로 유지됩니다.",center);GUI.Label(new Rect(650,520,620,36),lastCombatSummary,center);
        if(Btn(new Rect(650,625,290,70),"같은 설정으로 재도전")){ResetGame();lobby=false;}if(Btn(new Rect(980,625,290,70),"솔로 로비로")){lobby=true;}if(Btn(new Rect(650,720,620,55),"멀티 모드 선택으로")){FindAnyObjectByType<MultiLauncher>().ReturnToMulti();}
    }
    private void DrawTop()
    {
        DrawRect(new Rect(0,0,1920,92),new Color(.012f,.027f,.052f,.98f));DrawRect(new Rect(0,88,1920,4),new Color(accent.r,accent.g,accent.b,.7f));
        float sweep=Mathf.Repeat(Time.unscaledTime*145f,2060f)-70f;DrawRect(new Rect(sweep,88,70,4),new Color(1f,.92f,.58f,.82f));
        GUI.Label(new Rect(28,12,360,26),"FILE ISLAND",small);GUI.Label(new Rect(28,34,360,42),"DIGITAL AUTO ARENA",header);
        GUI.Box(new Rect(785,8,350,76),GUIContent.none,selectedStyle);GUI.Label(new Rect(815,12,290,24),RoundType().ToUpperInvariant(),center);GUI.Label(new Rect(815,34,290,42),RoundLabel(),title);
        Stat(430,"HP",hp.ToString());Stat(600,"GOLD",gold+" G");Stat(1190,"LEVEL",level==9?"LV.9 MAX":$"LV.{level}");Stat(1360,"XP",level==9?"MAX":$"{xp} / {NeedXp()}");
        MiniBar(new Rect(430,77,130,4),Mathf.Clamp01(hp/100f),new Color(.32f,.92f,.48f));MiniBar(new Rect(1360,77,150,4),level==9?1f:Mathf.Clamp01((float)xp/NeedXp()),new Color(.30f,.66f,1f));
        if(Btn(new Rect(1640,20,110,48),"로비",!battling))lobby=true;if(Btn(new Rect(1765,20,120,48),"종료"))Application.Quit();
    }
    private void Stat(float x,string key,string value){GUI.Label(new Rect(x,15,180,25),key,small);GUI.Label(new Rect(x,38,180,38),value,header);}
    private void MiniBar(Rect r,float value,Color color){DrawRect(r,new Color(.08f,.11f,.14f));DrawRect(new Rect(r.x,r.y,r.width*Mathf.Clamp01(value),r.height),color);}
    private void DrawLeft()
    {
        DrawRect(new Rect(16,108,248,620),new Color(panel.r,panel.g,panel.b,.94f));DrawRect(new Rect(16,108,4,620),accent);GUI.Label(new Rect(38,124,205,28),"TEAM TRAITS",header);
        List<TraitEntry> traits=TeamTraits();for(int i=0;i<traits.Count&&i<7;i++)Trait(160+i*42,traits[i]);
        GUI.Label(new Rect(38,477,205,25),"ITEM BENCH",header);
        for(int i=0;i<inventory.Count&&i<12;i++){Rect ir=new Rect(38+(i%4)*50,512+(i/4)*48,43,41);int item=inventory[i];GUI.Box(ir,GUIContent.none,i==selectedItem?selectedStyle:card);Event e=Event.current;if(e!=null&&e.type==EventType.MouseDown&&e.button==1&&ir.Contains(e.mousePosition)){traitFocus=null;recipeFocus=item;showRecipeGuide=true;recipeGuideUntil=Time.unscaledTime+2.8f;e.Use();}else if(GUI.Button(ir,new GUIContent(ItemIcons[item],ItemNames[item]+"\n"+ItemDescriptions[item]),button))SelectInventoryItem(i);}
        string itemHelp=selectedItem>=0&&selectedItem<inventory.Count?ItemNames[inventory[selectedItem]]+" 선택됨":"좌클릭 합성/장착 · 우클릭 조합법";GUI.Label(new Rect(35,660,215,42),itemHelp,small);
    }
    private int RoleCount(string role){return board.Where(u=>u!=null&&u.def.role==role).Select(u=>u.def.id).Distinct().Count();}
    private List<TraitEntry> TeamTraits()
    {
        Unit[] unique=board.Where(u=>u!=null).GroupBy(u=>u.def.id).Select(g=>g.First()).ToArray();List<TraitEntry> result=new List<TraitEntry>();
        foreach(var group in unique.GroupBy(u=>Meta(u.def.id).attr))result.Add(new TraitEntry("속성",group.Key,group.Count()));foreach(var group in unique.GroupBy(u=>Meta(u.def.id).family))result.Add(new TraitEntry("계열",group.Key,group.Count()));
        string[] roles={"전사","탱커","사수","마법사","지원"};foreach(string role in roles){int count=unique.Count(u=>u.def.role==role);if(count>0||result.Count<2)result.Add(new TraitEntry("역할",role,count));}
        return result.OrderByDescending(t=>TraitTier(t.category,t.count)).ThenByDescending(t=>t.count).ThenBy(t=>t.name).Take(7).ToList();
    }
    private int TraitTier(string category,int count){int second=category=="계열"?3:4,third=category=="계열"?4:6;return count>=third?3:count>=second?2:count>=2?1:0;}
    private int TraitTarget(string category,int count){int[] values=category=="계열"?new[]{2,3,4}:new[]{2,4,6};foreach(int value in values)if(count<value)return value;return values[2];}
    private Color TraitColor(int tier){return tier>=3?new Color(1f,.72f,.18f):tier==2?new Color(.62f,.72f,.90f):tier==1?new Color(.72f,.43f,.22f):new Color(.18f,.25f,.32f);}
    private void Trait(float y,TraitEntry entry){int tier=TraitTier(entry.category,entry.count);Rect r=new Rect(34,y,212,36);GUI.Box(r,GUIContent.none,tier>0?selectedStyle:card);DrawRect(new Rect(34,y,5,36),TraitColor(tier));GUI.Label(new Rect(48,y+3,135,29),entry.name,small);GUI.Label(new Rect(181,y+3,52,29),entry.count+" / "+TraitTarget(entry.category,entry.count),small);Event e=Event.current;if(e!=null&&e.type==EventType.MouseDown&&e.button==1&&r.Contains(e.mousePosition)){showRecipeGuide=false;traitFocus=entry;traitGuideUntil=Time.unscaledTime+3.5f;e.Use();}}
    private void DrawTraitGuide()
    {
        int tier=TraitTier(traitFocus.category,traitFocus.count);Rect r=new Rect(270,145,475,155);GUI.Box(r,GUIContent.none,card);DrawRect(new Rect(r.x,r.y,6,r.height),TraitColor(tier));GUI.Label(new Rect(r.x+22,r.y+12,430,30),traitFocus.name+"  ·  "+(tier==0?"비활성":""+tier+"단계 활성"),header);GUI.Label(new Rect(r.x+22,r.y+49,430,28),$"현재 {traitFocus.count}명  ·  구간 {TraitThresholdText(traitFocus.category)}",small);GUI.Label(new Rect(r.x+22,r.y+78,430,54),TraitEffectText(traitFocus.category,traitFocus.key,tier),small);GUI.Label(new Rect(r.x+335,r.y+131,115,18),"3.5초 후 닫힘",small);
    }
    private string TraitThresholdText(string category){return category=="계열"?"2 / 3 / 4":"2 / 4 / 6";}
    private string TraitEffectText(string category,string key,int tier)
    {
        if(tier==0)return "2명을 배치하면 첫 단계 효과가 활성화됩니다.";string[] values=tier==1?new[]{"8%","10%","12%"}:tier==2?new[]{"16%","20%","25%"}:new[]{"28%","35%","45%"};
        if(category=="속성")return key=="백신"?"백신 디지몬 최대 체력 +"+values[1]:key=="데이터"?"데이터 디지몬 공격 속도 +"+values[0]:"바이러스 디지몬 공격력 +"+values[0];
        if(category=="계열")return key=="용형"?"용형 디지몬 공격력 +"+values[0]:key=="야수형"?"야수형 디지몬 공격 속도 +"+values[0]:key=="곤충형"?"곤충형 디지몬 최대 체력 +"+values[1]:key=="천사형"?"천사형 디지몬 시작 마나 +"+(tier*10):"식물형 디지몬 회복 효과 +"+values[2];
        return key=="탱커"?"탱커 최대 체력 +"+values[1]:key=="전사"?"전사 공격력과 흡혈 +"+values[0]:key=="사수"?"사수 공격 속도 +"+values[0]:key=="마법사"?"마법사 스킬 피해 +"+values[0]:"아군 생존력과 회복 효과 +"+values[0];
    }
    private Rect BoardCellRect(int row,int col){EnsureArena();return arena.CellRect(row,col);}
    private Vector2 BoardPoint(Vector2 boardPos){EnsureArena();return arena.Project(TacticalArena.CellWorld(boardPos.x,boardPos.y)+Vector3.up*1.4f)-new Vector2(52,5);}

    private void DrawHex(Rect rect,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,hexTexture);GUI.color=old;}
    private void DrawBoard(){DrawPerspectiveBoard();}
    private void ClickBoard(int idx)
    {
        if(selectedItem>=0&&board[idx]!=null){Equip(board[idx]);return;}
        if(selectedBench>=0){Unit temp=board[idx];if(temp==null&&board.Count(u=>u!=null)>=level)return;board[idx]=bench[selectedBench];bench[selectedBench]=temp;selectedBench=-1;selectedBoard=-1;Save();return;}
        if(selectedBoard>=0){if(selectedBoard==idx){selectedBoard=-1;return;}Unit temp=board[idx];board[idx]=board[selectedBoard];board[selectedBoard]=temp;selectedBoard=-1;Save();return;}
        legendTarget=LegacyPoint(TacticalArena.CellWorld(idx%7,idx/7+4));if(board[idx]!=null){selectedBoard=idx;inspectedUnit=board[idx];}
    }
    private void UpdateLegendInput()
    {
        if(battling||showCarousel)return;Event e=Event.current;if(e==null||(e.type!=EventType.MouseDown&&e.type!=EventType.MouseDrag))return;
        if(TryMoveLegend(e.mousePosition))e.Use();
    }
    private bool TryMoveLegend(Vector2 point)
    {
        if(battling||lobby||showCarousel||hp<=0||scoutedRival>=0||selectedBoard>=0||selectedBench>=0||selectedItem>=0||draggingUnit)return false;
        EnsureArena();Vector3 world;
        if(!arena.GroundPoint(point,out world)||Mathf.Abs(world.x)>5||Mathf.Abs(world.z)>4.6f)return false;
        legendTarget=LegacyPoint(world);return true;
    }

    private void DrawFighter(Fighter f){DrawPerspectiveFighter(f);}
    private void DrawCombatPopups()
    {
        foreach(CombatPopup popup in combatPopups){float progress=1-Mathf.Clamp01(popup.life/.72f),alpha=Mathf.Clamp01(popup.life*3f);Color old=GUI.color;GUI.color=new Color(popup.color.r,popup.color.g,popup.color.b,alpha);GUI.Label(new Rect(popup.pos.x-35,popup.pos.y-25-progress*34,90,26),popup.text,center);GUI.color=old;}
    }
    private void AddCombatPopup(Fighter target,string value,Color color){combatPopups.Add(new CombatPopup{pos=BoardPoint(target.renderPos)+new Vector2(52,5),text=value,color=color,life=.72f});}
    private Color RoleColor(string role){if(role=="탱커")return new Color(.25f,.65f,1f);if(role=="전사")return new Color(1f,.42f,.2f);if(role=="사수")return new Color(1f,.8f,.2f);if(role=="마법사")return new Color(.72f,.35f,1f);return new Color(.25f,1f,.55f);}
    private void DrawUnit(Rect r,Unit u,bool enemy){Color role=RoleColor(u.def.role);DrawRect(new Rect(r.x+22,r.y+57,72,3),new Color(role.r,role.g,role.b,.85f));Texture2D t=Tex(UnitSprite(u.def));if(t)GUI.DrawTexture(new Rect(r.x+22,r.y-16,72,72),t,ScaleMode.ScaleToFit,true);GUI.Label(new Rect(r.x+4,r.y+43,r.width-8,18),new string('★',u.star)+"  "+UnitName(u.def),center);if(u.items.Count>0)GUI.Label(new Rect(r.x+72,r.y+3,40,38),string.Concat(u.items.Select(i=>ItemIcons[i])),small);}
    private void DrawRight()
    {
        DrawRect(new Rect(1368,108,532,620),new Color(panel.r,panel.g,panel.b,.96f));DrawRect(new Rect(1896,108,4,620),accent);if(inspectedUnit!=null)DrawUnitDetail();else{GUI.Label(new Rect(1390,125,480,30),"PLAYER SCOUT",header);string[] names=(new[]{"나의 테이머"}).Concat(RivalNames).ToArray();
        for(int i=0;i<8;i++){float y=170+i*55;Rect row=new Rect(1380,y,500,46);bool fighting=battling&&i>0&&names[i]==currentOpponent,viewing=!battling&&(i==0?scoutedRival<0:scoutedRival==i-1);int playerHp=i==0?hp:Mathf.Max(0,100-round*i);GUI.Box(row,GUIContent.none,viewing||fighting?selectedStyle:card);if(fighting)DrawRect(new Rect(1380,y,5,46),new Color(1f,.38f,.20f,.75f+Mathf.Sin(Time.unscaledTime*6f)*.2f));GUI.Label(new Rect(1395,y+7,35,25),(i+1).ToString(),label);GUI.Label(new Rect(1440,y+7,250,25),(fighting?"⚔ ":viewing?"◆ ":"")+names[i],label);GUI.Label(new Rect(1760,y+7,100,24),playerHp+" HP",small);MiniBar(new Rect(1440,y+35,420,4),playerHp/100f,i==0?new Color(.32f,.92f,.48f):new Color(.90f,.35f,.28f));if(!battling&&!showCarousel&&GUI.Button(row,GUIContent.none,GUIStyle.none)){if(i==0)scoutedRival=-1;else ShowRivalBoard(i-1);}}
        GUI.Label(new Rect(1390,635,480,48),battling?"상대 전력을 분석 중입니다.":scoutedRival>=0?"나의 테이머를 눌러 내 전장으로 복귀":"테이머를 눌러 배치 전장 관전",small);}
        bool canSell=!showCarousel&&(selectedBench>=0||(!battling&&selectedBoard>=0));if(Btn(new Rect(1645,674,225,42),battling?"대기석 유닛 판매":"선택 유닛 판매",canSell))SellSelectedUnit();
    }
    private void ShowRivalBoard(int rivalIndex)
    {
        scoutedRival=Mathf.Clamp(rivalIndex,0,RivalNames.Length-1);inspectedUnit=null;selectedBoard=selectedBench=selectedItem=-1;Array.Clear(scoutBoard,0,scoutBoard.Length);int stage=round<=3?1:2+(round-4)/7,count=Mathf.Clamp(stage+2,3,7),maxCost=Mathf.Clamp(stage,1,5);UnitDef[] choices=Roster.Where(d=>d.cost<=maxCost).ToArray();int[] slots={3,9,11,15,19,22,26};
        for(int i=0;i<count;i++){UnitDef definition=choices[(scoutedRival*7+round*3+i*5)%choices.Length];Unit unit=new Unit(definition);unit.star=stage>=5&&i<2?2:stage>=3&&i==0?2:1;scoutBoard[slots[i]]=unit;}
    }
    private void DrawBench(){DrawPerspectiveBench();}
    private void DrawSelectionGhost()
    {
        Unit held=draggingUnit?(dragFromBoard?board[dragSource]:bench[dragSource]):selectedBench>=0?bench[selectedBench]:selectedBoard>=0?board[selectedBoard]:null;if(held==null&&selectedItem<0)return;Vector2 p=Event.current.mousePosition;Rect hint=new Rect(Mathf.Clamp(p.x+22,290,1260),Mathf.Clamp(p.y-76,115,765),150,68);Color old=GUI.color;GUI.color=new Color(1,1,1,draggingUnit?.88f:.76f);GUI.Box(hint,GUIContent.none,selectedStyle);
        if(held!=null){Portrait(new Rect(hint.x+5,hint.y-3,65,62),UnitSprite(held.def));GUI.Label(new Rect(hint.x+67,hint.y+7,78,22),UnitName(held.def),small);GUI.Label(new Rect(hint.x+67,hint.y+31,78,22),"배치할 칸 선택",small);}else if(selectedItem<inventory.Count){int item=inventory[selectedItem];GUI.Label(new Rect(hint.x+8,hint.y+7,40,40),ItemIcons[item],header);GUI.Label(new Rect(hint.x+45,hint.y+7,100,22),ItemNames[item],small);GUI.Label(new Rect(hint.x+45,hint.y+31,100,22),"유닛에 장착",small);}GUI.color=old;
    }
    private void SelectInventoryItem(int index)
    {
        if(index<0||index>=inventory.Count)return;if(selectedItem==index){selectedItem=-1;return;}
        if(selectedItem>=0&&selectedItem<inventory.Count&&inventory[selectedItem]<=3&&inventory[index]<=3)
        {
            int first=inventory[selectedItem],second=inventory[index],completed=ItemRecipes[first,second];int high=Mathf.Max(selectedItem,index),low=Mathf.Min(selectedItem,index);inventory.RemoveAt(high);inventory.RemoveAt(low);inventory.Add(completed);selectedItem=-1;lastReward=ItemIcons[completed]+" "+ItemNames[completed]+" 보관함 합성 완료";Save();return;
        }
        selectedItem=index;selectedBench=-1;selectedBoard=-1;
    }
    private void DrawRecipeGuide()
    {
        float h=recipeFocus<=3?270:150,x=270,y=470;GUI.Box(new Rect(x,y,470,h),GUIContent.none,card);DrawRect(new Rect(x,y,5,h),accent);GUI.Label(new Rect(x+22,y+12,425,28),"빠른 조합표",header);
        if(recipeFocus>=0&&recipeFocus<ItemNames.Length)GUI.Label(new Rect(x+22,y+43,425,28),ItemIcons[recipeFocus]+"  "+ItemNames[recipeFocus]+" · "+ItemDescriptions[recipeFocus],small);
        if(recipeFocus<=3)
        {
            for(int ingredient=0;ingredient<4;ingredient++){int result=ItemRecipes[Mathf.Clamp(recipeFocus,0,3),ingredient];float rowY=y+76+ingredient*45;GUI.Box(new Rect(x+18,rowY,434,38),GUIContent.none,selectedStyle);GUI.Label(new Rect(x+30,rowY+5,410,28),ItemIcons[recipeFocus]+" + "+ItemIcons[ingredient]+"  →  "+ItemIcons[result]+" "+ItemNames[result],small);}
        }
        else
        {
            bool found=false;for(int a=0;a<4;a++)for(int b=a;b<4;b++)if(ItemRecipes[a,b]==recipeFocus){found=true;GUI.Box(new Rect(x+18,y+79,434,44),GUIContent.none,selectedStyle);GUI.Label(new Rect(x+30,y+86,410,28),ItemIcons[a]+" "+ItemNames[a]+" + "+ItemIcons[b]+" "+ItemNames[b]+"  →  "+ItemIcons[recipeFocus],small);}
            if(!found)GUI.Label(new Rect(x+22,y+82,425,35),"조합 장비가 아닙니다.",small);
        }
        GUI.Label(new Rect(x+270,y+h-28,175,20),"2.8초 후 자동으로 닫힘",small);
    }
    private void SellSelectedUnit()
    {
        Unit unit=selectedBench>=0?bench[selectedBench]:selectedBoard>=0?board[selectedBoard]:null;if(unit==null)return;int copies=(int)Mathf.Pow(3,unit.star-1);gold+=unit.def.cost*copies;pool[unit.def.id]+=copies;
        if(unit.items.Count>0){inventory.AddRange(unit.items);lastReward=UnitName(unit.def)+" 판매 · 장비 "+unit.items.Count+"개 보관함 반환";unit.items.Clear();}else lastReward=UnitName(unit.def)+" 판매";
        if(selectedBench>=0)bench[selectedBench]=null;else board[selectedBoard]=null;if(inspectedUnit==unit)inspectedUnit=null;selectedBench=selectedBoard=-1;Save();
    }
    private void DrawUnitDetail()
    {
        Unit u=inspectedUnit;UnitMeta m=Meta(u.def.id);SkillMeta skill=Skill(u.def.id);if(Btn(new Rect(1825,120,55,38),"×")){inspectedUnit=null;return;}Portrait(new Rect(1390,145,150,145),UnitSprite(u.def));GUI.Label(new Rect(1560,150,250,34),UnitName(u.def),header);GUI.Label(new Rect(1560,190,280,26),new string('★',u.star)+$"  ·  {u.def.cost}코스트",label);GUI.Label(new Rect(1560,225,280,25),$"{m.attr} · {m.family} · {u.def.role}",label);GUI.Label(new Rect(1560,255,300,25),$"마나 {skill.startMana:0}/{skill.maxMana:0} · 스킬 계수 {skill.power:0.00}",small);
        float mult=Mathf.Pow(1.8f,u.star-1);GUI.Label(new Rect(1390,305,470,28),"유닛 능력치",header);DetailStat(1390,342,"체력",Mathf.RoundToInt(m.hp*mult).ToString());DetailStat(1510,342,"공격력",Mathf.RoundToInt(m.atk*Mathf.Pow(1.5f,u.star-1)).ToString());DetailStat(1630,342,"공속",m.speed.ToString("0.00"));DetailStat(1750,342,"사거리",m.range.ToString());
        GUI.Label(new Rect(1390,405,470,28),"기여 시너지",header);int attrCount=SynergyCount(m.attr),familyCount=SynergyCount(m.family),roleCount=board.Count(x=>x!=null&&x.def.role==u.def.role);SynergyLine(1390,442,m.attr,attrCount,2,"같은 속성 2명부터 속성 효과 활성화");SynergyLine(1390,493,m.family,familyCount,2,"같은 계열 2/4명에서 전투 보너스 강화");SynergyLine(1390,544,u.def.role,roleCount,2,RoleDescription(u.def.role));
        GUI.Label(new Rect(1390,595,120,24),"개별 스킬",small);GUI.Label(new Rect(1510,588,350,38),SkillDescription(u.def),label);GUI.Label(new Rect(1390,640,120,24),"장착 장비",small);for(int i=0;i<u.items.Count;i++){int item=u.items[i];GUI.Button(new Rect(1510+i*165,633,155,38),new GUIContent(ItemIcons[item]+" "+ItemNames[item],ItemNames[item]+"\n"+ItemDescriptions[item]),button);}
    }
    private void DetailStat(float x,float y,string key,string value){GUI.Box(new Rect(x,y,108,52),GUIContent.none,card);GUI.Label(new Rect(x+5,y+4,98,18),key,small);GUI.Label(new Rect(x+5,y+22,98,25),value,center);}
    private int SynergyCount(string trait){return board.Where(x=>x!=null).GroupBy(x=>x.def.id).Select(g=>Meta(g.First().def.id)).Count(m=>m.attr==trait||m.family==trait);}
    private void SynergyLine(float x,float y,string name,int count,int need,string desc){GUI.Box(new Rect(x,y,470,44),GUIContent.none,count>=need?selectedStyle:card);GUI.Label(new Rect(x+12,y+4,120,20),$"{name}  {count}/{need}",label);GUI.Label(new Rect(x+142,y+5,315,32),desc,small);}
    private string RoleDescription(string role){if(role=="탱커")return "2명: 방어력과 생존력 증가";if(role=="전사")return "2명: 공격력과 흡혈 증가";if(role=="사수")return "2명: 공격 속도 증가";if(role=="마법사")return "2명: 스킬 피해 증가";return "2명: 아군 회복과 보호 효과 증가";}
    private string SkillDescription(UnitDef d){SkillMeta s=Skill(d.id);int count=SkillTargets(d.id);string prefix=SkillName(d)+$" · {s.maxMana:0} 마나 · ";if(d.role=="탱커")return prefix+$"자가 회복 + 보호막";if(d.role=="전사")return prefix+$"{count}명 강화 강타";if(d.role=="사수")return prefix+$"{count}명 연속 사격";if(d.role=="마법사")return prefix+$"반경 {SkillRadius(d.id):0.0} 폭발 + 기절";return prefix+$"아군 {count}명 회복 + 보호막";}
    private int SkillTargets(string id){switch(id){case "koromon":case "mochimon":case "agumon":case "tentomon":case "togemon":case "greymon":return 1;case "tsunomon":case "tanemon":case "pyocomon":case "tokomon":case "gabumon":case "palmon":case "piyomon":case "patamon":case "garurumon":case "kabuterimon":case "angemon":return 2;case "birdramon":case "metalgreymon":case "weregarurumon":case "lilimon":case "holyangemon":case "atlur":case "garudamon":return 3;default:return 4;}}
    private float SkillRadius(string id){switch(id){case "pyocomon":return 1.15f;case "piyomon":return 1.3f;case "kabuterimon":return 1.45f;case "holyangemon":return 1.7f;case "hououmon":return 2.25f;case "seraphimon":return 2f;default:return 1.6f;}}
    private void ClickBench(int idx)
    {
        if(selectedItem>=0&&bench[idx]!=null){Equip(bench[idx]);return;}
        if(battling){selectedBench=bench[idx]!=null?(selectedBench==idx?-1:idx):-1;return;}
        if(selectedBoard>=0){Unit temp=bench[idx];bench[idx]=board[selectedBoard];board[selectedBoard]=temp;selectedBoard=-1;selectedBench=-1;Save();return;}
        if(selectedBench>=0){if(selectedBench==idx){selectedBench=-1;return;}Unit temp=bench[idx];bench[idx]=bench[selectedBench];bench[selectedBench]=temp;selectedBench=-1;Save();return;}
        if(bench[idx]!=null){selectedBench=idx;inspectedUnit=bench[idx];}
    }
    private void Equip(Unit unit)
    {
        if(selectedItem<0||selectedItem>=inventory.Count)return;int item=inventory[selectedItem];
        if(item==14){if(unit.items.Count==0)return;inventory.RemoveAt(selectedItem);inventory.AddRange(unit.items);unit.items.Clear();selectedItem=-1;lastReward="자석 제거기 사용 · 장비를 보관함으로 회수했습니다";Save();return;}
        if(item<=3){int partner=unit.items.FindIndex(x=>x<=3);if(partner>=0){int completed=ItemRecipes[unit.items[partner],item];unit.items[partner]=completed;inventory.RemoveAt(selectedItem);selectedItem=-1;lastReward=ItemNames[completed]+" 자동 합성 완료";Save();return;}}
        if(unit.items.Count>=2)return;unit.items.Add(item);inventory.RemoveAt(selectedItem);selectedItem=-1;Save();
    }
    private void DrawShop()
    {
        DrawRect(new Rect(0,838,1920,242),new Color(.012f,.030f,.052f,.99f));DrawRect(new Rect(0,838,1920,3),accent);GUI.Label(new Rect(40,855,230,30),"SHOP · 디지몬 모집",header);
        int interest=Mathf.Min(5,gold/10);GUI.Label(new Rect(270,842,800,22),ShopOddsText(),small);GUI.Label(new Rect(40,1028,205,24),$"이자 +{interest}G  ·  다음 기본 수입 {5+interest}G",small);
        if(Btn(new Rect(40,895,145,42),"새로고침 2G  [D]",gold>=2)){gold-=2;RollShop();}if(Btn(new Rect(40,943,145,42),"경험치 +4  [F]",gold>=4&&level<9)){gold-=4;AddXp(4);Save();}if(Btn(new Rect(40,991,145,34),shopLocked?"◆ 잠금 유지":"◇ 상점 잠금")){shopLocked=!shopLocked;Save();}
        for(int i=0;i<5;i++){Rect r=new Rect(270+i*245,865,225,165);bool hovered=r.Contains(Event.current.mousePosition);if(Event.current.type==EventType.Repaint)shopHover[i]=Mathf.MoveTowards(shopHover[i],hovered?1f:0f,Time.unscaledDeltaTime*7f);GUI.Box(r,GUIContent.none,shopHover[i]>.03f?selectedStyle:card);UnitDef d=shop[i];if(d!=null){Color rarity=CostColor(d.cost);DrawRect(new Rect(r.x,r.y,r.width,4+shopHover[i]*2),rarity);float lift=shopHover[i]*6f,scale=shopHover[i]*5f;Color old=GUI.color;if(gold<d.cost)GUI.color=new Color(.62f,.65f,.68f,.82f);Portrait(new Rect(r.x+8-scale*.5f,r.y+8-lift,105+scale,100+scale),UnitSprite(d));GUI.color=old;GUI.Label(new Rect(r.x+112,r.y+12,105,28),UnitName(d),label);GUI.Label(new Rect(r.x+112,r.y+44,105,22),d.role,small);GUI.Label(new Rect(r.x+112,r.y+72,105,25),d.cost+" G",header);GUI.Label(new Rect(r.x+12,r.y+112,95,25),"POOL "+pool[d.id],small);if(Btn(new Rect(r.x+112,r.y+108,100,42),"구매",gold>=d.cost)&&Buy(i))Save();}else GUI.Label(r,"판매 완료",center);}
        string action=RoundType()=="초밥집"?RoundLabel()+" 선택창 열기":RoundLabel()+" 전투 시작";if(Btn(new Rect(1530,875,330,135),battling?"전투 진행 중":action+"  [SPACE]",!battling&&(RoundType()=="초밥집"||board.Any(u=>u!=null))))StartRoundAction();
    }
    private string ShopOddsText(){return $"LV.{level} 배치 {board.Count(u=>u!=null)}/{level}  ·  상점 확률  1G {ShopOdds[level-1,0]}%  2G {ShopOdds[level-1,1]}%  3G {ShopOdds[level-1,2]}%  4G {ShopOdds[level-1,3]}%  5G {ShopOdds[level-1,4]}%";}
    private Color CostColor(int cost){if(cost==1)return new Color(.55f,.62f,.68f);if(cost==2)return new Color(.20f,.72f,.42f);if(cost==3)return new Color(.22f,.55f,1f);if(cost==4)return new Color(.72f,.30f,1f);return new Color(1f,.70f,.18f);}
    private void OpenCarousel(){int cost=Mathf.Clamp(2+(round-4)/7,1,5);UnitDef[] choices=Roster.Where(d=>d.cost==cost&&pool[d.id]>0).OrderBy(_=>UnityEngine.Random.value).ToArray();for(int i=0;i<3;i++){carouselUnits[i]=choices.Length>0?choices[i%choices.Length]:null;carouselItems[i]=UnityEngine.Random.Range(0,4);}showCarousel=true;}
    private void DrawCarousel()
    {
        DrawRect(new Rect(360,220,1200,610),new Color(.025f,.065f,.075f,.98f));GUI.Label(new Rect(520,255,880,55),RoundLabel()+" · 디지타마 광장",title);GUI.Label(new Rect(520,315,880,32),"유닛과 장비 묶음 하나를 선택하세요",center);
        for(int i=0;i<3;i++){Rect r=new Rect(440+i*360,380,320,310);GUI.Box(r,GUIContent.none,card);UnitDef d=carouselUnits[i];if(d!=null)Portrait(new Rect(r.x+75,r.y+20,170,145),UnitSprite(d));GUI.Label(new Rect(r.x+20,r.y+170,280,32),d==null?"골드 보급":UnitName(d),header);GUI.Label(new Rect(r.x+20,r.y+207,280,30),ItemIcons[carouselItems[i]]+" "+ItemNames[carouselItems[i]],center);if(Btn(new Rect(r.x+45,r.y+250,230,45),"선택"))ChooseCarousel(i);}
    }
    private void ChooseCarousel(int index){UnitDef d=carouselUnits[index];if(d!=null){if(!GrantUnit(d))gold+=d.cost;}else gold+=Mathf.Max(2,round/7);inventory.Add(carouselItems[index]);showCarousel=false;round++;if(!shopLocked)RollShop();Save();}

    private IEnumerator Battle()
    {
        battling=true;battleProgress=0;battleStartedAt=Time.unscaledTime;battleTimeRemaining=28f;combatPopups.Clear();selectedBoard=-1;scoutedRival=-1;SetupBattle();battleText=RoundType()=="크립"?"악당 디지몬 토벌 중":currentOpponent+" 테이머와 전투 중";
        float elapsed=0,maxTime=28f;
        while(elapsed<maxTime&&fighters.Any(f=>!f.dead&&!f.enemy)&&fighters.Any(f=>!f.dead&&f.enemy)){
            float dt=Time.deltaTime;elapsed+=dt;battleTimeRemaining=Mathf.Max(0,maxTime-elapsed);battleProgress=Mathf.Clamp01(elapsed/maxTime);UpdateCombat(dt);yield return null;
        }
        win=fighters.Any(f=>!f.dead&&!f.enemy)&&!fighters.Any(f=>!f.dead&&f.enemy);
        if(elapsed>=maxTime)win=fighters.Where(f=>!f.enemy).Sum(f=>Mathf.Max(0,f.hp))>=fighters.Where(f=>f.enemy).Sum(f=>Mathf.Max(0,f.hp));
        battleProgress=1;yield return new WaitForSeconds(.65f);
        int income=5+Mathf.Min(5,gold/10);AddXp(2);
        if(win){gold+=income;if(RoundType()=="크립")CreateLootOrbs();lastReward=RoundType()=="크립"?$"수입 {income}G · 전리품 구슬 {lootOrbs.Count}개 생성":$"수입 {income}G · 승리";}else{int stage=round<=3?1:2+(round-4)/7;int damage=RoundType()=="크립"?stage+1:Mathf.CeilToInt((2+Mathf.Max(0,stage-2)*2+fighters.Count(f=>f.enemy&&!f.dead))*(1+Mathf.Max(0,stage-2)*.1f));hp=Mathf.Max(0,hp-damage);gold+=income;lastReward=$"체력 -{damage} · 수입 {income}G";}
        Fighter damageAce=fighters.Where(f=>!f.enemy).OrderByDescending(f=>f.damageDone).FirstOrDefault(),healAce=fighters.Where(f=>!f.enemy).OrderByDescending(f=>f.healingDone).FirstOrDefault(),shieldAce=fighters.Where(f=>!f.enemy).OrderByDescending(f=>f.shieldingDone).FirstOrDefault();lastCombatSummary=damageAce==null?"":$"전투 통계 · 피해 1위 {UnitName(damageAce.unit.def)} {damageAce.damageDone:0} · 스킬 {fighters.Where(f=>!f.enemy).Sum(f=>f.casts)}회"+(healAce!=null&&healAce.healingDone>0?$" · 회복 {healAce.healingDone:0}":"")+(shieldAce!=null&&shieldAce.shieldingDone>0?$" · 보호막 {shieldAce.shieldingDone:0}":"");battleText=(win?"승리 · ":"패배 · ")+lastReward;resultNoticeUntil=Time.unscaledTime+4.5f;round++;if(!shopLocked)RollShop();fighters.Clear();battling=false;Save();
    }
    private void SetupBattle()
    {
        fighters.Clear();
        for(int i=0;i<board.Length;i++)if(board[i]!=null){int col=i%7,row=i/7+4;fighters.Add(CreateFighter(board[i],false,new Vector2(col,row)));}
        int stage=round<=3?1:2+(round-4)/7,step=round<=3?round:1+(round-4)%7;
        if(RoundType()=="크립")
        {
            currentOpponent="";int enemyCount=stage==1?step:Mathf.Min(7,stage+1);UnitDef enemy=EnemyForRound();
            for(int i=0;i<enemyCount;i++){Unit copy=new Unit(enemy);copy.star=Mathf.Clamp(1+round/9,1,3);fighters.Add(CreateFighter(copy,true,new Vector2(i%7,(i/7)+1)));}
        }
        else SetupRivalTeam(stage,step);
    }
    private void SetupRivalTeam(int stage,int step)
    {
        int rivalIndex=Mathf.Abs((round-4)*3+stage)%RivalNames.Length;currentOpponent=RivalNames[rivalIndex];int enemyCount=Mathf.Clamp(stage+1,3,7),maxCost=Mathf.Clamp(stage,1,5);
        string[] preferredRoles={"전사","사수","마법사","지원","탱커","마법사","전사"};string preferred=preferredRoles[rivalIndex];
        List<UnitDef> choices=Roster.Where(d=>d.cost<=maxCost).OrderByDescending(d=>d.role==preferred).ThenBy(_=>UnityEngine.Random.value).ToList();
        int[] columns={3,2,4,1,5,0,6};int[] rows={1,1,1,2,2,2,2};
        for(int i=0;i<enemyCount;i++){Unit unit=new Unit(choices[i%choices.Count]);unit.star=stage>=5&&i<2?2:stage>=3&&i==0?2:1;if(stage>=4&&i<Mathf.Min(2,stage-3))unit.items.Add(UnityEngine.Random.Range(0,4));fighters.Add(CreateFighter(unit,true,new Vector2(columns[i],rows[i])));}
    }
    private Fighter CreateFighter(Unit unit,bool enemy,Vector2 pos)
    {
        float scale=1f;if(enemy){int stage=round<=3?1:2+(round-4)/7,step=round<=3?round:1+(round-4)%7;scale=RoundType()=="크립"?(stage==1?.38f+.12f*step:1f+.18f*(stage-2)):DifficultyScale[difficulty]*(1+round*.035f);}UnitMeta meta=Meta(unit.def.id);float itemHp=unit.items.Sum(ItemHealth),healthBonus=0;if(!enemy){if(unit.def.role=="탱커")healthBonus+=TierBonus(TraitLevel("역할","탱커"),.10f,.20f,.35f);if(unit.def.role=="지원")healthBonus+=TierBonus(TraitLevel("역할","지원"),.08f,.16f,.28f);if(meta.attr=="백신")healthBonus+=TierBonus(TraitLevel("속성","백신"),.10f,.20f,.35f);if(meta.family=="곤충형")healthBonus+=TierBonus(TraitLevel("계열","곤충형"),.10f,.20f,.35f);}float health=(meta.hp+itemHp)*Mathf.Pow(1.8f,unit.star-1)*scale*(1+healthBonus);
        SkillMeta skill=Skill(unit.def.id);float startMana=skill.startMana;if(!enemy&&meta.family=="천사형")startMana+=TraitLevel("계열","천사형")*10;return new Fighter{unit=unit,enemy=enemy,pos=pos,renderPos=pos,hp=health,maxHp=health,mana=Mathf.Min(skill.maxMana,startMana),maxMana=skill.maxMana,attackScale=scale,cooldown=UnityEngine.Random.Range(.1f,.55f)};
    }
    private int TraitCount(string category,string key){return board.Where(u=>u!=null).GroupBy(u=>u.def.id).Select(g=>g.First()).Count(u=>category=="역할"?u.def.role==key:category=="속성"?Meta(u.def.id).attr==key:Meta(u.def.id).family==key);}
    private int TraitLevel(string category,string key){return TraitTier(category,TraitCount(category,key));}
    private float TierBonus(int tier,float first,float second,float third){return tier>=3?third:tier==2?second:tier==1?first:0;}
    private bool RoleActive(string role){return TraitLevel("역할",role)>0;}
    private Fighter SelectTarget(Fighter attacker){IEnumerable<Fighter> enemies=fighters.Where(x=>!x.dead&&x.enemy!=attacker.enemy);if(attacker.unit.def.role=="마법사")return enemies.OrderBy(x=>x.hp/x.maxHp).ThenBy(x=>Vector2.Distance(attacker.pos,x.pos)).FirstOrDefault();if(attacker.unit.def.role=="사수")return enemies.OrderBy(x=>Vector2.Distance(attacker.pos,x.pos)*(x.unit.def.role=="탱커"?.72f:1f)).FirstOrDefault();return enemies.OrderBy(x=>Vector2.Distance(attacker.pos,x.pos)*(x.unit.def.role=="탱커"?.58f:1f)).FirstOrDefault();}
    private void UpdateCombat(float dt)
    {
        for(int i=combatPopups.Count-1;i>=0;i--){combatPopups[i].life-=dt;if(combatPopups[i].life<=0)combatPopups.RemoveAt(i);}foreach(Fighter f in fighters){f.renderPos=Vector2.SmoothDamp(f.renderPos,f.pos,ref f.renderVelocity,.12f,7f,dt);f.hitFlash=Mathf.Max(0,f.hitFlash-dt);f.healFlash=Mathf.Max(0,f.healFlash-dt);f.shieldFlash=Mathf.Max(0,f.shieldFlash-dt);f.attackFlash=Mathf.Max(0,f.attackFlash-dt);f.skillFlash=Mathf.Max(0,f.skillFlash-dt);if(f.dead)continue;if(f.unit.def.role=="마법사")f.mana=Mathf.Min(f.maxMana,f.mana+2f*dt);else if(f.unit.def.role=="지원")f.mana=Mathf.Min(f.maxMana,f.mana+1.5f*dt);f.cooldown-=dt;f.stun=Mathf.Max(0,f.stun-dt);if(f.stun>0)continue;Fighter target=SelectTarget(f);if(target==null)continue;
            UnitMeta meta=Meta(f.unit.def.id);float distance=Vector2.Distance(f.pos,target.pos),range=Mathf.Lerp(1.05f,2.9f,(meta.range-1)/3f);
            if(distance>range){float moveSpeed=(meta.range>1?.70f:1.0f)*dt;f.pos=Vector2.MoveTowards(f.pos,target.pos,moveSpeed);continue;}
            if(f.cooldown>0)continue;if(f.mana>=f.maxMana){CastSkill(f,target);continue;}float damage=AttackDamage(f);DealDamage(f,target,damage);float manaPerAttack=f.unit.def.role=="탱커"?5:f.unit.def.role=="마법사"?7:f.unit.def.role=="지원"?8:10;f.mana=Mathf.Min(f.maxMana,f.mana+manaPerAttack);f.attackFlash=.35f;float speedBonus=1+f.unit.items.Sum(ItemSpeed);if(!f.enemy){if(f.unit.def.role=="사수")speedBonus+=TierBonus(TraitLevel("역할","사수"),.08f,.16f,.28f);if(meta.attr=="데이터")speedBonus+=TierBonus(TraitLevel("속성","데이터"),.08f,.16f,.28f);if(meta.family=="야수형")speedBonus+=TierBonus(TraitLevel("계열","야수형"),.08f,.16f,.28f);}f.cooldown=1f/(meta.speed*speedBonus);if(!f.enemy&&f.unit.def.role=="전사"){float steal=TierBonus(TraitLevel("역할","전사"),.08f,.16f,.28f);if(steal>0)Heal(f,f,damage*steal);}
        }
    }
    private float AttackDamage(Fighter f){UnitMeta meta=Meta(f.unit.def.id);float bonus=0;if(!f.enemy){if(f.unit.def.role=="전사"||f.unit.def.role=="마법사")bonus+=TierBonus(TraitLevel("역할",f.unit.def.role),.08f,.16f,.28f);if(meta.attr=="바이러스")bonus+=TierBonus(TraitLevel("속성","바이러스"),.08f,.16f,.28f);if(meta.family=="용형")bonus+=TierBonus(TraitLevel("계열","용형"),.08f,.16f,.28f);}return meta.atk*Mathf.Pow(1.5f,f.unit.star-1)*(1+f.unit.items.Sum(ItemAttack))*(1+bonus)*(f.enemy?f.attackScale:1f);}
    private void DealDamage(Fighter source,Fighter target,float damage){float absorbed=Mathf.Min(target.shield,damage);target.shield-=absorbed;damage-=absorbed;float actual=Mathf.Min(target.hp,damage);target.hp-=damage;source.damageDone+=Mathf.Max(0,actual);if(actual>0)AddCombatPopup(target,"-"+Mathf.RoundToInt(actual),new Color(1f,.48f,.28f));if(target.unit.def.role=="탱커")target.mana=Mathf.Min(target.maxMana,target.mana+Mathf.Clamp((damage+absorbed)/target.maxHp*60f,2f,12f));target.hitFlash=.18f;if(target.hp<=0){target.hp=0;target.dead=true;}}
    private void Heal(Fighter source,Fighter target,float amount){if(!source.enemy){UnitMeta meta=Meta(source.unit.def.id);if(meta.family=="식물형")amount*=1+TierBonus(TraitLevel("계열","식물형"),.12f,.25f,.45f);if(source.unit.def.role=="지원")amount*=1+TierBonus(TraitLevel("역할","지원"),.08f,.16f,.28f);}float actual=Mathf.Min(target.maxHp-target.hp,amount);target.hp+=Mathf.Max(0,actual);target.healFlash=.28f;source.healingDone+=Mathf.Max(0,actual);if(actual>1)AddCombatPopup(target,"+"+Mathf.RoundToInt(actual),new Color(.35f,1f,.55f));}
    private void Shield(Fighter source,Fighter target,float amount){float cap=target.maxHp*.5f,actual=Mathf.Min(cap-target.shield,amount);target.shield+=Mathf.Max(0,actual);target.shieldFlash=.35f;source.shieldingDone+=Mathf.Max(0,actual);if(actual>1)AddCombatPopup(target,"+"+Mathf.RoundToInt(actual),new Color(.35f,.75f,1f));}
    private void CastSkill(Fighter caster,Fighter target)
    {
        caster.mana=0;caster.casts++;caster.attackFlash=.35f;caster.skillFlash=.8f;caster.cooldown=.55f;SkillMeta skill=Skill(caster.unit.def.id);float power=AttackDamage(caster)*skill.power;string role=caster.unit.def.role;int count=SkillTargets(caster.unit.def.id);
        if(role=="탱커"){float sustain=caster.maxHp*(.18f+skill.power*.05f);Heal(caster,caster,sustain);Shield(caster,caster,sustain*.65f);return;}
        if(role=="전사"){foreach(Fighter victim in fighters.Where(x=>!x.dead&&x.enemy!=caster.enemy).OrderBy(x=>Vector2.Distance(caster.pos,x.pos)).Take(count).ToArray())DealDamage(caster,victim,power*(victim==target?1.55f:.72f));return;}
        if(role=="사수"){int shot=0;foreach(Fighter victim in fighters.Where(x=>!x.dead&&x.enemy!=caster.enemy).OrderBy(x=>Vector2.Distance(target.pos,x.pos)).Take(count).ToArray()){DealDamage(caster,victim,power*(shot++==0?1f:.65f));}return;}
        if(role=="마법사"){DealDamage(caster,target,power*1.35f);target.stun=Mathf.Max(target.stun,.35f+caster.unit.def.cost*.09f);foreach(Fighter other in fighters.Where(x=>!x.dead&&x.enemy!=caster.enemy&&x!=target&&Vector2.Distance(x.pos,target.pos)<=SkillRadius(caster.unit.def.id)).OrderBy(x=>Vector2.Distance(x.pos,target.pos)).Take(Mathf.Max(0,count-1)).ToArray()){DealDamage(caster,other,power*.58f);other.stun=Mathf.Max(other.stun,.2f+caster.unit.def.cost*.05f);}return;}
        foreach(Fighter ally in fighters.Where(x=>!x.dead&&x.enemy==caster.enemy).OrderBy(x=>x.hp/x.maxHp).Take(count).ToArray()){float healing=power*(ally==caster?1.5f:2.2f);Heal(caster,ally,healing);Shield(caster,ally,healing*.28f);}
    }
    private float ItemHealth(int i){switch(i){case 1:return 120;case 3:return 150;case 5:return 80;case 7:return 120;case 8:return 250;case 9:return 180;case 10:return 500;case 12:return 200;case 13:return 650;default:return 0;}}
    private float ItemAttack(int i){switch(i){case 0:return .12f;case 4:return .4f;case 5:return .22f;case 6:return .18f;case 7:return .2f;default:return 0;}}
    private float ItemSpeed(int i){switch(i){case 2:return .12f;case 5:return .25f;case 6:return .18f;case 9:return .15f;case 11:return .35f;case 12:return .2f;default:return 0;}}
    private void CreateLootOrbs(){int kills=fighters.Count(f=>f.enemy&&f.dead);for(int i=0;i<kills;i++){float roll=UnityEngine.Random.value;if(roll<.42f)continue;int rarity=roll<.72f?0:roll<.92f?1:2;lootOrbs.Add(new LootOrb{rarity=rarity,rewardType=rarity==1?1:(rarity==2&&UnityEngine.Random.value<.55f?2:0),amount=rarity==2?3+Mathf.Max(1,round/7):1+UnityEngine.Random.Range(0,3),pos=new Vector2(UnityEngine.Random.Range(420,1220),UnityEngine.Random.Range(240,650))});}}
    private void CollectOrb(LootOrb orb){legendTarget=orb.pos;if(orb.rewardType==1){int item=UnityEngine.Random.value<.15f?14:UnityEngine.Random.Range(0,4);inventory.Add(item);lastReward=ItemNames[item]+" 획득";}else if(orb.rewardType==2){UnitDef[] choices=Roster.Where(d=>d.cost==Mathf.Clamp(1+round/7,1,4)&&pool[d.id]>0).ToArray();if(choices.Length>0){UnitDef d=choices[UnityEngine.Random.Range(0,choices.Length)];if(GrantUnit(d))lastReward=d.name+" 획득";else{gold+=d.cost;lastReward=d.cost+"G 전환";}}}else{gold+=orb.amount;lastReward=orb.amount+"G 획득";}lootOrbs.Remove(orb);Save();}
    private bool GrantUnit(UnitDef d){int slot=Array.FindIndex(bench,u=>u==null);if(slot>=0){bench[slot]=new Unit(d);pool[d.id]--;Combine(d.id);return true;}List<UnitRef> pair=FindUnits(d.id,1).Take(2).ToList();if(pair.Count<2)return false;pool[d.id]--;MergePurchasedThird(pair[0],pair[1]);Combine(d.id);return true;}
    private string RoundLabel(){int stage=round<=3?1:2+(round-4)/7,step=round<=3?round:1+(round-4)%7;return stage+"-"+step;}
    private string RoundType(){if(round<=3)return "크립";int step=1+(round-4)%7;return step==4?"초밥집":step==7?"크립":"PvP";}
    private UnitDef EnemyForRound(){string id=round<=1?"Kuwagamon":round==2?"Shellmon":round<8?"Devimon":"Etemon";return new UnitDef(id.ToLower(),id,Mathf.Clamp(1+round/5,1,5),id,"적");}
    private int NeedXp(){int[] need={0,2,2,6,10,20,36,60,68,0};return need[Mathf.Clamp(level,1,9)];}
    private void AddXp(int amount){xp+=amount;while(level<9&&xp>=NeedXp()){xp-=NeedXp();level++;}if(level==9)xp=0;}
    private bool HasThree(string id){return board.Concat(bench).Any(u=>u!=null&&u.def.id==id&&u.star==3);}
    private void RollShop(){foreach(UnitDef old in shop)if(old!=null)pool[old.id]++;for(int i=0;i<5;i++){int roll=UnityEngine.Random.Range(0,100),cost=1;for(int c=0;c<5;c++){roll-=ShopOdds[level-1,c];if(roll<0){cost=c+1;break;}}UnitDef[] eligible=Roster.Where(d=>d.cost==cost&&pool[d.id]>0&&!HasThree(d.id)).ToArray();if(eligible.Length==0){shop[i]=null;continue;}int total=eligible.Sum(d=>pool[d.id]),pick=UnityEngine.Random.Range(0,total);UnitDef chosen=eligible[0];foreach(UnitDef d in eligible){pick-=pool[d.id];if(pick<0){chosen=d;break;}}shop[i]=chosen;pool[chosen.id]--;}}
    private bool Buy(int index)
    {
        UnitDef d=shop[index];if(d==null||gold<d.cost)return false;int slot=Array.FindIndex(bench,u=>u==null);
        if(slot<0){List<UnitRef> pair=FindUnits(d.id,1).Take(2).ToList();if(pair.Count<2)return false;gold-=d.cost;shop[index]=null;MergePurchasedThird(pair[0],pair[1]);Combine(d.id);return true;}
        gold-=d.cost;bench[slot]=new Unit(d);shop[index]=null;Combine(d.id);return true;
    }
    private List<UnitRef> FindUnits(string id,int star)
    {
        List<UnitRef> refs=new List<UnitRef>();for(int i=0;i<board.Length;i++)if(board[i]!=null&&board[i].def.id==id&&board[i].star==star)refs.Add(new UnitRef{boardArea=true,index=i,unit=board[i]});for(int i=0;i<bench.Length;i++)if(bench[i]!=null&&bench[i].def.id==id&&bench[i].star==star)refs.Add(new UnitRef{boardArea=false,index=i,unit=bench[i]});return refs;
    }
    private void ClearRef(UnitRef r){if(r.boardArea)board[r.index]=null;else bench[r.index]=null;}
    private void PutRef(UnitRef r,Unit u){if(r.boardArea)board[r.index]=u;else bench[r.index]=u;}
    private void MergePurchasedThird(UnitRef keep,UnitRef consumed){List<int> items=new List<int>(keep.unit.items);items.AddRange(consumed.unit.items);ClearRef(keep);ClearRef(consumed);keep.unit.star++;keep.unit.items.Clear();keep.unit.items.AddRange(items.Take(2));inventory.AddRange(items.Skip(2));PutRef(keep,keep.unit);}
    private void Combine(string id)
    {
        bool changed=true;while(changed){changed=false;for(int star=1;star<3;star++){List<UnitRef> refs=FindUnits(id,star).Take(3).ToList();if(refs.Count<3)continue;UnitRef keep=refs[0];List<int> items=refs.SelectMany(r=>r.unit.items).ToList();foreach(UnitRef r in refs)ClearRef(r);keep.unit.star++;keep.unit.items.Clear();keep.unit.items.AddRange(items.Take(2));inventory.AddRange(items.Skip(2));PutRef(keep,keep.unit);changed=true;break;}}
        if(HasThree(id))for(int i=0;i<5;i++)if(shop[i]!=null&&shop[i].id==id){pool[id]++;shop[i]=null;}
    }
    private void ResetGame(){Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);Array.Clear(shop,0,shop.Length);inventory.Clear();lootOrbs.Clear();gold=0;hp=100;level=1;xp=0;round=1;selectedBench=selectedBoard=selectedItem=-1;shopLocked=false;showCarousel=false;inspectedUnit=null;legendPos=legendTarget=new Vector2(785,690);legendVelocity=Vector2.zero;InitPool();board[3]=new Unit(Roster[0]);RollShop();Save();}

    private static UnitSave SaveUnit(Unit unit){return unit==null?null:new UnitSave{id=unit.def.id,star=unit.star,items=unit.items.ToArray()};}
    private static Unit LoadUnit(UnitSave saved)
    {
        if(saved==null||string.IsNullOrEmpty(saved.id)||!RosterById.TryGetValue(saved.id,out UnitDef definition)||saved.star<1||saved.star>3)return null;
        Unit unit=new Unit(definition){star=saved.star};
        if(saved.items!=null)foreach(int item in saved.items.Take(2))if(item>=0&&item<ItemNames.Length)unit.items.Add(item);
        return unit;
    }

    private void RebuildPool()
    {
        int[] sizes={0,39,26,21,13,10};pool.Clear();foreach(UnitDef definition in Roster)pool[definition.id]=sizes[definition.cost];
        foreach(Unit unit in board.Concat(bench))
            if(unit!=null)pool[unit.def.id]=Mathf.Max(0,pool[unit.def.id]-(int)Mathf.Pow(3,unit.star-1));
        foreach(UnitDef definition in shop)
            if(definition!=null)pool[definition.id]=Mathf.Max(0,pool[definition.id]-1);
    }

    private bool Load()
    {
        if(!PlayerPrefs.HasKey(SaveKey))return false;
        try
        {
            SoloSave saved=JsonUtility.FromJson<SoloSave>(PlayerPrefs.GetString(SaveKey));
            if(saved==null||saved.version!=1||saved.board==null||saved.board.Length!=board.Length||saved.bench==null||saved.bench.Length!=bench.Length||saved.shop==null||saved.shop.Length!=shop.Length)return false;
            difficulty=Mathf.Clamp(saved.difficulty,0,Difficulties.Length-1);
            legend=Mathf.Clamp(saved.legend,0,Legends.Length-1);
            gold=Mathf.Max(0,saved.gold);hp=Mathf.Clamp(saved.hp,0,100);level=Mathf.Clamp(saved.level,1,9);xp=Mathf.Max(0,saved.xp);round=Mathf.Max(1,saved.round);shopLocked=saved.shopLocked;
            for(int i=0;i<board.Length;i++)board[i]=LoadUnit(saved.board[i]);
            for(int i=0;i<bench.Length;i++)bench[i]=LoadUnit(saved.bench[i]);
            for(int i=0;i<shop.Length;i++)shop[i]=string.IsNullOrEmpty(saved.shop[i])?null:(RosterById.TryGetValue(saved.shop[i],out UnitDef definition)?definition:null);
            inventory.Clear();
            if(saved.inventory!=null)inventory.AddRange(saved.inventory.Where(item=>item>=0&&item<ItemNames.Length));
            lootOrbs.Clear();
            if(saved.lootOrbs!=null)foreach(LootOrbSave orb in saved.lootOrbs)
                lootOrbs.Add(new LootOrb{pos=new Vector2(orb.x,orb.y),rarity=Mathf.Clamp(orb.rarity,0,2),rewardType=Mathf.Clamp(orb.rewardType,0,2),amount=Mathf.Max(0,orb.amount)});
            selectedBench=selectedBoard=selectedItem=-1;showCarousel=false;inspectedUnit=null;legendPos=legendTarget=new Vector2(785,690);RebuildPool();return true;
        }
        catch(Exception error)
        {
            Debug.LogWarning("Solo save could not be loaded; starting a new game. "+error.Message);return false;
        }
    }

    private void Save()
    {
        SoloSave saved=new SoloSave{version=1,difficulty=difficulty,legend=legend,gold=gold,hp=hp,level=level,xp=xp,round=round,shopLocked=shopLocked,
            board=board.Select(SaveUnit).ToArray(),bench=bench.Select(SaveUnit).ToArray(),shop=shop.Select(unit=>unit==null?"":unit.id).ToArray(),inventory=inventory.ToArray(),
            lootOrbs=lootOrbs.Select(orb=>new LootOrbSave{x=orb.pos.x,y=orb.pos.y,rarity=orb.rarity,rewardType=orb.rewardType,amount=orb.amount}).ToArray()};
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(saved));PlayerPrefs.SetInt("multiSoloDifficulty",difficulty);PlayerPrefs.SetInt("multiSoloLegend",legend);PlayerPrefs.Save();
    }
}
