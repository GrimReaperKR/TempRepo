using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C_Arch_02_Shino_S1", menuName = "UnitSkillEvent/C_Arch_02_Shino_S1")]
public class C_Arch_02_Shino_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
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
            GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_C_Arch_02_S1", unitBase.GetUnitCenterPos());
            objBullet.GetComponent<Bullet>().SetBullet(unitBase, unitBase.EnemyTarget);

            MgrSound.Instance.PlayOneShotSFX("SFX_C_Arch_02_s1_2", 1.0f);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();
        }
    }
}
