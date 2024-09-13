using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using BCH.Database;
using DG.Tweening;
using System;
using Random = UnityEngine.Random;
using SuperJam;
using System.Linq;
using Spine.Unity;
using UnityEngine.SceneManagement;
using System.Threading;
using System.Text;
using UnityEngine.Serialization;

public class MgrBattleSystem : Singleton<MgrBattleSystem>
{
    // 전체 게임 플레이 중인 유닛 리스트
    public List<UnitBase> ListUnitBase { get; private set; } = new List<UnitBase>();

    [field: Header("유닛 디버깅 전용 테스트 모드")]
    [field: SerializeField] public bool IsTestMode { get; private set; }
    [field: SerializeField] public bool IsTestFullDamaged { get; private set; }
    [SerializeField] private AudioClip clipBossBGM;

    // 스테이지 시작
    public bool isStageStart { get; private set; } // 스테이지 진행 여부
    private int remainReviveCnt; // 부활 남은 횟수
    public bool IsPause { get; private set; } // 일시정지 여부
    public float PlayTime { get; private set; } // 플레이 타임
    public BackGroundData.ChapterBackGround ChapterBackGroundData { get; private set; } // 맵 배경 및 설정 SO
    public SOMode_InGame SOModeData { get; private set; } // Mode 정보 SO

    // 도전 모드
    public bool IsChallengeMode { get; private set; } // 도전 모드 여부
    public int ChallengeLevel { get; private set; } // 도전 모드 단계

    // 킬 카운트
    public int KillCnt { get; private set; }
    public void AddKillCnt() => KillCnt++;
    public int NormalMonsterKillCnt { get; private set; }
    public void AddNormalMonsterKillCnt() => NormalMonsterKillCnt++;
    public int EnemyUnitKillCnt { get; private set; }
    public void AddEnemyUnitKillCnt() => EnemyUnitKillCnt++;
    public int EliteKillCnt { get; private set; }
    public void AddEliteKillCnt() => EliteKillCnt++;
    public int AnyBossKillCnt { get; private set; }
    public void AddAnyBossKillCnt() => AnyBossKillCnt++;

    // 인게임 글로벌 설정 클래스
    public InGameGlobalClass GlobalOption { get; private set; }

    // 체력 바 활성,비활성 체크
    public bool IsShowHPBar { get; private set; }
    public System.Action<bool> ActionShowHP; // 체력바 활성/비활성 이벤트

    [field: Header("오브젝트 세팅")]
    [field: SerializeField] public Sprite sprAlly { get; private set; } // 기지 체력바 스프라이트
    [field: SerializeField] public Sprite sprEnemy { get; private set; } // 적 체력바 스프라이트

    [SerializeField] private TextMeshProUGUI tmpAllyHP; // 기지 체력 표기 TMP
    private TextMeshProUGUI[] tmpBossHP = new TextMeshProUGUI[2]; // 보스 체력 표기 TMP (1마리, 2마리)
    private TextMeshProUGUI tmpBossType; // 보스 타입 (중간, 최종)
    private Image[] imgBossClass = new Image[2]; // 보스 이미지 (중간, 최종)

    [Header("인게임 진입 연출")]
    [SerializeField] private Image imgFade; // 페이드 연출 용 이미지
    [SerializeField] private RectTransform[] arrRtShowEffect_Left; // 좌측 연출
    [SerializeField] private RectTransform[] arrRtShowEffect_Top; // 상단 연출
    [SerializeField] private RectTransform[] arrRtShowEffect_Bottom; // 하단 연출
    [SerializeField] private RectTransform[] arrRtShowEffect_Right; // 우측 연출

    [Header("챕터 진입 연출")]
    [SerializeField] private RectTransform rtChapterInfo;
    private Image imgChapterInfo;
    private TextMeshProUGUI tmpChapterInfo;
    private TextMeshProUGUI tmpChapterTitle;
    [SerializeField] private RectTransform rtChallengeInfo;
    private Image imgChallengeInfo;
    private TextMeshProUGUI tmpChallengeInfo;
    private TextMeshProUGUI tmpChallengeTitle;
    [SerializeField] private Image imgChallengeMedalBack;
    [SerializeField] private Image imgChallengeMedal;
    private Transform tfParentTmpDesc;
    [SerializeField] private Transform tfCanvTopLeft;
    [SerializeField] private GameObject objChallengeMedalDesc;
    [SerializeField] private GameObject objChallengePressMedal;
    [SerializeField] private TextMeshProUGUI tmpChallengeDesc;

    [field: Header("필드 드랍 붕어빵 연출")]
    [field: SerializeField] public AnimationCurve AnimDropFishCurve;

    // 웨이브 세팅
    [field: Space(10.0f), Header("챕터/난이도 모드 세팅")]
    [field: SerializeField] public int ChapterID { get; private set; }
    public int GetCurrentThema() => ((ChapterID - 1) / 6) + 1;
    [field: SerializeField] public GAME_MODE GameMode { get; private set; } // 게임 모드

    [Header("웨이브 표기 텍스트")]
    [SerializeField] private TextMeshProUGUI tmpWave; // 웨이브 표기 TMP
    private float waveLimitTimer; // 웨이브 당 진행 제한 시간
    public bool IsBossAppeared { get; set; } // 보스 출현 여부
    public int currWave { get; private set; } // 현재 웨이브
    public int totalWave { get; private set; } // 총 웨이브
    public int ReserveSpawnCnt { get; set; } // 소환 예약된 유닛 수

    // 준보스, 보스 세팅
    [field: Space(10.0f), Header("중간 보스 / 보스 관련")]
    public List<UnitBase> listUnitBoss { get; private set; } = new List<UnitBase>(); // 보스 리스트 저장
    [SerializeField] private GameObject[] objBossHPBack; // 1줄 또는 2줄 보스 체력 오브젝트
    private SlicedFilledImage[] imgBossHP = new SlicedFilledImage[2]; // 보스 체력 바
    private SlicedFilledImage[] imgBossShield = new SlicedFilledImage[2]; // 보스 체력 바
    [SerializeField] private Sprite[] sprBossHPIcon; // 보스 아이콘
    private GameObject objAngryIcon;
    private GameObject objAngryBackImg;
    private TextMeshProUGUI tmpAngryRemain; // 광폭화 남은 시간 TEXT
    public bool IsBossAngry { get; private set; } // 광폭화 여부

    // 보스 등장 경고 연출 스파인
    [SerializeField] private SkeletonGraphic skgBossWarning;

    // 스폰 테이블
    private EnemySpawn enemySpawnData = null;
    public EnemySpawn EnemySpawnData => enemySpawnData;

    // 기지 세팅
    [Header("기지 관련")]
    private UnitBase unitAllyBase = null;
    [SerializeField] private SlicedFilledImage imgAllyHP;
    [SerializeField] private SlicedFilledImage imgAllyShield;
    [SerializeField] private Image imgAllyLowHP;

    // 사이드 엑티브, 유닛 소환 슬롯 오브젝트
    [Header("사이드 엑티브 스킬, 유닛 소환 버튼")]
    [SerializeField] private GameObject objSideSkillRoot;
    [field: SerializeField] public GameObject objUnitSpawnRoot { get; private set; }
    private UnitSlotBtn[] arrUnitSlotBtn = new UnitSlotBtn[6];
    public UnitSlotBtn[] ArrUnitSlotBtn => arrUnitSlotBtn;

    // 웨폰 시스템
    [field: Header("기지 무기 관련")]
    [field:SerializeField] public WeaponSystem WeaponSys { get; private set; }
    [field:SerializeField] public WeaponSystem WeaponSysOppo { get; private set; }

    // 부활 시스템
    [Header("부활")]
    [SerializeField] private GameObject objRevivePop;
    [SerializeField] private TextMeshProUGUI tmpReviveRemainCnt;
    [SerializeField] private Image imgReviveCircle;
    [SerializeField] private TextMeshProUGUI tmpReviveTime;
    [SerializeField] private Button btn_TokenOK;
    [SerializeField] private Button btn_AdOK;

    [Header("유닛 외부 체크")]
    [SerializeField] private GameObject objOutCameraUI;
    [SerializeField] private UnitOutCameraIcon[] arrUnitOutCamera;
    [SerializeField] private GameObject objOutCameraUIRight;
    [SerializeField] private UnitOutCameraIcon[] arrUnitOutCameraRight;

    // 일시정지 및 나가기 시스템
    [Header("일시정지")]
    [SerializeField] private GameObject objPausePop;
    [SerializeField] private GameObject objCanvLeaveConfirm;
    public GameObject ObjCanvLeaveConfirm => objCanvLeaveConfirm;

    // 챕터 결과 연출
    [Header("챕터 결과")]
    [SerializeField] private Canvas canvResult;
    [SerializeField] private GameObject[] objRoundResultPrefab;
    [SerializeField] private GameObject objResultUserData;
    [SerializeField] private CanvasGroup canvgPlayTime;
    [SerializeField] private TextMeshProUGUI tmpResultTime;
    [SerializeField] private GameObject objAccountExpUI;
    [SerializeField] private GameObject objEmptyItem;
    [SerializeField] private TextMeshProUGUI tmpUserInfo;

    // 챕터 보상 연출
    [Header("챕터 보상")]
    [SerializeField] private GameObject objRewardIconPrefab;
    [SerializeField] private GameObject objRewardMainStarPrefab;
    [SerializeField] private ScrollRect srReward;
    [SerializeField] private Transform tfRewardContent;
    [SerializeField] private RectTransform rtRewardBack;
    [SerializeField] private CanvasGroup canvgTouchNotice;
    [SerializeField] private SlicedFilledImage imgSliderEXPBase;
    [SerializeField] private SlicedFilledImage imgSliderEXPChanged;
    [SerializeField] private TextMeshProUGUI tmpChangedEXP;

    [field: Header("보상 팝업")] //로비에서 사용하던 팝업 그대로 가져 옴
    [field: SerializeField] public ItemInfoPopup popItemInfo { get; private set; }
    [field: SerializeField] public RewardItemInfoPopup popRewardItemInfo { get; private set; }
    [field: SerializeField] public LevelUpEffect popLvUp { get; private set; }

    // 미션 용 변수
    private int midBossKillCnt; // 중간보스 킬 카운트

    // 튜토리얼 용 변수
    public int TutorialStep { get; set; }
    public int TutorialSubStep { get; set; }

    // 임시 사용 전용 리스트
    private List<UnitBase> listTemp = new List<UnitBase>(); // 리스트를 항상 new 하지 않게 하기 위한 리스트

    [Header("골드 모드")]
    [SerializeField] private GameObject objTimerSlider; // 제한 시간 슬라이더 오브젝트
    public GameObject ObjTimerSlider => objTimerSlider;
    
    [SerializeField] private SlicedFilledImage imgTimerSlider; // 제한 시간 슬라이더 이미지
    public SlicedFilledImage ImgTimerSlider => imgTimerSlider;
    public float ModeTimer { get; set; } // 모드 시간
    public int GoldModeAngryLevel { get; private set; } // 공격력 증가 단계
    public float GoldCollectAmount = 0.0f;

    // 디버그 전용 오브젝트
    [Header("디버깅 용 UI")]
    [SerializeField] private GameObject objDebug;
    [SerializeField] private GameObject objContentUnitPrefab;
    [SerializeField] private GameObject objContentEnemyPrefab;
    [SerializeField] private RectTransform rtUnitDebugContent;
    [SerializeField] private RectTransform rtEnemyDebugContent;

    private void Awake()
    {
        if(MgrInGameUserData.Instance is not null)
        {
            ChapterID = MgrInGameUserData.Instance.CurrChapter;
            IsChallengeMode = MgrInGameUserData.Instance.IsChallengeMode;
            ChallengeLevel = MgrInGameUserData.Instance.ChallengeLevel;
            GameMode = MgrInGameUserData.Instance.GameMode;
        }

        foreach (var bossHp in objBossHPBack)
            bossHp.SetActive(false);

        for (int i = 0; i < arrUnitSlotBtn.Length; i++)
            arrUnitSlotBtn[i] = objUnitSpawnRoot.transform.GetChild(i).GetComponent<UnitSlotBtn>();

        imgChapterInfo = rtChapterInfo.transform.GetChild(0).GetComponent<Image>();
        tmpChapterInfo = rtChapterInfo.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        tmpChapterTitle = rtChapterInfo.transform.GetChild(2).GetComponent<TextMeshProUGUI>();

        imgChallengeInfo = rtChallengeInfo.transform.GetChild(0).GetComponent<Image>();
        tmpChallengeInfo = rtChallengeInfo.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        tmpChallengeTitle = rtChallengeInfo.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        tfParentTmpDesc = rtChallengeInfo.transform.GetChild(3);

        imgFade.gameObject.SetActive(true);

        btnLeaveBtn.interactable = !(GameMode == GAME_MODE.Chapter && ChapterID == 0);
    }

    private void Start()
    {
        for (int i = 0; i < arrRtShowEffect_Left.Length; i++)
            arrRtShowEffect_Left[i].anchoredPosition = arrRtShowEffect_Left[i].anchoredPosition + Vector2.left * 600.0f;

        for (int i = 0; i < arrRtShowEffect_Bottom.Length; i++)
            arrRtShowEffect_Bottom[i].anchoredPosition = arrRtShowEffect_Bottom[i].anchoredPosition + Vector2.down * 500.0f;

        for (int i = 0; i < arrRtShowEffect_Right.Length; i++)
            arrRtShowEffect_Right[i].anchoredPosition = arrRtShowEffect_Right[i].anchoredPosition + Vector2.right * 800.0f;

        for (int i = 0; i < arrRtShowEffect_Top.Length; i++)
            arrRtShowEffect_Top[i].anchoredPosition = arrRtShowEffect_Top[i].anchoredPosition + Vector2.up * 300.0f;

        if (!IsTestMode) TaskWaitDataLoaded().Forget();
        else
        {
            GlobalOption = new InGameGlobalClass();
            imgFade.DOFade(0.0f, 0.75f);
        }
    }

    private readonly List<string> listCheckDuplicateUnitIndex = new List<string>();
    private readonly string[] pvpRandomUnitIndex = new string[6];
    private async UniTaskVoid TaskWaitDataLoaded()
    {
        // 데이터 로드 완료 대기
        await UniTask.WaitUntil(() => DataManager.Instance.IsDataLoaded && MgrInGameData.Instance.isCSVLoaded);

        // 초기화 시작
        listCheckDuplicateUnitIndex.Clear();
        
        Time.timeScale = 1.2f;

        GlobalOption = new InGameGlobalClass();
        GlobalOption.Init_GlobalVariable(DataManager.Instance.UserInventory.traitLv);

        IsPause = false;
        currWave = 0;
        remainReviveCnt = GameMode == GAME_MODE.Chapter ? 1 : 0;
        midBossKillCnt = 0;

        TutorialStep = -1;
        TutorialSubStep = 0;

        // 모드에 맞는 챕터 세팅 정보 가져오기
        for(int i = 0; i < MgrInGameData.Instance.SoBackGroundData.data.Length; i++)
        {
            if (MgrInGameData.Instance.SoBackGroundData.data[i].GameMode == GameMode && MgrInGameData.Instance.SoBackGroundData.data[i].ModeLevel == ChapterID)
            {
                ChapterBackGroundData = MgrInGameData.Instance.SoBackGroundData.data[i];
                break;
            }
        }

        if (ChapterBackGroundData is null)
            Debug.LogError($"[ERROR] - {GameMode} 모드의 {ChapterID} 단계 ChapterBackGroundData 찾을 수 없음.");

        MgrCamera.Instance.SetBG();
        MgrSound.Instance.StartBGM(ChapterBackGroundData.ClipBGM, 1.0f);

        SOModeData = ChapterBackGroundData.SOMode;
        SOModeData.InitMode(this);

        // 초기 유닛 소환 용 (트레이닝, 생존전)
        if(GameMode is GAME_MODE.Training or GAME_MODE.Survival)
        {
            if (MgrInGameUserData.Instance is not null)
            {
                foreach (var unitSlot in arrUnitSlotBtn)
                {
                    if (unitSlot.UnitInfo is not null)
                    {
                        float yPos = Random.Range(0.0f, -3.5f);
                        Vector3 v3SpawnPos = new Vector3(GetAllyBase().transform.position.x - 2.0f, yPos, yPos * 0.01f);
                        MgrUnitPool.Instance.ShowObj(unitSlot.UnitInfo.unitIndex, 0, v3SpawnPos);
                    }
                }
            }
        }

        // PVP 테스트용
        if (GameMode == GAME_MODE.Pvp)
        {
            List<string> listUnitSelectIndex = new List<string>();
            for (int i = 0; i < MgrInGameData.Instance.SOUnitData.unitSetting.Length; i++)
            {
                if (MgrInGameData.Instance.SOUnitData.unitSetting[i].unitType == UnitType.Unit && MgrInGameData.Instance.SOUnitData.unitSetting[i].isActivate)
                    listUnitSelectIndex.Add(MgrInGameData.Instance.SOUnitData.unitSetting[i].unitIndex);
            }

            for (int i = 0; i < 6; i++)
            {
                pvpRandomUnitIndex[i] = listUnitSelectIndex[Random.Range(0, listUnitSelectIndex.Count)];
                listUnitSelectIndex.Remove(pvpRandomUnitIndex[i]);
            }

            for (int i = 0; i < pvpRandomUnitIndex.Length; i++)
            {
                if (listCheckDuplicateUnitIndex.Contains(pvpRandomUnitIndex[i]))
                    continue;

                listCheckDuplicateUnitIndex.Add(pvpRandomUnitIndex[i]);
            }
            OnActionShowHP(true);
        }

        SetWaveText();
        MgrBakingSystem.Instance.InitAutoBlockedText();

        // 스테이지 시작
        isStageStart = true;

        LoadInGameSound();

        await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());

        imgFade.color = Color.black;
        await imgFade.DOFade(0.0f, 0.75f).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(cancellationToken:this.GetCancellationTokenOnDestroy());

        imgFade.gameObject.SetActive(false);

