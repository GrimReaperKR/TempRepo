using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Final_Boss03_bd_S3", menuName = "UnitSkillEvent/Monster/C4_Final_Boss03_bd_S3")]
public class C4_Final_Boss03_bd_S3 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        personal.InitPersonal();
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private C4_Final_Boss03_bd_S1.PersonalVariable personalS1;

        public void InitPersonal() => personalS1 = unitBase.UnitSkillPersonalVariable[0] as C4_Final_Boss03_bd_S1.PersonalVariable;

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL) || personalS1.unitDoll.CheckIsState(UNIT_STATE.DEATH))
                return false;

            return true;
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
            C4_Final_Boss03_bd_Spawn_S3.PersonalVariable dollPersonal = personalS1.unitDoll.UnitSkillPersonalVariable[2] as C4_Final_Boss03_bd_Spawn_S3.PersonalVariable;
            dollPersonal.OnBiggerSkill();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill3_b,d");
            unitBase.PlayTimeline(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? 1 : 0);
        }
    }
}
