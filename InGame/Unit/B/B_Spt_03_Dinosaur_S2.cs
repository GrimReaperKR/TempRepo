using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_B_Spt_03_Dinosaur_S2", menuName = "UnitSkillEvent/B_Spt_03_Dinosaur_S2")]
public class B_Spt_03_Dinosaur_S2 : SOBase_UnitSkillEvent
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
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL) && MgrBattleSystem.Instance.CheckHasDebuffUnitInEllipse(unitBase, unitBase.transform.position, skillRange, true))
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
            listUnit.AddRange(MgrBattleSystem.Instance.GetHasDebuffUnitInEllipse(unitBase, unitBase.transform.position, skillRange, true));

            if(listUnit.Contains(unitBase))
            {
                listUnit.Remove(unitBase);
                listUnit.Add(unitBase);
            }

            int targetCnt = (int)unitSkillData.Param[0];

            foreach (UnitBase unit in listUnit)
            {
                if (targetCnt == 0)
                    break;

                targetCnt--;

                GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_B_Spt_03_S2", unitBase.GetUnitCenterPos());
                objBullet.GetComponent<Bullet>().SetBullet(unitBase, unit);
            }
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }
    }
}
