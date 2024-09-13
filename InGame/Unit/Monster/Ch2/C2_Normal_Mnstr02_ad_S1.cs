using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C2_Normal_Mnstr02_ad_S1", menuName = "UnitSkillEvent/Monster/C2_Normal_Mnstr02_ad_S1")]
public class C2_Normal_Mnstr02_ad_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        _unitBase.Ska.skeleton.SetSkin(_unitBase.UnitIndex.Equals("C2_Normal_Mnstr02_d") ? "d" : "a");

        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].skillCooldown = 1.0f;
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 0)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_Normal_Mnstr02_Metal_Sheild_2", 1.0f);
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            if (unitBase.EnemyTarget && MathLib.CheckIsInEllipse(unitBase.transform.position, unitBase.UnitStat.Range, unitBase.EnemyTarget.transform.position)) return true;
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
            MgrSound.Instance.PlayOneShotSFX("SFX_Normal_Mnstr02_Metal_Sheild_1", 1.0f);
            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Normal_Mnstr02_d") ? "FX_Desert_Tank_Golem_d_hit" : "FX_Desert_Tank_Golem_a_hit", unitBase.EnemyTarget.GetUnitCenterPos());
            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unitBase.EnemyTarget, unitBase.GetAtkRateToDamage(1.0f), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();
        }
    }
}
