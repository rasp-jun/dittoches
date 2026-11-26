using UnityEngine;

public sealed partial class MultiLauncher
{
    sealed class RecruitReceipt
    {
        public string id,area,text;public int star,slot;public float until;
    }
    RecruitReceipt recruitReceipt;
    static int RecruitCopies(Player player,string id,int star=0)
    {
        int count=0;foreach(var units in new[]{player.board,player.bench})foreach(var u in units)
            if(u.id==id&&(star==0||u.star==star))count+=star>0?1:u.star==3?9:u.star==2?3:1;
        return count;
    }
    RecruitReceipt RecruitResult(Room before,Room after,Command command)
    {
        if(command==null||command.action!="buy"||before==null||after==null||before.id!=after.id||before.side!=after.side||before.round!=after.round||before.phase!=after.phase||(after.phase!="prepare"&&after.phase!="battle"))return null;
        var prior=MatchPlayer(before);var next=MatchPlayer(after);int index=command.slot;
        if(prior==null||next==null||index<0||index>=prior.shop.Length||index>=next.shop.Length)return null;
        string id=prior.shop[index];var def=Def(id);
        if(def==null||!string.IsNullOrEmpty(next.shop[index])||prior.gold-next.gold!=def.cost||RecruitCopies(next,id)-RecruitCopies(prior,id)!=1)return null;
        int star=1;for(int rank=3;rank>=2;rank--)if(RecruitCopies(next,id,rank)>RecruitCopies(prior,id,rank)){star=rank;break;}
        var result=new RecruitReceipt{id=id,star=star,slot=-1,text=RecruitmentAdvice.Result(def.name,star,def.cost,Mathf.Max(0,next.inventory.Length-prior.inventory.Length))};
        foreach(string area in new[]{"board","bench"})foreach(var u in area=="board"?next.board:next.bench)
        {
            if(u.id!=id||u.star!=star)continue;
            var old=At(area=="board"?prior.board:prior.bench,u.slot);
            if(old==null||old.id!=u.id||old.star!=u.star){result.area=area;result.slot=u.slot;return result;}
        }
        return result;
    }
    void ReceiveRecruitment(Room before,Room after,string path,Command command)
    {
        if(before==null||after==null||before.id!=after.id||before.side!=after.side||before.round!=after.round||before.phase!=after.phase||(after.phase!="prepare"&&after.phase!="battle")){recruitReceipt=null;onlineMergeArrivals.Clear();}
        if(path=="/action"&&command!=null&&(command.action=="move"||command.action=="sell"))onlineMergeArrivals.Clear();
        if(path!="/action")return;
        var result=RecruitResult(before,after,command);if(result==null)return;
        result.until=Time.unscaledTime+StarUpgradeUI.ReceiptDuration;recruitReceipt=result;
        if(artPack==0)StarUpgradeAudio.Request(result.star);
        ReceiveMergeArrival(before,after,result);
    }
    float OnlinePromotion(Unit unit,string area)
    {
        var r=recruitReceipt;
        if(r==null||r.star<2||r.area!=area||r.slot!=unit.slot||r.id!=unit.id||r.star!=unit.star)return 0;
        float duration=StarUpgradeUI.Duration(r.star);
        return Mathf.Clamp01((r.until-Time.unscaledTime-(StarUpgradeUI.ReceiptDuration-duration))/duration);
    }
    void DrawRecruitmentReceipt(Room room)
    {
        if((room.phase!="prepare"&&room.phase!="battle")||recruitReceipt==null||OnlineScouting)return;
        float left=recruitReceipt.until-Time.unscaledTime;if(left<=0)return;
        if(recruitReceipt.star>=2)
        {
            StarUpgradeUI.DrawPromotionNotice(new Rect(500,244,750,70),Def(recruitReceipt.id).name,recruitReceipt.star,room.phase=="battle",left);
        }
        Color saved=GUI.color;GUI.color=new Color(saved.r,saved.g,saved.b,saved.a*Mathf.Clamp01(left/.35f));
        Rect r=OnlineRoundBannerRect;ArenaInterface.Fill(r,new Color(.035f,.095f,.10f,.97f));
        ArenaInterface.Fill(new Rect(r.x,r.y,r.width,2),new Color(.92f,.76f,.35f));
        GUI.Label(r,recruitReceipt.text,centered);GUI.color=saved;
    }
}
