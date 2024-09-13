using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using DG.Tweening;

public class UnitSlotBtn : MonoBehaviour
{
    private UnitData.UnitSetting unitInfo;
    public UnitData.UnitSetting UnitInfo => unitInfo;

    private bool isShowEffectEnd;
    private bool isCanSpawn;

    private Button btnSlot;
    private Image imgUnit;
    [SerializeField] private Image imgClass;
    [SerializeField] private Image imgCostBack;
    [SerializeField] private TextMeshProUGUI tmpCost;
    [SerializeField] private Image imgX2;
    private TextMeshProUGUI tmpCoolDown;

    [SerializeField] private string unitIndex;
    private int unitCost;
    private int originalCost;

    private float coolDown;
    private float maxCoolDown;

    // 부스트 엑티브 스킬 변수
    private bool isFirstSale;
    private bool isMultipleSpawn;

    // VFX
    [SerializeField] private ParticleSystem parsysSummonableVFX;
    [SerializeField] private ParticleSystem parsysSummonVFX;
    [SerializeField] private ParticleSystem parsysCoolDownVFX;
    [SerializeField] private ParticleSystem parsysUnitSoulVFX;
    [SerializeField] private ParticleSystem parsysCostChangeVFX;
    [SerializeField] private ParticleSystem parsysQuantityChangeVFX;

    // 도전 모드 패널티
    public float ChallengeTimer { get; private set; }
    [SerializeField] private Material matFrozen;
    [SerializeField] private GameObject objFrozenVFX;

    // 디버깅
    [SerializeField] private TextMeshProUGUI tmpDebugUnitCnt;

    private void Awake()
    {
        isCanSpawn = false;
        isShowEffectEnd = false;

        btnSlot = GetComponent<Button>();
        imgUnit = transform.GetChild(0).GetComponent<Image>();
        tmpCoolDown = transform.GetChild(2).GetComponent<TextMeshProUGUI>();

        if (MgrInGameUserData.Instance is null)
            SetUnitSlot(unitIndex);
    }

    public void SetUnitSlot(string _unitIndex)
    {
        if(string.IsNullOrEmpty(_unitIndex))
        {
            unitIndex = null;
            imgClass.gameObject.SetActive(false);
            imgCostBack.gameObject.SetActive(false);
            //return;
        }
        else
        {
            unitIndex = _unitIndex;
            unitInfo = MgrInGameData.Instance.GetUnitData(unitIndex);
            switch (unitInfo.unitClass)
            {
                case UnitClass.Warrior:
                    imgClass.sprite = MgrInGameData.Instance.SoClassImage.sprWar;
                    break;
                case UnitClass.Arch:
                    imgClass.sprite = MgrInGameData.Instance.SoClassImage.sprArch;
                    break;
                case UnitClass.Tank:
                    imgClass.sprite = MgrInGameData.Instance.SoClassImage.sprTank;
                    break;
                case UnitClass.Supporter:
                    imgClass.sprite = MgrInGameData.Instance.SoClassImage.sprSup;
                    break;
            }

            imgUnit.sprite = unitInfo.unitIcon;
            imgUnit.transform.GetChild(0).gameObject.SetActive(false);

            unitCost = unitInfo.unitCost;
            originalCost = unitCost;
            tmpCost.text = unitCost.ToString();

            MgrInGameEvent.Instance.AddActiveSkillEvent(OnActiveSkillAction);
            MgrInGameEvent.Instance.AddCanSpawnEvent(OnCanSpawnAction);

            imgUnit.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        }

        imgX2.gameObject.SetActive(false);

        transform.GetComponent<CanvasGroup>().alpha = 0.0f;

        transform.localScale = Vector3.zero;
    }

    public void InitShowEffect() => TaskShowEffect().Forget();
    public void InitHideEffect()
    {
        transform.DOScale(0.0f, 0.8f).SetEase(Ease.OutBack);
        transform.GetComponent<CanvasGroup>().DOFade(0.0f, 0.8f).SetEase(Ease.OutBack);
    }

    private async UniTaskVoid TaskShowEffect()
    {
        transform.DOScale(1.0f, 0.8f).SetEase(Ease.OutBack);

        transform.GetComponent<CanvasGroup>().DOFade(1.0f, 0.8f).SetEase(Ease.OutBack);

        await UniTask.Delay(800, cancellationToken: this.GetCancellationTokenOnDestroy());

        isShowEffectEnd = true;
    }

