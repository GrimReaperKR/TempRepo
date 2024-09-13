using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;
using BCH.Database;

public class SideSkill : MonoBehaviour
{
    [SerializeField] private string skillIndex;
    [SerializeField] private Image imgCooldown;
    [SerializeField] private TextMeshProUGUI tmpCooldown;
    [SerializeField] private Image imgIcon;

    private float activeDuration;
    private float maxActiveDuration;

    private float coolDown;
    private float maxCoolDown;

    [SerializeField] private GameObject objDescription;
    [SerializeField] private TextMeshProUGUI tmpDescription;

    [SerializeField] private GameObject objActivateSkill;
    [SerializeField] private Image imgActiveDuration;

    [SerializeField] private GameObject objActivatedVFX;
    [SerializeField] private Material matShine;

    [SerializeField] private ParticleSystem parsysReduceCoolDown;

    private CancellationTokenSource token_Cooldown = null;

    private void Awake()
    {
        if(skillIndex.Equals("Catbot_skill_001"))
        {
            imgIcon.material = matShine;
            MgrInGameEvent.Instance.AddBoosterEvent(BoosterUpgradeEvent);
        }
    }

    public void SetSideSkill(string _index, Sprite _sprIcon)
    {
        skillIndex = _index;
        imgIcon.sprite = _sprIcon;
        imgCooldown.sprite = _sprIcon;

        imgIcon.material = matShine;

        MgrInGameEvent.Instance.AddBoosterEvent(BoosterUpgradeEvent);
    }

    public void SetCoolDown(float _value)
    {
        maxCoolDown = _value;
        coolDown = _value;

        if(coolDown > 0.0f)
        {
            if(activeDuration <= 0.0f)
            {
                imgCooldown.gameObject.SetActive(true);
                tmpCooldown.text = $"{coolDown:F0}";
            }

            if (token_Cooldown is null)
                TaskCoolDown().Forget();
        }
        else if(coolDown == 0.0f)
        {
            token_Cooldown?.Cancel();

            imgIcon.material = matShine;
            imgCooldown.gameObject.SetActive(false);
        }
    }

    public float GetMaxCooldown() => maxCoolDown;

    private int limitCnt = 0;
    public void AddCoolDown(float _value, bool _isLimitCnt = false)
    {
        if (coolDown <= 0.0f)
            return;

        if (_isLimitCnt && limitCnt >= 50)
            return;

        coolDown += _value;
        if (_isLimitCnt)
            limitCnt++;

        if (_value < 0.0f)
        {
            parsysReduceCoolDown.Play();
            MgrSound.Instance.PlayOneShotSFX("SFX_Active_Skill_Cooldown", 0.75f);
        }
    }

    public void OnBtn_Skill()
    {
        if (coolDown > 0.0f || MgrBattleSystem.Instance.GetAllyBase().CheckIsState(UNIT_STATE.DEATH))
            return;

        if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter && (MgrBattleSystem.Instance.TutorialStep < 8 || (MgrBattleSystem.Instance.TutorialStep == 8 && MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf) || (MgrBattleSystem.Instance.TutorialStep == 16 && !skillIndex.Equals("Catbot_skill_001")) || MgrBattleSystem.Instance.TutorialStep == 17))
            return;

