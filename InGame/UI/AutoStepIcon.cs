using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class AutoStepIcon : MonoBehaviour
{
    [SerializeField] private Image imgMask;
    [SerializeField] private TextMeshProUGUI tmpProgress;
    [SerializeField] private RectTransform rtCheck;
    [SerializeField] private GameObject objSlotEmpty;

    public void SetProgress(float _value)
    {
        imgMask.fillAmount = 1.0f - _value;
        tmpProgress.text = $"{_value * 100.0f:F0}%";
    }

    public void SetComplete()
    {
        imgMask.fillAmount = 0.0f;
        tmpProgress.text = string.Empty;
        rtCheck.gameObject.SetActive(true);
        rtCheck.localScale = Vector3.one * 2.0f;
        rtCheck.DOScale(1.0f, 0.25f).SetEase(Ease.OutCubic);
    }

    public void SetEmpty() => objSlotEmpty.SetActive(true);

    public void InitStepIcon()
    {
        imgMask.fillAmount = 1.0f;
        tmpProgress.text = string.Empty;
        rtCheck.gameObject.SetActive(false);
        objSlotEmpty.SetActive(false);
    }
}
