using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    Unit skillDetailUnit;
    bool skillDetailFromShop;
    DigimonSkillCatalog.Stats InspectStats(Unit unit)
    {DigimonBuildCatalog.Bonus bonus;Fighter live;return InspectStats(unit,out bonus,out live);}
    DigimonSkillCatalog.Stats InspectStats(Unit unit,out DigimonBuildCatalog.Bonus bonus,out Fighter live)
    {
        live=InspectedFighter(unit);
        var ids=scoutedRival>=0&&scoutBoard.Contains(unit)?InspectedFormationMembers():board.Contains(unit)?BoardBuildMembers():Enumerable.Empty<DigimonBuildCatalog.Member>();
        bonus=live!=null?live.build:DigimonBuildCatalog.Resolve(unit.def.id,ids,unit.items);
        float scale=live!=null&&live.enemy?live.attackScale:scoutedRival>=0&&scoutBoard.Contains(unit)?DifficultyScale[difficulty]*(1+round*.035f):1;
        var result=new DigimonSkillCatalog.Stats(DigimonSkillCatalog.Find(unit.def.id),unit.star,bonus,scale);
        if(live!=null)result.speed=DigimonCombatMath.AttackSpeed(DigimonSkillCatalog.Find(unit.def.id).attackSpeed,bonus.speed,bonus.rampSpeed,live.combatAge);
        if(live!=null){result.health=live.maxHp;result.armor=EffectiveArmor(live);}
        return result;
    }
    string StatContext(Unit unit)
    {return StatContext(unit,InspectedFighter(unit));}
    string StatContext(Unit unit,Fighter live)
    {return live!=null?(battling?(trialsMode&&live.enemy?"현재 전투 능력치 · 시험 적의 개별 보정 포함":"현재 전투 능력치 · 야수 중첩 포함"):"지난 전투 · 저장된 능력치"):scoutedRival>=0&&scoutBoard.Contains(unit)?"상대 장비 + 상대 전장 시너지 적용":board.Contains(unit)?"장비 + 현재 전장 시너지 적용":"대기석 · 장비만 적용 (시너지 미포함)";}
    void OpenSkillDetails(Unit unit)
    {skillDetailUnit=unit;skillDetailFromShop=false;showRecipeGuide=false;traitFocus=null;arenaPointer.Reset();draggingUnit=false;dragSource=-1;}
    void DrawSkillDetails()
    {
#if DITTOCHES_PORTABLE_PREVIEW
        if(validationIconGallery){DrawValidationIcons();return;}
#endif
        if(skillDetailUnit==null)return;
        DrawRect(new Rect(0,0,1920,1080),new Color(0,0,0,.48f));
        var skill=DigimonSkillCatalog.Find(skillDetailUnit.def.id);
        DigimonBuildCatalog.Bonus bonus;Fighter live;var stats=InspectStats(skillDetailUnit,out bonus,out live);
        string collection=skillDetailFromShop||board.Contains(skillDetailUnit)||bench.Contains(skillDetailUnit)||live!=null&&!live.enemy?StarUpgradeUI.Collection(FindUnits(skill.id,1).Count,FindUnits(skill.id,2).Count,FindUnits(skill.id,3).Count):"";
        if(battling&&collection.Length>0)collection=StarUpgradeUI.Progress(FindUnits(skill.id,1).Count,FindUnits(skill.id,2).Count,FindUnits(skill.id,3).Count)+"\n전투 중 승급은 다음 전투부터 적용 · 장비는 즉시 적용";
        if(live!=null&&live.enemy&&trialsMode)collection="시험 적 · 개별 체력/공격 보정이 적용된 성급 비교\n상점에서 모집하는 같은 종은 기본 능력치를 기준으로 합니다";
        if(!DigimonSkillUI.DrawDetails(new Rect(1000,126,650,770),skill,skillDetailUnit.star,stats,skillDetailFromShop?"상점 · 1성 기본 능력치 (장비·시너지 미포함)":StatContext(skillDetailUnit,live),collection))skillDetailUnit=null;
        if(skillDetailUnit!=null&&skillDetailFromShop&&artPack==0)PurchaseForecastUI.Draw(new Rect(540,146,436,590),PreviewPurchase(skillDetailUnit.def),skill.id,UnitName(skillDetailUnit.def),battling);
    }
}
