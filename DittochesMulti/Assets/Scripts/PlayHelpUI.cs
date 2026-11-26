using UnityEngine;

// Shared help and local sound preferences; never changes match or inventory state.
public static class PlayHelpUI
{
    public static Rect Window(Rect canvas){return new Rect(canvas.center.x-570,canvas.center.y-365,1140,730);}
    public static Rect TabRect(Rect r,int tab){return new Rect(r.x+30+tab*270,r.y+116,258,47);}
    public static Rect CloseRect(Rect r){return new Rect(r.xMax-72,r.y+25,42,38);}
    public static bool Draw(Rect canvas,ref int tab,string context,bool online,bool trials,bool combat)
    {
        Rect r=Window(canvas);Event e=Event.current;
        if(e.type==EventType.KeyDown&&(e.keyCode==KeyCode.Escape||e.keyCode==KeyCode.F1)||e.type==EventType.MouseDown&&!r.Contains(e.mousePosition)){e.Use();return false;}
        ArenaInterface.Fill(canvas,new Color(.004f,.011f,.024f,.86f));BattleHudTheme.Surface(r,true,BattleHudTheme.Teal);
        BattleHudTheme.Label(new Rect(r.x+32,r.y+25,900,20),"PLAY GUIDE  /  F1",12,BattleHudTheme.Teal,true);
        BattleHudTheme.Label(new Rect(r.x+30,r.y+51,960,44),"조작 도움말과 소리 설정",30,BattleHudTheme.Text,true);
        if(Button(CloseRect(r),"×",false)){e.Use();return false;}
        string[] tabs={"01  모집과 배치","02  장비와 합성","03  전투와 정찰","04  소리"};
        for(int i=0;i<tabs.Length;i++)if(Button(TabRect(r,i),tabs[i],tab==i))tab=i;
        BattleHudTheme.Badge(new Rect(r.x+32,r.y+181,1076,32),context+(online?" · 온라인 타이머는 계속 진행됩니다":combat?" · 전투는 계속 진행됩니다":" · 준비 중에는 편성을 천천히 살펴보세요"),online||combat?BattleHudTheme.Gold:BattleHudTheme.Teal,14);
        string[,] rows=tab==0?new string[,] {
            {"클릭 / 드래그","적대 디지몬까지 47종을 직접 모집","성숙기 3G · 완전체 4G · 궁극체 5G. 같은 종류 3개를 모으면 다른 종으로 바뀌지 않고 성급이 오릅니다."},
            {"D / F","새로고침 / 경험치 구매","D는 2G, F는 4G를 사용합니다. 검색창에 입력 중에는 먼저 입력을 마치세요."},
            {"W","전장과 대기석 사이 이동","내 유닛 위에 마우스를 올리고 W. 전투 중 전장 이동은 제한됩니다."},
            {"E / 드래그","유닛 판매","내 유닛 위에서 E, 또는 유닛을 상점으로 드래그. 전투 중에는 대기석만 판매합니다."},
            {"상점 잠금","다음 라운드까지 모집 후보 유지","원하는 유닛을 나중에 살 때 잠가 두세요. 구매한 빈칸도 다음 라운드까지 유지됩니다."}}
            :tab==1?new string[,] {
            {"우클릭","장비 옵션과 조합 미리보기","보관함이나 장착 장비를 우클릭하세요. 디지몬마다 장비를 최대 3개 장착합니다."},
            {"합성 가능만","현재 재료로 만들 수 있는 결과 찾기","장비 도감에서 필터를 켜세요. 같은 재료 두 개를 요구하는 조합도 수량을 확인합니다."},
            {"합성하기","효과를 확인하고 보관함에서 합성","조합 결과 클릭은 조회만 합니다. 별도 합성 버튼을 누르면 표시된 재료 2개를 소비합니다."},
            {"클릭 / 드래그","장착 전 능력치 변화 확인","보관함 장비를 선택하고 아군 위에 마우스를 올려 장착 전후 수치를 비교하세요."},
            {"추출기","장비를 보관함으로 회수","추출기를 아군에게 사용하면 장비를 모두 돌려받습니다. 추출기만 소비합니다."}}
            :new string[,] {
            {"ENTER",online?"전투 준비 완료 / 취소":"다음 전투 시작",online?"준비 단계에서 편성을 마친 뒤 누르세요. 준비 완료 후 장착하려면 먼저 준비를 취소하세요.":"모집과 배치를 마친 뒤 시작합니다. 준비 시간은 자동으로 끝나지 않습니다."},
            {"우클릭","유닛의 능력치와 장비 확인","내 유닛과 상대 유닛을 살펴보고 기술 아이콘을 눌러 스킬 상세를 확인하세요."},
            {trials?"정찰 버튼":"Q / R",trials?"30개 시험의 적 배치 살펴보기":"이전 / 다음 테이머 정찰",trials?"로드맵 번호나 적 정보 버튼으로 난이도별 배치와 능력치를 확인합니다.":"상대의 다음 편성을 살펴봅니다. SPACE를 누르면 내 필드로 돌아옵니다."},
            {"전투 기록","누가 피해와 회복을 담당했는지 확인","전투 기록에서 피해량과 받은 피해를 비교하고 유닛을 눌러 상세를 확인하세요."},
            {"ESC / F1","선택 취소 / 도움말 닫기","장비·배치 드래그는 ESC로 취소합니다. 도움말은 ESC 또는 F1로 닫을 수 있습니다."}};
        if(tab==3)StarSoundUI.Draw(r);
        for(int i=0;tab!=3&&i<rows.GetLength(0);i++)
        {
            float y=r.y+230+i*82;BattleHudTheme.Round(new Rect(r.x+30,y,1080,74),i%2==0?BattleHudTheme.Raised:BattleHudTheme.Panel);
            BattleHudTheme.Badge(new Rect(r.x+45,y+20,149,32),rows[i,0],BattleHudTheme.Teal,14);
            BattleHudTheme.Label(new Rect(r.x+219,y+8,833,25),rows[i,1],18,BattleHudTheme.Text,true);
            BattleHudTheme.Label(new Rect(r.x+220,y+39,833,25),rows[i,2],14,BattleHudTheme.Muted);
        }
        BattleHudTheme.Label(new Rect(r.x+34,r.y+663,1070,35),"도움말을 보는 동안 뒤쪽 버튼은 작동하지 않습니다.  ·  ESC / F1 / 바깥 클릭으로 닫기",14,BattleHudTheme.Muted);
        if(e.isMouse||e.isKey||e.type==EventType.ScrollWheel)e.Use();return true;
    }
    static bool Button(Rect r,string text,bool selected)
    {
        BattleHudTheme.Round(r,selected?Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Teal,.26f):BattleHudTheme.Raised);
        BattleHudTheme.Label(r,text,17,selected?BattleHudTheme.Teal:BattleHudTheme.Text,true,TextAnchor.MiddleCenter);
        return GUI.Button(r,GUIContent.none,GUIStyle.none);
    }
}
