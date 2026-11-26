#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class FaithfulModelCacheValidation : MonoBehaviour
{
    [Serializable] sealed class Report {public bool passed;public int checks;public string error;public FaithfulModelData.CacheDiagnostics finalCache;}
    readonly Report report=new Report();
    public static bool TryBoot()
    {
        if(!PortablePreview.HasArgument("--model-cache-smoke"))return false;
        new GameObject("Faithful model cache validation").AddComponent<FaithfulModelCacheValidation>();return true;
    }
    void Require(bool condition,string message)
    {if(!condition)throw new InvalidOperationException(message);report.checks++;}
    IEnumerator Start()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;yield return null;
        var test=FaithfulModelData.ValidateCache(Require);
        while(true)
        {
            bool more=false;object step=null;
            try{more=test.MoveNext();if(more)step=test.Current;}
            catch(Exception error){report.error=error.ToString();Debug.LogError("MODEL CACHE FAILED: "+error);}
            if(!more||report.error!=null)break;yield return step;
        }
        var disposable=test as IDisposable;if(disposable!=null)disposable.Dispose();
        yield return null;yield return null;
        report.finalCache=FaithfulModelData.InspectCache();report.passed=report.error==null;
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","FaithfulCacheValidation"));Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,"cache-report.json"),JsonUtility.ToJson(report,true));
        Debug.Log((report.passed?"MODEL CACHE SMOKE COMPLETE: ":"MODEL CACHE SMOKE FAILED: ")+report.checks+" checks");Application.Quit(report.passed?0:2);
    }
}

