using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System.Threading;
using BCH.Database;
using Spine.Unity;

public class MgrBakingSystem : Singleton<MgrBakingSystem>
{
    // GetChild Index 수치 설정
    private const int childImgOutLine = 0;
    private const int childImgFillDough = 1;
    private const int childTmpFill = 2;
    private const int childImgBean = 3;
    private const int childImgWelldone = 4;
    private const int childImgCover = 5;
    private const int childImgCoverLocked = 6;
    private const int childVFXBakingSmoke = 7;
    private const int childSpineGrade = 8;
    private const int childSpineResult = 9;
    private const int childFillDoughVFX = 10;
    private const int childPullOutVFX = 11;
    private const int childObjBakingVFX = 12;

    // 판정 점수 수치 설정
    private const int scoreExcellent = 34;
    private const int scoreGreat = 31;
    private const int scoreGood = 28;
    private const int scoreNotBad = 25;
    private const int scoreBad = 22;

    // 타이쿤 시작/종료 체크
    public bool IsTycoonEnable { get; private set; }

    // 기본 컴포넌트 세팅
    [Header("타이쿤 트레이")]
    [SerializeField] private GameObject objTrayFrame;
    private Transform[] arrTfTray;
    [SerializeField] private GameObject objTrayLine;
    [SerializeField] private RectTransform rtTrayShake;

    [Header("레이저")]
    [SerializeField] private RectTransform rtLaser;

    [Header("판 갈기")]
    [SerializeField] private GameObject objRefreshTray;
    [SerializeField] private TextMeshProUGUI tmpRefreshTimer;
    [SerializeField] private TextMeshProUGUI tmpRefreshAutoTimer;

    [Header("붕어빵 저장소")]
    [SerializeField] private TextMeshProUGUI tmpBakedCnt;
    [SerializeField] private TextMeshProUGUI tmpUpDownCnt;
    [SerializeField] private Image imgStallBakedFish;
    [field: SerializeField] public RectTransform rtMovedPosition { get; private set; }
    [SerializeField] private Image imgBakedX2;

    [Header("스크립터블 오브젝트")]
    [SerializeField] private SOBase_BakedFishImage soBakedFishImage;

    // VFX
    [Header("파티클")]
    [SerializeField] private ParticleSystem parsysDropStuff;
    [field: SerializeField] public ParticleSystem parsysDropStall { get; private set; }
    [SerializeField] private ParticleSystem parsysClearTray;

    // 붕어빵 프레임 관련 변수
    private int currMaxFrame = 9;
    private int currDoMaxFrame; // 타이쿤 시작 기준 최대 틀 갯수 
    private int currCompleteCnt; // 완료된 붕아빵 갯수

    // 타이쿤 관련 진행 변수
    public int TycoonStep { get; private set; } // 타이쿤 스탭
    private int tycoonSubStep; // 타이쿤 서브 스탭 (카운팅 등)
    private int tycoonAutoLevel = 0;
    public bool IsBtnPressed { get; private set; } // 버튼 중복 터치 방지
    //private int missFlipCnt;

    // 타이쿤 관련 캐싱용 변수
    private List<TextMeshProUGUI> listTmp = new List<TextMeshProUGUI>(); // 반죽 % 표기용 TMP 받는 리스트
    private readonly float laserTopYPos = 580.0f; // +-164
    private readonly float topBakeFrameYPos = 491.0f; // +-164
    private List<int> listCanDropLine = new List<int>();
    private List<RectTransform> listRTCover = new List<RectTransform>(); // 붕어빵 덮개 리스트
    private List<BakeCompleteTimer> listComplete = new List<BakeCompleteTimer>(); // 굽기 완료된 리스트
    private Vector3[] arrV3bezier = new Vector3[6];

    // 제작된 붕어빵 갯수
    public int CurrBakedFish { get; private set; }
    public int MaxBakedFish { get; private set; }

    // 붕어빵 판정 (뒤집기는 뒤집은 시간에 따라 내부에서 판정)
    private int[] arrDoughScore = new int[15];
    private int[] arrBeanScore = new int[15];

    // 붕어빵 제작 등급 갯수
    public int[] ArrCurrBakedFishRank { get; private set; } = new int[5];
    public int S_Combo { get; set; }
    public int S_MaxCombo { get; set; }

    // 오토 여부
    [Header("타이쿤 자동 시스템")]
    [SerializeField] private GameObject objTrayRoot;
    private RectTransform rtTrayRoot;
    [SerializeField] private RectTransform rtAutoTray;
    [SerializeField] private CanvasGroup canvgBlockedAuto;

    [Header("자동 진행 연출")]
    [SerializeField] private GameObject objAutoStepRoot;
    private AutoStepIcon[] arrAutoStepIcon;
    [SerializeField] private TextMeshProUGUI tmpDescription;
    [SerializeField] private GameObject objBakedAutoFishStartPos;
    [SerializeField] private GameObject objAutoBakedFish;
    [SerializeField] private GameObject objCompleteImg;
    [SerializeField] private GameObject objAutoRefresh;
    [SerializeField] private BakeResultSpineEvent resultAutoSpine;

    [Header("신규 자동 진행 연출")]
    [SerializeField] private Sprite[] arrSpriteAutoFilled;
    [SerializeField] private Image imgAutoFilledImg;
    [SerializeField] private TextMeshProUGUI tmpAutoLevel;
    [SerializeField] private TextMeshProUGUI tmpMaxCost;

    [Header("자동 온/오프")]
    [SerializeField] private GameObject objAutoToggle;
    private int autoStepCnt;

    [Header("패널티 VFX 효과")]
    [SerializeField] private GameObject objPenaltyVFX;
    [SerializeField] private GameObject objAutoPenaltyVFX;

    [Header("불판 청소 스파인")]
    [SerializeField] private SkeletonDataAsset[] arrSkdaCleanCat;
    [SerializeField] private SkeletonGraphic skgPartTime;

    public struct AutoBoosterExp
    {
        public float TotalExp;
        public float OriginalExp;

        public AutoBoosterExp(float _total, float _original)
        {
            TotalExp = _total;
            OriginalExp = _original;
        }
    }
    private List<AutoBoosterExp> listAutoBoosterExp = new List<AutoBoosterExp>();

    public bool IsAuto { get; private set; }

    // 부스터 관련 효과 변수
    private bool canLucky = false;

    private void Awake()
    {
        arrTfTray = new Transform[objTrayFrame.transform.childCount];
        for (int i = 0; i < arrTfTray.Length; i++)
            arrTfTray[i] = objTrayFrame.transform.GetChild(i);

        rtTrayRoot = objTrayRoot.GetComponent<RectTransform>();

        arrAutoStepIcon = new AutoStepIcon[objAutoStepRoot.transform.childCount];
        for (int i = 0; i < objAutoStepRoot.transform.childCount; i++)
        {
            arrAutoStepIcon[i] = objAutoStepRoot.transform.GetChild(i).GetComponent<AutoStepIcon>();
            arrAutoStepIcon[i].InitStepIcon();
        }

        btnAutoLevel.interactable = false;
        tmpAutoLevelCost.text = requiredAutoLevelCost[0].ToString();
        tmpAutoLevel.text = $"Lv. 1";
        tmpMaxCost.text = $"9";

        InitBakedScore();
        UnlockBakeSystem();
    }

    private void Start()
    {
        MgrInGameEvent.Instance.AddBoosterEvent(BoosterUpgradeEvent);
    }

    public void InitAutoBlockedText()
    {
        int requireAutoChapter = 2;
        canvgBlockedAuto.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = string.Format(MgrInGameData.Instance.DicLocalizationCSVData[$"Unlock_000001"]["korea"], requireAutoChapter);
    }

    public void InitBakingData()
    {
        TaskBakingData().Forget();
    }

