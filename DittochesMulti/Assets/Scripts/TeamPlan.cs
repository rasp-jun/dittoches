#if DITTOCHES_PORTABLE_PREVIEW
using PlayerPrefs = PortablePreviewPrefs;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Local planning only. Never owns or changes inventory, board, shop odds or combat.</summary>
public static class TeamPlan
{
    public const string Key="dittoches.teamPlans.v1";
    [Serializable] public sealed class Plan {public List<string> units=new List<string>();}
    [Serializable] public sealed class Document {public int version=1,active;public Plan[] plans={new Plan(),new Plan(),new Plan()};}
    static Document data;
    static MultiLauncher.UnitDef[] roster;
    public static MultiLauncher.UnitDef[] Roster {get{return roster??(roster=JsonUtility.FromJson<MultiLauncher.Catalog>(Resources.Load<TextAsset>("MultiRoster").text).units);}}
    public static Document Data {get{if(data==null)data=Decode(PlayerPrefs.GetString(Key,""));return data;}}
    public static List<string> Units {get{return Data.plans[Data.active].units;}}
    public static Document Decode(string json)
    {
        Document value=null;try{if(!string.IsNullOrEmpty(json))value=JsonUtility.FromJson<Document>(json);}catch(Exception){}
        if(value==null)value=new Document();value.version=1;value.active=Mathf.Clamp(value.active,0,2);
        var plans=new Plan[3];
        for(int i=0;i<3;i++)
        {
            var source=value.plans!=null&&i<value.plans.Length&&value.plans[i]!=null?value.plans[i].units:null;
            plans[i]=new Plan{units=(source??new List<string>()).Where(id=>Roster.Any(u=>u.id==id)).Distinct().Take(9).ToList()};
        }
        value.plans=plans;return value;
    }
    public static void Save(){PlayerPrefs.SetString(Key,JsonUtility.ToJson(Data));PlayerPrefs.Save();}
    public static void Restore(string json){data=Decode(json);Save();}
    public static void Reload(){data=null;}
    public static bool Contains(string id){return Units.Contains(id);}
    public static bool Toggle(string id)
    {
        if(!Roster.Any(u=>u.id==id))return false;
        if(Units.Contains(id))Units.Remove(id);else if(Units.Count<9)Units.Add(id);else return false;
        Save();return true;
    }
    public static void Capture(IEnumerable<string> board)
    {Data.plans[Data.active].units=board.Where(id=>Roster.Any(u=>u.id==id)).Distinct().Take(9).ToList();Save();}
    public static MultiLauncher.UnitDef[] Search(string query,int cost)
    {
        query=(query??"").Trim();
        return Roster.Where(u=>(cost==0||u.cost==cost)&&(query.Length==0||u.name.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||
            DigimonBuildCatalog.ForUnit(u.id).Any(t=>t.name.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0))).ToArray();
    }
    public static string Progress(string id,IEnumerable<string> board,IEnumerable<string> bench)
    {return board.Contains(id)?"전장 배치":bench.Contains(id)?"대기석 보유":"모집 필요";}
    public static string Summary(IEnumerable<string> owned)
    {int found=Units.Count(id=>owned.Contains(id));return "플랜 "+(Data.active+1)+" · 확보 "+found+" / "+Units.Count;}
}
