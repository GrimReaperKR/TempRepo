using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class TestUnitSpawn : MonoBehaviour
{
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            TaskStun().Forget();

            //MgrBattleSystem.Instance.ListUnitBase[1].AddUnitEffect(UNIT_EFFECT.BUFF_ATK, MgrBattleSystem.Instance.ListUnitBase[1], MgrBattleSystem.Instance.ListUnitBase[1], new float[] { 0.5f, 2.0f });
        }
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            MgrBattleSystem.Instance.ListUnitBase[1].UnitStat.HP = MgrBattleSystem.Instance.ListUnitBase[1].UnitStat.MaxHP * 0.5f;
        }
        if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            for (int i = 0; i < MgrBattleSystem.Instance.ListUnitBase.Count; i++)
                MgrBattleSystem.Instance.ListUnitBase[i].SetUnitState(UNIT_STATE.DEATH);

            //MgrBattleSystem.Instance.ChangeBackGround();
            //MgrBattleSystem.Instance.ListUnitBase[1].UnitStat.HP = MgrBattleSystem.Instance.ListUnitBase[1].UnitStat.MaxHP * 0.5f;
        }
        if(Input.GetKeyDown(KeyCode.Alpha4))
        {
            MgrUnitPool.Instance.ShowObj("Catbot_001", 0, new Vector3(-12.0f, 0.0f, 0.0f));

            //MgrUnitPool.Instance.ShowEnemyMonsterObj("C3_Final_Boss02_b", 1, Vector3.zero + Vector3.down * 2.0f);
            //MgrUnitPool.Instance.ShowEnemyUnitObj("C1_Elite_Mnstr01$B_Tank_01", 1, Vector3.zero + Vector3.down * 2.0f);
        }
        if(Input.GetKeyDown(KeyCode.F1))
        {
            MgrBoosterSystem.Instance.ShowBoosterUpgrade();
        }
        if(Input.GetKeyDown(KeyCode.F2))
        {
            MgrBoosterSystem.Instance.AddRandomBoosterLv(Vector3.zero, 3);
            //MgrBoosterSystem.Instance.AddBoosterLv(1);
            //MgrBakingSystem.Instance.AddBakedFish(1);
        }
        if(Input.GetKeyDown(KeyCode.F3))
        {
            MgrBattleSystem.Instance.ResetSideSkillCoolDown();
        }
    }

    private async UniTaskVoid TaskStun()
    {
        UnitBase unit;
        for(int i = 0; i < MgrBattleSystem.Instance.ListUnitBase.Count; i++)
        {
            unit = MgrBattleSystem.Instance.ListUnitBase[i];
            unit.AddUnitEffect(UNIT_EFFECT.CC_FREEZE, unit, unit, new float[] { 3.0f });
            await UniTask.Delay(300);
        }
    }
}
