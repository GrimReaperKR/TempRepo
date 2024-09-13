using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class State_Idle : IState
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
        unitBase.SetUnitAnimation(unitBase.animIdleName, true);
        unitBase.UpdateSpineUnitSpineSpeed();
        unitBase.ResetUnitSkill();
    }

    public void OnExit()
    {

    }

    public void OnUpdate()
    {
        if (!mgrBattleSys.IsTestMode && !mgrBattleSys.isStageStart)
            return;

        if (unitBase.EnemyTarget)
        {
            if(unitBase.EnemyTarget.CheckIsState(UNIT_STATE.DEATH) || (unitBase.EnemyTarget.IsBlockedTarget && !unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT)))
                unitBase.EnemyTarget = null;
        }

        for(int i = 0; i < unitBase.ListSkillPriority.Count; i++)
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
            return;

        // 이동 속도가 0이면 이동 스태이트로 변경되지 않도록 수정
        if (unitBase.UnitStat.MoveSpeed == 0.0f || unitBase.moveLimitTime > 0.0f)
            return;

        // PVP 기지 이동 추가 판단
        if (mgrBattleSys.GameMode == GAME_MODE.Pvp && unitBase.UnitSetting.unitType == UnitType.AllyBase)
        {
            if (!mgrBattleSys.CheckIsPvpAllyBaseDistance())
                return;
        }

        // 각 이동 스크립터블에서 이동 가능 여부 판단
        if(unitBase.EnemyTarget && !unitBase.EnemyTarget.CheckIsState(UNIT_STATE.DEATH))
        {
            if (unitBase.UnitSetting.moveSO.CheckIsChangeToMoveState(unitBase))
                return;
        }
        
        // 스테이지 시작하지 않았을 때 이동 스테이트로 넘어가지 않도록
        if (!mgrBattleSys.IsTestMode && !mgrBattleSys.isStageStart)
            return;

        unitBase.SetUnitState(UNIT_STATE.MOVE);
    }
}
