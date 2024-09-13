using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_B_War_02_Chucky_S2", menuName = "UnitSkillEvent/B_War_02_Chucky_S2")]
public class B_War_02_Chucky_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private int hitCnt = 0;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || (_dmgChannel != 1 && _dmgChannel != 2))
                return;

            if (_dmgChannel == 1) MgrSound.Instance.PlayOneShotSFX("SFX_B_War_02_s1_2", 1.0f);
            if (_dmgChannel == 2) MgrObjectPool.Instance.ShowObj("FX_B_War_02_Chucky_skill2_dot-hit", _victim.GetUnitCenterPos());
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
            hitCnt++;

            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unitBase.EnemyTarget, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
            MgrObjectPool.Instance.ShowObj("FX_B_War_02_Chucky_skill2_hit", unitBase.EnemyTarget.GetUnitCenterPos());

            if(hitCnt == 4)
            {
                if (MathLib.CheckPercentage((float)unitSkillData.Param[1]))
                    TaskDotSkill(unitBase.EnemyTarget).Forget();
            }
        }

        public override void OnSkill()
        {
            hitCnt = 0;
            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskDotSkill(UnitBase _target)
        {
            int dotCnt = (int)unitSkillData.Param[3];
            float duration = 0.0f;
            while(dotCnt > 0)
            {
                if (_target.CheckIsState(UNIT_STATE.DEATH))
                    break;

                duration += Time.deltaTime;
                if(duration >= 1.0f)
                {
                    duration -= 1.0f;
                    dotCnt--;
                    MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, _target, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[2]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 2);
                }

                await UniTask.Yield(cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            }
        }
    }
}
