using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;
using Spine;
using Cysharp.Threading.Tasks;
using System.Threading;
using DG.Tweening;
using System.Linq;
using BCH.Database;
using TMPro;

public class UnitBase : MonoBehaviour, IDamageEvent, INotificationReceiver
{
    // 캐싱 및 기본 선언 변수
    public SkeletonAnimation Ska { get; private set; } // 스켈레톤 애니메이션

    public UNIT_STATE UnitState { get; private set; } // 유닛 스테이트
    [field: SerializeField] public UnitBase EnemyTarget { get; set; } // 식별중인 적

    private StateMachine stateMachine; // 스테이트 머신
    public Dictionary<UNIT_STATE, IState> DicState { get; private set; } // 스테이트 딕셔너리 캐싱

    public Unit_Stat UnitStat { get; private set; } // 유닛 스탯 정보
    public UnitData.UnitSetting UnitSetting { get; private set; } // 유닛 세팅 정보

    [field: SerializeField] public int TeamNum { get; private set; } // 팀 번호
    public string UnitIndex { get; private set; } // 실 내부 데이터 인덱스가 아닌 외부 표출용 인덱스 (스파인, 스킬 데이터 등)

    public float Shield { get; set; } // 쉴드 수치

    public UnitBase UnitBaseParent { get; private set; } // 소환 유닛인 경우 부모 체크
    public bool IsBlockedTarget; // 타겟 방지 여부
    public bool IsStackPosition; // 위치 고정 여부

    [Header("체력 바")]
    [SerializeField] private SpriteRenderer sprrdHPBarBack;
    [SerializeField] private SpriteRenderer sprrdHP;
    [SerializeField] private SpriteRenderer sprrdHPFrame;
    [SerializeField] private SpriteRenderer sprrdHPOutline;
    [SerializeField] private SpriteRenderer sprrdShield;
    [SerializeField] private SpriteRenderer sprrdYellow;
    [SerializeField] private Sprite[] sprHPOutline;

    // Idle, Move, Death 애니메이션 이름 변수
    public string animIdleName = "idle";
    public string animMoveName = "walk";
    public string animDeathName = "death";

    // 유닛 데이터
    private UnitInfo unitInfoData; // 유닛 데이터
    private CatbotInfo catBotInfoData; // 기지 봇 데이터
    private EnemyInfo enemyInfoData; // 몬스터 데이터
    private UserInventory.UserUnit unitLvData; // 현재 유닛 레벨 데이터
    public UserInventory.UserUnit UnitLvData => unitLvData;

    // 기지 스킬 데이터
    public CatbotSkill CatBotSkillData { get; private set; } // 기지 봇 스킬 데이터

    // y축 이동 관련 변수
    public float SpawnYPos { get; private set; } // 초기 소환 Y 좌표

    // 스킬 스크립터블 오브젝트 모듈
    public SOBase_UnitSkillEvent[] soSkillEvent { get; private set; } // 유닛이 지니고 있는 스킬
    public SOBase_UnitSkillEvent CurrUnitSkill { get; private set; } // 현재 사용중인 스킬

    // 스킬 변수
    public float[] Skill_CoolDown { get; private set; } // 쿨타임
    public UnitSkillPersonalVariableInstance[] UnitSkillPersonalVariable { get; private set; } // 유닛 스킬 개인 발동 클래스

    // 스킬 사용 우선순위 변수
    public class SkillPriority
    {
        public int Priority;
        public SOBase_UnitSkillEvent SOSkillEvent;
    }
    public List<SkillPriority> ListSkillPriority = new List<SkillPriority>();

    // 스킬 타임라인 변수
    public PlayableDirector UnitPlayableDirector { get; private set; } // 플레이어블 디렉터
    public PlayableAsset[][] ArrPlayableAssets { get; private set; } // [스킬 인덱스][타임라인 인덱스]

    // UniTask 변수
    private CancellationTokenSource token_Hit; // 히트 UniTask cancel용 토큰

    // 이벤트 변수
    private event System.Action<UnitBase, UnitBase, int> OnDeathAct; // 사망 시 발동 이벤트
    private event System.Action<UnitBase, int> OnKillAct; // 유닛을 죽였을 대 이벤트
    private event System.Action<UnitBase, UnitBase, int, float> OnTakeDamagedAct; // 대미지를 받았을 때 이벤트
    private event System.Action<UnitBase, UnitBase, int, float> OnGiveDamagedAct; // 대미지를 줬을 때 이벤트
    private event System.Action OnEffectUpdate; // 버프,디버프,상태이상 Update 이벤트
    private event System.Func<UnitBase, UnitBase, int, float, float> OnMicoZone; // 미코 버프 장판 이벤트
    private event System.Func<UnitBase, UnitBase, int, float, float> OnFinalDamageAttackerFunc; // 공격자가 대미지를 줬을 때 발동 이벤트
    private event System.Func<UnitBase, UnitBase, int, float, float> OnFinalDamageClientFunc; // 피격자가 대미지를 받았을 때 발동 이벤트

    // 특수 효과 변수 클래스
    public Dictionary<UnitEffect_Status, UnitEffectPersonalVariableInstance> DicCCStatus { get; private set; } = new Dictionary<UnitEffect_Status, UnitEffectPersonalVariableInstance>();
    public List<UnitEffectPersonalVariableInstance> ListEffectPersonalVariable { get; private set; } = new List<UnitEffectPersonalVariableInstance>();

    private Dictionary<UNIT_EFFECT, GameObject> dicUnitEffectVFX = new Dictionary<UNIT_EFFECT, GameObject>();

    [HideInInspector] public float FreezeDmg; // 빙결 중 입은 피해 누적
    [HideInInspector] public GameObject ObjEliteVFX; // 적용중인 엘리트 검은화염 VFX

    // 유닛 개별 시스템 변수
    public List<UnitBase> ListGaramTarget { get; private set; } = new List<UnitBase>(); // 가람 부적 대상
    private event System.Action<UnitBase, UnitBase, float> OnGaramDamaged; // 가람 부적 붙은 타겟이 대미지 입었을 때 발동하는 이벤트 (가람에게 보내주기 위한 이벤트)
    public UnitBase UnitGhostoEffect { get; set; } // 고스토 투사체 피해 감소 버프 시전자

    // 부스터 업그레이드 관련 변수
    public float DefaultAtk { get; private set; } // 초기 공격력
    public float DefaultMaxHP { get; private set; } // 초기 최대 체력

    // 보스용 체력 리젠 변수
    private float multiplyRegenTimer; // 보스 체력 회복 주기 타이머
    private bool isDummyMonster; // 경험치 주지 않는 몬스터인지

    // 기지 장비 관련 변수
    private bool isGearHead_0001_FirstTakeDmg;
    private bool isGearHead_0001_FirstHeal;
    private bool isGearHead_0001_FirstGiveDmgCriRate;
    private bool isGearHead_0002_Activate;
    private CancellationTokenSource token_GearHead_0002;
    private bool isGearHead_0004_Activate;
    private CancellationTokenSource token_GearHead_0004;
    private bool isGearTail_0001_Cooldown;
    private List<UnitBase> listGearTail_0002 = new List<UnitBase>();

    // 틴트 관련 함수
    public bool IsImmortalRunText { get; set; } // 불사 발동 시 텍스트 1회 출력 여부
    public MeshRenderer Meshrd { get; private set; } // 매쉬 렌더러
    private MaterialPropertyBlock mpbNormal; // MPB 기본 값

    // 이동 버벅임 조정 함수
    public float moveLimitTime { get; private set; } // 이동 버벅임 방지 변수
    public void SetLimitTime() => moveLimitTime = 1.5f;

    // 임시 변수
    private List<UnitBase> listUnitTemp = new List<UnitBase>();

    // 엘리트 유닛 변수
    private float elitePassiveCooldown = 0.0f; // 엘리트 몹 검은 화염 쿨타임
    private int eliteResistUnitEffectCnt = 0; // 상태이상 저항 가중 수치

    #region 유닛 초기화
    private void Awake()
    {
        Ska = transform.GetChild(0).GetComponent<SkeletonAnimation>();
        UnitPlayableDirector = GetComponent<PlayableDirector>();
    }

    /// <summary>
    /// 유닛 세팅 함수
    /// </summary>
    /// <param name="_unitSetting">세팅할 유닛 데이터</param>
    /// <param name="_teamNum">팀 번호 (0 / 1)</param>
    /// <param name="_unitBaseParent">소환몹인 경우 부모 유닛</param>
    /// <param name="_subIndex">현혹,엘리트 용 할당 유닛 인덱스</param>
    public void SetUnit(UnitData.UnitSetting _unitSetting, int _teamNum, UnitBase _unitBaseParent = null, string _subIndex = null)
    {
        // 기본적인 개인 변수 세팅
        TeamNum = _teamNum;
        UnitSetting = _unitSetting;
        UnitIndex = UnitSetting.unitIndex;
        UnitBaseParent = _unitBaseParent;
        
        InitUnitEvent(); // 유닛에게 세팅되는 이벤트 초기화

        InitUnitVariable(); // 유닛에게 세팅되는 개인 변수 초기화

        // 유닛 리스트 등록
        MgrBattleSystem.Instance.AddUnitBase(this);

        // 유닛 데이터 세팅
        if (UnitBaseParent is null)
        {
            switch (UnitSetting.unitType)
            {
                case UnitType.Unit:
                    unitInfoData = MgrInGameData.Instance.GetUnitDBData(UnitSetting.unitIndex);
                    break;
                case UnitType.AllyBase:
                    catBotInfoData = MgrInGameData.Instance.GetCatbotDBData(UnitSetting.unitIndex);
                    CatBotSkillData = DataManager.Instance.GetCatbotSkillData(UnitSetting.unitIndex);
                    break;
                default:
                    enemyInfoData = MgrInGameData.Instance.GetEnemyDBData(UnitSetting.unitIndex);
                    break;
            }
        }
        else
        {
            unitInfoData = UnitBaseParent.unitInfoData;
            enemyInfoData = UnitBaseParent.enemyInfoData;
        }

        // 유닛 스탯 설정
        UnitStat = new Unit_Stat();
        InitUnitStat();
        
        // 스킬 및 스파인 초기화
        if(UnitSetting.unitType == UnitType.EnemyUnit || UnitSetting.unitType == UnitType.Elite)
        {
            if (unitLvData is null)
            {
                unitLvData = new UserInventory.UserUnit();
                unitLvData.lv = 1;
                unitLvData.promotion = 0;
            }

            UnitIndex = _subIndex;
            UnitData.UnitSetting setting = MgrInGameData.Instance.GetUnitData(UnitIndex);
            SetSkeletonDataAsset(setting.spineDataAsset);

            if(UnitBaseParent is null)
                UnitStat.Range = (float)MgrInGameData.Instance.GetUnitDBData(UnitIndex).Range;

            if (CheckHasMaskSkin())
                Ska.skeleton.SetSkin("mask");

            InitUnitSkill(setting);
        }
        else
        {
            SetSkeletonDataAsset(UnitSetting.spineDataAsset);

            InitUnitSkill(UnitSetting);
        }

        // 스파인 이벤트 초기화
        InitSpineEvent();

        // 엘리트 스파인 크기 변동
        float skaScale;
        switch(UnitSetting.unitType)
        {
            case UnitType.Boss:
                skaScale = UnitBaseParent is null ? 1.2f : 1.0f;
                break;
            case UnitType.Elite:
                skaScale = 1.3f;
                break;
            default:
                skaScale = 1.0f;
                break;
        }
        Ska.transform.localScale = Vector3.one * skaScale;
        
        // 타임라인 초기화
        BindingPlayableDirector();

        DefaultAtk = UnitStat.Atk;
        DefaultMaxHP = UnitStat.MaxHP;

        // 부스터 : 유닛 업그레이드 효과 적용
        CalculateBoosterUnitStat();

        // 유닛 스킬 초기화
        ResetUnitSkill();

        // 유닛 상태 초기화
        InitStateMachine();
        UpdateSpineUnitSpineSpeed();

        // 각종 이벤트 초기화
        InitUnitInGameEvent();
        SetInitSpawnRotation();

        if(UnitSetting.unitType == UnitType.Elite)
        {
            ObjEliteVFX = MgrObjectPool.Instance.ShowObj("FX_Elite Aura", GetUnitCenterPos());
            ObjEliteVFX.transform.SetParent(transform);
        }

        // HP Bar 초기화
        sprrdHPBarBack.transform.localPosition = Vector3.up * GetUnitHeight() + new Vector3(0.0f, 0.0f, -0.001f);
        if(UnitSetting.unitType != UnitType.AllyBase) sprrdHPBarBack.gameObject.SetActive(true);
        else sprrdHPBarBack.gameObject.SetActive(TeamNum != 0);
        sprrdHP.sprite = TeamNum == 0 ? MgrBattleSystem.Instance.sprAlly : MgrBattleSystem.Instance.sprEnemy;
        // 임시
        tfTempCoolDown.transform.localPosition = sprrdHPBarBack.transform.localPosition + Vector3.up * 0.25f;

        if(UnitSetting.unitType == UnitType.MidBoss && MgrBattleSystem.Instance.currWave == MgrBattleSystem.Instance.totalWave && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter && UnitBaseParent is null)
        {
            sprrdHPOutline.sprite = sprHPOutline[MgrBattleSystem.Instance.listUnitBoss.Count];
            sprrdHPOutline.gameObject.SetActive(true);
        }
        else sprrdHPOutline.gameObject.SetActive(false);

        // 준보스, 보스 체력 리젠 태스크
        if ((UnitSetting.unitType == UnitType.MidBoss || UnitSetting.unitType == UnitType.Boss) && UnitBaseParent is null && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter)
        {
            multiplyRegenTimer = 15.0f;
            TaskBossHPRegen().Forget();
        }

        SpawnYPos = transform.position.y;
    }

    /// <summary>
    /// 유닛 리스폰
    /// </summary>
    /// <param name="_teamNum">리스폰 시킬 팀 번호</param>
    public void RespawnUnit(int _teamNum)
    {
        TeamNum = _teamNum;

        InitUnitEvent();

        InitUnitVariable();

        // 스파인 및 스킬 초기화
        if (UnitSetting.unitType == UnitType.EnemyUnit || UnitSetting.unitType == UnitType.Elite)
        {
            UnitData.UnitSetting setting = MgrInGameData.Instance.GetUnitData(UnitIndex);
            SetSkeletonDataAsset(setting.spineDataAsset);
        }
        else SetSkeletonDataAsset(UnitSetting.spineDataAsset);

        if ((UnitSetting.unitType == UnitType.EnemyUnit || UnitSetting.unitType == UnitType.Elite) && CheckHasMaskSkin())
            Ska.skeleton.SetSkin("mask");

        InitSpineEvent();

        InitUnitStat();

        // 유닛 리스트 등록
        MgrBattleSystem.Instance.AddUnitBase(this);

        for (int i = 0; i < soSkillEvent.Length; i++)
            soSkillEvent[i].InitializeSkill(this);

        DefaultAtk = UnitStat.Atk;
        DefaultMaxHP = UnitStat.MaxHP;

        CalculateBoosterUnitStat();

        ResetUnitSkill();

        // 유닛 상태 초기화
        SetUnitState(UNIT_STATE.IDLE, true);
        UpdateSpineUnitSpineSpeed();

        // 각종 이벤트 초기화
        InitUnitInGameEvent();
        SetInitSpawnRotation();

        if (UnitSetting.unitType != UnitType.AllyBase) sprrdHPBarBack.gameObject.SetActive(true);
        else sprrdHPBarBack.gameObject.SetActive(TeamNum != 0);

        SpawnYPos = transform.position.y;
    }

