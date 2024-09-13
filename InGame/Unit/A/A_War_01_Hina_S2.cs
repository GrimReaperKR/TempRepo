using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_A_War_01_Hina_S2", menuName = "UnitSkillEvent/A_War_01_Hina_S2")]
public class A_War_01_Hina_S2 : SOBase_UnitSkillEvent
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
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_A_War_01_s2_2", 1.0f);
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

            if (hitCnt == 1)
                TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            hitCnt = 0;

            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();

            unitBase.AddUnitEffect(UNIT_EFFECT.BUFF_SHIELD, unitBase, unitBase, new float[] { unitBase.UnitStat.MaxHP * (float)unitSkillData.Param[2], (float)unitSkillData.Param[3] });
        }

        private async UniTaskVoid TaskSkill()
        {
            int atkCnt = (int)unitSkillData.Param[1];
            int dmgDelay = (int)(1000.0f / atkCnt);

            for(int i = 0; i < atkCnt; i++)
            {
                if(unitBase.EnemyTarget is null || unitBase.EnemyTarget.CheckIsState(UNIT_STATE.DEATH))
                    unitBase.EnemyTarget = MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, skillRange);

                if (!unitBase.CheckIsState(UNIT_STATE.SKILL))
                    break;

                if (unitBase.EnemyTarget is not null)
                {
                    MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unitBase.EnemyTarget, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
                    MgrObjectPool.Instance.ShowObj("FX_A_War_01_Hina_skill2_Hit", unitBase.EnemyTarget.GetUnitCenterPos() + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.15f, 0.15f), 0.0f));
                }

                await UniTask.Delay(dmgDelay);
            }
        }
    }
}
