using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitOutCameraIcon : MonoBehaviour
{
    [SerializeField] private Image imgIcon;
    [SerializeField] private TextMeshProUGUI tmpCount;

    public void SetIcon(Sprite _sprIcon, int _cnt)
    {
        imgIcon.sprite = _sprIcon;
        tmpCount.text = $"x{_cnt}";
    }
}
