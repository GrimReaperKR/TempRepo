using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C_Spt_01_SunFlower_S1", menuName = "UnitSkillEvent/C_Spt_01_SunFlower_S1")]
public class C_Spt_01_SunFlower_S1 : SOBase_UnitSkillEvent
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
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL))
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
            UnitBase target = MgrBattleSystem.Instance.GetLowestHPUnit(unitBase, unitBase.transform.position, skillRange, true);
            if (!target)
                target = unitBase;

            float healAmount = unitBase.UnitStat.MaxHP * (float)unitSkillData.Param[0];
            if (target.UnitIndex.Equals("C_Spt_01"))
                healAmount *= 0.5f;
            TaskHeal(target, healAmount).Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskHeal(UnitBase _target, float _value)
        {
            await UniTask.Delay(350);

            MgrObjectPool.Instance.ShowObj("FX_PC-Heal", _target.GetUnitCenterPos());
            MgrObjectPool.Instance.ShowObj("FX_C_Spt_01_Sunflower_skill1_Drop", _target.GetUnitCenterPos());
            _target.SetHeal(_value);
        }
    }
}
