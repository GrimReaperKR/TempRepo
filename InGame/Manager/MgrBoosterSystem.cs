using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using BCH.Database;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using Spine.Unity;
using Spine;
using UnityEngine.Playables;

public class MgrBoosterSystem : Singleton<MgrBoosterSystem>
{
    [SerializeField] private SlicedFilledImage imgBoosterGuage; // 부스터 게이지 슬라이더 바

    [SerializeField] private TextMeshProUGUI tmpBoosterLevel; // 부스터 레벨 표기 TMP
    [SerializeField] private Image imgBoosterRaycast; // 부스터 클릭 방지 이미지
    [SerializeField] private Button btnRandomBoosterGet; // 랜덤 부스터 획득 버튼
    [field: SerializeField] public GameObject ObjBoosterSelect { get; private set; } // 부스터 선택 창
    [field: SerializeField] public GameObject ObjBoosterSelectVFX { get; private set; } // 부스터 선택 효과 VFX
    [SerializeField] private GameObject objRandomBoosterUFX; // 랜덤 부스터 선택 효과 VFX
    [field: SerializeField] public RectTransform rtCurrBoosterRoot { get; private set; } // 현재 부스터 현황 rt
    [field: SerializeField] public GameObject objBoosterIconRoot { get; private set; } // 부스터 아이콘 통합 오브젝트
    private CurrentBoosterSlot[] arrCurrBoosterSlot; // 현재 부스터 슬롯 배열
    [SerializeField] private GameObject objDelaySelect; // 선택 딜레이용 오브젝트

    [field: SerializeField] public GameObject objBoosterGauge { get; private set; } // 부스터 경험치 게이지 바 오브젝트

    [SerializeField] private GameObject objCanvCurrBoosterSelected; // 현재 부스터 현황 캔버스 오브젝트
    [SerializeField] private BoosterCard currBoosterSelected; // 선택된 부스터 카드

    [SerializeField] private GameObject objOtherUI; // otherUI 오브젝트
    [SerializeField] private GameObject objReroll; // 리롤 버튼 오브젝트
    [SerializeField] private CanvasGroup canvgSelectText; // 부스터 선택 안내 메세지 캔버스 그룹

    // 부스터 선택 남은 횟수 표기
    [SerializeField] private GameObject objBoosterPoint_InSliderPrefab;
    [SerializeField] private GameObject objBoosterPointSliderRoot;

    [SerializeField] private GameObject objBoosterPoint_InSelectUIPrefab;
    [SerializeField] private GameObject objBoosterPointSelectUIRoot;

    // 부스터 관련 기본 변수
    private BoosterExp boosterEXPData; // 부스터 레벨 당 경험치 정보

    private int boosterLv; // 현재 부스터 레벨
    private float boosterExp; // 현재 경험치
    private float maxBoosterExp; // 최대 경험치

    private int boosterStackCnt; // 부스터 스택
    private List<int> listBoosterRandomStack = new List<int>();
    public int RemainRandomSelect { get; private set; } // 랜덤 부스터 갯수 ( 3 = 3칸)

    public bool IsOpenBoosterCard { get; private set; } // 부스터 카드 선택창이 열렸는지 체크
    public int SelectBoosterGoldCnt { get; private set; } // 골드 부스터 선택 횟수

    private int currRerollCnt; // 현재 리롤 횟수

    private CancellationTokenSource token_BoosterExp;

    // 부스터 업그레이드 딕셔너리 (key : 인덱스, value : 현재 레벨)
    public Dictionary<string, int> DicWeapon { get; private set; } = new Dictionary<string, int>(); // 무기 업그레이드
    public Dictionary<string, int> DicSkill { get; private set; } = new Dictionary<string, int>(); // 스킬 업그레이드 (엑티브[최대 3개]+패시브) -> 엑티브 (3개)
    public Dictionary<string, int> DicEtc { get; private set; } = new Dictionary<string, int>(); // 나머지
    public Dictionary<string, int> DicNotUpgrade { get; private set; } = new Dictionary<string, int>(); // 업그레이드 불가 떨이 인덱스

    private Dictionary<string, int> dicTemp; // 임시 할당 딕셔너리

    private List<BoosterData.BoosterInfo> listBoosterUpgrade = new List<BoosterData.BoosterInfo>(); // 업그레이드 가능한 부스터 리스트
    private List<BoosterData.BoosterInfo> listRandomBoosterUpgrade = new List<BoosterData.BoosterInfo>(); // 업그레이드 가능한 부스터 리스트

    private void Awake()
    {
        arrCurrBoosterSlot = new CurrentBoosterSlot[objBoosterIconRoot.transform.childCount];
        for (int i = 0; i < arrCurrBoosterSlot.Length; i++)
            arrCurrBoosterSlot[i] = objBoosterIconRoot.transform.GetChild(i).GetComponent<CurrentBoosterSlot>();

        arrRouletteSlot = new RouletteSlot[skgRoulette.transform.childCount];
        for (int i = 0; i < skgRoulette.transform.childCount; i++)
            arrRouletteSlot[i] = skgRoulette.transform.GetChild(i).GetComponent<RouletteSlot>();

        rtRouletteStart = btnRouletteStart.GetComponent<RectTransform>();

        btnRandomBoosterGet.enabled = false;

        boosterLv = 0;
        boosterExp = 0.0f;
        maxBoosterExp = 600.0f;

        boosterStackCnt = 0;
        SelectBoosterGoldCnt = 0;

        currRerollCnt = 0;

        UpdateBoosterPointInSlider();
        tmpBoosterLevel.text = $"{boosterLv}";

        TaskWaitDataLoad().Forget();

        objDelaySelect.SetActive(false);

        imgBoosterRaycast.gameObject.SetActive(false);
        ObjBoosterSelect.SetActive(false);
        ObjBoosterSelectVFX.SetActive(false);
        objOtherUI.SetActive(false);

        HidePuaseBoosterSelected();
    }