    public int GetCurrentCost() => int.Parse(tmpCost.text);
    public float GetCurrentCoolDown() => coolDown;
    public string GetUnitIndex() => unitIndex;
    
    public void OnBtn_SpawnUnit()
    {
        int resultCost = int.Parse(tmpCost.text);
        if (MgrBakingSystem.Instance.CurrBakedFish < resultCost || coolDown > 0.0f)
            return;

        if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter)
        {
            if (MgrBattleSystem.Instance.TutorialStep < 8 || MgrBattleSystem.Instance.TutorialStep == 20)
                return;

            if(MgrBattleSystem.Instance.TutorialStep == 8 && MgrBattleSystem.Instance.TutorialSubStep == 0)
            {
                if (unitIndex.Equals("S_War_01"))
                {
                    MgrBattleSystem.Instance.TutorialSubStep = 1;
                    MgrBattleSystem.Instance.SetTutorialTimeScale(false);
                    MgrBattleSystem.Instance.ToggleTutorialUI(false);
                }
                else return;
            }
        }

        Vector3 v3AllyBasePos = MgrBattleSystem.Instance.GetAllyBase().transform.position;

        MgrBakingSystem.Instance.UseBakedFish(resultCost);

        int spawnCnt = 1;
        if (isMultipleSpawn)
            spawnCnt++;

        TaskCoolDown().Forget();

        GameObject objUnit = null;
        for(int i = 0; i < spawnCnt; i++)
        {
            float yPos = Random.Range(0.0f, -3.5f);

            Vector3 v3SpawnPos = new Vector3(v3AllyBasePos.x - 2.0f, yPos, yPos * 0.01f);
            objUnit = MgrUnitPool.Instance.ShowObj(unitIndex, 0, v3SpawnPos);
            TaskSpawnVFX(objUnit).Forget();
        }

        MgrSound.Instance.PlayOneShotSFX("SFX_Unit_Summon", 1.0f);

        parsysSummonVFX.Play();

