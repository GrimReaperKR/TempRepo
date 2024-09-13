using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using BCH.Database;

public class BoosterCard : MonoBehaviour
{
    private Image imgCardBack;

    [SerializeField] private Sprite[] arrSprCard; // 카드 이미지
    [SerializeField] private GameObject objCardContext;
    [SerializeField] private GameObject objGlow;

    [SerializeField] private TextMeshProUGUI tmpPrefix; // 업그레이드 항목 분류
    [SerializeField] private TextMeshProUGUI tmpTitle; // 업그레이드 항목 이름
    [SerializeField] private Image imgIcon; // 아이콘
    [SerializeField] private TextMeshProUGUI tmpDesc; // 업그레이드 설명
    [SerializeField] private GameObject objNew; // NEW 이미지

    [SerializeField] private GameObject objCooldown; // 쿨타임 이미지
    private Image imgCooldown;
    [SerializeField] private TextMeshProUGUI tmpCooldown; // 쿨타임 텍스트

    [SerializeField] private GameObject objLevelGroup; // 현재 아이템의 업그레이드 레벨 현황 그룹 오브젝트
    [SerializeField] private GameObject objLevelPrefab; // 레벨 표시 프리팹

    [SerializeField] private GameObject objSelectVFX; // 선택 VFX

    [SerializeField] private GameObject objRaycast; // 레이캐스트 오브젝트

    private BoosterData.BoosterInfo currBoosterInfo;
    private int currLevel;

    private Image imgCurrLevel;

    private void Awake()
    {
        imgCardBack = GetComponent<Image>();
        imgCooldown = objCooldown.GetComponent<Image>();
    }

    public void SetBoosterCard(BoosterData.BoosterInfo _info, int _currLevel, bool _isJustShow = false)
    {
        objGlow.SetActive(false);
        objNew.SetActive(_currLevel == 0 && _info.MaxLevel > 0);
        objRaycast.SetActive(true);
        objSelectVFX.SetActive(false);

        imgCardBack.rectTransform.localScale = Vector3.one;
        imgCardBack.GetComponent<CanvasGroup>().alpha = 1.0f;

        currBoosterInfo = _info; // 받아온 업그레이드 정보 세팅
        currLevel = _isJustShow ? _currLevel - 1 : _currLevel; // 현재 카드에 레벨 세팅
        tmpPrefix.text = $"{GetPrefixTitle(_info.Type)}";
        tmpTitle.text = $"{MgrInGameData.Instance.DicLocalizationCSVData[_info.TitleName]["korea"]}";
        imgIcon.sprite = _info.Icon;
        tmpDesc.text = MgrInGameData.Instance.DicLocalizationCSVData[_info.Desc[currLevel]]["korea"]; // 현재 레벨이 4이면 5번째 문구 출력

        switch (_info.Type)
        {
            case BoosterType.Weapon:
                BoosterWeapon currWeapon = currLevel == 0 ? null : DataManager.Instance.GetBoosterWeaponData($"{_info.Index}_{currLevel - 1}");
                BoosterWeapon nextWeapon = DataManager.Instance.GetBoosterWeaponData($"{_info.Index}_{currLevel}");

                tmpCooldown.text = GetCoolDown((float)nextWeapon.Cooldown, !(currWeapon is null) && currWeapon.Cooldown != nextWeapon.Cooldown);
                objCooldown.SetActive(nextWeapon.Cooldown > 0.0f);
                break;
            case BoosterType.Etc:
                break;
            default:
                BoosterSkill currSkill = currLevel == 0 ? null : DataManager.Instance.GetBoosterSkillData($"{_info.Index}_{currLevel - 1}");
                BoosterSkill nextSkill = DataManager.Instance.GetBoosterSkillData($"{_info.Index}_{currLevel}");

                tmpCooldown.text = GetCoolDown((float)nextSkill.Cooldown, !(currSkill is null) && currSkill.Cooldown != nextSkill.Cooldown);
                objCooldown.SetActive(nextSkill.Cooldown > 0.0f);
                break;
        }

        // 만약 최대 레벨 보다 레벨 오브젝트 수량이 적을 경우 추가 생성
        if (objLevelGroup.transform.childCount < _info.MaxLevel)
        {
            int addCnt = _info.MaxLevel - objLevelGroup.transform.childCount;
            for(int i = 0; i < addCnt; i++)
                Instantiate(objLevelPrefab, objLevelGroup.transform);
        }

        // 최대 레벨 만큼 레벨 표시 오브젝트 활성 및 현재 레벨 표시
        GameObject objCurrLevel;
        Image imgTemp;
        for(int i = 0; i < objLevelGroup.transform.childCount; i++)
        {
            objLevelGroup.transform.GetChild(i).gameObject.SetActive(i < _info.MaxLevel);
            objCurrLevel = objLevelGroup.transform.GetChild(i).GetChild(0).gameObject;
            objCurrLevel.SetActive(i < currLevel + 1);
            imgTemp = objCurrLevel.GetComponent<Image>();
            imgTemp.DOKill();
            imgTemp.color = Color.white;

            if (i == currLevel && !_isJustShow)
            {
                imgCurrLevel = imgTemp;
                imgCurrLevel.DOKill();
                imgCurrLevel.DOFade(0.0f, 0.5f).SetUpdate(true).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
            }
        }
    }

