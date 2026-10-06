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
        if(command==null||command.action!="buy"||before==null||after==null||before.id!=after.id||before.side!=after.side||before.round!=after.round||before.phase!="prepare"||after.phase!="prepare")return null;
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
        if(before==null||after==null||before.id!=after.id||before.round!=after.round||after.phase!="prepare")recruitReceipt=null;
        if(path!="/action")return;
        var result=RecruitResult(before,after,command);if(result==null)return;
        result.until=Time.unscaledTime+3.4f;recruitReceipt=result;
    }
    float OnlinePromotion(Unit unit,string area)
    {
        var r=recruitReceipt;
        if(r==null||r.star<2||r.area!=area||r.slot!=unit.slot||r.id!=unit.id||r.star!=unit.star)return 0;
        return Mathf.Clamp01((r.until-Time.unscaledTime-1.9f)/1.5f);
    }
    void DrawRecruitmentReceipt(Room room)
    {
        if(room.phase!="prepare"||recruitReceipt==null)return;
        float left=recruitReceipt.until-Time.unscaledTime;if(left<=0)return;
        Color saved=GUI.color;GUI.color=new Color(saved.r,saved.g,saved.b,saved.a*Mathf.Clamp01(left/.35f));
        Rect r=new Rect(500,94,780,28);ArenaInterface.Fill(r,new Color(.035f,.095f,.10f,.97f));
        ArenaInterface.Fill(new Rect(r.x,r.y,r.width,2),new Color(.92f,.76f,.35f));
        GUI.Label(r,recruitReceipt.text,centered);GUI.color=saved;
    }
}
