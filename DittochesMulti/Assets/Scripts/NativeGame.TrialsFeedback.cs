using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    void DrawTrialLobbyRecord(Rect r)
    {
        var record=TrialPersonalBest;
        BattleHudTheme.Surface(r,false,BattleHudTheme.Gold);
        BattleHudTheme.Label(new Rect(r.x+14,r.y+10,r.width-28,19),"PERSONAL BEST  /  "+TrialsCatalog.DifficultyNames[TrialDifficulty],10,BattleHudTheme.Gold,true);
        BattleHudTheme.Label(new Rect(r.x+13,r.y+33,r.width-26,32),record.HasRecord?record.bestScore.ToString("N0"):"첫 기록에 도전",record.HasRecord?27:20,BattleHudTheme.Text,true);
        BattleHudTheme.FitLabel(new Rect(r.x+14,r.y+75,r.width-28,21),record.HasRecord?(record.cleared?"합격 달성":"최고 도달 "+record.furthestStage+" / 30")+"  ·  최다 "+record.bestWins+"승":"새로 시작해도 최고 기록은 유지됩니다",12,BattleHudTheme.Muted,false);
    }
    void DrawTrialResultRecord(Rect r)
    {
        var record=TrialPersonalBest;
        BattleHudTheme.Round(r,BattleHudTheme.Raised);
        BattleHudTheme.Label(new Rect(r.x+18,r.y+8,r.width-36,19),"난이도별 개인 최고 기록 · 새 도전에서도 유지",11,BattleHudTheme.Muted);
        BattleHudTheme.FitLabel(new Rect(r.x+18,r.y+30,r.width-36,27),record.bestScore.ToString("N0")+"점   /   최고 도달 "+record.furthestStage+"단계   /   최다 "+record.bestWins+"승"+(record.cleared?"   /   합격 달성":""),18,BattleHudTheme.Gold,true);
    }
    string TrialOutcomeLabel(TrialRoundSummary result)
    {
        switch(result.outcome)
        {
            case TrialRoundOutcome.Victory:return "시험 통과";
            case TrialRoundOutcome.Timeout:return "시간 초과";
            case TrialRoundOutcome.Interrupted:return "중단된 전투";
            default:return "전투 패배";
        }
    }
    void DrawTrialRoundToast()
    {
        var result=LastTrialRound;if(result==null)return;
        bool success=result.outcome==TrialRoundOutcome.Victory;
        Color tint=success?BattleHudTheme.Teal:BattleHudTheme.Danger;
        Rect r=new Rect(SoloArenaViewport.center.x-322,204,644,134);
        BattleHudTheme.Surface(r,true,tint);
        ArenaInterface.Fill(new Rect(r.x+1,r.y+1,5,r.height-2),tint);
        BattleHudTheme.Label(new Rect(r.x+23,r.y+12,401,22),"TRIAL "+result.stage.ToString("00")+"  /  "+TrialsCatalog.Get(result.stage).name,12,BattleHudTheme.Muted,true);
        BattleHudTheme.Label(new Rect(r.x+22,r.y+39,334,36),TrialOutcomeLabel(result),28,tint,true);
        BattleHudTheme.Label(new Rect(r.xMax-267,r.y+44,244,27),result.elapsedKnown?result.elapsed.ToString("0.0")+"초  ·  목숨 "+TrialLives+" / 3":"목숨 "+TrialLives+" / 3",15,BattleHudTheme.Text,true,TextAnchor.MiddleRight);
        string gains="+"+result.goldGained+" G    +"+result.xpAwarded+" XP";
        if(result.item>=0)gains+="    "+ItemNames[result.item];
        if(result.scoreGained>0)gains+="    +"+result.scoreGained.ToString("N0")+"점";
        if(result.livesLost>0)gains+="    목숨 -"+result.livesLost;
        BattleHudTheme.FitLabel(new Rect(r.x+23,r.y+85,r.width-46,26),gains,16,BattleHudTheme.Text,true);
        BattleHudTheme.Bar(new Rect(r.x+23,r.yMax-10,r.width-46,3),Mathf.Clamp01((resultNoticeUntil-Time.unscaledTime)/5f),tint);
    }
    void DrawTrialLastResultStrip(Rect r)
    {
        var result=LastTrialRound;if(result==null)return;
        Color tint=result.outcome==TrialRoundOutcome.Victory?BattleHudTheme.Teal:BattleHudTheme.Danger;
        BattleHudTheme.Round(r,new Color(.04f,.065f,.085f,.95f));
        string text="지난 시험 "+result.stage.ToString("00")+" · "+TrialOutcomeLabel(result)+"    +"+result.goldGained+"G · +"+result.xpAwarded+"XP";
        if(result.item>=0)text+=" · "+ItemNames[result.item];
        if(result.scoreGained>0)text+=" · +"+result.scoreGained.ToString("N0")+"점";
        if(result.livesLost>0)text+=" · 목숨 -"+result.livesLost;
        BattleHudTheme.FitLabel(new Rect(r.x+12,r.y+3,r.width-24,r.height-6),text,13,tint,false);
    }
    void DrawTrialBattleCue()
    {
        var wave=CurrentTrial;
        float age=Time.unscaledTime-battleStartedAt;
        if(age<1.7f)
        {
            Color tint=wave.boss?BattleHudTheme.Gold:BattleHudTheme.Teal;
            Rect r=new Rect(SoloArenaViewport.center.x-292,205,584,85);
            BattleHudTheme.Surface(r,true,tint);
            BattleHudTheme.Label(new Rect(r.x+18,r.y+9,r.width-36,19),wave.boss?"BOSS ENCOUNTER / 관문 "+wave.chapter:"TRIAL START / "+wave.number.ToString("00"),11,tint,true,TextAnchor.MiddleCenter);
            BattleHudTheme.Label(new Rect(r.x+18,r.y+32,r.width-36,30),wave.name,25,BattleHudTheme.Text,true,TextAnchor.MiddleCenter);
            BattleHudTheme.Bar(new Rect(r.x+18,r.yMax-9,r.width-36,3),1-Mathf.Clamp01(age/1.7f),tint);
        }
        else if(battleTimeRemaining<=10)
            BattleHudTheme.Badge(new Rect(SoloArenaViewport.center.x-172,199,344,32),"남은 시간 "+Mathf.CeilToInt(battleTimeRemaining)+"초 · 적을 모두 제압하세요",BattleHudTheme.Danger,14);
    }
    void DrawTrialBossHealth(Rect r)
    {
        var boss=fighters.FirstOrDefault(f=>f.enemy);if(boss==null)return;
        BattleHudTheme.Round(r,new Color(.045f,.064f,.083f,.94f));
        BattleHudTheme.Label(new Rect(r.x+10,r.y+2,r.width-20,18),"보스  "+Mathf.CeilToInt(Mathf.Max(0,boss.hp)).ToString("N0")+" / "+Mathf.CeilToInt(boss.maxHp).ToString("N0"),11,BattleHudTheme.Gold,true);
        BattleHudTheme.Bar(new Rect(r.x+10,r.yMax-9,r.width-20,4),boss.hp/Mathf.Max(1,boss.maxHp),BattleHudTheme.Gold);
    }
}
