using UnityEngine;

public static class PurchaseForecastUI
{
    static GUIStyle body,small;
    public static void Draw(Rect r,PurchaseForecast forecast,string id,string name,bool battle)
    {
        if(body==null){body=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};body.normal.textColor=BattleHudTheme.Text;small=new GUIStyle(body){fontSize=12};small.normal.textColor=BattleHudTheme.Muted;}
        Color tint=forecast.available?StarUpgradeUI.Tint(forecast.result.star):BattleHudTheme.Muted;
        BattleHudTheme.Surface(r,false,tint);
        BattleHudTheme.Label(new Rect(r.x+20,r.y+17,r.width-40,18),"PURCHASE PREVIEW",10,tint,true);
        BattleHudTheme.Label(new Rect(r.x+18,r.y+42,r.width-36,31),"이번 구매의 예상 결과",23,BattleHudTheme.Text,true);
        GUI.Label(new Rect(r.x+20,r.y+80,r.width-40,24),StarUpgradeUI.Progress(forecast.singles,forecast.doubles,forecast.triples),small);
        BattleHudTheme.Round(new Rect(r.x+18,r.y+116,r.width-36,100),BattleHudTheme.Raised);
        FaithfulPortraits.Draw(new Rect(r.x+23,r.y+121,86,90),id,true);
        BattleHudTheme.FitLabel(new Rect(r.x+123,r.y+124,r.width-143,26),name,18,BattleHudTheme.Text);
        BattleHudTheme.Label(new Rect(r.x+123,r.y+153,r.width-143,25),forecast.available?new string('★',forecast.result.star)+(forecast.result.star>=3?" 최고 성급 완성":forecast.result.star==2?" 자동 합성":" 새로 모집"):"현재 구매 불가",18,tint,true);
        GUI.Label(new Rect(r.x+123,r.y+184,r.width-143,22),forecast.available?(forecast.result.board?"전장 기물 유지":"대기석 "+(forecast.result.slot+1)+"번에 배치"):"현재 편성 그대로 유지",small);
        if(!forecast.available)
        {
            GUI.Label(new Rect(r.x+22,r.y+239,r.width-44,68),forecast.reason,body);
            GUI.Label(new Rect(r.x+22,r.y+324,r.width-44,78),"같은 종·같은 성급 3개 → 다음 성급\n2성 3개(1성 총 9개) → 3성\n빈 대기석이 없어도 즉시 합성되면 구매 가능",small);
        }
        else
        {
            GUI.Label(new Rect(r.x+20,r.y+229,r.width-40,25),RecruitmentAdvice.Spend(forecast.gold,forecast.cost),small);
            BattleHudTheme.Label(new Rect(r.x+20,r.y+265,r.width-40,22),"유지 장비  "+forecast.result.items.Length+" / 3",12,tint,true);
            string itemHint="";float cell=(r.width-40)/3;
            for(int i=0;i<3;i++)
            {
                Rect icon=new Rect(r.x+20+i*cell,r.y+293,39,39);BattleHudTheme.Round(icon,BattleHudTheme.Raised);
                if(i>=forecast.result.items.Length){BattleHudTheme.Label(new Rect(icon.x+46,icon.y,cell-50,39),"빈 슬롯",11,BattleHudTheme.Muted);continue;}
                int item=forecast.result.items[i];DigimonEquipmentArt.Draw(icon,item);
                BattleHudTheme.FitLabel(new Rect(icon.x+44,icon.y,cell-47,39),DigimonBuildCatalog.Data.items[item].name,11,BattleHudTheme.Text);
                if(icon.Contains(Event.current.mousePosition))itemHint=DigimonBuildCatalog.Data.items[item].name;
            }
            BattleHudTheme.Label(new Rect(r.x+20,r.y+344,r.width-40,21),"보관함 반환  "+forecast.returned.Count+"개"+(forecast.returned.Count>0?" · 초과 장비 / 중복 인장":""),12,forecast.returned.Count>0?BattleHudTheme.Gold:BattleHudTheme.Muted,true);
            for(int i=0;i<forecast.returned.Count;i++)
            {
                Rect icon=new Rect(r.x+20+i*35,r.y+371,29,29);DigimonEquipmentArt.Draw(icon,forecast.returned[i]);
                if(icon.Contains(Event.current.mousePosition))itemHint=DigimonBuildCatalog.Data.items[forecast.returned[i]].name;
            }
            if(forecast.returned.Count==0)GUI.Label(new Rect(r.x+20,r.y+373,r.width-40,24),"합성 중 잃는 장비가 없습니다",small);
            var stats=forecast.Stats;var skill=DigimonSkillCatalog.Find(id);
            BattleHudTheme.Round(new Rect(r.x+18,r.y+418,r.width-36,92),BattleHudTheme.Raised);
            GUI.Label(new Rect(r.x+29,r.y+427,r.width-58,22),forecast.result.board?"합성 후 · 유지 장비 + 전장 시너지":(forecast.result.star>1?"합성 후":"모집 후")+" · 유지 장비만 적용",small);
            GUI.Label(new Rect(r.x+29,r.y+453,r.width-58,25),"체력 "+stats.health.ToString("0")+" · 공격력 "+stats.attack.ToString("0.#"),body);
            GUI.Label(new Rect(r.x+29,r.y+478,r.width-58,23),"스킬 "+skill.Damage(forecast.result.star,stats.attack,stats.abilityPower).ToString("0.#")+" "+skill.DamageLabel+" / 대상",body);
            if(itemHint.Length>0)BattleHudTheme.Label(new Rect(r.x+20,r.y+513,r.width-40,21),itemHint,12,BattleHudTheme.Gold);
        }
        GUI.Label(new Rect(r.x+20,r.y+539,r.width-40,37),battle?"승급은 다음 전투부터 · 장비는 즉시 반영\n예상치는 전투 시작 전 기준 · 중첩/방어 감소 전":"미리보기는 기물·골드·장비를 바꾸지 않습니다\n상세창을 닫은 뒤 상점에서 구매하세요",small);
    }
}
