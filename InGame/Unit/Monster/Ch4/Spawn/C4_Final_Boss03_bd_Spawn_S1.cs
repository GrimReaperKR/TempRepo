using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Final_Boss03_bd_Spawn_S1", menuName = "UnitSkillEvent/Monster/C4_Final_Boss03_bd_Spawn_S1")]
public class C4_Final_Boss03_bd_Spawn_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        _unitBase.UnitStat.Atk = _unitBase.UnitBaseParent.UnitStat.Atk;
        _unitBase.UnitStat.MaxHP = _unitBase.UnitBaseParent.UnitStat.MaxHP * _unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(0, "param.1");
        _unitBase.UnitStat.HP = _unitBase.UnitStat.MaxHP;
        _unitBase.UnitStat.CriRate = 0.15f;
        _unitBase.UnitStat.Range = 4.6f;
        _unitBase.UnitStat.MoveSpeed = 2.0f;

        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].skillRange = 2.6f;
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].skillCooldown = 0.2f;

        _unitBase.Ska.skeleton.SetSkin("1");

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        personal.InitDoll();
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 0)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Huge_Doll_s1_3", 1.0f);
            _victim.AddUnitEffect(UNIT_EFFECT.CC_FEAR, _attacker, _victim, new float[] { 5.0f });
        }

        public void InitDoll()
        {
            unitBase.UnitBaseParent.AddUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, unitBase.UnitBaseParent, new float[] { unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(0, "param.5"), 0.0f }, false);
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
                cooldown -= skillCooldown * unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(2, "param.3");
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), cooldown);
        }

        public override void EventTriggerSkill()
        {
            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unitBase.EnemyTarget, unitBase.GetAtkRateToDamage(unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(0, "param.2")), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 0);
            MgrObjectPool.Instance.ShowObj("FX_Ghost_Necromancer_Boss_Bear_b_skill1_hit", unitBase.EnemyTarget.transform.position);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();
        }
    }
}
