using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_B_Spt_02_Snowman_S2", menuName = "UnitSkillEvent/B_Spt_02_Snowman_S2")]
public class B_Spt_02_Snowman_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private int triggerCnt = 0;

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
            triggerCnt++;

            if(triggerCnt == 1)
                TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            triggerCnt = 0;

            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskSkill()
        {
            await UniTask.Delay(200, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            UnitBase unit = MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, _range: skillRange);
            if (unit)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_B_Spt_02_s2_2", 1.0f);
                MgrSound.Instance.PlayOneShotSFX("SFX_B_Spt_02_s2_3", 1.0f);

                MgrObjectPool.Instance.ShowObj("FX_B_Spt_02_Snowman_skill2_change hit", unit.GetUnitCenterPos());
                unit.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitSkillData.Param[0]), unitBase, unit, new float[] { (float)unitSkillData.Param[1] });
            }
        }
    }
}
