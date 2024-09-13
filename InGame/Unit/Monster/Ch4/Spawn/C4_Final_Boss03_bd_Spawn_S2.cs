using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Final_Boss03_bd_Spawn_S2", menuName = "UnitSkillEvent/Monster/C4_Final_Boss03_bd_Spawn_S2")]
public class C4_Final_Boss03_bd_Spawn_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].skillRange = 2.4f;
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].skillCooldown = 9.0f;
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Huge_Doll_s2_2", 1.0f);
            _victim.AddUnitEffect(UNIT_EFFECT.CC_FEAR, _attacker, _victim, new float[] { unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(0, "param.4") });
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            if (unitBase.EnemyTarget && MathLib.CheckIsInEllipse(unitBase.transform.position, skillRange * (unitBase.transform.localScale.x > 1.0f ? 1.7f : 1.0f), unitBase.EnemyTarget.transform.position)) return true;
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
            float cooldown = skillCooldown;
            if (unitBase.transform.localScale.x > 1.0f)
                cooldown -= skillCooldown * unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(2, "param.4");
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), cooldown);
        }

        public override void EventTriggerSkill()
        {
            float xRange = 3.5f;
            if (unitBase.transform.localScale.x > 1.0f)
                xRange *= 1.7f;

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, xRange, 10.0f, _isContainBlockedTarget: true));

            foreach(UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage(unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(0, "param.3")), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);

            MgrObjectPool.Instance.ShowObj("FX_Ghost_Necromancer_Boss_Bear_b_skill2_zone", unitBase.transform.position);
            MgrObjectPool.Instance.ShowObj("FX_splash_demon", unitBase.GetUnitCenterPos());
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }
    }
}
