using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_A_Spt_01_Necromancer_S1", menuName = "UnitSkillEvent/A_Spt_01_Necromancer_S1")]
public class A_Spt_01_Necromancer_S1 : SOBase_UnitSkillEvent
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
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, skillRange));

            listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());
            listUnit.Reverse();

            if (listUnit.Count == 0)
                return;

            GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_A_Spt_01_S1", unitBase.Ska.skeleton.FindBone("weapon_wing_R").GetWorldPosition(unitBase.Ska.transform) + Vector3.up * 0.5f);
            objBullet.GetComponent<Bullet>().SetBullet(unitBase, listUnit[0]);

            MgrSound.Instance.PlayOneShotSFX("SFX_A_Spt_01_s1_2", 1.0f);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();
        }
    }
}
