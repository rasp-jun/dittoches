#if DITTOCHES_PORTABLE_PREVIEW
using PlayerPrefs = PortablePreviewPrefs;
#endif
using System;
using UnityEngine;

[Serializable]
public sealed class TamerLoadout
{
    public int version=1,tamer,field,finisher;
    public const string Key="dittoches.tamerLoadout.v1";
    public static readonly string[] Tamers={"비트몬","코로몬","토코몬","어니몬"};
    public static readonly string[] Models={"","koromon","tokomon","pyocomon"};
    public static readonly string[] Fields={"파일 아일랜드","프로스트 서버","황혼의 디지코어"};
    public static readonly string[] Finishers={"데이터 펄스","가이아 임팩트","홀리 게이트"};
    public TamerLoadout Copy(){return new TamerLoadout{tamer=tamer,field=field,finisher=finisher};}
    public void Normalize(){version=1;tamer=Mathf.Clamp(tamer,0,3);field=Mathf.Clamp(field,0,2);finisher=Mathf.Clamp(finisher,0,2);}
    public static TamerLoadout Load(int legacy=-1)
    {
        TamerLoadout result=null;
        if(PlayerPrefs.HasKey(Key))try{result=JsonUtility.FromJson<TamerLoadout>(PlayerPrefs.GetString(Key));}catch(Exception){}
        if(result==null)result=new TamerLoadout{tamer=legacy>=0?legacy:PlayerPrefs.GetInt("multiSoloLegend",0)};
        result.Normalize();return result;
    }
    public void Save(){Normalize();PlayerPrefs.SetString(Key,JsonUtility.ToJson(this));PlayerPrefs.SetInt("multiSoloLegend",tamer);PlayerPrefs.Save();}
    public string Summary {get{return Tamers[tamer]+"  /  "+Fields[field]+"  /  "+Finishers[finisher];}}
    public static Color FieldColor(int id){return id==1?new Color(.34f,.72f,.95f):id==2?new Color(.7f,.4f,1):new Color(.29f,.77f,.65f);}
    public static Color FinishColor(int id){return id==1?new Color(1,.46f,.13f):id==2?new Color(1,.85f,.4f):new Color(.22f,.85f,1);}
    static Texture2D bitmon;
    public static Texture2D Portrait(int id)
    {
        id=Mathf.Clamp(id,0,3);
        if(id>0)return FaithfulPortraits.Get(Models[id]);
        if(bitmon==null)bitmon=Resources.Load<Texture2D>("ArtVariants/Legends/Bitmon-v1");
#if DITTOCHES_PORTABLE_PREVIEW
        if(bitmon==null)bitmon=PortablePreview.Texture("ArtVariants/Legends/Bitmon-v1");
#endif
        return bitmon;
    }
}
