using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrentBoosterSlot : MonoBehaviour
{
    private BoosterData.BoosterInfo currBoosterInfo = null;

    [SerializeField] private BoosterType type;
    [SerializeField] private Image imgIcon;
    [SerializeField] private GameObject objProgress;
    [SerializeField] private SlicedFilledImage imgProgress;
    [SerializeField] private GameObject objSelectUI;

    public void SetCurrentBoosterSlot(BoosterData.BoosterInfo _info)
    {
        currBoosterInfo = _info;
        objProgress.SetActive(true);
        imgIcon.sprite = _info.Icon;
        imgIcon.color = Color.white;
    }

    public void UpdateBoosterSlot(BoosterData.BoosterInfo _info)
    {
        if (currBoosterInfo is null)
            return;

        int currLevel;
        switch (currBoosterInfo.Type)
        {
            case BoosterType.Weapon:
                currLevel = MgrBoosterSystem.Instance.DicWeapon[currBoosterInfo.Index];
                break;
            case BoosterType.Active:
                currLevel = MgrBoosterSystem.Instance.DicSkill[currBoosterInfo.Index];
                break;
            default:
                currLevel = MgrBoosterSystem.Instance.DicEtc[currBoosterInfo.Index];
                break;
        }

        if(currBoosterInfo == _info)
        {
            imgIcon.transform.localScale = Vector3.one * 2.0f;
            imgIcon.transform.DOKill();
            imgIcon.transform.DOScale(1.0f, 0.125f).SetUpdate(true);
        }

        imgProgress.fillAmount = (float)currLevel / currBoosterInfo.MaxLevel;
    }

    public void OnBtn_ShowPauseBoosterSelectedCard()
    {
        if (currBoosterInfo is null || !MgrBattleSystem.Instance.IsPause || MgrBattleSystem.Instance.ObjCanvLeaveConfirm.activeSelf)
            return;

        SetSelectUI(true);
        MgrBoosterSystem.Instance.ShowPauseBoosterSelcted(currBoosterInfo, this);
    }

    public void SetSelectUI(bool _isActivated) => objSelectUI.SetActive(_isActivated);
}
