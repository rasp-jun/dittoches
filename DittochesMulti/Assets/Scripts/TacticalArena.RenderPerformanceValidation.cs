#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Diagnostics;
using UnityEngine;

public sealed partial class TacticalArena
{
    int propertyBlockWrites;
    void RenderWorkBatch(int count,bool oldPath,int variant=0)
    {
        BeginEffects();
        for(int i=0;i<count;i++)
        {
            Mesh mesh=(i+variant)%3==0?hex:cube;Material material=(i+variant)%2==0?stone:glow;
            Vector3 position=new Vector3(i%7*.15f,.4f,i/7*.02f),scale=Vector3.one*.08f;
            Color tint=(i+variant)%2==0?Color.cyan:Color.yellow;Quaternion rotation=Quaternion.Euler(0,i,0);
            if(!oldPath){Effect(mesh,position,scale,tint,rotation,material);continue;}
            var renderer=effectPool[i].renderer;effectCursor++;
            renderer.enabled=true;renderer.sharedMaterial=material;renderer.GetComponent<MeshFilter>().sharedMesh=mesh;
            renderer.transform.localPosition=position;renderer.transform.localScale=scale;renderer.transform.localRotation=rotation;Tint(renderer,tint);
        }
        if(oldPath)for(int i=effectCursor;i<effectPool.Count;i++)effectPool[i].renderer.enabled=false;
        else EndEffects();
    }
    void RestoreEffectMetadata()
    {
        activeEffectCount=effectCursor;
        foreach(var slot in effectPool){slot.mesh=slot.filter.sharedMesh;slot.material=slot.renderer.sharedMaterial;slot.enabled=slot.renderer.enabled;}
    }
    public void ValidateRenderWork(Action<bool,string> require)
    {
        var block=new MaterialPropertyBlock();int variant=0;
        foreach(int count in new[]{0,128,4,0,512,32,0})
        {
            RenderWorkBatch(count,false,variant);
            require(effectCursor==count,"effect pool count matches active draw count");
            for(int i=0;i<effectPool.Count;i++)
            {
                var slot=effectPool[i];require(slot.renderer.enabled==(i<count)&&slot.enabled==slot.renderer.enabled,"shrinking effect pool disables only inactive tail");
                if(i>=count)continue;
                require(slot.filter.sharedMesh==((i+variant)%3==0?hex:cube)&&slot.renderer.sharedMaterial==((i+variant)%2==0?stone:glow),"reused effect restores changed mesh and material");
                slot.renderer.GetPropertyBlock(block);require(block.GetColor("_Color")==((i+variant)%2==0?Color.cyan:Color.yellow),"effect cache preserves tint");
                require((slot.transform.localPosition-new Vector3(i%7*.15f,.4f,i/7*.02f)).sqrMagnitude<.000001f,"effect cache preserves position");
            }
            variant++;
        }
        BeginEffects();for(int i=0;i<128;i++)Effect(cube,Vector3.zero,Vector3.one,Color.white,Quaternion.identity);
        // Even an interrupted render pass must not leave newly enabled slots visible.
        BeginEffects();for(int i=0;i<4;i++)Effect(cube,Vector3.zero,Vector3.one,Color.white,Quaternion.identity);EndEffects();
        require(SkillEffectsHealthy(),"interrupted pass cannot leave stale effect renderers active");
        int objects=effectPool.Count,resources=OwnedResourceCount;
        RenderWorkBatch(512,false);RenderWorkBatch(512,false);
        var oldTimes=new double[7];var newTimes=new double[7];var clock=new Stopwatch();
        for(int sample=0;sample<7;sample++)for(int pass=0;pass<2;pass++)
        {
            bool legacy=(sample+pass)%2==0;RestoreEffectMetadata();clock.Restart();
            for(int iteration=0;iteration<32;iteration++)RenderWorkBatch(512,legacy);
            clock.Stop();if(legacy)oldTimes[sample]=clock.Elapsed.TotalMilliseconds;else newTimes[sample]=clock.Elapsed.TotalMilliseconds;
        }
        RestoreEffectMetadata();Array.Sort(oldTimes);Array.Sort(newTimes);
        require(effectPool.Count==objects&&OwnedResourceCount==resources,"warmed effect rendering creates no new pool slots or owned resources");
        UnityEngine.Debug.Log("RENDER WORK CPU: 32 x 512 effects median old="+oldTimes[3].ToString("F2")+"ms cached="+newTimes[3].ToString("F2")+"ms (submission work, not whole-game FPS)");
        foreach(int field in new[]{0,1,2,0})
        {
            SetField(field);BeginFrame(-1,-1,-1,false);
            int written=propertyBlockWrites;BeginFrame(-1,-1,-1,false);
            require(propertyBlockWrites==written,"unchanged tiles and bench perform zero property block submissions instead of 65");
            for(int i=0;i<tiles.Length;i++){tiles[i].GetPropertyBlock(block);require(block.GetColor("_Color")==tileTint[i],"theme change refreshes cached tile tint");}
            HighlightDestination(34,2,false);BeginFrame(-1,-1,-1,false);
            tiles[34].GetPropertyBlock(block);require(block.GetColor("_Color")==tileTint[34]&&block.GetColor("_Color")!=new Color(1f,.25f,.18f),"destination highlight resets on next frame");
            seats[2].GetPropertyBlock(block);require(block.GetColor("_Color")==new Color(.57f,.43f,.22f),"bench destination returns to base color");
            HighlightHexAttackRange(3,4,2);BeginFrame(31,32,3,true);
            tiles[31].GetPropertyBlock(block);require(block.GetColor("_Color")==new Color(1,.76f,.24f),"selection replaces previous attack range");
            tiles[32].GetPropertyBlock(block);require(block.GetColor("_Color")==new Color(.45f,1,.86f),"hover still wins over placement tint");
        }
        RenderWorkBatch(0,false);require(SkillEffectsHealthy(),"render work test clears all effects");
        UnityEngine.Debug.Log("ARENA TINT WORK: unchanged frame 65 -> 0 property block submissions; selection/range/theme reset verified");
    }
}
#endif
