using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_B_Spt_02_Snowman_S1", menuName = "UnitSkillEvent/B_Spt_02_Snowman_S1")]
public class B_Spt_02_Snowman_S1 : SOBase_UnitSkillEvent
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
            TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskSkill()
        {
            await UniTask.Delay(150, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            UnitBase lowTarget = MgrBattleSystem.Instance.GetLowestHPUnit(unitBase, true);
            if (lowTarget is null)
                lowTarget = unitBase;

            GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_B_Spt_02_S1", unitBase.transform.position + unitBase.GetUnitLookDirection() * 1.5f + Vector3.up * 1.0f);
            objBullet.GetComponent<Bullet>().SetBullet(unitBase, lowTarget);
        }
    }
}
