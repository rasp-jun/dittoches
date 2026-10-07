using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    Unit skillDetailUnit;
    bool skillDetailFromShop;
    DigimonSkillCatalog.Stats InspectStats(Unit unit)
    {
        Fighter live=InspectedFighter(unit);
        var ids=board.Contains(unit)?BoardBuildMembers():Enumerable.Empty<DigimonBuildCatalog.Member>();
        var bonus=live!=null?live.build:DigimonBuildCatalog.Resolve(unit.def.id,ids,unit.items);
        var result=new DigimonSkillCatalog.Stats(DigimonSkillCatalog.Find(unit.def.id),unit.star,bonus,live!=null&&live.enemy?live.attackScale:1);
        if(live!=null)result.speed=DigimonCombatMath.AttackSpeed(DigimonSkillCatalog.Find(unit.def.id).attackSpeed,bonus.speed,bonus.rampSpeed,live.combatAge);
        return result;
    }
    string StatContext(Unit unit)
    {return InspectedFighter(unit)!=null?(battling?"현재 전투 능력치 · 우정 중첩 포함":"지난 전투 · 저장된 능력치"):board.Contains(unit)?"장비 + 현재 전장 시너지 적용":"대기석 · 장비만 적용 (시너지 미포함)";}
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
        if(!DigimonSkillUI.DrawDetails(new Rect(1000,184,650,640),skill,skillDetailUnit.star,InspectStats(skillDetailUnit),skillDetailFromShop?"상점 · 1성 기본 능력치 (장비·시너지 미포함)":StatContext(skillDetailUnit)))skillDetailUnit=null;
    }
}
