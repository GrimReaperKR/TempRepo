using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Mid_Boss03_ac_Spawn_S1", menuName = "UnitSkillEvent/Monster/C4_Mid_Boss03_ac_Spawn_S1")]
public class C4_Mid_Boss03_ac_Spawn_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        _unitBase.UnitStat.Atk = _unitBase.UnitBaseParent.UnitStat.Atk * _unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(0, "param.3");
        _unitBase.UnitStat.MaxHP = _unitBase.UnitBaseParent.UnitStat.MaxHP * _unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(0, "param.4");
        _unitBase.UnitStat.HP = _unitBase.UnitStat.MaxHP;
        _unitBase.UnitStat.Range = 2.0f;
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].skillRange = 2.0f;
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].skillCooldown = 0.5f;

        if (MgrBattleSystem.Instance.IsBossAngry)
            MgrBattleSystem.Instance.SetUnitAngry(_unitBase);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 0)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Mini_Doll_AC_1", 1.0f);
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            if (unitBase.EnemyTarget && MathLib.CheckIsInEllipse(unitBase.transform.position, skillRange, unitBase.EnemyTarget.transform.position)) return true;
            else
            {
                if (unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    return false;

                UnitBase unitInEllipse = MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, skillRange);
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

            unitBase.SetUnitState(UNIT_STATE.IDLE);
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);
        }

        public override void EventTriggerSkill()
        {
            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unitBase.EnemyTarget, unitBase.GetAtkRateToDamage((float)unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(0, "param.5")), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);
            MgrObjectPool.Instance.ShowObj("FX_Ghost_Mini-doll_skill1_hit", unitBase.EnemyTarget.GetUnitCenterPos() + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0.0f));
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();
        }
    }
}
