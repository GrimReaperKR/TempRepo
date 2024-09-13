using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using BCH.Database;

public class Content_Unit : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tmpDescription;
    private UnitInfo info;

    public void SetContent(UnitBase _unitbase)
    {
        info = MgrInGameData.Instance.GetUnitDBData(_unitbase.UnitSetting.unitIndex);

        if(DataManager.Instance.UserInventory.unitInventory.TryGetValue(_unitbase.UnitSetting.unitIndex, out UserInventory.UserUnit unitLvData))
            tmpDescription.text = $"{_unitbase.UnitSetting.unitIndex}\n기본 공/체 : {info.AtkPower * (1.0f + 0.15f * (unitLvData.lv - 1)):F1} / {info.Hp * (1.0f + 0.15f * (unitLvData.lv - 1)):F1}\n현재 공/체 : {_unitbase.UnitStat.Atk:F1} / {_unitbase.UnitStat.MaxHP:F1}\n효과 반영 공격력 : {_unitbase.GetAtk()}";
    }
}