public sealed partial class FaithfulModelData
{
    static FaithfulModelData TestAcquire(string id,List<FaithfulModelData> held,List<UnityEngine.Object> objects)
    {
        var data=Acquire(id);held.Add(data);objects.AddRange(data.owned);return data;
    }
    static void TestRelease(FaithfulModelData data,List<FaithfulModelData> held)
    {if(!held.Remove(data))throw new InvalidOperationException("Untracked model release");data.Release();}
    static void TestLimits(Action<bool,string> require)
    {
        var stats=InspectCache();long sum=0;int active=0,refs=0;
        foreach(var data in idle){sum+=data.estimatedBytes;require(data.references==0&&!data.disposed,"LRU contains only live zero-reference model data");}
        foreach(var data in cache.Values)
        {
            if(data.references>0){active++;refs+=data.references;require(data.idleNode==null||data.idleNode.List==null,"active model is never an eviction candidate");}
            else require(data.idleNode!=null&&data.idleNode.List==idle,"zero-reference cached model belongs to LRU");
        }
        require(stats.idleModels<=IdleModelLimit&&stats.idleBytes<=IdleByteLimit&&stats.idleBytes==sum,"idle model count and estimated bytes obey both limits");
        require(stats.models==active+stats.idleModels&&stats.references==refs,"cache diagnostics reconcile active ownership and idle models");
    }
    static void TestZero(Action<bool,string> require,List<UnityEngine.Object> objects,string phase)
    {
        var stats=InspectCache();
        require(stats.owners==0&&stats.models==0&&stats.references==0&&stats.idleModels==0&&stats.idleBytes==0&&stats.ownedObjects==0,"last owner leaves no cached model ownership / "+phase);
        foreach(var resource in objects)require(resource==null,"deferred model resource destruction completes / "+phase);
    }
    static void TestArenaActor(TacticalArena arena,string key,string id,int star=1)
    {
        arena.BeginFrame(-1,-1,-1,false);arena.SetActor(key,TacticalArena.CellWorld(3,4),null,Color.green);
        arena.PoseDigimon(key,id,0,0,Time.unscaledTime,star:star);arena.Render();
    }
    public static IEnumerator ValidateCache(Action<bool,string> require)
    {
        require(InspectCache().models==0&&InspectCache().owners==0,"cache smoke starts without another game's arenas");
        var held=new List<FaithfulModelData>();var objects=new List<UnityEngine.Object>();var leases=new List<IDisposable>();var arenas=new List<TacticalArena>();
        bool previousPreview=DigimonModelLibrary.PreviewEnabled;DigimonModelLibrary.PreviewEnabled=false;
        try
        {
            // Standalone tools neither retain stale materials nor need an arena shutdown hook.
            var alone=TestAcquire("agumon",held,objects);var shared=TestAcquire("agumon",held,objects);
            require(ReferenceEquals(alone,shared)&&alone.references==2,"two consumers share one loaded model");
            TestRelease(shared,held);require(!alone.disposed&&alone.references==1,"one release preserves another active consumer");
            TestRelease(alone,held);require(alone.disposed&&InspectCache().models==0,"standalone zero-reference model releases immediately");
            alone.Release();require(InspectCache().references==0,"duplicate release cannot underflow or remove another model");
            yield return null;yield return null;TestZero(require,objects,"standalone");objects.Clear();

            var firstLease=RetainIdleCache();leases.Add(firstLease);var secondLease=RetainIdleCache();leases.Add(secondLease);
            var active=TestAcquire("greymon",held,objects);
            string[] light={"tokomon","shellmon","tanemon","patamon","lilimon","togemon","piyomon","etemon","palmon","tentomon","gabumon","angemon","herakle"};
            FaithfulModelData oldest=null,second=null;
            for(int i=0;i<12;i++)
            {
                var data=TestAcquire(light[i],held,objects);if(i==0)oldest=data;if(i==1)second=data;
                TestRelease(data,held);TestLimits(require);
            }
            require(InspectCache().idleModels==12,"small real models reach the count budget before the byte budget");
            int loads=cacheLoads,hits=idleHits;var warm=TestAcquire(light[0],held,objects);
            require(ReferenceEquals(warm,oldest)&&cacheLoads==loads&&idleHits==hits+1,"warm model reuses decoded textures and meshes");TestRelease(warm,held);
            var extra=TestAcquire(light[12],held,objects);TestRelease(extra,held);TestLimits(require);
            require(!oldest.disposed&&second.disposed&&!active.disposed&&active.references==1,"LRU evicts the least recent idle model and protects an active model");
            secondLease.Dispose();secondLease.Dispose();require(InspectCache().owners==1&&InspectCache().idleModels==12,"closing one scope twice preserves the other scope");
            firstLease.Dispose();require(InspectCache().idleModels==0&&InspectCache().models==1&&!active.disposed,"last scope clears idle data without invalidating an active reader");
            TestRelease(active,held);yield return null;yield return null;TestZero(require,objects,"count budget and last scope");objects.Clear();

            var heavyLease=RetainIdleCache();leases.Add(heavyLease);long estimates=0;
            string[] heavy={"greymon","agumon","pyocomon","tsunomon","atlur","kabuterimon","garurumon"};
            FaithfulModelData firstHeavy=null;
            foreach(string id in heavy)
            {
                var data=TestAcquire(id,held,objects);if(firstHeavy==null)firstHeavy=data;estimates+=data.estimatedBytes;
                TestRelease(data,held);TestLimits(require);
            }
            require(estimates>IdleByteLimit&&InspectCache().idleModels<heavy.Length&&firstHeavy.disposed,"real large models trim by estimated memory before twelve models");
            heavyLease.Dispose();yield return null;yield return null;TestZero(require,objects,"byte budget");objects.Clear();

            // Native actor destruction happens after Dispose, not synchronously at the call site.
            var arenaA=new TacticalArena(new Rect(0,0,640,480),true,false);arenas.Add(arenaA);
            var arenaB=new TacticalArena(new Rect(0,0,640,480),true,false);arenas.Add(arenaB);
            TestArenaActor(arenaA,"own","agumon");TestArenaActor(arenaB,"scout","agumon",3);
            var live=cache["agumon"];objects.AddRange(live.owned);
            require(live.references==2&&InspectCache().owners==2,"two actual arenas share species data across star sizes");
            arenaA.Dispose();require(InspectCache().owners==1&&!live.disposed,"first arena shutdown leaves the other renderers valid");
            yield return null;yield return null;require(live.references==1&&!live.disposed,"deferred first actor destruction releases exactly one reference");
            arenaB.Dispose();require(InspectCache().owners==0&&InspectCache().idleModels==0,"last arena closes retention before delayed actor destruction");
            yield return null;yield return null;TestZero(require,objects,"actual arena shutdown");objects.Clear();

            var scouting=new TacticalArena(new Rect(0,0,640,480),true,false);arenas.Add(scouting);loads=cacheLoads;hits=idleHits;
            string sourceHash=null;long estimate=0;
            for(int step=0;step<20;step++)
            {
                string id=step%2==0?"agumon":"patamon";TestArenaActor(scouting,"field:"+step,id,step%3+1);
                var data=cache[id];objects.AddRange(data.owned);
                if(step==0){sourceHash=data.sourceHash;estimate=data.estimatedBytes;}
                if(step>0&&step%2==0)require(data.sourceHash==sourceHash&&data.estimatedBytes==estimate&&data.hasBounds,"cached model preserves identity and rest geometry after scout return");
                yield return null;yield return null;TestLimits(require);
            }
            require(cacheLoads-loads==2&&idleHits-hits==18,"twenty field changes decode two species once and reuse eighteen idle entries");
            Debug.Log("MODEL CACHE SCOUT REUSE: 20 field switches, "+(cacheLoads-loads)+" model loads, "+(idleHits-hits)+" idle reuses; budget "+IdleByteLimit+" estimated bytes / "+IdleModelLimit+" idle models (not measured Unity memory)");
            scouting.Dispose();yield return null;yield return null;TestZero(require,objects,"repeated scouting");
            int before=cacheLoads;bool failed=false;
            try{Acquire("cache-validation-missing-model");}catch(FileNotFoundException){failed=true;}
            require(failed&&cacheLoads==before&&InspectCache().models==0,"failed load never publishes or retains a partial cache entry");
        }
        finally
        {
            foreach(var arena in arenas)arena.Dispose();
            foreach(var lease in leases)lease.Dispose();
            foreach(var data in held)data.Release();
            DigimonModelLibrary.PreviewEnabled=previousPreview;
        }
    }
}
#endif
