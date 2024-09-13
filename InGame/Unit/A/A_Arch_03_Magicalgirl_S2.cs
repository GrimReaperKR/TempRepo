using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_A_Arch_03_Magicalgirl_S2", menuName = "UnitSkillEvent/A_Arch_03_Magicalgirl_S2")]
public class A_Arch_03_Magicalgirl_S2 : SOBase_UnitSkillEvent
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
            return false;
        }

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
