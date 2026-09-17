using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    Unit skillDetailUnit;
    bool skillDetailFromShop;
    DigimonSkillCatalog.Stats InspectStats(Unit unit)
    {
        Fighter live=InspectedFighter(unit);
        var ids=board.Contains(unit)?board.Where(u=>u!=null).Select(u=>u.def.id):Enumerable.Empty<string>();
        var bonus=live!=null?live.build:DigimonBuildCatalog.Resolve(unit.def.id,ids,unit.items);
        return new DigimonSkillCatalog.Stats(DigimonSkillCatalog.Find(unit.def.id),unit.star,bonus,live!=null&&live.enemy?live.attackScale:1);
    }
    string StatContext(Unit unit)
    {return InspectedFighter(unit)!=null?(battling?"전투 시작 시 확정된 능력치":"지난 전투 · 저장된 능력치"):board.Contains(unit)?"장비 + 현재 전장 시너지 적용":"대기석 · 장비만 적용 (시너지 미포함)";}
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