    [SerializeField] private CanvasGroup canvgSubText;
    [SerializeField] private TextMeshProUGUI tmpCostSubText;
    private async UniTaskVoid TaskBakingData()
    {
        int maxCnt = 25;
        int currCnt = 0;

        int currIncrease = MgrBattleSystem.Instance.GlobalOption.Option_StartCost;
        int maxIncrease = MgrBattleSystem.Instance.GlobalOption.Option_MaxCost;
        int maxDecrease = 0;

        UserGear gearStove = DataManager.Instance.GetUsingGearInfo(2);
        if (gearStove is not null && gearStove.gearId.Equals("gear_stove_0000"))
        {
            if (gearStove.gearRarity >= 1) maxIncrease += (int)DataManager.Instance.GetGearOptionValue(gearStove.gearId, 0);
            if (gearStove.gearRarity >= 3) currIncrease += (int)DataManager.Instance.GetGearOptionValue(gearStove.gearId, 2);
            if (gearStove.gearRarity >= 10) maxIncrease += (int)DataManager.Instance.GetGearOptionValue(gearStove.gearId, 4);
        }

        if (MgrBattleSystem.Instance.IsChallengeMode && MgrBattleSystem.Instance.ChallengeLevel == 1)
            maxDecrease = 10;

        if(maxIncrease > 0 || maxDecrease > 0 || currIncrease > 0)
        {
            if (maxIncrease > 0 || maxDecrease > 0)
            {
                tmpBakedCnt.rectTransform.DOScale(0.25f, 0.25f);
                tmpBakedCnt.rectTransform.DOAnchorPos(new Vector2(40.0f, -45.0f), 0.25f).SetEase(Ease.Linear);
                tmpCostSubText.text = $"{maxCnt}";
                canvgSubText.DOFade(1.0f, 0.25f).SetDelay(0.125f).SetEase(Ease.Linear);

                await UniTask.Delay(375 + 250, cancellationToken: this.GetCancellationTokenOnDestroy());

                if(maxIncrease > 0)
                {
                    tmpUpDownCnt.color = Color.green;

                    tmpUpDownCnt.rectTransform.anchoredPosition = new Vector2(-5.0f, -15.0f);

                    tmpUpDownCnt.text = $"+{maxIncrease}";
                    tmpUpDownCnt.gameObject.SetActive(true);

                    tmpUpDownCnt.rectTransform.localScale = Vector3.one * 1.0f;
                    tmpUpDownCnt.rectTransform.DOScale(0.5f, 0.125f).SetEase(Ease.InCirc).OnComplete(() =>
                    {
                        tmpUpDownCnt.rectTransform.DOAnchorPosY(75.0f, 0.5f).SetEase(Ease.Linear).SetDelay(0.2f);
                        tmpUpDownCnt.DOFade(0.0f, 0.5f).SetEase(Ease.Linear).SetDelay(0.2f);
                    });

                    await UniTask.Delay(450, cancellationToken: this.GetCancellationTokenOnDestroy());

                    tmpCostSubText.color = Color.green;

                    float perTime = 1.0f / (maxIncrease + 1);
                    while (maxIncrease > 0)
                    {
                        MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Locker_Count_Change", 0.5f);

                        maxCnt++;
                        tmpCostSubText.text = $"{maxCnt}";
                        maxIncrease--;

                        await UniTask.Delay(System.TimeSpan.FromSeconds(perTime), cancellationToken: this.GetCancellationTokenOnDestroy());
                    }

                    await UniTask.Delay(200, cancellationToken: this.GetCancellationTokenOnDestroy());
                }

                if (maxDecrease > 0)
                {
                    tmpUpDownCnt.color = Color.red;

                    tmpUpDownCnt.rectTransform.anchoredPosition = new Vector2(-5.0f, -15.0f);

                    tmpUpDownCnt.text = $"-{maxDecrease}";
                    tmpUpDownCnt.gameObject.SetActive(true);

                    tmpUpDownCnt.rectTransform.localScale = Vector3.one * 1.0f;
                    tmpUpDownCnt.rectTransform.DOScale(0.5f, 0.125f).SetEase(Ease.InCirc).OnComplete(() =>
                    {
                        tmpUpDownCnt.rectTransform.DOAnchorPosY(75.0f, 0.5f).SetEase(Ease.Linear).SetDelay(0.2f);
                        tmpUpDownCnt.DOFade(0.0f, 0.5f).SetEase(Ease.Linear).SetDelay(0.2f);
                    });

                    await UniTask.Delay(450, cancellationToken: this.GetCancellationTokenOnDestroy());

                    tmpCostSubText.color = Color.red;

                    float perTime = 1.0f / (maxDecrease + 1);
                    while (maxDecrease > 0)
                    {
                        MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Locker_Count_Change", 0.5f);

                        maxCnt--;
                        tmpCostSubText.text = $"{maxCnt}";
                        maxDecrease--;

                        await UniTask.Delay(System.TimeSpan.FromSeconds(perTime), cancellationToken: this.GetCancellationTokenOnDestroy());
                    }
                    await UniTask.Delay(200, cancellationToken: this.GetCancellationTokenOnDestroy());
                }

                tmpUpDownCnt.gameObject.SetActive(false);

                await UniTask.Delay(350, cancellationToken: this.GetCancellationTokenOnDestroy());

                canvgSubText.DOFade(0.0f, 0.25f).SetEase(Ease.Linear);
                tmpBakedCnt.rectTransform.DOScale(0.5f, 0.25f).SetDelay(0.125f);
                tmpBakedCnt.rectTransform.DOAnchorPos(new Vector2(50.0f, -47.5f), 0.25f).SetEase(Ease.Linear).SetDelay(0.125f);
                await UniTask.Delay(375 + 250, cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            Sequence seq = DOTween.Sequence();
            while (currIncrease > 0)
            {
                currCnt++;
                tmpBakedCnt.text = $"{currCnt}";
                currIncrease--;

                seq.Kill();
                seq = DOTween.Sequence();
                imgStallBakedFish.color = Color.white;
                imgStallBakedFish.rectTransform.rotation = Quaternion.identity;
                imgStallBakedFish.rectTransform.anchoredPosition = new Vector3(0.0f, 400.0f);
                seq.Append(imgStallBakedFish.rectTransform.DOAnchorPosY(0.0f, 0.1f).SetEase(Ease.Linear));
                seq.Join(imgStallBakedFish.rectTransform.DORotate(new Vector3(0.0f, 0.0f, 720.0f), 0.1f, RotateMode.FastBeyond360).SetEase(Ease.Linear));
                //seq.Join(imgStallBakedFish.DOFade(0.0f, 0.2f).SetEase(Ease.OutCirc));
                seq.OnComplete(() => {
                    MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Locker_Cost", 0.5f);
                    imgStallBakedFish.color = Color.clear;
                    parsysDropStall.Play();
                });

                await UniTask.Delay(125, cancellationToken: this.GetCancellationTokenOnDestroy());
            }
        }

        // 실제 값 세팅
        MaxBakedFish = 25 + MgrBattleSystem.Instance.GlobalOption.Option_MaxCost;
        AddBakedFish(MgrBattleSystem.Instance.GlobalOption.Option_StartCost);
        
        if(MgrBattleSystem.Instance.GameMode == GAME_MODE.Farm)
            AddBakedFish(12);
        
        tycoonAutoLevel = 0;
        if (tycoonAutoLevel < 5)
            btnAutoLevel.interactable = requiredAutoLevelCost[tycoonAutoLevel] <= CurrBakedFish;

        //UserGear gearStove = DataManager.Instance.GetUsingGearInfo(2);
        if (gearStove is not null && gearStove.gearId.Equals("gear_stove_0000"))
        {
            if (gearStove.gearRarity >= 1) MaxBakedFish += (int)DataManager.Instance.GetGearOptionValue(gearStove.gearId, 0);
            if (gearStove.gearRarity >= 3) AddBakedFish((int)DataManager.Instance.GetGearOptionValue(gearStove.gearId, 2));
            if (gearStove.gearRarity >= 10) MaxBakedFish += (int)DataManager.Instance.GetGearOptionValue(gearStove.gearId, 4);
        }

        if (MgrBattleSystem.Instance.IsChallengeMode && MgrBattleSystem.Instance.ChallengeLevel == 1)
            MaxBakedFish -= (int)DataManager.Instance.GetChallengePenaltyData("penalty_000003").Param[0];

        if (MgrBattleSystem.Instance.ChapterID > 0)
            TaskAutoFishBaked(0).Forget();
    }
    
    private void RunTutorialBaking() => TaskAutoFishBaked(0).Forget();

    private void BoosterUpgradeEvent(string _index)
    {
        switch(_index)
        {
            case "skill_passive_009":
                canLucky = true;
                break;
            case "skill_passive_011":
                currMaxFrame = 9 + MgrBoosterSystem.Instance.DicEtc[_index];
                if (MgrBoosterSystem.Instance.DicEtc[_index] >= 5)
                    currMaxFrame++;
                
                tmpMaxCost.text = $"{currMaxFrame}";
                UnlockBakeSystem();
                break;
            default:
                break;
        }
    }

    private readonly int[] requiredAutoLevelCost = new int[5] { 5, 7, 10, 15, 20 };
    [SerializeField] private Button btnAutoLevel;
    [SerializeField] private TextMeshProUGUI tmpAutoLevelCost;
    public void OnBtn_BakedAutoLevel()
    {
        if ((MgrBattleSystem.Instance.ChapterID == 0 && (MgrBattleSystem.Instance.TutorialStep <= 8 && MgrBattleSystem.Instance.TutorialStep != 5)) || tycoonAutoLevel >= 5 || requiredAutoLevelCost[tycoonAutoLevel] > CurrBakedFish)
            return;

        if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.TutorialStep == 5)
        {
            RunTutorialBaking();
            MgrBattleSystem.Instance.TutorialStep = 6;
            MgrBattleSystem.Instance.SetTutorialTimeScale(false);
            MgrBattleSystem.Instance.ToggleTutorialUI(false);
        }

        UseBakedFish(requiredAutoLevelCost[tycoonAutoLevel]);
        tycoonAutoLevel++;

        if (tycoonAutoLevel < 5)
        {
            tmpAutoLevelCost.text = requiredAutoLevelCost[tycoonAutoLevel].ToString();
            tmpAutoLevel.text = $"Lv. {tycoonAutoLevel + 1}";
        }
        else
        {
            tmpAutoLevelCost.transform.parent.gameObject.SetActive(false);
            tmpAutoLevel.text = $"MAX";
            btnAutoLevel.interactable = false;
        }
    }

    private int GetAutoAbility()
    {
        float result = 100.0f;

        // if (MgrBoosterSystem.Instance.DicEtc.TryGetValue("skill_passive_010", out int level))
        //     result += 100.0f * (float)DataManager.Instance.GetBoosterSkillData($"skill_passive_010_{level - 1}").Params[0];

        result += tycoonAutoLevel * 10.0f;

        result += MgrBattleSystem.Instance.GlobalOption.Option_AutoAbility * 100.0f;

        UserGear gear = DataManager.Instance.GetUsingGearInfo(2);
        if (gear is not null)
        {
            if (gear.gearRarity >= 2) result += (float)DataManager.Instance.GetGearOptionValue(gear.gearId, 1) * 100.0f;
            if (gear.gearRarity >= 6) result += (float)DataManager.Instance.GetGearOptionValue(gear.gearId, 3) * 100.0f;
        }

        if (result > 200.0f) result = 200.0f;
        if (result < 100.0f) result = 100.0f;

        return (int)result;
    }

    /// <summary>
    /// 현재 활성화된 프레임 갯수에 따른 활성/비활성 처리
    /// </summary>
    private void UnlockBakeSystem()
    {
        int childCnt = arrTfTray.Length;
        for (int i = 0; i < childCnt; i++)
            arrTfTray[i].GetChild(childImgCoverLocked).gameObject.SetActive(i >= currMaxFrame);
    }

    /// <summary>
    /// 점수 시스템 초기화
    /// </summary>
    private void InitBakedScore()
    {
        for (int i = 0; i < arrDoughScore.Length; i++)
            arrDoughScore[i] = 0;

        for (int i = 0; i < arrBeanScore.Length; i++)
            arrBeanScore[i] = 0;
    }

    public void OnBtn_PointerDownEvent()
    {
        if (IsBtnPressed)
            return;

        IsBtnPressed = true;

        OperateTycoonBehaviour();
    }

    public void OnBtn_PointerUpEvent()
    {
        if (!IsBtnPressed)
            return;

        IsBtnPressed = false;
    }

    private bool isToggleAutoCoolDown = false;
    public void OnBtn_ToggleAuto()
    {
        if (isToggleAutoCoolDown || MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf) // 자동 전환 쿨타임 중에는 안내도 띄우지 않도록
            return;

        if ((MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter) || DataManager.Instance.UserInventory.userCurrentChapter < 2) // 튜토리얼 또는 클리어 챕터가 2챕터 미만인 경우 오토 사용 불가
        {
            canvgBlockedAuto.alpha = 1.0f;
            canvgBlockedAuto.DOKill();
            canvgBlockedAuto.DOFade(0.0f, 1.0f).SetEase(Ease.Linear).SetDelay(2.0f);
            return;
        }

        IsAuto = !IsAuto;
        isToggleAutoCoolDown = true;
        ToggleAuto();
        TaskToggleAutoCoolDown().Forget();
    }
    
    public void OnBtn_ToggleUnitAutoSpawn()
    {
        if (isToggleAutoCoolDown || MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf) // 자동 전환 쿨타임 중에는 안내도 띄우지 않도록
            return;

        // if ((MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter) || DataManager.Instance.UserInventory.userCurrentChapter < 2) // 튜토리얼 또는 클리어 챕터가 2챕터 미만인 경우 오토 사용 불가
        // {
        //     canvgBlockedAuto.alpha = 1.0f;
        //     canvgBlockedAuto.DOKill();
        //     canvgBlockedAuto.DOFade(0.0f, 1.0f).SetEase(Ease.Linear).SetDelay(2.0f);
        //     return;
        // }

        IsAuto = !IsAuto;
        isToggleAutoCoolDown = true;
        
        int currChildIndex = IsAuto ? 1 : 0;
        int prevChildIndex = IsAuto ? 0 : 1;
        objAutoToggle.transform.GetChild(currChildIndex).gameObject.SetActive(true);
        objAutoToggle.transform.GetChild(prevChildIndex).gameObject.SetActive(false);
        
        TaskToggleAutoCoolDown().Forget();
    }

    private async UniTaskVoid TaskToggleAutoCoolDown()
    {
        await UniTask.Delay(750, cancellationToken: this.GetCancellationTokenOnDestroy());
        isToggleAutoCoolDown = false;
    }

    private CancellationTokenSource token_SwitchAuto;
    private void ToggleAuto()
    {
        // 토클 이미지 변경
        int currChildIndex = IsAuto ? 1 : 0;
        int prevChildIndex = IsAuto ? 0 : 1;
        objAutoToggle.transform.GetChild(currChildIndex).gameObject.SetActive(true);
        objAutoToggle.transform.GetChild(prevChildIndex).gameObject.SetActive(false);

        rtAutoTray.DOKill();
        rtAutoTray.DOAnchorPosY(IsAuto ? 20.0f : -400.0f, 0.25f).SetEase(Ease.OutBack, 0.75f);

        rtTrayRoot.DOKill();
        rtTrayRoot.DOAnchorPosX(IsAuto ? 800.0f : 0.0f, 0.25f).SetEase(Ease.OutBack, 0.75f);

        if (IsAuto)
        {
            token_SwitchAuto?.Cancel();

            if (TycoonStep == 4)
                return;

            foreach (var cover in token_cover)
                cover?.Cancel();

            int stepCnt = 0;
            if (TycoonStep == 1) stepCnt = 1;
            if (TycoonStep == 2) stepCnt = 2;
            if (TycoonStep == 3)
            {
                Step4ToAutoCheck();
                stepCnt = 3;
            }
            TaskAutoFishBaked(stepCnt).Forget();
        }
        else
        {
            if (TycoonStep <= 4)
                AutoToChangedManual(TycoonStep);

            token_SwitchAuto?.Cancel();
            token_SwitchAuto?.Dispose();
            token_SwitchAuto = new CancellationTokenSource();

            switch (TycoonStep)
            {
                case 1:
                    TaskUpdate_Step2(true).Forget();
                    break;
                case 2:
                    TaskUpdate_Step3(true).Forget();
                    break;
                case 3:
                    TaskUpdate_Step4(true).Forget();
                    break;
                case 0:
                case 4:
                    break;
                default:
                    Debug.LogError($"타이쿤 스탭 에러 : {TycoonStep}");
                    break;
            }
        }
    }

    private void OperateTycoonBehaviour()
    {
        if (MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf && MgrBattleSystem.Instance.ImgTutorialMask.gameObject.activeSelf)
            return;

        if ((MgrBattleSystem.Instance.TutorialStep == 15 && MgrBattleSystem.Instance.TutorialSubStep == 0) || MgrBattleSystem.Instance.TutorialStep == 20)
            return;

        switch (TycoonStep)
        {
            case 0:
                if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter)
                {
                    if(MgrBattleSystem.Instance.TutorialStep <= 3 && MgrBattleSystem.Instance.TutorialStep != 0)
                        return;
                }

                if(!IsTycoonEnable)
                {
                    currDoMaxFrame = currMaxFrame;
                    IsTycoonEnable = true;
                }
                TaskDoughFill_Step1().Forget();
                break;
            case 1:
                if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter)
                {
                    if (MgrBattleSystem.Instance.TutorialStep == 4 || (MgrBattleSystem.Instance.TutorialStep == 6 && MgrBattleSystem.Instance.TutorialSubStep == 1))
                        return;
                }

                DropStuff_Step2().Forget();
                break;
            case 3:
                rtTrayShake.DOKill();
                rtTrayShake.anchoredPosition = new Vector2(-381.0f, 806.0f);
                rtTrayShake.DOShakeAnchorPos(0.1f, 8, 10);

                if (listComplete.Count == 0)
                    return;

                FlipBake_Step4().Forget();
                break;
            case 2:
            case 4:
                break;
            default:
                Debug.LogError($"타이쿤 스탭 에러 : {TycoonStep}");
                break;
        }
    }

    #region 타이쿤 스탭별 함수
    // 자동
    private Sequence autoSeq;
    // private async UniTaskVoid TaskAutoFishBaked(int _autoIndex)
    // {
    //     //await UniTask.WaitUntil(() => (!IsTycoonEnable && TycoonStep == 0 && tycoonSubStep == 0), cancellationToken: this.GetCancellationTokenOnDestroy());
    //
    //     IsTycoonEnable = true;
    //
    //     foreach (var icon in arrAutoStepIcon)
    //         icon.InitStepIcon();
    //
    //     autoStepCnt = _autoIndex;
    //     TycoonStep = 0;
    //     tycoonSubStep = 0;
    //
    //     currDoMaxFrame = _autoIndex == 0 ? currMaxFrame : currDoMaxFrame;
    //     int autoMaxFrame = currDoMaxFrame - currCompleteCnt;
    //
    //     if (autoStepCnt == 3 && autoMaxFrame == 0)
    //     {
    //         autoStepCnt = 4;
    //         TycoonStep = 4;
    //         tmpDescription.text = string.Empty;
    //         ChallengePenalty(true);
    //         TaskRefresh_Step5(true).Forget();
    //         return;
    //     }
    //
    //     int addRow = Mathf.CeilToInt((float)currDoMaxFrame / 3.0f) - 3;
    //
    //     float bakedTimeMultiply = 1.0f / (1.0f + (GetAutoAbility() - 100) * 0.004f);
    //
    //     float[] autoBakedStepTime = new float[] { 2.65f * bakedTimeMultiply, (2.65f + addRow * 0.44f) * bakedTimeMultiply, 4.25f * bakedTimeMultiply, (4.43f + ((autoMaxFrame < 9 ? 9 : autoMaxFrame) - 9) * 0.15f) * (autoMaxFrame / (float)currDoMaxFrame) * bakedTimeMultiply };
    //     float startBakedFishTime = autoBakedStepTime[0] + autoBakedStepTime[1] + autoBakedStepTime[2];
    //     float totalBakedTime = autoBakedStepTime[0] + autoBakedStepTime[1] + autoBakedStepTime[2] + autoBakedStepTime[3];
    //     float bakedTime = 0.0f;
    //     int currStepIconIndex = 0;
    //
    //     tmpDescription.text = $"반죽을 채우는 중입니다.";
    //
    //     if (autoStepCnt == 1)
    //     {
    //         bakedTime = autoBakedStepTime[0];
    //         arrAutoStepIcon[0].SetComplete();
    //         currStepIconIndex = 1;
    //         TycoonStep = 1;
    //
    //         tmpDescription.text = $"붕어빵 소를 떨어뜨리는 중입니다.";
    //     }
    //     if (autoStepCnt == 2)
    //     {
    //         bakedTime = autoBakedStepTime[0] + autoBakedStepTime[1];
    //         arrAutoStepIcon[0].SetComplete();
    //         arrAutoStepIcon[1].SetComplete();
    //         currStepIconIndex = 2;
    //         TycoonStep = 2;
    //
    //         tmpDescription.text = $"붕어빵을 맛있게 굽는 중입니다.";
    //     }
    //     if (autoStepCnt == 3)
    //     {
    //         bakedTime = autoBakedStepTime[0] + autoBakedStepTime[1] + autoBakedStepTime[2];
    //         arrAutoStepIcon[0].SetComplete();
    //         arrAutoStepIcon[1].SetComplete();
    //         currStepIconIndex = 2;
    //         TycoonStep = 3;
    //
    //         tmpDescription.text = $"붕어빵을 맛있게 굽는 중입니다.";
    //     }
    //     if (autoStepCnt == 4)
    //     {
    //         tmpDescription.text = string.Empty;
    //         return;
    //     }
    //
    //     MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Auto_Progress", 1.0f);
    //
    //     listAutoBoosterExp.Clear();
    //
    //     int prevBakedCnt = 0;
    //     while (bakedTime < totalBakedTime && IsAuto && MgrBattleSystem.Instance.isStageStart)
    //     {
    //         bakedTime += Time.deltaTime;
    //
    //         float progressRate = 0.0f;
    //         if (autoStepCnt == 0)
    //         {
    //             progressRate = bakedTime / autoBakedStepTime[0];
    //             arrAutoStepIcon[currStepIconIndex].SetProgress(progressRate);
    //
    //             if (progressRate >= 1.0f)
    //             {
    //                 MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Auto_Complete", 1.0f);
    //                 MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Auto_Progress", 1.0f);
    //
    //                 arrAutoStepIcon[currStepIconIndex].SetComplete();
    //                 TycoonStep++;
    //                 currStepIconIndex++;
    //                 autoStepCnt++;
    //                 tmpDescription.text = $"붕어빵 소를 떨어뜨리는 중입니다.";
    //
    //                 for (int i = 0; i < currDoMaxFrame; i++)
    //                     arrDoughScore[i] = GetAutoScore();
    //             }
    //         }
    //         else if (autoStepCnt == 1)
    //         {
    //             progressRate = (bakedTime - autoBakedStepTime[0]) / autoBakedStepTime[1];
    //             arrAutoStepIcon[currStepIconIndex].SetProgress(progressRate);
    //
    //             if (progressRate >= 1.0f)
    //             {
    //                 MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Auto_Complete", 1.0f);
    //                 MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Auto_Progress", 1.0f);
    //
    //                 arrAutoStepIcon[currStepIconIndex].SetComplete();
    //                 TycoonStep++;
    //                 currStepIconIndex++;
    //                 autoStepCnt++;
    //                 tmpDescription.text = $"붕어빵을 맛있게 굽는 중입니다.";
    //
    //                 for (int i = 0; i < currDoMaxFrame; i++)
    //                     arrBeanScore[i] = GetAutoScore();
    //             }
    //         }
    //         else if (autoStepCnt == 2)
    //         {
    //             float localProgressRate = (bakedTime - autoBakedStepTime[0] - autoBakedStepTime[1]) / autoBakedStepTime[2];
    //             progressRate = (bakedTime - autoBakedStepTime[0] - autoBakedStepTime[1]) / (autoBakedStepTime[2] + autoBakedStepTime[3]);
    //             arrAutoStepIcon[currStepIconIndex].SetProgress(progressRate);
    //
    //             if (localProgressRate >= 1.0f)
    //             {
    //                 autoStepCnt++;
    //                 TycoonStep++;
    //             }
    //         }
    //         else if (autoStepCnt == 3)
    //         {
    //             progressRate = (bakedTime - autoBakedStepTime[0] - autoBakedStepTime[1]) / (autoBakedStepTime[2] + autoBakedStepTime[3]);
    //             arrAutoStepIcon[currStepIconIndex].SetProgress(progressRate);
    //
    //             if (progressRate >= 1.0f)
    //             {
    //                 MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Auto_Complete", 1.0f);
    //
    //                 autoStepCnt++;
    //                 TycoonStep++;
    //                 arrAutoStepIcon[currStepIconIndex].SetComplete();
    //                 tmpDescription.text = string.Empty;
    //                 objCompleteImg.SetActive(true);
    //                 objCompleteImg.transform.DOKill();
    //                 objCompleteImg.transform.localScale = Vector3.one * 2.0f;
    //                 objCompleteImg.transform.DOScale(1.0f, 0.125f).SetEase(Ease.OutCubic);
    //             }
    //         }
    //
    //         int currBakedCnt = (int)Mathf.Lerp(0.0f, (float)autoMaxFrame, (bakedTime - startBakedFishTime) / (totalBakedTime - startBakedFishTime));
    //         float fishFXLimitTime = ((totalBakedTime - startBakedFishTime) / autoMaxFrame) - 0.1f;
    //         if (fishFXLimitTime > 0.25f) fishFXLimitTime = 0.25f;
    //         if (fishFXLimitTime < 0.125f) fishFXLimitTime = 0.125f;
    //
    //         if (prevBakedCnt != currBakedCnt)
    //         {
    //             prevBakedCnt = currBakedCnt;
    //
    //             currCompleteCnt++;
    //
    //             int frameIndex = -1;
    //             for (int i = 0; i < currDoMaxFrame; i++)
    //             {
    //                 if (arrDoughScore[i] > 0)
    //                 {
    //                     frameIndex = i;
    //                     break;
    //                 }
    //             }
    //
    //             // 최종 판정
    //             int finalScore = GetAutoScore();
    //             finalScore += arrDoughScore[frameIndex] <= 0.0f ? GetAutoScore() : arrDoughScore[frameIndex];
    //             finalScore += arrBeanScore[frameIndex] <= 0.0f ? GetAutoScore() : arrBeanScore[frameIndex];
    //
    //             string resultGrade = GetAutoFinalGrade(finalScore);
    //             AddRankBakedFish(resultGrade);
    //             resultAutoSpine.SetResult(resultGrade);
    //
    //             if(resultGrade.Equals("S")) MgrSound.Instance.PlayOneShotSFX($"SFX_Fish_Bread_Rank_{resultGrade}", 1.0f);
    //             else if(resultGrade.Equals("F")) MgrSound.Instance.PlayOneShotSFX($"SFX_Fish_Bread_Rank_{resultGrade}", 1.0f);
    //             else MgrSound.Instance.PlayOneShotSFX($"SFX_Fish_Bread_Rank_ABC", 1.0f);
    //
    //             arrDoughScore[frameIndex] = 0;
    //             arrBeanScore[frameIndex] = 0;
    //
    //             // 최종 판정에 따른 부스터 EXP
    //             float originalExp = 0.0f;
    //             float boosterAmount = GetResultToBoosterExp(resultGrade, ref originalExp);
    //
    //             if (boosterAmount > 0.0f)
    //                 listAutoBoosterExp.Add(new AutoBoosterExp(boosterAmount, originalExp));
    //
    //             objAutoBakedFish.GetComponent<Image>().color = Color.white;
    //             objAutoBakedFish.transform.localPosition = Vector3.zero;
    //             objAutoBakedFish.transform.localScale = Vector3.one;
    //             objAutoBakedFish.transform.rotation = Quaternion.identity;
    //             objAutoBakedFish.SetActive(true);
    //
    //             if (boosterAmount > 0.0f)
    //             {
    //                 // DoTween Path 경로 지정
    //                 Vector3 v3Dir = (rtMovedPosition.position - objAutoBakedFish.transform.position).normalized;
    //                 float distance = (rtMovedPosition.position - objAutoBakedFish.transform.position).magnitude;
    //                 Vector3 v3Way0 = objAutoBakedFish.transform.position + (distance * 0.5f * v3Dir) + Quaternion.Euler(0, 0, -90.0f) * v3Dir * 1.5f;
    //
    //                 arrV3bezier[0] = v3Way0;
    //                 arrV3bezier[1] = objAutoBakedFish.transform.position + Vector3.right;
    //                 arrV3bezier[2] = v3Way0 - v3Dir;
    //                 arrV3bezier[3] = rtMovedPosition.position;
    //                 arrV3bezier[4] = v3Way0 + v3Dir;
    //                 arrV3bezier[5] = rtMovedPosition.position + Vector3.up;
    //
    //                 // DoTween
    //                 autoSeq.Kill();
    //                 autoSeq = DOTween.Sequence();
    //                 autoSeq.Append(objAutoBakedFish.transform.DOPath(arrV3bezier, fishFXLimitTime, PathType.CubicBezier).SetEase(Ease.InSine).OnComplete(() => {
    //                     objAutoBakedFish.gameObject.SetActive(false);
    //                     parsysDropStall.Play();
    //
    //                     int addBaked = 1;
    //                     if (canLucky)
    //                     {
    //                         if (Random.Range(0.001f, 1.0f) <= (float)DataManager.Instance.GetBoosterSkillData($"skill_passive_009_{MgrBoosterSystem.Instance.DicEtc["skill_passive_009"] - 1}").Params[0])
    //                         {
    //                             MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Double", 0.5f);
    //
    //                             imgBakedX2.DOKill();
    //                             imgBakedX2.rectTransform.DOKill();
    //
    //                             imgBakedX2.color = Color.white;
    //                             imgBakedX2.rectTransform.anchoredPosition = Vector2.zero;
    //                             imgBakedX2.rectTransform.DOAnchorPosY(50.0f, 0.5f);
    //                             imgBakedX2.DOFade(0.0f, 0.5f);
    //
    //                             addBaked = 2;
    //                         }
    //                     }
    //                     AddBakedFish(addBaked);
    //
    //                     MgrBoosterSystem.Instance.AddBoosterExp(listAutoBoosterExp[0].TotalExp, listAutoBoosterExp[0].OriginalExp);
    //                     listAutoBoosterExp.RemoveAt(0);
    //                 }));
    //                 autoSeq.Join(objAutoBakedFish.transform.DORotate(new Vector3(0.0f, 0.0f, Random.Range(180.0f, 450.0f)), fishFXLimitTime, RotateMode.FastBeyond360));
    //                 autoSeq.Join(objAutoBakedFish.transform.DOScale(Vector3.one * 0.5f, fishFXLimitTime));
    //             }
    //             else
    //             {
    //                 Image imgAutoBakedFish = objAutoBakedFish.GetComponent<Image>();
    //
    //                 autoSeq.Kill();
    //                 autoSeq = DOTween.Sequence();
    //
    //                 autoSeq.Append(objAutoBakedFish.transform.DOLocalMoveY(200.0f, fishFXLimitTime).SetEase(Ease.Linear).OnComplete(() =>
    //                 {
    //                     objAutoBakedFish.gameObject.SetActive(false);
    //                 }));
    //                 autoSeq.Join(objAutoBakedFish.transform.DORotate(new Vector3(0.0f, 0.0f, Random.Range(180.0f, 450.0f)), fishFXLimitTime, RotateMode.FastBeyond360));
    //                 autoSeq.Join(objAutoBakedFish.transform.DOScale(Vector3.one * 0.5f, fishFXLimitTime));
    //                 autoSeq.Join(imgAutoBakedFish.DOFade(0.0f, fishFXLimitTime).SetEase(Ease.Linear));
    //             }
    //         }
    //
    //         await UniTask.Yield(this.GetCancellationTokenOnDestroy());
    //     }
    //
    //     if (!IsAuto || autoStepCnt < 4 || !MgrBattleSystem.Instance.isStageStart)
    //         return;
    //
    //     await UniTask.WaitUntil(() => currDoMaxFrame == currCompleteCnt, cancellationToken: this.GetCancellationTokenOnDestroy());
    //
    //     await UniTask.Delay(250, cancellationToken: this.GetCancellationTokenOnDestroy());
    //
    //     ChallengePenalty(true);
    //
    //     await UniTask.Delay(250, cancellationToken: this.GetCancellationTokenOnDestroy());
    //
    //     TaskRefresh_Step5(true).Forget();
    // }

    private async UniTaskVoid TaskAutoFishBaked(int _autoIndex)
    {
        IsTycoonEnable = true;

        imgAutoFilledImg.sprite = arrSpriteAutoFilled[0];
        imgAutoFilledImg.fillAmount = 0.0f;
        
        TycoonStep = 0;
        tycoonSubStep = 0;
        
        currDoMaxFrame = currMaxFrame;
        //tmpMaxCost.text = $"{currDoMaxFrame}";
        tmpMaxCost.text = $"{currMaxFrame}";
        currCompleteCnt = 0;
        
        int addRow = Mathf.CeilToInt((float)currDoMaxFrame / 3.0f) - 3;
        
        float bakedTimeMultiply = 1.0f / (1.0f + (GetAutoAbility() - 100) * 0.004f);
        
        float[] autoBakedStepTime = new float[] { 2.65f * bakedTimeMultiply * 0.85f,
            (2.65f + addRow * 0.44f) * bakedTimeMultiply * 0.85f,
            4.25f * bakedTimeMultiply * 0.85f,
            (4.43f + currDoMaxFrame * 0.15f) * bakedTimeMultiply * 0.85f
        };
        float startBakedFishTime = autoBakedStepTime[0] + autoBakedStepTime[1] + autoBakedStepTime[2];
        float totalBakedTime = autoBakedStepTime[0] + autoBakedStepTime[1] + autoBakedStepTime[2] + autoBakedStepTime[3];
        float bakedTime = 0.0f;
        
        int prevBakedCnt = 0;
        while (bakedTime < totalBakedTime && MgrBattleSystem.Instance.isStageStart)
        {
            bakedTime += Time.deltaTime;
        
            float progressRate = bakedTime / totalBakedTime;
            imgAutoFilledImg.fillAmount = Mathf.Lerp(0.0f, 1.0f, progressRate);
            
            if (progressRate >= 1.0f)
            {
                if (TycoonStep == 0)
                {
                    MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Auto_Complete", 1.0f);
                    TycoonStep++;
                }
            }
            
            int currBakedCnt = (int)Mathf.Lerp(0.0f, (float)currDoMaxFrame, (bakedTime - startBakedFishTime) / (totalBakedTime - startBakedFishTime));
            float fishFXLimitTime = ((totalBakedTime - startBakedFishTime) / currDoMaxFrame) - 0.1f;
            if (fishFXLimitTime > 0.25f) fishFXLimitTime = 0.25f;
            if (fishFXLimitTime < 0.125f) fishFXLimitTime = 0.125f;
        
            if (prevBakedCnt != currBakedCnt)
            {
                prevBakedCnt = currBakedCnt;
        
                currCompleteCnt++;
        
                // 최종 판정
                string resultGrade = GetAutoGrade();
                AddRankBakedFish(resultGrade);
                resultAutoSpine.SetResult(resultGrade);
        
                if(resultGrade.Equals("S")) MgrSound.Instance.PlayOneShotSFX($"SFX_Fish_Bread_Rank_{resultGrade}", 1.0f);
                else if(resultGrade.Equals("F")) MgrSound.Instance.PlayOneShotSFX($"SFX_Fish_Bread_Rank_{resultGrade}", 1.0f);
                else MgrSound.Instance.PlayOneShotSFX($"SFX_Fish_Bread_Rank_ABC", 1.0f);
        
                // 최종 판정에 따른 부스터 EXP
                float originalExp = 0.0f;
                float boosterAmount = GetResultToBoosterExp(resultGrade, ref originalExp);
        
                if (boosterAmount > 0.0f)
                    listAutoBoosterExp.Add(new AutoBoosterExp(boosterAmount, originalExp));
        
                objAutoBakedFish.GetComponent<Image>().color = Color.white;
                objAutoBakedFish.transform.localPosition = Vector3.zero;
                objAutoBakedFish.transform.localScale = Vector3.one;
                objAutoBakedFish.transform.rotation = Quaternion.identity;
                objAutoBakedFish.SetActive(true);
        
                if (boosterAmount > 0.0f)
                {
                    // DoTween Path 경로 지정
                    Vector3 v3Dir = (objAutoBakedFish.transform.position - objAutoBakedFish.transform.position).normalized;
                    float distance = (objAutoBakedFish.transform.position - objAutoBakedFish.transform.position).magnitude;
                    Vector3 v3Way0 = objAutoBakedFish.transform.position + (distance * 0.5f * v3Dir) + Quaternion.Euler(0, 0, 90.0f) * v3Dir * 1.5f;
        
                    arrV3bezier[0] = v3Way0;
                    arrV3bezier[1] = objAutoBakedFish.transform.position + Vector3.left;
                    arrV3bezier[2] = v3Way0 - v3Dir;
                    arrV3bezier[3] = rtMovedPosition.position;
                    arrV3bezier[4] = v3Way0 + v3Dir;
                    arrV3bezier[5] = rtMovedPosition.position + Vector3.up;
        
                    // DoTween
                    autoSeq.Kill();
                    autoSeq = DOTween.Sequence();
                    autoSeq.Append(objAutoBakedFish.transform.DOPath(arrV3bezier, fishFXLimitTime, PathType.CubicBezier).SetEase(Ease.InSine).OnComplete(() => {
                        objAutoBakedFish.gameObject.SetActive(false);
                        parsysDropStall.Play();
        
                        int addBaked = 1;
                        if (canLucky)
                        {
                            if (Random.Range(0.001f, 1.0f) <= (float)DataManager.Instance.GetBoosterSkillData($"skill_passive_009_{MgrBoosterSystem.Instance.DicEtc["skill_passive_009"] - 1}").Params[0])
                            {
                                MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Double", 0.5f);
        
                                imgBakedX2.DOKill();
                                imgBakedX2.rectTransform.DOKill();
        
                                imgBakedX2.color = Color.white;
                                imgBakedX2.rectTransform.anchoredPosition = Vector2.zero;
                                imgBakedX2.rectTransform.DOAnchorPosY(50.0f, 0.5f);
                                imgBakedX2.DOFade(0.0f, 0.5f);
        
                                addBaked = 2;
                            }
                        }
                        AddBakedFish(addBaked);
        
                        MgrBoosterSystem.Instance.AddBoosterExp(listAutoBoosterExp[0].TotalExp, listAutoBoosterExp[0].OriginalExp);
                        listAutoBoosterExp.RemoveAt(0);
                    }));
                    autoSeq.Join(objAutoBakedFish.transform.DORotate(new Vector3(0.0f, 0.0f, Random.Range(180.0f, 450.0f)), fishFXLimitTime, RotateMode.FastBeyond360));
                    autoSeq.Join(objAutoBakedFish.transform.DOScale(Vector3.one * 0.5f, fishFXLimitTime));
                }
                else
                {
                    Image imgAutoBakedFish = objAutoBakedFish.GetComponent<Image>();
        
                    autoSeq.Kill();
                    autoSeq = DOTween.Sequence();
        
                    autoSeq.Append(objAutoBakedFish.transform.DOLocalMoveY(200.0f, fishFXLimitTime).SetEase(Ease.Linear).OnComplete(() =>
                    {
                        objAutoBakedFish.gameObject.SetActive(false);
                    }));
                    autoSeq.Join(objAutoBakedFish.transform.DORotate(new Vector3(0.0f, 0.0f, Random.Range(180.0f, 450.0f)), fishFXLimitTime, RotateMode.FastBeyond360));
                    autoSeq.Join(objAutoBakedFish.transform.DOScale(Vector3.one * 0.5f, fishFXLimitTime));
                    autoSeq.Join(imgAutoBakedFish.DOFade(0.0f, fishFXLimitTime).SetEase(Ease.Linear));
                }
            }
        
            await UniTask.Yield(this.GetCancellationTokenOnDestroy());
        }
        
        if (!MgrBattleSystem.Instance.isStageStart)
            return;
        
        await UniTask.WaitUntil(() => currDoMaxFrame == currCompleteCnt, cancellationToken: this.GetCancellationTokenOnDestroy());
        
        await UniTask.Delay(250, cancellationToken: this.GetCancellationTokenOnDestroy());
        
        ChallengePenalty(true);
        
        if (MgrBattleSystem.Instance.TutorialStep == 6)
        {
            await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.SetTutorialTimeScale(true);
            MgrBattleSystem.Instance.ShowTutorialTextUI(6, ANCHOR_TYPE.BOTTOM_LEFT, new Vector2(0.5f, 0.0f), new Vector2(1125.0f, 400.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(1130.0f, 175.0f), new Vector2(250.0f, 250.0f), ANCHOR_TYPE.BOTTOM_LEFT, 1);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.TutorialStep = 7;
            MgrBattleSystem.Instance.SetTutorialTimeScale(false);
            MgrBattleSystem.Instance.ToggleTutorialUI(false);

            MgrBoosterSystem.Instance.AddBoosterExp(460.0f);
            MgrBattleSystem.Instance.SetTutorialInitWave();
        }
        
        await UniTask.Delay(250, cancellationToken: this.GetCancellationTokenOnDestroy());

        IsTycoonEnable = false;
        imgAutoFilledImg.sprite = arrSpriteAutoFilled[1];

        TycoonStep = 0;
        tycoonSubStep = 0;
        
        //tmpMaxCost.text = $"-";

        if(IsAuto)
            TaskAutoUnitSpawn().Forget();

        float duration = 4.5f;
        while (duration > 0.0f)
        {
            duration -= Time.deltaTime;
            imgAutoFilledImg.fillAmount = Mathf.Lerp(0.0f, 1.0f, duration / 4.5f);
            
            await UniTask.Yield(cancellationToken:this.GetCancellationTokenOnDestroy());
        }
        
        if (!MgrBattleSystem.Instance.isStageStart)
            return;

        TaskAutoFishBaked(0).Forget();
    }

    private readonly List<UnitSlotBtn> listCanSpawnSlot = new List<UnitSlotBtn>();
    private async UniTaskVoid TaskAutoUnitSpawn()
    {
        await UniTask.Delay(250);
        
        while (true)
        {
            listCanSpawnSlot.Clear();
            foreach (UnitSlotBtn slot in MgrBattleSystem.Instance.ArrUnitSlotBtn)
            {
                if (string.IsNullOrEmpty(slot.GetUnitIndex()) || slot.GetCurrentCost() > CurrBakedFish || slot.GetCurrentCoolDown() > 0.0f)
                    continue;

                listCanSpawnSlot.Add(slot);
            }

            if (listCanSpawnSlot.Count == 0)
                break;
            
            listCanSpawnSlot[Random.Range(0, listCanSpawnSlot.Count)].OnBtn_SpawnUnit();

            await UniTask.Delay(250, cancellationToken: this.GetCancellationTokenOnDestroy());
        }
    }

    // 붕어빵 반죽 채우기
    private async UniTaskVoid TaskDoughFill_Step1()
    {
        float fillSize = 0.0f;
        float fillSpeed = 1.5f;

        listTmp.Clear();

        if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter && MgrBattleSystem.Instance.TutorialStep == 0)
        {
            MgrBattleSystem.Instance.TutorialStep = 1;
            MgrBattleSystem.Instance.SetTutorialTimeScale(false);
            MgrBattleSystem.Instance.ToggleTutorialUI(false);
        }

        for (int i = 0; i < currDoMaxFrame - currCompleteCnt; i++)
        {
            if (i % 3 != tycoonSubStep)
                continue;

            arrTfTray[i].GetChild(childImgFillDough).gameObject.SetActive(true);
            arrTfTray[i].GetChild(childTmpFill).gameObject.SetActive(true);
            listTmp.Add(arrTfTray[i].GetChild(childTmpFill).GetComponent<TextMeshProUGUI>());
        }

        MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Step1", 1.0f);

        while (IsBtnPressed && !IsAuto)
        {
            fillSize += fillSpeed * Time.deltaTime;

            int listCnt = listTmp.Count;
            for (int i = 0; i < listCnt; i++)
                listTmp[i].text = fillSize >= 1.2f ? "120%" : $"{fillSize * 100.0f:F0}%";

            for (int i = 0; i < currDoMaxFrame - currCompleteCnt; i++)
            {
                if (i % 3 != tycoonSubStep)
                    continue;

                arrTfTray[i].GetChild(childImgFillDough).transform.localScale = Vector3.one * (fillSize <= 1.0f ? Mathf.Lerp(0.5f, 1.0f, fillSize / 1.0f) : Mathf.Lerp(1.0f, 1.1f, (fillSize - 1.0f) / 0.2f));
            }

            if (fillSize >= 1.0f && MgrBattleSystem.Instance.TutorialStep == 1)
            {
                await UniTask.Delay(200, cancellationToken: this.GetCancellationTokenOnDestroy());

                MgrBattleSystem.Instance.SetTutorialTimeScale(true);
                MgrBattleSystem.Instance.ShowTutorialTextUI(1, ANCHOR_TYPE.BOTTOM_RIGHT, new Vector2(1.0f, 0.5f), new Vector2(-750.0f, 925.0f));
                MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(-360.0f, 925.0f), new Vector2(900.0f, 750.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 1, false);
                MgrBattleSystem.Instance.ToggleTutorialUI(true);
                await UniTask.WaitUntil(() => !IsBtnPressed, cancellationToken: this.GetCancellationTokenOnDestroy());
                await UniTask.Delay(200, true, cancellationToken: this.GetCancellationTokenOnDestroy());
                MgrBattleSystem.Instance.SetTutorialTimeScale(false);
                MgrBattleSystem.Instance.ToggleTutorialUI(false);
                MgrBattleSystem.Instance.TutorialStep = 2;
            }

            if (fillSize >= 1.2f)
                break;

            await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        if (IsAuto)
            return;

        for (int i = 0; i < currDoMaxFrame - currCompleteCnt; i++)
        {
            if (i % 3 != tycoonSubStep)
                continue;

            string grade = "Bad";
            if (fillSize <= 0.8f || 1.2f <= fillSize)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Bad", 0.25f);
                arrDoughScore[i] = scoreBad;
            }
            else if (fillSize <= 0.9f || 1.1f <= fillSize)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Not Bad", 0.25f);
                grade = "NotBad";
                arrDoughScore[i] = scoreNotBad;
            }
            else if (fillSize <= 0.95f || 1.05f <= fillSize)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Good", 0.25f);
                grade = "Good";
                arrDoughScore[i] = scoreGood;
            }
            else if (fillSize <= 0.98f || 1.02f <= fillSize)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Great", 0.25f);
                grade = "Great";
                arrDoughScore[i] = scoreGreat;
            }
            else if (fillSize <= 1.01f || 0.99f <= fillSize)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Excellent", 0.25f);
                grade = "Excellent";
                arrDoughScore[i] = scoreExcellent;
            }

            arrTfTray[i].GetChild(childSpineGrade).GetComponent<BakeGradeSpineEvent>().SetGrade(grade);
            arrTfTray[i].GetChild(childFillDoughVFX).GetComponent<ParticleSystem>().Play();
        }

        if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter && (MgrBattleSystem.Instance.TutorialStep == 1 || MgrBattleSystem.Instance.TutorialStep == 2))
        {
            await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.SetTutorialTimeScale(true);
            MgrBattleSystem.Instance.ShowTutorialTextUI(2, ANCHOR_TYPE.BOTTOM_RIGHT, new Vector2(1.0f, 0.5f), new Vector2(-750.0f, 925.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(-360.0f, 925.0f), new Vector2(900.0f, 750.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 1);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);
            MgrBattleSystem.Instance.TutorialStep = 3;

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.ShowTutorialTextUI(3, ANCHOR_TYPE.BOTTOM_RIGHT, new Vector2(1.0f, 0.5f), new Vector2(-750.0f, 925.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(-360.0f, 925.0f), new Vector2(900.0f, 750.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 1);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.TutorialStep = 4;
            MgrBattleSystem.Instance.SetTutorialTimeScale(false);
        }

        if (tycoonSubStep < 2)
        {
            tycoonSubStep++;
            return;
        }

        for (int i = 0; i < currDoMaxFrame - currCompleteCnt; i++)
            arrTfTray[i].GetChild(childTmpFill).gameObject.SetActive(false);

        TycoonStep++;
        tycoonSubStep = 0;

        if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter && MgrBattleSystem.Instance.TutorialStep == 4)
            await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());

        TaskUpdate_Step2().Forget();
    }

    // 붕어빵 소 떨구기 (Update)
    private async UniTaskVoid TaskUpdate_Step2(bool _isDelay = false)
    {
        if(token_SwitchAuto is null)
            token_SwitchAuto = new CancellationTokenSource();

        if (_isDelay)
            await UniTask.Delay(500, cancellationToken: token_SwitchAuto.Token);

        for(int i = 0; i < arrBeanScore.Length; i++)
            arrBeanScore[i] = 0;

        listCanDropLine.Clear();

        int maxRow = Mathf.CeilToInt((float)(currDoMaxFrame - currCompleteCnt) / 3.0f);
        float laserBottomYPos = 580.0f - (maxRow * 164.0f);

        for (int i = 0; i < maxRow; i++)
            listCanDropLine.Add(i);

        rtLaser.gameObject.SetActive(true);
        objTrayLine.SetActive(true);
        for (int i = 0; i < objTrayLine.transform.childCount; i++)
            objTrayLine.transform.GetChild(i).gameObject.SetActive(i < maxRow);

        rtLaser.anchoredPosition = new Vector2(rtLaser.anchoredPosition.x, laserBottomYPos);

        bool isReturn = false;

        float movedTime = 1.5f;
        if (maxRow >= 5) movedTime *= 1.66f;
        else if (maxRow >= 4) movedTime *= 1.33f;

        if(MgrBattleSystem.Instance.TutorialStep == 4)
        {
            MgrBattleSystem.Instance.SetTutorialTimeScale(true);
            MgrBattleSystem.Instance.ShowTutorialTextUI(4, ANCHOR_TYPE.BOTTOM_RIGHT, new Vector2(1.0f, 0.5f), new Vector2(-750.0f, 925.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(-360.0f, 925.0f), new Vector2(900.0f, 750.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 1);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.ShowTutorialTextUI(5, ANCHOR_TYPE.BOTTOM_RIGHT, new Vector2(1.0f, 0.5f), new Vector2(-750.0f, 925.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(-373.5f, 913.0f), new Vector2(625.0f, 25.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 0);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.ShowTutorialTextUI(6, ANCHOR_TYPE.BOTTOM_RIGHT, new Vector2(0.5f, 0.5f), new Vector2(-900.0f, 425.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(-375.0f, 125.0f), new Vector2(750.0f, 750.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 1);
            MgrBattleSystem.Instance.ShowTutorialFingerUI(new Vector2(-365.0f, 250.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 2);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.TutorialStep = 6;
            MgrBattleSystem.Instance.TutorialSubStep = 0;
            MgrBattleSystem.Instance.SetTutorialTimeScale(false);
            MgrBattleSystem.Instance.ToggleTutorialUI(false);
        }

        float timer = 0.0f;
        while (TycoonStep == 1 && !IsAuto)
        {
            if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter && MgrBattleSystem.Instance.TutorialStep == 6 && MgrBattleSystem.Instance.TutorialSubStep == 1)
            {
                await UniTask.Yield(cancellationToken: token_SwitchAuto.Token);
                continue;
            }

            timer += Time.deltaTime;

            if (!isReturn) rtLaser.anchoredPosition = Vector2.Lerp(new Vector2(rtLaser.anchoredPosition.x, laserBottomYPos), new Vector2(rtLaser.anchoredPosition.x, laserTopYPos), timer / movedTime);
            else rtLaser.anchoredPosition = Vector2.Lerp(new Vector2(rtLaser.anchoredPosition.x, laserTopYPos), new Vector2(rtLaser.anchoredPosition.x, laserBottomYPos), timer / movedTime);

            if (timer >= movedTime)
            {
                isReturn = !isReturn;
                timer = 0.0f;
            }

            await UniTask.Yield(cancellationToken: token_SwitchAuto.Token);
        }

        if (IsAuto)
            return;

        objTrayLine.SetActive(false);
        rtLaser.gameObject.SetActive(false);

        TaskUpdate_Step3().Forget();
    }

    // 붕어빵 소 떨구기 (Click Btn)
    private async UniTaskVoid DropStuff_Step2()
    {
        if (IsAuto)
            return;

        float currLineYPos = rtLaser.anchoredPosition.y;

        float dist = 9999.0f;
        int lineIndex = -1;
        foreach (int lineNum in listCanDropLine)
        {
            float lineYPos = topBakeFrameYPos - (lineNum * 164.0f);
            float tempDist = Mathf.Abs(currLineYPos - lineYPos);
            if (tempDist < dist)
            {
                dist = tempDist;
                lineIndex = lineNum;
            }
        }

        if (lineIndex > -1)
        {
            listCanDropLine.Remove(lineIndex);
            objTrayLine.transform.GetChild(lineIndex).gameObject.SetActive(false);

            parsysDropStuff.transform.position = new Vector3(parsysDropStuff.transform.position.x, rtLaser.position.y, 0.0f);
            parsysDropStuff.Play();

            float percentage = (currLineYPos - (topBakeFrameYPos - (lineIndex * 164.0f))) / 124.0f;

            if (percentage < 0.0f)
                percentage *= -1.0f;

            Transform tfBean;
            Image imgBean;
            for (int i = lineIndex * 3; i < lineIndex * 3 + 3; i++)
            {
                if (i >= currDoMaxFrame - currCompleteCnt)
                    break;

                string grade = "Bad";
                if (percentage < 0.05f)
                {
                    MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Excellent", 0.25f);
                    grade = "Excellent";
                    arrBeanScore[i] = scoreExcellent;
                }
                else if (percentage < 0.15f)
                {
                    MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Great", 0.25f);
                    grade = "Great";
                    arrBeanScore[i] = scoreGreat;
                }
                else if (percentage < 0.35f)
                {
                    MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Good", 0.25f);
                    grade = "Good";
                    arrBeanScore[i] = scoreGood;
                }
                else if (percentage < 0.7f)
                {
                    MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Not Bad", 0.25f);
                    grade = "NotBad";
                    arrBeanScore[i] = scoreNotBad;
                }
                else
                {
                    MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Bad", 0.25f);
                    arrBeanScore[i] = scoreBad;
                }

                tfBean = arrTfTray[i].GetChild(childImgBean);
                imgBean = tfBean.GetComponent<Image>();
                imgBean.color = Color.white;
                tfBean.position = new Vector3(tfBean.transform.position.x, rtLaser.position.y, 0.0f);
                tfBean.gameObject.SetActive(true);
                arrTfTray[i].GetChild(childSpineGrade).GetComponent<BakeGradeSpineEvent>().SetGrade(grade);
            }

            MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Step2", 1.0f);
        }

        if (MgrBattleSystem.Instance.TutorialStep == 6 && MgrBattleSystem.Instance.TutorialSubStep == 0)
        {
            MgrBattleSystem.Instance.TutorialSubStep = 1;

            MgrBattleSystem.Instance.HideTutorialFingerUI();

            await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.SetTutorialTimeScale(true);
            MgrBattleSystem.Instance.ShowTutorialTextUI(7, ANCHOR_TYPE.BOTTOM_RIGHT, new Vector2(1.0f, 0.5f), new Vector2(-750.0f, 925.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(-360.0f, 925.0f), new Vector2(900.0f, 750.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 1);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.SetTutorialTimeScale(false);
            MgrBattleSystem.Instance.ToggleTutorialUI(false);
            MgrBattleSystem.Instance.TutorialStep = 7;
        }

        if (listCanDropLine.Count > 0)
            return;

        TycoonStep++;
    }

    // 붕어빵 커버 덮기
    private CancellationTokenSource[] token_cover = new CancellationTokenSource[15];
    private async UniTaskVoid TaskUpdate_Step3(bool _isDelay = false)
    {
        if (_isDelay)
            await UniTask.Delay(500, cancellationToken: token_SwitchAuto.Token);

        if (IsAuto) return;

        Image imgBean;
        for (int i = 0; i < currDoMaxFrame; i++)
        {
            if (arrBeanScore[i] <= 0.0f)
                continue;

            imgBean = arrTfTray[i].GetChild(childImgBean).GetComponent<Image>();
            imgBean.DOFade(0.0f, 1.0f).SetEase(Ease.Linear);
        }

        await UniTask.Delay(1000, cancellationToken: token_SwitchAuto.Token); // default : 2000
        if (IsAuto) return;

        // 덮게 덮기
        int coverCnt = 0;

        RectTransform rtCover = null;
        SkeletonGraphic skgTemp = null;
        for (int i = 0; i < currDoMaxFrame; i++)
        {
            if (IsAuto)
                return;

            if (arrBeanScore[i] <= 0.0f)
                continue;

            coverCnt++;
            rtCover = arrTfTray[i].GetChild(childImgCover).GetComponent<RectTransform>();
            skgTemp = rtCover.GetComponent<SkeletonGraphic>();
            skgTemp.color = Color.white;
            skgTemp.Skeleton.SetColor(Color.white);
            skgTemp.Skeleton.SetBonesToSetupPose();
            skgTemp.AnimationState.SetAnimation(0, "open", false);
            rtCover.gameObject.SetActive(true);

            token_cover[i]?.Cancel();
            token_cover[i]?.Dispose();
            token_cover[i] = new CancellationTokenSource();
            TaskCoverDelay(i).Forget();

            await UniTask.DelayFrame(5, cancellationToken: token_SwitchAuto.Token);
        }

        await UniTask.Delay(600, cancellationToken: token_SwitchAuto.Token); // 연출 딜레이
        if (IsAuto) return;

        // 반죽 비활성화
        for (int i = 0; i < currDoMaxFrame; i++)
            arrTfTray[i].GetComponent<Image>().enabled = true;

        // 덮게 덮어진 뒤 딜레이
        await UniTask.Delay(200, cancellationToken: token_SwitchAuto.Token);
        //await UniTask.Delay(1200, cancellationToken: this.GetCancellationTokenOnDestroy());

        if (IsAuto) return;

        for (int i = 0; i < currDoMaxFrame; i++)
        {
            arrTfTray[i].GetComponent<Image>().enabled = true;
            arrTfTray[i].GetChild(childImgOutLine).gameObject.SetActive(true);
        }

        if (MgrBattleSystem.Instance.TutorialStep == 7)
        {
            MgrBattleSystem.Instance.SetTutorialTimeScale(true);
            MgrBattleSystem.Instance.ShowTutorialTextUI(8, ANCHOR_TYPE.BOTTOM_RIGHT, new Vector2(1.0f, 0.5f), new Vector2(-750.0f, 925.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(-360.0f, 925.0f), new Vector2(900.0f, 750.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 1);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.ShowTutorialTextUI(9, ANCHOR_TYPE.BOTTOM_RIGHT, new Vector2(1.0f, 0.5f), new Vector2(-750.0f, 925.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(-360.0f, 925.0f), new Vector2(900.0f, 750.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 1);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.ShowTutorialTextUI(10, ANCHOR_TYPE.BOTTOM_RIGHT, new Vector2(0.5f, 0.0f), new Vector2(-370.0f, 430.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(-375.0f, 125.0f), new Vector2(750.0f, 750.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 1);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.TutorialStep = 10;
            MgrBattleSystem.Instance.ShowTutorialFingerUI(new Vector2(-365.0f, 250.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 3);
            MgrBattleSystem.Instance.HideTutorialMaskBackUI();
            MgrBattleSystem.Instance.SetTutorialTimeScale(false);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);
        }

        TycoonStep++;
        tycoonSubStep = 0;

        TaskUpdate_Step4().Forget();
    }

    private async UniTaskVoid TaskCoverDelay(int _index)
    {
        await UniTask.Delay(125, cancellationToken: token_cover[_index].Token);

        if(_index % 3 == 0)
            MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Step3", 1.0f);

        arrTfTray[_index].GetComponent<Image>().enabled = false;
        arrTfTray[_index].GetChild(childImgOutLine).gameObject.SetActive(false);
        arrTfTray[_index].GetChild(childImgFillDough).gameObject.SetActive(false);
        arrTfTray[_index].GetChild(childImgBean).gameObject.SetActive(false);

        await UniTask.Delay(475, cancellationToken: token_cover[_index].Token);

        arrTfTray[_index].GetChild(childObjBakingVFX).gameObject.SetActive(true);
    }

    // 붕어빵 굽기 (Update)
    private async UniTaskVoid TaskUpdate_Step4(bool _isDelay = false)
    {
        if (_isDelay)
            await UniTask.Delay(500, cancellationToken: token_SwitchAuto.Token);

        if (IsAuto) return;

        listComplete.Clear();
        listRTCover.Clear();
        tycoonSubStep = 0;

        int coverCnt = 0;
        RectTransform rtCover = null;
        for (int i = 0; i < currDoMaxFrame; i++)
        {
            if (arrBeanScore[i] <= 0.0f)
                continue;

            coverCnt++;
            rtCover = arrTfTray[i].GetChild(childImgCover).GetComponent<RectTransform>();
            listRTCover.Add(rtCover);
        }
        listRTCover.Shuffle(); // 셔플

        // 굽기
        float waitDuration = Random.Range(0.5f, 1.5f);
        while (listRTCover.Count > 0 && !IsAuto)
        {
            await UniTask.Yield(cancellationToken: token_SwitchAuto.Token);

            if (waitDuration <= 0.0f)
            {
                int randCnt = Random.Range(1, 5);
                if (randCnt > listRTCover.Count)
                    randCnt = listRTCover.Count;

                for (int i = 0; i < randCnt; i++)
                {
                    var complete = listRTCover[0].parent.GetComponent<BakeCompleteTimer>();
                    
                    complete.SetComplete(true, 0);
                    complete.transform.GetChild(childVFXBakingSmoke).GetComponent<ParticleSystem>().Play();
                    listRTCover[0].localPosition = new Vector3(0.0f, -10.0f);
                    listRTCover[0].DOLocalMoveY(5.0f, 0.175f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear).OnStepComplete(() =>
                    {
                        if(MgrBattleSystem.Instance.isStageStart)
                            MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Frame_Shake", 0.1f);
                    });
                    listRTCover[0].GetComponent<SkeletonGraphic>().DOColor(Color.red, 5.0f).SetEase(Ease.Linear).SetDelay(2.0f).OnStart(() => MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Frame_Burn", 0.5f));
                    listRTCover.RemoveAt(0);
                    listComplete.Add(complete);
                }

                //if (MgrBattleSystem.Instance.TutorialStep == 10)
                //{
                //    MgrBattleSystem.Instance.ShowTutorialFingerUI(new Vector2(-380.0f, 350.0f), "빠르게 터치!", ANCHOR_TYPE.BOTTOM_RIGHT);
                //    MgrBattleSystem.Instance.ToggleTutorialUI(true);
                //}

                waitDuration = Random.Range(0.5f, 1.5f);
                continue;
            }

            waitDuration -= Time.deltaTime;
        }

        // 뒤집기를 다 시행했는지 대기
        await UniTask.WaitUntil(() => currDoMaxFrame == currCompleteCnt || IsAuto, cancellationToken: token_SwitchAuto.Token);

        if (IsAuto) return;

        TycoonStep++;

        // 완성된 붕어빵이 틀에 다 들어갔는지 대기
        await UniTask.WaitUntil(() => tycoonSubStep == coverCnt, cancellationToken: this.GetCancellationTokenOnDestroy());

        tycoonSubStep = 0;

        ChallengePenalty();
        TaskRefresh_Step5().Forget();
    }

    // 다 구워진 붕어빵 뒤집기 (Click Btn)
    private async UniTaskVoid FlipBake_Step4()
    {
        if (IsAuto)
            return;

        if (MgrBattleSystem.Instance.TutorialStep == 10)
        {
            MgrBattleSystem.Instance.TutorialStep = 11;
            MgrBattleSystem.Instance.ToggleTutorialUI(false);
        }

        currCompleteCnt++;

        // 캐싱
        RectTransform rtComplete = listComplete[0].transform.GetChild(childImgWelldone).GetComponent<RectTransform>();
        RectTransform rtCover = listComplete[0].transform.GetChild(childImgCover).GetComponent<RectTransform>();

        // 연기 파티클 제거
        ParticleSystem parsysSmoke = listComplete[0].transform.GetChild(childVFXBakingSmoke).GetComponent<ParticleSystem>();
        parsysSmoke.Stop();
        parsysSmoke.Clear();
        listComplete[0].transform.GetChild(childObjBakingVFX).gameObject.SetActive(false);

        // 완성된 붕어빵 위치,스케일,회전값 초기화
        rtComplete.localPosition = Vector3.zero;
        rtComplete.localScale = Vector3.one;
        rtComplete.rotation = Quaternion.identity;
        rtComplete.GetComponent<Canvas>().sortingOrder = 0;

        // 타이머 비활성화 및 완성 붕어빵 크기 반죽 크기만큼 세팅
        listComplete[0].SetComplete(false);
        rtComplete.localPosition = Vector2.zero;
        rtComplete.localScale = listComplete[0].transform.GetChild(childImgFillDough).localScale;

        // 덮게 DoTween Kill 및 비활성화
        rtCover.DOKill();
        rtCover.GetComponent<SkeletonGraphic>().DOKill();
        //tfCover.GetComponent<Image>().DOKill();
        rtCover.gameObject.SetActive(false);
        rtCover.anchoredPosition = new Vector2(0.0f, -4.0f);

        // 뒤집기 VFX 실행
        listComplete[0].transform.GetChild(childPullOutVFX).GetComponent<ParticleSystem>().Play();
        
        //rtTrayShake.DOKill();
        //rtTrayShake.anchoredPosition = new Vector2(-381.0f, 806.0f);
        //rtTrayShake.DOShakeAnchorPos(0.5f, 8, 15);

        // 점수 판정
        string resultGrade = GetGradeResult(listComplete[0]);
        AddRankBakedFish(resultGrade);
        listComplete[0].transform.GetChild(childSpineResult).GetComponent<BakeResultSpineEvent>().SetResult(resultGrade);

        // 최종 결과 등급에 따른 부스터 경험치
        float originalExp = 0.0f;
        float boosterAmount = GetResultToBoosterExp(resultGrade, ref originalExp);

        MgrBoosterSystem.Instance.AddBoosterExp(boosterAmount, originalExp);

        Image imgComplete = rtComplete.GetComponent<Image>();
        imgComplete.color = Color.white;

        // 최종 결과 등급에 따른 이미지 설정
        switch (resultGrade)
        {
            case "S":
                MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Rank_S", 1.0f);
                rtComplete.GetComponent<Image>().sprite = soBakedFishImage.sprS;
                break;
            case "A":
                MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Rank_ABC", 1.0f);
                rtComplete.GetComponent<Image>().sprite = soBakedFishImage.sprA;
                break;
            case "B":
                MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Rank_ABC", 1.0f);
                rtComplete.GetComponent<Image>().sprite = soBakedFishImage.sprB;
                break;
            case "C":
                MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Rank_ABC", 1.0f);
                rtComplete.GetComponent<Image>().sprite = soBakedFishImage.sprC;
                break;
            case "F":
                MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Rank_F", 1.0f);
                rtComplete.GetComponent<Image>().sprite = soBakedFishImage.sprF;
                break;
            default:
                break;
        }

        // 완성 붕어빵 활성화
        rtComplete.gameObject.SetActive(true);

        // 완료 대상 리스트에서 제거
        listComplete.RemoveAt(0);

        await UniTask.Delay(500, cancellationToken: this.GetCancellationTokenOnDestroy()); // default : 1000

        if(boosterAmount > 0.0f)
        {
            MgrSound.Instance.PlayOneShotSFX(resultGrade.Equals("F") ? "SFX_Fish_Bread_Burn_disappear" : "SFX_Fish_Bread_dart", 1.0f);

            // DoTween Path 경로 지정
            Vector3 v3Dir = (rtMovedPosition.position - rtComplete.position).normalized;
            float distance = (rtMovedPosition.position - rtComplete.position).magnitude;
            Vector3 v3Way0 = rtComplete.position + (distance * 0.5f * v3Dir) + Quaternion.Euler(0, 0, -90.0f) * v3Dir;

            arrV3bezier[0] = v3Way0;
            arrV3bezier[1] = rtComplete.position + Vector3.left;
            arrV3bezier[2] = v3Way0 - v3Dir;
            arrV3bezier[3] = rtMovedPosition.position;
            arrV3bezier[4] = v3Way0 + v3Dir;
            arrV3bezier[5] = rtMovedPosition.position + Vector3.up;

            // y 좌표 위치에 따른 이동 시간 세팅
            float moveTime = Mathf.Lerp(0.5f, 0.75f, rtComplete.position.y / 491.0f);

            // 오더 설정
            rtComplete.GetComponent<Canvas>().sortingOrder = 2;

            // DoTween
            Sequence seq = DOTween.Sequence();
            seq.Append(rtComplete.DOPath(arrV3bezier, moveTime, PathType.CubicBezier).SetEase(Ease.InSine).OnComplete(() => {
                rtComplete.gameObject.SetActive(false);
                parsysDropStall.Play();
                tycoonSubStep++;

                if (!resultGrade.Equals("F"))
                {
                    int addBaked = 1;
                    if (canLucky)
                    {
                        if (Random.Range(0.001f, 1.0f) <= (float)DataManager.Instance.GetBoosterSkillData($"skill_passive_009_{MgrBoosterSystem.Instance.DicEtc["skill_passive_009"] - 1}").Params[0])
                        {
                            MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Double", 0.5f);

                            imgBakedX2.DOKill();
                            imgBakedX2.rectTransform.DOKill();

                            imgBakedX2.color = Color.white;
                            imgBakedX2.rectTransform.anchoredPosition = Vector2.zero;
                            imgBakedX2.rectTransform.DOAnchorPosY(50.0f, 0.5f);
                            imgBakedX2.DOFade(0.0f, 0.5f);

                            addBaked = 2;
                        }
                    }
                    AddBakedFish(addBaked);
                }
            }));
            seq.Join(rtComplete.DORotate(new Vector3(0.0f, 0.0f, Random.Range(180.0f, 450.0f)), moveTime, RotateMode.FastBeyond360));
            seq.Join(rtComplete.DOScale(Vector3.one * 0.5f, moveTime).SetEase(Ease.InExpo));
        }
        else
        {
            Image imgAutoBakedFish = rtComplete.GetComponent<Image>();

            Sequence seq = DOTween.Sequence();
            seq.Append(rtComplete.transform.DOLocalMoveY(200.0f, 0.25f).SetEase(Ease.Linear).OnComplete(() =>
            {
                rtComplete.gameObject.SetActive(false);
                tycoonSubStep++;
            }));
            seq.Join(rtComplete.DORotate(new Vector3(0.0f, 0.0f, Random.Range(180.0f, 450.0f)), 0.25f, RotateMode.FastBeyond360));
            seq.Join(rtComplete.DOScale(Vector3.one * 0.5f, 0.25f));
            seq.Join(imgAutoBakedFish.DOFade(0.0f, 0.25f).SetEase(Ease.Linear));
        }
    }

    private void Step4ToAutoCheck()
    {
        for(int i = listComplete.Count - 1; i >= 0; i--)
        {
            if(!GetGradeResult(listComplete[i], true).Equals("F"))
            {
                listComplete.RemoveAt(i);
                continue;
            }

            currCompleteCnt++;

            GetGradeResult(listComplete[i]);
            AddRankBakedFish("F");

            // 캐싱
            RectTransform rtComplete = listComplete[i].transform.GetChild(childImgWelldone).GetComponent<RectTransform>();
            Transform tfCover = listComplete[i].transform.GetChild(childImgCover);

            // 연기 파티클 제거
            ParticleSystem parsysSmoke = listComplete[i].transform.GetChild(childVFXBakingSmoke).GetComponent<ParticleSystem>();
            parsysSmoke.Stop();
            parsysSmoke.Clear();

            // 완성된 붕어빵 위치,스케일,회전값 초기화
            rtComplete.localPosition = Vector3.zero;
            rtComplete.localScale = Vector3.one;
            rtComplete.rotation = Quaternion.identity;
            rtComplete.GetComponent<Canvas>().sortingOrder = 0;

            // 타이머 비활성화 및 완성 붕어빵 크기 반죽 크기만큼 세팅
            listComplete[i].SetComplete(false);
            rtComplete.localPosition = Vector2.zero;
            rtComplete.localScale = listComplete[i].transform.GetChild(childImgFillDough).localScale;

            // 덮게 DoTween Kill 및 비활성화
            tfCover.DOKill();
            tfCover.GetComponent<Image>().DOKill();
            tfCover.gameObject.SetActive(false);

            // 완성 붕어빵 활성화
            rtComplete.gameObject.SetActive(true);

            // 완료 대상 리스트에서 제거
            listComplete.RemoveAt(i);

            Image imgAutoBakedFish = rtComplete.GetComponent<Image>();
            imgAutoBakedFish.color = Color.white;

            Sequence seq = DOTween.Sequence();
            seq.Append(rtComplete.transform.DOLocalMoveY(200.0f, 0.25f).SetEase(Ease.Linear).OnComplete(() =>
            {
                rtComplete.gameObject.SetActive(false);
                //tycoonSubStep++;
            }));
            seq.Join(rtComplete.DORotate(new Vector3(0.0f, 0.0f, Random.Range(180.0f, 450.0f)), 0.25f, RotateMode.FastBeyond360));
            seq.Join(rtComplete.DOScale(Vector3.one * 0.5f, 0.25f));
            seq.Join(imgAutoBakedFish.DOFade(0.0f, 0.25f).SetEase(Ease.Linear));
        }
    }

    // 판 청소 (Update)
    private List<UnitBase> listUnitTemp = new List<UnitBase>();
    private async UniTaskVoid TaskRefresh_Step5(bool _isAuto = false)
    {
        if (MgrBattleSystem.Instance.TutorialStep == 11)
        {
            await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.SetTutorialTimeScale(true);
            MgrBattleSystem.Instance.ShowTutorialTextUI(CurrBakedFish < 5 ? 11 : 12, ANCHOR_TYPE.BOTTOM_RIGHT, new Vector2(0.5f, 0.0f), new Vector2(-925.0f, 400.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(-920.0f, 175.0f), new Vector2(250.0f, 250.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 1);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

            if (CurrBakedFish < 5)
                AddBakedFish(5 - CurrBakedFish);
            MgrBattleSystem.Instance.ShowTutorialTextUI(13, ANCHOR_TYPE.BOTTOM_RIGHT, new Vector2(0.5f, 0.0f), new Vector2(-925.0f, 400.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(-920.0f, 175.0f), new Vector2(250.0f, 250.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 1);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.TutorialStep = 13;
            MgrBattleSystem.Instance.SetTutorialTimeScale(false);
            MgrBattleSystem.Instance.ToggleTutorialUI(false);

            MgrBoosterSystem.Instance.AddBoosterExp(460.0f);
            MgrBattleSystem.Instance.SetTutorialInitWave();
        }

        objCompleteImg.SetActive(false);

        objRefreshTray.SetActive(true);
        objAutoRefresh.SetActive(true);

        foreach (var stepIcon in arrAutoStepIcon)
            stepIcon.SetEmpty();

        IsTycoonEnable = false;

        int randomSpine = Random.Range(0, arrSkdaCleanCat.Length);
        skgPartTime.skeletonDataAsset = arrSkdaCleanCat[randomSpine];
        skgPartTime.Initialize(true);
        skgPartTime.AnimationState.SetAnimation(0, randomSpine == 0 ? "rest cat" : "idle", true);
        skgPartTime.Skeleton.SetToSetupPose();

        tmpDescription.text = string.Empty;

        float refreshTimer = _isAuto ? 4.5f : 5.0f;
        while (refreshTimer > 0.0f)
        {
            await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());

            refreshTimer -= Time.deltaTime;
            tmpRefreshTimer.text = $"불판 청소중..\n{refreshTimer:F1}";
            tmpRefreshAutoTimer.text = $"불판을 정리중입니다.. {refreshTimer:F1}";
        }

        parsysClearTray.Play();
        objRefreshTray.SetActive(false);
        objAutoRefresh.SetActive(false);

        TycoonStep = 0;
        tycoonSubStep = 0;
        currCompleteCnt = 0;

        InitBakedScore();

        if(IsAuto && MgrBattleSystem.Instance.isStageStart)
            TaskAutoFishBaked(0).Forget();
    }

    private void ChallengePenalty(bool _isAuto = false)
    {
        if (MgrBattleSystem.Instance.IsChallengeMode && MgrBattleSystem.Instance.ChallengeLevel == 2 && MgrBattleSystem.Instance.GetCurrentThema() == 2)
        {
            UnitBase allyBase = MgrBattleSystem.Instance.GetAllyBase();
            if (allyBase is not null)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_Challenge_C2_Penalty_a", 1.0f);

                if (_isAuto) objAutoPenaltyVFX.SetActive(true);
                else objPenaltyVFX.SetActive(true);

                GameObject objExVFX = MgrObjectPool.Instance.ShowObj("FX_Tycoon Breakdown_hit", allyBase.GetUnitCenterPos());
                objExVFX.transform.localScale = Vector3.one * 0.5f;
                objExVFX.transform.SetParent(allyBase.transform);
                TaskTycoonDamage(allyBase, allyBase.UnitStat.MaxHP * (float)DataManager.Instance.GetChallengePenaltyData("penalty_000007").Param[0]).Forget();

                listUnitTemp.Clear();
                listUnitTemp.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(allyBase, true));
                listUnitTemp.Remove(allyBase);

                foreach (UnitBase unit in listUnitTemp)
                {
                    objExVFX = MgrObjectPool.Instance.ShowObj("FX_Tycoon Breakdown_hit", unit.GetUnitCenterPos());
                    objExVFX.transform.localScale = Vector3.one * 0.5f;
                    objExVFX.transform.SetParent(unit.transform);
                    TaskTycoonDamage(unit, unit.UnitStat.MaxHP * (float)DataManager.Instance.GetChallengePenaltyData("penalty_000007").Param[0]).Forget();
                }
            }
        }
    }

    private async UniTaskVoid TaskTycoonDamage(UnitBase _target, float _dmg)
    {
        await UniTask.Delay(250, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrInGameEvent.Instance.BroadcastDamageEvent(_target, _target, _dmg, _dmgChannel: -1);
    }
    #endregion

    public void AddBakedFish(int _cnt = 1, bool _isLimitBreak = false)
    {
        if (_cnt < 1)
            return;

        int limitCnt = _isLimitBreak ? MaxBakedFish * 2 : MaxBakedFish;
        if (CurrBakedFish >= limitCnt)
        {
            MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Locker_Delete", 0.5f);
            return;
        }

        MgrSound.Instance.PlayOneShotSFX("SFX_Fish_Bread_Locker_Cost", 0.5f);
        int addFish = CurrBakedFish + _cnt > limitCnt ? _cnt - (CurrBakedFish + _cnt - limitCnt) : _cnt;

        CurrBakedFish += addFish;
        tmpBakedCnt.text = $"{CurrBakedFish}";
        tmpBakedCnt.color = CurrBakedFish >= MaxBakedFish ? Color.red : Color.white;

        if (tycoonAutoLevel < 5)
            btnAutoLevel.interactable = requiredAutoLevelCost[tycoonAutoLevel] <= CurrBakedFish;

        MgrInGameEvent.Instance.BroadcastCanSpawnEvent();
    }

    public void UseBakedFish(int _cnt)
    {
        if (CurrBakedFish < _cnt)
            return;

        CurrBakedFish -= _cnt;
        tmpBakedCnt.text = $"{CurrBakedFish}";
        tmpBakedCnt.color = CurrBakedFish >= MaxBakedFish ? Color.red : Color.white;

        if (tycoonAutoLevel < 5)
            btnAutoLevel.interactable = requiredAutoLevelCost[tycoonAutoLevel] <= CurrBakedFish;

        MgrInGameEvent.Instance.BroadcastCanSpawnEvent();

        Transform tfRoot = MgrBattleSystem.Instance.objUnitSpawnRoot.transform;
        int slotCnt = tfRoot.childCount;
        for (int i = 0; i < slotCnt; i++)
            tfRoot.GetChild(i).GetComponent<UnitSlotBtn>().ReturnFirstSaleDiscount();
    }

    private string GetGradeResult(BakeCompleteTimer _bakeCom, bool _isJustCheck = false)
    {
        int timeScore;
        if (_bakeCom.TimePassedAfterCompletion < 0.4f) timeScore = scoreExcellent;
        else if (_bakeCom.TimePassedAfterCompletion < 0.9f) timeScore = scoreGreat;
        else if (_bakeCom.TimePassedAfterCompletion < 1.4f) timeScore = scoreGood;
        else if (_bakeCom.TimePassedAfterCompletion < 2.0f) timeScore = scoreNotBad;
        else if (_bakeCom.TimePassedAfterCompletion < 7.0f) timeScore = scoreBad;
        else timeScore = 0;

        timeScore -= _bakeCom.PenaltyScore;

        int index = -1;
        for(int i = 0; i < currMaxFrame; i++)
        {
            if(arrTfTray[i].Equals(_bakeCom.transform))
            {
                index = i;
                break;
            }
        }

        int resultScore = arrDoughScore[index] + arrBeanScore[index] + timeScore;

        if(!_isJustCheck)
        {
            arrDoughScore[index] = 0;
            arrBeanScore[index] = 0;
        }

        if (resultScore >= 97) return "S";
        else if (resultScore >= 91) return "A";
        else if (resultScore >= 84) return "B";
        else if (resultScore >= 76) return "C";
        else return "F";
    }

    private string GetAutoFinalGrade(int _score)
    {
        if (_score >= 97) return "S";
        else if (_score >= 91) return "A";
        else if (_score >= 84) return "B";
        else if (_score >= 76) return "C";
        else return "F";
    }

    private int GetAutoScore_Legacy()
    {
        float[] gradePer = new float[5];
        gradePer[0] = Mathf.Lerp(0.0f, 15.0f, (GetAutoAbility() - 100) * 0.01f) + 5.0f; // s
        gradePer[1] = Mathf.Lerp(0.0f, 25.0f, (GetAutoAbility() - 100) * 0.01f) + 15.0f; // a
        gradePer[2] = Mathf.Lerp(0.0f, -10.0f, (GetAutoAbility() - 100) * 0.01f) + 30.0f; // b
        gradePer[3] = Mathf.Lerp(0.0f, -22.0f, (GetAutoAbility() - 100) * 0.01f) + 40.0f; // c
        gradePer[4] = Mathf.Lerp(0.0f, -8.0f, (GetAutoAbility() - 100) * 0.01f) + 10.0f; // f

        float maxAmount = 0.0f;
        foreach (float value in gradePer)
            maxAmount += value;

        float pivot = Random.value * maxAmount;

        int currIndex = gradePer.Length - 1;
        for (int i = 0; i < gradePer.Length; i++)
        {
            if (pivot < gradePer[i])
            {
                currIndex = i;
                break;
            }
            else
            {
                pivot -= gradePer[i];
            }
        }

        switch (currIndex)
        {
            case 0: return scoreExcellent;
            case 1: return scoreGreat;
            case 2: return scoreGood;
            case 3: return scoreNotBad;
            case 4: return scoreBad;
            default: return scoreBad;
        }
    }

    private string GetAutoGrade()
    {
        float[] gradePer = new float[5];
        gradePer[0] = Mathf.Lerp(0.0f, 15.0f, (GetAutoAbility() - 100) * 0.01f) + 5.0f; // s
        gradePer[1] = Mathf.Lerp(0.0f, 25.0f, (GetAutoAbility() - 100) * 0.01f) + 15.0f; // a
        gradePer[2] = Mathf.Lerp(0.0f, -10.0f, (GetAutoAbility() - 100) * 0.01f) + 30.0f; // b
        gradePer[3] = Mathf.Lerp(0.0f, -22.0f, (GetAutoAbility() - 100) * 0.01f) + 40.0f; // c
        gradePer[4] = Mathf.Lerp(0.0f, -8.0f, (GetAutoAbility() - 100) * 0.01f) + 10.0f; // f

        float maxAmount = 0.0f;
        foreach (float value in gradePer)
            maxAmount += value;

        float pivot = Random.value * maxAmount;

        int currIndex = gradePer.Length - 1;
        for (int i = 0; i < gradePer.Length; i++)
        {
            if (pivot < gradePer[i])
            {
                currIndex = i;
                break;
            }
            else
            {
                pivot -= gradePer[i];
            }
        }

        switch (currIndex)
        {
            case 0: return "S";
            case 1: return "A";
            case 2: return "B";
            case 3: return "C";
            default: return "F";
        }
    }

    private float GetResultToBoosterExp(string _rank, ref float _originalExp)
    {
        float resultExp;
        switch (_rank)
        {
            case "S": resultExp = 128.0f; break;
            case "A": resultExp = 106.0f; break;
            case "B": resultExp = 85.0f; break;
            case "C": resultExp = 64.0f; break;
            case "F": resultExp = 0.0f; break;
            default: resultExp = 0.0f; break;
        }

        float multiplyExp = 1.0f;
        UserGear gearStove = DataManager.Instance.GetUsingGearInfo(2);
        if(gearStove is not null)
        {
            if(gearStove.gearId.Equals("gear_stove_0002") && gearStove.gearRarity >= 3)
            {
                multiplyExp += (float)DataManager.Instance.GetGearOptionValue(gearStove.gearId, 2);
            }
            if(gearStove.gearId.Equals("gear_stove_0004"))
            {
                switch (_rank)
                {
                    case "S":
                    case "A":
                        if (gearStove.gearRarity >= 10)
                            multiplyExp += (float)DataManager.Instance.GetGearOptionValue(gearStove.gearId, 4);
                        break;
                    case "B":
                        if (gearStove.gearRarity >= 3)
                            multiplyExp += (float)DataManager.Instance.GetGearOptionValue(gearStove.gearId, 2);
                        break;
                    case "C":
                        if (gearStove.gearRarity >= 1)
                            multiplyExp += (float)DataManager.Instance.GetGearOptionValue(gearStove.gearId, 0);
                        break;
                    default:
                        break;
                }
            }
        }

        _originalExp = resultExp;

        return resultExp * multiplyExp;
    }

    private void AddRankBakedFish(string _grade)
    {
        switch(_grade)
        {
            case "S": ArrCurrBakedFishRank[0]++; break;
            case "A": ArrCurrBakedFishRank[1]++; break;
            case "B": ArrCurrBakedFishRank[2]++; break;
            case "C": ArrCurrBakedFishRank[3]++; break;
            case "F": ArrCurrBakedFishRank[4]++; break;
        }
        switch (_grade)
        {
            case "S":
                S_Combo++;
                if (S_Combo > S_MaxCombo)
                    S_MaxCombo = S_Combo;
                break;
            default: S_Combo = 0; break;
        }
    }

    private void AutoToChangedManual(int _step)
    {
        // 초기화
        SkeletonGraphic skgCover = null;
        for (int i = 0; i < currMaxFrame; i++)
        {
            arrTfTray[i].GetComponent<Image>().enabled = true;
            arrTfTray[i].GetChild(childImgOutLine).gameObject.SetActive(true);
            arrTfTray[i].GetChild(childImgFillDough).gameObject.SetActive(false);
            arrTfTray[i].GetChild(childTmpFill).gameObject.SetActive(false);
            arrTfTray[i].GetChild(childImgBean).gameObject.SetActive(false);
            arrTfTray[i].GetChild(childImgBean).GetComponent<Image>().color = Color.white;
            arrTfTray[i].GetChild(childImgBean).localPosition = Vector3.zero;
            arrTfTray[i].GetChild(childImgCover).gameObject.SetActive(false);
            arrTfTray[i].GetChild(childImgCover).DOKill();
            skgCover = arrTfTray[i].GetChild(childImgCover).GetComponent<SkeletonGraphic>();
            skgCover.DOKill();
            skgCover.color = Color.white;
            arrTfTray[i].GetChild(childVFXBakingSmoke).GetComponent<ParticleSystem>().Stop();
            arrTfTray[i].GetChild(childObjBakingVFX).gameObject.SetActive(false);
        }
        rtLaser.gameObject.SetActive(false);
        objTrayLine.SetActive(false);

        if (_step == 4)
            return;

        if (_step >= 1)
        {
            for (int i = 0; i < currDoMaxFrame; i++)
            {
                if (arrDoughScore[i] <= 0)
                    continue;

                arrTfTray[i].GetChild(childImgFillDough).transform.localScale = Vector3.one;
                arrTfTray[i].GetChild(childImgFillDough).gameObject.SetActive(true);
            }
        }

        if(_step >= 2)
        {
            for (int i = 0; i < currDoMaxFrame; i++)
            {
                if (arrBeanScore[i] <= 0)
                    continue;

                if(_step >= 3)
                {
                    arrTfTray[i].GetChild(childImgFillDough).gameObject.SetActive(false);
                    arrTfTray[i].GetChild(childImgBean).gameObject.SetActive(false);

                    arrTfTray[i].GetChild(childImgCover).localPosition = Vector3.zero;
                    arrTfTray[i].GetChild(childImgCover).gameObject.SetActive(true);
                }
                else
                {
                    arrTfTray[i].GetChild(childImgBean).transform.localScale = Vector3.one;
                    arrTfTray[i].GetChild(childImgBean).gameObject.SetActive(true);
                }
            }
        }
    }

    public override void OnDestroy()
    {
        token_SwitchAuto?.Cancel();
        token_SwitchAuto?.Dispose();
        token_SwitchAuto = null;

        base.OnDestroy();
    }
}
