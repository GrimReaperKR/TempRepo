using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_A_Spt_02_UFO_S1", menuName = "UnitSkillEvent/A_Spt_02_UFO_S1")]
public class A_Spt_02_UFO_S1 : SOBase_UnitSkillEvent
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
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL) && MgrBattleSystem.Instance.CheckIsEnemyUnitInEllipse(unitBase, unitBase.transform.position, skillRange, true))
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
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, skillRange, _isAlly: true));
            listUnit.Sort((a, b) => (a.UnitStat.HP / a.UnitStat.MaxHP).CompareTo(b.UnitStat.HP / b.UnitStat.MaxHP));

            listUnit.Remove(unitBase);
            listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());

            GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_A_Spt_02_S1", unitBase.transform.position + Vector3.up * 0.5f);
            objBullet.GetComponent<Bullet>().SetBullet(unitBase, listUnit.Count > 0 ? listUnit[0] : unitBase);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();
        }
    }
}