    private async UniTaskVoid TaskWaitDataLoad()
    {
        await UniTask.WaitUntil(() => DataManager.Instance.IsDataLoaded && MgrBattleSystem.Instance.isStageStart, cancellationToken: this.GetCancellationTokenOnDestroy());

        // 업그레이드 가능 리스트에 모든 부스터 리스트 등록
        BoosterData.BoosterInfo[] info = MgrInGameData.Instance.SOBoosterData.boosterInfo;
        listBoosterUpgrade = info.ToList();

        if (MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode)
        {
            foreach (BoosterData.BoosterInfo booster in info)
            {
                if (booster.Type == BoosterType.Active)
                {
                    if (booster.Index.Equals("skill_active_002") || booster.Index.Equals("skill_active_003"))
                        listBoosterUpgrade.Remove(booster);
                }
                else listBoosterUpgrade.Remove(booster);
            }
        }
        else
        {
            for(int i = listBoosterUpgrade.Count - 1; i >= 0; i--)
                if (listBoosterUpgrade[i].Type == BoosterType.Weapon)
                    if (!DicWeapon.ContainsKey(listBoosterUpgrade[i].Index) || DicWeapon[listBoosterUpgrade[i].Index] >= 5)
                        listBoosterUpgrade.RemoveAt(i);

            listBoosterUpgrade.Remove(MgrInGameData.Instance.GetBoosterData("Etc_1"));
            listBoosterUpgrade.Remove(MgrInGameData.Instance.GetBoosterData("Etc_2"));
        }

        // 튜토리얼 이거나 2챕터 클리어가 되지 않았을 경우 자동 효율 부스터 제거
        //if ((MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter) || DataManager.Instance.UserInventory.userCurrentChapter < 2)
        listBoosterUpgrade.Remove(MgrInGameData.Instance.GetBoosterData("skill_passive_010"));
    }

    private void Update()
    {
        if ((boosterStackCnt <= 0 && listBoosterRandomStack.Count <= 0) || !MgrBattleSystem.Instance.isStageStart)
            return;

        if (IsOpenBoosterCard || MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf)
            return;

        if(listBoosterRandomStack.Count > 0)
        {
            if ((MgrBakingSystem.Instance.TycoonStep == 0 && MgrBakingSystem.Instance.IsBtnPressed) || (MgrBakingSystem.Instance.TycoonStep == 1 && !MgrBakingSystem.Instance.IsAuto))
                return;

            ShowRandomBoosterUpgrade();
        }
        else
        {
            if (MgrBakingSystem.Instance.IsTycoonEnable)
                return;

            if(MgrBattleSystem.Instance.TutorialStep == 8)
            {
                if(MgrBattleSystem.Instance.TutorialSubStep == 1)
                {
                    MgrBattleSystem.Instance.TutorialSubStep = 2;
                    TaskTutorialBooster().Forget();
                }
                return;
            }

            boosterStackCnt--;
            UpdateBoosterPointInSlider();
            ShowBoosterUpgrade();
        }
    }

    private async UniTaskVoid TaskTutorialBooster()
    {
        MgrBattleSystem.Instance.SetTutorialTimeScale(true);
        MgrBattleSystem.Instance.ShowTutorialTextUI(9, ANCHOR_TYPE.TOP_LEFT, new Vector2(0.5f, 1.0f), new Vector2(1475.0f, -320.0f));
        MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(1475.0f, -100.0f), new Vector2(1000.0f, 175.0f), ANCHOR_TYPE.TOP_LEFT, 1);
        MgrBattleSystem.Instance.ToggleTutorialUI(true);

