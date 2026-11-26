using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    bool showTrialDossier;
    int trialScoutedStage=1,trialScoutedEnemyIndex;
    static readonly Rect TrialDossierRect=new Rect(204,98,1512,884);
    static readonly Rect TrialDossierCloseRect=new Rect(1609,127,77,42);
    static readonly Rect TrialDossierPreviousRect=new Rect(1409,171,76,36);
    static readonly Rect TrialDossierNextRect=new Rect(1493,171,76,36);
    public bool TrialDossierOpen {get{return showTrialDossier;}}
    public int TrialScoutedStage {get{return trialScoutedStage;}}
    public int TrialScoutedEnemyIndex {get{return trialScoutedEnemyIndex;}}

    public sealed class TrialEnemyPreview
    {
        public string id,name,skillName,damageType;
        public int star,col,row,range;
        public float hp,attack,armor,magicResist,speed,shield,skillDamage;
    }

    // This is an inspection projection, never a simulated fighter. Creating a fighter
    // consumes random cooldowns and would change the next shop/combat merely by scouting.
    public TrialEnemyPreview InspectTrialEnemy(int stage,int enemyIndex,int selectedDifficulty)
    {
        var wave=TrialsCatalog.Get(stage);
        var entry=wave.enemies[Mathf.Clamp(enemyIndex,0,wave.enemies.Length-1)];
        int mode=Mathf.Clamp(selectedDifficulty,0,2);
        var skill=DigimonSkillCatalog.Find(entry.id);
        var bonus=DigimonBuildCatalog.Resolve(entry.id,wave.enemies.Select(e=>e.id),Enumerable.Empty<int>());
        float initialHp=skill.baseHealth*Mathf.Pow(1.8f,entry.star-1)*(1+bonus.hp);
        float health=initialHp*entry.healthScale*TrialsCatalog.HealthScale[mode];
        float attackScale=entry.attackScale*TrialsCatalog.AttackScale[mode];
        float attack=DigimonCombatMath.Attack(skill.baseAttack,entry.star,bonus.attack)*attackScale;
        // Match InitializeBuildBonuses followed by ApplyTrialEnemyModifiers, including
        // shields granted before the encounter's health multiplier and the 50% cap.
        float startShield=Mathf.Max(0,Mathf.Min(initialHp*.5f,initialHp*bonus.startShield));
        float shield=startShield+Mathf.Max(0,Mathf.Min(health*.5f-startShield,health*entry.shield));
        return new TrialEnemyPreview{id=entry.id,name=TrialEnemyName(entry.id),star=entry.star,col=entry.col,row=entry.row,
            hp=health,attack=attack,armor=bonus.armor+entry.armor,magicResist=bonus.magicResist+entry.magicResist,
            speed=DigimonCombatMath.AttackSpeed(skill.attackSpeed,bonus.speed+entry.speed,bonus.rampSpeed,0),shield=shield,range=skill.attackRange,
            skillName=skill.name,damageType=skill.DamageLabel,
            skillDamage=skill.Damage(entry.star,attack,DigimonCombatMath.AbilityPower(bonus.abilityPower)*attackScale)};
    }

    string TrialEnemyName(string id)
    {
        var unit=TeamPlan.Roster.FirstOrDefault(u=>u.id==id);
        int index=Array.IndexOf(DigimonModelLibrary.Roster,id);
        return unit!=null?unit.name:index>=0?DigimonModelLibrary.RosterNames[index]:id;
    }

    void OpenTrialDossier(int stage)
    {
        if(!trialsMode)return;
        trialScoutedStage=Mathf.Clamp(stage,1,30);trialScoutedEnemyIndex=0;showTrialDossier=true;
        showTeamPlan=false;showRecipeGuide=false;traitFocus=null;skillDetailUnit=null;
        draggingUnit=false;dragSource=-1;equipmentDrag.Reset();arenaPointer.Reset();
    }
    void CloseTrialDossier(){showTrialDossier=false;}

    string TrialStageStatus(int stage)
    {
        int outcome=TrialWaveResult(stage);
        if(outcome>0)return "승리한 시험";
        if(outcome<0)return "패배한 시험";
        if(stage==TrialStage&&!TrialFinished)return "현재 도전할 시험";
        if(stage<TrialStage||(TrialFinished&&stage==TrialStage))return "진행 완료 · 개별 기록 없음";
        return "앞으로 만날 시험";
    }

    Color TrialStageColor(int stage)
    {
        int outcome=TrialWaveResult(stage);
        if(outcome>0)return BattleHudTheme.Teal;
        if(outcome<0)return BattleHudTheme.Danger;
        return stage==TrialStage&&!TrialFinished?BattleHudTheme.Gold:BattleHudTheme.Muted;
    }

    void DrawTrialRoadmapNode(Rect node,int number,bool dossier)
    {
        int outcome=TrialWaveResult(number);bool current=number==TrialStage&&!TrialFinished;
        bool selected=trialScoutedStage==number;
        Color tint=TrialStageColor(number);
        bool hover=GUI.enabled&&node.Contains(Event.current.mousePosition);
        BattleHudTheme.Round(node,Color.Lerp(BattleHudTheme.Raised,tint,hover?.35f:current?.25f:outcome!=0?.13f:.025f));
        if(selected)ArenaInterface.Fill(new Rect(node.x+5,node.y,node.width-10,2),BattleHudTheme.Teal);
        if(outcome!=0)ArenaInterface.Fill(new Rect(node.x+8,node.yMax-3,node.width-16,2),tint);
        BattleHudTheme.Label(node,number.ToString("00"),dossier?14:13,tint,true,TextAnchor.MiddleCenter);
        if(GUI.Button(node,new GUIContent("",number.ToString("00")+" · "+TrialsCatalog.Get(number).name+"\n"+TrialStageStatus(number)+" · 클릭하여 정찰"),GUIStyle.none))OpenTrialDossier(number);
    }

    static Rect TrialDossierCellRect(int col,int row){return new Rect(270+col*114,317+row*90,108,84);}

    string TrialProfileText(TrialsCatalog.Enemy entry)
    {
        var traits=new List<string>();
        if(entry.armor>0)traits.Add("물리 방어 +"+entry.armor.ToString("0"));
        if(entry.magicResist>0)traits.Add("마법 저항 +"+entry.magicResist.ToString("0"));
        if(entry.speed>0)traits.Add("공격 속도 +"+(entry.speed*100).ToString("0")+"%");
        if(entry.shield>0)traits.Add("시작 보호막 "+(entry.shield*100).ToString("0")+"%");
        return traits.Count==0?"기본형 · 추가 방어/속도 보정 없음":string.Join("  ·  ",traits.ToArray());
    }

    void DrawTrialDossier()
    {
        if(!showTrialDossier||!trialsMode)return;
        Event e=Event.current;
        if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){CloseTrialDossier();e.Use();return;}
        if(e.type==EventType.MouseDown&&!TrialDossierRect.Contains(e.mousePosition)){CloseTrialDossier();e.Use();return;}
        if(e.type==EventType.KeyDown&&(e.keyCode==KeyCode.LeftArrow||e.keyCode==KeyCode.RightArrow))
        {OpenTrialDossier(trialScoutedStage+(e.keyCode==KeyCode.LeftArrow?-1:1));e.Use();}
        bool enabled=GUI.enabled;GUI.enabled=true;
        var wave=TrialsCatalog.Get(trialScoutedStage);
        trialScoutedEnemyIndex=Mathf.Clamp(trialScoutedEnemyIndex,0,wave.enemies.Length-1);
        ArenaInterface.Fill(new Rect(0,0,1920,1080),new Color(.003f,.009f,.018f,.91f));
        BattleHudTheme.Surface(TrialDossierRect,true,BattleHudTheme.Teal);
        BattleHudTheme.Label(new Rect(237,123,950,24),"ENCOUNTER DOSSIER  /  아구몬박사의 정찰 노트",12,BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(233,163,1150,49),wave.number.ToString("00")+"  "+wave.name,32,BattleHudTheme.Text,true);
        BattleHudTheme.Badge(new Rect(1104,127,204,32),TrialsCatalog.DifficultyNames[TrialDifficulty]+" · "+(wave.boss?"보스 시험":"일반 시험"),BattleHudTheme.Gold,13);
        BattleHudTheme.Label(new Rect(1324,128,266,29),TrialStageStatus(wave.number),12,TrialStageColor(wave.number),true,TextAnchor.MiddleRight);
        if(arenaInterface.Button(TrialDossierCloseRect,new GUIContent("닫기 ×"),true,false,14))CloseTrialDossier();
        if(arenaInterface.Button(TrialDossierPreviousRect,new GUIContent("← 이전"),wave.number>1,false,12))OpenTrialDossier(wave.number-1);
        if(arenaInterface.Button(TrialDossierNextRect,new GUIContent("다음 →"),wave.number<30,false,12))OpenTrialDossier(wave.number+1);
        DrawTrialDossierBoard(wave);
        DrawTrialDossierEnemy(wave,trialScoutedEnemyIndex);
        BattleHudTheme.Surface(new Rect(234,819,1452,59));
        BattleHudTheme.Label(new Rect(254,825,568,19),"승리 보상 · 보유 골드에 따라 이자 최대 5G 추가",11,BattleHudTheme.Muted);
        BattleHudTheme.FitLabel(new Rect(254,847,568,24),TrialRewardText(wave),17,BattleHudTheme.Gold,true);
        ArenaInterface.Fill(new Rect(844,831,1,35),BattleHudTheme.Line);
        BattleHudTheme.Label(new Rect(865,826,335,18),"제한 시간 안에 모든 적 제압",11,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(865,847,335,24),wave.timeLimit.ToString("0")+"초  ·  적 "+wave.enemies.Length+"명",18,BattleHudTheme.Text,true);
        ArenaInterface.Fill(new Rect(1224,831,1,35),BattleHudTheme.Line);
        BattleHudTheme.Label(new Rect(1245,826,423,18),"패배 · 시간 초과 · 전투 중 중단",11,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(1245,847,423,24),"목숨 -1   ·   3G + 1 XP",17,BattleHudTheme.Text,true);
        BattleHudTheme.Label(new Rect(236,885,1000,18),"모든 시험 정찰   ·   금색 현재 / 청록 승리 / 붉은색 패배 / 윗줄 선택",11,BattleHudTheme.Muted);
        for(int i=0;i<30;i++)DrawTrialRoadmapNode(new Rect(238+i*48,910,40,35),i+1,true);
        BattleHudTheme.Label(new Rect(238,953,1438,19),"번호와 적을 눌러 살펴보세요  ·  ← → 시험 이동  ·  ESC / 바깥 클릭으로 닫기  ·  정찰은 진행과 편성을 바꾸지 않습니다"+(battling?"  ·  전투 진행 중":""),11,BattleHudTheme.Muted);
        GUI.enabled=enabled;
        if(e.isMouse||e.type==EventType.ScrollWheel||e.type==EventType.KeyDown||e.type==EventType.KeyUp)e.Use();
    }

    void DrawTrialDossierBoard(TrialsCatalog.Wave wave)
    {
        BattleHudTheme.Surface(new Rect(234,229,872,575));
        BattleHudTheme.Label(new Rect(256,246,547,29),"적 시작 배치",20,BattleHudTheme.Text,true);
        BattleHudTheme.Label(new Rect(784,251,298,23),"7열 × 4행  ·  개체를 눌러 선택",12,BattleHudTheme.Muted,false,TextAnchor.MiddleRight);
        BattleHudTheme.Label(new Rect(257,278,525,19),"후열 · 아군에서 먼 쪽",11,BattleHudTheme.Muted);
        for(int col=0;col<7;col++)BattleHudTheme.Label(new Rect(270+col*114,294,108,18),((char)('A'+col)).ToString(),11,BattleHudTheme.Muted,true,TextAnchor.MiddleCenter);
        for(int row=0;row<4;row++)
        {
            BattleHudTheme.Label(new Rect(244,317+row*90,20,84),(row+1).ToString(),11,BattleHudTheme.Muted,true,TextAnchor.MiddleCenter);
            for(int col=0;col<7;col++)
            {
                Rect cell=TrialDossierCellRect(col,row);int index=Array.FindIndex(wave.enemies,entry=>entry.col==col&&entry.row==row);
                bool selected=index>=0&&trialScoutedEnemyIndex==index;
                BattleHudTheme.Round(cell,selected?Color.Lerp(BattleHudTheme.Raised,BattleHudTheme.Teal,.34f):BattleHudTheme.Raised);
                if(index<0){ArenaInterface.Fill(new Rect(cell.center.x-2,cell.center.y-2,4,4),BattleHudTheme.Line);continue;}
                var enemy=wave.enemies[index];
                if(selected)ArenaInterface.Fill(new Rect(cell.x+8,cell.y+1,cell.width-16,3),BattleHudTheme.Teal);
                FaithfulPortraits.Draw(new Rect(cell.x+23,cell.y+3,64,55),enemy.id,true);
                BattleHudTheme.Label(new Rect(cell.x+5,cell.y+3,33,16),(index+1).ToString("00"),10,selected?BattleHudTheme.Teal:BattleHudTheme.Muted,true);
                BattleHudTheme.FitLabel(new Rect(cell.x+6,cell.y+55,cell.width-12,16),TrialEnemyName(enemy.id),12,BattleHudTheme.Text,true);
                BattleHudTheme.Label(new Rect(cell.x+5,cell.y+69,cell.width-10,12),new string('★',enemy.star),9,BattleHudTheme.Gold,true,TextAnchor.MiddleCenter);
                if(GUI.Button(cell,new GUIContent("","개체 "+(index+1)+" · "+TrialEnemyName(enemy.id)+" · "+TrialProfileText(enemy)),GUIStyle.none))trialScoutedEnemyIndex=index;
            }
        }
        BattleHudTheme.Label(new Rect(269,683,792,22),"전열  ↓  아군 전장 방향",12,BattleHudTheme.Teal,true,TextAnchor.MiddleCenter);
        ArenaInterface.Fill(new Rect(256,718,828,1),BattleHudTheme.Line);
        BattleHudTheme.Label(new Rect(256,731,134,21),"공략 메모",12,BattleHudTheme.Gold,true);
        TrialParagraph(new Rect(396,732,682,56),wave.description,15,BattleHudTheme.Text);
    }

    void DrawTrialDossierEnemy(TrialsCatalog.Wave wave,int index)
    {
        var enemy=wave.enemies[index];var stats=InspectTrialEnemy(wave.number,index,TrialDifficulty);
        BattleHudTheme.Surface(new Rect(1124,229,562,575),false,BattleHudTheme.Teal);
        FaithfulPortraits.Draw(new Rect(1143,250,147,132),enemy.id,true);
        BattleHudTheme.Label(new Rect(1309,251,354,24),"개체 "+(index+1).ToString("00")+"   ·   "+((char)('A'+enemy.col))+(enemy.row+1)+" 위치",12,BattleHudTheme.Teal,true);
        BattleHudTheme.FitLabel(new Rect(1307,289,356,42),stats.name,28,BattleHudTheme.Text,true);
        BattleHudTheme.Label(new Rect(1309,339,354,26),new string('★',stats.star)+"   ·   사거리 "+stats.range+"칸",15,BattleHudTheme.Gold,true);
        string[] labels={"체력","공격력 · AD","물리 방어","마법 저항","공격 속도","시작 보호막"};
        string[] values={stats.hp.ToString("N0"),stats.attack.ToString("0.0"),stats.armor.ToString("0.#"),stats.magicResist.ToString("0.#"),stats.speed.ToString("0.00")+" / 초",stats.shield.ToString("N0")};
        for(int i=0;i<6;i++)BattleHudTheme.StatTile(new Rect(1146+i%2*260,397+i/2*76,248,68),labels[i],values[i],i==5?BattleHudTheme.Teal:(Color?)null);
        BattleHudTheme.Label(new Rect(1147,628,518,22),"이 개체의 특성",12,BattleHudTheme.Gold,true);
        TrialParagraph(new Rect(1147,657,518,46),TrialProfileText(enemy),14,BattleHudTheme.Text);
        ArenaInterface.Fill(new Rect(1147,712,516,1),BattleHudTheme.Line);
        BattleHudTheme.FitLabel(new Rect(1147,725,516,25),stats.skillName+"  ·  "+stats.damageType+" 기술",16,BattleHudTheme.Text,true);
        BattleHudTheme.Label(new Rect(1147,762,516,24),"난이도 · 성급 · 편성 적용 / 전투 시작 기준",11,BattleHudTheme.Muted);
    }
}