        SOModeData.InitModeShowEffect().Forget();
        SOModeData.StartMode();
    }

    private void LoadInGameSound()
    {
        StringBuilder sbUnitIndex = new StringBuilder();
        for (int i = 0; i < 6; i++)
        {
            if (arrUnitSlotBtn[i].UnitInfo is null || listCheckDuplicateUnitIndex.Contains(arrUnitSlotBtn[i].UnitInfo.unitIndex))
                continue;

            listCheckDuplicateUnitIndex.Add(arrUnitSlotBtn[i].UnitInfo.unitIndex);
        }
        foreach (var strUnitIndex in listCheckDuplicateUnitIndex)
            sbUnitIndex.Append(sbUnitIndex.Length == 0 ? strUnitIndex : $":{strUnitIndex}");

        MgrSound.Instance.LoadInGameUnitSound(sbUnitIndex.ToString()).Forget();
        if(GameMode == GAME_MODE.Chapter)
            MgrSound.Instance.LoadThemeSound(GetCurrentThema()).Forget();
    }

    public void ShowEnterEffect_Left()
    {
        MgrSound.Instance.PlayOneShotSFX("SFX_Chapter_Start_Icon_1", 1.0f);

        // 인게임 진입 연출
        for (int i = 0; i < arrRtShowEffect_Left.Length; i++)
            arrRtShowEffect_Left[i].DOAnchorPosX(arrRtShowEffect_Left[i].anchoredPosition.x + 600.0f, 1.0f).SetEase(Ease.OutBack, 0.75f);
    }

    public async UniTask ShowEnterEffect_Bottom()
    {
        MgrSound.Instance.PlayOneShotSFX("SFX_Chapter_Start_Icon_1", 1.0f);

        foreach (var unitSlot in arrUnitSlotBtn)
        {
            unitSlot.InitShowEffect();
            await UniTask.Delay(66, cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        for (int i = 0; i < arrRtShowEffect_Bottom.Length; i++)
        {
            if (ChapterID == 0 && i > 0)
                continue;
                
            arrRtShowEffect_Bottom[i].DOAnchorPosY(arrRtShowEffect_Bottom[i].anchoredPosition.y + 500.0f, 0.75f).SetEase(Ease.OutBack, 0.75f);
        }
    }

    public void ShowEnterEffect_Top()
    {
        for (int i = 0; i < arrRtShowEffect_Top.Length; i++)
            arrRtShowEffect_Top[i].DOAnchorPosY(arrRtShowEffect_Top[i].anchoredPosition.y - 300.0f, 1.0f).SetEase(Ease.OutBack, 0.75f);

        MgrSound.Instance.PlayOneShotSFX("SFX_Chapter_Start_Icon_2", 1.0f);
    }

    private int ChallengeSpecialMapEffectNum()
    {
        switch (ChapterID)
        {
            case <= 6:
                return 6;
            case <= 12:
                return 7;
            case <= 18:
                return 8;
            default:
                return 0;
        }
    }

    /// <summary>
    /// 챕터 타이틀 연출
    /// </summary>
    public async UniTaskVoid TaskChapterTitle()
    {
        // 도전,챕터 진입 연출
        if (IsChallengeMode)
        {
            tmpChallengeInfo.text = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Chapter/Text/Challenge"]["korea"]}";
            if (MgrInGameData.Instance.DicLocalizationCSVData.ContainsKey($"Chapter/Title/Chapter_{100000 + ChapterID}")) tmpChallengeTitle.text = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Chapter/Title/Chapter_{100000 + ChapterID}"]["korea"]}";
            else tmpChallengeTitle.text = $"ERROR_{100000 + ChapterID}";

            imgChallengeInfo.transform.localScale = new Vector3(0.0f, 1.0f, 1.0f);
            rtChallengeInfo.GetComponent<CanvasGroup>().alpha = 0.0f;
        }
        else
        {
            tmpChapterInfo.text = $"Chapter {ChapterID}";
            if (MgrInGameData.Instance.DicLocalizationCSVData.ContainsKey($"Chapter/Title/Chapter_{100000 + ChapterID}")) tmpChapterTitle.text = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Chapter/Title/Chapter_{100000 + ChapterID}"]["korea"]}";
            else tmpChapterTitle.text = $"ERROR_{100000 + ChapterID}";

            imgChapterInfo.transform.localScale = new Vector3(0.0f, 1.0f, 1.0f);
            rtChapterInfo.GetComponent<CanvasGroup>().alpha = 0.0f;
        }

        await UniTask.Delay(10, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrSound.Instance.PlayOneShotSFX("SFX_Chapter_Start_Icon_3", 1.0f);

        if (IsChallengeMode)
        {
            imgChallengeMedal.raycastTarget = false;

            rtChallengeInfo.localPosition = new Vector2(0.0f, -20.0f);
            rtChallengeInfo.gameObject.SetActive(true);

            rtChallengeInfo.DOLocalMoveY(80.0f, 0.5f).SetEase(Ease.Linear);
            rtChallengeInfo.GetComponent<CanvasGroup>().DOFade(1.0f, 0.33f).SetEase(Ease.Linear);
            imgChallengeInfo.transform.DOScaleX(1.0f, 0.5f).SetEase(Ease.Linear);

            await UniTask.Delay(700, cancellationToken: this.GetCancellationTokenOnDestroy());

            tmpChallengeDesc.text = string.Empty;

            TextMeshProUGUI tmpTemp;
            if (ChallengeLevel == 0)
            {
                imgChallengeInfo.rectTransform.DOSizeDelta(new Vector2(imgChallengeInfo.rectTransform.sizeDelta.x, 460.0f), 0.3f);

                tmpTemp = tfParentTmpDesc.GetChild(0).GetComponent<TextMeshProUGUI>();
                tmpTemp.text = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Chapter/Text/Challenge_Mission_000"]["korea"]}";
                tmpChallengeDesc.text = tmpTemp.text;
                tmpTemp.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                tmpTemp.gameObject.SetActive(true);
                tmpTemp.DOFade(1.0f, 0.3f).SetEase(Ease.Linear);
            }
            if (ChallengeLevel == 1)
            {
                imgChallengeInfo.rectTransform.DOSizeDelta(new Vector2(imgChallengeInfo.rectTransform.sizeDelta.x, 585.0f), 0.3f);

                tmpTemp = tfParentTmpDesc.GetChild(0).GetComponent<TextMeshProUGUI>();
                tmpTemp.text = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Chapter/Text/Challenge_Mission_001"]["korea"]}";
                tmpChallengeDesc.text = tmpTemp.text;
                tmpTemp.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                tmpTemp.gameObject.SetActive(true);
                tmpTemp.DOFade(1.0f, 0.3f).SetEase(Ease.Linear);

                await UniTask.Delay(400, cancellationToken: this.GetCancellationTokenOnDestroy());

                tmpTemp = tfParentTmpDesc.GetChild(1).GetComponent<TextMeshProUGUI>();
                tmpTemp.text = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Chapter/Text/Challenge_Mission_002"]["korea"]}";
                tmpChallengeDesc.text += $"\n{tmpTemp.text}";
                tmpTemp.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                tmpTemp.gameObject.SetActive(true);
                tmpTemp.DOFade(1.0f, 0.3f).SetEase(Ease.Linear);

                await UniTask.Delay(400, cancellationToken: this.GetCancellationTokenOnDestroy());

                tmpTemp = tfParentTmpDesc.GetChild(2).GetComponent<TextMeshProUGUI>();
                tmpTemp.text = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Chapter/Text/Challenge_Mission_003"]["korea"]}";
                tmpChallengeDesc.text += $"\n{tmpTemp.text}";
                tmpTemp.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                tmpTemp.gameObject.SetActive(true);
                tmpTemp.DOFade(1.0f, 0.3f).SetEase(Ease.Linear);
            }
            if (ChallengeLevel == 2)
            {
                imgChallengeInfo.rectTransform.DOSizeDelta(new Vector2(imgChallengeInfo.rectTransform.sizeDelta.x, 585.0f), 0.3f);

                tmpTemp = tfParentTmpDesc.GetChild(0).GetComponent<TextMeshProUGUI>();
                tmpTemp.text = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Chapter/Text/Challenge_Mission_004"]["korea"]}";
                tmpChallengeDesc.text = tmpTemp.text;
                tmpTemp.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                tmpTemp.gameObject.SetActive(true);
                tmpTemp.DOFade(1.0f, 0.3f).SetEase(Ease.Linear);

                await UniTask.Delay(400, cancellationToken: this.GetCancellationTokenOnDestroy());

                tmpTemp = tfParentTmpDesc.GetChild(1).GetComponent<TextMeshProUGUI>();
                tmpTemp.text = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Chapter/Text/Challenge_Mission_005"]["korea"]}";
                tmpChallengeDesc.text += $"\n{tmpTemp.text}";
                tmpTemp.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                tmpTemp.gameObject.SetActive(true);
                tmpTemp.DOFade(1.0f, 0.3f).SetEase(Ease.Linear);

                await UniTask.Delay(400, cancellationToken: this.GetCancellationTokenOnDestroy());

                tmpTemp = tfParentTmpDesc.GetChild(2).GetComponent<TextMeshProUGUI>();
                tmpTemp.text = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Chapter/Text/Challenge_Mission_{ChallengeSpecialMapEffectNum():D3}"]["korea"]}";
                tmpChallengeDesc.text += $"\n{tmpTemp.text}";
                tmpTemp.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
                tmpTemp.gameObject.SetActive(true);
                tmpTemp.DOFade(1.0f, 0.3f).SetEase(Ease.Linear);
            }

            await UniTask.Delay(2500, cancellationToken: this.GetCancellationTokenOnDestroy());

            imgChallengeMedalBack.transform.SetParent(tfCanvTopLeft);

            List<Vector3> listBezierCurve = MathLib.CalculateBezierCurves(new Vector3[]
            {
                imgChallengeMedalBack.rectTransform.anchoredPosition,
                imgChallengeMedalBack.rectTransform.anchoredPosition + Vector2.left * 350.0f + Vector2.down * 500.0f,
                new Vector3(885.0f, -115.0f, 0.0f),
                new Vector3(685.0f, -115.0f, 0.0f)
            }, 100);

            imgChallengeMedalBack.rectTransform.DOScale(0.645f, 0.15f).SetDelay(1.6f);
            rtChallengeInfo.GetComponent<CanvasGroup>().DOFade(0.0f, 0.33f).SetEase(Ease.Linear);

            float duration = 1.6f;
            while (duration > 0.0f)
            {
                duration -= Time.deltaTime;

                int curveIndex = (int)Mathf.Lerp(0.0f, 99.0f, (1.6f - duration) / 1.6f);
                imgChallengeMedalBack.rectTransform.anchoredPosition = listBezierCurve[curveIndex];

                await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            imgChallengeMedalBack.enabled = true;
        }
        else
        {
            // 페이드 인 되면서 아래에서 위로 나타나는 연출
            rtChapterInfo.localPosition = new Vector2(0.0f, -20.0f);
            rtChapterInfo.gameObject.SetActive(true);

            rtChapterInfo.DOLocalMoveY(80.0f, 0.5f).SetEase(Ease.Linear);
            rtChapterInfo.GetComponent<CanvasGroup>().DOFade(1.0f, 0.33f).SetEase(Ease.Linear);
            imgChapterInfo.transform.DOScaleX(1.0f, 0.5f).SetEase(Ease.Linear);

            await UniTask.Delay(2500, cancellationToken: this.GetCancellationTokenOnDestroy());
            
            // 페이드 아웃
            rtChapterInfo.GetComponent<CanvasGroup>().DOFade(0.0f, 0.33f).SetEase(Ease.Linear);
        }

        await UniTask.Delay(350, cancellationToken: this.GetCancellationTokenOnDestroy());

        rtChapterInfo.gameObject.SetActive(false);
        rtChallengeInfo.gameObject.SetActive(false);

        rtChallengeInfo.GetComponent<CanvasGroup>().alpha = 1.0f;
        imgChallengeMedal.raycastTarget = true;
    }

    private bool isLowHPVFX = false;
    private CancellationTokenSource token_LowHP;
    /// <summary>
    /// 기지 체력바 세팅
    /// </summary>
    /// <param name="_unitbase">기지</param>
    /// <param name="_hp">현재 체력</param>
    /// <param name="_maxHp">최대 체력</param>
    /// <param name="_shield">쉴드</param>
    public void SetAllyHPBar(UnitBase _unitbase, float _hp, float _maxHp, float _shield)
    {
        if (_unitbase.TeamNum != 0)
            return;

        int displayHP = (int)_hp;

        if (0.0f < _hp && _hp < 1.0f)
            displayHP = 1;

        if (displayHP < 0)
            displayHP = 0;

        tmpAllyHP.text = $"{displayHP} / {(int)_maxHp}";

        if (_shield > 0.0f)
        {
            if (_hp + _shield > _maxHp)
            {
                imgAllyShield.fillAmount = 1.0f;
                imgAllyHP.fillAmount = _hp / (_hp + _shield);
            }
            else
            {
                imgAllyShield.fillAmount = (_hp + _shield) / _maxHp;
                imgAllyHP.fillAmount = _hp / _maxHp;
            }
        }
        else
        {
            imgAllyShield.fillAmount = 0.0f;
            imgAllyHP.fillAmount = _hp / _maxHp;
        }

        if (!isStageStart)
            return;

        if(imgAllyHP.fillAmount > 0.2f)
        {
            imgAllyLowHP.DOKill();
            imgAllyLowHP.DOFade(0.0f, 0.2f).SetEase(Ease.Linear);
            isLowHPVFX = false;

            token_LowHP?.Cancel();
        }
        else
        {
            if(!isLowHPVFX)
            {
                isLowHPVFX = true;
                imgAllyLowHP.DOKill();
                imgAllyLowHP.DOFade(1.0f, 1.0f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);

                token_LowHP?.Cancel();
                token_LowHP?.Dispose();
                token_LowHP = new CancellationTokenSource();
                TaskLowHPSFX().Forget();
            }
        }
    }

    private async UniTaskVoid TaskLowHPSFX()
    {
        while(isLowHPVFX)
        {
            MgrSound.Instance.PlayOneShotSFX("SFX_Base_Hp_Low", 1.0f);
            await UniTask.Delay(1950, cancellationToken: token_LowHP.Token);
        }
    }

    /// <summary>
    /// 보스 체력바 세팅
    /// </summary>
    /// <param name="_hp">체력</param>
    /// <param name="_maxHp">최대 체력</param>
    /// <param name="_shield">쉴드</param>
    /// <param name="_unit">대상 보스</param>
    public void SetBossHPBar(float _hp, float _maxHp, float _shield, UnitBase _unit)
    {
        int displayHP = (int)_hp;
        if (displayHP < 0)
            displayHP = 0;

        int index = -1;
        for(int i = 0; i < listUnitBoss.Count; i++)
        {
            if(listUnitBoss[i] == _unit)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
            return;

        tmpBossHP[index].text = $"{displayHP} / {(int)_maxHp}";

        if (_shield > 0.0f)
        {
            if (_hp + _shield > _maxHp)
            {
                imgBossShield[index].fillAmount = 1.0f;
                imgBossHP[index].fillAmount = _hp / (_hp + _shield);
            }
            else
            {
                imgBossShield[index].fillAmount = (_hp + _shield) / _maxHp;
                imgBossHP[index].fillAmount = _hp / _maxHp;
            }
        }
        else
        {
            imgBossShield[index].fillAmount = 0.0f;
            imgBossHP[index].fillAmount = _hp / _maxHp;
        }
    }
    /// <summary>
    /// 보스 체력바 UI 세팅
    /// </summary>
    /// <param name="_type">표기 이름 (BOSS / MID-BOSS)</param>
    /// <param name="_class">타입 클래스</param>
    /// <param name="_indexCnt">보스 인덱스</param>
    public void SetBossHPBarUI(string _type, UnitClass _class, int _indexCnt)
    {
        tmpBossType.text = _type;
        switch (_class)
        {
            case UnitClass.Warrior:
                imgBossClass[_indexCnt - 1].sprite = MgrInGameData.Instance.SoClassImage.sprWar;
                break;
            case UnitClass.Arch:
                imgBossClass[_indexCnt - 1].sprite = MgrInGameData.Instance.SoClassImage.sprArch;
                break;
            case UnitClass.Tank:
                imgBossClass[_indexCnt - 1].sprite = MgrInGameData.Instance.SoClassImage.sprTank;
                break;
            case UnitClass.Supporter:
                imgBossClass[_indexCnt - 1].sprite = MgrInGameData.Instance.SoClassImage.sprSup;
                break;
        }
    }
    
    /// <summary>
    /// 보스 체력바 비활성화
    /// </summary>
    public void DisabledBossHPBar()
    {
        foreach (var bossHp in objBossHPBack)
            bossHp.SetActive(false);
    }

    private bool isTestBackGround = false;
    public void ChangeBackGround()
    {
        isTestBackGround = !isTestBackGround;
        foreach (var backScroll in MgrCamera.Instance.ArrBgScroll)
            backScroll.TaskChangedBackGround(isTestBackGround).Forget();
    }

    /// <summary>
    /// 전투 종료 함수
    /// </summary>
    /// <param name="_isClear">클리어 여부</param>
    public void SetEndBattle(bool _isClear = false)
    {
        if (!isStageStart)
            return;

        imgAllyLowHP.DOKill();
        token_LowHP?.Cancel();

        if (_isClear)
        {
            // 전투를 완전 종료하며 종료 연출 실행
            isStageStart = false;
            TaskEndBattle(_delay: 1500).Forget();
        }
        else
        {
            // 클리어 실패 시, 부활 팝업 창 띄우기
            if(remainReviveCnt > 0)
            {
                isBlocked_ReviveBtn = false;

                DataManager.Instance.UserInventory.itemInventory.TryGetValue(Utiles.REVIVAL_COIN_ID, out int reviveItemCnt);

                Color color;
                ColorUtility.TryParseHtmlString(reviveItemCnt > 0 ? "#ffffff" : "#ff2500", out color);

                objRevivePop.SetActive(true);
                tmpReviveRemainCnt.text = $"{reviveItemCnt}/1";
                tmpReviveRemainCnt.color = color;
                Time.timeScale = 0.00001f;
                MgrSound.Instance.PauseAllSFX();

                token_RevivePop?.Cancel();
                token_RevivePop?.Dispose();
                token_RevivePop = new CancellationTokenSource();
                TaskRevivePopup().Forget();
            }
            else
            {
                isStageStart = false;
                TaskEndBattle(true, _delay: 1500).Forget();
            }
        }
    }

    private CancellationTokenSource token_RevivePop;
    private async UniTaskVoid TaskRevivePopup()
    {
        imgReviveCircle.rectTransform.DOKill();
        imgReviveCircle.rectTransform.localRotation = Quaternion.identity;
        //imgReviveCircle.rectTransform.DOLocalRotate(new Vector3(0.0f, 0.0f, -3960.0f), 11.0f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetUpdate(true);

        float rotValue = 0.0f;
        float remainTime = 11.0f;
        int prevTime = 10;
        while(remainTime > 0.0f)
        {
            if (!isBlocked_ReviveBtn)
            {
                remainTime -= Time.unscaledDeltaTime;
                rotValue += -360.0f * Time.unscaledDeltaTime;
                imgReviveCircle.rectTransform.localRotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, rotValue));
            }
            if(prevTime != (int)remainTime)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_Revive_Count_Down", 1.0f);
                tmpReviveTime.text = $"{(int)remainTime }";
                prevTime = (int)remainTime;
            }

            await UniTask.Yield(token_RevivePop.Token);
        }

        OnBtn_ExitRevive();
    }

    #region OnBtn 함수
    public void OnBtn_ToggleChallengeInfoPop(bool _isToggle)
    {
        objChallengeMedalDesc.SetActive(_isToggle);
        objChallengePressMedal.SetActive(_isToggle);
    }

    public void OnBtn_TogglePause()
    {
        if ((GameMode == GAME_MODE.Chapter && ChapterID == 0 && TutorialStep < 22) || !isStageStart)
            return;

        if (IsPause)
        {
            IsPause = false;
            Time.timeScale = 1.2f;
            MgrSound.Instance.UnPauseAllSFX();

            objPausePop.SetActive(false);

            MgrBoosterSystem.Instance.ToggleRootCurrentBoosterImage(MgrBoosterSystem.Instance.IsOpenBoosterCard);
        }
        else
        {
            IsPause = true;
            Time.timeScale = 0.00001f;
            MgrSound.Instance.PauseAllSFX();

            objPausePop.SetActive(true);

            MgrBoosterSystem.Instance.UpdateCurrentBooster();
            MgrBoosterSystem.Instance.ToggleRootCurrentBoosterImage(true);
        }
    }

    public void OnBtn_BattleSkip()
    {
        currWave = totalWave;
        KillCnt = 100;

        SetEndBattle(true);
    }

    public void OnBtn_LeaveGame()
    {
        objCanvLeaveConfirm.SetActive(true);
    }

    public void OnBtn_ConfirmLeaveGame()
    {
        //if (ChapterID == 0)
        //    return;

        objPausePop.SetActive(false);
        MgrBoosterSystem.Instance.rtCurrBoosterRoot.gameObject.SetActive(false);

        Time.timeScale = 1.2f;
        MgrSound.Instance.UnPauseAllSFX();

        if (!isStageStart)
            return;

        isStageStart = false;
        TaskEndBattle(true, true).Forget();
    }

    public void OnBtn_ExitConfirmLeave()
    {
        objCanvLeaveConfirm.SetActive(false);
    }

    public void OnBtn_ExitRevive()
    {
        token_RevivePop?.Cancel();

        objRevivePop.SetActive(false);

        Time.timeScale = 1.2f;
        MgrSound.Instance.UnPauseAllSFX();

        isStageStart = false;
        TaskEndBattle(true, _delay:1500).Forget();
    }

    public void OnBtn_AdRevive()
    {
        if (isBlocked_ReviveBtn)
            return;

        btn_AdOK.interactable = false;
        isBlocked_ReviveBtn = true;
#if UNITY_EDITOR
        //유니티에서 광고모듈 불러오기 안됨 바로 성공했을때 띄울 함수 실행
        Revive_Ad_Success();
#else
        var adsController = FindObjectOfType<AdsController>();
        if(adsController)
        {
            adsController.successAction = Revive_Ad_Success; //광고 성고했을 때 호출
            adsController.failedAction = Revive_Ad_Fail; //광고 실패했을 때 호출
            adsController.cancelAction = Revive_Ad_Fail; //광고 취소했을 때 호출
            adsController.ShowRewardedAds();
        }
        else
	    {
            if(SuperJam.GameManager.Instance == null)
                Revive_Ad_Success();
	    }
#endif
    }

    private void Revive_Ad_Success()
    {
        ReviveAllyBase(true).Forget();
    }

    private void Revive_Ad_Fail()
    {
        btn_AdOK.interactable = true;
        isBlocked_ReviveBtn = false;
    }

    public void OnBtn_AcceptRevive()
    {
        DataManager.Instance.UserInventory.itemInventory.TryGetValue("item_000006", out int reviveItemCnt);
        if (isBlocked_ReviveBtn || reviveItemCnt < 1)
            return;

        btn_TokenOK.interactable = false;
        isBlocked_ReviveBtn = true;
        ReviveAllyBase(false).Forget();
    }

    private bool isBlocked_ReviveBtn;
    private async UniTaskVoid ReviveAllyBase(bool _isAd)
    {
        ResultCode code = await DataManager.Instance.UseRevivalAsync(_isAd);
        switch (code)
        {
            case ResultCode.SUCCESS:
                break;
            case ResultCode.OUT_OF_PERIOD_REQUEST:
                if (!DataManager.Instance.UserInfo.hiveId.Equals("superjam"))
                {
                    btn_TokenOK.interactable = true;
                    isBlocked_ReviveBtn = false;
                    return;
                }
                break;
            default:
                btn_TokenOK.interactable = true;
                isBlocked_ReviveBtn = false;
                return;
        }

        token_RevivePop?.Cancel();

        Time.timeScale = 1.2f;
        MgrSound.Instance.UnPauseAllSFX();

        MgrSound.Instance.PlayOneShotSFX("SFX_Base_Revive", 1.0f);
        MgrSound.Instance.PlayOneShotSFX("SFX_Base_Revive_Shove", 1.0f);

        List<UnitBase> listUnitTemp = GetEnemyUnitList(GetAllyBase());
        foreach (UnitBase unit in listUnitTemp)
            unit.AddUnitEffect(UNIT_EFFECT.CC_SUPER_KONCKBACK, unit, unit, new float[] { 5.0f });

        objRevivePop.SetActive(false);
        remainReviveCnt--;

        MgrObjectPool.Instance.ShowObj("FX_Resurrection", GetAllyBase().GetUnitCenterPos());
        MgrObjectPool.Instance.ShowObj("FX_allyBase_skill_wind_Revive", GetAllyBase().GetUnitCenterPos() + Vector3.right * 3.48f);
        MgrObjectPool.Instance.ShowObj("FX_Energy_allyBase_Wind_Revive", GetAllyBase().transform.position + Vector3.up * 1.93f);

        GetAllyBase().UnitStat.HP = GetAllyBase().UnitStat.MaxHP;
        SetAllyHPBar(GetAllyBase(), GetAllyBase().UnitStat.HP, GetAllyBase().UnitStat.MaxHP, GetAllyBase().Shield);
        GetAllyBase().SetUnitState(UNIT_STATE.IDLE, true);

        UserGear gearCore = DataManager.Instance.GetUsingGearInfo(1);
        if (gearCore is not null && gearCore.gearId.Equals("gear_core_0000") && gearCore.gearRarity >= 1)
            GetAllyBase().AddUnitEffect(UNIT_EFFECT.ETC_GOD, GetAllyBase(), GetAllyBase(), new float[] { 2.0f + (gearCore.gearRarity >= 10 ? (float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 4) : 0.0f) });

        MgrBakingSystem.Instance.AddBakedFish(MgrBakingSystem.Instance.MaxBakedFish);

        ResetSideSkillCoolDown();

        foreach (var unitSlot in arrUnitSlotBtn)
            unitSlot.ResetCooldown();

        if (WeaponSys.SOWeaponData is not null)
        {
            WeaponSys.SetWeaponAnimation("idle", true);
            WeaponSys.ReSettingEvent();
        }
    }

    public void OnBtn_ResultBack()
    {
        if (isResultVFXPlayed)
            return;

        if (SuperJam.GameManager.Instance == null)
        {
            isResultVFXPlayed = true;
            UnityEngine.AddressableAssets.Addressables.LoadSceneAsync("98. TestLevelSelect");
            return;
        }

        if(SuperJam.GameManager.Instance.lastLoadScene.Equals("98. TestLevelSelect"))
            SuperJam.GameManager.Instance.LoadScene("TestSelect");
        else
            SuperJam.GameManager.Instance.LoadScene("Lobby");
    }

    private bool isToggleDebug;
    private readonly List<UnitBase> listDebugUnit = new List<UnitBase>();
    public void OnBtn_ToggleDebug()
    {
        if (!isToggleDebug)
        {
            isToggleDebug = true;

            objDebug.SetActive(true);

            int childCnt = rtUnitDebugContent.childCount;

            listDebugUnit.Clear();
            listDebugUnit.AddRange(GetEnemyUnitList(GetAllyBase(), true));
            listDebugUnit.Remove(GetAllyBase());

            if (childCnt < listDebugUnit.Count)
            {
                for (int i = 0; i < listDebugUnit.Count - childCnt; i++)
                    Instantiate(objContentUnitPrefab, rtUnitDebugContent);
            }
            else if (childCnt > listDebugUnit.Count)
            {
                for (int i = listDebugUnit.Count - 1; i < childCnt; i++)
                    rtUnitDebugContent.GetChild(i).gameObject.SetActive(false);
            }

            for (int i = 0; i < listDebugUnit.Count; i++)
            {
                rtUnitDebugContent.GetChild(i).GetComponent<Content_Unit>().SetContent(listDebugUnit[i]);
                rtUnitDebugContent.GetChild(i).gameObject.SetActive(true);
            }


            childCnt = rtEnemyDebugContent.childCount;

            listDebugUnit.Clear();
            listDebugUnit.AddRange(GetEnemyUnitList(GetAllyBase(), _isContainBlockedTarget:true));

            if (childCnt < listDebugUnit.Count)
            {
                for (int i = 0; i < listDebugUnit.Count - childCnt; i++)
                    Instantiate(objContentEnemyPrefab, rtEnemyDebugContent);
            }
            else if (childCnt > listDebugUnit.Count)
            {
                for (int i = listDebugUnit.Count - 1; i < childCnt; i++)
                    rtEnemyDebugContent.GetChild(i).gameObject.SetActive(false);
            }

            for (int i = 0; i < listDebugUnit.Count; i++)
            {
                rtEnemyDebugContent.GetChild(i).GetComponent<Content_Enemy>().SetContent(listDebugUnit[i]);
                rtEnemyDebugContent.GetChild(i).gameObject.SetActive(true);
            }
        }
        else
        {
            isToggleDebug = false;
            objDebug.SetActive(false);
        }
    }
    #endregion

    /// <summary>
    /// 소환한 유닛 수 표기 갱신 (현재 디버그 용)
    /// </summary>
    /// <param name="_index">유닛 인덱스</param>
    public void RefreshUnitSpawnCnt(string _index)
    {
        if (IsTestMode)
            return;

        foreach(UnitSlotBtn btn in arrUnitSlotBtn)
        {
            if (btn.CheckIsSameUnitIndex(_index))
                btn.RefreshCurrentUnitCnt();
        }
    }
    
    /// <summary>
    /// 유닛 소환 남은 쿨타임 감소
    /// </summary>
    /// <param name="_index">유닛 인덱스</param>
    /// <param name="_rate">비율</param>
    /// <param name="_isDelay">딜레이 여부</param>
    public void ReduceUnitSpawnCooldown(string _index, float _rate, bool _isDelay = false)
    {
        if (IsTestMode)
            return;

        foreach (UnitSlotBtn btn in arrUnitSlotBtn)
        {
            if (btn.CheckIsSameUnitIndex(_index))
            {
                if (_isDelay) TaskReduceCoolDown(btn, _rate).Forget();
                else btn.ReduceCoolDown(_rate);
            }
        }
    }
    private async UniTaskVoid TaskReduceCoolDown(UnitSlotBtn _btn, float _rate)
    {
        await UniTask.Delay(350, cancellationToken: this.GetCancellationTokenOnDestroy());
        _btn.ReduceCoolDown(_rate, true);
    }
    
    /// <summary>
    /// 모든 유닛 소환 쿨타임 초기화
    /// </summary>
    public void ResetAllUnitSpawnCooldown()
    {
        foreach(UnitSlotBtn btn in arrUnitSlotBtn)
            btn.ReduceCoolDown(1.0f);
    }

    private bool isResultAsync = false;
    private bool isResultVFXPlayed = false;
    private ResultObject<ChapterResult> resultObject;
    private readonly List<ProgressData> listGuideData = new List<ProgressData>();
    private readonly List<ProgressData> listMissionData = new List<ProgressData>();
    private readonly List<ProgressData> listAchievementData = new List<ProgressData>();
    private async UniTaskVoid TaskEndBattle(bool _isGameOver = false, bool _isLeave = false, int _delay = 0)
    {
        ChapterClearType type = GetClearType(_isGameOver, _isLeave);

        isResultAsync = false;

        int baseLv = DataManager.Instance.GetUserLv();

        float prevExp = baseLv > 1 ? (float)DataManager.Instance.GetAccountExpReqData(baseLv - 2).ReqAccAccountExp : 0.0f;
        float currExp = (DataManager.Instance.UserInventory.itemInventory.ContainsKey(Utiles.ACCOUNT_EXP_ID) ? (float)DataManager.Instance.UserInventory.itemInventory[Utiles.ACCOUNT_EXP_ID] : 0.0f);
        float maxExp = (float)DataManager.Instance.GetAccountExpReqData(baseLv - 1).ReqAccAccountExp;
        float currExpTotal = maxExp - prevExp;

        // 가이드 미션 진행도 갱신용 데이터 수집
        DataManager.Instance.UserInventory.itemInventory.TryGetValue(Utiles.GOLD_ID, out var previousGold);
        DataManager.Instance.UserInventory.itemInventory.TryGetValue(Utiles.FREE_DAIA_ID, out var previousFreeDaia);
        DataManager.Instance.UserInventory.itemInventory.TryGetValue(Utiles.DAIA_ID, out var previousDaia);

        SetProgressData(_isGameOver, _isLeave);

        if (!(_isGameOver && _isLeave)) TaskResultAsync(_isGameOver, type).Forget();
        else isResultAsync = true;

        // 감속 연출
        if (!_isGameOver)
        {
            Time.timeScale = 0.1f;
            MgrSound.Instance.PlayOneShotSFX("SFX_Final_Boss_Slow_Kill", 1.0f);
            await UniTask.Delay(2000, true, cancellationToken: this.GetCancellationTokenOnDestroy());
            Time.timeScale = 1.2f;
        }

        // 추가 딜레이 수치가 존재하면 그만큼 딜레이
        if (_delay > 0)
            await UniTask.Delay(_delay, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrSound.Instance.StopBGM(); // 임시

        // UI 빼버리는 연출
        TaskUIRemoved().Forget();

        await UniTask.Delay(500, cancellationToken: this.GetCancellationTokenOnDestroy());

        // 만약 클리어 판정이면 기지 날라가는 연출
        if (!_isGameOver)
        {
            GetAllyBase().Ska.AnimationState.SetAnimation(0, "fly_ready", false);
            GetAllyBase().Ska.AnimationState.Complete -= OnComplete;
            GetAllyBase().Ska.AnimationState.Complete += OnComplete;
            
            // 유닛 이동 연출
            List<UnitBase> listTempUnit = new List<UnitBase>();
            listTempUnit.AddRange(GetEnemyUnitList(GetAllyBase(), true));
            listTempUnit.Remove(GetAllyBase());
            foreach (UnitBase unit in listTempUnit)
                TaskUnitBattleEndMove(unit).Forget();
            
            await UniTask.Delay(1350, cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        isResultVFXPlayed = true;
        canvResult.gameObject.SetActive(true);

        if (!IsChallengeMode) Instantiate(objRoundResultPrefab[(int)type + 1], canvResult.transform).transform.localScale = Vector3.one * 100.0f;
        else Instantiate(_isGameOver ? objRoundResultPrefab[4] : objRoundResultPrefab[5], canvResult.transform).transform.localScale = Vector3.one * 100.0f;

        MgrSound.Instance.PlayOneShotSFX(_isGameOver ? "SFX_Battle_Defeat" : "SFX_Battle_Victory", 1.0f);

        await UniTask.Delay(500, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        if ((IsChallengeMode && !_isGameOver) || (!IsChallengeMode && type != ChapterClearType.NOT_CLEAR))
            MgrSound.Instance.PlayOneShotSFX("SFX_Clear_Star_Get_ab", 1.0f);

        await UniTask.Delay(1000, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        int minute = (int)PlayTime / 60;
        int seconds = (int)PlayTime - (minute * 60);
        tmpResultTime.text = $"{minute}분 {seconds}초";

        canvgPlayTime.alpha = 0.0f;
        canvgPlayTime.gameObject.SetActive(true);
        canvgPlayTime.DOFade(1.0f, 0.5f).SetEase(Ease.Linear);

        tmpUserInfo.text = $"Lv. {baseLv} {DataManager.Instance.UserInfo.name}";

        await UniTask.Delay(500, cancellationToken: this.GetCancellationTokenOnDestroy());

        await UniTask.WaitUntil(() => isResultAsync, cancellationToken:this.GetCancellationTokenOnDestroy());

        // 보상이 존재할 경우
        int rewardTotalCnt = 0;
        if (!(resultObject is null) && !(resultObject.data is null))
        {
            // 갯수에 따른 뒷배경 세로 사이즈 조정
            rewardTotalCnt = resultObject.data.mainMissionReceiveItems.Count + resultObject.data.basisReceiveItems.Count + resultObject.data.challengeReceiveItems.Count + resultObject.data.basisReceiveGears.Count;
            if (rewardTotalCnt > 8)
                rtRewardBack.sizeDelta = new Vector2(2020.0f, 360.0f);
        }

        objEmptyItem.SetActive(rewardTotalCnt == 0);
        objAccountExpUI.SetActive(!IsChallengeMode);

        tmpChangedEXP.text = string.Empty;
        imgSliderEXPBase.fillAmount = (currExp - prevExp) / currExpTotal;
        imgSliderEXPChanged.fillAmount = imgSliderEXPBase.fillAmount;

        // 결과 등장 연출
        objResultUserData.GetComponent<CanvasGroup>().alpha = 0.0f;
        objResultUserData.SetActive(true);
        objResultUserData.GetComponent<CanvasGroup>().DOFade(1.0f, 0.5f).SetEase(Ease.Linear);

        await UniTask.Delay(500, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        // 만약 획득 아이템이 존재하는 경우 아이템 획득 연출 함수 실행
        if (resultObject is not null && resultObject.data is not null)
        {
            await TaskChapterReward().AttachExternalCancellation(this.GetCancellationTokenOnDestroy());

            if (resultObject.data.basisReceiveItems.TryGetValue(Utiles.ACCOUNT_EXP_ID, out int addExp))
                await TaskExpAdd(baseLv, currExp, addExp).AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
        }

        // 터치하세요 연출
        canvgTouchNotice.DOFade(1.0f, 0.5f).SetEase(Ease.Linear).SetUpdate(true);

        await UniTask.Delay(2000, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        isResultVFXPlayed = false;
    }

    private void SetProgressData(bool _isGameOver, bool _isLeave)
    {
        // 가이드 미션
        listGuideData.Clear();
        listGuideData.Add(new ProgressData("guide_m_001007", KillCnt));
        listGuideData.Add(new ProgressData("guide_m_002007", KillCnt));
        listGuideData.Add(new ProgressData("guide_m_003007", KillCnt));
        listGuideData.Add(new ProgressData("guide_m_004007", KillCnt));
        listGuideData.Add(new ProgressData("guide_m_005007", KillCnt));
        listGuideData.Add(new ProgressData("guide_m_006006", KillCnt));
        listGuideData.Add(new ProgressData("guide_m_007004", KillCnt));
        listGuideData.Add(new ProgressData("guide_m_007005", KillCnt));
        if (!_isGameOver)
        {
            if (ChapterID == 2) listGuideData.Add(new ProgressData("guide_m_001001", 1));
            if (ChapterID == 4) listGuideData.Add(new ProgressData("guide_m_002001", 1));
            if (ChapterID == 5) listGuideData.Add(new ProgressData("guide_m_003001", 1));
            if (ChapterID == 6) listGuideData.Add(new ProgressData("guide_m_004001", 1));
            if (ChapterID == 7) listGuideData.Add(new ProgressData("guide_m_005001", 1));
            if (ChapterID == 8) listGuideData.Add(new ProgressData("guide_m_006001", 1));
            if (ChapterID == 9) listGuideData.Add(new ProgressData("guide_m_007001", 1));
            if (ChapterID == 3 && ChallengeLevel == 2) listGuideData.Add(new ProgressData("guide_m_004005", 1));
            if (ChapterID == 5 && ChallengeLevel == 2) listGuideData.Add(new ProgressData("guide_m_005005", 1));
            if (ChapterID == 7 && ChallengeLevel == 2) listGuideData.Add(new ProgressData("guide_m_006004", 1));
        }

        // 일반 미션
        listMissionData.Clear();
        listMissionData.Add(new ProgressData("mission_w_00", KillCnt));
        listMissionData.Add(new ProgressData("mission_w_01", AnyBossKillCnt));

        // 업적
        listAchievementData.Clear();
        if (!_isGameOver)
        {
            // 업적 메인 챕터 클리어 갱신 판정
            DataManager.Instance.UserAchievement.achievementList.TryGetValue("achievement_00004", out var userAchievementProgressData);
            var userProgressData = userAchievementProgressData?.progress ?? 0;
            if (ChapterID > userProgressData)
                listAchievementData.Add(new ProgressData("achievement_00004", ChapterID - userProgressData));
        }
        listAchievementData.Add(new ProgressData("achievement_00006", NormalMonsterKillCnt));
        listAchievementData.Add(new ProgressData("achievement_00007", EnemyUnitKillCnt));
        listAchievementData.Add(new ProgressData("achievement_00008", EliteKillCnt));
        listAchievementData.Add(new ProgressData("achievement_00009", AnyBossKillCnt));
    }

    private async UniTaskVoid TaskUnitBattleEndMove(UnitBase _unit)
    {
        _unit.SetUnitAnimation(_unit.animMoveName, true);
        _unit.UnitStat.MoveSpeed *= 3.0f;
        
        Transform tf = _unit.transform;
        while (_unit != null)
        {
            _unit.UnitSetting.moveSO.Move(_unit);
            tf.position = new Vector3(tf.position.x, tf.position.y, tf.position.y * 0.01f);
            await UniTask.Yield(this.GetCancellationTokenOnDestroy());
        }
    }

    private async UniTaskVoid TaskResultAsync(bool _isGameOver, ChapterClearType _type)
    {
        resultObject = await DataManager.Instance.SetChapterResultAsync($"Chapter_{100000 + ChapterID}", _type, IsChallengeMode, _isGameOver ? currWave : totalWave, KillCnt, MgrBakingSystem.Instance.ArrCurrBakedFishRank.ToList(), MgrBakingSystem.Instance.S_Combo, listGuideData, listMissionData, listAchievementData, MgrBoosterSystem.Instance.SelectBoosterGoldCnt, MgrBakingSystem.Instance.S_MaxCombo);
        isResultAsync = true;
    }

    private void OnComplete(Spine.TrackEntry trackEntry)
    {
        string animationName = trackEntry.Animation.Name;
        if (animationName.Equals("fly_ready"))
        {
            MgrSound.Instance.PlayOneShotSFX("SFX_Base_Victory", 1.0f);
            GetAllyBase().Ska.AnimationState.SetAnimation(0, "fly_move", true);
            GetAllyBase().Ska.AnimationState.Complete -= OnComplete;
            GetAllyBase().transform.DOMoveX(GetAllyBase().transform.position.x + 40.0f, 2.0f).SetEase(Ease.InCubic);
        }
    }

    /// <summary>
    /// 챕터 결과 종류 반환 함수
    /// </summary>
    /// <param name="_isGameOver">게임 오버 여부</param>
    /// <param name="_isLeave">나가기 여부</param>
    /// <returns></returns>
    private ChapterClearType GetClearType(bool _isGameOver, bool _isLeave)
    {
        ChapterClearType clearType = ChapterClearType.NOT_CLEAR;

        if (_isGameOver && _isLeave)
            return clearType;

        if (IsChallengeMode)
        {
            if (_isGameOver) clearType = ChapterClearType.NOT_CLEAR;
            else
            {
                if(ChallengeLevel == 0) clearType = ChapterClearType.ONE_STAR_CLEAR;
                else if(ChallengeLevel == 1) clearType = ChapterClearType.TWO_STAR_CLEAR;
                else if(ChallengeLevel == 2) clearType = ChapterClearType.THREE_STAR_CLEAR;
            }
        }
        else
        {
            if (_isGameOver)
            {
                int starCnt = 0;
                if (ChapterID % 3 == 1)
                {
                    if (currWave > 5) starCnt++;
                    if (midBossKillCnt >= 1) starCnt++;
                }
                if (ChapterID % 3 == 2)
                {
                    if (currWave > 10) starCnt++;
                    if (midBossKillCnt >= 2) starCnt++;
                }
                if (ChapterID % 3 == 0)
                {
                    if (currWave > 15) starCnt++;
                    if (midBossKillCnt >= 2) starCnt++;
                }

                if (starCnt == 2) clearType = ChapterClearType.TWO_STAR_CLEAR;
                else if (starCnt == 1) clearType = ChapterClearType.ONE_STAR_CLEAR;
                else clearType = ChapterClearType.NOT_CLEAR;
            }
            else clearType = ChapterClearType.THREE_STAR_CLEAR;
        }

        return clearType;
    }

    private async UniTask TaskChapterReward()
    {
        srReward.verticalNormalizedPosition = 1.0f;

        InventoryItem icon;
        for (int i = 0; i < resultObject.data.mainMissionReceiveItems.Count; i++)
        {
            await UniTask.Delay(150, true, cancellationToken: this.GetCancellationTokenOnDestroy());
            icon = Instantiate(objRewardIconPrefab, tfRewardContent).GetComponent<InventoryItem>();
            GearInfo info = new GearInfo();
            info.Id = resultObject.data.mainMissionReceiveItems[i].item_id;
            info.Count = resultObject.data.mainMissionReceiveItems[i].quantity;
            info.ReceiveItemType = ReceiveItemType.Item;
            icon.SetInfo(info, GearInventoryType.Popup);
            icon.SetClickInfoEvent(ShowItemInfo);
            icon.transform.localScale = Vector3.zero;
            icon.transform.DOScale(1.0f, 0.25f).SetEase(Ease.OutBack);

            GameObject objStar = Instantiate(objRewardMainStarPrefab, icon.GetComponent<RectTransform>());
            for (int x = 0; x < 3; x++)
                objStar.transform.GetChild(x).gameObject.SetActive(x <= resultObject.data.mainMissionReceiveItems[i].step);

            MgrSound.Instance.PlayOneShotSFX("SFX_Reward_Get", 1.0f);

            if (srReward.verticalNormalizedPosition > 0.0f)
            {
                srReward.DOKill();
                srReward.DOVerticalNormalizedPos(0.0f, 0.25f).SetEase(Ease.Linear);
            }
        }

        List<string> dicKey = new List<string>(resultObject.data.basisReceiveItems.Keys);
        for (int i = 0; i < resultObject.data.basisReceiveItems.Count; i++)
        {
            await UniTask.Delay(150, true, cancellationToken: this.GetCancellationTokenOnDestroy());
            icon = Instantiate(objRewardIconPrefab, tfRewardContent).GetComponent<InventoryItem>();
            GearInfo info = new GearInfo();
            info.Id = dicKey[i];
            info.Count = resultObject.data.basisReceiveItems[dicKey[i]];
            info.ReceiveItemType = ReceiveItemType.Item;
            icon.SetInfo(info, GearInventoryType.Popup);
            icon.SetClickInfoEvent(ShowItemInfo);
            icon.transform.localScale = Vector3.zero;
            icon.transform.DOScale(1.0f, 0.25f).SetEase(Ease.OutBack);

            MgrSound.Instance.PlayOneShotSFX("SFX_Reward_Get", 1.0f);

            if (srReward.verticalNormalizedPosition > 0.0f)
            {
                srReward.DOKill();
                srReward.DOVerticalNormalizedPos(0.0f, 0.25f).SetEase(Ease.Linear);
            }
        }

        dicKey = new List<string>(resultObject.data.challengeReceiveItems.Keys);
        for (int i = 0; i < resultObject.data.challengeReceiveItems.Count; i++)
        {
            await UniTask.Delay(150, true, cancellationToken: this.GetCancellationTokenOnDestroy());
            icon = Instantiate(objRewardIconPrefab, tfRewardContent).GetComponent<InventoryItem>();
            GearInfo info = new GearInfo();
            info.Id = dicKey[i];
            info.Count = resultObject.data.challengeReceiveItems[dicKey[i]];
            info.ReceiveItemType = ReceiveItemType.Item;
            icon.SetInfo(info, GearInventoryType.Popup);
            icon.SetClickInfoEvent(ShowItemInfo);
            icon.transform.localScale = Vector3.zero;
            icon.transform.DOScale(1.0f, 0.25f).SetEase(Ease.OutBack);

            MgrSound.Instance.PlayOneShotSFX("SFX_Reward_Get", 1.0f);

            if (srReward.verticalNormalizedPosition > 0.0f)
            {
                srReward.DOKill();
                srReward.DOVerticalNormalizedPos(0.0f, 0.25f).SetEase(Ease.Linear);
            }
        }

        dicKey = new List<string>(resultObject.data.basisReceiveGears.Keys);
        for (int i = 0; i < resultObject.data.basisReceiveGears.Count; i++)
        {
            await UniTask.Delay(150, true, cancellationToken: this.GetCancellationTokenOnDestroy());
            icon = Instantiate(objRewardIconPrefab, tfRewardContent).GetComponent<InventoryItem>();
            GearInfo info = new GearInfo();
            info.Id = resultObject.data.basisReceiveGears[dicKey[i]].gearId;
            info.Count = 1;
            info.ReceiveItemType = ReceiveItemType.Gear;

            var gearData = DataManager.Instance.GetGearData(info.Id);

            info.Parts = gearData.Parts;

            var status = gearData.GearStatus;
            info.GearStatus = new ArrayItemStringFloat[status.Count];
            for (int x = 0; x < status.Count; x++)
            {
                info.GearStatus[x] = new ArrayItemStringFloat();
                info.GearStatus[x].Type = status[x].Type;
                info.GearStatus[x].Value = (float)status[x].Value;
            }

            var options = gearData.GearOption;
            info.GearOptions = new ArrayItemStringFloat[options.Count];
            for (int x = 0; x < options.Count; x++)
            {
                info.GearOptions[x] = new ArrayItemStringFloat();
                info.GearOptions[x].Type = options[x].Type;
                info.GearOptions[x].Value = (float)options[x].Value;
            }

            icon.SetInfo(info, GearInventoryType.Popup);
            icon.SetClickInfoEvent(ShowItemInfo);
            icon.transform.localScale = Vector3.zero;
            icon.transform.DOScale(1.0f, 0.25f).SetEase(Ease.OutBack);

            MgrSound.Instance.PlayOneShotSFX("SFX_Reward_Get", 1.0f);

            if (srReward.verticalNormalizedPosition > 0.0f)
            {
                srReward.DOKill();
                srReward.DOVerticalNormalizedPos(0.0f, 0.25f).SetEase(Ease.Linear);
            }
        }

        await UniTask.Delay(250, true, cancellationToken: this.GetCancellationTokenOnDestroy());
        if (srReward.verticalNormalizedPosition > 0.0f)
        {
            srReward.DOKill();
            srReward.DOVerticalNormalizedPos(0.0f, 0.25f).SetEase(Ease.Linear);
        }
    }

    private void ShowItemInfo(GearInfo info)
    {
        if (info.ReceiveItemType == ReceiveItemType.Gear)
        {
            popItemInfo.Show();
            popItemInfo.SetInfo(info);
            popItemInfo.HideButton();
        }
        else
        {
            popRewardItemInfo.Show();
            popRewardItemInfo.SetInfo(info.Id);
        }
    }

    private int getExpSFXChannel = -1;
    private async UniTask TaskExpAdd(int _baseLv, float _currExp, float _addExp)
    {
        getExpSFXChannel = MgrSound.Instance.PlaySFX("SFX_EXP_Up", 1.0f, true);

        float prevExp = _baseLv > 1 ? (float)DataManager.Instance.GetAccountExpReqData(_baseLv - 2).ReqAccAccountExp : 0.0f;
        float startExpValue = _currExp; // 현재 경험치 수치
        float endExpValue = DataManager.Instance.GetAccountExpReqData(_baseLv - 1).ReqAccAccountExp; // 현재 경험치 MAX 수치

        if (_addExp >= (endExpValue - startExpValue))
        {
            float startValue = imgSliderEXPBase.fillAmount; // 슬라이더 연출 시작 위치값
            float endValue = 1.0f; // 현재 레벨 슬라이더 최종 위치 값

            float remainTotalExpValue = _addExp - (endExpValue - startExpValue); // 남은 총 EXP 증가량

            float currMaxDuration = Mathf.Lerp(0.0f, 2.0f, (endExpValue - startExpValue) / _addExp); // 시간 계산
            float duration = currMaxDuration;
            float spendDuration = 0.0f; // 경험치 증가량 연출용 시간 변수

            int currLvUp = 0;

            // 연출 시간이 남았거나 증가할 경험치가 남았을 경우 계속 루프
            while (remainTotalExpValue > 0 || duration > 0.0f)
            {
                // 연출 시간이 남았을 경우
                while(duration > 0.0f)
                {
                    duration -= Time.unscaledDeltaTime; // 연출 남은 시간 감소
                    spendDuration += Time.unscaledDeltaTime; // 경험치 증가량 연출 시간 증가

                    imgSliderEXPChanged.fillAmount = Mathf.Lerp(startValue, endValue, (currMaxDuration - duration) / currMaxDuration); // 경험치 증가 슬라이더 연출
                    tmpChangedEXP.text = $"+ {(int)Mathf.Lerp(0.0f, _addExp, spendDuration / 2.0f)}"; // 경험치 증가 TEXT 연출

                    await UniTask.Yield(this.GetCancellationTokenOnDestroy());
                }

                // 연출 시간이 끝났고 경험치가 100% 다 찬 상태인 경우
                if(imgSliderEXPChanged.fillAmount == 1.0f)
                {
                    currLvUp++; // 레벨 업

                    tmpUserInfo.text = $"Lv. {_baseLv + currLvUp} {DataManager.Instance.UserInfo.name}"; // 계정 레벨 UP 연출

                    // 슬라이더 위치 0으로 초기화
                    imgSliderEXPBase.fillAmount = 0.0f;
                    imgSliderEXPChanged.fillAmount = 0.0f;
                }

                // EXP 증가량이 아직 남아있을 경우
                if (remainTotalExpValue > 0.0f)
                {
                    startExpValue = endExpValue; // 시작 EXP 수치를 현재 연출이 끝난 수치로 설정
                    endExpValue = (float)DataManager.Instance.GetAccountExpReqData(_baseLv - 1 + currLvUp).ReqAccAccountExp; // 종료 EXP 수치를 다음 계정 레벨의 경험치 요구량 세팅

                    // 남은 EXP 증가량이 다음 계정 레벨 경험치 요구량 보다 크거나 같을 경우
                    if (remainTotalExpValue >= (endExpValue - startExpValue))
                    {
                        remainTotalExpValue -= (endExpValue - startExpValue); // 남은 EXP 증가량에서 해당 경험치 요구량 감소
                        currMaxDuration = Mathf.Lerp(0.0f, 2.0f, (endExpValue - startExpValue) / _addExp); // 해당 경험치 요구량에 대한 연출 시간 계산
                        startValue = 0.0f; // 시작 지점 슬라이더 세팅
                        endValue = 1.0f; // 종료 지점 슬라이더 세팅
                    }
                    else
                    {
                        currMaxDuration = Mathf.Lerp(0.0f, 2.0f, remainTotalExpValue / _addExp); // 남은 EXP 증가량에 대한 연출 시간 계산
                        startValue = 0.0f; // 시작 지점 슬라이더 세팅
                        endValue = remainTotalExpValue / (endExpValue - startExpValue); // 현재 경험치 요구량 기준 남은 EXP 증가량 비율을 계산하여 종료 지점 슬라이더 세팅
                        remainTotalExpValue = 0.0f; // 남은 경험치가 존재하지 않으니 0으로 세팅
                    }
                    duration = currMaxDuration; // 연출 시간 세팅
                }
            }

            //레벨업 후 최대치보다 현재 스태미너의 양이 많아지면 로컬푸시를 해제해야 한다
            var maxStaminaUp = DataManager.Instance.GetUserMaxStaminaUp();
            var userStamina = DataManager.Instance.UserInventory.itemInventory[Utiles.STAMINA_ID];

            // 스태미나가 스태미나 최대치보다 작을 경우 자연 충전 계산
            if (userStamina >= Utiles.MAX_STAMINA + maxStaminaUp)
            {
                LocalPushController.Instance.UnRegisterLocalPush(1);
            }

            popLvUp.SetOpenItemInfoEvent(ShowItemInfo);
            popLvUp.SetLevelUpEffectSound(MgrSound.Instance.PlayOneShotSFX);
            popLvUp.SetPrevLevel(_baseLv);
            popLvUp.SetCurrentLevel(_baseLv + currLvUp);
            popLvUp.Builder().AddReceivedItem(resultObject.data.lvupReceiveItems).Build();
            popLvUp.ShowAnimation();
        }
        else
        {
            float duration = 2.0f;
            while (duration > 0.0f)
            {
                duration -= Time.unscaledDeltaTime;

                imgSliderEXPChanged.fillAmount = Mathf.Lerp(imgSliderEXPBase.fillAmount, ((startExpValue - prevExp) + _addExp) / (endExpValue - prevExp), (2.0f - duration) / 2.0f);
                tmpChangedEXP.text = $"+ {(int)Mathf.Lerp(0.0f, _addExp, (2.0f - duration) / 2.0f)}";

                await UniTask.Yield(this.GetCancellationTokenOnDestroy());
            }
        }

        MgrSound.Instance.StopSFX("SFX_EXP_Up", getExpSFXChannel);
    }

    [Header("타이쿤 판 결과창 연출")]
    [SerializeField] private RectTransform rtTrayNormal;
    [SerializeField] private RectTransform rtTrayAuto;

    [Header("보스 체력바 결과창 연출")]
    [SerializeField] private RectTransform rtBossHP;
    [SerializeField] private RectTransform rtTwinBossHP;
    private async UniTaskVoid TaskUIRemoved()
    {
        foreach (var rt in arrRtShowEffect_Left)
            rt.DOAnchorPosX(rt.anchoredPosition.x - 700.0f, 1.0f).SetEase(Ease.OutBack, 0.75f);

        foreach (var unitSlot in arrUnitSlotBtn)
            unitSlot.InitHideEffect();

        foreach (var rt in arrRtShowEffect_Bottom)
            rt.DOAnchorPosY(rt.anchoredPosition.y - 600.0f, 0.75f).SetEase(Ease.OutBack, 0.75f);

        foreach (var rt in arrRtShowEffect_Top)
            rt.DOAnchorPosY(rt.anchoredPosition.y + 500.0f, 1.0f).SetEase(Ease.OutBack, 0.75f);

        rtTrayNormal.DOKill();
        rtTrayNormal.DOAnchorPosX(1000.0f, 1.0f).SetEase(Ease.OutBack, 0.75f);

        rtTrayAuto.DOKill();
        rtTrayAuto.DOAnchorPosY(-600.0f, 1.0f).SetEase(Ease.OutBack, 0.75f);

        rtTwinBossHP.DOAnchorPosY(rtTwinBossHP.anchoredPosition.y + 500.0f, 1.0f).SetEase(Ease.OutBack, 0.75f);
        rtBossHP.DOAnchorPosY(rtBossHP.anchoredPosition.y + 500.0f, 1.0f).SetEase(Ease.OutBack, 0.75f);

        imgChallengeMedalBack.rectTransform.DOKill();
        imgChallengeMedalBack.rectTransform.DOAnchorPosY(415.0f, 1.0f).SetEase(Ease.OutBack, 0.75f);

        await UniTask.Delay(100, cancellationToken: this.GetCancellationTokenOnDestroy());
    }

    #region 웨이브 전투 진행
    private void SetWaveText()
    {
        List<EnemySpawn> listEnemySpawn = DataManager.Instance.GetEnemySpawnDatas();

        int totalCnt = 0;

        string spawnMode = "Chapter";
        if (GameMode == GAME_MODE.GoldMode) spawnMode = "GoldMode";
        if (GameMode == GAME_MODE.Training) spawnMode = "Training";
        if (GameMode == GAME_MODE.Survival) spawnMode = "Survival";
        if (GameMode == GAME_MODE.Farm) spawnMode = "Farm";

        foreach (EnemySpawn spawn in listEnemySpawn)
        {
            if (spawn.ChapterId != $"{spawnMode}_{100000 + ChapterID}")
                continue;

            totalCnt++;

            foreach (var spawnOrder in spawn.Orders)
            {
                if (!spawnOrder.EnemyId.Contains("Unit_Mnstr") && !spawnOrder.EnemyId.Contains("Elite_Mnstr"))
                    continue;

                string unitIndex = spawnOrder.EnemyId.Split('$')[1];
                if(!listCheckDuplicateUnitIndex.Contains(unitIndex))
                    listCheckDuplicateUnitIndex.Add(unitIndex);
            }
        }

        totalWave = totalCnt;

        tmpWave.text = $"Wave {currWave}/{totalWave}";
    }

    public void SetTutorialInitWave() => waveLimitTimer = 0.5f;
    public void SetReserveSpawnCount(int _cnt) => ReserveSpawnCnt = _cnt;

    public readonly List<UnitBase> ListChallengeUnit = new List<UnitBase>();
    public readonly List<UnitBase> ListChallengeHitUnit = new List<UnitBase>();
    public async UniTaskVoid TaskWaveSystem()
    {
        // 소환 시간 세팅
        waveLimitTimer = SOModeData.SetSpawnTime(enemySpawnData is null);
        
        // if (enemySpawnData is null)
        // {
        //     waveLimitTimer = SOModeData.GetFirstWaveWaitTime();
        //     
        //     // if (GameMode == GAME_MODE.Chapter && ChapterID == 0) waveLimitTimer = 999999.0f;
        //     // else if (GameMode is GAME_MODE.Pvp or GAME_MODE.Training or GAME_MODE.Survival or GAME_MODE.Farm) waveLimitTimer = 5.0f;
        //     // else waveLimitTimer = 20.0f;
        // }
        // else
        // {
        //     if (GameMode == GAME_MODE.Pvp) waveLimitTimer = 20.0f;
        //     else waveLimitTimer = (float)enemySpawnData.SpawnTime;
        // }

        while(waveLimitTimer > 0.0f && isStageStart)
        {
            await UniTask.Yield(this.GetCancellationTokenOnDestroy());

            PlayTime += Time.unscaledDeltaTime;
            waveLimitTimer -= Time.deltaTime;
            
            // 도전 모드 효과
            SOModeData.DoChallengeTask();

            if(IsBossAppeared)
            {
                // 보스 상태이면 광폭화 체크
                SOModeData.DoBossAppearedTask();
                
                // if (!IsBossAngry && CheckIsBossAlive())
                // {
                //     bossAngryTimer -= Time.deltaTime;
                //
                //     if(tmpAngryRemain is not null)
                //         tmpAngryRemain.text = bossAngryTimer <= 30.0f ? $"광폭화까지 남은 시간 {bossAngryTimer:F0} s" : string.Empty;
                //
                //     if (bossAngryTimer < 0.0f && GameMode != GAME_MODE.Pvp)
                //     {
                //         MgrSound.Instance.PlayOneShotSFX("SFX_Final_Boss_Berserker", 1.0f);
                //
                //         IsBossAngry = true;
                //         objAngryBackImg.SetActive(true);
                //         objAngryIcon.SetActive(true);
                //         objAngryIcon.transform.GetChild(0).gameObject.SetActive(currWave != totalWave);
                //         objAngryIcon.transform.GetChild(1).gameObject.SetActive(currWave == totalWave);
                //         if (tmpAngryRemain != null)
                //         {
                //             tmpAngryRemain.color = Color.yellow;
                //             tmpAngryRemain.text = $"보스 광폭화 : 공격력 100% 증가";
                //         }
                //
                //         foreach (var unit in listUnitBoss)
                //         {
                //             if (unit.CheckIsState(UNIT_STATE.DEATH))
                //                 continue;
                //
                //             SetBossAngry(unit);
                //         }
                //
                //         foreach (var unit in ListUnitBase)
                //         {
                //             if (unit.UnitSetting.unitType != UnitType.Boss && unit.UnitSetting.unitType != UnitType.MidBoss)
                //                 continue;
                //
                //             SetBossAngry(unit);
                //         }
                //     }
                // }
            }
        }

        if (!isStageStart)
            return;

        // 모드 별 웨이브 증가 판정
        if (!SOModeData.CheckIsCanNextWave())
            return;
        
        // if(!IsBossAppeared) currWave++;
        // else
        // {
        //     if(!CheckIsBossAlive())
        //     {
        //         TaskWaveSystem().Forget();
        //         if(GameMode != GAME_MODE.GoldMode && GameMode != GAME_MODE.Survival)
        //             SetNextWave();
        //         return;
        //     }
        // }
        
        // 다음 웨이브가 총 웨이브 수 보다 커졌을 경우 클리어 판정
        if (currWave > totalWave)
        {
            ListChallengeUnit.Clear();
            ListChallengeUnit.AddRange(GetEnemyUnitList(GetAllyBase()));
            foreach (UnitBase unit in ListChallengeUnit)
                unit.OnDefaultDeath(GetAllyBase(), unit, -1);

            WeaponSys.SetCoolDown(999999.0f);

            SetEndBattle(true);
            return;
        }

        // 웨이브 유닛 소환
        SpawnWaveUnit();

        // 장비 효과
        UserGear gearCore = DataManager.Instance.GetUsingGearInfo(1);
        if (gearCore is not null && gearCore.gearId.Equals("gear_core_0002") && gearCore.gearRarity >= 3)
        {
            GetAllyBase().AddUnitEffect(UNIT_EFFECT.BUFF_SHIELD, GetAllyBase(), GetAllyBase(), new float[] { (float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 0) * GetAllyBase().UnitStat.MaxHP, 0.0f });
            MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Core_0002", 1.0f);
        }

        // 웨이브 텍스트 세팅 및 함수 반복 (재귀)
        tmpWave.text = $"Wave {currWave}/{totalWave}";
        TaskWaveSystem().Forget();
    }

    public void AddCurrWave() => currWave++;

    public bool CheckIsBossAlive()
    {
        foreach (var unit in listUnitBoss)
            if (!unit.CheckIsState(UNIT_STATE.DEATH))
                return true;

        return false;
    }

    /// <summary>
    /// 보스 광폭화 안내 텍스트 세팅
    /// </summary>
    /// <param name="_time">시간</param>
    public void SetBossAngryRemainTime(float _time)
    {
        if(tmpAngryRemain is not null)
            tmpAngryRemain.text = _time <= 30.0f ? $"광폭화까지 남은 시간 {_time:F0} s" : string.Empty;
    }

    /// <summary>
    /// 보스 광폭화 UI 세팅
    /// </summary>
    public void SetBossAngry()
    {
        MgrSound.Instance.PlayOneShotSFX("SFX_Final_Boss_Berserker", 1.0f);

        IsBossAngry = true;
        objAngryBackImg.SetActive(true);
        objAngryIcon.SetActive(true);
        objAngryIcon.transform.GetChild(0).gameObject.SetActive(currWave != totalWave);
        objAngryIcon.transform.GetChild(1).gameObject.SetActive(currWave == totalWave);
        if (tmpAngryRemain != null)
        {
            tmpAngryRemain.color = Color.yellow;
            tmpAngryRemain.text = $"보스 광폭화 : 공격력 100% 증가";
        }
        
        foreach (var unit in listUnitBoss)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH))
                continue;

            SetUnitAngry(unit);
        }

        foreach (var unit in ListUnitBase)
        {
            if (unit.UnitSetting.unitType != UnitType.Boss && unit.UnitSetting.unitType != UnitType.MidBoss)
                continue;

            SetUnitAngry(unit);
        }
    }

    /// <summary>
    /// 보스 광폭화 세팅
    /// </summary>
    /// <param name="_target">타겟 유닛</param>
    public void SetUnitAngry(UnitBase _target)
    {
        if (_target.ObjEliteVFX != null)
            return;

        _target.ObjEliteVFX = MgrObjectPool.Instance.ShowObj("FX_Elite Aura", _target.GetUnitCenterPos());
        _target.ObjEliteVFX.transform.localScale = _target.UnitSetting.unitType == UnitType.Boss ? Vector3.one * 1.5f : Vector3.one;
        _target.ObjEliteVFX.transform.SetParent(_target.transform);

        ColorUtility.TryParseHtmlString("#E2C8C8", out var color);
        ColorUtility.TryParseHtmlString("#3A1E1E", out var black);

        MaterialPropertyBlock mpbNormal = new MaterialPropertyBlock();
        mpbNormal.SetColor("_Color", color);
        mpbNormal.SetColor("_Black", black);
        _target.Meshrd.SetPropertyBlock(mpbNormal);
    }

    /// <summary>
    /// 다음 웨이브 세팅
    /// </summary>
    public void SetNextWave()
    {
        if (currWave < totalWave)
        {
            DisabledBossHPBar();
            tmpWave.gameObject.SetActive(true);
            MgrBoosterSystem.Instance.objBoosterGauge.SetActive(true);

            if(IsBossAppeared)
                midBossKillCnt++;
        }
        listUnitBoss.Clear();
        IsBossAppeared = false;
        IsBossAngry = false;
        waveLimitTimer = 0.0f;
    }

    /// <summary>
    /// 보스 체력바 표기 -> 부스터 게이지 표기로 변경
    /// </summary>
    public void SwitchBossHPBarToBoosterGauge()
    {
        foreach (var unit in listUnitBoss)
        {
            if (!unit.CheckIsState(UNIT_STATE.DEATH))
                return;
        }

        DisabledBossHPBar();
        tmpWave.gameObject.SetActive(true);
        MgrBoosterSystem.Instance.objBoosterGauge.SetActive(true);
    }

    private async UniTaskVoid TaskSurvivalSpawn()
    {
        float spawnTimer = 0.0f;
        while (isStageStart)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer < 0.0f)
            {
                List<string> listRandomBoss = new List<string>();
                UnitData monsterData = MgrInGameData.Instance.SOMonsterData;
                foreach (var unitData in monsterData.unitSetting)
                {
                    if (!unitData.isActivate || unitData.unitType != UnitType.Monster)
                        continue;

                    listRandomBoss.Add(unitData.unitIndex);
                }

                int spawnCnt = 1 + (int)((60.0f - ModeTimer) / 8.0f);
                ReserveSpawnCnt += spawnCnt;
                TaskSpawnEnemy(listRandomBoss[Random.Range(0, listRandomBoss.Count)], spawnCnt, 0.0f, false, 0).Forget();
                
                spawnTimer = 3.0f;
            }

            await UniTask.Yield(this.GetCancellationTokenOnDestroy());
        }
    }
     
    /// <summary>
    /// 웨이브 유닛 소환
    /// </summary>
    public void SpawnWaveUnit()
    {
        if (IsTestMode)
            return;

        string spawnMode = "Chapter";
        if (GameMode == GAME_MODE.GoldMode) spawnMode = "GoldMode";
        if (GameMode == GAME_MODE.Training) spawnMode = "Training";
        if (GameMode == GAME_MODE.Survival) spawnMode = "Survival";
        if (GameMode == GAME_MODE.Farm) spawnMode = "Farm";
        enemySpawnData = DataManager.Instance.GetEnemySpawnData($"{spawnMode}_{100000 + ChapterID}_{currWave}");

        // 마지막 웨이브 보스 출현 연출 (모드 마다 다르게 할 경우 분리 필요)
        if(currWave == totalWave && !IsBossAppeared)
        {
            MgrSound.Instance.StartBGM(clipBossBGM, 1.0f);
            MgrSound.Instance.PlayOneShotSFX("SFX_Final_Boss_Appear", 1.0f);

            skgBossWarning.AnimationState.SetAnimation(0, "idle", false);
            skgBossWarning.gameObject.SetActive(true);
            TaskDelayWarning().Forget();
        }

        // if(GameMode == GAME_MODE.GoldMode)
        // {
        //     if(!IsBossAppeared)
        //     {
        //         TaskGoldModeSpawn().Forget();
        //         IsBossAppeared = true;
        //         
        //         modeTimer = 10000.0f;
        //         GoldCollectAmount = 0.0f;
        //         objTimerSlider.SetActive(true);
        //         imgTimerSlider.fillAmount = 0.0f;
        //         TaskGoldModeTimer().Forget();
        //     }
        // }
        // else if(GameMode == GAME_MODE.Training)
        // {
        //     if(!IsBossAppeared)
        //     {
        //         ReserveSpawnCnt += 1;
        //
        //         TaskSpawnEnemy("C1_Mid_Boss00_a", 1, 0.0f, true, 1).Forget();
        //
        //         IsBossAppeared = true;
        //     }
        // }
        // else if(GameMode == GAME_MODE.Pvp)
        // {
        //     if(!IsBossAppeared)
        //     {
        //         oppoAlly = MgrUnitPool.Instance.ShowObj("Catbot_001", 1, new Vector3(unitAllyBase.transform.position.x + 90.0f, 0.0f, 0.0f)).GetComponent<UnitBase>();
        //         listUnitBoss.Add(oppoAlly);
        //
        //         IsBossAppeared = true;
        //
        //         TaskEnemyPVPAI().Forget();
        //     }
        // }
        // else if(GameMode == GAME_MODE.Survival)
        // {
        //     if(!IsBossAppeared)
        //     {
        //         IsBossAppeared = true;
        //         modeTimer = 65.0f;
        //         objTimerSlider.SetActive(true);
        //         imgTimerSlider.fillAmount = 1.0f;
        //
        //         TaskSurvivalSpawn().Forget();
        //         TaskSurvivalTimer().Forget();
        //     }
        // }
        // else
        // {
        //     if(IsChallengeMode && ChallengeLevel == 2 && GetCurrentThema() == 4 && !IsBossAppeared)
        //     {
        //         ListChallengeUnit.Clear();
        //         ListChallengeUnit.AddRange(GetEnemyUnitList(GetAllyBase(), _isAlly: true));
        //         foreach(UnitBase unit in ListChallengeUnit)
        //             unit.AddUnitEffect(UNIT_EFFECT.CC_FEAR, unit, unit, new float[] { 3.0f });
        //     }
        //
        //     int bossCnt = 0;
        //     foreach (var spawnOrder in enemySpawnData.Orders)
        //     {
        //         if (spawnOrder.EnemyId.Contains("Mid_Boss") || spawnOrder.EnemyId.Contains("Final_Boss"))
        //             bossCnt++;
        //     }
        //
        //     foreach (var spawnOrder in enemySpawnData.Orders)
        //     {
        //         if (spawnOrder.EnemyId.Contains("Mid_Boss") || spawnOrder.EnemyId.Contains("Final_Boss"))
        //         {
        //             if (!IsBossAppeared)
        //             {
        //                 ReserveSpawnCnt += spawnOrder.EnemyCount;
        //                 TaskSpawnEnemy(spawnOrder.EnemyId, spawnOrder.EnemyCount, (float)spawnOrder.DistanceTime, true, bossCnt).Forget();
        //             }
        //         }
        //         else
        //         {
        //             if (IsBossAppeared)
        //             {
        //                 int spawnCnt = MathLib.CheckPercentage(0.5f) ? Mathf.CeilToInt(spawnOrder.EnemyCount * 0.5f) : Mathf.FloorToInt(spawnOrder.EnemyCount * 0.5f);
        //                 ReserveSpawnCnt += spawnCnt;
        //                 TaskSpawnEnemy(spawnOrder.EnemyId, spawnCnt, (float)spawnOrder.DistanceTime).Forget();
        //
        //             }
        //             else
        //             {
        //                 ReserveSpawnCnt += spawnOrder.EnemyCount;
        //                 TaskSpawnEnemy(spawnOrder.EnemyId, spawnOrder.EnemyCount, (float)spawnOrder.DistanceTime).Forget();
        //             }
        //         }
        //     }
        //
        //     if (TutorialStep == 7)
        //         TaskTutorial_7().Forget();
        //
        //     IsBossAppeared = enemySpawnData.IsBossWave;
        // }
        
        SOModeData.ModeSpawn();
    }

    private async UniTaskVoid TaskDelayWarning()
    {
        await UniTask.Delay(2500, cancellationToken: this.GetCancellationTokenOnDestroy());

        skgBossWarning.gameObject.SetActive(false);
    }

    /// <summary>
    /// 유닛 소환 테스크
    /// </summary>
    /// <returns></returns>
    public async UniTaskVoid TaskSpawnEnemy(string _enemyID, int _spawnCnt, float _distanceTime, bool _isBoss = false, int _bossMaxCnt = 0)
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(_distanceTime), cancellationToken: this.GetCancellationTokenOnDestroy());

        if (!isStageStart)
            return;

        UnitBase unitSpawn = null;
        for(int i = 0; i < _spawnCnt; i++)
        {
            float yPos = Random.Range(0.0f, -3.5f);

            if (_isBoss)
            {
                unitSpawn = MgrUnitPool.Instance.ShowEnemyMonsterObj(_enemyID, 1, new Vector3(unitAllyBase.transform.position.x + 25.0f, yPos, yPos * 0.01f)).GetComponent<UnitBase>();

                listUnitBoss.Add(unitSpawn);

                if (listUnitBoss.Count == 1) yPos = -1.0f;
                else if (listUnitBoss.Count == 2) yPos = -3.0f;

                if (_bossMaxCnt >= 2)
                {
                    objBossHPBack[1].SetActive(true);

                    imgBossShield[listUnitBoss.Count - 1] = objBossHPBack[1].transform.GetChild(1 + listUnitBoss.Count - 1).GetChild(0).GetComponent<SlicedFilledImage>();
                    imgBossHP[listUnitBoss.Count - 1] = objBossHPBack[1].transform.GetChild(1 + listUnitBoss.Count - 1).GetChild(1).GetComponent<SlicedFilledImage>();
                    tmpBossHP[listUnitBoss.Count - 1] = objBossHPBack[1].transform.GetChild(1 + listUnitBoss.Count - 1).GetChild(3).GetComponent<TextMeshProUGUI>();

                    tmpAngryRemain = objBossHPBack[1].transform.GetChild(5).GetComponent<TextMeshProUGUI>();

                    objAngryIcon = objBossHPBack[1].transform.GetChild(3).gameObject;
                    objAngryBackImg = objBossHPBack[1].transform.GetChild(4).gameObject;

                    tmpBossType = objBossHPBack[1].transform.GetChild(7).GetComponent<TextMeshProUGUI>();
                    imgBossClass[listUnitBoss.Count - 1] = tmpBossType.transform.GetChild(listUnitBoss.Count - 1).GetComponent<Image>();

                    objBossHPBack[1].transform.GetChild(4).gameObject.SetActive(false);
                    objBossHPBack[1].transform.GetChild(6).GetComponent<Image>().sprite = sprBossHPIcon[currWave == totalWave ? 1 : 0];

                    if(listUnitBoss.Count == 1)
                    {
                        tmpBossHP[1] = objBossHPBack[1].transform.GetChild(2).GetChild(3).GetComponent<TextMeshProUGUI>();
                        imgBossClass[1] = tmpBossType.transform.GetChild(1).GetComponent<Image>();

                        tmpBossHP[1].text = $"{(int)unitSpawn.UnitStat.HP} / {(int)unitSpawn.UnitStat.MaxHP}";
                        SetBossHPBarUI(currWave != totalWave ? "MID-BOSS" : "BOSS", unitSpawn.UnitSetting.unitClass, 2);
                    }
                }
                else
                {
                    yPos = -2.0f;

                    objBossHPBack[0].SetActive(true);
                    imgBossShield[0] = objBossHPBack[0].transform.GetChild(1).GetComponent<SlicedFilledImage>();
                    imgBossHP[0] = objBossHPBack[0].transform.GetChild(2).GetComponent<SlicedFilledImage>();
                    tmpBossHP[0] = objBossHPBack[0].transform.GetChild(10).GetComponent<TextMeshProUGUI>();

                    objBossHPBack[0].transform.GetChild(3).gameObject.SetActive(unitSpawn.UnitSetting.unitType == UnitType.MidBoss);
                    objBossHPBack[0].transform.GetChild(4).gameObject.SetActive(unitSpawn.UnitSetting.unitType == UnitType.Boss);

                    tmpAngryRemain = objBossHPBack[0].transform.GetChild(7).GetComponent<TextMeshProUGUI>();

                    objAngryIcon = objBossHPBack[0].transform.GetChild(5).gameObject;
                    objAngryBackImg = objBossHPBack[0].transform.GetChild(6).gameObject;

                    tmpBossType = objBossHPBack[0].transform.GetChild(9).GetComponent<TextMeshProUGUI>();
                    imgBossClass[0] = tmpBossType.transform.GetChild(0).GetComponent<Image>();

                    objBossHPBack[0].transform.GetChild(6).gameObject.SetActive(false);
                    objBossHPBack[0].transform.GetChild(8).GetComponent<Image>().sprite = sprBossHPIcon[currWave == totalWave ? 1 : 0];
                }

                unitSpawn.transform.position = new Vector3(unitAllyBase.transform.position.x + 25.0f, yPos, yPos * 0.01f);

                objAngryIcon.SetActive(false);
                objAngryBackImg.SetActive(false);

                SetBossHPBar(unitSpawn.UnitStat.HP, unitSpawn.UnitStat.MaxHP, unitSpawn.Shield, unitSpawn);
                SetBossHPBarUI(currWave != totalWave ? "MID-BOSS" : "BOSS", unitSpawn.UnitSetting.unitClass, listUnitBoss.Count);
                
                SOModeData.SetBossAngryTimer();
                //bossAngryTimer = (GameMode == GAME_MODE.GoldMode || GameMode == GAME_MODE.Pvp) ? 60 : 180;
                tmpAngryRemain.color = Color.red;
                tmpWave.gameObject.SetActive(false);
                MgrBoosterSystem.Instance.objBoosterGauge.SetActive(false);
            }
            else
            {
                if (_enemyID.Contains('$')) MgrUnitPool.Instance.ShowEnemyUnitObj(_enemyID, 1, new Vector3(unitAllyBase.transform.position.x + 25.0f, yPos, yPos * 0.01f));
                else MgrUnitPool.Instance.ShowEnemyMonsterObj(_enemyID, 1, new Vector3(unitAllyBase.transform.position.x + 25.0f, yPos, yPos * 0.01f));
            }

            ReserveSpawnCnt--;
        }

        if(ReserveSpawnCnt == 0)
        {
            if (GameMode == GAME_MODE.Training)
            {
                ModeTimer = 60.0f;
                objTimerSlider.SetActive(true);
                imgTimerSlider.fillAmount = 1.0f;
                TaskTrainingModeTimer().Forget();
            }
        }
    }

    private async UniTaskVoid TaskSurvivalTimer()
    {
        while(isStageStart && ModeTimer > 0.0f)
        {
            ModeTimer -= Time.deltaTime;
            imgTimerSlider.fillAmount = ModeTimer / 60.0f;
            await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        imgTimerSlider.fillAmount = 0.0f;

        if (isStageStart)
            SetEndBattle(false);
    }
    
    private async UniTaskVoid TaskTrainingModeTimer()
    {
        while(isStageStart && ModeTimer > 0.0f)
        {
            ModeTimer -= Time.deltaTime;
            imgTimerSlider.fillAmount = ModeTimer / 60.0f;
            await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        if (isStageStart)
            SetEndBattle(false);
    }
    #endregion

    #region 사이드 스킬 세팅
    /// <summary>
    /// 사이드 스킬 추가
    /// </summary>
    /// <param name="_index">스킬 인덱스</param>
    /// <param name="_sprite">아이콘 이미지</param>
    public void AddSideSkill(string _index, Sprite _sprite)
    {
        List<string> listKey = new List<string>(MgrBoosterSystem.Instance.DicSkill.Keys);

        int keyCnt = 0;
        foreach (string key in listKey)
        {
            if (!key.Contains("skill_active"))
                continue;

            keyCnt++;
        }

        objSideSkillRoot.transform.GetChild(keyCnt).gameObject.SetActive(true);
        objSideSkillRoot.transform.GetChild(keyCnt).GetComponent<SideSkill>().SetSideSkill(_index, _sprite);
    }

    public void ResetTutorialSideSkillCoolDown() => objSideSkillRoot.transform.GetChild(0).GetComponent<SideSkill>().SetCoolDown(0.0f);

    /// <summary>
    /// 전체 사이드 스킬 쿨타임 초기화
    /// </summary>
    public void ResetSideSkillCoolDown()
    {
        for (int i = 0; i < objSideSkillRoot.transform.childCount; i++)
            objSideSkillRoot.transform.GetChild(i).GetComponent<SideSkill>().SetCoolDown(0.5f);
    }

    /// <summary>
    /// 전체 사이드 스킬 쿨타임 감소
    /// </summary>
    /// <param name="_ratio">감소율</param>
    public void ReduceSideSkillCoolDown(float _ratio)
    {
        SideSkill side;
        for (int i = 0; i < objSideSkillRoot.transform.childCount; i++)
        {
            side = objSideSkillRoot.transform.GetChild(i).GetComponent<SideSkill>();
            side.AddCoolDown(side.GetMaxCooldown() * _ratio, true);
        }
    }

    /// <summary>
    /// 특정 사이드 스킬 쿨타임 감소
    /// </summary>
    /// <param name="_ratio">감소율</param>
    /// <param name="_sideSkill">사이드 스킬 인덱스</param>
    public void ReduceSideSkillCoolDown(float _ratio, SideSkill _sideSkill)
    {
        SideSkill side;
        for (int i = 0; i < objSideSkillRoot.transform.childCount; i++)
        {
            side = objSideSkillRoot.transform.GetChild(i).GetComponent<SideSkill>();
            if (_sideSkill == side)
                continue;

            side.AddCoolDown(side.GetMaxCooldown() * _ratio);
        }
    }
    #endregion

    #region 전투 유닛 리스트 제어
    /// <summary>
    /// 유닛 필드 추가
    /// </summary>
    /// <param name="_unitBase">대상 유닛</param>
    public void AddUnitBase(UnitBase _unitBase)
    {
        if (ListUnitBase.Contains(_unitBase))
            return;

        if (_unitBase.UnitSetting.unitType == UnitType.AllyBase && _unitBase.TeamNum == 0)
            unitAllyBase = _unitBase;

        ListUnitBase.Add(_unitBase);
    }

    /// <summary>
    /// 유닛 필드 제거
    /// </summary>
    /// <param name="_unitBase">대상 유닛</param>
    public void RemoveUnitBase(UnitBase _unitBase) => ListUnitBase.Remove(_unitBase);

    private readonly List<UnitBase> listUnitOutCamera = new List<UnitBase>();
    /// <summary>
    /// 카메라 외부 유닛 추가
    /// </summary>
    /// <param name="_unit">대상 유닛</param>
    public void AddUnitOutCamera(UnitBase _unit)
    {
        if (listUnitOutCamera.Contains(_unit))
            return;

        listUnitOutCamera.Add(_unit);
    }
    /// <summary>
    /// 카메라 외부 유닛 제거
    /// </summary>
    /// <param name="_unit">대상 유닛</param>
    public void RemoveUnitOutCamera(UnitBase _unit) => listUnitOutCamera.Remove(_unit);

    private readonly List<UnitBase> listUnitOutCameraLeft = new List<UnitBase>();
    private readonly List<UnitBase> listUnitOutCameraRight = new List<UnitBase>();
    /// <summary>
    /// 유닛 카메라 외부 체크 갱신
    /// </summary>
    public void RefreshUnitOutCamera()
    {
        if (GameMode == GAME_MODE.Pvp)
            return;
        
        listUnitOutCameraLeft.Clear();
        listUnitOutCameraRight.Clear();

        foreach(UnitBase unit in listUnitOutCamera)
        {
            if (unit.transform.position.x < MgrCamera.Instance.CameraMain.transform.position.x) listUnitOutCameraLeft.Add(unit);
            else listUnitOutCameraRight.Add(unit);
        }

        var leftPartitions = listUnitOutCameraLeft.GroupBy(x => x.UnitSetting.unitIndex);
        var rightPartitions = listUnitOutCameraRight.GroupBy(x => x.UnitSetting.unitIndex);

        // 왼쪽 체크
        int indexCnt = 0;
        foreach (var partition in leftPartitions)
        {
            arrUnitOutCamera[indexCnt].SetIcon(partition.First().UnitSetting.unitCameraOutIcon, partition.Count());
            indexCnt++;
        }

        for (int i = 0; i < arrUnitOutCamera.Length; i++)
            arrUnitOutCamera[i].gameObject.SetActive(i < indexCnt);
        objOutCameraUI.SetActive(indexCnt > 0);

        // 오른쪽 체크
        indexCnt = 0;
        foreach (var partition in rightPartitions)
        {
            arrUnitOutCameraRight[indexCnt].SetIcon(partition.First().UnitSetting.unitCameraOutIcon, partition.Count());
            indexCnt++;
        }

        for (int i = 0; i < arrUnitOutCameraRight.Length; i++)
            arrUnitOutCameraRight[i].gameObject.SetActive(i < indexCnt);
        objOutCameraUIRight.SetActive(indexCnt > 0);
    }
    #endregion

    #region 전투 중 유닛 체크 및 반환 함수

    /// <summary>
    /// 기지 반환 함수
    /// </summary>
    /// <returns></returns>
    public UnitBase GetAllyBase(bool _isEnemy = false)
    {
        return _isEnemy ? oppoAlly : unitAllyBase;
    }

    /// <summary>
    /// 상대 유닛이 살아있는지 여부
    /// </summary>
    /// <param name="_unitBase">체크 기준 유닛</param>
    /// <returns></returns>
    public bool CheckIsAliveEnemy(UnitBase _unitBase)
    {
        foreach(UnitBase ub in ListUnitBase)
        {
            if (ub.TeamNum == _unitBase.TeamNum || ub.CheckIsState(UNIT_STATE.DEATH))
                continue;

            return true;
        }
        return false;
    }

    /// <summary>
    /// 가장 낮은 유닛 반환
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <param name="_isAlly">아군 여부</param>
    /// <returns></returns>
    public UnitBase GetLowestHPUnit(UnitBase _unitBase, bool _isAlly = false)
    {
        float prevHPValue = 1.0f;
        UnitBase currUnit = null;

        foreach(UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || unit.UnitStat.HP / unit.UnitStat.MaxHP > prevHPValue || unit.UnitSetting.unitType == UnitType.AllyBase)
                continue;

            if (unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            prevHPValue = unit.UnitStat.HP / unit.UnitStat.MaxHP;
            currUnit = unit;
        }

        return currUnit;
    }

    /// <summary>
    /// 가장 낮은 유닛 반환
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <param name="_v3Pos">기준 위치</param>
    /// <param name="_radius">범위 (반지름)</param>
    /// <param name="_isAlly">아군 여부</param>
    /// <returns></returns>
    public UnitBase GetLowestHPUnit(UnitBase _unitBase, Vector3 _v3Pos, float _radius, bool _isAlly = false)
    {
        float prevHPValue = 1.0f;
        UnitBase currUnit = null;

        foreach(UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || !MathLib.CheckIsInEllipse(_v3Pos, _radius, unit.transform.position) || unit.UnitStat.HP / unit.UnitStat.MaxHP > prevHPValue || unit.UnitSetting.unitType == UnitType.AllyBase)
                continue;

            if (unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            prevHPValue = unit.UnitStat.HP / unit.UnitStat.MaxHP;
            currUnit = unit;
        }

        return currUnit;
    }

    /// <summary>
    /// 범위 내 특수 효과를 가지고 있는 유닛이 있는지 체크
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <param name="_v3Pos">기준 위치</param>
    /// <param name="_radius">범위 (반지름)</param>
    /// <param name="_effect">체크할 특수효과 인덱스</param>
    /// <param name="_isAlly">아군 여부</param>
    /// <returns></returns>
    public bool CheckHasEffectUnitInEllipse(UnitBase _unitBase, Vector3 _v3Pos, float _radius, UNIT_EFFECT _effect, bool _isAlly = false)
    {
        foreach(UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || unit.UnitSetting.unitType == UnitType.AllyBase || !MathLib.CheckIsInEllipse(_v3Pos, _radius, unit.transform.position) || !unit.CheckHasUnitEffect(_effect))
                continue;

            if (unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// 범위 내 디버프를 가지고 있는 유닛이 있는지 체크
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <param name="_v3Pos">기준 위치</param>
    /// <param name="_radius">범위 (반지름)</param>
    /// <param name="_isAlly">아군 여부</param>
    /// <returns></returns>
    public bool CheckHasDebuffUnitInEllipse(UnitBase _unitBase, Vector3 _v3Pos, float _radius, bool _isAlly = false)
    {
        foreach(UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || unit.UnitSetting.unitType == UnitType.AllyBase || !MathLib.CheckIsInEllipse(_v3Pos, _radius, unit.transform.position) || !unit.CheckHasDebuffUnitEffect())
                continue;

            if (unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// 범위 내 특수 효과를 가지고 있는 유닛이 있는지 체크
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <param name="_v3Pos">기준 위치</param>
    /// <param name="_radius">범위 (반지름)</param>
    /// <param name="_effect">체크할 특수효과 인덱스</param>
    /// <param name="_isAlly">아군 여부</param>
    /// <returns></returns>
    public List<UnitBase> GetHasEffectUnitInEllipse(UnitBase _unitBase, Vector3 _v3Pos, float _radius, UNIT_EFFECT _effect, bool _isAlly = false, bool _isContainBlockedTarget = false)
    {
        listTemp.Clear();

        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || !MathLib.CheckIsInEllipse(_v3Pos, _radius, unit.transform.position) || !unit.CheckHasUnitEffect(_effect))
                continue;

            if (!_isContainBlockedTarget && unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            listTemp.Add(unit);
        }
        listTemp.Sort((a, b) => (_v3Pos - a.transform.position).sqrMagnitude.CompareTo((_v3Pos - b.transform.position).sqrMagnitude));

        if (CheckIsContainAllyBase(_unitBase, _isAlly) && listTemp.Count > 1)
            listTemp.Remove(GetAllyBase());

        return listTemp;
    }

    /// <summary>
    /// 범위 내 디버프를 가지고 있는 유닛이 있는지 체크
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <param name="_v3Pos">기준 위치</param>
    /// <param name="_radius">범위 (반지름)</param>
    /// <param name="_isAlly">아군 여부</param>
    /// <returns></returns>
    public List<UnitBase> GetHasDebuffUnitInEllipse(UnitBase _unitBase, Vector3 _v3Pos, float _radius, bool _isAlly = false, bool _isContainBlockedTarget = false)
    {
        listTemp.Clear();

        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || !MathLib.CheckIsInEllipse(_v3Pos, _radius, unit.transform.position) || !unit.CheckHasDebuffUnitEffect())
                continue;

            if (!_isContainBlockedTarget && unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            listTemp.Add(unit);
        }
        listTemp.Sort((a, b) => (_v3Pos - a.transform.position).sqrMagnitude.CompareTo((_v3Pos - b.transform.position).sqrMagnitude));

        if (CheckIsContainAllyBase(_unitBase, _isAlly) && listTemp.Count > 1)
            listTemp.Remove(GetAllyBase());

        return listTemp;
    }

    /// <summary>
    /// 랜덤 유닛 1명 반환
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <param name="_isAlly">아군 여부</param>
    /// <returns></returns>
    public UnitBase GetRandomUnit(UnitBase _unitBase, bool _isAlly = false)
    {
        listTemp.Clear();

        foreach(UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f)
                continue;

            if (unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            listTemp.Add(unit);
        }

        if (CheckIsContainAllyBase(_unitBase, _isAlly) && listTemp.Count > 1)
            listTemp.Remove(GetAllyBase());

        listTemp.Shuffle();

        return listTemp.Count > 0 ? listTemp[0] : null;
    }

    /// <summary>
    /// 가장 X축이 가까운 적 유닛 반환
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    public UnitBase GetNearestXEnemyUnit(UnitBase _unitBase)
    {
        float dist = _unitBase.TeamNum == 0 ? 9999.0f : -9999.0f;
        UnitBase unitResult = null;
        foreach(UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || _unitBase.TeamNum == unit.TeamNum)
                continue;

            if (unit.IsBlockedTarget)
                continue;

            if (_unitBase.TeamNum == 0)
            {
                if(unit.transform.position.x < dist)
                {
                    dist = unit.transform.position.x;
                    unitResult = unit;
                }
            }
            else
            {
                if (unit.transform.position.x > dist)
                {
                    dist = unit.transform.position.x;
                    unitResult = unit;
                }
            }
        }
        return unitResult;
    }

    /// <summary>
    /// 사정거리 내 가장 X축이 먼 적 유닛 반환
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    public UnitBase GetFarestXEnemyUnitInRange(UnitBase _unitBase, float _range)
    {
        float dist = _unitBase.TeamNum == 0 ? -9999.0f : 9999.0f;
        UnitBase unitResult = null;
        float unitDist;
        foreach(UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || _unitBase.TeamNum == unit.TeamNum || unit.UnitSetting.unitType == UnitType.AllyBase)
                continue;

            if (unit.IsBlockedTarget)
                continue;

            unitDist = unit.transform.position.x - _unitBase.transform.position.x;
            if (unitDist > _range)
                continue;

            if(_unitBase.TeamNum == 0)
            {
                if(unitDist > dist)
                {
                    dist = unitDist;
                    unitResult = unit;
                }
            }
            else
            {
                if (unitDist < dist)
                {
                    dist = unitDist;
                    unitResult = unit;
                }
            }
        }

        return unitResult;
    }

    /// <summary>
    /// 가장 X축이 먼 아군 유닛 반환
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    public UnitBase GetFarestXAllyUnit(UnitBase _unitBase)
    {
        float dist = _unitBase.TeamNum == 0 ? -9999.0f : 9999.0f;
        UnitBase unitResult = null;
        foreach(UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || _unitBase.TeamNum != unit.TeamNum || unit.UnitSetting.unitType == UnitType.AllyBase)
                continue;

            if (unit.IsBlockedTarget)
                continue;

            if (_unitBase.TeamNum == 0)
            {
                if(unit.transform.position.x > dist)
                {
                    dist = unit.transform.position.x;
                    unitResult = unit;
                }
            }
            else
            {
                if (unit.transform.position.x < dist)
                {
                    dist = unit.transform.position.x;
                    unitResult = unit;
                }
            }
        }
        return unitResult;
    }

    /// <summary>
    /// 적 유닛 반환
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <param name="_isAlly">아군 여부</param>
    /// <returns></returns>
    public List<UnitBase> GetEnemyUnitList(UnitBase _unitBase, bool _isAlly = false, bool _isContainBlockedTarget = false)
    {
        listTemp.Clear();

        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f)
                continue;

            if (!_isContainBlockedTarget && unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            listTemp.Add(unit);
        }
        listTemp.Sort((a, b) => (_unitBase.transform.position - a.transform.position).sqrMagnitude.CompareTo((_unitBase.transform.position - b.transform.position).sqrMagnitude));

        if (CheckIsContainAllyBase(_unitBase, _isAlly) && listTemp.Count > 1)
            listTemp.Remove(GetAllyBase());

        return listTemp;
    }

    /// <summary>
    /// 클래스 별 적 유닛 반환
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <param name="_unitClass">클래스</param>
    /// <param name="_isAlly">아군 여부</param>
    /// <returns></returns>
    public List<UnitBase> GetEnemyUnitListInClass(UnitBase _unitBase, UnitClass _unitClass, bool _isAlly = false, bool _isContainBlockedTarget = false)
    {
        listTemp.Clear();

        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f)
                continue;

            if (!_isContainBlockedTarget && unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            if (unit.UnitSetting.unitClass != _unitClass)
                continue;

            listTemp.Add(unit);
        }

        return listTemp;
    }

    /// <summary>
    /// 동일한 인덱스 유닛 반환
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <param name="_isAlly">아군 여부</param>
    /// <returns></returns>
    public List<UnitBase> GetEnemyUnitInSameIndex(UnitBase _unitBase, string _index, bool _isAlly = false, bool _excludeSpawnUnit = false, bool _isContainBlockedTarget = false)
    {
        listTemp.Clear();

        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f)
                continue;

            if (!_isContainBlockedTarget && unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            if (!unit.UnitSetting.unitIndex.Equals(_index) || (_excludeSpawnUnit && !(unit.UnitBaseParent is null)))
                continue;

            listTemp.Add(unit);
        }
        listTemp.Sort((a, b) => (_unitBase.transform.position - a.transform.position).sqrMagnitude.CompareTo((_unitBase.transform.position - b.transform.position).sqrMagnitude));

        return listTemp;
    }

    /// <summary>
    /// 동일한 인덱스 유닛 반환
    /// </summary>
    /// <param name="_isAlly">아군 여부</param>
    /// <returns></returns>
    public List<UnitBase> GetUnitInSameIndex(int _teamNum, string _index)
    {
        listTemp.Clear();

        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || _teamNum != unit.TeamNum)
                continue;

            if (!unit.UnitSetting.unitIndex.Equals(_index))
                continue;

            listTemp.Add(unit);
        }

        return listTemp;
    }

    /// <summary>
    /// 가장 공격력이 높은 적 반환
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <param name="_radius">범위</param>
    /// <returns></returns>
    public UnitBase GetHighestAtkEnemyUnit(UnitBase _unitBase, float _radius = 0.0f, bool _isAlly = false)
    {
        float prevAtk = 0.0f;
        UnitBase unitResult = null;
        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || unit.UnitSetting.unitType == UnitType.AllyBase || (_radius > 0.0f && !MathLib.CheckIsInEllipse(_unitBase.transform.position, _radius, unit.transform.position)))
                continue;

            if (unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            if (unit.GetAtk() <= prevAtk)
                continue;

            prevAtk = unit.GetAtk();
            unitResult = unit;
        }

        return unitResult;
    }

    /// <summary>
    /// 좌표 위치 기준 타원 범위 내 가람 이벤트 적용 가능한 적 있는지 체크
    /// </summary>
    /// <param name="_unitBase">시전 유닛</param>
    /// <param name="_v3Pos">기준 좌표</param>
    /// <param name="_radius">반지름</param>
    /// <returns></returns>
    public bool CheckCanAddGaramEventUnitInEllipse(UnitBase _unitBase, Vector3 _v3Pos, float _radius, bool _isAlly = false)
    {
        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || unit.CheckHasGaramDamagedEvent() || !MathLib.CheckIsInEllipse(_v3Pos, _radius, unit.transform.position))
                continue;

            if (unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            return true;
        }
        return false;
    }

    /// <summary>
    /// 좌표 위치 기준 타원 범위 내 가람 부적 없는 가장 가까운 적들 반환
    /// </summary>
    /// <param name="_unitBase">시전 유닛</param>
    /// <param name="_v3Pos">기준 좌표</param>
    /// <param name="_radius">반지름</param>
    /// <param name="_limitCnt">제한 인원 수</param>
    /// <returns></returns>
    public List<UnitBase> GetNearestEnemyUnitCanAddGaramEventInEllipse(UnitBase _unitBase, Vector3 _v3Pos, float _radius, int _limitCnt = 0, bool _isAlly = false, bool _isContainBlockedTarget = false)
    {
        listTemp.Clear();

        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || unit.CheckHasGaramDamagedEvent())
                continue;

            if (!_isContainBlockedTarget && unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            if (MathLib.CheckIsInEllipse(_v3Pos, _radius, unit.transform.position))
                listTemp.Add(unit);
        }
        listTemp.Sort((a, b) => (_v3Pos - a.transform.position).sqrMagnitude.CompareTo((_v3Pos - b.transform.position).sqrMagnitude));

        if (CheckIsContainAllyBase(_unitBase, _isAlly) && listTemp.Count > 1)
            listTemp.Remove(GetAllyBase());

        return _limitCnt == 0 ? listTemp : listTemp.GetRange(0, listTemp.Count > _limitCnt ? _limitCnt : listTemp.Count);
    }

    /// <summary>
    /// 좌표 위치 기준 타원 범위 내 적 있는지 체크
    /// </summary>
    /// <param name="_unitBase">시전 유닛</param>
    /// <param name="_v3Pos">기준 좌표</param>
    /// <param name="_radius">반지름</param>
    /// <returns></returns>
    public bool CheckIsEnemyUnitInEllipse(UnitBase _unitBase, Vector3 _v3Pos, float _radius, bool _isAlly = false)
    {
        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || !MathLib.CheckIsInEllipse(_v3Pos, _radius, unit.transform.position))
                continue;

            if (unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            return true;
        }
        return false;
    }

    /// <summary>
    /// 타원 범위 내 기준 유닛 중심으로 가장 가까운 적 반환
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <returns></returns>
    public UnitBase GetNearestEnemyUnitInEllipse(UnitBase _unitBase, float _range = 0.0f, float _ratio = 0.5f)
    {
        float dist = 9999.0f;
        UnitBase unitResult = null;
        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || _unitBase.TeamNum == unit.TeamNum)
                continue;

            if (unit.IsBlockedTarget)
                continue;

            if (MathLib.CheckIsInEllipse(_unitBase.transform.position, _range == 0.0f ? _unitBase.UnitStat.Range : _range, unit.transform.position, _ratio) && (_unitBase.transform.position - unit.transform.position).sqrMagnitude < dist)
            {
                dist = (_unitBase.transform.position - unit.transform.position).sqrMagnitude;
                unitResult = unit;
            }
        }

        return unitResult;
    }

    /// <summary>
    /// 타원 범위 내 기준 유닛 중심으로 가장 먼 적 반환
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <returns></returns>
    public UnitBase GetFarestEnemyUnitInEllipse(UnitBase _unitBase, float _range = 0.0f, float _ratio = 0.5f)
    {
        float dist = 0.001f;
        UnitBase unitResult = null;
        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || unit.UnitSetting.unitType == UnitType.AllyBase || _unitBase.TeamNum == unit.TeamNum)
                continue;

            if (unit.IsBlockedTarget)
                continue;

            if (MathLib.CheckIsInEllipse(_unitBase.transform.position, _range == 0.0f ? _unitBase.UnitStat.Range : _range, unit.transform.position, _ratio) && (_unitBase.transform.position - unit.transform.position).sqrMagnitude > dist)
            {
                dist = (_unitBase.transform.position - unit.transform.position).sqrMagnitude;
                unitResult = unit;
            }
        }

        return unitResult;
    }

    /// <summary>
    /// 좌표 위치 기준 타원 범위 내 가장 가까운 적들 반환
    /// </summary>
    /// <param name="_unitBase">시전 유닛</param>
    /// <param name="_v3Pos">기준 좌표</param>
    /// <param name="_radius">반지름</param>
    /// <param name="_limitCnt">제한 인원 수</param>
    /// <returns></returns>
    public List<UnitBase> GetNearestEnemyUnitInEllipse(UnitBase _unitBase, Vector3 _v3Pos, float _radius, int _limitCnt = 0, bool _isCenterPos = false, bool _isAlly = false, bool _isContainBlockedTarget = false)
    {
        listTemp.Clear();

        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f)
                continue;

            if (!_isContainBlockedTarget && unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            if (MathLib.CheckIsInEllipse(_v3Pos, _radius, _isCenterPos ? unit.GetUnitCenterPos() : unit.transform.position))
                listTemp.Add(unit);
        }
        listTemp.Sort((a, b) => (_v3Pos - a.transform.position).sqrMagnitude.CompareTo((_v3Pos - b.transform.position).sqrMagnitude));

        return _limitCnt == 0 ? listTemp : listTemp.GetRange(0, listTemp.Count > _limitCnt ? _limitCnt : listTemp.Count);
    }

    /// <summary>
    /// 좌표 위치 기준 직선 범위 내 적들 반환<br/>
    /// 단, 해당 함수는 좌/우 구분이 없음
    /// </summary>
    /// <param name="_unitBase">시전 유닛</param>
    /// <param name="_v3Pos">기준 좌표</param>
    /// <param name="_range">길이</param>
    /// <param name="_width">넓이 (총 너비를 입력)</param>
    /// <param name="_limitCnt">제한 인원 수</param>
    /// <returns></returns>
    public List<UnitBase> GetNearestEnemyUnitInLine(UnitBase _unitBase, Vector3 _v3Pos, float _range, float _width, int _limitCnt = 0, bool _isAlly = false, bool _isContainBlockedTarget = false)
    {
        listTemp.Clear();

        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f)
                continue;

            if (!_isContainBlockedTarget && unit.IsBlockedTarget)
                continue;

            if ((_unitBase.TeamNum == unit.TeamNum && !_isAlly) || (_unitBase.TeamNum != unit.TeamNum && _isAlly))
                continue;

            float xValue = unit.transform.position.x - _v3Pos.x;
            xValue = xValue < 0.0f ? -xValue : xValue;

            float yValue = unit.transform.position.y - _v3Pos.y;
            yValue = yValue < 0.0f ? -yValue : yValue;

            if (xValue <= _range && yValue <= _width * 0.5f)
                listTemp.Add(unit);
        }
        listTemp.Sort((a, b) => (_v3Pos - a.transform.position).sqrMagnitude.CompareTo((_v3Pos - b.transform.position).sqrMagnitude));

        return _limitCnt == 0 ? listTemp : listTemp.GetRange(0, listTemp.Count > _limitCnt ? _limitCnt : listTemp.Count);
    }

    /// <summary>
    /// 좌표 위치 기준 직선 범위 내 적들 반환<br/>
    /// 해당 함수는 목표 지점 기준으로 넓이만큼 직선 체크
    /// </summary>
    /// <param name="_unitBase">시전 유닛</param>
    /// <param name="_v3StartPos">기준 좌표</param>
    /// <param name="_v3EndPos">목표 좌표</param>
    /// <param name="_width">넓이 (총 너비를 입력)</param>
    /// <param name="_limitCnt">제한 인원 수</param>
    /// <param name="_isContainBlockedTarget">타겟팅 불가 포함 여부</param>
    /// <param name="_isCheckCenterPos">체크 기준을 유닛 중심 or 발 위치</param>
    /// <returns></returns>
    public List<UnitBase> GetEnemyUnitInPolygon(UnitBase _unitBase, Vector3 _v3StartPos, Vector3 _v3EndPos, float _width, int _limitCnt = 0, bool _isContainBlockedTarget = false, bool _isCheckCenterPos = false)
    {
        listTemp.Clear();

        Vector3[] arrV3Corner = new Vector3[4];
        MathLib.GetSquareVertex(_v3StartPos, _v3EndPos, _width, ref arrV3Corner);

        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || _unitBase.TeamNum == unit.TeamNum)
                continue;

            if (!_isContainBlockedTarget && unit.IsBlockedTarget)
                continue;

            if (!MathLib.CheckIsPosInPolygon(_isCheckCenterPos ? unit.GetUnitCenterPos() : unit.transform.position, arrV3Corner))
                continue;

            listTemp.Add(unit);
        }
        listTemp.Sort((a, b) => (_v3StartPos - a.transform.position).sqrMagnitude.CompareTo((_v3StartPos - b.transform.position).sqrMagnitude));

        return _limitCnt == 0 ? listTemp : listTemp.GetRange(0, listTemp.Count > _limitCnt ? _limitCnt : listTemp.Count);
    }

    /// <summary>
    /// 좌표 위치 기준 직선 범위 내 적들 반환
    /// </summary>
    /// <param name="_unitBase">시전 유닛</param>
    /// <param name="_v3Pos">기준 좌표</param>
    /// <param name="_range">길이</param>
    /// <param name="_width">넓이 (총 너비를 입력)</param>
    /// <returns></returns>
    public bool CheckIsEnemyUnitInLine(UnitBase _unitBase, Vector3 _v3Pos, float _range, float _width)
    {
        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || _unitBase.TeamNum == unit.TeamNum)
                continue;

            if (unit.IsBlockedTarget)
                continue;

            float xValue = unit.transform.position.x - _v3Pos.x;
            xValue = xValue < 0.0f ? -xValue : xValue;

            float yValue = unit.transform.position.y - _v3Pos.y;
            yValue = yValue < 0.0f ? -yValue : yValue;

            if (xValue <= _range && yValue <= _width * 0.5f)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 좌표 위치 기준 부채꼴 범위 내 적들 반환
    /// </summary>
    /// <param name="_unitBase">기준 유닛</param>
    /// <param name="_v3Pos">기준 좌표</param>
    /// <param name="_v3Dir">방향</param>
    /// <param name="_radius">반지름</param>
    /// <param name="_angle">총 각도</param>
    /// <returns></returns>
    public List<UnitBase> GetEnemyUnitInPanShape(UnitBase _unitBase, Vector3 _v3Pos, Vector3 _v3Dir, float _radius, float _angle, bool _isCenterPos = false, bool _isContainBlockedTarget = false)
    {
        listTemp.Clear();

        foreach (UnitBase unit in ListUnitBase)
        {
            if (unit.CheckIsState(UNIT_STATE.DEATH) || unit.UnitStat.HP <= 0.0f || _unitBase.TeamNum == unit.TeamNum)
                continue;

            if (!_isContainBlockedTarget && unit.IsBlockedTarget)
                continue;

            if (!MathLib.CheckIsInPanShape(_v3Pos, _v3Dir, _radius, _angle, _isCenterPos ? unit.GetUnitCenterPos() : unit.transform.position))
                continue;

            listTemp.Add(unit);
        }
        listTemp.Sort((a, b) => (_v3Pos - a.transform.position).sqrMagnitude.CompareTo((_v3Pos - b.transform.position).sqrMagnitude));

        return listTemp;
    }

    private bool CheckIsContainAllyBase(UnitBase _unitBase, bool _isAlly)
    {
        if(_isAlly)
        {
            return _unitBase.UnitSetting.unitType == UnitType.Unit;
        }
        else
        {
            return (_unitBase.UnitSetting.unitType != UnitType.Unit && _unitBase.UnitSetting.unitType != UnitType.AllyBase);
        }
    }
    #endregion

    #region 델리게이트/이벤트 함수
    public void OnActionShowHP(bool _isToggle)
    {
        if (_isToggle && ObjCanvTutorial.activeSelf)
            return;

        IsShowHPBar = _isToggle;
        ActionShowHP?.Invoke((GameMode == GAME_MODE.Pvp) ? true : _isToggle);
    }
    public void AddActionShowHP(System.Action<bool> _action) => ActionShowHP += _action;
    public void RemoveActionShowHP(System.Action<bool> _action) => ActionShowHP -= _action;
    #endregion

    #region 튜토리얼 관련 함수
    [field: Header("튜토리얼 관련")]
    [field: SerializeField] public GameObject ObjCanvTutorial { get; private set; }
    [field: SerializeField] public Image ImgTutorialMask { get; private set; }
    [field: SerializeField] public Image ImgTutorialRaycastTarget { get; private set; }
    [SerializeField] private RectTransform rtTextArrow;
    [SerializeField] private Sprite[] sprMaskType = new Sprite[2];
    [SerializeField] private RectTransform rtTutorialText;
    [SerializeField] private TextMeshProUGUI tmpTutorialText;

    [SerializeField] private SkeletonGraphic skgFinger;

    [SerializeField] private Button btnLeaveBtn;

    public void SetTutorialTimeScale(bool _isActivated) => Time.timeScale = _isActivated ? 0.00001f : 1.2f;

    public void ShowTutorialTextUI(int _index, ANCHOR_TYPE _anchorType, Vector2 _v2Pivot, Vector2 _v2Pos)
    {
        tmpTutorialText.text = MgrInGameData.Instance.DicLocalizationCSVData[$"Tutorial_{_index:D6}"]["korea"];
        SetRectTransformAnchor(rtTutorialText, _anchorType);
        rtTutorialText.pivot = _v2Pivot;
        rtTutorialText.anchoredPosition = _v2Pos;

        SetTutorialTextArrow();

        rtTutorialText.gameObject.SetActive(true);
    }

    private void SetTutorialTextArrow()
    {
        rtTextArrow.pivot = new Vector2(0.5f, 0.5f);
        rtTextArrow.anchorMin = rtTutorialText.anchorMin;
        rtTextArrow.anchorMax = rtTutorialText.anchorMax;
        rtTextArrow.anchoredPosition = rtTutorialText.anchoredPosition;

        rtTextArrow.gameObject.SetActive(false);

        switch (rtTutorialText.pivot.x)
        {
            case 1.0f: // 우
                rtTextArrow.rotation = Quaternion.Euler(0.0f, 0.0f, -90.0f);
                rtTextArrow.anchoredPosition += new Vector2(50.0f, 0.0f);
                rtTextArrow.gameObject.SetActive(true);
                break;
            case 0.0f: // 좌
                rtTextArrow.rotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
                rtTextArrow.anchoredPosition += new Vector2(-50.0f, 0.0f);
                rtTextArrow.gameObject.SetActive(true);
                break;
        }
        switch (rtTutorialText.pivot.y)
        {
            case 0.0f: // 하
                rtTextArrow.rotation = Quaternion.Euler(0.0f, 0.0f, 180.0f);
                rtTextArrow.anchoredPosition += new Vector2(0.0f, -50.0f);
                rtTextArrow.gameObject.SetActive(true);
                break;
            case 1.0f: // 상
                rtTextArrow.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                rtTextArrow.anchoredPosition += new Vector2(0.0f, 50.0f);
                rtTextArrow.gameObject.SetActive(true);
                break;
        }
    }

    public void ShowTutorialFingerUI(Vector2 _v2Pos, ANCHOR_TYPE _anchorType, int _touchType)
    {
        SetRectTransformAnchor(skgFinger.rectTransform, _anchorType);
        skgFinger.rectTransform.anchoredPosition = _v2Pos;
        skgFinger.AnimationState.SetAnimation(0, $"touch{_touchType}", true);
        skgFinger.gameObject.SetActive(true);
    }

    private CancellationTokenSource token_ToggleRaycast;
    public void ShowTutorialMaskBackGround(Vector2 _v2Pos, Vector2 _v2Size, ANCHOR_TYPE _anchorType, int _maskType, bool _isRaycast = true)
    {
        SetRectTransformAnchor(ImgTutorialMask.rectTransform, _anchorType);
        ImgTutorialMask.rectTransform.anchoredPosition = _v2Pos;
        ImgTutorialMask.rectTransform.sizeDelta = _v2Size;
        ImgTutorialMask.sprite = sprMaskType[_maskType];
        ImgTutorialMask.gameObject.SetActive(true);

        if(_isRaycast)
        {
            token_ToggleRaycast?.Cancel();
            token_ToggleRaycast?.Dispose();
            token_ToggleRaycast = new CancellationTokenSource();
            TutorialRaycastDelay().Forget();
        }
    }

    public void ToggleTutorialUI(bool _isToggle)
    {
        ObjCanvTutorial.SetActive(_isToggle);
        ImgTutorialRaycastTarget.raycastTarget = false;
        //ImgTutorialMask.gameObject.SetActive(_isToggle);
        if (!_isToggle)
        {
            rtTextArrow.gameObject.SetActive(false);
            HideTutorialTextUI();
            HideTutorialFingerUI();
        }
    }
    public void HideTutorialTextUI() => rtTutorialText.gameObject.SetActive(false);
    public void HideTutorialFingerUI() => skgFinger.gameObject.SetActive(false);
    public void HideTutorialMaskBackUI() => ImgTutorialMask.gameObject.SetActive(false);

    private void SetRectTransformAnchor(RectTransform _rt, ANCHOR_TYPE _anchorType)
    {
        switch (_anchorType)
        {
            case ANCHOR_TYPE.CENTER:
                _rt.anchorMin = new Vector2(0.5f, 0.5f);
                _rt.anchorMax = new Vector2(0.5f, 0.5f);
                break;
            case ANCHOR_TYPE.TOP:
                _rt.anchorMin = new Vector2(0.5f, 1.0f);
                _rt.anchorMax = new Vector2(0.5f, 1.0f);
                break;
            case ANCHOR_TYPE.BOTTOM:
                _rt.anchorMin = new Vector2(0.5f, 0.0f);
                _rt.anchorMax = new Vector2(0.5f, 0.0f);
                break;
            case ANCHOR_TYPE.LEFT:
                _rt.anchorMin = new Vector2(0.0f, 0.5f);
                _rt.anchorMax = new Vector2(0.0f, 0.5f);
                break;
            case ANCHOR_TYPE.RIGHT:
                _rt.anchorMin = new Vector2(1.0f, 0.5f);
                _rt.anchorMax = new Vector2(1.0f, 0.5f);
                break;
            case ANCHOR_TYPE.TOP_LEFT:
                _rt.anchorMin = new Vector2(0.0f, 1.0f);
                _rt.anchorMax = new Vector2(0.0f, 1.0f);
                break;
            case ANCHOR_TYPE.TOP_RIGHT:
                _rt.anchorMin = new Vector2(1.0f, 1.0f);
                _rt.anchorMax = new Vector2(1.0f, 1.0f);
                break;
            case ANCHOR_TYPE.BOTTOM_LEFT:
                _rt.anchorMin = new Vector2(0.0f, 0.0f);
                _rt.anchorMax = new Vector2(0.0f, 0.0f);
                break;
            case ANCHOR_TYPE.BOTTOM_RIGHT:
                _rt.anchorMin = new Vector2(1.0f, 0.0f);
                _rt.anchorMax = new Vector2(1.0f, 0.0f);
                break;
        }
    }

    private async UniTaskVoid TutorialRaycastDelay()
    {
        await UniTask.Delay(500, true, cancellationToken: token_ToggleRaycast.Token);
        ImgTutorialRaycastTarget.raycastTarget = true;
    }
    #endregion

    public override void OnDestroy()
    {
        base.OnDestroy();
        token_LowHP?.Cancel();
    }
    
    #region 테스트 용 함수
    // TODO : PVP 테스트용 상대 AI 함수
    public bool CheckIsPvpAllyBaseDistance()
    {
        if (GetAllyBase() is null || GetAllyBase(true) is null)
            return false;
        
        float xDistance = (GetAllyBase(true).transform.position.x - GetAllyBase().transform.position.x);
        
        if (xDistance < 0.0f)
            xDistance = -xDistance;

        if (xDistance < MovePvpLimitDistance)
            return false;

        return true;
    }
    
    private int enemyPvpCost;
    private float pvpMoveTimer = 180.0f;
    public float MovePvpLimitDistance { get; private set; } = 90.0f;
    private UnitBase oppoAlly;
    private async UniTaskVoid TaskEnemyPVPAI()
    {
        enemyPvpCost = Random.Range(6, 9 + 1);
        float bakedTime = 12.75f;
        float tickAITimer = Random.Range(1.0f, 3.0f);
        pvpMoveTimer = 180.0f;
        
        while (isStageStart)
        {
            if (bakedTime > 0.0f)
            {
                bakedTime -= Time.deltaTime;
                if (bakedTime <= 0.0f)
                {
                    enemyPvpCost += Random.Range(6, 9 + 1);
                    if (enemyPvpCost > 25)
                        enemyPvpCost = 25;
                    
                    bakedTime = 12.75f + 4.5f;
                }
            }
            
            if (tickAITimer > 0.0f)
            {
                tickAITimer -= Time.deltaTime;
                if (tickAITimer <= 0.0f)
                {
                    DoPVPAI();
                    tickAITimer = Random.Range(0.2f, 1.5f);
                }
            }

            if (pvpMoveTimer > 0.0f)
            {
                pvpMoveTimer -= Time.deltaTime;
                
                switch (pvpMoveTimer)
                {
                    case <= 0.0f:
                        MovePvpLimitDistance = 20.0f;
                        break;
                    case < 20.0f:
                        MovePvpLimitDistance = 26.0f;
                        break;
                    case < 40.0f:
                        MovePvpLimitDistance = 34.0f;
                        break;
                    case < 60.0f:
                        MovePvpLimitDistance = 42.0f;
                        break;
                    case < 80.0f:
                        MovePvpLimitDistance = 50.0f;
                        break;
                    case < 100.0f:
                        MovePvpLimitDistance = 58.0f;
                        break;
                    case < 120.0f:
                        MovePvpLimitDistance = 66.0f;
                        break;
                    case < 140.0f:
                        MovePvpLimitDistance = 74.0f;
                        break;
                    case < 160.0f:
                        MovePvpLimitDistance = 82.0f;
                        break;
                    default:
                        MovePvpLimitDistance = 90.0f;
                        break;
                }
            }
            
            await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
        }
    }

    private readonly List<string> listPvpSpawnList = new List<string>();
    private void DoPVPAI()
    {
        if (MathLib.CheckPercentage(0.5f))
        {
            listPvpSpawnList.Clear();
            
            foreach (var unitIndex in pvpRandomUnitIndex)
            {
                if (DataManager.Instance.GetUnitInfoData(unitIndex).Cost > enemyPvpCost)
                    continue;
                
                listPvpSpawnList.Add(unitIndex);
            }

            if (listPvpSpawnList.Count > 0)
            {
                float yPos = Random.Range(0.0f, -3.5f);

                int indexNum = Random.Range(0, listPvpSpawnList.Count);
                MgrUnitPool.Instance.ShowObj(listPvpSpawnList[indexNum], 1, new Vector3(oppoAlly.transform.position.x + 1.0f, yPos, yPos * 0.01f));

                enemyPvpCost -= DataManager.Instance.GetUnitInfoData(listPvpSpawnList[indexNum]).Cost;
            }
        }
    }
    #endregion

    #region QA용 함수

    [SerializeField] private GameObject objQABtn;
    [SerializeField] private GameObject objQABtnReRoll;
    [SerializeField] private GameObject objQABtnSkip;
    private bool isToggleAllyBaseGod;

    public void OnBtn_ToggleQAButton()
    {
        objQABtn.SetActive(!objQABtn.activeSelf);
        objQABtnReRoll.SetActive(!objQABtnReRoll.activeSelf);
        objQABtnSkip.SetActive(!objQABtnSkip.activeSelf);
    }

    public void OnBtn_ToggleAllyBaseGod()
    {
        if (GetAllyBase() is null)
            return;

        isToggleAllyBaseGod = !isToggleAllyBaseGod;

        if (isToggleAllyBaseGod)
        {
            MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", GetAllyBase().transform.position).transform.SetParent(GetAllyBase().transform);
            GetAllyBase().AddUnitEffect(UNIT_EFFECT.ETC_GOD, GetAllyBase(), GetAllyBase(), new float[] { 0.0f }, false);
        }
        else GetAllyBase().RemoveUnitEffect(UNIT_EFFECT.ETC_GOD, GetAllyBase(), true);
    }

    public void OnBtn_KillAllyUnit()
    {
        foreach (var unit in ListUnitBase)
        {
            if(unit.UnitSetting.unitType == UnitType.Unit)
                unit.SetUnitState(UNIT_STATE.DEATH);
        }
    }

    public void OnBtn_AddFish()
    {
        MgrBakingSystem.Instance.AddBakedFish(1);
    }

    public void OnBtn_EnemyInstanceDeath()
    {
        foreach (var unit in ListUnitBase)
            if (unit.TeamNum != 0)
                unit.AddUnitEffect(UNIT_EFFECT.ETC_INSTANT_DEATH, unit, unit, null);
    }

    public void OnBtn_EnemyMaxDamage()
    {
        foreach (var unit in ListUnitBase)
            if (unit.TeamNum != 0)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unit, unit, 999999.0f, 0.0f, 1.0f, -1);
    }
    #endregion
}