        if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter && MgrBattleSystem.Instance.TutorialStep == 16)
        {
            MgrBattleSystem.Instance.TutorialStep = 17;
            MgrBattleSystem.Instance.ToggleTutorialUI(false);
            MgrBattleSystem.Instance.SetTutorialTimeScale(false);
        }

        MgrInGameEvent.Instance.BroadcastActiveSkillEvent(skillIndex);

        coolDown = GetCoolDown();

        float multiplyRatio = 1.0f;

        if (MgrBoosterSystem.Instance.DicEtc.TryGetValue("skill_passive_001", out int _level))
            multiplyRatio -= (float)DataManager.Instance.GetBoosterSkillData($"skill_passive_001_{_level - 1}").Params[0];

        UserGear gearCore = DataManager.Instance.GetUsingGearInfo(1);
        if (gearCore is not null && gearCore.gearId.Equals("gear_core_0001"))
        {
            if (gearCore.gearRarity >= 1) multiplyRatio += MgrBattleSystem.Instance.GlobalOption.GearCore_0001_AllyUnitDeathCnt * -0.001f;
            if (gearCore.gearRarity >= 3) multiplyRatio += (float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 2);
        }

        multiplyRatio += MgrBattleSystem.Instance.GlobalOption.Option_AllyBaseSkillCoolDown;

        if (MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode)
            multiplyRatio -= 0.7f;

        if(multiplyRatio < 0.0f) multiplyRatio = 0.0f;

        coolDown *= multiplyRatio;

        limitCnt = 0;

        activeDuration = GetActiveDuration();
        objActivateSkill.gameObject.SetActive(true);

        if (!(gearCore is null) && gearCore.gearId.Equals("gear_core_0001") && gearCore.gearRarity >= 10)
            MgrBattleSystem.Instance.ReduceSideSkillCoolDown((float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 4), this);

        TaskCoolDown().Forget();
    }

    public void OnStay_SkillDescription()
    {
        if (MgrBoosterSystem.Instance.DicSkill.TryGetValue(skillIndex, out int _level)) tmpDescription.text = MgrInGameData.Instance.DicLocalizationCSVData[MgrInGameData.Instance.GetBoosterData(skillIndex).SkillBtnDesc[_level - 1]]["korea"];
        else tmpDescription.text = MgrInGameData.Instance.DicLocalizationCSVData["Base_Skill_Desc_000"]["korea"];
    }

    private async UniTaskVoid TaskCoolDown()
    {
        maxCoolDown = coolDown;
        maxActiveDuration = activeDuration;

        token_Cooldown?.Cancel();
        token_Cooldown?.Dispose();
        token_Cooldown = new CancellationTokenSource();

        imgCooldown.fillAmount = coolDown / maxCoolDown;

        transform.localScale = Vector3.one * 1.1f;

        while (activeDuration > 0.0f)
        {
            await UniTask.Yield(token_Cooldown.Token);
            activeDuration -= Time.deltaTime;
            imgActiveDuration.fillAmount = activeDuration / maxActiveDuration;
        }

        imgIcon.material = null;

        transform.localScale = Vector3.one;

        objActivateSkill.SetActive(false);

        imgCooldown.gameObject.SetActive(true);
        tmpCooldown.text = $"{coolDown:F0}";

        token_Cooldown?.Cancel();
        token_Cooldown?.Dispose();
        token_Cooldown = new CancellationTokenSource();

        while (coolDown > 0.0f)
        {
            await UniTask.Yield(token_Cooldown.Token);
            coolDown -= Time.deltaTime;
            tmpCooldown.text = $"{coolDown:F0}";
            imgCooldown.fillAmount = coolDown / maxCoolDown;
        }

        imgIcon.material = matShine;
        objActivatedVFX.SetActive(true);

        imgCooldown.gameObject.SetActive(false);
        token_Cooldown.Dispose();
        token_Cooldown = null;
    }

    private float GetCoolDown()
    {
        BoosterSkill boosterSkillData = skillIndex.Equals("Catbot_skill_001") ? null : DataManager.Instance.GetBoosterSkillData($"{skillIndex}_{MgrBoosterSystem.Instance.DicSkill[skillIndex] - 1}");
        return boosterSkillData is null ? 90.0f : (float)boosterSkillData.Cooldown;
    }

    private void BoosterUpgradeEvent(string _index)
    {
        if (coolDown <= 0.0f || (!_index.Equals("skill_passive_001") && !skillIndex.Equals(_index)))
            return;

        if(_index.Equals("skill_passive_001"))
        {
            coolDown -= maxCoolDown * 0.05f;
            parsysReduceCoolDown.Play();
            MgrSound.Instance.PlayOneShotSFX("SFX_Active_Skill_Cooldown", 0.75f);
        }
        else
        {
            BoosterSkill boosterSkillData = skillIndex.Equals("Catbot_skill_001") ? null : DataManager.Instance.GetBoosterSkillData($"{skillIndex}_{MgrBoosterSystem.Instance.DicSkill[skillIndex] - 1}");
            if (boosterSkillData is null || MgrBoosterSystem.Instance.DicSkill[skillIndex] <= 1)
                return;

            BoosterSkill prevBoosterSkillData = DataManager.Instance.GetBoosterSkillData($"{skillIndex}_{MgrBoosterSystem.Instance.DicSkill[skillIndex] - 2}");
            if (prevBoosterSkillData.Cooldown != boosterSkillData.Cooldown)
            {
                coolDown -= (float)(prevBoosterSkillData.Cooldown - boosterSkillData.Cooldown);
                parsysReduceCoolDown.Play();
                MgrSound.Instance.PlayOneShotSFX("SFX_Active_Skill_Cooldown", 0.75f);
            }
        }
    }

    private float GetActiveDuration()
    {
        switch(skillIndex)
        {
            case "skill_active_001":
            case "skill_active_002":
            case "skill_active_003":
            case "skill_active_004":
            case "skill_active_005":
                return (float)DataManager.Instance.GetBoosterSkillData($"{skillIndex}_{MgrBoosterSystem.Instance.DicSkill[skillIndex] - 1}").Params[0];
            default:
                return 0.0f;
        }
    }

    private void OnDestroy()
    {
        token_Cooldown?.Cancel();
        token_Cooldown?.Dispose();
        token_Cooldown = null;
    }
}
