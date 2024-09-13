using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class DamageText : MonoBehaviour
{
    private TextMeshPro tmpDmg;
    private Sequence seqText;

    private void Awake()
    {
        tmpDmg = GetComponent<TextMeshPro>();
    }

    public void SetDamageText(float _dmg, bool _isCritical = false, bool _isBlockedShield = false, bool _isGod = false, bool _isHeal = false, bool _isUnitEffectResistance = false, bool _isDodge = false,
                                bool _immunity = false, bool _isUnitEffect = false, int _unitEffectIndex = -1, string _customText = null)
    {
        //transform.position = new Vector3(transform.position.x, transform.position.y, 0.0f);
        tmpDmg.color = _isCritical ? new Color(1.0f, 0.25f, 0.25f, 1.0f) : Color.white;

        if (_isBlockedShield) tmpDmg.color = Color.cyan;
        if (_isGod) tmpDmg.color = Color.magenta;
        if (_isHeal) tmpDmg.color = Color.green;
        if (_isUnitEffectResistance) tmpDmg.color = Color.yellow;

        int damage = Mathf.RoundToInt(_dmg);
        tmpDmg.text = $"{(damage >= 0 ? damage : 0)}";
        if (_isBlockedShield) tmpDmg.text = $"{MgrInGameData.Instance.DicLocalizationCSVData["Effect_buff_9100"]["korea"]}";
        if (_isGod) tmpDmg.text = $"{MgrInGameData.Instance.DicLocalizationCSVData["Effect_Etc_9400"]["korea"]}";
        if (_isHeal) tmpDmg.text = $"{(damage >= 0 ? damage : 0)}";
        if (_isUnitEffectResistance)
        {
            string effectName;
            if (9100 <= _unitEffectIndex && _unitEffectIndex < 9200) effectName = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Effect_buff_{_unitEffectIndex}"]["korea"]}";
            else if (9200 <= _unitEffectIndex && _unitEffectIndex < 9300) effectName = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Effect_Debuff_{_unitEffectIndex}"]["korea"]}";
            else if (9300 <= _unitEffectIndex && _unitEffectIndex < 9400) effectName = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Effect_CC_{_unitEffectIndex}"]["korea"]}";
            else effectName = string.Empty;
            tmpDmg.text = $"{effectName} {MgrInGameData.Instance.DicLocalizationCSVData["Effect_Etc_9404"]["korea"]}";
        }
        if (_isDodge) tmpDmg.text = $"{MgrInGameData.Instance.DicLocalizationCSVData["Effect_Etc_9405"]["korea"]}";
        if (_immunity)
        {
            string effectName;
            if (9100 <= _unitEffectIndex && _unitEffectIndex < 9200) effectName = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Effect_buff_{_unitEffectIndex}"]["korea"]}";
            else if (9200 <= _unitEffectIndex && _unitEffectIndex < 9300) effectName = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Effect_Debuff_{_unitEffectIndex}"]["korea"]}";
            else if (9300 <= _unitEffectIndex && _unitEffectIndex < 9400) effectName = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Effect_CC_{_unitEffectIndex}"]["korea"]}";
            else effectName = string.Empty;
            tmpDmg.text = $"{effectName} {MgrInGameData.Instance.DicLocalizationCSVData["Effect_Etc_9403"]["korea"]}";
        }
        if(_isUnitEffect)
        {
            string effectName;
            if (9100 <= _unitEffectIndex && _unitEffectIndex < 9200) effectName = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Effect_buff_{_unitEffectIndex}"]["korea"]}";
            else if (9200 <= _unitEffectIndex && _unitEffectIndex < 9300) effectName = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Effect_Debuff_{_unitEffectIndex}"]["korea"]}";
            else if (9300 <= _unitEffectIndex && _unitEffectIndex < 9400) effectName = $"{MgrInGameData.Instance.DicLocalizationCSVData[$"Effect_CC_{_unitEffectIndex}"]["korea"]}";
            else effectName = string.Empty;
            tmpDmg.text = $"{effectName}";

            transform.position += Vector3.up * (Random.Range(-1.0f, 1.0f));
        }
        if(_customText is not null)
        {
            tmpDmg.text = _customText;
        }

        seqText = DOTween.Sequence();
        seqText.Append(tmpDmg.transform.DOLocalMoveY(tmpDmg.transform.position.y + 2.0f, 1.0f));
        seqText.Join(tmpDmg.DOFade(0.0f, 0.5f).SetDelay(0.5f));
        seqText.OnComplete(() => MgrObjectPool.Instance.HideObj(gameObject.name, gameObject));
    }
}
