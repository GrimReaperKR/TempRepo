using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C_Tank_02_Ely_S2", menuName = "UnitSkillEvent/C_Tank_02_Ely_S2")]
public class C_Tank_02_Ely_S2 : SOBase_UnitSkillEvent
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
            unitBase.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitSkillData.Param[0]), unitBase, unitBase, new float[] { (float)unitSkillData.Param[1], (float)unitSkillData.Param[2] });
            MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", unitBase.transform.position).transform.SetParent(unitBase.transform);
            MgrSound.Instance.PlayOneShotSFX("SFX_C_Tank_02_s2_2", 1.0f);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }
    }
}
