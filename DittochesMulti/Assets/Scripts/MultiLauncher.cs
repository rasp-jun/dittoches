#if DITTOCHES_PORTABLE_PREVIEW
using PlayerPrefs = PortablePreviewPrefs;
#endif
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Online state comes exclusively from the dedicated server; the solo game stays isolated.
public sealed partial class MultiLauncher : MonoBehaviour
{
    [Serializable] public class UnitDef { public string id, name, sprite, role; public int cost; }
    [Serializable] public class Catalog { public UnitDef[] units; }
    [Serializable] public class Unit { public string id; public int star, slot; public int[] items; }
    [Serializable] public class Player { public string name; public int rating, hp, gold, level, xp, inventoryRevision; public bool ready; public Unit[] board, bench; public string[] shop; public int[] inventory; }
    [Serializable] public class Fighter { public int key, side, star,slot; public string id; public float x, y, hp, maxHp, shield, mana, maxMana, attackAt, hitAt, stun; public int target; }
    [Serializable] public class Frame { public float time; public Fighter[] units; }
    [Serializable] public class SkillEvent { public int serial,caster,target; public string id; public float started,sx,sy,tx,ty; }
    [Serializable] public class Room { public string id, mode, phase, result, message; public int round, side, ratingDelta; public float remaining,battleDuration; public SkillEvent[] skillEvents; public Player[] players; public Frame[] frames; }
    [Serializable] public class State { public string token, name, queue, error; public int rating, waiting; public Room room; }
    [Serializable] public class Command { public string name, key, mode, action, area, targetArea; public int slot, targetSlot, itemSlot, targetItemSlot, inventoryRevision; }

    string server = "http://127.0.0.1:7777", nickname = "테이머", token = "", notice = "서버에 접속한 뒤 일반 / 랭크 매칭을 시작하세요.";
    string profile = "", selectedArea = "";
    int selectedSlot = -1;
    bool busy, solo, confirmLeave, connectionError;
    int lobbyTab, codexPage, artPack;
    float nextPoll, receivedAt;
    State state;
    Catalog catalog;
    NativeGame soloGame;
    GUIStyle title, heroTitle, text, small, button, compactButton, goldButton, navButton, box, eyebrow, centered, stat, input;
    Texture2D lobbyBackground, lobbyMascot;
    readonly Color navy = new Color(.025f,.043f,.075f), surface = new Color(.045f,.075f,.115f), surface2 = new Color(.065f,.105f,.15f);
    readonly Color gold = new Color(.79f,.63f,.30f), paleGold = new Color(.94f,.84f,.57f), cyan = new Color(.22f,.72f,.79f), muted = new Color(.57f,.65f,.72f);
    readonly System.Collections.Generic.Dictionary<string, Texture2D> textures = new System.Collections.Generic.Dictionary<string, Texture2D>();
    readonly System.Collections.Generic.List<Texture2D> uiTextures = new System.Collections.Generic.List<Texture2D>();
    readonly System.Collections.Generic.Dictionary<string,string> originalNames = new System.Collections.Generic.Dictionary<string,string>{{"koromon","비트버드"},{"tsunomon","프리즈마이트"},{"mochimon","모스바이트"}};
    readonly System.Collections.Generic.Dictionary<string,string> originalSprites = new System.Collections.Generic.Dictionary<string,string>{{"koromon","ArtVariants/Original/Bitbud-v1"},{"tsunomon","ArtVariants/Original/Prismite-v1"},{"mochimon","ArtVariants/Original/Mossbyte-v1"}};
    readonly System.Collections.Generic.Dictionary<string,string> fanUnitSprites = new System.Collections.Generic.Dictionary<string,string>{
        {"koromon","ArtVariants/LicensedFanArt/Koromon-unit-v2"},{"tsunomon","ArtVariants/LicensedFanArt/Tsunomon-unit-v2"},{"mochimon","ArtVariants/LicensedFanArt/Mochimon-unit-v2"},
        {"tanemon","ArtVariants/LicensedFanArt/Tanemon-unit-v2"},{"pyocomon","ArtVariants/LicensedFanArt/Pyocomon-unit-v2"},{"tokomon","ArtVariants/LicensedFanArt/Tokomon-unit-v2"},
        {"agumon","ArtVariants/LicensedFanArt/Agumon-unit-v2"},{"gabumon","ArtVariants/LicensedFanArt/Gabumon-unit-v2"}
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
#if DITTOCHES_PORTABLE_PREVIEW
        if(PortablePreview.TryBoot())return;
#endif
        if (FindAnyObjectByType<MultiLauncher>() != null) return;
        var root = new GameObject("Dittoches Multi");
        DontDestroyOnLoad(root);
        root.AddComponent<MultiLauncher>();
    }

