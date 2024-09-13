using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_S_Spt_02_Mico_S2", menuName = "UnitSkillEvent/S_Spt_02_Mico_S2")]
public class S_Spt_02_Mico_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        public override bool CheckCanUseSkill() => false;

        public override void EventTriggerEnd(string _animationName)
        {

        }

        public override void EventTriggerSkill()
        {

        }

        public override void OnSkill()
        {

        }
    }
}
