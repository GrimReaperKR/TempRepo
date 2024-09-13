using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_B_Tank_01_Minii_S1", menuName = "UnitSkillEvent/B_Tank_01_Minii_S1")]
public class B_Tank_01_Minii_S1 : SOBase_UnitSkillEvent
{
    public SkeletonDataAsset skda;

    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 0)
                return;

            B_Tank_01_Minii_S1 comp = unitBase.soSkillEvent[0] as B_Tank_01_Minii_S1;
            MgrSound.Instance.PlayOneShotSFX(comp.skda == unitBase.Ska.SkeletonDataAsset ? "SFX_B_Tank_01_s1_a_2" : "SFX_B_Tank_01_s1_b_3", 1.0f);
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

            B_Tank_01_Minii_S1 comp = unitBase.soSkillEvent[0] as B_Tank_01_Minii_S1;
            unitBase.SetUnitState(UNIT_STATE.IDLE);
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown * (1.0f - (comp.skda == unitBase.Ska.SkeletonDataAsset ? 0.0f : (float)unitSkillData.Param[3])));
        }

        public override void EventTriggerSkill()
        {
            B_Tank_01_Minii_S1 comp = unitBase.soSkillEvent[0] as B_Tank_01_Minii_S1;
            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unitBase.EnemyTarget, unitBase.GetAtkRateToDamage(comp.skda == unitBase.Ska.SkeletonDataAsset ? (float)unitSkillData.Param[0] : (float)unitSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);
        }

        public override void OnSkill()
        {
            B_Tank_01_Minii_S1 comp = unitBase.soSkillEvent[0] as B_Tank_01_Minii_S1;

            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline(comp.skda == unitBase.Ska.SkeletonDataAsset ? 0 : 1);
        }
    }
}
