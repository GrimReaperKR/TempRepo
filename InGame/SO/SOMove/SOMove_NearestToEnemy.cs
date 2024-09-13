using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "SOMove_NearestToEnemy", menuName = "UnitMove/NearestToEnemy")]
public class SOMove_NearestToEnemy : SOBase_MoveUnit
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

        // 타원 범위 내 가장 가까운 적이 존재하는 경우 해당 적으로 변경
        if(!_unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
        {
            UnitBase unitInEllipse = mgrBattleSystem.GetNearestEnemyUnitInEllipse(_unitBase, _ratio: 0.2f);
            if (unitInEllipse is not null)
            {
                _unitBase.EnemyTarget = unitInEllipse;
                _unitBase.SetUnitState(UNIT_STATE.IDLE);
                return;
            }
        }

        // X축이 가장 가까운 적 반환
        UnitBase unitBaseResult = mgrBattleSystem.GetNearestXEnemyUnit(_unitBase);

        if (!_unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT) && _unitBase.EnemyTarget != unitBaseResult)
            _unitBase.EnemyTarget = unitBaseResult;

        float moveSpd = _unitBase.UnitStat.MoveSpeed * (1.0f + _unitBase.GetUnitEffectTotalValue(UNIT_EFFECT.BUFF_MOVE_SPEED)) * (1.0f - _unitBase.GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_SLOW)) * (1.0f - _unitBase.GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_FROSTBITE)) * (1.0f - _unitBase.GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_BLACK_FIRE));

        if (_unitBase.EnemyTarget != null)
        {
            float xValue = 0.0f;
            if (_unitBase.EnemyTarget.transform.position.x == _unitBase.transform.position.x) xValue = _unitBase.TeamNum == 0 ? 0.0f : -180.0f;
            else if (_unitBase.EnemyTarget.transform.position.x < _unitBase.transform.position.x) xValue = -180.0f;

            _unitBase.transform.rotation = Quaternion.Euler(0.0f, xValue, 0.0f);
            _unitBase.SetRotationHpBar();

            // X 사거리 보다 멀 경우 X축만 이동, X 사거리 이내에 있는 경우 서서히 해당 유닛에게 다가가기
            float calculateX = _unitBase.EnemyTarget.transform.position.x - _unitBase.transform.position.x;
            calculateX = calculateX < 0.0f ? -calculateX : calculateX;

            float yMovedDistance = _unitBase.UnitSetting.unitClass == UnitClass.Arch ? _unitBase.UnitStat.Range : _unitBase.UnitStat.Range * 2.0f;
            if (calculateX > yMovedDistance)
            {
                float yPos = _unitBase.SpawnYPos - _unitBase.transform.position.y;
                Vector3 v3Dir = new Vector3(_unitBase.EnemyTarget.transform.position.x - _unitBase.transform.position.x > 0.0f ? 1.0f : -1.0f, (yPos > 0.1f ? 0.1f : yPos < 0.2f ? -0.2f : 0.0f), 0.0f);
                _unitBase.transform.position += moveSpd * Time.deltaTime * v3Dir;
            }
            else _unitBase.transform.position += moveSpd * Time.deltaTime * (_unitBase.EnemyTarget.transform.position - _unitBase.transform.position).normalized;
        }
        else
        {
            if(!mgrBattleSystem.IsTestMode)
                _unitBase.transform.position += moveSpd * Time.deltaTime * (_unitBase.TeamNum == 0 ? Vector3.right : Vector3.left);
        }
    }
}
