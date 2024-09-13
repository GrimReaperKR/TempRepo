using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_A_Spt_02_UFO_S2", menuName = "UnitSkillEvent/A_Spt_02_UFO_S2")]
public class A_Spt_02_UFO_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private Transform tfVFX = null;
        private UnitBase targetUnit = null;

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL) && MgrBattleSystem.Instance.CheckIsEnemyUnitInEllipse(unitBase, unitBase.transform.position, skillRange))
                return true;

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
            GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_A_Spt_02_S2", unitBase.transform.position + Vector3.up * 0.5f);
            objBullet.GetComponent<Bullet>().SetBullet(unitBase, targetUnit);
        }

        public override void OnSkill()
        {
            if (tfVFX is null)
                tfVFX = unitBase.transform.Find("FX_A_Spt_02_UFO_skill2_cast(Clone)");

            targetUnit = MgrBattleSystem.Instance.GetHighestAtkEnemyUnit(unitBase, skillRange);
            if (targetUnit is null)
                targetUnit = unitBase.EnemyTarget;

            Vector3 v3Dir = (targetUnit.GetUnitCenterPos() - tfVFX.transform.position).normalized;
            float angle = Mathf.Atan2(v3Dir.y, v3Dir.x) * Mathf.Rad2Deg;
            tfVFX.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }
    }
}
