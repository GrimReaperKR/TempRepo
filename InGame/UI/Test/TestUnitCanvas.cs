using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestUnitCanvas : MonoBehaviour
{
    [SerializeField] private GameObject objTestCanv;
    [SerializeField] private GameObject objSpawnOK;
    [SerializeField] private GameObject objSpawnEnemy;
    [SerializeField] private TMP_Dropdown drdTestUnit;
    [SerializeField] private TMP_Dropdown drdEnemyUnit;

    private List<string> listUnitIndex = new List<string>();

    private void Start()
    {
        objTestCanv.SetActive(MgrBattleSystem.Instance.IsTestMode);

        listUnitIndex.Clear();
        foreach (UnitData.UnitSetting unit in MgrInGameData.Instance.SOUnitData.unitSetting)
        {
            if (unit.unitType != UnitType.Unit || unit.unitIndex.Contains("_Spawn") || !unit.isActivate)
                continue;

            listUnitIndex.Add(unit.unitIndex);
        }

        drdTestUnit.ClearOptions();
        drdTestUnit.AddOptions(listUnitIndex);

        listUnitIndex.Clear();
        foreach (UnitData.UnitSetting unit in MgrInGameData.Instance.SOMonsterData.unitSetting)
        {
            if ((unit.unitType != UnitType.Monster && unit.unitType != UnitType.MidBoss && unit.unitType != UnitType.Boss) || unit.unitIndex.Contains("_Spawn") || !unit.isActivate)
                continue;

            listUnitIndex.Add(unit.unitIndex);
        }

        drdEnemyUnit.ClearOptions();
        drdEnemyUnit.AddOptions(listUnitIndex);
    }

    public void OnBtn_AllySpawn()
    {
        objSpawnOK.SetActive(!objSpawnOK.activeSelf);
    }
    
    public void OnBtn_EnemySpawnUI()
    {
        objSpawnEnemy.SetActive(!objSpawnEnemy.activeSelf);
    }

    public void OnBtn_OK()
    {
        MgrUnitPool.Instance.ShowObj(drdTestUnit.options[drdTestUnit.value].text, 0, new Vector3(-10.0f, -1.0f, 0.0f));
    }

    public void OnBtn_EnemySpawn()
    {
        MgrUnitPool.Instance.ShowEnemyMonsterObj(drdEnemyUnit.options[drdEnemyUnit.value].text, 1, Vector3.zero + Vector3.down);
    }
}
