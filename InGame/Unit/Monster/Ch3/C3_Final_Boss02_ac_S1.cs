using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C3_Final_Boss02_ac_S1", menuName = "UnitSkillEvent/Monster/C3_Final_Boss02_ac_S1")]
public class C3_Final_Boss02_ac_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetTakeDamageEvent(personal.OnDamagedAction);

        _unitBase.Ska.skeleton.SetSkin(_unitBase.UnitIndex.Equals("C3_Final_Boss02_c") ? "c" : "a");
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private float nextHPToSpawn = 0.8f;
        private float spawnToDelay = 0.0f;

        public void OnDamagedAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if(unitBase.UnitStat.HP / unitBase.UnitStat.MaxHP <= nextHPToSpawn)
            {
                if(spawnToDelay <= 0.0f && nextHPToSpawn > 0.0f)
                {
                    MgrBattleSystem.Instance.SpawnWaveUnit();
                    spawnToDelay = 5.0f;
                    TaskSpawnDelay().Forget();
                }
                nextHPToSpawn -= 0.2f;
            }
        }

        private async UniTaskVoid TaskSpawnDelay()
        {
            while(spawnToDelay > 0.0f)
            {
                await UniTask.Yield();
                spawnToDelay -= Time.deltaTime;
            }
        }

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 0)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C3_Final_Boss02_ac_s1_3", 1.0f);
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
            if(unitBase.EnemyTarget.CheckHasUnitEffect(UNIT_EFFECT.CC_FREEZE))
            {
                unitBase.EnemyTarget.AddUnitEffect(UNIT_EFFECT.ETC_INSTANT_DEATH, unitBase, unitBase.EnemyTarget, null);
            }
            else
            {
                MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C3_Final_Boss02_c") ? "FX_Dullahan_boss_c_skill1_hit" : "FX_Dullahan_boss_a_skill1_hit", unitBase.EnemyTarget.GetUnitCenterPos());
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unitBase.EnemyTarget, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);
            }
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1_a,c");
            unitBase.PlayTimeline();
        }
    }
}
