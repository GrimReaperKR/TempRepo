using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOMove_OnlyToCatBot", menuName = "UnitMove/OnlyToCatBot")]
public class SOMove_OnlyToCatBot : SOBase_MoveUnit
{
    private MgrBattleSystem mgrBattleSystem;

    public override bool CheckIsChangeToMoveState(UnitBase _unitBase)
    {
        return MathLib.CheckIsInEllipse(_unitBase.transform.position, _unitBase.UnitStat.Range, _unitBase.EnemyTarget.transform.position, 0.2f);
    }

    public override void Move(UnitBase _unitBase)
    {
        if (!mgrBattleSystem)
            mgrBattleSystem = MgrBattleSystem.Instance;

        UnitBase target = mgrBattleSystem.GetAllyBase();
        if (target is not null && _unitBase.EnemyTarget != target)
            _unitBase.EnemyTarget = target;

        float moveSpd = _unitBase.UnitStat.MoveSpeed * (1.0f + _unitBase.GetUnitEffectTotalValue(UNIT_EFFECT.BUFF_MOVE_SPEED)) * (1.0f - _unitBase.GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_SLOW)) * (1.0f - _unitBase.GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_FROSTBITE)) * (1.0f - _unitBase.GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_BLACK_FIRE));

        if (_unitBase.EnemyTarget != null)
        {
            float xValue = 0.0f;
            if (_unitBase.EnemyTarget.transform.position.x == _unitBase.transform.position.x) xValue = _unitBase.TeamNum == 0 ? 0.0f : -180.0f;
            else if (_unitBase.EnemyTarget.transform.position.x < _unitBase.transform.position.x) xValue = -180.0f;

            _unitBase.transform.rotation = Quaternion.Euler(0.0f, xValue, 0.0f);
            _unitBase.SetRotationHpBar();

            _unitBase.transform.position += moveSpd * Time.deltaTime * (_unitBase.EnemyTarget.transform.position - _unitBase.transform.position).normalized;
        }
    }
}
