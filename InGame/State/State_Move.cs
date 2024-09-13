using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State_Move : IState
{
    private UnitBase unitBase;
    private MgrBattleSystem mgrBattleSys;

    public void InitializeState(UnitBase _unitBase)
    {
        unitBase = _unitBase;
        mgrBattleSys = MgrBattleSystem.Instance;
    }

    public void OnEnter()
    {
        unitBase.SetUnitAnimation(unitBase.animMoveName, true);
        unitBase.UpdateSpineUnitSpineSpeed();
        unitBase.SetLimitTime();
    }

    public void OnExit()
    {

    }

    public void OnUpdate()
    {
        if (!mgrBattleSys.IsTestMode && !mgrBattleSys.isStageStart)
        {
            unitBase.SetUnitState(UNIT_STATE.IDLE);
            return;
        }

        for (int i = 0; i < unitBase.ListSkillPriority.Count; i++)
        {
            if (!unitBase.CheckHasBlockedSkillCC() && unitBase.UnitSkillPersonalVariable[unitBase.GetSkillIndex(unitBase.ListSkillPriority[i].SOSkillEvent)].CheckCanUseSkill())
            {
                if (unitBase.EnemyTarget is not null)
                {
                    float xValue = unitBase.EnemyTarget.transform.position.x < unitBase.transform.position.x ? -180.0f : unitBase.EnemyTarget.transform.position.x > unitBase.transform.position.x ? 0.0f : unitBase.transform.rotation.y;
                    unitBase.transform.rotation = Quaternion.Euler(0.0f, xValue, 0.0f);
                }
                else unitBase.transform.rotation = Quaternion.Euler(0.0f, unitBase.TeamNum == 0 ? 0.0f : -180.0f, 0.0f);

                unitBase.SetRotationHpBar();
                unitBase.SetUnitUseSkill(unitBase.ListSkillPriority[i].SOSkillEvent);
                return;
            }
        }

        // 이동 방해 CC 인 경우 Idle 로 변경
        if (unitBase.CheckHasBlockedMoveCC())
        {
            unitBase.SetUnitState(UNIT_STATE.IDLE);
            return;
        }

        unitBase.UnitSetting.moveSO.Move(unitBase);
        unitBase.transform.position = new Vector3(unitBase.transform.position.x, unitBase.transform.position.y, unitBase.transform.position.y * 0.01f);
    }
}