    /// <summary>
    /// 스파인 데이터 에셋 세팅
    /// </summary>
    /// <param name="_asset">스켈레톤 데이터 에셋</param>
    private void SetSkeletonDataAsset(SkeletonDataAsset _asset)
    {
        // 만약 이미 동일한 데이터면 상태만 클리어 하도록 최적화
        if (Ska.SkeletonDataAsset != _asset)
        {
            Ska.skeletonDataAsset = _asset;
            Ska.Initialize(true);
        }
        else Ska.ClearState();

        InitUnitColor();
    }

    /// <summary>
    /// 유닛 스파인 색상 초기화
    /// </summary>
    private void InitUnitColor()
    {
        Color color = Color.white, black = Color.black;
        if (UnitSetting.unitType == UnitType.Elite)
        {
            ColorUtility.TryParseHtmlString("#E2C8C8", out color);
            ColorUtility.TryParseHtmlString("#3A1E1E", out black);
        }

        mpbNormal = new MaterialPropertyBlock();
        mpbNormal.SetColor("_Color", color);
        mpbNormal.SetColor("_Black", black);

        Meshrd = Ska.GetComponent<MeshRenderer>();
        Meshrd.SetPropertyBlock(mpbNormal);

        Ska.skeleton.SetColor(Color.white);
    }

    /// <summary>
    /// 개인 변수 초기화
    /// </summary>
    private void InitUnitVariable()
    {
        isDummyMonster = false;
        IsBlockedTarget = false;
        IsStackPosition = false;
        UnitGhostoEffect = null;

        isGearHead_0001_FirstTakeDmg = false;
        isGearHead_0001_FirstHeal = false;
        isGearHead_0001_FirstGiveDmgCriRate = false;
        isGearHead_0002_Activate = false;
        isGearHead_0004_Activate = false;
        token_GearHead_0004?.Cancel();
        isGearTail_0001_Cooldown = false;

        eliteResistUnitEffectCnt = 0;

        UserGear gearHead = DataManager.Instance.GetUsingGearInfo(3);
        if(gearHead is not null && gearHead.gearId.Equals("gear_head_0002") && UnitSetting.unitType == UnitType.Unit)
        {
            token_GearHead_0002?.Cancel();
            token_GearHead_0002?.Dispose();
            token_GearHead_0002 = new CancellationTokenSource();
            TaskGearHead_0002().Forget();
        }
    }

