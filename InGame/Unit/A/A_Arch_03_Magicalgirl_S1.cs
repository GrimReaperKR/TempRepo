using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_A_Arch_03_Magicalgirl_S1", menuName = "UnitSkillEvent/A_Arch_03_Magicalgirl_S1")]
public class A_Arch_03_Magicalgirl_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

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
            GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_A_Arch_03_S1", unitBase.EnemyTarget.transform.position + new Vector3(unitBase.GetUnitLookDirection() == Vector3.right ? -5.0f : 5.0f, 20.0f, 0.0f));
            objBullet.GetComponent<Bullet>().SetBullet(unitBase, listUnit.Count > 0 ? listUnit[0] : unitBase.EnemyTarget);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, skillRange * 2.0f));
            listUnit.Shuffle();
        }
    }
}
