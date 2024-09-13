using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_B_Tank_03_Taro_S2", menuName = "UnitSkillEvent/B_Tank_03_Taro_S2")]
public class B_Tank_03_Taro_S2 : SOBase_UnitSkillEvent
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
            unitBase.SetHeal(unitBase.UnitStat.MaxHP * (float)unitSkillData.Param[0]);
            MgrObjectPool.Instance.ShowObj("FX_PC-Heal", unitBase.GetUnitCenterPos());

            unitBase.AddUnitEffect(UNIT_EFFECT.ETC_DODGE, unitBase, unitBase, new float[] { (float)unitSkillData.Param[2], (float)unitSkillData.Param[1] });

            TaskSkillVFX().Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskSkillVFX()
        {
            await UniTask.Delay(100, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", unitBase.transform.position).transform.SetParent(unitBase.transform);
            MgrObjectPool.Instance.ShowObj("FX_B_Tank_03_Taro_skill2_Smoke", unitBase.transform.position).transform.SetParent(unitBase.transform);
            MgrSound.Instance.PlayOneShotSFX("SFX_Buff_ab", 1.0f);
        }
    }
}
