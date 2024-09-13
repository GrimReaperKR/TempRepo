using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOMove_FrontestToAlly", menuName = "UnitMove/FrontestToAlly")]
public class SOMove_FrontestToAlly : SOBase_MoveUnit
{
    private MgrBattleSystem mgrBattleSystem;

    public override bool CheckIsChangeToMoveState(UnitBase _unitBase)
    {
        return _unitBase.CheckIsAllyInXDistance();
    }

    public override void Move(UnitBase _unitBase)
    {
        if (!mgrBattleSystem)
            mgrBattleSystem = MgrBattleSystem.Instance;

        // 타원 범위 내 가장 가까운 적이 존재하는 경우 해당 적으로 변경
        if (!_unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
        {
            UnitBase unitInEllipse = mgrBattleSystem.GetNearestEnemyUnitInEllipse(_unitBase, _ratio:0.25f);
            if (unitInEllipse is not null)
            {
                _unitBase.EnemyTarget = unitInEllipse;
                _unitBase.SetUnitState(UNIT_STATE.IDLE);
                return;
            }
        }

        // X축이 가장 먼 아군 반환
        UnitBase unitBaseResult = mgrBattleSystem.GetFarestXAllyUnit(_unitBase);
        if (unitBaseResult == _unitBase)
            unitBaseResult = mgrBattleSystem.GetNearestXEnemyUnit(_unitBase);

        if (!_unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT) && _unitBase.EnemyTarget != unitBaseResult)
            _unitBase.EnemyTarget = unitBaseResult;

        if (!(_unitBase.EnemyTarget is null) && _unitBase != _unitBase.EnemyTarget)
        {
            float xDist = _unitBase.transform.position.x - _unitBase.EnemyTarget.transform.position.x;
            xDist = xDist < 0.0f ? -xDist : xDist;
            if (xDist <= _unitBase.UnitStat.Range)
            {
                _unitBase.SetUnitState(UNIT_STATE.IDLE);
                return;
            }
        }

        float moveSpd = _unitBase.UnitStat.MoveSpeed * (1.0f + _unitBase.GetUnitEffectTotalValue(UNIT_EFFECT.BUFF_MOVE_SPEED)) * (1.0f - _unitBase.GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_SLOW)) * (1.0f - _unitBase.GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_FROSTBITE)) * (1.0f - _unitBase.GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_BLACK_FIRE));

        if (_unitBase.EnemyTarget != null && _unitBase != _unitBase.EnemyTarget)
        {
            _unitBase.transform.rotation = Quaternion.Euler(0.0f, _unitBase.EnemyTarget.transform.position.x < _unitBase.transform.position.x ? -180.0f : 0.0f, 0.0f);
            _unitBase.SetRotationHpBar();
            //_unitBase.transform.localScale = new Vector3(_unitBase.EnemyTarget.transform.position.x < _unitBase.transform.position.x ? -1.0f : 1.0f, 1.0f, 1.0f);
            _unitBase.transform.position += moveSpd * Time.deltaTime * (_unitBase.EnemyTarget.transform.position.x - _unitBase.transform.position.x > 0.0f ? Vector3.right : Vector3.left);
        }
        else
        {
            if (!mgrBattleSystem.IsTestMode)
                _unitBase.transform.position += moveSpd * Time.deltaTime * (_unitBase.TeamNum == 0 ? Vector3.right : Vector3.left);
        }
    }
}