    public void ShowBoosterCard(BoosterData.BoosterInfo _info, int _currLevel, int _index, bool _isJustShow = false)
    {
        SetBoosterCard(_info, _currLevel, _isJustShow);

        if (_isJustShow)
            return;

        imgCardBack.enabled = false;
        imgCardBack.sprite = arrSprCard[1];
        objCardContext.SetActive(false);
        objRaycast.SetActive(false);

        TaskBoosterCard(_index).Forget();
    }

    public void ShowBoosterRandomCard(int _index)
    {
        if(imgCurrLevel is not null)
        {
            imgCurrLevel.DOKill();
            imgCurrLevel.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        }

        imgCardBack.sprite = arrSprCard[1];
        objCardContext.SetActive(false);
        objRaycast.SetActive(false);

        TaskRandomCard(_index).Forget();
    }

    public void OnBtn_SelectCard()
    {
        if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter && MgrBattleSystem.Instance.TutorialStep < 11)
            return;

        MgrSound.Instance.PlayOneShotSFX("SFX_Booster_Card_Choice", 1.0f);

        objGlow.SetActive(true);

        objSelectVFX.transform.position = this.transform.position;
        objSelectVFX.SetActive(true);

        imgCardBack.rectTransform.DOKill();
        imgCardBack.rectTransform.DOScale(2.0f, 0.25f).SetEase(Ease.InBack, 1.0f).SetUpdate(true);
        imgCardBack.GetComponent<CanvasGroup>().DOFade(0.0f, 0.25f).SetUpdate(true);

        BoosterCard card;
        for (int i = 0; i < MgrBoosterSystem.Instance.ObjBoosterSelect.transform.childCount; i++)
        {
            card = MgrBoosterSystem.Instance.ObjBoosterSelect.transform.GetChild(i).GetComponent<BoosterCard>();
            if (card != this)
            {
                card.objRaycast.SetActive(false);
                card.GetComponent<CanvasGroup>().alpha = 0.0f;
            }
        }

        MgrBoosterSystem.Instance.SetBoosterUpgrade(currBoosterInfo);
        MgrBoosterSystem.Instance.UpdateCurrentBooster(currBoosterInfo);
        MgrBoosterSystem.Instance.SetFadeOutBoosterRaycast();
        MgrBoosterSystem.Instance.ToggleRerollBtn(false);

        objRaycast.SetActive(false);

