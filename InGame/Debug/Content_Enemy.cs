using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using BCH.Database;

public class Content_Enemy : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tmpDescription;
    private EnemyInfo info;

    public void SetContent(UnitBase _unitbase)
    {
        float chapterStat = (1.0f + (0.5f * (MgrBattleSystem.Instance.ChapterID - 1) < 0.0f ? 0.0f : 0.5f * (MgrBattleSystem.Instance.ChapterID - 1)));
        float waveStat = (1.0f + (0.05f * (MgrBattleSystem.Instance.currWave - 1)));

        if(_unitbase.UnitBaseParent == null)
        {
            info = MgrInGameData.Instance.GetEnemyDBData(_unitbase.UnitSetting.unitIndex);
            tmpDescription.text = $"{_unitbase.UnitSetting.unitIndex}\n챕터,웨이브 공/체 : {info.Power * chapterStat * waveStat:F1} / {info.Hp * chapterStat * waveStat:F1}\n현재 공/체 : {_unitbase.UnitStat.Atk:F1} / {_unitbase.UnitStat.MaxHP:F1}\n효과 반영 공격력 : {_unitbase.GetAtk()}";
        }
        else
        {
            tmpDescription.text = $"{_unitbase.UnitSetting.unitIndex}\n현재 공/체 : {_unitbase.UnitStat.Atk:F1} / {_unitbase.UnitStat.MaxHP:F1}\n효과 반영 공격력 : {_unitbase.GetAtk()}";
        }
    }
}