    /// <summary>
    /// 유닛 스탯 초기화
    /// </summary>
    private int pvpUnitStatCnt = 0;
    private void InitUnitStat()
    {
        Shield = 0.0f;

        if (UnitSetting.unitType == UnitType.Unit) // 유닛
        {
            float moveSpeedMultiply = 1.0f;
            UserGear gearTail = DataManager.Instance.GetUsingGearInfo(5);
            if (gearTail is not null && gearTail.gearId.Equals("gear_tail_0004"))
            {
                if(gearTail.gearRarity >= 1) moveSpeedMultiply += (float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 0);
            }
            moveSpeedMultiply += MgrBattleSystem.Instance.GlobalOption.Option_UnitMoveSpeed;

            if(MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode || MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp) moveSpeedMultiply += 0.3f;

            if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter) // 튜토리얼 용 강제 세팅
            {
                unitLvData = new UserInventory.UserUnit();
                unitLvData.lv = 20;
                unitLvData.promotion = 5;
            }
            else if (MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp) // PVP 용 강제 세팅
            {
                DataManager.Instance.UserInventory.unitInventory.TryGetValue(DataManager.Instance.UserInventory.unitDeck[MgrInGameUserData.Instance.CurrUnitDeckIndex][pvpUnitStatCnt], out unitLvData);
                if (unitLvData is null)
                {
                    unitLvData = new UserInventory.UserUnit();
                    unitLvData.lv = 1;
                    unitLvData.promotion = 0;
                }
                pvpUnitStatCnt++;
                if (pvpUnitStatCnt >= 6)
                    pvpUnitStatCnt = 0;
            }
            else
            {
                // 튜토리얼이 아닐 경우 본인 유닛 데이터 가져오도록 호출
                // 단, 데이터가 없을 경우 [레벨1, 돌파0] 으로 강제 세팅
                DataManager.Instance.UserInventory.unitInventory.TryGetValue(UnitSetting.unitIndex, out unitLvData);
                if (unitLvData is null)
                {
                    unitLvData = new UserInventory.UserUnit();
                    unitLvData.lv = 1;
                    unitLvData.promotion = 0;
                }
            }

            // 각 특성, 장비 효과 등등 으로 변동되는 스탯 반영
            float unitHpPerLv = Mathf.Floor(unitInfoData.Hp * 0.15f + 0.5f);
            float unitAtkPerLv = Mathf.Floor(unitInfoData.AtkPower * 0.15f + 0.5f);

            float gearAtkBase = GetGearStatusBase(0, 1) + GetGearStatusBase(1, 1) + GetGearStatusBase(2, 1);
            float gearHpBase = GetGearStatusBase(3, 1) + GetGearStatusBase(4, 1) + GetGearStatusBase(5, 1);

            float gearAtkPerLv = GetGearStatusPerLv(0, 1) + GetGearStatusPerLv(1, 1) + GetGearStatusPerLv(2, 1);
            float gearHpPerLv = GetGearStatusPerLv(3, 1) + GetGearStatusPerLv(4, 1) + GetGearStatusPerLv(5, 1);
            
            float gearAtkPerRarity = GetGearStatusPerRarity(0, 1) + GetGearStatusPerRarity(1, 1) + GetGearStatusPerRarity(2, 1);
            float gearHpPerRarity = GetGearStatusPerRarity(3, 1) + GetGearStatusPerRarity(4, 1) + GetGearStatusPerRarity(5, 1);

            float gearMultiplyAtk = 1.0f + GetGearStatusMultiply(0, 3) + GetGearStatusMultiply(1, 3);
            float gearMultiplyHp = 1.0f + GetGearStatusMultiply(3, 3) + GetGearStatusMultiply(4, 3) + GetGearStatusMultiply(5, 3);
            
            UnitStat.Cost = unitInfoData.Cost;
            UnitStat.Range = (float)unitInfoData.Range;
            UnitStat.HP = (unitInfoData.Hp + (unitHpPerLv * (unitLvData.lv)) + MgrBattleSystem.Instance.GlobalOption.TraitUnitHP + gearHpBase + gearHpPerLv + gearHpPerRarity) * gearMultiplyHp;
            UnitStat.MaxHP = UnitStat.HP;
            UnitStat.Atk = (unitInfoData.AtkPower + (unitAtkPerLv * (unitLvData.lv)) + MgrBattleSystem.Instance.GlobalOption.TraitUnitAtk + gearAtkBase + gearAtkPerLv + gearAtkPerRarity) * gearMultiplyAtk;
            UnitStat.MoveSpeed = (float)unitInfoData.MoveSpd * moveSpeedMultiply;
            UnitStat.CriRate = (float)unitInfoData.CriRate;
            UnitStat.SoMove = UnitSetting.moveSO;

            if (gearTail is not null && gearTail.gearId.Equals("gear_tail_0003") && gearTail.gearRarity > 0)
                MgrBattleSystem.Instance.GlobalOption.Add_GearTail_0003();

            if (MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp)
            {
                UnitStat.HP *= 2.0f;
                UnitStat.MaxHP = UnitStat.HP;
            }
        }
        else if (UnitSetting.unitType == UnitType.AllyBase) // 기지
        {
            // 각 특성, 장비 효과 등등 으로 변동되는 스탯 반영
            float gearAtkBase = GetGearStatusBase(0, 0) + GetGearStatusBase(1, 0) + GetGearStatusBase(2, 0);
            float gearHpBase = GetGearStatusBase(3, 0) + GetGearStatusBase(4, 0) + GetGearStatusBase(5, 0);

            float gearAtkPerLv = GetGearStatusPerLv(0, 0) + GetGearStatusPerLv(1, 0) + GetGearStatusPerLv(2, 0);
            float gearHpPerLv = GetGearStatusPerLv(3, 0) + GetGearStatusPerLv(4, 0) + GetGearStatusPerLv(5, 0);

            float gearAtkPerRarity = GetGearStatusPerRarity(0, 0) + GetGearStatusPerRarity(1, 0) + GetGearStatusPerRarity(2, 0);
            float gearHpPerRarity = GetGearStatusPerRarity(3, 0) + GetGearStatusPerRarity(4, 0) + GetGearStatusPerRarity(5, 0);

            float gearMultiplyAtk = 1.0f + GetGearStatusMultiply(0, 1) + GetGearStatusMultiply(1, 1);
            float gearMultiplyHp = 1.0f + GetGearStatusMultiply(3, 1) + GetGearStatusMultiply(4, 1) + GetGearStatusMultiply(5, 1);

            UnitStat.HP = (catBotInfoData.Hp + MgrBattleSystem.Instance.GlobalOption.TraitAllyBaseHP + gearHpBase + gearHpPerLv + gearHpPerRarity) * gearMultiplyHp;

            UnitStat.MaxHP = UnitStat.HP;
            UnitStat.Atk = (catBotInfoData.Power + MgrBattleSystem.Instance.GlobalOption.TraitAllyBaseAtk + gearAtkBase + gearAtkPerLv + gearAtkPerRarity) * gearMultiplyAtk;
            UnitStat.CriRate = (float)catBotInfoData.CriRate;
            UnitStat.SoMove = UnitSetting.moveSO;

            if(MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp)
            {
                UnitStat.HP *= 1.5f;
                UnitStat.MaxHP = UnitStat.HP;
            }
        }
        else // 이외는 다 몬스터
        {
            // 테마 및 웨이브 별 스탯 보정 수치
            //float themaStat = 1.0f + (0.5f * (MgrBattleSystem.Instance.ChapterID - 1)) + (0.6f * (MgrBattleSystem.Instance.GetCurrentThema() - 1));
            //float waveStat = (1.0f + (0.05f * (MgrBattleSystem.Instance.currWave - 1)));
            // TODO 퍼블리셔 제공 테스트 스텟
            float themaStat = 1.0f + (0.25f * (MgrBattleSystem.Instance.ChapterID - 1)) + (0.3f * (MgrBattleSystem.Instance.GetCurrentThema() - 1));
            float waveStat = (1.0f + (0.025f * (MgrBattleSystem.Instance.currWave - 1)));

            if (MgrBattleSystem.Instance.GameMode is GAME_MODE.GoldMode or GAME_MODE.Survival)
                themaStat = 1.0f;

            // 도전모드 스탯 변동
            float challengeHpMultiply = 1.0f;

            // 각 특성, 장비 효과 등등 으로 변동되는 스탯 반영
            float hpMultiply = 1.0f;
            float atkMultiply = 1.0f;
            float criMultiply = 0.0f;
            float moveSpeedMultiply = 1.0f;

            UserGear gearTail = DataManager.Instance.GetUsingGearInfo(5);
            if (gearTail is not null)
            {
                if (gearTail.gearId.Equals("gear_tail_0003") && (UnitSetting.unitType == UnitType.Monster || UnitSetting.unitType == UnitType.EnemyUnit))
                {
                    if (gearTail.gearRarity >= 1) criMultiply += (float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 0) * MgrBattleSystem.Instance.GlobalOption.GearTail_0003_SpawnCnt;
                    if (gearTail.gearRarity >= 3) atkMultiply += (float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 2) * MgrBattleSystem.Instance.GlobalOption.GearTail_0003_SpawnCnt;
                    if (gearTail.gearRarity >= 10) hpMultiply += (float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 4) * MgrBattleSystem.Instance.GlobalOption.GearTail_0003_SpawnCnt;
                }

                if (gearTail.gearId.Equals("gear_tail_0004"))
                {
                    if (gearTail.gearRarity >= 3) moveSpeedMultiply += (float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 2);
                    if (gearTail.gearRarity >= 10) moveSpeedMultiply += (float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 4);
                }
            }

            // 도전 모드
            if (MgrBattleSystem.Instance.IsChallengeMode)
            {
                // 도전모드 공,체 % 증가는 월드 효과로 지정 되어 있음
                switch(MgrBattleSystem.Instance.ChallengeLevel)
                {
                    case 0:
                        challengeHpMultiply += (float)DataManager.Instance.GetChallengePenaltyData("penalty_000000").Param[1];
                        break;
                    case 1:
                        challengeHpMultiply += (float)DataManager.Instance.GetChallengePenaltyData("penalty_000001").Param[1];
                        break;
                    case 2:
                        challengeHpMultiply += (float)DataManager.Instance.GetChallengePenaltyData("penalty_000004").Param[1];
                        moveSpeedMultiply += (float)DataManager.Instance.GetChallengePenaltyData("penalty_000004").Param[2];
                        break;
                }
            }

            moveSpeedMultiply += MgrBattleSystem.Instance.GlobalOption.Option_EnemyMoveSpeed;

            UnitStat.Atk = enemyInfoData.Power * themaStat * waveStat * atkMultiply;
            UnitStat.HP = enemyInfoData.Hp * themaStat * waveStat * hpMultiply * challengeHpMultiply;
            UnitStat.MaxHP = UnitStat.HP;
            UnitStat.Range = (float)enemyInfoData.Range;
            UnitStat.MoveSpeed = (float)enemyInfoData.MoveSpd * moveSpeedMultiply;
            UnitStat.CriRate = (float)enemyInfoData.CriRate + criMultiply;
            UnitStat.SoMove = UnitSetting.moveSO;

            if (UnitSetting.unitType == UnitType.Monster)
            {
                // 몬스터 원거리/탱커 사거리 1.2배
                if (UnitSetting.unitClass == UnitClass.Arch || UnitSetting.unitClass == UnitClass.Tank)
                    UnitStat.Range *= 1.2f;

                // 몬스터 사정거리 +-10% 랜덤
                UnitStat.Range *= Random.Range(0.9f, 1.1f);
            }

            if(MgrBattleSystem.Instance.GameMode == GAME_MODE.Training)
            {
                UnitStat.HP = 100000.0f;
                UnitStat.MaxHP = UnitStat.HP;
                UnitStat.Atk = 500;
            }

            isDummyMonster = (UnitSetting.unitType == UnitType.MidBoss || UnitSetting.unitType == UnitType.Boss) ? false : MgrBattleSystem.Instance.IsBossAppeared;
        }
    }

    private float GetGearStatusBase(int _gearIndex, int _statIndex)
    {
        UserGear gear = DataManager.Instance.GetUsingGearInfo(_gearIndex);
        if (gear is null || gear.gearLv == -1)
            return 0.0f;

        return (float)DataManager.Instance.GetGearStatusValue(gear.gearId, _statIndex);
    }
    
    private float GetGearStatusPerLv(int _gearIndex, int _statIndex)
    {
        UserGear gear = DataManager.Instance.GetUsingGearInfo(_gearIndex);
        if (gear is null || gear.gearLv == -1)
            return 0.0f;

        float baseStat = (float)DataManager.Instance.GetGearStatusValue(gear.gearId, _statIndex);
        float perLvStat = Mathf.Floor(baseStat * (_statIndex == 0 ? 0.2f : 0.1f) + 0.5f);
        return (perLvStat * (gear.gearLv));
    }
    
    private float GetGearStatusPerRarity(int _gearIndex, int _statIndex)
    {
        UserGear gear = DataManager.Instance.GetUsingGearInfo(_gearIndex);
        if (gear is null || gear.gearLv == -1)
            return 0.0f;

        float baseStat = (float)DataManager.Instance.GetGearStatusValue(gear.gearId, _statIndex);
        float perRarityStat = Mathf.Floor(baseStat * (_statIndex == 0 ? 0.3f : 0.2f) + 0.5f);
        return (perRarityStat * gear.gearRarity);
    }
    
    private float GetGearStatusMultiply(int _gearIndex, int _optionIndex)
    {
        UserGear gear = DataManager.Instance.GetUsingGearInfo(_gearIndex);
        if (gear is null || gear.gearLv == -1 || (_optionIndex >= 1 && gear.gearRarity < 2) || (_optionIndex >= 3 && gear.gearRarity < 6))
            return 0.0f;

        return (float)DataManager.Instance.GetGearOptionValue(gear.gearId, _optionIndex);
    }

    private void InitUnitSkill(UnitData.UnitSetting _setting)
    {
        ListSkillPriority.Clear();

        // 유닛 스킬 스크립터블 모듈 초기화
        soSkillEvent = new SOBase_UnitSkillEvent[_setting.unitSkill.Length];
        ArrPlayableAssets = new PlayableAsset[_setting.unitSkill.Length][];
        Skill_CoolDown = new float[_setting.unitSkill.Length];
        UnitSkillPersonalVariable = new UnitSkillPersonalVariableInstance[_setting.unitSkill.Length];
        for (int i = 0; i < _setting.unitSkill.Length; i++)
        {
            if (_setting.unitSkill[i] is null)
                continue;

            soSkillEvent[i] = _setting.unitSkill[i];
            soSkillEvent[i].InitializeSkill(this);

            SkillPriority sPriority = new SkillPriority();
            if ((_setting.unitType == UnitType.MidBoss || _setting.unitType == UnitType.Boss) && UnitBaseParent is null) sPriority.Priority = UnitSkillPersonalVariable[i].bossSkillData.Priority;
            else if (UnitSkillPersonalVariable[i].unitSkillData is null) sPriority.Priority = (_setting.unitSkill.Length - 1) - i;
            else sPriority.Priority = UnitSkillPersonalVariable[i].unitSkillData.Priority ? 1 : 0;
            sPriority.SOSkillEvent = soSkillEvent[i];
            ListSkillPriority.Add(sPriority);
        }
        ListSkillPriority.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    private void CalculateBoosterUnitStat()
    {
        if (TeamNum == 0)
        {
            string indexName;
            switch (UnitSetting.unitClass)
            {
                case UnitClass.Warrior:
                    indexName = "skill_passive_005";
                    break;
                case UnitClass.Arch:
                    indexName = "skill_passive_006";
                    break;
                case UnitClass.Tank:
                    indexName = "skill_passive_007";
                    break;
                case UnitClass.Supporter:
                    indexName = "skill_passive_008";
                    break;
                default:
                    indexName = string.Empty;
                    break;
            }

            if (MgrBoosterSystem.Instance.DicEtc.TryGetValue(indexName, out int _value))
            {
                if(!indexName.Equals("skill_passive_008"))
                    UnitStat.Atk *= 1.0f + (float)DataManager.Instance.GetBoosterSkillData($"{indexName}_{_value - 1}").Params[0];

                UnitStat.HP *= 1.0f + (float)DataManager.Instance.GetBoosterSkillData($"{indexName}_{_value - 1}").Params[0];
                UnitStat.MaxHP = UnitStat.HP;
            }
        }
    }

    public void InitSpineEvent()
    {
        Ska.AnimationState.Complete -= OnComplete;
        Ska.AnimationState.Complete += OnComplete;
    }

    public void RemoveSpineEvent()
    {
        Ska.AnimationState.Complete -= OnComplete;
    }

    private void InitStateMachine()
    {
        DicState = new Dictionary<UNIT_STATE, IState>();

        IState IStateIdle = new State_Idle();
        IStateIdle.InitializeState(this);

        IState IStateMove = new State_Move();
        IStateMove.InitializeState(this);

        IState IStateSkill = new State_Skill();
        IStateSkill.InitializeState(this);
        
        IState IStateDeath = new State_Death();
        IStateDeath.InitializeState(this);

        DicState.Add(UNIT_STATE.IDLE, IStateIdle);
        DicState.Add(UNIT_STATE.MOVE, IStateMove);
        DicState.Add(UNIT_STATE.SKILL, IStateSkill);
        DicState.Add(UNIT_STATE.DEATH, IStateDeath);

        stateMachine = new StateMachine(IStateIdle);
        stateMachine.ChangeState(DicState[UNIT_STATE.IDLE]);
    }

    private void SetInitSpawnRotation()
    {
        switch(UnitSetting.unitType)
        {
            case UnitType.Monster:
            case UnitType.EnemyUnit:
            case UnitType.Elite:
            case UnitType.MidBoss:
            case UnitType.Boss:
                transform.rotation = Quaternion.Euler(0.0f, -180.0f, 0.0f);
                break;
            default:
                transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                break;
        }
        SetRotationHpBar();
    }

    private List<ControlTrack> listControlTrack = new List<ControlTrack>();
    private void BindingPlayableDirector()
    {
        // 각 스킬 스크립터블 오브젝트 내 타임라인 VFX 프리팹들 복제 생성
        for(int skillIndex = 0; skillIndex < soSkillEvent.Length; skillIndex++)
        {
            ArrPlayableAssets[skillIndex] = new PlayableAsset[soSkillEvent[skillIndex].ArrSkillTimeline.Length];
            for (int i = 0; i < soSkillEvent[skillIndex].ArrSkillTimeline.Length; i++)
            {
                ArrPlayableAssets[skillIndex][i] = soSkillEvent[skillIndex].ArrSkillTimeline[i].timelineAsset;
                
                listControlTrack.Clear();

                bool isSpineSetting = false;
                foreach (var output in ArrPlayableAssets[skillIndex][i].outputs)
                {
                    if (output.sourceObject is ControlTrack)
                    {
                        listControlTrack.Add((ControlTrack)output.sourceObject);
                        continue;
                    }

                    // 스파인 바인딩
                    if (!isSpineSetting && output.streamName.Contains("Spine Animation State Track"))
                    {
                        UnitPlayableDirector.SetGenericBinding(output.sourceObject, Ska);
                        isSpineSetting = true;
                    }
                }

                // 본 팔로워 있는지 체크
                BoneFollower follower;
                int childCnt = 0;
                for (int x = 0; x < soSkillEvent[skillIndex].ArrSkillTimeline[i].ArrObjTimelineVFXPrefab.Length; x++)
                {
                    if (soSkillEvent[skillIndex].ArrSkillTimeline[i].ArrObjTimelineVFXPrefab[x].TryGetComponent(out follower))
                    {
                        follower.skeletonRenderer = Ska;
                        childCnt += follower.transform.childCount;
                    }
                    else childCnt++;
                }

                // 타임라인 VFX 복제 및 바인딩
                int currIndex = 0;
                GameObject objTemp;
                for (int x = 0; x < soSkillEvent[skillIndex].ArrSkillTimeline[i].ArrObjTimelineVFXPrefab.Length; x++)
                {
                    if (soSkillEvent[skillIndex].ArrSkillTimeline[i].ArrObjTimelineVFXPrefab[x].TryGetComponent(out follower))
                    {
                        GameObject objFolower = Instantiate(soSkillEvent[skillIndex].ArrSkillTimeline[i].ArrObjTimelineVFXPrefab[x], transform);
                        for (int j = 0; j < objFolower.transform.childCount; j++)
                        {
                            BindingPrefab(listControlTrack[currIndex], objFolower.transform.GetChild(j).gameObject);
                            currIndex++;
                        }
                    }
                    else
                    {
                        objTemp = Instantiate(soSkillEvent[skillIndex].ArrSkillTimeline[i].ArrObjTimelineVFXPrefab[x], transform);
                        BindingPrefab(listControlTrack[currIndex], objTemp);
                        objTemp.SetActive(false);
                        currIndex++;
                    }
                }
            }
        }

        UnitPlayableDirector.RebindPlayableGraphOutputs();
    }

    private void BindingPrefab(ControlTrack _track, GameObject _obj)
    {
        foreach (TimelineClip clip in _track.GetClips())
        {
            ControlPlayableAsset playableAsset = (ControlPlayableAsset)clip.asset;
            UnitPlayableDirector.SetReferenceValue(playableAsset.sourceGameObject.exposedName, _obj);
        }
    }

    private void InitUnitInGameEvent()
    {
        MgrInGameEvent.Instance.AddDamageEvent(this);
        MgrInGameEvent.Instance.AddActiveSkillEvent(OnActiveSkillAction);
        MgrInGameEvent.Instance.AddBoosterEvent(OnUpgradeBooster);
        MgrBattleSystem.Instance.AddActionShowHP(ActionToggleHPBar);
        if(MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp) ActionToggleHPBar(true);
        else ActionToggleHPBar(MgrBattleSystem.Instance.IsShowHPBar);
    }

    private void InitUnitEvent()
    {
        OnDeathAct = null;
        OnKillAct = null;
        OnTakeDamagedAct = null;
        OnGiveDamagedAct = null;
        OnMicoZone = null;
        OnFinalDamageAttackerFunc = null;
        OnFinalDamageClientFunc = null;

        for (int i = ListEffectPersonalVariable.Count - 1; i >= 0; i--)
            ListEffectPersonalVariable[i].OnEnd();
    }
    #endregion

    /// <summary>
    /// HP 슬라이더 바 회전 체크 및 적용
    /// </summary>
    public void SetRotationHpBar()
    {
        sprrdHPBarBack.transform.localRotation = Quaternion.Euler(0.0f, GetUnitLookDirection() == Vector3.left ? -180.0f : 0.0f, 0.0f);
        sprrdHPBarBack.transform.localPosition = new Vector3(sprrdHPBarBack.transform.localPosition.x, sprrdHPBarBack.transform.localPosition.y, GetUnitLookDirection() == Vector3.left ? 0.001f : -0.001f);
    }

    /// <summary>
    /// HP 슬라이더 바 토글 (이벤트)
    /// </summary>
    /// <param name="_isToggle"></param>
    private void ActionToggleHPBar(bool _isToggle)
    {
        switch (UnitSetting.unitType)
        {
            case UnitType.AllyBase:
                if(TeamNum != 0)
                    ToggleHPBar(_isToggle);
                break;
            case UnitType.Boss:
                if(UnitBaseParent is not null)
                    ToggleHPBar(_isToggle);
                break;
            case UnitType.MidBoss:
                if(MgrBattleSystem.Instance.currWave == MgrBattleSystem.Instance.totalWave || UnitBaseParent is not null)
                    ToggleHPBar(_isToggle);
                break;
            default:
                ToggleHPBar(_isToggle);
                break;
        }
    }

    private void ToggleHPBar(bool _isToggle)
    {
        SetHpShieldBar();

        Color color = Color.white;
        if (!_isToggle || CheckIsState(UNIT_STATE.DEATH))
            color.a = 0.0f;

        SetRotationHpBar();

        sprrdHPBarBack.color = color;
        sprrdHP.color = color;
        sprrdHPFrame.color = color;
        sprrdHPOutline.color = color;
        sprrdShield.color = color;
        sprrdYellow.color = color;
        sprrdYellow.gameObject.SetActive((CheckHasUnitEffect(UNIT_EFFECT.ETC_GOD) || (CheckHasUnitEffect(UNIT_EFFECT.BUFF_IMMORTALITY) && UnitStat.HP - (UnitStat.MaxHP * GetUnitEffectHighestValue(UNIT_EFFECT.BUFF_IMMORTALITY)) < 1.0f)));

        if (seqHPBar != null && seqHPBar.IsActive())
            seqHPBar.Kill();
    }
    
    /// <summary>
    /// 유닛 Update (프레임) 함수
    /// </summary>
    private void Update()
    {
        stateMachine.OperateUpdate();
        OnEffectUpdate?.Invoke();
        UpdateSkillCoolDown(Time.deltaTime);

        if (elitePassiveCooldown > 0.0f) elitePassiveCooldown -= Time.deltaTime;
        if (moveLimitTime > 0.0f) moveLimitTime -= Time.deltaTime;
    }
    
    /// <summary>
    /// 유닛 사망 애니메이션 완료 이후 처리 함수
    /// </summary>
    public void OnAfterDeath()
    {
        // 애니메이션 세팅 초기화
        animIdleName = "idle";
        animMoveName = "walk";
        animDeathName = "death";
        
        token_GearHead_0002?.Cancel();

        // 각종 특수효과 제거
        for (int i = ListEffectPersonalVariable.Count - 1; i >= 0; i--)
            ListEffectPersonalVariable[i].OnEnd();

        if (UnitSetting.unitType == UnitType.AllyBase)
        {
            UserGear gearCore = DataManager.Instance.GetUsingGearInfo(1);
            if (gearCore is not null && gearCore.gearId.Equals("gear_core_0000") && gearCore.gearRarity >= 1 && !MgrBattleSystem.Instance.GlobalOption.isGearCoreActive_000)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Core_0000_C", 1.0f);

                MgrBattleSystem.Instance.GlobalOption.isGearCoreActive_000 = true;
                UnitStat.HP = UnitStat.MaxHP * (float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 0);
                MgrBattleSystem.Instance.SetAllyHPBar(this, UnitStat.HP, UnitStat.MaxHP, Shield);
                SetUnitState(UNIT_STATE.IDLE, true);

                MgrObjectPool.Instance.ShowObj("FX_Resurrection", GetUnitCenterPos());
                if(gearCore.gearRarity >= 3)
                    MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", transform.position).transform.SetParent(transform);

                MgrSound.Instance.PlayOneShotSFX("SFX_Buff_ab", 1.0f);

                AddUnitEffect(UNIT_EFFECT.ETC_GOD, this, this, new float[] { 2.0f + (gearCore.gearRarity >= 10 ? (float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 4) : 0.0f) });

                if (MgrBattleSystem.Instance.WeaponSys.SOWeaponData is not null)
                {
                    MgrBattleSystem.Instance.WeaponSys.SetWeaponAnimation("idle", true);
                    MgrBattleSystem.Instance.WeaponSys.ReSettingEvent();
                }
            }
            else MgrBattleSystem.Instance.SetEndBattle();
        }
        else
        {
            if(UnitSetting.unitType == UnitType.Unit)
            {
                UserGear gearCore = DataManager.Instance.GetUsingGearInfo(1);
                if (gearCore is not null && gearCore.gearId.Equals("gear_core_0001") && gearCore.gearRarity >= 1)
                    MgrBattleSystem.Instance.GlobalOption.Add_GearCore_0001_AllyDeath();

                MgrBattleSystem.Instance.RefreshUnitSpawnCnt(UnitIndex);
            }
            if (MgrBattleSystem.Instance.currWave < MgrBattleSystem.Instance.totalWave && ((UnitSetting.unitType == UnitType.MidBoss || UnitSetting.unitType == UnitType.Boss) && UnitBaseParent is null))
                MgrBattleSystem.Instance.SwitchBossHPBarToBoosterGauge();

            OnVisibleInCamera();
            MgrBattleSystem.Instance.RemoveUnitBase(this);
            MgrInGameEvent.Instance.RemoveDamageEvent(this);
            MgrInGameEvent.Instance.RemoveActiveSkillEvent(OnActiveSkillAction);
            MgrInGameEvent.Instance.RemoveBoosterEvent(OnUpgradeBooster);
            MgrUnitPool.Instance.HideObj(gameObject.name, gameObject);
        }
    }

    // 쿨타임 계산 함수
    // TODO : 아래 변수 2개는 디버깅 용으로 쓰지 않을 시 이후 삭제 처리 필요
    [SerializeField] private Transform tfTempCoolDown;
    [SerializeField] private TextMeshPro[] tmpTempCoolDown = new TextMeshPro[3];
    private void UpdateSkillCoolDown(float _deltaTime)
    {
        int cooldownCnt = 0;
        for (int i = 0; i < Skill_CoolDown.Length; i++)
        {
            if (Skill_CoolDown[i] > 0.0f)
            {
                GameObject objCooldown = tmpTempCoolDown[i].transform.parent.gameObject;

                Skill_CoolDown[i] -= _deltaTime;
                tmpTempCoolDown[i].text = $"{Skill_CoolDown[i]:F1}";

                if (Skill_CoolDown[i] < 0.0f)
                {
                    Skill_CoolDown[i] = 0.0f;
                    objCooldown.SetActive(false);
                }
                else
                {
                    if (!objCooldown.activeSelf)
                        objCooldown.SetActive(true);

                    cooldownCnt++;
                }
            }
        }
        
        // TODO : 디버깅용, 라이브 빌드 시에는 주석 처리 또는 삭제
        int activeCnt = 0;
        for(int i = 0; i < tmpTempCoolDown.Length; i++)
        {
            GameObject objCooldown = tmpTempCoolDown[i].transform.parent.gameObject;
            if (objCooldown.activeSelf)
            {
                activeCnt++;

                if (cooldownCnt == 1) objCooldown.transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);
                if (cooldownCnt == 2)
                {
                    if(activeCnt == 1) objCooldown.transform.localPosition = new Vector3(-0.625f, 0.5f, 0.0f);
                    if(activeCnt == 2) objCooldown.transform.localPosition = new Vector3(0.625f, 0.5f, 0.0f);
                }
                if (cooldownCnt == 3)
                {
                    if(activeCnt == 1) objCooldown.transform.localPosition = new Vector3(-1.25f, 0.5f, 0.0f);
                    if(activeCnt == 2) objCooldown.transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);
                    if(activeCnt == 3) objCooldown.transform.localPosition = new Vector3(1.25f, 0.5f, 0.0f);
                }
            }
        }

        tfTempCoolDown.rotation = Quaternion.identity;
    }
    
    // 타임라인 커스텀 마커 받아와 해당 타이밍에 특정 함수 발동 로직
    private readonly PropertyName propertyAttack = new PropertyName("Attack");
    private readonly PropertyName propertySound = new PropertyName("Sound");
    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification.id.Equals(propertyAttack) && CurrUnitSkill is not null)
            UnitSkillPersonalVariable[GetSkillIndex(CurrUnitSkill)].EventTriggerSkill();

        if(notification.id.Equals(propertySound))
        {
            Marker_Sound sound = notification as Marker_Sound;
            if (sound != null) MgrSound.Instance.PlayOneShotSFX(sound.SoundName, sound.SoundVolume);
        }
    }

    // 애니메이션 재생이 완료되었을 때 발동 함수
    private void OnComplete(TrackEntry trackEntry)
    {
        string animationName = trackEntry.Animation.Name;

        if(animationName.Contains(animDeathName))
        {
            // 사망 애니메이션이 완료됐을 경우 사망 이후 처리 함수 발동
            OnAfterDeath();
        }
        else
        {
            if(animationName.Equals("summon")) // 소환 애니메이션 완료 시 상태를 idle로 변경 (소환 중에는 Death 판정)
                SetUnitState(UNIT_STATE.IDLE, true);

            if (CurrUnitSkill) // 사용 중인 스킬이 있을 경우 함수 발동
                UnitSkillPersonalVariable[GetSkillIndex(CurrUnitSkill)].EventTriggerEnd(animationName);
        }
    }

    /// <summary>
    /// 엑티브 스킬 발동 효과 함수 (이벤트)
    /// </summary>
    /// <param name="_index">발동한 엑티브 스킬 인덱스</param>
    private void OnActiveSkillAction(string _index)
    {
        switch(_index)
        {
            case "skill_active_006":
                if (UnitSetting.unitType != UnitType.Unit || UnitBaseParent is not null || CheckIsState(UNIT_STATE.DEATH))
                    return;

                OnRecallUnit();
                break;
            default:
                break;
        }
    }
    
    /// <summary>
    /// 부스터 업그레이드 효과 적용 함수 (이벤트)
    /// </summary>
    /// <param name="_index">적용할 부스터 인덱스</param>
    private void OnUpgradeBooster(string _index)
    {
        if (UnitSetting.unitType != UnitType.Unit)
            return;

        switch (_index)
        {
            case "skill_passive_005":
                if (UnitSetting.unitClass != UnitClass.Warrior || CheckIsState(UNIT_STATE.DEATH))
                    return;
                break;
            case "skill_passive_006":
                if (UnitSetting.unitClass != UnitClass.Arch || CheckIsState(UNIT_STATE.DEATH))
                    return;
                break;
            case "skill_passive_007":
                if (UnitSetting.unitClass != UnitClass.Tank || CheckIsState(UNIT_STATE.DEATH))
                    return;
                break;
            case "skill_passive_008":
                if (UnitSetting.unitClass != UnitClass.Supporter || CheckIsState(UNIT_STATE.DEATH))
                    return;
                break;
            default:
                return;
        }

        UnitStat.HP += DefaultMaxHP * 0.1f;
        UnitStat.MaxHP = DefaultMaxHP * (1.0f + (float)DataManager.Instance.GetBoosterSkillData($"{_index}_{MgrBoosterSystem.Instance.DicEtc[_index] - 1}").Params[0]);

        if(!_index.Equals("skill_passive_008"))
            UnitStat.Atk = DefaultAtk * (1.0f + (float)DataManager.Instance.GetBoosterSkillData($"{_index}_{MgrBoosterSystem.Instance.DicEtc[_index] - 1}").Params[0]);
    }

    #region Get/Set/Check 함수
    public void SetUnitState(UNIT_STATE _state, bool _isForceChange = false, bool _isChangeOnly = false)
    {
        if (CheckIsState(UNIT_STATE.DEATH) && !_isForceChange)
            return;

        UnitState = _state;
        stateMachine.ChangeState(DicState[_state], _isChangeOnly);
    }

    public bool CheckIsState(UNIT_STATE _state) => UnitState == _state;
    public void SetUnitAnimation(string _name, bool _isLoop = false) => Ska.AnimationState.SetAnimation(0, _name, _isLoop);
    public bool CheckIsEnemyInRange() => MathLib.CheckIsInEllipse(transform.position, UnitStat.Range, EnemyTarget.transform.position, 0.5f);
    public bool CheckIsAllyInXDistance()
    {
        if (EnemyTarget == this)
            return false;

        float xDist = transform.position.x - EnemyTarget.transform.position.x;
        xDist = xDist < 0.0f ? -xDist : xDist;
        return xDist <= UnitStat.Range;
    }
    public void SetUnitUseSkill(SOBase_UnitSkillEvent _event)
    {
        CurrUnitSkill = _event;
        SetUnitState(UNIT_STATE.SKILL);
        UnitSkillPersonalVariable[GetSkillIndex(CurrUnitSkill)].OnSkill();

        if((UnitSetting.unitType == UnitType.Elite || ((UnitSetting.unitType == UnitType.MidBoss || UnitSetting.unitType == UnitType.Boss) && MgrBattleSystem.Instance.IsBossAngry)) && GetSkillIndex(CurrUnitSkill) == 1 && elitePassiveCooldown <= 0.0f)
        {
            elitePassiveCooldown = 10.0f;

            listUnitTemp.Clear();
            listUnitTemp.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(this));
            listUnitTemp.Remove(MgrBattleSystem.Instance.GetAllyBase());

            listUnitTemp.Shuffle();

            foreach (UnitBase unit in listUnitTemp)
            {
                if (unit.CheckHasUnitEffect(UNIT_EFFECT.DEBUFF_BLACK_FIRE))
                    continue;

                GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_Elite", GetUnitCenterPos());
                objBullet.GetComponent<Bullet>().SetBullet(this, unit);
                break;
            }
        }

        // 출혈
        if(CheckHasUnitEffect(UNIT_EFFECT.CC_BLEEDING))
        {
            for(int i = ListEffectPersonalVariable.Count - 1; i >= 0; i--)
            {
                if (MgrInGameData.Instance.GetUnitEffectData(ListEffectPersonalVariable[i].effectEvent).Index == UNIT_EFFECT.CC_BLEEDING)
                    MgrInGameEvent.Instance.BroadcastDamageEvent(ListEffectPersonalVariable[i].Caster, ListEffectPersonalVariable[i].Victim, ListEffectPersonalVariable[i].Victim.UnitStat.MaxHP * 0.03f, 0.0f, 1.0f, 10);
            }
        }
    }
    public void ChangeForceToIdle()
    {
        if (!(CurrUnitSkill is null))
        {
            int skillIndex = GetSkillIndex(CurrUnitSkill);
            SetUnitSkillCoolDown(skillIndex, UnitSetting.unitType == UnitType.Monster ? 1.0f : UnitSkillPersonalVariable[skillIndex].skillCooldown);

            UnitSkillPersonalVariable[skillIndex].ResetSkill();

            UnitPlayableDirector.Stop();
            UnitPlayableDirector.playableAsset = null;
        }

        SetUnitState(UNIT_STATE.IDLE);
    }
    public void ResetUnitSkill() => CurrUnitSkill = null;
    public void SetUnitSkillCoolDown(int _index, float _value, bool _isForcedSet = false)
    {
        float cooldownMultifly = 1.0f;

        if(!_isForcedSet)
        {
            if (UnitSetting.unitType == UnitType.Unit && UnitSetting.unitClass == UnitClass.Supporter)
            {
                if (MgrBoosterSystem.Instance.DicEtc.TryGetValue("skill_passive_008", out int _level))
                    cooldownMultifly -= (float)DataManager.Instance.GetBoosterSkillData($"skill_passive_008_{_level - 1}").Params[1];

                UserGear gearNeck = DataManager.Instance.GetUsingGearInfo(4);
                if (!(gearNeck is null) && gearNeck.gearId.Equals("gear_necklace_0004") && gearNeck.gearRarity >= 3)
                {
                    int unitCnt = MgrBattleSystem.Instance.GetEnemyUnitListInClass(this, UnitClass.Supporter, true).Count;
                    unitCnt = unitCnt > 5 ? 5 : unitCnt;
                    cooldownMultifly -= (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 2) * unitCnt;
                }
            }
            if (UnitSetting.unitType == UnitType.EnemyUnit && UnitSetting.unitClass == UnitClass.Supporter) cooldownMultifly -= 0.7f;
            if (UnitSetting.unitType == UnitType.Elite) cooldownMultifly -= 0.3f;

            if (UnitSetting.unitType == UnitType.Unit)
            {
                if (MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode || MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp) cooldownMultifly -= 0.5f;
            }
        }

        if (cooldownMultifly < 0.0f)
            cooldownMultifly = 0.0f;

        Skill_CoolDown[_index] = _value * cooldownMultifly;
    }
    public float GetUnitSkillCoolDown(int _index) => Skill_CoolDown[_index];
    public int GetSkillIndex(SOBase_UnitSkillEvent _event)
    {
        for(int i = 0; i < soSkillEvent.Length; i++)
        {
            if (soSkillEvent[i] == _event)
                return i;
        }
        return -1;
    }
    public void SetDeathEvent(System.Action<UnitBase, UnitBase, int> _action)
    {
        OnDeathAct -= _action;
        OnDeathAct += _action;
    }
    public void SetKillEvent(System.Action<UnitBase, int> _action)
    {
        OnKillAct -= _action;
        OnKillAct += _action;
    }
    public void SetTakeDamageEvent(System.Action<UnitBase, UnitBase, int, float> _action)
    {
        OnTakeDamagedAct -= _action;
        OnTakeDamagedAct += _action;
    }
    public void SetGiveDamageEvent(System.Action<UnitBase, UnitBase, int, float> _action)
    {
        OnGiveDamagedAct -= _action;
        OnGiveDamagedAct += _action;
    }
    public void OnKillEvent(UnitBase _victim, int _dmgChannel) => OnKillAct?.Invoke(_victim, _dmgChannel);

    public void AddEffectUpdateEvent(System.Action _action) => OnEffectUpdate += _action;
    public void RemoveEffectUpdateEvent(System.Action _action) => OnEffectUpdate -= _action;
    
    public void AddGaramDamagedEvent(System.Action<UnitBase, UnitBase, float> _action) => OnGaramDamaged += _action;
    public void RemoveGaramDamagedEvent(System.Action<UnitBase, UnitBase, float> _action) => OnGaramDamaged -= _action;
    public bool CheckHasGaramDamagedEvent() => !(OnGaramDamaged is null);

    public void AddMicoZoneEvent(System.Func<UnitBase, UnitBase, int, float, float> _func) => OnMicoZone += _func;
    public void RemoveMicoZoneEvent(System.Func<UnitBase, UnitBase, int, float, float> _func) => OnMicoZone -= _func;
    public bool CheckHasMicoZoneEvent() => !(OnMicoZone is null);

    public void SetFinalDamageAttackerEvent(System.Func<UnitBase, UnitBase, int, float, float> _func)
    {
        OnFinalDamageAttackerFunc -= _func;
        OnFinalDamageAttackerFunc += _func;
    }
    public void SetFinalDamageClientEvent(System.Func<UnitBase, UnitBase, int, float, float> _func)
    {
        OnFinalDamageClientFunc -= _func;
        OnFinalDamageClientFunc += _func;
    }

    public void PlayTimeline(int _timelineIndex = 0, SOBase_UnitSkillEvent _event = null, bool _isLoop = false)
    {
        UnitPlayableDirector.playableAsset = ArrPlayableAssets[GetSkillIndex(_event is null ? CurrUnitSkill : _event)][_timelineIndex];
        UnitPlayableDirector.extrapolationMode = _isLoop ? DirectorWrapMode.Loop : DirectorWrapMode.None;
        UnitPlayableDirector.Play();
        UpdatePlayableDirectorSpeed();
    }
    public void SetHeal(float _value, bool _isActiveSound = true)
    {
        float multiflyHeal = 1.0f;

        if(_isActiveSound)
            MgrSound.Instance.PlayOneShotSFX("SFX_Effect_Hill_ab", 1.0f);

        if (UnitSetting.unitType == UnitType.Unit)
        {
            UserGear gearHead = DataManager.Instance.GetUsingGearInfo(3);
            UserGear gearTail = DataManager.Instance.GetUsingGearInfo(5);

            if(!isGearHead_0001_FirstHeal && gearHead is not null && gearHead.gearId.Equals("gear_head_0001") && gearHead.gearRarity >= 3)
            {
                isGearHead_0001_FirstHeal = true;
                multiflyHeal += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 2);
            }
            if(gearHead is not null && gearHead.gearId.Equals("gear_head_0004"))
            {
                if(gearHead.gearRarity >= 1) multiflyHeal += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 0);
                if(gearHead.gearRarity >= 10) multiflyHeal += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 4);

                if(gearHead.gearRarity >= 3)
                {
                    token_GearHead_0004?.Cancel();
                    token_GearHead_0004?.Dispose();
                    token_GearHead_0004 = new CancellationTokenSource();
                    TaskGearHead_0004().Forget();
                }
            }
            if (gearTail is not null && gearTail.gearId.Equals("gear_tail_0001") && gearTail.gearRarity >= 1)
                multiflyHeal += (float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 0);

            multiflyHeal += MgrBattleSystem.Instance.GlobalOption.Option_UnitHeal;
        }

        float resultHeal = _value * multiflyHeal;

        UnitStat.HP += resultHeal;
        if (UnitStat.HP > UnitStat.MaxHP)
            UnitStat.HP = UnitStat.MaxHP;

        MgrObjectPool.Instance.ShowObj("tmpDmg", GetUnitCenterPos() + Vector3.down * 0.25f).GetComponent<DamageText>().SetDamageText(resultHeal, _isHeal: true);

        DoHPBarEffect();
    }

    public float GetAtk()
    {
        float bossAngryValue = (MgrBattleSystem.Instance.IsBossAngry && (UnitSetting.unitType == UnitType.MidBoss || UnitSetting.unitType == UnitType.Boss)) ? 1.0f : 0.0f;

        if(MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode && ((UnitSetting.unitType == UnitType.MidBoss || UnitSetting.unitType == UnitType.Boss)))
        {
            if (MgrBattleSystem.Instance.IsBossAngry)
            {
                bossAngryValue += MgrBattleSystem.Instance.GoldModeAngryLevel * 0.2f;
                bossAngryValue -= 1.0f;
            }
            else bossAngryValue += MgrBattleSystem.Instance.GoldModeAngryLevel * 0.1f;
        }

        return UnitStat.Atk * (1.0f + bossAngryValue + MgrBattleSystem.Instance.GlobalOption.GetWorldAtkBonus(this)) * (1.0f + GetUnitEffectTotalValue(UNIT_EFFECT.BUFF_ATK) - GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_WEAK));
    }
    public float GetCriRate() => UnitStat.CriRate + GetUnitEffectTotalValue(UNIT_EFFECT.BUFF_CRI_RATE) - GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_WEAK);
    public float GetAtkRateToDamage(float _atkRate) => GetAtk() * _atkRate;

    public float GetUnitSkillFloatDataValue(int _index, string _param)
    {
        string[] splitString = _param.Split(".");
        switch(UnitSetting.unitType)
        {
            case UnitType.MidBoss:
            case UnitType.Boss:
                return (float)UnitSkillPersonalVariable[_index].bossSkillData.Param[int.Parse(splitString[1])];
            default:
                return (float)UnitSkillPersonalVariable[_index].unitSkillData.Param[int.Parse(splitString[1])];
        }
    }
    public int GetUnitSkillIntDataValue(int _index, string _param)
    {
        string[] splitString = _param.Split(".");
        switch (UnitSetting.unitType)
        {
            case UnitType.MidBoss:
            case UnitType.Boss:
                return (int)UnitSkillPersonalVariable[_index].bossSkillData.Param[int.Parse(splitString[1])];
            default:
                return (int)UnitSkillPersonalVariable[_index].unitSkillData.Param[int.Parse(splitString[1])];
        }
    }

    public float GetUnitHeight()
    {
        //switch(UnitSetting.unitIndex)
        float resultHeight;
        switch(UnitIndex)
        {
            case "C_Tank_01": resultHeight = 2.8f; break;
            case "A_Arch_01": resultHeight = 2.2f; break;
            case "S_War_03": resultHeight = 2.3f; break;
            case "S_Arch_01": resultHeight = 2.2f; break;
            case "S_Arch_02": resultHeight = 3.0f; break;
            case "C2_Final_Boss01_a":
            case "C2_Final_Boss01_b":
            case "C2_Final_Boss01_c":
            case "C2_Final_Boss01_d":
                resultHeight = (Ska.skeleton.Data.Height * 0.01f) - 3.0f;
                break;
            case "C3_Final_Boss02_a":
            case "C3_Final_Boss02_b":
            case "C3_Final_Boss02_c":
            case "C3_Final_Boss02_d":
                resultHeight = (Ska.skeleton.Data.Height * 0.01f) - 1.0f;
                break;
            case "C4_Final_Boss03_a":
            case "C4_Final_Boss03_b":
            case "C4_Final_Boss03_c":
            case "C4_Final_Boss03_d":
                resultHeight = (Ska.skeleton.Data.Height * 0.01f) - 3.0f;
                break;
            default:
                resultHeight = (Ska.skeleton.Data.Height * 0.01f) - 0.05f;
                break;
        }
        return resultHeight * Ska.transform.localScale.y;
    }
    public Vector3 GetUnitCenterPos() => new Vector3(transform.position.x, (transform.position.y + (GetUnitHeight() * 0.5f) - 0.2f) * transform.localScale.y, transform.position.z);
    public Vector3 GetUnitLookDirection(bool _isReverse = false) => transform.rotation.y == -1.0f ? (_isReverse ? Vector3.right : Vector3.left) : (_isReverse ? Vector3.left : Vector3.right);
    #endregion

    #region 특수 효과 관련 함수
    public void AddUnitEffect(UNIT_EFFECT _index, UnitBase _caster, UnitBase _victim, float[] _value, bool _canRemove = true)
    {
        // 유닛 타입에 따른 효과 면역
        switch (_victim.UnitSetting.unitType)
        {
            case UnitType.AllyBase: // 기지는 자기 자신이 건 게 아니라면 모든 것이 면역
                if(_caster != _victim)
                {
                    MgrObjectPool.Instance.ShowObj("tmpDmg", GetUnitCenterPos() + Vector3.up * 0.5f).GetComponent<DamageText>().SetDamageText(0.0f, _immunity: true, _unitEffectIndex: (int)_index);
                    return;
                }
                else
                {
                    if(_index != UNIT_EFFECT.ETC_GOD && _index != UNIT_EFFECT.BUFF_SHIELD)
                    {
                        MgrObjectPool.Instance.ShowObj("tmpDmg", GetUnitCenterPos() + Vector3.up * 0.5f).GetComponent<DamageText>().SetDamageText(0.0f, _immunity: true, _unitEffectIndex: (int)_index);
                        return;
                    }
                }
                break;
            case UnitType.Boss:
            case UnitType.MidBoss: // 보스와 중간보스는 슈퍼 넉백을 제외한 상태 이상 면역
                if ((int)_index >= 9300 && (int)_index < 9400 && _index != UNIT_EFFECT.CC_SUPER_KONCKBACK)
                {
                    MgrObjectPool.Instance.ShowObj("tmpDmg", GetUnitCenterPos() + Vector3.up * 0.5f).GetComponent<DamageText>().SetDamageText(0.0f, _immunity: true, _unitEffectIndex: (int)_index);
                    return;
                }
                break;
        }

        //if ((int)_index >= 9300 && (int)_index < 9400 && _victim.CheckHasUnitEffect(UNIT_EFFECT.ETC_GOD))
        //{
        //    MgrObjectPool.Instance.ShowObj("tmpDmg", GetUnitCenterPos() + Vector3.up * 0.5f).GetComponent<DamageText>().SetDamageText(0.0f, _immunity: true, _immunityIndex: (int)_index);
        //    return;
        //}

        if (_victim.CheckIsState(UNIT_STATE.DEATH))
            return;

        UnitEffectData.UnitEffectSetting cc = MgrInGameData.Instance.GetEffectData(_index);

        // 이동 제어 상태 이상인데 만약 이미 걸려있다면 제외
        if (cc.IsMovedEffect && CheckHasBlockedMoveCC() && cc.Index != UNIT_EFFECT.CC_SUPER_KONCKBACK)
            return;

        // 저항 체크
        if (CheckIsResistanceUnitEffect(_index, _victim))
        {
            MgrObjectPool.Instance.ShowObj("tmpDmg", GetUnitCenterPos() + Vector3.up).GetComponent<DamageText>().SetDamageText(0.0f, _isUnitEffectResistance: true, _unitEffectIndex: (int)_index);
            return;
        }

        if(cc.Index == UNIT_EFFECT.BUFF_SHIELD)
        {
            if(CheckHasUnitEffect(UNIT_EFFECT.BUFF_SHIELD))
            {
                List<UnitEffectPersonalVariableInstance> listPersonal = DicCCStatus.Values.ToList();

                bool isRemoved = false;
                for (int i = listPersonal.Count - 1; i >= 0; i--)
                {
                    if (listPersonal[i].Index != UNIT_EFFECT.BUFF_SHIELD || !listPersonal[i].CanRemove)
                        continue;

                    isRemoved = true;
                    listPersonal[i].OnEnd();
                }
                if (!isRemoved) // 쉴드 제거하지 못 했다면 진행 종료
                    return;
            }
        }
        else
        {
            // 동일 인덱스 유닛이 동일한 피격 대상에게 동일한 CC 넣을 시 해당 CC 삭제 시키며 재갱신
            if (DicCCStatus.TryGetValue(new UnitEffect_Status(_caster is null ? "World" : _caster.UnitIndex, _victim, _index), out UnitEffectPersonalVariableInstance _out))
            {
                if(_index != UNIT_EFFECT.DEBUFF_FROSTBITE)
                    _out.OnEnd();
            }
            else
            {
                if(9100 <= (int)_index && (int)_index < 9200 && _canRemove && _index != UNIT_EFFECT.BUFF_IMMORTALITY)
                {
                    if (MgrInGameData.Instance.DicLocalizationCSVData.ContainsKey($"Effect_buff_{(int)_index}")) MgrObjectPool.Instance.ShowObj("tmpDmg", transform.position).GetComponent<DamageText>().SetDamageText(0.0f, _isUnitEffect: true, _unitEffectIndex: (int)_index);
                    else Debug.LogError($"[ERROR] - 버프 로컬라이징 누락 > {_index}");
                }
            }
        }

        if (UnitSetting.unitType == UnitType.Elite || MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp)
            eliteResistUnitEffectCnt++;

        cc.soUnitEffectEvent.OnInitialize(_caster, _victim, _index, _value, _canRemove);
    }

    public void RemoveUnitEffect(UNIT_EFFECT _index, UnitBase _caster = null, bool _isForceRemove = false, bool _isForceWorldRemove = false)
    {
        for (int i = ListEffectPersonalVariable.Count - 1; i >= 0; i--)
        {
            if (MgrInGameData.Instance.GetUnitEffectData(ListEffectPersonalVariable[i].effectEvent).Index != _index || (!_isForceRemove && !ListEffectPersonalVariable[i].CanRemove))
                continue;

            // 시전자와 체크할 시전자가 같은 다르면 제외
            if ((_caster is null && ListEffectPersonalVariable[i].Caster is null && !_isForceWorldRemove) || (_caster is not null && ListEffectPersonalVariable[i].Caster != _caster))
                continue;

            ListEffectPersonalVariable[i].OnEnd();
        }
    }

    public void RemoveDebuffUnitEffect()
    {
        for (int i = ListEffectPersonalVariable.Count - 1; i >= 0; i--)
        {
            if (UNIT_EFFECT.DEBUFF_WEAK <= MgrInGameData.Instance.GetUnitEffectData(ListEffectPersonalVariable[i].effectEvent).Index && MgrInGameData.Instance.GetUnitEffectData(ListEffectPersonalVariable[i].effectEvent).Index < UNIT_EFFECT.CC_STUN)
                ListEffectPersonalVariable[i].OnEnd(true);
        }
    }

    public float GetUnitEffectTotalValue(UNIT_EFFECT _index)
    {
        float resultValue = 0.0f;

        foreach (UnitEffectPersonalVariableInstance effect in ListEffectPersonalVariable)
        {
            if (MgrInGameData.Instance.GetUnitEffectData(effect.effectEvent).Index == _index)
                resultValue += effect.GetUnitEffectValue();
        }

        return resultValue;
    }
    
    public float GetUnitEffectHighestValue(UNIT_EFFECT _index)
    {
        float resultValue = 0.0f;

        foreach (UnitEffectPersonalVariableInstance effect in ListEffectPersonalVariable)
        {
            if (MgrInGameData.Instance.GetUnitEffectData(effect.effectEvent).Index == _index)
            {
                if(effect.GetUnitEffectValue() > resultValue)
                    resultValue = effect.GetUnitEffectValue();
            }
        }

        return resultValue;
    }

    public bool CheckHasBlockedMoveSkillCC()
    {
        foreach (UnitEffectPersonalVariableInstance effect in ListEffectPersonalVariable)
        {
            UnitEffectData.UnitEffectSetting effectData = MgrInGameData.Instance.GetUnitEffectData(effect.effectEvent);
            if (effectData.IsMovedEffect && effectData.IsBlockedSkillEffect)
                return true;
        }
        return false;
    }
    
    public bool CheckHasBlockedMoveCC()
    {
        foreach (UnitEffectPersonalVariableInstance effect in ListEffectPersonalVariable)
        {
            if (MgrInGameData.Instance.GetUnitEffectData(effect.effectEvent).IsMovedEffect)
                return true;
        }
        return false;
    }

    public bool CheckHasBlockedSkillCC()
    {
        foreach (UnitEffectPersonalVariableInstance effect in ListEffectPersonalVariable)
        {
            if (MgrInGameData.Instance.GetUnitEffectData(effect.effectEvent).IsBlockedSkillEffect)
                return true;
        }
        return false;
    }

    public bool CheckHasUnitEffect(UNIT_EFFECT _index)
    {
        foreach (UnitEffectPersonalVariableInstance effect in ListEffectPersonalVariable)
        {
            if (MgrInGameData.Instance.GetUnitEffectData(effect.effectEvent).Index == _index)
                return true;
        }
        return false;
    }

    public bool CheckHasDebuffUnitEffect()
    {
        foreach (UnitEffectPersonalVariableInstance effect in ListEffectPersonalVariable)
        {
            if (UNIT_EFFECT.DEBUFF_WEAK <= MgrInGameData.Instance.GetUnitEffectData(effect.effectEvent).Index && MgrInGameData.Instance.GetUnitEffectData(effect.effectEvent).Index < UNIT_EFFECT.CC_STUN)
                return true;
        }
        return false;
    }
    
    public bool CheckHasCCUnitEffect()
    {
        foreach (UnitEffectPersonalVariableInstance effect in ListEffectPersonalVariable)
        {
            if (UNIT_EFFECT.CC_STUN <= MgrInGameData.Instance.GetUnitEffectData(effect.effectEvent).Index && MgrInGameData.Instance.GetUnitEffectData(effect.effectEvent).Index < UNIT_EFFECT.ETC_GOD)
                return true;
        }
        return false;
    }

    private bool CheckIsResistanceUnitEffect(UNIT_EFFECT _index, UnitBase _victim)
    {
        float resistance = 0.0f;

        UserGear gearHead = DataManager.Instance.GetUsingGearInfo(3);
        if(_victim.UnitSetting.unitType == UnitType.Unit && !(gearHead is null) && gearHead.gearId.Equals("gear_head_0003"))
        {
            if (9200 <= (int)_index && (int)_index < 9300)
            {
                if (gearHead.gearRarity >= 1) resistance += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 0);
                if (gearHead.gearRarity >= 10) resistance += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 4);
            }
            if (9300 <= (int)_index && (int)_index < 9400)
            {
                if (gearHead.gearRarity >= 3) resistance += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 0);
                if (gearHead.gearRarity >= 10) resistance += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 4);
            }
        }

        if (_victim.UnitSetting.unitType == UnitType.Elite && 9300 <= (int)_index && (int)_index < 9400 && _index != UNIT_EFFECT.CC_SUPER_KONCKBACK)
        {
            resistance += 0.8f;
            for (int i = 0; i < eliteResistUnitEffectCnt; i++)
                resistance += (1.0f - 0.8f) * MathLib.Pow(0.5f, i + 1);
        }

        if(MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp && 9300 <= (int)_index && (int)_index < 9400 && _index != UNIT_EFFECT.CC_SUPER_KONCKBACK)
        {
            resistance += 0.5f;
            for (int i = 0; i < eliteResistUnitEffectCnt; i++)
                resistance += (1.0f - 0.5f) * MathLib.Pow(0.5f, i + 1);
        }

        return MathLib.CheckPercentage(resistance);
    }

    public void AddUnitEffectVFX(UNIT_EFFECT _index, string _vfxName, Vector3 _v3Pos, bool _isInitLocalScale = false)
    {
        if (dicUnitEffectVFX.ContainsKey(_index))
            return;

        GameObject objVFX = MgrObjectPool.Instance.ShowObj(_vfxName, _v3Pos);
        objVFX.transform.SetParent(transform);
        if (_isInitLocalScale) objVFX.transform.localScale = Vector3.one;
        dicUnitEffectVFX.Add(_index, objVFX);
    }

    public void RemoveUnitEffectVFX(UNIT_EFFECT _index, string _vfxName)
    {
        if (CheckHasUnitEffect(_index) || !dicUnitEffectVFX.ContainsKey(_index))
            return;

        MgrObjectPool.Instance.HideObj(_vfxName, dicUnitEffectVFX[_index]);
        dicUnitEffectVFX.Remove(_index);
    }

    public void UpdateSpineUnitSpineSpeed()
    {
        float ratio = 1.0f;
        if (CheckIsState(UNIT_STATE.SKILL))
        {
            if (UnitSetting.unitType == UnitType.Unit)
                if (MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode || MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp) ratio *= 1.3f;

            ratio *= 1.0f + GetUnitEffectTotalValue(UNIT_EFFECT.BUFF_ATK_SPEED);
            ratio *= 1.0f - GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_SLOW);
            ratio *= 1.0f - GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_FROSTBITE);
            ratio *= 1.0f - GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_BLACK_FIRE);
        }
        else if (CheckIsState(UNIT_STATE.MOVE))
        {
            ratio *= 1.0f + GetUnitEffectTotalValue(UNIT_EFFECT.BUFF_MOVE_SPEED);
            ratio *= 1.0f - GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_SLOW);
            ratio *= 1.0f - GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_FROSTBITE);
            ratio *= 1.0f - GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_BLACK_FIRE);
        }

        if (CheckHasUnitEffect(UNIT_EFFECT.CC_FREEZE))
            ratio = 0.0f;

        Ska.timeScale = ratio;
        if(UnitPlayableDirector.playableGraph.IsValid())
            UpdatePlayableDirectorSpeed();
    }

    private void UpdatePlayableDirectorSpeed()
    {
        UnitPlayableDirector.playableGraph.GetRootPlayable(0).SetSpeed(Ska.timeScale);
        UnitPlayableDirector.Evaluate();
    }

    public void UpdateSpineTint()
    {
        if (CheckHasUnitEffect(UNIT_EFFECT.DEBUFF_FROSTBITE) || CheckHasUnitEffect(UNIT_EFFECT.CC_FREEZE)) Meshrd.SetPropertyBlock(MgrInGameData.Instance.MpbFrostBite);
        else if (CheckHasUnitEffect(UNIT_EFFECT.DEBUFF_SLOW)) Meshrd.SetPropertyBlock(MgrInGameData.Instance.MpbSlow);
        else if ((CheckHasUnitEffect(UNIT_EFFECT.ETC_GOD) || (CheckHasUnitEffect(UNIT_EFFECT.BUFF_IMMORTALITY) && UnitStat.HP - (UnitStat.MaxHP * GetUnitEffectHighestValue(UNIT_EFFECT.BUFF_IMMORTALITY)) < 1.0f)))
        {
            if(UnitSetting.unitType != UnitType.AllyBase && UnitSetting.unitType != UnitType.MidBoss && UnitSetting.unitType != UnitType.Boss)
            {
                if (!IsImmortalRunText && CheckHasUnitEffect(UNIT_EFFECT.BUFF_IMMORTALITY) && UnitStat.HP - (UnitStat.MaxHP * GetUnitEffectHighestValue(UNIT_EFFECT.BUFF_IMMORTALITY)) < 1.0f)
                {
                    MgrSound.Instance.PlayOneShotSFX("SFX_Effect_Invincibility_ab", 1.0f);
                    MgrObjectPool.Instance.ShowObj("tmpDmg", GetUnitCenterPos()).GetComponent<DamageText>().SetDamageText(0.0f, _isUnitEffect:true, _unitEffectIndex:(int)UNIT_EFFECT.BUFF_IMMORTALITY);
                    IsImmortalRunText = true;
                }
                Meshrd.SetPropertyBlock(MgrInGameData.Instance.MpbGod);
            }
        }
        else Meshrd.SetPropertyBlock(mpbNormal);
    }

    private bool CheckHasMaskSkin()
    {
        if (Ska is null)
            return false;

        foreach(Skin skin in Ska.skeleton.Data.Skins)
        {
            if (skin.Name.Equals("mask"))
                return true;
        }
        return false;
    }
    #endregion

    public void DecreaseHP(UnitBase _attacker, float _amount, int _dmgChannel = 0)
    {
        if (CheckIsState(UNIT_STATE.DEATH))
            return;

        UnitStat.HP -= _amount;

        MgrObjectPool.Instance.ShowObj("tmpDmg", GetUnitCenterPos()).GetComponent<DamageText>().SetDamageText(_amount);

        TaskHitEffect().Forget();

        if (UnitStat.HP <= 0.0f)
            SetUnitDeath(_attacker, this, _dmgChannel);
    }

    public void OnDamage(UnitBase _attacker, UnitBase _victim, float _damage, float _criPer, float _criDmg, int _dmgChannel)
    {
        // 피격자가 아니거나 피격자가 사망한 상태일 경우 return
        if (_victim != this || _victim.CheckIsState(UNIT_STATE.DEATH))
            return;

        // 회피 체크
        if(_victim.CheckIsDodge())
        {
            MgrObjectPool.Instance.ShowObj("tmpDmg", GetUnitCenterPos()).GetComponent<DamageText>().SetDamageText(0.0f, _isDodge: true);
            return;
        }

        // 무적 체크
        if (_victim.CheckHasUnitEffect(UNIT_EFFECT.ETC_GOD))
        {
            MgrObjectPool.Instance.ShowObj("tmpDmg", GetUnitCenterPos()).GetComponent<DamageText>().SetDamageText(0.0f, _isGod: true);
            return;
        }

        // 대미지 계산
        bool isCritical = false;
        float resultDamage = CalculateDamage(_attacker, _victim, _damage, _criPer, _criDmg, _dmgChannel, ref isCritical);
        if (resultDamage < 0.0f) // 대미지가 0보다 작을 경우 return
            return;

        UserGear gearCore = DataManager.Instance.GetUsingGearInfo(1);
        if (_attacker.UnitSetting.unitType == UnitType.AllyBase && (_victim.UnitSetting.unitType == UnitType.Monster || _victim.UnitSetting.unitType == UnitType.EnemyUnit) && gearCore is not null && gearCore.gearId.Equals("gear_core_0003") && gearCore.gearRarity >= 10 && _victim.UnitStat.HP / _victim.UnitStat.MaxHP <= (float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 4))
        {
            _victim.AddUnitEffect(UNIT_EFFECT.ETC_INSTANT_DEATH, _attacker, _victim, null);
            return;
        }

        // 빙결 대미지 누적
        if (CheckHasUnitEffect(UNIT_EFFECT.CC_FREEZE))
            FreezeDmg += resultDamage;

        // 쉴드 계산
        bool isBlockedShield = false;
        if(Shield > 0.0f)
        {
            if(Shield > resultDamage)
            {
                Shield -= resultDamage;
                resultDamage = 0.0f;
                isBlockedShield = true;

                if(UnitSetting.unitType != UnitType.AllyBase && UnitSetting.unitType != UnitType.MidBoss && UnitSetting.unitType != UnitType.Boss)
                {
                    GameObject objShieldVFX = MgrObjectPool.Instance.ShowObj("FX_Buff_Barrier_Hit", GetUnitCenterPos());
                    objShieldVFX.transform.GetChild(0).localRotation = Quaternion.Euler(0.0f, 0.0f, GetUnitLookDirection() == Vector3.left ? -70.0f : -120.0f);
                    objShieldVFX.transform.GetChild(1).localRotation = Quaternion.Euler(0.0f, 0.0f, GetUnitLookDirection() == Vector3.left ? 180.0f : 0.0f);
                    objShieldVFX.transform.SetParent(transform);
                }
            }
            else
            {
                resultDamage -= Shield;
                RemoveUnitEffect(UNIT_EFFECT.BUFF_SHIELD, _isForceRemove: true, _isForceWorldRemove:true);
            }
        }
        UnitStat.HP -= resultDamage;

        MgrObjectPool.Instance.ShowObj("tmpDmg", GetUnitCenterPos()).GetComponent<DamageText>().SetDamageText(resultDamage, isCritical, isBlockedShield);

        if (UnitSetting.unitType == UnitType.AllyBase)
            MgrBattleSystem.Instance.SetAllyHPBar(this, UnitStat.HP, UnitStat.MaxHP, Shield);

        TaskHitEffect().Forget();

        OnTakeDamagedAct?.Invoke(_attacker, _victim, _dmgChannel, resultDamage);
        _attacker.OnGiveDamagedAct?.Invoke(_attacker, _victim, _dmgChannel, resultDamage);

        if(_dmgChannel >= 0)
            OnGaramDamaged?.Invoke(_attacker, _victim, resultDamage);

        if (UnitStat.HP <= 0.0f)
        {
            if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter && UnitSetting.unitType == UnitType.AllyBase)
            {
                UnitStat.HP = 1.0f;
                AddUnitEffect(UNIT_EFFECT.ETC_GOD, this, this, new float[] { 5.0f }, false);
                TaskTutorialHeal().Forget();
                return;
            }

            SetUnitDeath(_attacker, _victim, _dmgChannel);
        }
    }

    private async UniTaskVoid TaskTutorialHeal()
    {
        for(int i = 0; i < 5; i++)
        {
            SetHeal(UnitStat.MaxHP * 0.2f);
            MgrObjectPool.Instance.ShowObj("FX_Buff_Dot Heal", GetUnitCenterPos());
            await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());
        }
    }

    public void SetUnitDeath(UnitBase _attacker, UnitBase _victim, int _dmgChannel)
    {
        if (OnDeathAct is null) OnDefaultDeath(_attacker, _victim, _dmgChannel);
        else OnDeathAct?.Invoke(_attacker, _victim, _dmgChannel);
    }

    public bool CheckIsDodge()
    {
        float dodgePercent = 0.0f;
        dodgePercent += GetUnitEffectTotalValue(UNIT_EFFECT.ETC_DODGE);
        return MathLib.CheckPercentage(dodgePercent);
    }

    private float CalculateDamage(UnitBase _attacker, UnitBase _victim, float _damage, float _criPer, float _criDmg, int _dmgChannel, ref bool _isCritical)
    {
        // 받는, 주는 피해 계산 (주는 피해량 증가 - 받는 피해량 감소)
        float totalGiveTakeDamage = CalculateGiveTakeDamageMultifly(_attacker, _victim);

        // 상성 피해 계산
        float totalElementDamage = CalculateElementMultifly(_attacker, _victim);

        // 크리티컬에 확인
        float criRate = CalculateCriRate(_attacker, _victim, _criPer);
        if (Random.Range(0.001f, 1.0f) <= criRate)
            _isCritical = true;

        float criDmgRate = CalculateCriDmgRate(_attacker, _victim, _criDmg);
        float resultDamage = _damage * totalGiveTakeDamage * totalElementDamage * (_isCritical ? criDmgRate : 1.0f);

        if (CheckHasUnitEffect(UNIT_EFFECT.BUFF_GHOST_ARMOR))
        {
            resultDamage -= GetUnitEffectHighestValue(UNIT_EFFECT.BUFF_GHOST_ARMOR);
            if (resultDamage < 0.0f)
                resultDamage = 0.0f;
        }

        // 불사
        if (CheckHasUnitEffect(UNIT_EFFECT.BUFF_IMMORTALITY))
        {
            float limitHP;
            limitHP = UnitStat.MaxHP * GetUnitEffectHighestValue(UNIT_EFFECT.BUFF_IMMORTALITY);

            if (UnitStat.HP - limitHP < resultDamage)
            {
                resultDamage = UnitStat.HP - limitHP;
                if (resultDamage < 0.0f)
                    resultDamage = 0.0f;

                UpdateSpineTint();
            }
        }

        if (OnFinalDamageClientFunc is not null) resultDamage = OnFinalDamageClientFunc.Invoke(_attacker, _victim, _dmgChannel, resultDamage);
        if (_attacker.OnFinalDamageAttackerFunc is not null) resultDamage = _attacker.OnFinalDamageAttackerFunc.Invoke(_attacker, _victim, _dmgChannel, resultDamage);
        if (OnMicoZone is not null) resultDamage = OnMicoZone.Invoke(_attacker, _victim, _dmgChannel, resultDamage);

        // 테스트 용
        if (MgrBattleSystem.Instance.IsTestMode && !MgrBattleSystem.Instance.IsTestFullDamaged)
            resultDamage = 1.0f;

        return resultDamage;
    }

    private float CalculateGiveTakeDamageMultifly(UnitBase _attacker, UnitBase _victim)
    {
        // 받는, 주는 피해 계산 (주는 피해량 증가 - 받는 피해량 감소)
        float totalGiveTakeDamage = 1.0f + (_attacker.GetUnitEffectTotalValue(UNIT_EFFECT.BUFF_GIVE_DMG) - GetUnitEffectTotalValue(UNIT_EFFECT.BUFF_TAKE_DMG));

        if (CheckHasUnitEffect(UNIT_EFFECT.ETC_REVIVE)) // 부활 상태일 때 받는 피해량 감소
            totalGiveTakeDamage -= 0.8f;

        if (UnitGhostoEffect is not null && _attacker.UnitSetting.unitClass == UnitClass.Arch) // 고스토 원거리 피해 감소
            totalGiveTakeDamage -= (float)UnitGhostoEffect.UnitSkillPersonalVariable[0].unitSkillData.Param[1];

        if (_attacker.UnitSetting.unitType == UnitType.AllyBase && MgrBoosterSystem.Instance.DicEtc.TryGetValue("skill_passive_000", out int _giveLevel)) // 기지 피해량 증가
            totalGiveTakeDamage += (float)DataManager.Instance.GetBoosterSkillData($"skill_passive_000_{_giveLevel - 1}").Params[0];

        if (UnitSetting.unitType == UnitType.AllyBase && MgrBoosterSystem.Instance.DicEtc.TryGetValue("skill_passive_003", out int _takeLevel)) // 기지 받는 피해량 감소
            totalGiveTakeDamage -= (float)DataManager.Instance.GetBoosterSkillData($"skill_passive_003_{_takeLevel - 1}").Params[0];

        UserGear gearCore = DataManager.Instance.GetUsingGearInfo(1);
        if (_attacker.UnitSetting.unitType == UnitType.AllyBase && (_victim.UnitSetting.unitType == UnitType.Monster || _victim.UnitSetting.unitType == UnitType.EnemyUnit) && gearCore is not null && gearCore.gearId.Equals("gear_core_0003") && gearCore.gearRarity >= 3)
            totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 2);

        if(_victim.UnitSetting.unitType == UnitType.Unit)
        {
            UserGear gearHead = DataManager.Instance.GetUsingGearInfo(3);
            if(gearHead is not null)
            {
                if (gearHead.gearId.Equals("gear_head_0000"))
                {
                    if ((_attacker.UnitSetting.unitType == UnitType.Monster || _attacker.UnitSetting.unitType == UnitType.EnemyUnit) && gearHead.gearRarity >= 1) totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 0);
                    if ((_attacker.UnitSetting.unitType == UnitType.Elite || (_attacker.UnitSetting.unitType == UnitType.MidBoss && MgrBattleSystem.Instance.currWave != MgrBattleSystem.Instance.totalWave)) && gearHead.gearRarity >= 3) totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 2);
                    if (((_attacker.UnitSetting.unitType == UnitType.MidBoss && MgrBattleSystem.Instance.currWave == MgrBattleSystem.Instance.totalWave) || _attacker.UnitSetting.unitType == UnitType.Boss) && gearHead.gearRarity >= 10) totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 4);
                }
                if (!isGearHead_0001_FirstTakeDmg && gearHead.gearId.Equals("gear_head_0001") && gearHead.gearRarity >= 1)
                {
                    isGearHead_0001_FirstTakeDmg = true;
                    totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 0);
                }
                if (isGearHead_0002_Activate)
                {
                    if (gearHead.gearRarity >= 1) totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 0);
                    if (gearHead.gearRarity >= 10) totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 4);
                }
                if (isGearHead_0004_Activate)
                    totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 2);
            }
        }

        if(_attacker.UnitSetting.unitType == UnitType.Unit)
        {
            UserGear gearNeck = DataManager.Instance.GetUsingGearInfo(4);
            UserGear gearTail = DataManager.Instance.GetUsingGearInfo(5);

            if(gearNeck is not null && gearNeck.gearId.Equals("gear_necklace_0000"))
            {
                if (gearNeck.gearRarity >= 1 && (_victim.UnitSetting.unitType == UnitType.Monster || _victim.UnitSetting.unitType == UnitType.EnemyUnit)) totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 0);
                if (gearNeck.gearRarity >= 3 && (_victim.UnitSetting.unitType == UnitType.Elite || (_victim.UnitSetting.unitType == UnitType.MidBoss && MgrBattleSystem.Instance.currWave != MgrBattleSystem.Instance.totalWave))) totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 2);
                if (gearNeck.gearRarity >= 10 && ((_victim.UnitSetting.unitType == UnitType.MidBoss && MgrBattleSystem.Instance.currWave == MgrBattleSystem.Instance.totalWave) || _victim.UnitSetting.unitType == UnitType.Boss)) totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 4);
            }
            if(gearTail is not null && gearTail.gearId.Equals("gear_tail_0000"))
            {
                if ((_victim.UnitSetting.unitType == UnitType.MidBoss && MgrBattleSystem.Instance.currWave != MgrBattleSystem.Instance.totalWave) && _victim.UnitStat.HP / _victim.UnitStat.MaxHP >= 0.9f && gearTail.gearRarity >= 1) totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 0);
                if (_victim.UnitSetting.unitType == UnitType.Elite && _victim.UnitStat.HP / _victim.UnitStat.MaxHP >= 0.8f && gearTail.gearRarity >= 3) totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 2);
                if ((_victim.UnitSetting.unitType == UnitType.Monster || _victim.UnitSetting.unitType == UnitType.EnemyUnit) && _victim.UnitStat.HP / _victim.UnitStat.MaxHP >= 0.7f && gearTail.gearRarity >= 10) totalGiveTakeDamage += (float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 4);
            }
        }

        if (_victim.CheckHasUnitEffect(UNIT_EFFECT.CC_WATERBALL)) // 물방울 받는 피해 20% 곱연산
            totalGiveTakeDamage *= 1.2f;

        if (totalGiveTakeDamage < 0.0f)
            totalGiveTakeDamage = 0.0f;

        return totalGiveTakeDamage;
    }
    
    private float CalculateElementMultifly(UnitBase _attacker, UnitBase _victim)
    {
        // 상성 피해 계산
        float totalElementDamage = 1.0f;

        // 유닛 클래스 별 상성 대미지 변화
        bool isElementDamageMultifly = false;
        if ((_attacker.UnitSetting.unitClass == UnitClass.Warrior && UnitSetting.unitClass == UnitClass.Tank) || (_attacker.UnitSetting.unitClass == UnitClass.Arch && UnitSetting.unitClass == UnitClass.Warrior) || (_attacker.UnitSetting.unitClass == UnitClass.Tank && UnitSetting.unitClass == UnitClass.Arch))
        {
            CalculateElementTotal(ref totalElementDamage, 0.2f);
            isElementDamageMultifly = true;
        }
        if ((_attacker.UnitSetting.unitClass == UnitClass.Warrior && UnitSetting.unitClass == UnitClass.Arch) || (_attacker.UnitSetting.unitClass == UnitClass.Arch && UnitSetting.unitClass == UnitClass.Tank) || (_attacker.UnitSetting.unitClass == UnitClass.Tank && UnitSetting.unitClass == UnitClass.Warrior))
        {
            CalculateElementTotal(ref totalElementDamage, -0.2f);
            isElementDamageMultifly = true;
        }

        // 상성 대미지 증감 추가 효과는 상성 대미지일 때만 발동
        if(isElementDamageMultifly)
        {
            // 장비 효과로 인한 상성 대미지 변화
            UserGear gearHead = DataManager.Instance.GetUsingGearInfo(3);
            UserGear gearNeck = DataManager.Instance.GetUsingGearInfo(4);

            if (_victim.UnitSetting.unitType == UnitType.Unit)
            {
                if (isGearHead_0002_Activate && gearHead is not null && gearHead.gearId.Equals("gear_head_0002") && gearHead.gearRarity >= 3) // N초간 상성 추댐 감소
                    CalculateElementTotal(ref totalElementDamage, (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 2));

                if (gearNeck is not null && gearNeck.gearId.Equals("gear_necklace_0001") && gearNeck.gearRarity >= 3) // 받는 상성 추댐 감소
                    CalculateElementTotal(ref totalElementDamage, (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 2));
            }

            if (_attacker.UnitSetting.unitType == UnitType.Unit && gearNeck is not null)
            {
                if (gearNeck.gearId.Equals("gear_necklace_0001")) // 상성 추댐 증가
                {
                    if (gearNeck.gearRarity >= 1) CalculateElementTotal(ref totalElementDamage, (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 0));
                    if (gearNeck.gearRarity >= 10) CalculateElementTotal(ref totalElementDamage, (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 4));
                }

                if (_attacker.UnitStat.HP >= _attacker.UnitStat.MaxHP && gearNeck.gearId.Equals("gear_necklace_0002") && gearNeck.gearRarity >= 10) // 상추댐 증가
                    CalculateElementTotal(ref totalElementDamage, (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 4));

                if (_attacker.UnitStat.HP / _attacker.UnitStat.MaxHP <= 0.3f && gearNeck.gearId.Equals("gear_necklace_0003") && gearNeck.gearRarity >= 10) // 상추댐 증가
                    CalculateElementTotal(ref totalElementDamage, (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 4));
            }

            // 버프 등 효과로 인한 상성 대미지 변화
            if (CheckHasUnitEffect(UNIT_EFFECT.BUFF_TAKE_ELEMENT_DMG))
                CalculateElementTotal(ref totalElementDamage, -GetUnitEffectTotalValue(UNIT_EFFECT.BUFF_TAKE_ELEMENT_DMG));
        }

        return totalElementDamage < 0.0f ? 0.0f : totalElementDamage;
    }

    private void CalculateElementTotal(ref float _value, float _amount)
    {
        if(_amount > 0.0f)
        {
            if (MgrBattleSystem.Instance.IsChallengeMode && MgrBattleSystem.Instance.ChallengeLevel == 2 && UnitSetting.unitType != UnitType.Unit && UnitSetting.unitType != UnitType.AllyBase)
                return;
        }

        _value += _amount;
    }

    private float CalculateCriRate(UnitBase _attacker, UnitBase _victim, float _baseCriRate)
    {
        float resultRate = _baseCriRate;

        if (_attacker.UnitSetting.unitType == UnitType.AllyBase)
            resultRate += MgrBattleSystem.Instance.GlobalOption.Option_AllyBaseCriRate;

        if (_attacker.UnitSetting.unitType == UnitType.Unit)
        {
            resultRate += MgrBattleSystem.Instance.GlobalOption.Option_UnitCriRate;

            UserGear gearHead = DataManager.Instance.GetUsingGearInfo(3);
            UserGear gearNeck = DataManager.Instance.GetUsingGearInfo(4);

            if (!isGearHead_0001_FirstGiveDmgCriRate && gearHead is not null && gearHead.gearId.Equals("gear_head_0001") && gearHead.gearRarity >= 10)
            {
                isGearHead_0001_FirstGiveDmgCriRate = true;
                resultRate += (float)DataManager.Instance.GetGearOptionValue(gearHead.gearId, 4);
            }

            if(gearNeck is not null)
            {
                if (_attacker.UnitStat.HP >= _attacker.UnitStat.MaxHP && gearNeck.gearId.Equals("gear_necklace_0002") && gearNeck.gearRarity >= 3)
                    resultRate += (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 2);

                if (_attacker.UnitStat.HP / _attacker.UnitStat.MaxHP <= 0.3f && gearNeck.gearId.Equals("gear_necklace_0003") && gearNeck.gearRarity >= 3)
                    resultRate += (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 2);

                if (_attacker.UnitSetting.unitClass == UnitClass.Arch && gearNeck.gearId.Equals("gear_necklace_0004") && gearNeck.gearRarity >= 10)
                {
                    int unitCnt = MgrBattleSystem.Instance.GetEnemyUnitListInClass(_attacker, UnitClass.Arch, true).Count;
                    unitCnt = unitCnt > 4 ? 4 : unitCnt;
                    resultRate += (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 4) * unitCnt;
                }
            }
        }

        return resultRate;
    }
    
    private float CalculateCriDmgRate(UnitBase _attacker, UnitBase _victim, float _baseCriDmg)
    {
        float resultDmg = _baseCriDmg;

        if (_attacker.UnitSetting.unitType == UnitType.AllyBase) resultDmg += MgrBattleSystem.Instance.GlobalOption.Option_AllyBaseCriDmg;
        if (_attacker.UnitSetting.unitType == UnitType.Unit) resultDmg += MgrBattleSystem.Instance.GlobalOption.Option_UnitCriDmg;

        return resultDmg;
    }

    public void OnDefaultDeath(UnitBase _attacker, UnitBase _victim, int _dmgChannel)
    {
        if (CurrUnitSkill is not null)
        {
            int skillIndex = GetSkillIndex(CurrUnitSkill);
            UnitSkillPersonalVariable[skillIndex].ResetSkill();

            CurrUnitSkill = null;
        }

        bool isAlreadyDeath = CheckIsState(UNIT_STATE.DEATH);

        UnitPlayableDirector.Stop();
        UnitPlayableDirector.playableAsset = null;
        SetUnitState(UNIT_STATE.DEATH);

        _attacker.OnKillEvent(this, _dmgChannel);

        MgrBattleSystem.Instance.RemoveActionShowHP(ActionToggleHPBar);
        sprrdHPBarBack.gameObject.SetActive(false);

        for (int i = ListEffectPersonalVariable.Count - 1; i >= 0; i--)
            ListEffectPersonalVariable[i].OnEnd();

        if (UnitSetting.unitType != UnitType.AllyBase && UnitSetting.unitType != UnitType.Unit)
        {
            if ((UnitSetting.unitType == UnitType.MidBoss || UnitSetting.unitType == UnitType.Boss) && UnitBaseParent is null)
                DeathSlowMotion();

            UserGear gearStove = DataManager.Instance.GetUsingGearInfo(2);
            UserGear gearTail = DataManager.Instance.GetUsingGearInfo(5);

            if(gearStove is not null && gearStove.gearId.Equals("gear_stove_0003") && gearStove.gearRarity >= 3)
                MgrBattleSystem.Instance.GlobalOption.Add_GearStove_0003_EnemyKill(_victim);

            if(!isGearTail_0001_Cooldown && gearTail is not null && gearTail.gearId.Equals("gear_tail_0001"))
            {
                if ((_attacker.UnitSetting.unitGrade == UnitGrade.C || _attacker.UnitSetting.unitGrade == UnitGrade.B) && gearTail.gearRarity >= 3)
                {
                    isGearTail_0001_Cooldown = true;
                    _attacker.SetHeal(_attacker.UnitStat.MaxHP * (float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 2));
                    TaskGearTail_0001().Forget();
                }
                if (_attacker.UnitSetting.unitGrade == UnitGrade.A && gearTail.gearRarity >= 10)
                {
                    isGearTail_0001_Cooldown = true;
                    _attacker.SetHeal(_attacker.UnitStat.MaxHP * (float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 4));
                    TaskGearTail_0001().Forget();
                }
            }

            if (!isDummyMonster)
                SetEnemyBoosterExp(gearStove);

            if (ObjEliteVFX is not null)
            {
                MgrObjectPool.Instance.HideObj("FX_Elite Aura", ObjEliteVFX);
                ObjEliteVFX = null;
            }

            if (MgrBattleSystem.Instance.GetEnemyUnitList(this, true).Count == 0 && MgrBattleSystem.Instance.ReserveSpawnCnt == 0)
            {
                if(MgrBattleSystem.Instance.GameMode != GAME_MODE.GoldMode && MgrBattleSystem.Instance.GameMode != GAME_MODE.Survival)
                    MgrBattleSystem.Instance.SetNextWave();
            }

            MgrBattleSystem.Instance.AddKillCnt();

            switch (UnitSetting.unitType)
            {
                case UnitType.Monster:
                {
                    if (MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode) MgrBattleSystem.Instance.GoldCollectAmount += Random.Range(100.0f, 400.0f);
                    else MgrBattleSystem.Instance.AddNormalMonsterKillCnt();
                    break;
                }
                case UnitType.EnemyUnit: MgrBattleSystem.Instance.AddEnemyUnitKillCnt(); break;
                case UnitType.Elite: MgrBattleSystem.Instance.AddEliteKillCnt(); break;
                case UnitType.MidBoss:
                    if(UnitBaseParent is null)
                        MgrBattleSystem.Instance.AddAnyBossKillCnt();
                    break;
                case UnitType.Boss:
                    if (UnitBaseParent is null)
                        MgrBattleSystem.Instance.AddAnyBossKillCnt();
                    break;
            }
        }
        else
        {
            if(UnitSetting.unitType == UnitType.Unit)
            {
                if (UnitSetting.unitClass == UnitClass.Warrior)
                    MgrBattleSystem.Instance.GlobalOption.AddWarriorFishCount(this, UnitSetting.unitCost * 0.2f);

                MgrBattleSystem.Instance.RefreshUnitSpawnCnt(UnitIndex);

                UserGear gearStove = DataManager.Instance.GetUsingGearInfo(2);
                if (gearStove is not null && gearStove.gearId.Equals("gear_stove_0003"))
                {
                    if (gearStove.gearRarity >= 1 && MathLib.CheckPercentage((float)DataManager.Instance.GetGearOptionValue(gearStove.gearId, 0)))
                    {
                        for (int i = 0; i < 2; i++)
                            MgrBattleSystem.Instance.GlobalOption.TaskDropBakedFish(_victim.transform.position + Vector3.up * _victim.GetUnitHeight(), _victim.transform.position).Forget();
                    }
                    if (gearStove.gearRarity >= 10) MgrBattleSystem.Instance.GlobalOption.Add_GearStove_0003_AllyDeath(_victim);
                }

                UserGear gearTail = DataManager.Instance.GetUsingGearInfo(5);

                if (gearTail is not null && gearTail.gearId.Equals("gear_tail_0002"))
                {
                    bool isSkillActive = false;

                    if (gearTail.gearRarity >= 1 && MathLib.CheckPercentage((float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 0)))
                    {
                        Vector3 v3AllyBasePos = MgrBattleSystem.Instance.GetAllyBase().transform.position;
                        float yPos = Random.Range(0.0f, -3.5f);

                        MgrObjectPool.Instance.ShowObj("FX_Return Unit", transform.position);
                        UnitBase spawnUnit = MgrUnitPool.Instance.ShowObj(UnitSetting.unitIndex, 0, new Vector3(v3AllyBasePos.x - 2.0f, yPos, yPos * 0.01f)).GetComponent<UnitBase>();
                        MgrObjectPool.Instance.ShowObj("tmpDmg", spawnUnit.GetUnitCenterPos()).GetComponent<DamageText>().SetDamageText(0.0f, _customText: MgrInGameData.Instance.DicLocalizationCSVData["Unit_Recall"]["korea"]);
                        isSkillActive = true;
                    }

                    if (gearTail.gearRarity >= 3 && MathLib.CheckPercentage((float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 2)))
                    {
                        MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Tail_0002_C", 1.0f);
                        MgrBattleSystem.Instance.ReduceUnitSpawnCooldown(UnitSetting.unitIndex, 0.5f, true);
                        isSkillActive = true;
                    }

                    if (gearTail.gearRarity >= 10 && MathLib.CheckPercentage((float)DataManager.Instance.GetGearOptionValue(gearTail.gearId, 4)))
                    {
                        listGearTail_0002.Clear();
                        listGearTail_0002.AddRange(MgrBattleSystem.Instance.GetEnemyUnitInSameIndex(this, UnitSetting.unitIndex, true));

                        foreach (UnitBase unit in listGearTail_0002)
                        {
                            for (int i = 0; i < unit.Skill_CoolDown.Length; i++)
                            {
                                unit.SetUnitSkillCoolDown(i, 0.0f);
                                MgrSound.Instance.PlayOneShotSFX("SFX_Unit_Summon_Cooldown", 1.0f);
                                MgrObjectPool.Instance.ShowObj("FX_Buff_Cooltime-Return", unit.transform.position).transform.SetParent(unit.transform);
                            }
                        }
                        isSkillActive = true;
                    }

                    if (isSkillActive)
                        MgrObjectPool.Instance.ShowObj("FX_Unit_Cooltime-Down_ Death Soul", transform.position);
                }
            }
            if (UnitSetting.unitType == UnitType.AllyBase)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_Base_Death", 1.0f);
                if(MgrBattleSystem.Instance.WeaponSys.SOWeaponData is not null)
                {
                    MgrBattleSystem.Instance.WeaponSys.Ska.AnimationState.ClearTrack(0);
                    MgrBattleSystem.Instance.WeaponSys.SetWeaponAnimation("death", false);
                }
            }
        }

        if(isAlreadyDeath)
            OnAfterDeath();
    }

    private void DeathSlowMotion()
    {
        if (MgrBattleSystem.Instance.currWave < MgrBattleSystem.Instance.totalWave)
            return;

        if(UnitSetting.unitType == UnitType.MidBoss)
        {
            bool isAlive = false;
            foreach (var unit in MgrBattleSystem.Instance.listUnitBoss)
            {
                if (!unit.CheckIsState(UNIT_STATE.DEATH))
                {
                    isAlive = true;
                    break;
                }
            }
            if(!isAlive)
            {
                MgrObjectPool.Instance.ShowObj("FX_ending hit shot", GetUnitCenterPos());
                MgrBattleSystem.Instance.SetReserveSpawnCount(0);
                MgrBattleSystem.Instance.SetNextWave();
            }
        }
        else
        {
            MgrObjectPool.Instance.ShowObj("FX_ending hit shot", GetUnitCenterPos());
            MgrBattleSystem.Instance.SetReserveSpawnCount(0);
            MgrBattleSystem.Instance.SetNextWave();
        }
    }

    private void SetEnemyBoosterExp(UserGear _gearStove)
    {
        if (MgrBattleSystem.Instance.ChapterID >= 31)
            return;

        float expMultiply = 1.0f;

        switch (UnitSetting.unitType)
        {
            case UnitType.Monster:
            case UnitType.EnemyUnit:
                if (!(_gearStove is null) && _gearStove.gearId.Equals("gear_stove_0002"))
                {
                    if (_gearStove.gearRarity >= 1) expMultiply += (float)DataManager.Instance.GetGearOptionValue(_gearStove.gearId, 0);
                    if (_gearStove.gearRarity >= 10) expMultiply += (float)DataManager.Instance.GetGearOptionValue(_gearStove.gearId, 4);
                }
                MgrBoosterSystem.Instance.AddBoosterExp((UnitSetting.unitType == UnitType.Monster ? 85.0f : 128.0f) * expMultiply, (UnitSetting.unitType == UnitType.Monster ? 85.0f : 128.0f));
                break;
            case UnitType.Elite:
                if (UnitBaseParent is not null)
                    return;

                int eliteRandomCnt = 2;

                if (_gearStove is not null && _gearStove.gearId.Equals("gear_stove_0001"))
                {
                    if (_gearStove.gearRarity >= 1 && MathLib.CheckPercentage((float)DataManager.Instance.GetGearOptionValue(_gearStove.gearId, 0))) eliteRandomCnt++;
                    if (_gearStove.gearRarity >= 10 && MathLib.CheckPercentage((float)DataManager.Instance.GetGearOptionValue(_gearStove.gearId, 4))) eliteRandomCnt++;
                }

                MgrBoosterSystem.Instance.AddRandomBoosterLv(transform.position, eliteRandomCnt);
                break;
            case UnitType.MidBoss:
                if (MgrBattleSystem.Instance.currWave != MgrBattleSystem.Instance.totalWave && UnitBaseParent is null)
                {
                    int midBossRandomCnt = 3;

                    if (_gearStove is not null && _gearStove.gearId.Equals("gear_stove_0001") && _gearStove.gearRarity >= 3 && MathLib.CheckPercentage((float)DataManager.Instance.GetGearOptionValue(_gearStove.gearId, 2)))
                        midBossRandomCnt++;

                    MgrBoosterSystem.Instance.AddRandomBoosterLv(transform.position, midBossRandomCnt);
                }
                break;
            default:
                break;
        }
    }

    public void OnRecallUnit()
    {
        if (UnitSetting.unitType != UnitType.Unit)
            return;

        //ChangeForceToIdle();
        //SetUnitState(UNIT_STATE.DEATH, _isChangeOnly: true);

        MgrSound.Instance.PlayOneShotSFX("SFX_Skill_Active_006", 0.5f);
        MgrObjectPool.Instance.ShowObj("FX_Return Unit", transform.position);

        TaskRecall().Forget();
    }

    private async UniTaskVoid TaskRecall()
    {
        await UniTask.Delay(1000, cancellationToken:this.GetCancellationTokenOnDestroy());

        if (CheckIsState(UNIT_STATE.DEATH))
            return;

        Ska.AnimationState.ClearTrack(0);
        ChangeForceToIdle();
        Ska.skeleton.SetToSetupPose();
        //SetUnitState(UNIT_STATE.IDLE, true);

        transform.position = new Vector3(MgrBattleSystem.Instance.GetAllyBase().transform.position.x, transform.position.y, transform.position.z);
        MgrObjectPool.Instance.ShowObj("FX_Unit Summons", transform.position);
        SetHeal(UnitStat.MaxHP * (float)DataManager.Instance.GetBoosterSkillData($"skill_active_006_{MgrBoosterSystem.Instance.DicSkill["skill_active_006"] - 1}").Params[0]);

        if (MgrBoosterSystem.Instance.DicSkill["skill_active_006"] >= 5)
        {
            MgrObjectPool.Instance.ShowObj("FX_PC-Heal", GetUnitCenterPos());
            AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)DataManager.Instance.GetBoosterSkillData($"skill_active_006_{MgrBoosterSystem.Instance.DicSkill["skill_active_006"] - 1}").Params[2]), this, this, new float[] { (float)DataManager.Instance.GetBoosterSkillData($"skill_active_006_{MgrBoosterSystem.Instance.DicSkill["skill_active_006"] - 1}").Params[1] });
        }
    }

    private async UniTaskVoid TaskHitEffect()
    {
        token_Hit?.Cancel();
        token_Hit?.Dispose();
        token_Hit = new CancellationTokenSource();

        Color color = Color.red;
        color.a = Ska.skeleton.GetColor().a;
        Ska.skeleton.SetColor(color);

        DoHPBarEffect();

        await UniTask.Delay(125, cancellationToken:token_Hit.Token);

        color = Color.white;
        color.a = Ska.skeleton.GetColor().a;
        Ska.skeleton.SetColor(color);
    }

    public void DoHPBarEffect()
    {
        switch(UnitSetting.unitType)
        {
            case UnitType.Boss:
                multiplyRegenTimer = 15.0f;
                MgrBattleSystem.Instance.SetBossHPBar(UnitStat.HP, UnitStat.MaxHP, Shield, this);

                if (UnitBaseParent is not null)
                {
                    SetHpShieldBar();
                    SetRotationHpBar();
                    sprrdHPBarBack.transform.localScale = new Vector3(transform.localScale.x * sprrdHPBarBack.transform.localScale.y, sprrdHPBarBack.transform.localScale.y, sprrdHPBarBack.transform.localScale.z);
                    SetHpFade();
                }
                break;
            case UnitType.MidBoss:
                multiplyRegenTimer = 15.0f;
                MgrBattleSystem.Instance.SetBossHPBar(UnitStat.HP, UnitStat.MaxHP, Shield, this);

                if(MgrBattleSystem.Instance.listUnitBoss.Count > 1 || UnitBaseParent is not null)
                {
                    SetHpShieldBar();
                    SetRotationHpBar();
                    sprrdHPBarBack.transform.localScale = new Vector3(transform.localScale.x * sprrdHPBarBack.transform.localScale.y, sprrdHPBarBack.transform.localScale.y, sprrdHPBarBack.transform.localScale.z);
                    SetHpFade();
                }
                break;
            case UnitType.AllyBase:
                if(TeamNum == 0) MgrBattleSystem.Instance.SetAllyHPBar(this, UnitStat.HP, UnitStat.MaxHP, Shield);
                else
                {
                    SetHpShieldBar();
                    SetRotationHpBar();
                    sprrdHPBarBack.transform.localScale = new Vector3(transform.localScale.x * sprrdHPBarBack.transform.localScale.y, sprrdHPBarBack.transform.localScale.y, sprrdHPBarBack.transform.localScale.z);
                    SetHpFade();
                }
                break;
            default:
                SetHpShieldBar();
                SetRotationHpBar();
                sprrdHPBarBack.transform.localScale = new Vector3(transform.localScale.x * sprrdHPBarBack.transform.localScale.y, sprrdHPBarBack.transform.localScale.y, sprrdHPBarBack.transform.localScale.z);
                SetHpFade();
                break;
        }
    }

    private void SetHpShieldBar()
    {
        if (Shield > 0.0f)
        {
            sprrdShield.color = Color.white;

            float subHpShield = UnitStat.HP + Shield;
            if (subHpShield >= UnitStat.MaxHP)
            {
                sprrdHP.size = new Vector2(Mathf.Lerp(0.0f, 4.0f, UnitStat.HP / subHpShield), 0.5f);
                sprrdShield.size = new Vector2(4.0f, 0.5f);
                sprrdYellow.size = sprrdShield.size;
            }
            else
            {
                sprrdHP.size = new Vector2(Mathf.Lerp(0.0f, 4.0f, UnitStat.HP / UnitStat.MaxHP), 0.5f);
                sprrdShield.size = new Vector2(Mathf.Lerp(0.0f, 4.0f, (UnitStat.HP + Shield) / UnitStat.MaxHP), 0.5f);
                sprrdYellow.size = sprrdShield.size;
            }
        }
        else
        {
            sprrdHP.size = new Vector2(Mathf.Lerp(0.0f, 4.0f, UnitStat.HP / UnitStat.MaxHP), 0.5f);
            sprrdShield.size = new Vector2(0.0f, 0.5f);
            sprrdYellow.size = sprrdHP.size;
        }

        sprrdYellow.gameObject.SetActive((CheckHasUnitEffect(UNIT_EFFECT.ETC_GOD) || (CheckHasUnitEffect(UNIT_EFFECT.BUFF_IMMORTALITY) && UnitStat.HP - (UnitStat.MaxHP * GetUnitEffectHighestValue(UNIT_EFFECT.BUFF_IMMORTALITY)) < 1.0f)));
    }

    private Sequence seqHPBar;
    private void SetHpFade()
    {
        if (!MgrBattleSystem.Instance.IsShowHPBar)
        {
            SetRotationHpBar();

            sprrdHPBarBack.color = Color.white;
            sprrdHP.color = Color.white;
            sprrdShield.color = Color.white;
            sprrdYellow.color = Color.white;
            sprrdHPFrame.color = Color.white;
            sprrdHPOutline.color = Color.white;

            if (seqHPBar != null && seqHPBar.IsActive())
                seqHPBar.Kill();

            seqHPBar = DOTween.Sequence();
            seqHPBar.Append(sprrdHPBarBack.DOFade(0.0f, 1.0f).SetDelay(1.0f).SetEase(Ease.Linear));
            seqHPBar.Join(sprrdHP.DOFade(0.0f, 1.0f).SetEase(Ease.Linear));
            seqHPBar.Join(sprrdShield.DOFade(0.0f, 1.0f).SetEase(Ease.Linear));
            seqHPBar.Join(sprrdYellow.DOFade(0.0f, 1.0f).SetEase(Ease.Linear));
            seqHPBar.Join(sprrdHPFrame.DOFade(0.0f, 1.0f).SetEase(Ease.Linear));
            seqHPBar.Join(sprrdHPOutline.DOFade(0.0f, 1.0f).SetEase(Ease.Linear));
        }
    }

    private async UniTaskVoid TaskBossHPRegen()
    {
        float timer = 4.0f;
        while(!CheckIsState(UNIT_STATE.DEATH))
        {
            await UniTask.Yield(this.GetCancellationTokenOnDestroy());

            multiplyRegenTimer -= Time.deltaTime;
            timer -= Time.deltaTime;
            if(timer <= 0.0f)
            {
                timer = 4.0f;

                float regenRate = UnitSetting.unitType == UnitType.MidBoss ? 0.005f : 0.01f;
                if (multiplyRegenTimer <= 0.0f)
                    regenRate *= 5.0f;

                float regenMultiply = 1.0f;
                regenMultiply += MgrBattleSystem.Instance.GlobalOption.Option_BossHPRegen;

                SetHeal(UnitStat.MaxHP * regenRate * regenMultiply);
                MgrBattleSystem.Instance.SetBossHPBar(UnitStat.HP, UnitStat.MaxHP, Shield, this);
                MgrObjectPool.Instance.ShowObj("FX_Buff_Dot Heal", GetUnitCenterPos());
            }
        }
    }

    private async UniTaskVoid TaskGearHead_0002()
    {
        isGearHead_0002_Activate = true;

        UserGear gearHead = DataManager.Instance.GetUsingGearInfo(3);

        if(gearHead.gearRarity > 0)
        {
            MgrSound.Instance.PlayOneShotSFX("SFX_Buff_ab", 1.0f);
            MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", transform.position).transform.SetParent(transform);
            MgrObjectPool.Instance.ShowObj("tmpDmg", transform.position).GetComponent<DamageText>().SetDamageText(0.0f, _isUnitEffect: true, _unitEffectIndex: (int)UNIT_EFFECT.BUFF_TAKE_DMG);
            if (gearHead.gearRarity >= 3) MgrObjectPool.Instance.ShowObj("tmpDmg", transform.position).GetComponent<DamageText>().SetDamageText(0.0f, _isUnitEffect: true, _unitEffectIndex: (int)UNIT_EFFECT.BUFF_TAKE_ELEMENT_DMG);
        }

        float duration = 10.0f;
        while (duration > 0.0f)
        {
            await UniTask.Yield(token_GearHead_0002.Token);
            duration -= Time.deltaTime;
        }

        isGearHead_0002_Activate = false;
    }

    private async UniTaskVoid TaskGearHead_0004()
    {
        isGearHead_0004_Activate = true;

        float duration = 2.0f;
        while (duration > 0.0f)
        {
            await UniTask.Yield(token_GearHead_0004.Token);
            duration -= Time.deltaTime;
        }

        isGearHead_0004_Activate = false;
    }

    private async UniTaskVoid TaskGearTail_0001()
    {
        isGearTail_0001_Cooldown = true;
        await UniTask.Delay(200, cancellationToken: this.GetCancellationTokenOnDestroy());
        isGearTail_0001_Cooldown = false;
    }

    // 카메라에 유닛 노출 여부
    public void OnVisibleInCamera()
    {
        if (UnitSetting.unitType != UnitType.Unit)
            return;

        MgrBattleSystem.Instance.RemoveUnitOutCamera(this);
        MgrBattleSystem.Instance.RefreshUnitOutCamera();
    }

    public void OnInvisibleInCamera()
    {
        if (UnitSetting.unitType != UnitType.Unit)
            return;

        MgrBattleSystem.Instance.AddUnitOutCamera(this);
        MgrBattleSystem.Instance.RefreshUnitOutCamera();
    }
}