        TaskSelectCard().Forget();
    }

    public void RandomRouletteCardVFX()
    {
        imgCardBack.rectTransform.DOKill();
        imgCardBack.rectTransform.DOScale(2.0f, 0.25f).SetEase(Ease.InBack, 1.0f).SetUpdate(true);
        imgCardBack.GetComponent<CanvasGroup>().DOFade(0.0f, 0.25f).SetUpdate(true);
    }

    private async UniTaskVoid TaskSelectCard()
    {
        MgrBoosterSystem.Instance.UpdateBoosterPointInSelectUI(1);

        await UniTask.Delay(200, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrBoosterSystem.Instance.ToggleRootCurrentBoosterImage(false);
        Resources.UnloadUnusedAssets();

        await UniTask.Delay(100, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrBoosterSystem.Instance.ClosedBoosterCard();

        // 초기화
        imgCurrLevel.DOKill();
        imgCurrLevel = null;

        currBoosterInfo = null;
        currLevel = 0;
    }

    private string GetPrefixTitle(BoosterType _type)
    {
        switch (_type)
        {
            case BoosterType.Active:
                return "스킬";
            case BoosterType.Passive:
                return "기지";
            case BoosterType.Weapon:
                return "무기";
            case BoosterType.Tycoon:
                return "타이쿤";
            case BoosterType.UnitUpgrade:
                return "유닛";
            case BoosterType.Etc:
                return "기타";
            default:
                return string.Empty;
        }
    }

    private async UniTaskVoid TaskBoosterCard(int _index)
    {
        await UniTask.Yield(this.GetCancellationTokenOnDestroy());

        imgCardBack.enabled = true;

        await UniTask.Delay(1 + 75 * _index, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        await transform.DOScaleX(0.0f, 0.1f).SetEase(Ease.Linear).SetUpdate(true).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
        imgCardBack.sprite = arrSprCard[0];
        objCardContext.SetActive(true);
        await transform.DOScaleX(1.0f, 0.1f).SetEase(Ease.Linear).SetUpdate(true).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(this.GetCancellationTokenOnDestroy());

        objRaycast.SetActive(true);
    }
    
    private async UniTaskVoid TaskRandomCard(int _index)
    {
        await UniTask.Delay(1 + 75 * _index, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        await transform.DOScaleX(0.0f, 0.1f).SetEase(Ease.Linear).SetUpdate(true).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
        imgCardBack.sprite = arrSprCard[0];
        objCardContext.SetActive(true);
        await transform.DOScaleX(1.0f, 0.1f).SetEase(Ease.Linear).SetUpdate(true).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(this.GetCancellationTokenOnDestroy());

        //objGlow.SetActive(true);
        if (imgCurrLevel is not null)
        {
            imgCurrLevel.transform.DOKill();
            imgCurrLevel.color = Color.white;
            imgCurrLevel.transform.localScale = Vector3.one * 4.0f;
            imgCurrLevel.transform.DOScale(1.0f, 0.25f).SetUpdate(true);
        }

        await UniTask.Delay(500, true, cancellationToken: this.GetCancellationTokenOnDestroy());

        MgrBoosterSystem.Instance.ClosedRandomBoosterCard();
    }

    private string GetCoolDown(float _coolDown, bool _isUpgrade)
    {
        float resultCooldown = _coolDown;

        if (MgrBoosterSystem.Instance.DicEtc.TryGetValue("skill_passive_001", out int _level))
            resultCooldown *= 1.0f - (float)DataManager.Instance.GetBoosterSkillData($"skill_passive_001_{_level - 1}").Params[0];

        if(_isUpgrade)
        {
            Color color;
            ColorUtility.TryParseHtmlString("#009534", out color);
            imgCooldown.color = color;
        }
        else
        {
            imgCooldown.color = new Color(0.3294118f, 0.3294118f, 0.3294118f, 1.0f);
        }

        return $"{(_isUpgrade ? "<#009534>" : string.Empty)}{resultCooldown:F0}{(_isUpgrade ? "</color>" : string.Empty)}";
    }
}
