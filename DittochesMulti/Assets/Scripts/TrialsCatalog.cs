using System;

/// <summary>Original PvE encounters. Combat numbers are this game's balance, not source canon.</summary>
public static class TrialsCatalog
{
    [Serializable] public sealed class Enemy
    {
        public string id;public int star,col,row;
        public float healthScale=1,attackScale=1,shield,armor,magicResist,speed;
    }
    [Serializable] public sealed class Wave
    {
        public int number,chapter,gold,xp,item;public string name,description;public bool boss;
        public Enemy[] enemies;public float timeLimit;
    }
    public static readonly string[] DifficultyNames={"입문","도전","혼돈"};
    public static readonly string[] NewEnemyIds={"monochromon","ogremon","meramon","leomon","andromon","skullgreymon","piedmon","machinedramon"};
    public static readonly float[] HealthScale={.82f,1f,1.24f},AttackScale={.78f,1f,1.2f},ScoreScale={1f,1.5f,2.25f};
    static Enemy E(string id,int col,int row,int star=1,float health=1,float attack=1,float shield=0,float armor=0,float resist=0,float speed=0)
    {
        // Encounter budgets remain independent of recruitable champion base stats.
        switch(id)
        {
            case "kuwagamon":health*=0.72916667f;attack*=0.80882353f;break;
            case "shellmon":health*=0.67307692f;attack*=1.14583333f;break;
            case "devimon":health*=0.82352941f;attack*=0.78571429f;break;
            case "monochromon":health*=0.71428571f;attack*=0.92857143f;break;
            case "ogremon":health*=0.75510204f;attack*=0.81081081f;break;
            case "meramon":health*=0.90277778f;attack*=1.06000000f;break;
            case "leomon":health*=0.77659574f;attack*=0.80263158f;break;
            case "etemon":health*=0.66666667f;attack*=0.76388889f;break;
            case "andromon":health*=0.57746479f;attack*=0.67073171f;break;
            case "skullgreymon":health*=0.73076923f;attack*=0.55555556f;break;
            case "piedmon":health*=0.53278689f;attack*=0.53846154f;break;
            case "machinedramon":health*=0.62068966f;attack*=0.56000000f;break;
        }
        return new Enemy{id=id,col=col,row=row,star=star,healthScale=health,attackScale=attack,shield=shield,armor=armor,magicResist=resist,speed=speed};
    }
    static Wave W(int n,string name,string description,params Enemy[] enemies)
    {
        int chapter=(n-1)/5+1;bool boss=n%5==0;int[] prizes={8,6,10,12,13,11};
        return new Wave{number=n,chapter=chapter,name=name,description=description,boss=boss,enemies=enemies,
            gold=6+chapter+(boss?6:0),xp=boss?4:2,item=boss?prizes[chapter-1]:n%2==0?(n/2)%4:-1,timeLimit=boss?60:45};
    }
    public static readonly Wave[] Waves={
        W(1,"숲의 첫 신호","쿠가몬과 우가몬이 길을 막습니다. 전열과 후열을 나누어 배치하세요.",E("kuwagamon",2,3,health:.45f,attack:.42f),E("ogremon",4,3,health:.45f,attack:.42f)),
        W(2,"불꽃의 갈림길","메라몬의 정면 화염과 쿠가몬의 근접 공격을 분산시키세요.",E("meramon",0,3,health:.58f,attack:.5f,speed:.12f),E("kuwagamon",6,3,health:.58f,attack:.5f,speed:.12f)),
        W(3,"해변과 화산","쉘몬과 쿠가몬의 공격에 메라몬의 화염이 이어집니다.",E("shellmon",3,3,health:.8f,attack:.48f,armor:18),E("kuwagamon",2,1,health:.52f,attack:.5f),E("meramon",4,1,health:.52f,attack:.5f)),
        W(4,"야수의 경비대","레오몬·우가몬이 돌격하고 에테몬이 후방을 지원합니다.",E("leomon",2,3,health:.7f,attack:.58f),E("ogremon",4,3,health:.7f,attack:.58f),E("etemon",3,1,health:.6f,attack:.65f)),
        W(5,"모노크로몬의 협곡","갑주와 보호막을 두른 보스 모노크로몬이 화염탄을 내뿜습니다.",E("monochromon",3,3,health:2.4500f,attack:0.8462f,shield:.18f,armor:18),E("shellmon",1,2,health:.65f,attack:.5f),E("meramon",5,2,health:.65f,attack:.5f)),
        W(6,"세 갈래 전열","우가몬·모노크로몬의 단단한 전열과 레오몬의 직선 공격입니다.",E("ogremon",2,3,health:1.15f,attack:.75f,armor:30),E("monochromon",4,3,health:1.15f,attack:.75f,armor:30),E("leomon",3,1,health:.9f,attack:.7f)),
        W(7,"공장의 검은 불꽃","안드로몬의 방어선 뒤에 메라몬과 데블몬이 배치됐습니다.",E("andromon",2,3,health:1.3f,attack:.7f),E("meramon",4,3,health:1.3f,attack:.7f),E("devimon",3,0,health:1.1f,attack:.95f)),
        W(8,"엇갈린 습격","집게·곤봉·주먹·화염의 혼성 부대가 양측에서 압박합니다.",E("kuwagamon",0,3,health:.95f,attack:.8f,speed:.22f),E("ogremon",6,3,health:.95f,attack:.8f,speed:.22f),E("leomon",1,2,health:.85f,attack:.75f),E("meramon",5,2,health:.85f,attack:.75f)),
        W(9,"해안의 열기","마법 저항 쉘몬 뒤에서 메라몬과 에테몬이 공격합니다.",E("shellmon",3,3,health:1.6f,attack:.85f,resist:40),E("meramon",1,0,health:1.15f,attack:.95f),E("etemon",5,0,health:1.15f,attack:.95f)),
        W(10,"레오몬의 결투","보스 레오몬의 직선 수왕권에 여러 기물이 함께 맞지 않게 배치하세요.",E("leomon",3,1,2,2.6849f,0.8566f,resist:35),E("ogremon",2,3,health:1.6f,attack:.8f),E("andromon",4,3,health:1.6f,attack:.8f)),
        W(11,"기계와 갑각","보호막 전열을 안드로몬과 쉘몬이 맡고 메라몬이 지원합니다.",E("andromon",2,3,2,1.05f,.8f,.18f),E("shellmon",4,3,2,1.05f,.8f,.18f),E("meramon",3,0,health:1.4f,attack:1)),
        W(12,"야수와 집게","서로 다른 세 근접 전사가 빠르게 접근합니다.",E("leomon",1,3,2,1.1f,.9f,speed:.18f),E("ogremon",3,3,2,1.1f,.9f,speed:.18f),E("kuwagamon",5,3,2,1.1f,.9f,speed:.18f)),
        W(13,"뼈의 포격","모노크로몬을 앞세운 데블몬과 스컬그레이몬의 원거리 조합입니다.",E("monochromon",3,3,2,1.6f,.85f,armor:35),E("devimon",0,1,2,1.05f,1),E("skullgreymon",6,1,2,1.05f,1)),
        W(14,"혼성 경비병","기계 칼날·곤봉·수왕권 뒤에서 에테몬이 공격합니다.",E("andromon",1,3,2,1.1f,.95f),E("ogremon",3,3,2,1.3f,.85f),E("leomon",5,3,2,1.1f,.95f),E("etemon",3,0,2,1.15f,1.1f)),
        W(15,"스컬그레이몬의 잔해","보스의 미사일 폭발에 대비해 후열 간격을 벌리세요.",E("skullgreymon",3,0,2,3.4079f,1.2375f,shield:.12f,speed:.3f),E("meramon",1,3,2,1.35f,.95f),E("monochromon",5,3,2,1.35f,.95f)),
        W(16,"세 종류의 갑주","물리 방어 55의 기계·갑주·갑각 전열입니다. 마법 공격을 활용하세요.",E("andromon",1,3,2,1.6f,1,armor:55),E("monochromon",3,3,2,1.6f,1,armor:55),E("shellmon",5,3,2,1.6f,1,armor:55)),
        W(17,"뒤틀린 보호막","서로 다른 다섯 적 모두 시작 보호막을 지닙니다.",E("andromon",1,3,2,1.2f,1,.2f),E("leomon",5,3,2,1.2f,1,.2f),E("meramon",1,1,2,1.15f,1.1f,.2f),E("devimon",5,1,2,1.15f,1.1f,.2f),E("etemon",3,0,2,1.2f,1.1f,.2f)),
        W(18,"광란의 혼성대","공격 속도가 높아진 여섯 적입니다. 근접과 화염 역할을 구분하세요.",E("kuwagamon",0,3,2,1.2f,.95f,speed:.4f),E("leomon",2,3,2,1.2f,.95f,speed:.4f),E("ogremon",4,3,2,1.2f,.95f,speed:.4f),E("meramon",6,3,2,1.2f,.95f,speed:.4f),E("andromon",1,1,2,1.1f,.9f,speed:.4f),E("kuwagamon",5,1,2,1.1f,.9f,speed:.4f)),
        W(19,"마검의 전조","저항 전열 뒤에서 피에몬의 검과 어둠의 기술이 이어집니다.",E("monochromon",2,3,2,1.8f,1,resist:60),E("skullgreymon",4,3,2,1.8f,1,resist:60),E("devimon",0,0,2,1.4f,1.2f),E("piedmon",3,0,2,1.4f,1.2f),E("etemon",6,0,2,1.4f,1.2f)),
        W(20,"안드로몬의 통제실","보스 안드로몬의 직선 칼날과 네 종류의 호위대를 돌파하세요.",E("andromon",3,1,2,4.4766f,2.0045f,.15f,30),E("monochromon",2,3,2,2,1.05f),E("shellmon",4,3,2,2,1.05f),E("ogremon",0,2,2,1.5f,1.1f),E("meramon",6,2,2,1.5f,1.1f)),
        W(21,"기계와 야수","3성 안드로몬과 빠른 야수 전열, 후방 미사일의 혼합 편성입니다.",E("andromon",3,3,3,1.5f,1.05f,.18f,40),E("ogremon",0,3,2,1.8f,1.2f,speed:.35f),E("leomon",6,3,2,1.8f,1.2f,speed:.35f),E("skullgreymon",3,0,2,1.7f,1.25f)),
        W(22,"검은 불꽃 분대","갑주 전열이 데블몬·메라몬·피에몬을 엄호합니다.",E("monochromon",2,3,2,2.1f,1.1f,armor:35),E("shellmon",4,3,2,2.1f,1.1f,armor:35),E("devimon",0,1,2,1.9f,1.3f),E("meramon",3,0,2,1.9f,1.3f),E("piedmon",6,1,2,1.9f,1.3f)),
        W(23,"빛을 삼키는 안개","마법 저항 65의 야수·암흑 혼성대입니다. 물리 딜러를 준비하세요.",E("leomon",1,3,3,1.25f,1.05f,resist:65),E("ogremon",5,3,3,1.25f,1.05f,resist:65),E("devimon",1,0,2,2.1f,1.3f,resist:65),E("piedmon",5,0,2,2.1f,1.3f,resist:65)),
        W(24,"폭풍 직전의 포대","서로 다른 세 방패병 뒤에 스컬그레이몬과 에테몬이 있습니다.",E("andromon",1,3,2,2.4f,1.15f),E("monochromon",3,3,2,2.4f,1.15f),E("shellmon",5,3,2,2.4f,1.15f),E("skullgreymon",0,0,3,1.4f,1.2f),E("etemon",6,0,3,1.4f,1.2f)),
        W(25,"피에몬의 마검극","보호막을 두른 보스 피에몬이 네 검을 연속 소환합니다.",E("piedmon",3,1,3,4.5231f,1.3259f,.28f,resist:45),E("andromon",1,3,2,2.3f,1.1f,resist:35),E("monochromon",5,3,2,2.3f,1.1f,resist:35),E("devimon",0,0,2,1.8f,1.3f),E("meramon",6,0,2,1.8f,1.3f)),
        W(26,"최후의 혼성 군단","서로 다른 다섯 3성 전사입니다. 사거리와 공격 방향을 살피세요.",E("kuwagamon",0,3,3,1.5f,1.1f,speed:.2f),E("ogremon",2,3,3,1.5f,1.1f,speed:.2f),E("leomon",4,3,3,1.5f,1.1f,speed:.2f),E("meramon",6,3,3,1.5f,1.1f,speed:.2f),E("andromon",3,1,3,1.5f,1.1f,speed:.2f)),
        W(27,"철벽과 포성","물리 방어 전열과 미사일·어둠의 구체·마검 후열입니다.",E("andromon",1,3,3,1.9f,1.15f,armor:65),E("monochromon",5,3,3,1.9f,1.15f,armor:65),E("skullgreymon",0,0,3,1.5f,1.3f,speed:.25f),E("etemon",3,0,3,1.5f,1.3f,speed:.25f),E("piedmon",6,0,3,1.5f,1.3f,speed:.25f)),
        W(28,"세 갈래 시험","물리 방어·마법 저항 방패와 빠른 레오몬, 세 종류의 후열입니다.",E("shellmon",1,3,3,1.8f,1.15f,armor:60),E("shellmon",5,3,3,1.8f,1.15f,resist:60),E("leomon",3,3,3,1.5f,1.3f,speed:.45f),E("meramon",0,0,3,1.5f,1.25f),E("skullgreymon",6,0,3,1.5f,1.25f),E("piedmon",3,0,3,1.7f,1.3f)),
        W(29,"마지막 수문","보호막을 두른 기계·갑주·야수·미사일·암흑 정예대입니다.",E("andromon",1,3,3,2.2f,1.2f,.25f,35),E("monochromon",5,3,3,2.2f,1.2f,.25f,35),E("ogremon",3,3,3,1.7f,1.3f,.2f,speed:.25f),E("skullgreymon",0,0,3,1.8f,1.35f,.2f),E("devimon",3,0,3,1.8f,1.35f,.2f),E("piedmon",6,0,3,1.8f,1.35f,.2f)),
        W(30,"파워드라몬의 최종 시험","보스의 쌍포가 일직선을 관통합니다. 네 정예 호위와 함께 제한 시간 안에 제압하세요.",E("machinedramon",3,0,3,4.6667f,1.2964f,.25f,35,35,.3f),E("andromon",2,3,3,2,1.15f,.15f,45),E("monochromon",4,3,3,2,1.15f,.15f,45),E("skullgreymon",0,1,3,1.7f,1.3f),E("piedmon",6,1,3,1.7f,1.3f))
    };
    public static Wave Get(int number){return Waves[Math.Max(0,Math.Min(Waves.Length-1,number-1))];}
}
