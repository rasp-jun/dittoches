#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed partial class NativeGame
{
    // Frozen pre-optimization implementation: protects exact placement and tie ordering.
    sealed class OriginalLabelLayout
    {
        public int OverlapChecks;
    readonly List<Rect> occupied=new List<Rect>();
    public void Arrange(List<CombatLabelLayout.Entry> entries,Rect bounds)
    {
        occupied.Clear();OverlapChecks=0;
        // Stable combat identity preserves priority when units pass each other.
        for(int priority=1;priority>=0;priority--)foreach(var entry in entries)
        {
            if(entry.priority!=(priority==1))continue;
            float height=entry.caption?39:19,best=float.MaxValue;Rect chosen=new Rect();
            for(int column=0;column<7;column++)for(int row=0;row<17;row++)
            {
                float dx=column==0?0:((column+1)/2)*(entry.width+7)*(column%2==1?-1:1);
                float dy=row==0?0:((row+1)/2)*23*(row%2==1?-1:1);
                Rect candidate=new Rect(Mathf.Clamp(entry.anchor.x-entry.width*.5f+dx,bounds.x,bounds.xMax-entry.width),
                    Mathf.Clamp(entry.anchor.y-(entry.caption?20:0)+dy,bounds.y,bounds.yMax-height),entry.width,height);
                float overlap=0;
                foreach(var other in occupied)
                {
                    OverlapChecks++;float w=Mathf.Min(candidate.xMax+3,other.xMax+3)-Mathf.Max(candidate.x-3,other.x-3);
                    float h=Mathf.Min(candidate.yMax+2,other.yMax+2)-Mathf.Max(candidate.y-2,other.y-2);
                    if(w>0&&h>0)overlap+=w*h;
                }
                float distance=(candidate.center-new Vector2(entry.anchor.x,entry.anchor.y+(entry.caption?-1:9))).sqrMagnitude;
                float score=overlap*100000+distance;
                if(score<best){best=score;chosen=candidate;}
            }
            entry.rect=chosen;occupied.Add(chosen);
        }
    }
    }
    [Serializable] sealed class HudPerformanceResult
    {
        public int layouts,labels;public long originalOverlapChecks,optimizedOverlapChecks;
        public double originalMedianMs,optimizedMedianMs;
        public string scope="Label placement microbenchmark in portable Unity Mono; not whole-game FPS";
    }
    void ValidateHudPerformance()
    {
        var random=new System.Random(61006);var bounds=new Rect(312,205,956,524);
        var oldLayout=new OriginalLabelLayout();var layout=new CombatLabelLayout();
        var cases=new List<List<CombatLabelLayout.Entry>>();var result=new HudPerformanceResult();
        for(int sample=0;sample<512;sample++)
        {
            var entries=new List<CombatLabelLayout.Entry>();int count=sample%19;
            for(int i=0;i<count;i++)
            {
                Vector2 anchor=sample%5==0?bounds.center:sample%5==1?bounds.min:sample%5==2?bounds.max:
                    new Vector2(bounds.x-80+(float)random.NextDouble()*(bounds.width+160),bounds.y-40+(float)random.NextDouble()*(bounds.height+80));
                entries.Add(new CombatLabelLayout.Entry{key=i,anchor=anchor,width=random.Next(56,147),caption=random.Next(3)==0,priority=random.Next(5)==0});
            }
            oldLayout.Arrange(entries,bounds);result.originalOverlapChecks+=oldLayout.OverlapChecks;
            var expected=entries.Select(e=>e.rect).ToArray();layout.Arrange(entries,bounds);result.optimizedOverlapChecks+=layout.OverlapChecks;
            for(int i=0;i<count;i++)Require(entries[i].rect==expected[i],"HUD placement matches original layout "+sample+" label "+i);
            cases.Add(entries);result.labels+=count;
        }
        result.layouts=cases.Count;Require(result.optimizedOverlapChecks<result.originalOverlapChecks,"label pruning reduces overlap probes");
        var pooled=layout.Borrow(0);layout.Borrow(17);Require(ReferenceEquals(pooled,layout.Borrow(0)),"label pool reuses entries after growth");
        // Alternate order and use medians; timings are evidence, never a flaky pass threshold.
        var originalTimes=new List<double>();var optimizedTimes=new List<double>();
        for(int pass=0;pass<7;pass++)for(int order=0;order<2;order++)
        {
            bool original=(pass+order)%2==0;var watch=System.Diagnostics.Stopwatch.StartNew();
            foreach(var entries in cases){if(original)oldLayout.Arrange(entries,bounds);else layout.Arrange(entries,bounds);}
            watch.Stop();if(pass>0)(original?originalTimes:optimizedTimes).Add(watch.Elapsed.TotalMilliseconds);
        }
        originalTimes.Sort();optimizedTimes.Sort();result.originalMedianMs=originalTimes[originalTimes.Count/2];result.optimizedMedianMs=optimizedTimes[optimizedTimes.Count/2];
        string json=JsonUtility.ToJson(result,true);File.WriteAllText(Path.Combine(Application.dataPath,"../hud-performance.json"),json);
        Debug.Log("HUD PERFORMANCE "+json);
        var table=new CombatReportUI.Table();table.Prepare(0);
        Require(table.Count==0&&table.VisibleCount==0&&table.Total==0&&table.Maximum==1,"empty report table");
        var rows=new List<CombatReportUI.Row>();
        for(int i=0;i<18;i++)
        {
            var row=table.Add();row.key=i;row.damage=(i%5)*.1f;row.taken=i;row.absorbed=20-i;row.healing=17-i;row.shielding=i%3;rows.Add(row);
        }
        for(int metric=0;metric<4;metric++)
        {
            table.Prepare(metric);int m=metric;var expected=rows.OrderByDescending(r=>r.Value(m)).Take(9).ToArray();
            Require(table.Count==18&&table.VisibleCount==9,"report shows top nine without truncating totals");
            Require(table.Total==rows.Sum(r=>r.Value(m))&&table.Maximum==Mathf.Max(1,rows.Max(r=>r.Value(m))),"report aggregate parity metric "+metric);
            for(int i=0;i<9;i++)Require(ReferenceEquals(table.Ranked(i),expected[i]),"stable report rank metric "+metric+" row "+i);
        }
        var first=rows[0];table.Clear();Require(ReferenceEquals(first,table.Add()),"report reuses rows");
        first.damage=0;first.taken=0;first.absorbed=0;table.Prepare(0);
        Require(table.VisibleCount==1&&table.Total==0&&table.Maximum==1,"report shrink removes old ranks and totals");
        table.Clear();table.Prepare(0);Require(table.VisibleCount==0&&table.Total==0,"cleared report has no stale rows");
    }
}
#endif
