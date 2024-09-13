using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C2_Mid_Boss01_bd_S1", menuName = "UnitSkillEvent/Monster/C2_Mid_Boss01_bd_S1")]
public class C2_Mid_Boss01_bd_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetTakeDamageEvent(personal.OnDamagedAction);

        _unitBase.Ska.skeleton.SetSkin(_unitBase.UnitIndex.Equals("C2_Mid_Boss01_d") ? "d" : "b");
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private float nextHPToSpawn = 0.66f;
        private float spawnToDelay = 0.0f;

        public void OnDamagedAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (unitBase.UnitStat.HP / unitBase.UnitStat.MaxHP <= nextHPToSpawn)
            {
                if (spawnToDelay <= 0.0f && nextHPToSpawn > 0.0f)
                {
                    MgrBattleSystem.Instance.SpawnWaveUnit();
                    spawnToDelay = 5.0f;
                    TaskSpawnDelay().Forget();
                }
                nextHPToSpawn -= 0.33f;
            }
        }

        private async UniTaskVoid TaskSpawnDelay()
        {
            while (spawnToDelay > 0.0f)
            {
                await UniTask.Yield();
                spawnToDelay -= Time.deltaTime;
            }
        }

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 0)
                return;

            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Mid_Boss01_d") ? "FX_Sphinx_mid boss_d_skill_hit" : "FX_Sphinx_mid boss_b_skill_hit", _victim.GetUnitCenterPos());
            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Mid_Boss01_bd_s1_3", 1.0f);
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
            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Mid_Boss01_bd_s1_2", 1.0f);
            TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1_b,d");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskSkill()
        {
            await UniTask.Delay((int)(60.0f * (unitBase.transform.position - unitBase.EnemyTarget.transform.position).magnitude), cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            for(int i = 0; i < 3; i++)
            {
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unitBase.EnemyTarget, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);
                await UniTask.Delay(150, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            }
        }
    }
}
