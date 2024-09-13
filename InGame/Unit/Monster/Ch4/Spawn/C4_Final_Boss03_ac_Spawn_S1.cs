using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Final_Boss03_ac_Spawn_S1", menuName = "UnitSkillEvent/Monster/C4_Final_Boss03_ac_Spawn_S1")]
public class C4_Final_Boss03_ac_Spawn_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        _unitBase.UnitStat.Atk = _unitBase.UnitBaseParent.UnitStat.Atk * _unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(1, "param.2");
        _unitBase.UnitStat.MaxHP = _unitBase.UnitBaseParent.UnitStat.MaxHP * _unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(1, "param.3");
        _unitBase.UnitStat.HP = _unitBase.UnitStat.MaxHP;
        _unitBase.UnitStat.CriRate = 0.15f;
        _unitBase.UnitStat.Range = 2.0f;
        _unitBase.UnitStat.MoveSpeed = 2.0f;
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].skillRange = 2.0f;
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].skillCooldown = 0.5f;

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.Ska.AnimationState.Complete -= personal.OnComplete;
        _unitBase.Ska.AnimationState.Complete += personal.OnComplete;

        if (MgrBattleSystem.Instance.IsBossAngry)
            MgrBattleSystem.Instance.SetUnitAngry(_unitBase);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
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
            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unitBase.EnemyTarget, unitBase.GetAtkRateToDamage((float)unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(1, "param.4")), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);
            MgrObjectPool.Instance.ShowObj("FX_Ghost_Mini-doll_skill1_hit", unitBase.EnemyTarget.GetUnitCenterPos() + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0.0f));
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline(0);
        }

        public void OnComplete(Spine.TrackEntry trackEntry)
        {
            string animationName = trackEntry.Animation.Name;

            if (animationName.Equals("resurrection"))
            {
                unitBase.SetUnitState(UNIT_STATE.IDLE, true);

                if (unitBase.UnitBaseParent.CheckIsState(UNIT_STATE.DEATH))
                    unitBase.OnDefaultDeath(MgrBattleSystem.Instance.GetAllyBase(), unitBase, -1);

                unitBase.Ska.AnimationState.Complete -= OnComplete;
            }
        }
    }
}
