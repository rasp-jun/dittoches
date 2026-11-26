using UnityEngine;

public static class StarSoundUI
{
    public static Rect MuteRect(Rect panel){return new Rect(panel.x+898,panel.y+292,186,47);}
    public static Rect VolumeRect(Rect panel,int index){return new Rect(panel.x+55+index*139,panel.y+292,126,47);}
    public static Rect PreviewRect(Rect panel,int star){return new Rect(panel.x+55+(star-2)*550,panel.y+438,475,34);}
    public static Rect ResetRect(Rect panel){return new Rect(panel.x+35,panel.y+602,230,43);}
    public static Rect CombatRect(Rect panel,int index){return new Rect(panel.x+55+index*262,panel.y+538,244,35);}
    static bool Button(Rect rect,string text,bool selected=false,bool enabled=true)
    {
        bool before=GUI.enabled;GUI.enabled=before&&enabled;
        BattleHudTheme.Round(rect,selected?Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Teal,.25f):BattleHudTheme.Raised);
        BattleHudTheme.Label(rect,text,16,selected?BattleHudTheme.Teal:BattleHudTheme.Text,true,TextAnchor.MiddleCenter);
        bool clicked=GUI.Button(rect,GUIContent.none,GUIStyle.none);GUI.enabled=before;return clicked;
    }
    public static void Draw(Rect panel)
    {
        BattleHudTheme.Surface(new Rect(panel.x+30,panel.y+230,1080,124),true);
        BattleHudTheme.Label(new Rect(panel.x+55,panel.y+242,580,35),"효과음  ·  "+(StarUpgradeAudio.Muted?"음소거":StarUpgradeAudio.Volume+"%"),23,BattleHudTheme.Text,true);
        int[] values={0,20,35,50,75,100};
        for(int i=0;i<values.Length;i++)if(Button(VolumeRect(panel,i),values[i]+"%",StarUpgradeAudio.Volume==values[i]))StarUpgradeAudio.Configure(values[i],StarUpgradeAudio.Muted);
        if(Button(MuteRect(panel),StarUpgradeAudio.Muted?"소리 켜기":"음소거",StarUpgradeAudio.Muted))StarUpgradeAudio.Configure(StarUpgradeAudio.Volume,!StarUpgradeAudio.Muted);
        for(int star=2;star<=3;star++)
        {
            float x=panel.x+30+(star-2)*550;Color tint=StarUpgradeUI.Tint(star);
            BattleHudTheme.Surface(new Rect(x,panel.y+370,530,110),true,tint);
            BattleHudTheme.Label(new Rect(x+25,panel.y+377,480,28),new string('★',star)+(star==3?"  최고 성급 완성":"  성급 강화"),22,tint,true);
            BattleHudTheme.Label(new Rect(x+25,panel.y+407,480,23),star==3?"더 풍성한 상승음으로 완성을 알립니다":"짧고 선명한 소리로 합성을 알립니다",14,BattleHudTheme.Muted);
            if(Button(PreviewRect(panel,star),StarUpgradeAudio.Audible?star+"성 미리 듣기":"소리를 켜면 미리 들을 수 있습니다",false,StarUpgradeAudio.Audible))StarUpgradeAudio.Preview(star);
        }
        BattleHudTheme.Surface(new Rect(panel.x+30,panel.y+494,1080,88),true);
        BattleHudTheme.Label(new Rect(panel.x+55,panel.y+502,1015,27),"전투 효과음  ·  근접 타격과 속성별 명중음을 들려줍니다",18,BattleHudTheme.Text,true);
        if(Button(CombatRect(panel,0),CombatAudio.Enabled?"전투음 켜짐":"전투음 꺼짐",CombatAudio.Enabled))CombatAudio.Configure(!CombatAudio.Enabled);
        bool audible=CombatAudio.Audible;
        if(Button(CombatRect(panel,1),"근접 · 미리 듣기",false,audible))CombatAudio.Preview(CombatSound.Kind.Impact,false);
        if(Button(CombatRect(panel,2),"불꽃 · 미리 듣기",false,audible))CombatAudio.Preview(CombatSound.Kind.Fire,false);
        if(Button(CombatRect(panel,3),"빛 스킬 · 미리 듣기",false,audible))CombatAudio.Preview(CombatSound.Kind.Holy,true);
        if(Button(ResetRect(panel),"기본 음량으로 · 35%"))StarUpgradeAudio.Configure(StarUpgradeAudio.DefaultVolume,false);
        BattleHudTheme.Label(new Rect(panel.x+289,panel.y+596,802,26),"설정은 자동 저장되며 솔로 · 시험 · 온라인에서 함께 사용합니다",14,BattleHudTheme.Text);
        BattleHudTheme.Label(new Rect(panel.x+289,panel.y+625,802,24),"전투음은 따로 끌 수 있습니다 · 연속 합성은 마지막 성급으로 알립니다",13,BattleHudTheme.Muted);
    }
}