        await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrBattleSystem.Instance.ShowTutorialTextUI(10, ANCHOR_TYPE.TOP_LEFT, new Vector2(0.5f, 1.0f), new Vector2(1475.0f, -320.0f));
        MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(1475.0f, -100.0f), new Vector2(1000.0f, 175.0f), ANCHOR_TYPE.TOP_LEFT, 1);
        MgrBattleSystem.Instance.ToggleTutorialUI(true);

        await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrBattleSystem.Instance.ShowTutorialTextUI(11, ANCHOR_TYPE.TOP_LEFT, new Vector2(0.5f, 1.0f), new Vector2(1180.0f, -320.0f));
        MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(1180.0f, -170.0f), new Vector2(200.0f, 200.0f), ANCHOR_TYPE.TOP_LEFT, 1);
        MgrBattleSystem.Instance.ToggleTutorialUI(true);

        await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrBattleSystem.Instance.SetTutorialTimeScale(false);
        MgrBattleSystem.Instance.TutorialStep = 11;
    }

    /// <summary>
    /// 현재 부스터 레벨 기반 최대 EXP 세팅
    /// </summary>
    public void SetBoosterMaxEXP(float _divideAmount = 0.0f)
    {
        boosterEXPData = DataManager.Instance.GetBoosterExpData(boosterLv + 1);
        maxBoosterExp = boosterEXPData is null ? DataManager.Instance.GetBoosterExpData(60).Exp : boosterEXPData.Exp;

        imgBoosterGuage.fillAmount = prevBoosterGuageValue;

        token_BoosterExp?.Cancel();
        token_BoosterExp?.Dispose();
        token_BoosterExp = new CancellationTokenSource();
        TaskBoosterFillAmount(imgBoosterGuage.fillAmount, boosterExp / maxBoosterExp, _divideAmount, true).Forget();
    }

    /// <summary>
    /// 부스터 경험치 증가
    /// </summary>
    /// <param name="_amount">증가량</param>
    /// <param name="_originalAmount">증가량 원본</param>
    public void AddBoosterExp(float _amount, float _originalAmount = 0.0f)
    {
        if (MgrBattleSystem.Instance.IsTestMode || (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter && MgrBattleSystem.Instance.TutorialStep < 7) || MgrBattleSystem.Instance.GameMode is not GAME_MODE.Chapter)
            return;

        float resultAmount = _amount;
        float divideAmount = _amount - _originalAmount;

        if (MgrBattleSystem.Instance.IsChallengeMode && MgrBattleSystem.Instance.ChallengeLevel == 1)
            resultAmount *= (1.0f - (float)DataManager.Instance.GetChallengePenaltyData("penalty_000002").Param[0]);

        boosterExp += resultAmount;

        if(boosterExp >= maxBoosterExp)
        {
            MgrSound.Instance.PlayOneShotSFX("SFX_Booster_Point_Count", 1.0f);

            boosterLv++;
            tmpBoosterLevel.text = $"{boosterLv}";

            boosterExp -= maxBoosterExp;

            SetBoosterMaxEXP(divideAmount);

            boosterStackCnt++;
            UpdateBoosterPointInSlider();
        }
        else
        {
            imgBoosterGuage.fillAmount = prevBoosterGuageValue;

            token_BoosterExp?.Cancel();
            token_BoosterExp?.Dispose();
            token_BoosterExp = new CancellationTokenSource();
            TaskBoosterFillAmount(imgBoosterGuage.fillAmount, boosterExp / maxBoosterExp, divideAmount).Forget();
        }
    }

    private float prevBoosterGuageValue = 0.0f;
    private async UniTaskVoid TaskBoosterFillAmount(float _baseValue, float _changeValue, float _divideAmount, bool _isLevelUp = false)
    {
        prevBoosterGuageValue = _changeValue;

        float firstTime = 0.0f;
        float totalTime = 0.25f;

        if(_isLevelUp)
        {
            float totalUpgradeValue = (1.0f - _baseValue) + _changeValue;
            firstTime = Mathf.Lerp(0.0f, 0.25f, (1.0f - _baseValue) / totalUpgradeValue);
            totalTime -= firstTime;

            float maxFirstTime = firstTime;
            while(firstTime > 0.0f)
            {
                firstTime -= Time.deltaTime;
                imgBoosterGuage.fillAmount = Mathf.Lerp(imgBoosterGuage.fillAmount, 1.0f, (maxFirstTime - firstTime) / maxFirstTime);

                await UniTask.Yield(cancellationToken: token_BoosterExp.Token);
            }

            imgBoosterGuage.fillAmount = 0.0f;

            float maxTotalTime = totalTime;
            while (totalTime > 0.0f)
            {
                totalTime -= Time.deltaTime;
                imgBoosterGuage.fillAmount = Mathf.Lerp(imgBoosterGuage.fillAmount, _changeValue, (maxTotalTime - totalTime) / maxTotalTime);

                await UniTask.Yield(cancellationToken: token_BoosterExp.Token);
            }
        }
        else
        {
            while (totalTime > 0.0f)
            {
                totalTime -= Time.deltaTime;
                imgBoosterGuage.fillAmount = Mathf.Lerp(imgBoosterGuage.fillAmount, _changeValue, (0.25f - totalTime) / 0.25f);

                await UniTask.Yield(cancellationToken: token_BoosterExp.Token);
            }
        }
    }

    /// <summary>
    /// 부스터 레벨 증가
    /// </summary>
    /// <param name="_amount">증가량</param>
    public void AddBoosterLv(int _amount)
    {
        if (_amount < 1)
            return;

        MgrSound.Instance.PlayOneShotSFX("SFX_Booster_Point_Count", 1.0f);

        boosterLv += _amount;
        tmpBoosterLevel.text = $"{boosterLv}";

        SetBoosterMaxEXP();

        boosterStackCnt += _amount;
        UpdateBoosterPointInSlider();
    }

    private void UpdateBoosterPointInSlider()
    {
        int childCnt = objBoosterPointSliderRoot.transform.childCount;
        if (childCnt < boosterStackCnt)
        {
            int addPrefabCnt = boosterStackCnt - childCnt;
            for (int i = 0; i < addPrefabCnt; i++)
                Instantiate(objBoosterPoint_InSliderPrefab, objBoosterPointSliderRoot.transform).SetActive(false);
        }

        childCnt = objBoosterPointSliderRoot.transform.childCount;
        GameObject objTarget = null;
        for (int i = 0; i < childCnt; i++)
        {
            objTarget = objBoosterPointSliderRoot.transform.GetChild(i).gameObject;
            bool isActivated = objTarget.activeSelf;
            objTarget.SetActive(i < boosterStackCnt);
            objTarget.GetComponent<BoosterPoint>().SetBoosterPointTimeline(!isActivated ? 0 : 1);
        }
    }

    /// <summary>
    /// 랜덤 부스터 레벨 증가
    /// </summary>
    /// <param name="_amount">증가량</param>
    public void AddRandomBoosterLv(Vector3 _v3DropPos, int _amount)
    {
        if (_amount < 1)
            return;

        TaskRandomBoosterDrop(_v3DropPos, _amount).Forget();
    }

    private List<GameObject> listObjBeacon = new List<GameObject>();
    private async UniTaskVoid TaskRandomBoosterDrop(Vector3 _v3DropPos, int _amount)
    {
        MgrSound.Instance.PlayOneShotSFX("SFX_Booster_Call_Bubble_Appear", 1.0f);

        SkeletonAnimation skaBeacon = MgrObjectPool.Instance.ShowObj("RouletteBeacon", _v3DropPos).GetComponent<SkeletonAnimation>();
        skaBeacon.skeleton.SetSkin($"{_amount}");
        skaBeacon.AnimationState.SetAnimation(0, "idle", true);

        skaBeacon.GetComponent<MeshRenderer>().sortingLayerName = "Unit";
        skaBeacon.GetComponent<MeshRenderer>().sortingOrder = 0;

        listObjBeacon.Add(skaBeacon.gameObject);

        await UniTask.Delay(200, cancellationToken: this.GetCancellationTokenOnDestroy());

        Vector3 v3EndPos = MgrCamera.Instance.CameraMain.transform.position + new Vector3(0.0f, 2.0f, 0.0f);
        v3EndPos.z = 0.0f;
        skaBeacon.transform.DOMove(v3EndPos, 1.0f).SetEase(Ease.InQuad);

        skaBeacon.GetComponent<MeshRenderer>().sortingLayerName = "VFX Unit front";
        skaBeacon.GetComponent<MeshRenderer>().sortingOrder = 999;

        await UniTask.Delay(1300, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrSound.Instance.PlayOneShotSFX("SFX_Booster_Call_Bubble_Break", 1.0f);
        MgrSound.Instance.PlayOneShotSFX("SFX_Booster_Call_Trigger", 1.0f);

        skaBeacon.AnimationState.SetAnimation(0, "open", false);
        skaBeacon.GetComponent<PlayableDirector>().Play();

        await UniTask.Delay(750, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrSound.Instance.PlayOneShotSFX("SFX_Booster_Call_Trigger", 1.0f);

        await UniTask.Delay(750, cancellationToken: this.GetCancellationTokenOnDestroy());

        boosterLv += _amount;
        tmpBoosterLevel.text = $"{boosterLv}";

        SetBoosterMaxEXP();

        listBoosterRandomStack.Add(_amount);
    }

    /// <summary>
    /// 부스터 업그레이드 UI 출력
    /// </summary>
    public void ShowBoosterUpgrade()
    {
        // # 임시 -> 부스터 업그레이드 가능한 것이 0개인 경우 기타 띄우기
        if (listBoosterUpgrade.Count == 0)
        {
            if (!MgrBattleSystem.Instance.IsChallengeMode)
                listBoosterUpgrade.Add(MgrInGameData.Instance.GetBoosterData("Etc_1"));

            listBoosterUpgrade.Add(MgrInGameData.Instance.GetBoosterData("Etc_2"));
        }

        ObjBoosterSelect.GetComponent<RectTransform>().offsetMax = new Vector2(0.0f, -75.0f);
        ObjBoosterSelectVFX.GetComponent<RectTransform>().offsetMax = new Vector2(0.0f, -75.0f);

        UpdateCurrentBooster();
        canvgSelectText.alpha = 0.0f;

        // 게임 일시 정지
        Time.timeScale = 0.00001f;
        IsOpenBoosterCard = true;
        MgrSound.Instance.PauseAllSFX();

        // 리스트 셔플
        listBoosterUpgrade.Shuffle();

        MgrSound.Instance.PlayOneShotSFX("SFX_Booster_Card_Reverse", 1.0f);

        // 업그레이드 가능 갯수 체크
        int canUpgradeCnt = listBoosterUpgrade.Count >= 3 ? 3 : listBoosterUpgrade.Count;
        for (int i = 0; i < ObjBoosterSelect.transform.childCount; i++)
        {
            // 업그레이드 가능한 카드인 경우
            if(i < canUpgradeCnt)
            {
                // 출력할 업그레이드 타입에 따른 딕셔너리 임시 할당
                dicTemp = GetDicUpgradeType(listBoosterUpgrade[i].Type);
                // 업그레이드 되지 않은 항목이라면 레벨 0, 업그레이드 된 항목이라면 해당 레벨 보내주기
                //objBoosterSelect.transform.GetChild(i).GetComponent<BoosterCard>().SetBoosterCard(listBoosterUpgrade[i], dicTemp.ContainsKey(listBoosterUpgrade[i].Index) ? dicTemp[listBoosterUpgrade[i].Index] : 0);
                ObjBoosterSelect.transform.GetChild(i).GetComponent<BoosterCard>().ShowBoosterCard(listBoosterUpgrade[i], dicTemp.ContainsKey(listBoosterUpgrade[i].Index) ? dicTemp[listBoosterUpgrade[i].Index] : 0, i);
                ObjBoosterSelect.transform.GetChild(i).gameObject.SetActive(true);
            }
            else // 갯수가 부족하여 업그레이드 카드가 아닌 경우
            {
                // 필요하지 않으니 카드 비활성화
                ObjBoosterSelect.transform.GetChild(i).gameObject.SetActive(false);
            }
        }

        // 선택 잠시 불가능 하도록 최상위 레이캐스트 오브젝트 활성화
        objDelaySelect.SetActive(true);
        TaskDelayRaycast().Forget();

        // 현재 선택한 부스터 리스트 Active
        ToggleRootCurrentBoosterImage(true, true);

        // 최종 적으로 부스터 선택 창 활성화
        imgBoosterRaycast.color = new Color(0.0f, 0.0f, 0.0f, 0.75f);
        imgBoosterRaycast.gameObject.SetActive(true);
        ObjBoosterSelect.SetActive(true);

        // 리롤 여부 확인 하여 버튼 활성화
        ToggleRerollBtn(currRerollCnt < MgrBattleSystem.Instance.GlobalOption.Option_BoosterReRollCnt);

        objOtherUI.SetActive(true);

        // 튜토리얼
        if (MgrBattleSystem.Instance.TutorialStep == 11)
            TaskTutorialSelectBooster().Forget();
    }

    private async UniTaskVoid TaskTutorialSelectBooster()
    {
        await UniTask.Delay(500, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrBattleSystem.Instance.ShowTutorialTextUI(12, ANCHOR_TYPE.TOP, new Vector2(0.5f, 1.0f), new Vector2(0.0f, -450.0f));
        MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(0.0f, -250.0f), new Vector2(200.0f, 200.0f), ANCHOR_TYPE.TOP, 1);
        MgrBattleSystem.Instance.ToggleTutorialUI(true);

        await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrBattleSystem.Instance.ShowTutorialTextUI(13, ANCHOR_TYPE.CENTER, new Vector2(0.5f, 0.5f), new Vector2(0.0f, -550.0f));
        MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(0.0f, 0.0f), new Vector2(1900.0f, 900.0f), ANCHOR_TYPE.CENTER, 0);
        MgrBattleSystem.Instance.ToggleTutorialUI(true);

        await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrBattleSystem.Instance.ShowTutorialTextUI(14, ANCHOR_TYPE.CENTER, new Vector2(0.5f, 0.5f), new Vector2(0.0f, -550.0f));
        MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(0.0f, 0.0f), new Vector2(1900.0f, 900.0f), ANCHOR_TYPE.CENTER, 0);
        MgrBattleSystem.Instance.ToggleTutorialUI(true);

        await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrBattleSystem.Instance.TutorialStep = 14;
    }

    public void ToggleRerollBtn(bool _isToggle) => objReroll.SetActive(_isToggle);

    private CurrentBoosterSlot currBoosterSelectSlot;
    public void ShowPauseBoosterSelcted(BoosterData.BoosterInfo _info, CurrentBoosterSlot _slot)
    {
        if (!MgrBattleSystem.Instance.IsPause)
            return;

        currBoosterSelectSlot = _slot;
        dicTemp = GetDicUpgradeType(_info.Type);
        currBoosterSelected.SetBoosterCard(_info, dicTemp[_info.Index], true);
        objCanvCurrBoosterSelected.SetActive(true);
    }

    public void HidePuaseBoosterSelected()
    {
        if (currBoosterSelectSlot is not null)
        {
            currBoosterSelectSlot.SetSelectUI(false);
            currBoosterSelectSlot = null;
        }
        objCanvCurrBoosterSelected.SetActive(false);
    }

    private async UniTaskVoid TaskDelayRaycast()
    {
        await UniTask.Delay(300, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        objDelaySelect.SetActive(false);

        canvgSelectText.DOKill();
        canvgSelectText.DOFade(1.0f, 1.0f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetEase(Ease.Linear);
    }

    public void UpdateCurrentBooster(BoosterData.BoosterInfo _info = null)
    {
        foreach (CurrentBoosterSlot slot in arrCurrBoosterSlot)
            slot.UpdateBoosterSlot(_info);
    }

    /// <summary>
    /// 랜덤 부스터 업그레이드 UI 출력
    /// </summary>
    [Header("랜덤 룰렛")]
    [SerializeField] private SkeletonGraphic skgRoulette;
    [SerializeField] private Button btnRouletteStart;
    private RectTransform rtRouletteStart;
    [SerializeField] private Button btnRouletteStop;

    [SerializeField] private GameObject objSubBackground;
    [SerializeField] private Image imgRouletteCardFrame;

    private RouletteSlot[] arrRouletteSlot;

    [SerializeField] private ParticleSystem parsysRouletteBackVFX;
    [SerializeField] private ParticleSystem parsysRouletteDustVFX;
    [SerializeField] private ParticleSystem parsysRouletteStopVFX;
    [SerializeField] private ParticleSystem parsysRouletteGetVFX;

    public void ShowRandomBoosterUpgrade()
    {
        // 게임 일시 정지
        Time.timeScale = 0.00001f;
        IsOpenBoosterCard = true;
        MgrSound.Instance.PauseAllSFX();

        MgrSound.Instance.PlayOneShotSFX("SFX_Booster_Roulette_Appear", 1.0f);

        // 스파인 출력
        objRandomBoosterUFX.SetActive(true);
        imgBoosterRaycast.color = new Color(0.0f, 0.0f, 0.0f, 0.75f);
        imgBoosterRaycast.gameObject.SetActive(true);

        skgRoulette.Skeleton.SetSkin($"{listBoosterRandomStack[0]}");
        skgRoulette.Skeleton.SetSlotsToSetupPose();
        skgRoulette.AnimationState.SetAnimation(0, "open", false);

        skgRoulette.AnimationState.Complete -= OnRouletteComplete;
        skgRoulette.AnimationState.Complete += OnRouletteComplete;

        // VFX 세팅
        parsysRouletteBackVFX.Clear();
        parsysRouletteDustVFX.Clear();

        MgrObjectPool.Instance.HideObj("RouletteBeacon", listObjBeacon[0]);
        listObjBeacon.RemoveAt(0);
    }

    private CancellationTokenSource token_RouletteAutoStop;
    private int rollSFXChannel = -1;
    public void OnBtn_StartRoulette()
    {
        rollSFXChannel = MgrSound.Instance.PlaySFX("SFX_Booster_Roulette_Roll", 1.0f, true);

        btnRouletteStart.gameObject.SetActive(false);
        btnRouletteStop.gameObject.SetActive(true);
        skgRoulette.AnimationState.SetAnimation(0, "start", false);
        skgRoulette.AnimationState.AddAnimation(0, "start_idle", true, 0.0f);

        int canUpgradeCnt = listBoosterRandomStack[0];
        for (int i = 0; i < arrRouletteSlot.Length; i++)
        {
            arrRouletteSlot[i].gameObject.SetActive(i < canUpgradeCnt);
            if (i < canUpgradeCnt)
                arrRouletteSlot[i].SetRoll();
        }

        parsysRouletteDustVFX.Play();

        token_RouletteAutoStop?.Cancel();
        token_RouletteAutoStop?.Dispose();
        token_RouletteAutoStop = new CancellationTokenSource();
        TaskRouletteAutoStop().Forget();
    }

    private async UniTaskVoid TaskRouletteAutoStop()
    {
        await UniTask.Delay(4000, true, cancellationToken: token_RouletteAutoStop.Token);
        OnBtn_StopRoulette();
    }

    private List<BoosterData.BoosterInfo> listResultRandomBooster = new List<BoosterData.BoosterInfo>();
    public void OnBtn_StopRoulette()
    {
        MgrSound.Instance.StopSFX("SFX_Booster_Roulette_Roll", rollSFXChannel);

        token_RouletteAutoStop?.Cancel();

        btnRouletteStop.gameObject.SetActive(false);
        skgRoulette.AnimationState.ClearTrack(0);
        skgRoulette.AnimationState.SetAnimation(0, "stop", false);

        parsysRouletteBackVFX.Stop();

        TaskStopRoulette().Forget();
    }

    private void OnRouletteComplete(TrackEntry trackEntry)
    {
        string animationName = trackEntry.Animation.Name;

        if (animationName.Equals("open"))
        {
            parsysRouletteBackVFX.Play();

            rtRouletteStart.anchoredPosition = new Vector2(-217.0f, -300.0f);
            btnRouletteStart.enabled = false;
            btnRouletteStart.gameObject.SetActive(true);
            rtRouletteStart.DOKill();
            rtRouletteStart.DOAnchorPosY(118.0f, 0.25f).SetEase(Ease.Linear).SetUpdate(true).OnComplete(() => btnRouletteStart.enabled = true);
        }
    }

    private async UniTaskVoid TaskStopRoulette()
    {
        // 업그레이드 가능 갯수 체크
        int canUpgradeCnt = listBoosterRandomStack[0];

        listBoosterRandomStack.RemoveAt(0);

        listResultRandomBooster.Clear();

        MgrSound.Instance.PlayOneShotSFX("SFX_Booster_Roulette_Stop", 1.0f);

        for (int i = 0; i < canUpgradeCnt; i++)
        {
            if (listRandomBoosterUpgrade.Count == 0)
            {
                if(SelectBoosterGoldCnt < 5 && !MgrBattleSystem.Instance.IsChallengeMode)
                {
                    SelectBoosterGoldCnt++;
                    listRandomBoosterUpgrade.Add(MgrInGameData.Instance.GetBoosterData("Etc_1"));
                }
                else
                    listRandomBoosterUpgrade.Add(MgrInGameData.Instance.GetBoosterData("Etc_2"));
            }

            // 리스트 셔플
            listRandomBoosterUpgrade.Shuffle();

            // 결과 리스트 저장
            listResultRandomBooster.Add(listRandomBoosterUpgrade[0]);

            dicTemp = GetDicUpgradeType(listRandomBoosterUpgrade[0].Type);
            ObjBoosterSelect.transform.GetChild(i).GetComponent<BoosterCard>().SetBoosterCard(listRandomBoosterUpgrade[0], dicTemp.ContainsKey(listRandomBoosterUpgrade[0].Index) ? dicTemp[listRandomBoosterUpgrade[0].Index] : 0);

            arrRouletteSlot[i].SetResultIcon(listRandomBoosterUpgrade);

            SetRandomBoosterUpgrade(listRandomBoosterUpgrade[0]);

            listRandomBoosterUpgrade.Remove(MgrInGameData.Instance.GetBoosterData("Etc_1"));
            listRandomBoosterUpgrade.Remove(MgrInGameData.Instance.GetBoosterData("Etc_2"));

            await UniTask.Delay(50, true, cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        await UniTask.Delay(760, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        parsysRouletteDustVFX.Stop();
        parsysRouletteStopVFX.Play();

        await UniTask.Delay(500, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        ObjBoosterSelect.GetComponent<RectTransform>().offsetMax = new Vector2(0.0f, 0.0f);
        ObjBoosterSelectVFX.GetComponent<RectTransform>().offsetMax = new Vector2(0.0f, 0.0f);

        MgrSound.Instance.PlayOneShotSFX("SFX_Booster_Roulette_Get", 1.0f);

        for (int i = 0; i < ObjBoosterSelect.transform.childCount; i++)
        {
            if(i >= canUpgradeCnt)
            {
                ObjBoosterSelect.transform.GetChild(i).gameObject.SetActive(false);
                ObjBoosterSelectVFX.transform.GetChild(i).gameObject.SetActive(false);
                continue;
            }

            RemainRandomSelect++;

            // 업그레이드 가능한 카드인 경우 출력할 업그레이드 타입에 따른 딕셔너리 임시 할당
            dicTemp = GetDicUpgradeType(listResultRandomBooster[i].Type);
            // 업그레이드 되지 않은 항목이라면 레벨 0, 업그레이드 된 항목이라면 해당 레벨 보내주기
            ObjBoosterSelect.transform.GetChild(i).GetComponent<BoosterCard>().ShowBoosterRandomCard(i);
            ObjBoosterSelect.transform.GetChild(i).gameObject.SetActive(true);
            ObjBoosterSelectVFX.transform.GetChild(i).gameObject.SetActive(true);
        }
        ObjBoosterSelect.SetActive(true);
        imgRouletteCardFrame.rectTransform.sizeDelta = new Vector2(632.0f + (622.0f * (canUpgradeCnt - 1)), 932.0f);
        objSubBackground.SetActive(true);

        parsysRouletteGetVFX.Clear();
        parsysRouletteGetVFX.Play();
    }

    /// <summary>
    /// 부스터 업그레이드 함수
    /// </summary>
    /// <param name="_info">업그레이드 할 부스터 정보</param>
    /// <param name="_isOppoBooster">상대방 부스터인지 (PVP 무기 부스터 용)</param>
    public void SetBoosterUpgrade(BoosterData.BoosterInfo _info, bool _isOppoBooster = false)
    {
        if (_info.Type == BoosterType.Etc)
        {
            if(_info.Index.Equals("Etc_1"))
            {
                SelectBoosterGoldCnt++;
                if(SelectBoosterGoldCnt >= 5)
                    listBoosterUpgrade.Remove(MgrInGameData.Instance.GetBoosterData("Etc_1"));
            }
            MgrInGameEvent.Instance.BroadcastBoosterUpgradeEvent(_info.Index);
            //ClosedBoosterCard();
            return;
        }

        // 임시 딕셔너리에 맞는 타입 딕셔너리 할당
        dicTemp = GetDicUpgradeType(_info.Type);

        // 만약 이미 업그레이드 된 항목이면 값 증가, 처음 업그레이드 하는 것이면 새로 추가
        if (dicTemp.ContainsKey(_info.Index)) dicTemp[_info.Index]++;
        else
        {
            dicTemp.Add(_info.Index, MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode ? 5 : 1);
        }

        // 만약 최대 레벨 이상 도달 했을 경우 업그레이드 가능 항목에서 삭제
        if (dicTemp[_info.Index] >= _info.MaxLevel && _info.Type != BoosterType.Etc)
        {
            listBoosterUpgrade.Remove(_info);
            listRandomBoosterUpgrade.Remove(_info);
        }

        // 엑티브 타입인 경우 사이드 스킬 세팅
        if (_info.Type == BoosterType.Active && (dicTemp[_info.Index] == 1 || MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode))
            MgrBattleSystem.Instance.AddSideSkill(_info.Index, _info.Icon);

        if (_info.Type == BoosterType.Weapon)
        {
            if (_isOppoBooster) MgrBattleSystem.Instance.WeaponSysOppo.SetWeaponBoosterData(dicTemp[_info.Index]);
            else MgrBattleSystem.Instance.WeaponSys.SetWeaponBoosterData(dicTemp[_info.Index]);
        }

        // 만약 해당 업그레이드가 첫 획득인 경우
        if (dicTemp[_info.Index] == 1 || MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode)
        {
            // 부스터 타입에 따른 행동 개시
            SetCurrentBooster(_info);
            // 랜덤 부스터 업그레이드 가능 항목에 리스트 추가
            listRandomBoosterUpgrade.Add(_info);
        }

        // 각 타입에 따른 업그레이드 가능 부스터 삭제 체크
        CheckBoosterUpgradeToRemove();

        MgrInGameEvent.Instance.BroadcastBoosterUpgradeEvent(_info.Index);
    }

    public void SetFadeOutBoosterRaycast()
    {
        canvgSelectText.DOKill();
        canvgSelectText.alpha = 0;

        imgBoosterRaycast.DOKill();
        imgBoosterRaycast.DOFade(0.0f, 0.25f).SetEase(Ease.Linear).SetUpdate(true);
    }
    
    /// <summary>
    /// 랜덤 부스터 업그레이드 함수
    /// </summary>
    /// <param name="_info">업그레이드 할 부스터 정보</param>
    public void SetRandomBoosterUpgrade(BoosterData.BoosterInfo _info)
    {
        if(_info.Type == BoosterType.Etc)
        {
            MgrInGameEvent.Instance.BroadcastBoosterUpgradeEvent(_info.Index);
            return;
        }

        // 임시 딕셔너리에 맞는 타입 딕셔너리 할당
        dicTemp = GetDicUpgradeType(_info.Type);

        // 만약 이미 업그레이드 된 항목이면 값 증가, 처음 업그레이드 하는 것이면 에러
        if (dicTemp.ContainsKey(_info.Index)) dicTemp[_info.Index]++;
        else
        {
            Debug.LogError($"예외 발생 - {_info.Index} 첫 업그레이드 시도");
            return;
        }

        // 만약 최대 레벨 이상 도달 했을 경우 업그레이드 가능 항목에서 삭제
        if (dicTemp[_info.Index] >= _info.MaxLevel)
        {
            listBoosterUpgrade.Remove(_info);
            listRandomBoosterUpgrade.Remove(_info);
        }

        if (_info.Type == BoosterType.Weapon)
            MgrBattleSystem.Instance.WeaponSys.SetWeaponBoosterData(dicTemp[_info.Index]);

        MgrInGameEvent.Instance.BroadcastBoosterUpgradeEvent(_info.Index);
    }

    public void ToggleRootCurrentBoosterImage(bool _isToggle, bool _isShowRemainSelectCount = false)
    {
        rtCurrBoosterRoot.DOKill();

        if (_isToggle)
        {
            objBoosterPointSelectUIRoot.SetActive(_isShowRemainSelectCount);
            UpdateBoosterPointInSelectUI();
            rtCurrBoosterRoot.DOAnchorPosY(-75.0f, 0.125f).SetUpdate(true).SetEase(Ease.Linear);
        }
        else rtCurrBoosterRoot.DOAnchorPosY(200.0f, 0.125f).SetUpdate(true).SetEase(Ease.Linear);
    }

    public void UpdateBoosterPointInSelectUI(int _discount = 0)
    {
        int childCnt = objBoosterPointSelectUIRoot.transform.childCount;
        if (childCnt < boosterStackCnt + 1)
        {
            int addPrefabCnt = (boosterStackCnt + 1) - childCnt;
            for (int i = 0; i < addPrefabCnt; i++)
                Instantiate(objBoosterPoint_InSelectUIPrefab, objBoosterPointSelectUIRoot.transform).SetActive(false);
        }

        childCnt = objBoosterPointSelectUIRoot.transform.childCount;
        GameObject objTarget = null;
        for (int i = 0; i < childCnt; i++)
        {
            bool isActive = i <= boosterStackCnt - _discount;
            objTarget = objBoosterPointSelectUIRoot.transform.GetChild(i).gameObject;
            objTarget.SetActive(isActive);
            if(isActive)
                objTarget.GetComponent<BoosterPoint>().SetBoosterPointTimeline(1);
        }
    }

    /// <summary>
    /// 부스터 카드 닫기 함수
    /// </summary>
    public void ClosedRandomBoosterCard()
    {
        RemainRandomSelect--;
        if(RemainRandomSelect == 0)
        {
            Resources.UnloadUnusedAssets();
            btnRandomBoosterGet.enabled = true;
        }
    }

    public void ClosedRandomRoulette()
    {
        btnRandomBoosterGet.enabled = false;
        objSubBackground.SetActive(false);

        for(int i = 0; i < ObjBoosterSelect.transform.childCount; i++)
        {
            if (ObjBoosterSelect.transform.GetChild(i).gameObject.activeSelf)
                ObjBoosterSelect.transform.GetChild(i).GetComponent<BoosterCard>().RandomRouletteCardVFX();
        }
        ObjBoosterSelectVFX.SetActive(true);

        parsysRouletteGetVFX.Stop();
        parsysRouletteGetVFX.Clear();

        TaskClosedRandomRoulette().Forget();
    }

    private async UniTaskVoid TaskClosedRandomRoulette()
    {
        UpdateCurrentBooster();
        ToggleRootCurrentBoosterImage(true);
        skgRoulette.AnimationState.SetAnimation(0, "close", false);

        MgrSound.Instance.PlayOneShotSFX("SFX_Booster_Roulette_Exit", 1.0f);

        await UniTask.Delay(150, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        foreach (var slot in arrRouletteSlot)
            slot.gameObject.SetActive(false);

        await UniTask.Delay(850, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        ObjBoosterSelect.SetActive(false);
        ObjBoosterSelectVFX.SetActive(false);

        SetFadeOutBoosterRaycast();
        ToggleRootCurrentBoosterImage(false);

        await UniTask.Delay(250, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        ClosedBoosterCard();
    }

    /// <summary>
    /// 부스터 카드 닫기 함수
    /// </summary>
    public void ClosedBoosterCard()
    {
        // 부스터 선택 창 비활성화
        btnRandomBoosterGet.enabled = false;
        objRandomBoosterUFX.SetActive(false);
        imgBoosterRaycast.gameObject.SetActive(false);
        ObjBoosterSelect.SetActive(false);
        objReroll.SetActive(false);

        canvgSelectText.DOKill();
        canvgSelectText.alpha = 0;

        objOtherUI.SetActive(false);

        Time.timeScale = 1.2f;
        IsOpenBoosterCard = false;
        MgrSound.Instance.UnPauseAllSFX();

        if (MgrBattleSystem.Instance.TutorialStep == 14 && boosterStackCnt == 0)
            TaskTutorialCloseBooster().Forget();
    }

    private async UniTaskVoid TaskTutorialCloseBooster()
    {
        MgrBattleSystem.Instance.SetTutorialTimeScale(true);
        MgrBattleSystem.Instance.ShowTutorialTextUI(15, ANCHOR_TYPE.CENTER, new Vector2(0.5f, 0.5f), new Vector2(0.0f, 0.0f));
        MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f), ANCHOR_TYPE.CENTER, 0);
        MgrBattleSystem.Instance.ToggleTutorialUI(true);

        await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrBattleSystem.Instance.TutorialStep = 15;
        MgrBattleSystem.Instance.SetTutorialTimeScale(false);
    }

    public void OnBtn_Reroll()
    {
        currRerollCnt++;
        ShowBoosterUpgrade();
    }

    #region 현재 획득한 부스터 관련 함수
    public void SetCurrentBooster(BoosterData.BoosterInfo _info)
    {
        switch(_info.Type)
        {
            case BoosterType.Weapon:
                arrCurrBoosterSlot[0].SetCurrentBoosterSlot(_info);
                break;
            case BoosterType.Active:
                arrCurrBoosterSlot[1 + DicSkill.Count - 1].SetCurrentBoosterSlot(_info);
                break;
            default:
                arrCurrBoosterSlot[4 + DicEtc.Count - 1].SetCurrentBoosterSlot(_info);
                break;
        }
    }
    #endregion

    #region 부스터 업그레이드 가능 리스트 삭제
    private void CheckBoosterUpgradeToRemove()
    {
        if (DicWeapon.Count >= 1) RemoveUpgradeList(BoosterType.Weapon);
        if (DicSkill.Count >= 3) RemoveUpgradeList(BoosterType.Active);
        if (DicEtc.Count >= 8)
        {
            RemoveUpgradeList(BoosterType.Passive);
            RemoveUpgradeList(BoosterType.Tycoon);
            RemoveUpgradeList(BoosterType.UnitUpgrade);
        }
    }

    public void RemoveUpgradeList(BoosterType _type)
    {
        dicTemp = GetDicUpgradeType(_type);

        for (int i = listBoosterUpgrade.Count - 1; i >= 0; i--)
        {
            // 타입이 제거할 타입과 다를 경우 스킵
            if (listBoosterUpgrade[i].Type != _type)
                continue;

            // 타입은 같으나 업그레이드 항목에 포함이 되어 있으면 스킵
            if (dicTemp.ContainsKey(listBoosterUpgrade[i].Index))
                continue;

            listBoosterUpgrade.RemoveAt(i);
        }
    }
    #endregion

    /// <summary>
    /// 타입에 따른 딕셔너리 반환
    /// </summary>
    /// <param name="_type">부스터 타입</param>
    /// <returns></returns>
    private Dictionary<string, int> GetDicUpgradeType(BoosterType _type)
    {
        switch (_type)
        {
            case BoosterType.Weapon:
                return DicWeapon;
            case BoosterType.Active:
                return DicSkill;
            case BoosterType.Passive:
            case BoosterType.Tycoon:
            case BoosterType.UnitUpgrade:
                return DicEtc;
            case BoosterType.Etc:
                return DicNotUpgrade;
            default:
                Debug.Log($"{_type}");
                return null;
        }
    }

    public override void OnDestroy()
    {
        token_BoosterExp?.Cancel();
        token_BoosterExp?.Dispose();
        token_BoosterExp = null;

        base.OnDestroy();
    }
}
