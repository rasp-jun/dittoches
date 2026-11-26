#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public sealed partial class TeamPlanValidation
{
    StarCollection collectionData;
    IEnumerator ValidateCollection(Rect panel)
    {
        int start=checks;
        foreach(var def in TeamPlan.Roster)for(int singles=0;singles<=2;singles++)for(int doubles=0;doubles<=2;doubles++)
        {
            var pieces=Enumerable.Range(0,singles).Select(n=>new StarCollection.Piece(def.id,1,false)).Concat(Enumerable.Range(0,doubles).Select(n=>new StarCollection.Piece(def.id,2,true))).ToArray();
            var data=StarCollection.Create(pieces,new[]{def.id},new[]{def.id},50,true,"","test");var row=data.entries.Single();
            var forecast=PurchaseForecast.Create(def.id,pieces.Select((p,i)=>new PurchaseForecast.Piece(p.id,p.board,i,p.star,null)),9,50,def.cost);
            Require(row.Owned==singles+doubles&&row.Copies==singles+3*doubles&&row.board==doubles&&row.bench==singles,"collection counts actual star weight and locations "+def.id);
            Require(row.CanUpgrade==(forecast.result.star>1)&&(!row.CanUpgrade||row.NextStar==forecast.result.star),"collection immediate upgrade matches purchase simulation "+def.id);
            Require(data.View(1,"").Length==(singles==2?1:0)&&data.View(2,"").Length==1&&data.View(3,def.name).Length==1,"collection filters and search match owned pairs, shop and goals");
            row.allowed=false;Require(!row.CanBuy&&!row.CanUpgrade,"read-only unavailable state cannot promise purchase");row.allowed=true;
            row.gold=def.cost-1;Require(!row.CanBuy&&row.Status.Contains("1G 부족"),"collection uses actual species cost");
        }
        var full=Enumerable.Range(0,9).Select(n=>new StarCollection.Piece(n<2?"agumon":"greymon",n<2?1:3,false)).ToArray();
        var fullData=StarCollection.Create(full,new[]{"agumon","piedmon"},new[]{"piedmon"},50,true,"","test");
        Require(fullData.entries.First(r=>r.id=="agumon").CanUpgrade&&!fullData.entries.First(r=>r.id=="piedmon").CanBuy,"full bench still allows exact-pair merge but blocks plain purchase");
        Require(fullData.entries.First(r=>r.id=="greymon").Complete&&!fullData.entries.First(r=>r.id=="greymon").OneAway,"three-star completion never asks for another copy");
        collectionData=StarCollection.Create(new[]{
            new StarCollection.Piece("agumon",2,true),new StarCollection.Piece("agumon",2,false),new StarCollection.Piece("agumon",1,true),new StarCollection.Piece("agumon",1,false),
            new StarCollection.Piece("greymon",1,true),new StarCollection.Piece("greymon",1,false),new StarCollection.Piece("wargreymon",3,true),
            new StarCollection.Piece("devimon",2,true),new StarCollection.Piece("meramon",1,false)},new[]{"agumon","agumon","greymon","piedmon"},new[]{"agumon","piedmon"},20,true,"","현재 편성 기준");
        string plan=JsonUtility.ToJson(TeamPlan.Data),query=ui.Query;int price=ui.Cost;
        yield return Click(TeamPlannerUI.CollectionButton(panel));Require(ui.CollectionOpen,"collection button opens live overview");
        yield return new WaitForEndOfFrame();Capture("collection-overview");
        yield return Click(TeamPlannerUI.CollectionFilterRect(panel,1));
        Require(ui.CollectionFilter==1&&collectionData.View(1,"").Length==2,"one-away filter retains both two-star and chain-three-star targets");
        yield return new WaitForEndOfFrame();Capture("collection-one-away");
        yield return Click(TeamPlannerUI.CollectionCard(panel,0),1);
        Require(ui.InspectionId=="agumon"&&JsonUtility.ToJson(TeamPlan.Data)==plan,"collection right-click opens the correct sorted target without changing plan");
        yield return new WaitForEndOfFrame();Capture("collection-inspection");
        foreach(var key in new[]{KeyCode.D,KeyCode.F,KeyCode.E,KeyCode.Return})yield return Press(key);
        Require(ui.InspectionId=="agumon"&&ui.CollectionFilter==1&&JsonUtility.ToJson(TeamPlan.Data)==plan,"collection child blocks gameplay keys and parent filtering");
        yield return Press(KeyCode.Escape);Require(ui.CollectionOpen&&ui.InspectionId==""&&open,"first Escape returns from skill details to collection");
        yield return Click(TeamPlannerUI.CollectionFilterRect(panel,3));
        Require(collectionData.View(3,"").Any(r=>r.id=="piedmon"&&r.Copies==0),"plan filter includes missing goals without counting them as owned");
        yield return new WaitForEndOfFrame();Capture("collection-plan-targets");
        ui.CollectionQuery="성역";yield return new WaitForEndOfFrame();Capture("collection-empty-filter");Require(collectionData.View(3,ui.CollectionQuery).Length==0,"empty search does not fabricate owned ranks");
        ui.CollectionQuery="";yield return Click(TeamPlannerUI.CollectionFilterRect(panel,0));
        collectionData=StarCollection.Create(TeamPlan.Roster.Take(37).Select((d,i)=>new StarCollection.Piece(d.id,1,i<28)),new string[0],new string[0],50,true,"","현재 편성 기준");
        yield return null;var last=TeamPlannerUI.CollectionCard(panel,36);
        yield return Click(last,1);Require(ui.InspectionId=="","clipped collection cards cannot open details");
        clickPoint=TeamPlannerUI.CollectionViewport(panel).center;wheelPending=true;while(wheelPending)yield return null;yield return null;
        Require(ui.CollectionScrollY>0,"collection scroll reaches late owned species");last.y-=ui.CollectionScrollY;
        yield return Click(last,1);Require(ui.InspectionId==collectionData.View(0,"")[36].id,"last collection row uses clipped, scrolled coordinates");
        yield return Press(KeyCode.Escape);yield return new WaitForEndOfFrame();Capture("collection-last-row");
        yield return Click(TeamPlannerUI.CollectionButton(panel));
        Require(!ui.CollectionOpen&&open&&ui.Query==query&&ui.Cost==price&&JsonUtility.ToJson(TeamPlan.Data)==plan,"return to team plan preserves goals and planner filters");
        yield return Click(TeamPlannerUI.CollectionButton(panel));yield return Press(KeyCode.Escape);Require(!open,"Escape closes collection modal directly");open=true;ui.ResetView();collectionData=null;
        Debug.Log("STAR COLLECTION PLANNER COMPLETE: "+(checks-start)+" checks; all 47 species, exact three-copy rules, full bench, costs, filters, missing goals, nested inspection and clipped scrolling");
    }
}
#endif