    void Awake()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
        profile = Application.isEditor ? "editor" : "player";
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "--profile") profile = args[i + 1];
        server = PlayerPrefs.GetString("multi.server", server);
        nickname = PlayerPrefs.GetString("multi.name." + profile, nickname);
        catalog = JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("MultiRoster").text);
        artPack = Mathf.Clamp(PlayerPrefs.GetInt("multiSoloArtPack",0),0,1);
        lobbyBackground = Resources.Load<Texture2D>("UI/file-island-lobby-v2");
        if (lobbyBackground == null) lobbyBackground = Resources.Load<Texture2D>("UI/file-island-arena-v1");
        lobbyMascot = Resources.Load<Texture2D>("ArtVariants/LicensedFanArt/Koromon-v1");
        if (lobbyMascot == null) lobbyMascot = Resources.Load<Texture2D>("Sprites/Koromon");
    }

    public void ReturnToMulti()
    {
        if (soloGame != null) soloGame.gameObject.SetActive(false);
        solo = false;
    }

    void EnterSolo()
    {
        token = ""; state = null; solo = true;
        if (soloGame == null)
        {
            var root = new GameObject("Dittoches Solo");
            DontDestroyOnLoad(root);
            soloGame = root.AddComponent<NativeGame>();
        }
        soloGame.gameObject.SetActive(true);
    }

    void Update()
    {
#if DITTOCHES_PORTABLE_PREVIEW
        if(PortablePreview.HasArgument("--online-smoke"))return;
#endif
        if (!solo && token.Length > 0 && !busy && Time.unscaledTime >= nextPoll)
            StartCoroutine(Request("/state", new Command()));
    }

    string AccountKey()
    {
        // Separate guest identity for each server and local test profile.
        string scope;
        using (var hash = System.Security.Cryptography.SHA256.Create())
            scope = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(server + "|" + profile))).Replace("-", "");
        string pref = "multi.key." + scope;
        string key = PlayerPrefs.GetString(pref, "");
        if (key.Length != 64)
        {
            byte[] bytes = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            key = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            PlayerPrefs.SetString(pref, key); PlayerPrefs.Save();
        }
        return key;
    }

    void Connect()
    {
        server = server.Trim().TrimEnd('/'); nickname = nickname.Trim();
        if (!Uri.TryCreate(server, UriKind.Absolute, out Uri uri) || (uri.Scheme != "http" && uri.Scheme != "https") || !string.IsNullOrEmpty(uri.UserInfo))
        { notice = "http://주소:7777 또는 https://주소 형식으로 입력하세요."; return; }
        if (nickname.Length < 1 || nickname.Length > 20) { notice = "닉네임은 1~20자로 입력하세요."; return; }
        PlayerPrefs.SetString("multi.server", server); PlayerPrefs.SetString("multi.name." + profile, nickname); PlayerPrefs.Save();
        StartCoroutine(Request("/login", new Command { name = nickname, key = AccountKey() }));
    }

    IEnumerator Request(string path, Command command)
    {
        busy = true;
        using (var request = new UnityWebRequest(server + path, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(command)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (token.Length > 0) request.SetRequestHeader("Authorization", "Bearer " + token);
            request.timeout = 5;
            yield return request.SendWebRequest();
            State response = null;
            try { response = DecodeState(request.downloadHandler.text); }
            catch (Exception) { /* Network/proxy responses may not be JSON. */ }
            if (request.result != UnityWebRequest.Result.Success || response == null || !string.IsNullOrEmpty(response.error))
            {
                connectionError = true;
                notice = response != null && !string.IsNullOrEmpty(response.error) ? response.error : "서버 연결 실패 · 주소와 서버 실행 상태를 확인하세요. 자동으로 재시도합니다.";
                if (response != null && response.error != null && response.error.Contains("인증이 만료")) { token = ""; state = null; }
            }
            else
            {
                state = response; token = response.token ?? ""; receivedAt = Time.unscaledTime;
                ReconcileEquipmentSelection();
                if (connectionError || path != "/state") notice = path == "/login" ? "서버 접속 완료" : "서버에 연결되었습니다.";
                connectionError = false;
                if (state.room == null || state.room.phase == "finished") { selectedSlot = -1; selectedArea = ""; }
                if (path == "/leave") confirmLeave = false;
            }
        }
        busy = false; nextPoll = Time.unscaledTime + 1;
    }

    void Send(string path, Command command = null)
    {
        if (!busy) StartCoroutine(Request(path, command ?? new Command()));
    }

    static State DecodeState(string json)
    {
        State decoded=JsonUtility.FromJson<State>(json);
        // JsonUtility can materialize JSON null as an empty serializable Room.
        if(decoded!=null&&decoded.room!=null&&string.IsNullOrEmpty(decoded.room.id))decoded.room=null;
        if(decoded!=null&&decoded.room!=null&&decoded.room.players!=null)
        {
            foreach(Player player in decoded.room.players)
            {
                player.inventory=player.inventory??new int[0];player.board=player.board??new Unit[0];player.bench=player.bench??new Unit[0];player.shop=player.shop??new string[0];
                foreach(Unit unit in player.board)unit.items=unit.items??new int[0];
                foreach(Unit unit in player.bench)unit.items=unit.items??new int[0];
            }
        }
        return decoded;
    }

    void Styles()
    {
        if (title != null) return;
        title = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft }; title.normal.textColor = paleGold;
        heroTitle = new GUIStyle(title) { fontSize = 54 }; heroTitle.normal.textColor = Color.white;
        text = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true }; text.normal.textColor = new Color(.88f,.91f,.94f);
        small = new GUIStyle(text) { fontSize = 15 }; small.normal.textColor = muted;
        eyebrow = new GUIStyle(small) { fontSize = 14, fontStyle = FontStyle.Bold }; eyebrow.normal.textColor = gold;
        centered = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter };
        stat = new GUIStyle(text) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; stat.normal.textColor = paleGold;
        button = new GUIStyle(GUI.skin.button) { fontSize=17,fontStyle=FontStyle.Bold,wordWrap=true,padding=new RectOffset(14,14,8,8),border=new RectOffset(1,1,1,1) };
        button.normal.background=UiTex(new Color(.075f,.13f,.18f)); button.hover.background=UiTex(new Color(.12f,.22f,.27f)); button.active.background=UiTex(new Color(.20f,.25f,.17f));
        button.focused.background=button.hover.background; button.normal.textColor=new Color(.94f,.91f,.82f); button.hover.textColor=Color.white; button.active.textColor=paleGold; button.focused.textColor=Color.white;
        goldButton = new GUIStyle(button); goldButton.normal.background=UiTex(new Color(.76f,.49f,.16f)); goldButton.hover.background=UiTex(new Color(.91f,.64f,.24f)); goldButton.active.background=UiTex(new Color(.64f,.39f,.10f)); goldButton.normal.textColor=Color.white; goldButton.hover.textColor=Color.white;
        navButton = new GUIStyle(button) { fontSize=18,alignment=TextAnchor.MiddleCenter };
        box = new GUIStyle(GUI.skin.box) { fontSize=18 }; box.normal.background=UiTex(surface);
        input = new GUIStyle(GUI.skin.textField) { fontSize=18,padding=new RectOffset(14,14,8,8),border=new RectOffset(1,1,1,1) };
        input.normal.background=UiTex(new Color(.025f,.05f,.08f)); input.focused.background=UiTex(new Color(.04f,.085f,.115f));
        input.hover.background=input.focused.background; input.normal.textColor=Color.white; input.focused.textColor=Color.white; input.hover.textColor=Color.white;
    }

    Texture2D UiTex(Color color)
    {
        var texture=new Texture2D(1,1,TextureFormat.RGBA32,false) { hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Point };
        texture.SetPixel(0,0,color); texture.Apply(); uiTextures.Add(texture); return texture;
    }

    void OnDestroy()
    {
        if(arena!=null){arena.Dispose();arena=null;}
        foreach(Texture2D texture in uiTextures) if(texture!=null) Destroy(texture);
    }

    bool Btn(Rect rect, string label, bool enabled = true)
    {
        bool previous = GUI.enabled; GUI.enabled = previous && enabled && !busy;
        GUIStyle style=rect.height<40?(compactButton??(compactButton=new GUIStyle(button){fontSize=13,padding=new RectOffset(6,6,2,2)})):button;
        bool clicked = GUI.Button(rect, label, style); GUI.enabled = previous; return clicked;
    }

    bool GoldBtn(Rect rect, string label)
    {
        bool previous=GUI.enabled; GUI.enabled=previous&&!busy;
        bool clicked=GUI.Button(rect,label,goldButton); GUI.enabled=previous; return clicked;
    }

    void Panel(Rect rect, Color color)
    {
        Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
    }

    void Border(Rect rect, Color color, float width = 2)
    {
        Panel(new Rect(rect.x,rect.y,rect.width,width),color); Panel(new Rect(rect.x,rect.yMax-width,rect.width,width),color);
        Panel(new Rect(rect.x,rect.y,width,rect.height),color); Panel(new Rect(rect.xMax-width,rect.y,width,rect.height),color);
    }

    void Card(Rect rect, Color fill, Color line)
    {
        Panel(new Rect(rect.x+4,rect.y+7,rect.width,rect.height),new Color(0,0,0,.28f)); Panel(rect,fill); Border(rect,line,1.5f);
    }

    void DrawBackdrop()
    {
        Panel(new Rect(0,0,1600,1000),navy);
        for(int i=0;i<10;i++) Panel(new Rect(0,i*100,1600,100),Color.Lerp(new Color(.025f,.04f,.075f),new Color(.015f,.09f,.11f),i/12f));
        Panel(new Rect(0,0,7,1000),gold); Panel(new Rect(1593,0,7,1000),gold);
        Panel(new Rect(0,76,1600,1),new Color(gold.r,gold.g,gold.b,.45f));
    }

    void Pill(Rect rect, string value, Color color)
    {
        Panel(rect,new Color(color.r,color.g,color.b,.16f)); Border(rect,new Color(color.r,color.g,color.b,.65f),1); GUI.Label(rect,value,stat);
    }

    void OnGUI()
    {
        if (solo) return;
#if DITTOCHES_PORTABLE_PREVIEW
        // Automated handlers drive the isolated smoke match; incidental desktop
        // clicks must not move its fixture units while screenshots are captured.
        if(PortablePreview.HasArgument("--online-smoke")&&(Event.current.isMouse||Event.current.isKey))Event.current.Use();
#endif
        Styles();
        Matrix4x4 old = GUI.matrix;
        float rawScale=Mathf.Min(Screen.width/1600f,Screen.height/1000f);
        float scale=rawScale>=.85f?Mathf.Floor(rawScale*20f)/20f:rawScale;
        float offsetX=Mathf.Floor((Screen.width-1600*scale)/2f),offsetY=Mathf.Floor((Screen.height-1000*scale)/2f);
        GUI.matrix=Matrix4x4.TRS(new Vector3(offsetX,offsetY,0),Quaternion.identity,new Vector3(scale,scale,1));
        Color oldColor=GUI.color; Panel(new Rect(-offsetX/scale,-offsetY/scale,Screen.width/scale,Screen.height/scale),Color.black); GUI.color=oldColor;
        DrawBackdrop();
        GUI.enabled=string.IsNullOrEmpty(onlineSkillId);
        if (state != null && state.room != null) DrawMatch(); else DrawLobby();
        GUI.enabled=true;
        Panel(new Rect(0,944,1600,56),new Color(.015f,.028f,.05f,.96f));
        Panel(new Rect(30,965,8,8),busy ? gold : connectionError ? new Color(.9f,.3f,.25f) : cyan);
        GUI.Label(new Rect(50,951,1500,38), busy ? "서버 통신 중  ·  " + notice : notice, small);
        DrawOnlineSkillDetails();
        GUI.matrix = old;
    }

    void DrawLobby()
    {
        if(lobbyTab==0) DrawHomeLobby();
        else
        {
            DrawLobbyBackground(.20f);
            DrawTopNavigation();
            if(lobbyTab==1) DrawPlayLobby();
            else if(lobbyTab==2) DrawCodexLobby();
            else DrawServerLobby();
        }
    }

    void DrawLobbyBackground(float opacity)
    {
        if(lobbyBackground!=null)
        {
            Color old=GUI.color; GUI.color=new Color(1,1,1,opacity);
            GUI.DrawTexture(new Rect(0,77,1600,867),lobbyBackground,ScaleMode.ScaleAndCrop);
            GUI.color=old;
        }
        Panel(new Rect(0,77,1600,867),new Color(.01f,.025f,.055f,.40f));
    }

    void DrawTopNavigation()
    {
        Panel(new Rect(0,0,1600,78),new Color(.012f,.027f,.047f,.98f));
        GUI.Label(new Rect(50,13,260,55),"DITTOCHES",title);
        string[] tabs={"로비","게임하기","디지몬 도감"};
        for(int i=0;i<tabs.Length;i++)
        {
            Rect r=new Rect(340+i*160,18,145,44);
            if(i==lobbyTab) { Panel(r,new Color(gold.r,gold.g,gold.b,.92f)); GUI.Label(r,tabs[i],stat); }
            else if(GUI.Button(r,tabs[i],navButton)) lobbyTab=i;
        }
        GUI.Label(new Rect(1020,19,170,42),state==null?"테이머":"테이머 · "+state.name,centered);
        Rect serverTab=new Rect(1370,18,180,44);
        if(lobbyTab==3) { Panel(serverTab,new Color(gold.r,gold.g,gold.b,.92f)); GUI.Label(serverTab,"서버 설정",stat); }
        else if(GUI.Button(serverTab,"서버 설정",navButton)) lobbyTab=3;
        Panel(new Rect(0,77,1600,1),new Color(gold.r,gold.g,gold.b,.55f));
    }

    void DrawHomeLobby()
    {
        DrawLobbyBackground(1f); DrawLobbyAtmosphere(); DrawTopNavigation();
        Panel(new Rect(0,78,860,866),new Color(.006f,.018f,.038f,.78f));
        GUI.Label(new Rect(85,180,560,28),"FILE ISLAND  /  새로운 모험의 시작",eyebrow);
        GUI.Label(new Rect(85,238,660,80),"디지몬 오토체스",heroTitle);
        GUI.Label(new Rect(88,330,590,75),"작은 디지몬의 가능성은 무한하다.\n모으고, 배치하고, 나만의 팀으로 승리하세요.",text);
        if(GoldBtn(new Rect(88,455,310,72),"게임하기   →")) lobbyTab=1;
        GUI.Label(new Rect(88,545,500,28),"일반 대전  ·  랭크 대전  ·  솔로 플레이",small);
        Rect info=new Rect(88,650,575,112); Card(info,new Color(.018f,.055f,.075f,.94f),new Color(cyan.r,cyan.g,cyan.b,.55f));
        GUI.Label(new Rect(110,668,520,25),"팀을 완성하는 건 당신의 선택",eyebrow);
        GUI.Label(new Rect(110,700,520,50),"같은 디지몬 3마리를 모아 승급하고\n레벨을 올려 더 강력한 디지몬을 만나세요.",text);
        Texture2D mascot=LobbyPortrait("koromon")??lobbyMascot;
        if(mascot!=null) GUI.DrawTexture(new Rect(1010,330,310,310),mascot,ScaleMode.ScaleToFit,true);
        Rect legendCard=new Rect(980,665,390,95); Card(legendCard,new Color(.015f,.045f,.065f,.95f),new Color(cyan.r,cyan.g,cyan.b,.45f));
        GUI.Label(new Rect(1002,678,350,22),"MY LITTLE LEGEND",eyebrow);
        GUI.Label(new Rect(1002,707,350,40),LobbyName(catalog.units[0]),title);
        GUI.Label(new Rect(50,895,550,30),"FILE ISLAND LEAGUE    /    DIGITAL FRONTIER 0.3",eyebrow);
        GUI.Label(new Rect(1280,895,260,30),"●  솔로 플레이 가능",small);
        if(Btn(new Rect(1370,820,180,48),artPack==0?"디지몬 버전":"오리지널 버전")) SetArtPack(1-artPack);
    }

    void DrawLobbyAtmosphere()
    {
        float time=Time.unscaledTime;
        for(int i=0;i<14;i++)
        {
            float x=880+(i*83%650), baseY=120+(i*137%700), y=baseY+Mathf.Sin(time*(.35f+i*.025f)+i)*18f;
            float size=3+(i%3)*2, alpha=.16f+(i%4)*.045f;
            Panel(new Rect(x,y,size,size),new Color(.35f,.9f,1f,alpha));
        }
        Panel(new Rect(0,78,1600,5),new Color(gold.r,gold.g,gold.b,.14f));
    }

    void DrawPlayLobby()
    {
        GUI.Label(new Rect(70,105,500,30),"GAME MODE",eyebrow);
        GUI.Label(new Rect(70,135,900,55),"플레이할 모드를 선택하세요",title);
        string[] labels = { "솔로 플레이", "일반 대전", "랭크 대전" };
        string[] tags = { "SOLO", "NORMAL", "RANKED" };
        string[] descriptions = { "나만의 속도로 즐기는 파일 아일랜드 리그\n난이도와 캐릭터 버전을 선택할 수 있습니다.", "부담 없이 즐기는 온라인 1대1\n승패에 따른 RP 변동이 없습니다.", "실력을 겨루는 온라인 1대1\n승패가 서버 랭크에 반영됩니다." };
        bool queued = state != null && !string.IsNullOrEmpty(state.queue);
        for (int i=0;i<3;i++)
        {
            float x=70+i*495; Rect mode=new Rect(x,225,460,470); Color line=i==2?gold:i==1?cyan:new Color(.45f,.58f,.68f);
            Card(mode,new Color(surface2.r,surface2.g,surface2.b,.96f),line); Panel(new Rect(x,225,460,7),line);
            GUI.Label(new Rect(x+30,260,400,25),tags[i],eyebrow); GUI.Label(new Rect(x+30,300,400,55),labels[i],title);
            GUI.Label(new Rect(x+30,380,400,90),descriptions[i],text);
            GUI.Label(new Rect(x+30,505,400,30),i==0?"AI 7명 · 서버 불필요":i==1?"2 PLAYERS · CASUAL":"2 PLAYERS · ELO RATING",small);
            if(Btn(new Rect(x+30,595,400,62),i==0?"솔로 리그 입장":"대전 찾기",!queued&&(i==0||token.Length>0)))
            { if(i==0) EnterSolo(); else Send("/queue",new Command{mode=i==1?"normal":"ranked"}); }
        }
        if(state==null) GUI.Label(new Rect(70,735,1100,35),"온라인 대전은 먼저 서버 설정에서 접속해 주세요.",small);
        else GUI.Label(new Rect(70,735,1100,35),$"● ONLINE  {state.name}  ·  {state.rating} RP",text);
        if(queued)
        {
            Card(new Rect(70,800,1460,70),new Color(.05f,.11f,.14f),cyan);
            GUI.Label(new Rect(105,817,1050,35),(state.queue=="ranked"?"RANKED":"NORMAL")+" · 상대 테이머를 찾는 중…",text);
            if(Btn(new Rect(1240,812,260,46),"매칭 취소")) Send("/cancel");
        }
    }

    void DrawServerLobby()
    {
        GUI.Label(new Rect(70,105,500,30),"ONLINE CONNECTION",eyebrow);
        GUI.Label(new Rect(70,135,900,55),"서버 설정",title);
        Rect connect = new Rect(70,220,1460,300); Card(connect,new Color(surface.r,surface.g,surface.b,.97f),new Color(gold.r,gold.g,gold.b,.55f));
        GUI.Label(new Rect(105,250,250,28),"SERVER ADDRESS",eyebrow); GUI.Label(new Rect(795,250,250,28),"TAMER NAME",eyebrow);
        GUI.enabled=!busy&&token.Length==0;
        server=GUI.TextField(new Rect(105,290,650,54),server,200,input); nickname=GUI.TextField(new Rect(795,290,375,54),nickname,20,input);
        GUI.enabled = true;
        if (token.Length == 0)
        { if (Btn(new Rect(1200,290,285,54),"서버 접속")) Connect(); }
        else if (Btn(new Rect(1200,290,285,54),"접속 해제",string.IsNullOrEmpty(state.queue)))
        { token = ""; state = null; notice = "접속을 해제했습니다."; }
        GUI.Label(new Rect(105,390,1070,40),state==null?"로컬 127.0.0.1:7777 · 다른 기기는 서버 PC의 IP 주소를 사용하세요":$"● ONLINE   {state.name}   ·   {state.rating} RP",small);
        if(state!=null) Pill(new Rect(1265,385,220,44),state.rating+" RP",gold);
        GUI.Label(new Rect(105,455,1320,40),notice,text);
    }

    void DrawCodexLobby()
    {
        GUI.Label(new Rect(70,105,500,30),"DIGIMON CODEX",eyebrow);
        GUI.Label(new Rect(70,135,900,55),"디지몬 도감",title);
        GUI.Label(new Rect(70,195,1100,35),"현재 로스터의 디지몬과 역할군을 확인하세요.",text);
        int pageSize=15, pageCount=Mathf.CeilToInt(catalog.units.Length/(float)pageSize);
        codexPage=Mathf.Clamp(codexPage,0,pageCount-1);
        int start=codexPage*pageSize, count=Mathf.Min(pageSize,catalog.units.Length-start);
        GUI.Label(new Rect(1180,145,170,35),artPack==0?"디지몬 버전":"오리지널 버전",centered);
        if(Btn(new Rect(1360,138,170,45),"버전 전환")) SetArtPack(1-artPack);
        for(int i=0;i<count;i++)
        {
            int col=i%5,row=i/5; Rect r=new Rect(70+col*294,260+row*190,270,165); UnitDef def=catalog.units[start+i];
            Card(r,new Color(surface2.r,surface2.g,surface2.b,.96f),new Color(cyan.r,cyan.g,cyan.b,.35f));
            Texture2D portrait=LobbyPortrait(def.id);
            if(portrait!=null) GUI.DrawTexture(new Rect(r.x+15,r.y+15,115,105),portrait,ScaleMode.ScaleToFit,true);
            GUI.Label(new Rect(r.x+140,r.y+20,120,32),LobbyName(def),text);
            GUI.Label(new Rect(r.x+140,r.y+55,120,28),def.role,eyebrow);
            GUI.Label(new Rect(r.x+140,r.y+90,120,28),def.cost+" GOLD",small);
            var skill=artPack==0?DigimonSkillCatalog.Find(def.id):null;
            if(skill!=null)
            {
                if(DigimonSkillUI.DrawIcon(new Rect(r.x+15,r.y+126,31,31),skill)){onlineSkillId=def.id;onlineSkillArea="";onlineSkillSlot=-1;}
                GUI.Label(new Rect(r.x+55,r.y+132,200,24),skill.name,new GUIStyle(small){fontSize=13});
            }
        }
        if(Btn(new Rect(610,850,150,48),"← 이전",codexPage>0)) codexPage--;
        GUI.Label(new Rect(770,852,160,44),$"{codexPage+1} / {pageCount}",centered);
        if(Btn(new Rect(940,850,150,48),"다음 →",codexPage<pageCount-1)) codexPage++;
        if(artPack==1) GUI.Label(new Rect(1120,850,410,48),"오리지널 캐릭터는 현재 3종부터 순차 제작 중",small);
    }

    void SetArtPack(int value)
    {
        artPack=Mathf.Clamp(value,0,1);
        PlayerPrefs.SetInt("multiSoloArtPack",artPack); PlayerPrefs.Save();
        notice=artPack==0?"디지몬 팬 버전으로 전환했습니다.":"오리지널 캐릭터 버전으로 전환했습니다.";
    }

    string LobbyName(UnitDef def)
    {
        if(artPack==1 && originalNames.TryGetValue(def.id,out string value)) return value;
        return def.name;
    }

    Texture2D LobbyPortrait(string id)
    {
        UnitDef def=Def(id); if(def==null) return null;
        string path="Sprites/"+def.sprite;
        if(artPack==1 && originalSprites.TryGetValue(id,out string originalPath)) path=originalPath;
        else if(artPack==0 && fanUnitSprites.TryGetValue(id,out string fanPath)) path=fanPath;
        if(!textures.TryGetValue(path,out Texture2D texture))
        {
            texture=Resources.Load<Texture2D>(path);
#if DITTOCHES_PORTABLE_PREVIEW
            if(texture==null)texture=PortablePreview.Texture(path);
#endif
            textures[path]=texture;
        }
        if(texture==null && path!="Sprites/"+def.sprite)
        {
            path="Sprites/"+def.sprite;
            if(!textures.TryGetValue(path,out texture)) { texture=Resources.Load<Texture2D>(path); textures[path]=texture; }
        }
        return texture;
    }

    UnitDef Def(string id) { return Array.Find(catalog.units, u => u.id == id); }
    void Portrait(Rect rect, string id)
    {
        UnitDef def = Def(id); if (def == null) return;
        if (!textures.TryGetValue(id, out Texture2D texture)) { texture = Resources.Load<Texture2D>("Sprites/" + def.sprite); textures[id] = texture; }
        if (texture != null) GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
    }

    Unit At(Unit[] units, int slot) { return Array.Find(units, u => u.slot == slot); }
    void ClickSlot(string area, int slot, Unit unit)
    {
        if(EquipOnlineSelection(area,slot,unit))return;
        if (selectedSlot >= 0)
        {
            Player me=state.room.players[state.room.side];
            if(area=="board"&&selectedArea=="bench"&&unit==null&&me.board.Length>=me.level)
            {notice="배치 인원이 가득 찼습니다. 다른 유닛과 교환하세요.";return;}
            if (selectedArea != area || selectedSlot != slot)
                Send("/action", new Command { action = "move", area = selectedArea, slot = selectedSlot, targetArea = area, targetSlot = slot });
            selectedSlot = -1; selectedArea = "";
        }
        else if (unit != null) { selectedArea = area; selectedSlot = slot; }
    }

    void DrawSlot(Rect rect, Unit unit, string area, int slot, bool editable)
    {
        bool selected=selectedArea==area&&selectedSlot==slot;
        Panel(rect,selected?new Color(.20f,.25f,.13f):unit==null?new Color(.035f,.075f,.095f,.82f):new Color(.07f,.13f,.17f));
        Border(rect,selected?gold:new Color(.16f,.30f,.34f),selected?2:1);
        if (unit != null)
        {
            Portrait(new Rect(rect.x+4,rect.y+2,50,rect.height-5),unit.id);
            GUI.Label(new Rect(rect.x+55,rect.y+4,rect.width-58,24),Def(unit.id).name,small);
            GUI.Label(new Rect(rect.x+55,rect.y+27,rect.width-58,24),new string('★',unit.star),eyebrow);
        }
        if (editable && !busy && GUI.Button(rect, GUIContent.none, GUIStyle.none)) ClickSlot(area, slot, unit);
    }

    void DrawMatch()
    {
        ReconcileEquipmentSelection();
        Room room = state.room; Player me = room.players[room.side], enemy = room.players[1 - room.side];
        if(onlineItemGuide>=0&&Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){onlineItemGuide=-1;Event.current.Use();}
        if (room.phase == "finished") confirmLeave = false;
        GUI.enabled = !confirmLeave&&onlineItemGuide<0&&string.IsNullOrEmpty(onlineSkillId);
        bool fresh = Time.unscaledTime - receivedAt < 6;
        bool editable = room.phase == "prepare" && !me.ready && fresh;
        float remaining = Mathf.Max(0, room.remaining - (Time.unscaledTime - receivedAt));
        GUI.Label(new Rect(30,14,250,24),room.mode=="ranked"?"RANKED MATCH":"NORMAL MATCH",eyebrow);
        GUI.Label(new Rect(30,36,570,42),$"ROUND {room.round}",title);
        Pill(new Rect(610,23,190,43),room.phase=="prepare"?"준비 단계":room.phase=="battle"?"전투 중":"경기 종료",room.phase=="battle"?new Color(.85f,.28f,.22f):cyan);
        Pill(new Rect(815,23,145,43),Mathf.CeilToInt(remaining)+"초",remaining<10?new Color(.9f,.35f,.22f):gold);
        if (Btn(new Rect(1370,20,190,46),room.phase=="finished"?"로비로":"경기 포기"))
        { if (room.phase == "finished") Send("/leave"); else confirmLeave = !confirmLeave; }
        Card(new Rect(25,95,260,165),surface,new Color(gold.r,gold.g,gold.b,.5f));
        GUI.Label(new Rect(45,108,220,25),"MY TACTICIAN",eyebrow); GUI.Label(new Rect(45,136,220,34),me.name,text);
        Pill(new Rect(43,180,102,36),"HP "+me.hp,new Color(.30f,.78f,.52f)); Pill(new Rect(155,180,108,36),me.rating+" RP",gold);
        GUI.Label(new Rect(45,225,220,25),$"{me.gold} G    ·    LV {me.level}    ·    XP {me.xp}",small);
        Card(new Rect(1305,95,270,165),surface,new Color(.65f,.25f,.28f,.7f));
        GUI.Label(new Rect(1325,108,225,25),"OPPONENT",eyebrow); GUI.Label(new Rect(1325,136,225,34),enemy.name,text);
        Pill(new Rect(1323,180,102,36),"HP "+enemy.hp,new Color(.82f,.28f,.25f)); Pill(new Rect(1435,180,118,36),enemy.rating+" RP",gold);
        GUI.Label(new Rect(1325,225,225,25),$"LV {enemy.level}   ·   {(enemy.ready?"준비 완료":"준비 중")}",small);
        GUI.Label(new Rect(310,94,950,30),"상대 진영",eyebrow);
        DrawPerspectiveMatch(room,me,enemy,editable,remaining);
        GUI.Label(new Rect(310,790,500,22),"RECRUIT SHOP",eyebrow);
        for (int i = 0; i < me.shop.Length; i++)
        {
            float x = 310 + i * 191; string id = me.shop[i]; UnitDef def = Def(id);
            Card(new Rect(x,814,181,108),surface2,def==null?new Color(.15f,.25f,.3f):gold);
            if (def != null)
            {
                Portrait(new Rect(x + 4, 825, 60, 78), id);
                if (Btn(new Rect(x + 65, 822, 112, 88), def.name + "\n" + def.cost + " G", editable && me.gold >= def.cost))
                    Send("/action", new Command { action = "buy", slot = i });
            }
        }
        GUI.Label(new Rect(30,286,250,24),"ECONOMY",eyebrow);
        if (Btn(new Rect(30,320,245,58),"새로고침     2 G",editable&&me.gold>=2)) Send("/action",new Command{action="reroll"});
        if (Btn(new Rect(30,390,245,58),"경험치 +4     4 G",editable&&me.gold>=4&&me.level<9)) Send("/action",new Command{action="xp"});
        if (Btn(new Rect(30,460,245,58),"선택 유닛 판매",editable&&selectedSlot>=0))
        { Send("/action", new Command { action = "sell", area = selectedArea, slot = selectedSlot }); selectedSlot = -1; selectedArea = ""; }
        if (Btn(new Rect(30,555,245,78),me.ready?"준비 취소":"전투 준비 완료",room.phase=="prepare"&&fresh)) Send("/action",new Command{action="ready"});
        DrawOnlineEquipment(me,editable);
        DrawOnlineUnitEquipment(me,room);
        if(artPack==0)DrawOnlineTraits(me);
        if (!fresh) GUI.Label(new Rect(310, 80, 970, 50), "연결 복구 중 · 조작을 잠시 중지합니다.", text);
        if (room.phase == "finished")
        {
            Card(new Rect(510,300,610,270),new Color(.035f,.07f,.12f,.99f),gold);
            GUI.Label(new Rect(560,335,510,60),"경기 결과  ·  "+room.result,title);
            GUI.Label(new Rect(560, 420, 510, 65), room.mode == "ranked" ? $"랭크 변동 {room.ratingDelta:+0;-0;0} RP  /  현재 {state.rating} RP" : "일반 모드 · 랭크 점수 변동 없음", text);
        }
        GUI.enabled = true;
        if(!confirmLeave)DrawOnlineEquipmentGuide();
        if (confirmLeave && room.phase != "finished")
        {
            Panel(new Rect(490, 330, 680, 250), new Color(.08f,.1f,.16f));
            GUI.Label(new Rect(530, 360, 600, 80), "경기를 포기하면 패배 처리됩니다.\n랭크 모드에서는 RP에도 반영됩니다.", text);
            if (Btn(new Rect(530, 470, 270, 65), "계속 플레이")) confirmLeave = false;
            if (Btn(new Rect(840, 470, 270, 65), "포기하고 로비로")) Send("/leave");
        }
    }

    void DrawCombat(Room room, float remaining)
    {

        if (room.frames == null || room.frames.Length == 0) return;
        float progress = CombatProgress(room,remaining);
        int frame = Mathf.FloorToInt(progress), next = Mathf.Min(frame + 1, room.frames.Length - 1);
        foreach (Fighter f in room.frames[frame].units)
        {
            if (f.hp <= 0) continue;
            Fighter to = Array.Find(room.frames[next].units, u => u.key == f.key) ?? f;
            float x = Mathf.Lerp(f.x,to.x,progress-frame), y = Mathf.Lerp(f.y,to.y,progress-frame);
            if (room.side == 1) { x = 6-x; y = 7-y; }
            Vector2 head=arena.Project(TacticalArena.CellWorld(x,y)+Vector3.up*(artPack==0?TacticalArena.DigimonHeadHeight(f.id,f.star):1.4f));
            float width=Mathf.Clamp(arena.CellRect(Mathf.Clamp(Mathf.RoundToInt(y),0,7),3).width*.78f,52,88);
            float health=Mathf.Lerp(f.hp,to.hp,progress-frame)/Mathf.Max(1,f.maxHp);
            Panel(new Rect(head.x-width/2-2,head.y-2,width+4,17),new Color(.008f,.015f,.025f,.95f));
            Panel(new Rect(head.x-width/2,head.y,width,7),new Color(.12f,.16f,.2f));
            Panel(new Rect(head.x-width/2,head.y,width*Mathf.Clamp01(health),7),f.side==room.side?new Color(.32f,.94f,.57f):new Color(.96f,.3f,.27f));
            float protection=Mathf.Lerp(f.shield,to.shield,progress-frame)/Mathf.Max(1,f.maxHp);
            if(protection>0)Panel(new Rect(head.x-width/2,head.y-3,width*Mathf.Clamp01(protection),2),new Color(.75f,.9f,1f));
            if(f.maxMana>0)Panel(new Rect(head.x-width/2,head.y+9,width*Mathf.Clamp01(Mathf.Lerp(f.mana,to.mana,progress-frame)/f.maxMana),3),new Color(.25f,.65f,1));
            if(f.side==room.side&&GUI.enabled&&Event.current.type==EventType.MouseDown&&Event.current.button==0&&new Rect(head.x-width/2,head.y-5,width,60).Contains(Event.current.mousePosition))
            {selectedArea="board";selectedSlot=f.slot;Event.current.Use();}
            float castAge=ActiveCastAge(room,f.key,CombatTime(room,remaining));
            var skill=artPack==0&&castAge>=0?DigimonSkillCatalog.Find(f.id):null;
            if(skill!=null)GUI.Label(new Rect(head.x-115,head.y-25,230,22),skill.name,centered);
            int segments=Mathf.Clamp(Mathf.CeilToInt(f.maxHp/250f),1,12);
            for(int i=1;i<segments;i++)Panel(new Rect(head.x-width/2+width*i/segments,head.y,1,7),new Color(.015f,.025f,.03f,.7f));
        }
    }
}
