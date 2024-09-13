using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOMove_Ally", menuName = "UnitMove/Ally")]
public class SOMove_Ally : SOBase_MoveUnit
{
    private MgrBattleSystem mgrBattleSystem;

    public override bool CheckIsChangeToMoveState(UnitBase _unitBase)
    {
        if (_unitBase.EnemyTarget is null)
            return false;

        float xDistance = (_unitBase.EnemyTarget.transform.position.x - _unitBase.transform.position.x);
        if (xDistance < 0.0f)
            xDistance = -xDistance;
        
        return xDistance <= _unitBase.UnitStat.Range;
    }

    public override void Move(UnitBase _unitBase)
    {
        if (!mgrBattleSystem)
            mgrBattleSystem = MgrBattleSystem.Instance;

        if (_unitBase.EnemyTarget)
        {
            if (mgrBattleSystem.GameMode == GAME_MODE.Pvp && !mgrBattleSystem.CheckIsPvpAllyBaseDistance())
            {
                _unitBase.SetUnitState(UNIT_STATE.IDLE);
                return;
            }

            if (((_unitBase.EnemyTarget.transform.position.x - _unitBase.transform.position.x) < 0.0f
                    ? -(_unitBase.EnemyTarget.transform.position.x - _unitBase.transform.position.x)
                    : (_unitBase.EnemyTarget.transform.position.x - _unitBase.transform.position.x)) <=
                _unitBase.UnitStat.Range && !_unitBase.EnemyTarget.CheckIsState(UNIT_STATE.DEATH))
            {
                _unitBase.SetUnitState(UNIT_STATE.IDLE);
                return;
            }
        }

        UnitBase unitBaseResult = mgrBattleSystem.GetNearestXEnemyUnit(_unitBase);

        if (_unitBase.EnemyTarget != unitBaseResult)
            _unitBase.EnemyTarget = unitBaseResult;

        _unitBase.transform.rotation = Quaternion.Euler(0.0f, (_unitBase.TeamNum == 0 ? 0.0f : -180.0f), 0.0f);
        _unitBase.SetRotationHpBar();

        _unitBase.transform.position += _unitBase.UnitStat.MoveSpeed * Time.deltaTime * (_unitBase.TeamNum == 0 ? Vector3.right :Vector3.left);
    }
}
