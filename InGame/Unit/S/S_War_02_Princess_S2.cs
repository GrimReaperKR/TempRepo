using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_S_War_02_Princess_S2", menuName = "UnitSkillEvent/S_War_02_Princess_S2")]
public class S_War_02_Princess_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetFinalDamageAttackerEvent(personal.OnFinalDamageAttackerAction);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private int attackCnt = 0;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 2)
                return;

            _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitSkillData.Param[3]), _attacker, _victim, new float[] { 2.0f });
        }
        
        public float OnFinalDamageAttackerAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || (_dmgChannel != 1 && _dmgChannel != 2))
                return _damage;

            if (_victim.CheckHasCCUnitEffect())
                return _damage * (1.0f + (float)unitSkillData.Param[4]);

            return _damage; 
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            if (unitBase.EnemyTarget && MgrBattleSystem.Instance.CheckIsEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, 2.0f)) return true;
            else
            {
                if (unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    return false;

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, 2.0f));
                UnitBase unitInEllipse = listUnit.Count > 0 ? listUnit[0] : null;
                if (unitInEllipse is not null)
                {
                    unitBase.EnemyTarget = unitInEllipse;
                    return true;
                }
            }

            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill"))
                return;

            TaskSkillEnd().Forget();
        }

        public override void EventTriggerSkill()
        {
            attackCnt++;

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, 2.0f, _isContainBlockedTarget: true));

            foreach(UnitBase unit in listUnit)
            {
                if(attackCnt == (int)unitSkillData.Param[1] + 1) MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[2]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 2);
                else MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);

                MgrObjectPool.Instance.ShowObj("FX_S_War_02_Princeses_skill2_hit", unit.GetUnitCenterPos());
            }
        }

        public override void OnSkill()
        {
            attackCnt = 0;
            unitBase.SetUnitAnimation(unitBase.UnitLvData.promotion >= 5 ? "skill2_lv5" : "skill2");
            unitBase.PlayTimeline(unitBase.UnitLvData.promotion >= 5 ? 1 : 0);
        }

        private async UniTaskVoid TaskSkillEnd()
        {
            await UniTask.Delay(250, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            unitBase.SetUnitState(UNIT_STATE.IDLE);
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);
        }
    }
}
