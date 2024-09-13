using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_B_Arch_03_Ray_S2", menuName = "UnitSkillEvent/B_Arch_03_Ray_S2")]
public class B_Arch_03_Ray_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetDeathEvent(personal.OnEventDeath);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private Vector3 v3EndPos;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_B_Arch_03_s2_4", 1.0f);
        }

        public void OnEventDeath(UnitBase _attacker, UnitBase _victim, int _dmgChannel)
        {
            MgrSound.Instance.PlayOneShotSFX("SFX_B_Arch_03_death_1", 1.0f);
            MgrSound.Instance.PlayOneShotSFX("SFX_B_Arch_03_death_2", 1.0f);

            unitBase.OnDefaultDeath(_attacker, _victim, _dmgChannel);
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            if (unitBase.EnemyTarget && MathLib.CheckIsInEllipse(unitBase.transform.position, skillRange, unitBase.EnemyTarget.transform.position)) return true;
            else
            {
                if (unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    return false;

                UnitBase unitInEllipse = MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, skillRange);
                if (unitInEllipse is not null)
                {
                    unitBase.EnemyTarget = unitInEllipse;
                    return true;
                }
            }

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
            GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_B_Arch_03_S2", unitBase.GetUnitCenterPos());
            objBullet.GetComponent<Bullet>().SetBullet(unitBase, listUnit[0], _v3Pos:v3EndPos);
        }

        public override void OnSkill()
        {
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, skillRange + (float)unitSkillData.Param[0]));
            listUnit.Shuffle();

            if (listUnit.Count > 0)
                v3EndPos = listUnit[0].GetUnitCenterPos();

            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }
    }
}