        RefreshCurrentUnitCnt();
    }

    private async UniTaskVoid TaskSpawnVFX(GameObject _objUnit)
    {
        await UniTask.NextFrame(cancellationToken: this.GetCancellationTokenOnDestroy());
        MgrObjectPool.Instance.ShowObj("FX_Unit Summons", _objUnit.transform.position);
    }

    private async UniTaskVoid TaskCoolDown()
    {
        coolDown = (float)BCH.Database.DataManager.Instance.GetUnitCostCooldownDatas(originalCost).CostCooldown;

        int fieldUnitCnt = MgrBattleSystem.Instance.GetUnitInSameIndex(0, unitIndex).Count;
        coolDown *= MathLib.Pow(1.01f, fieldUnitCnt * (originalCost + 1));

        float coolDownMultiplay = 1.0f;
        coolDownMultiplay += MgrBattleSystem.Instance.GlobalOption.Option_UnitSpawnCoolDown;

        coolDown *= coolDownMultiplay;

        if (unitInfo.unitClass == UnitClass.Warrior) coolDown *= 0.7f;

        maxCoolDown = coolDown;

        tmpCoolDown.gameObject.SetActive(true);

        MgrInGameEvent.Instance.BroadcastCanSpawnEvent();

        while (coolDown > 0.0f)
        {
            await UniTask.Yield(this.GetCancellationTokenOnDestroy());

            if(MgrBattleSystem.Instance.IsChallengeMode && MgrBattleSystem.Instance.ChallengeLevel == 2 && 13 <= MgrBattleSystem.Instance.ChapterID && MgrBattleSystem.Instance.ChapterID <= 18)
            {
                if (ChallengeTimer <= 0.0f)
                    coolDown -= Time.deltaTime;
            }
            else
                coolDown -= Time.deltaTime;

            tmpCoolDown.text = $"{coolDown:F1}";
        }

        //objSummonableVFX.SetActive(true);
        tmpCoolDown.gameObject.SetActive(false);

        OnCanSpawnAction();
    }

    public void ReturnFirstSaleDiscount()
    {
        if(isFirstSale)
        {
            unitCost = Mathf.FloorToInt(originalCost * (1.0f - (float)BCH.Database.DataManager.Instance.GetBoosterSkillData($"skill_active_002_{MgrBoosterSystem.Instance.DicSkill["skill_active_002"] - 1}").Params[1]) + 0.5f);

            isFirstSale = false;

            OnCanSpawnAction();
        }
    }
    
    public void ResetCooldown()
    {
        if (coolDown > 0.0f)
            coolDown = 0.0f;
    }

    public bool CheckIsSameUnitIndex(string _unitIndex) => (!(unitInfo is null) && unitInfo.unitIndex.Equals(_unitIndex));

    public void ReduceCoolDown(float _rate, bool _isUnitSoul = false)
    {
        parsysCoolDownVFX.Play();
        if (_isUnitSoul) parsysUnitSoulVFX.Play();

        if (coolDown <= 0.0f)
            return;

        coolDown -= maxCoolDown * _rate;
        MgrSound.Instance.PlayOneShotSFX("SFX_Unit_Summon_Cooldown", 1.0f);
    }

    private void OnActiveSkillAction(string _index)
    {
        switch (_index)
        {
            // 일정시간 코스트 감소
            case "skill_active_002":
                if (MgrBoosterSystem.Instance.DicSkill[_index] >= 5)
                    isFirstSale = true;

                MgrSound.Instance.PlayOneShotSFX("SFX_Unit_Cost_Change", 0.5f);

                unitCost = isFirstSale ? 0 : Mathf.FloorToInt(originalCost * (1.0f - (float)BCH.Database.DataManager.Instance.GetBoosterSkillData($"skill_active_002_{MgrBoosterSystem.Instance.DicSkill["skill_active_002"] - 1}").Params[1]) + 0.5f);

                imgCostBack.rectTransform.anchoredPosition = new Vector2(15.0f, -15.0f);
                imgCostBack.rectTransform.DOKill();
                imgCostBack.rectTransform.DOAnchorPosY(-45.0f, 0.125f).OnComplete(() => imgCostBack.rectTransform.DOAnchorPosY(-15.0f, 0.125f));
                parsysCostChangeVFX.Play();

                OnCanSpawnAction();
                TaskSkill_3().Forget();
                break;
            // 일정시간 코스트 소모 +n%, 유닛 소환 2배
            case "skill_active_003":
                isMultipleSpawn = true;

                MgrSound.Instance.PlayOneShotSFX("SFX_Skill_Active_003", 0.5f);
                MgrSound.Instance.PlayOneShotSFX("SFX_Unit_Cost_Change", 0.5f);

                imgX2.gameObject.SetActive(isMultipleSpawn);
                imgX2.DOKill();
                imgX2.rectTransform.localScale = Vector3.one * 1.75f;
                imgX2.rectTransform.DOScale(1.0f, 0.2f).SetEase(Ease.InQuad).OnComplete(() => parsysQuantityChangeVFX.Play());
                
                imgCostBack.rectTransform.anchoredPosition = new Vector2(15.0f, -15.0f);
                imgCostBack.rectTransform.DOKill();
                imgCostBack.rectTransform.DOAnchorPosY(15.0f, 0.125f).OnComplete(() => imgCostBack.rectTransform.DOAnchorPosY(-15.0f, 0.125f));
                parsysCostChangeVFX.Play();

                OnCanSpawnAction();
                TaskSkill_4().Forget();
                break;
            default:
                break;
        }
    }

    private async UniTaskVoid TaskSkill_3()
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds((float)BCH.Database.DataManager.Instance.GetBoosterSkillData($"skill_active_002_{MgrBoosterSystem.Instance.DicSkill["skill_active_002"] - 1}").Params[0]), cancellationToken:this.GetCancellationTokenOnDestroy());

        unitCost = originalCost;

        MgrSound.Instance.PlayOneShotSFX("SFX_Unit_Cost_Change", 0.5f);

        imgCostBack.rectTransform.anchoredPosition = new Vector2(15.0f, -15.0f);
        imgCostBack.rectTransform.DOKill();
        imgCostBack.rectTransform.DOAnchorPosY(15.0f, 0.125f).OnComplete(() => imgCostBack.rectTransform.DOAnchorPosY(-15.0f, 0.125f));
        parsysCostChangeVFX.Play();

        isFirstSale = false;

        OnCanSpawnAction();
    }
    
    private async UniTaskVoid TaskSkill_4()
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds((float)BCH.Database.DataManager.Instance.GetBoosterSkillData($"skill_active_003_{MgrBoosterSystem.Instance.DicSkill["skill_active_003"] - 1}").Params[0]), cancellationToken: this.GetCancellationTokenOnDestroy());

        isMultipleSpawn = false;

        MgrSound.Instance.PlayOneShotSFX("SFX_Unit_Cost_Change", 0.5f);

        imgX2.gameObject.SetActive(isMultipleSpawn);

        imgCostBack.rectTransform.anchoredPosition = new Vector2(15.0f, -15.0f);
        imgCostBack.rectTransform.DOKill();
        imgCostBack.rectTransform.DOAnchorPosY(-45.0f, 0.125f).OnComplete(() => imgCostBack.rectTransform.DOAnchorPosY(-15.0f, 0.125f));
        parsysCostChangeVFX.Play();

        OnCanSpawnAction();
    }

    private void OnCanSpawnAction()
    {
        if (!isShowEffectEnd)
            return;

        int resultCost = unitCost;
        if(isMultipleSpawn)
            resultCost = Mathf.FloorToInt(unitCost * (1.0f + (float)BCH.Database.DataManager.Instance.GetBoosterSkillData($"skill_active_003_{MgrBoosterSystem.Instance.DicSkill["skill_active_003"] - 1}").Params[1]) + 0.5f);

        tmpCost.text = resultCost.ToString();

        Color costColor = Color.white;
        if (resultCost > originalCost) costColor = new Color(1.0f, 0.25f, 0.25f);
        if (resultCost < originalCost) costColor = new Color(0.5f, 1.0f, 0.5f);
        tmpCost.color = costColor;

        if (!isCanSpawn && coolDown <= 0.0f && MgrBakingSystem.Instance.CurrBakedFish >= resultCost && !string.IsNullOrEmpty(unitIndex))
        {
            isCanSpawn = true;
            parsysSummonableVFX.Play();

            imgUnit.DOKill();
            imgUnit.DOColor(Color.white, 0.25f);

            transform.DOKill();
            transform.DOLocalMoveY(230.0f, 0.25f);
        }

        if(isCanSpawn && (coolDown > 0.0f || MgrBakingSystem.Instance.CurrBakedFish < resultCost || ChallengeTimer > 0.0f))
        {
            isCanSpawn = false;

            imgUnit.DOKill();
            imgUnit.DOColor(new Color(0.5f, 0.5f, 0.5f, 1.0f), 0.25f);

            transform.DOKill();
            transform.DOLocalMoveY(190.0f, 0.25f);
        }
    }

    public void SetChallengePanalty(float _time)
    {
        if (ChallengeTimer > 0.0f)
            return;

        ChallengeTimer = _time;

        OnCanSpawnAction();
        TaskChallengePanalty().Forget();
    }

    private async UniTaskVoid TaskChallengePanalty()
    {
        btnSlot.interactable = false;
        objFrozenVFX.SetActive(true);
        imgUnit.material = matFrozen;

        float sfxTimer = 1.0f;
        while (ChallengeTimer > 0.0f)
        {
            await UniTask.Yield(this.GetCancellationTokenOnDestroy());
            ChallengeTimer -= Time.deltaTime;
            sfxTimer -= Time.deltaTime;
            if(sfxTimer < 0.0f)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_Challenge_C3_Penalty_b", 0.2f);
                sfxTimer = 1.0f;
            }
        }

        parsysSummonableVFX.Play();
        objFrozenVFX.SetActive(false);
        imgUnit.material = null;
        btnSlot.interactable = true;

        OnCanSpawnAction();
    }

    private List<UnitBase> listUnit = new List<UnitBase>();
    public void RefreshCurrentUnitCnt()
    {
        listUnit.Clear();
        listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitInSameIndex(MgrBattleSystem.Instance.GetAllyBase(), unitIndex, true, true));

        tmpDebugUnitCnt.text = $"{listUnit.Count}";
    }
}
