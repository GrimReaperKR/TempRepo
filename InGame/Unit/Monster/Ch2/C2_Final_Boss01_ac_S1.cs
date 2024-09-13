using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C2_Final_Boss01_ac_S1", menuName = "UnitSkillEvent/Monster/C2_Final_Boss01_ac_S1")]
public class C2_Final_Boss01_ac_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetTakeDamageEvent(personal.OnDamagedAction);

        _unitBase.Ska.skeleton.SetSkin(_unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "c" : "a");
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private float nextHPToSpawn = 0.8f;
        private float spawnToDelay = 0.0f;

        private bool isTrigger;
        private Transform tfBullet;

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

            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Final_Boss01_ac_s1_3", 1.0f);
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
            isTrigger = true;
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1_a,c");
            unitBase.PlayTimeline();

            isTrigger = false;

            TaskSkill().Forget();
        }

        private async UniTaskVoid TaskSkill()
        {
            Vector3 v3CreatePos = unitBase.transform.position + Vector3.up * unitBase.GetUnitHeight() + (2.0f * unitBase.GetUnitLookDirection(true));

            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill1_bullet_summon" : "FX_Anubis_Boss_a_skill1_bullet_summon", v3CreatePos);
            await UniTask.Delay(125, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            tfBullet = MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill1_bullet" : "FX_Anubis_Boss_a_skill1_bullet", v3CreatePos).transform;

            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Final_Boss01_ac_s1_1", 1.0f);

            await UniTask.WaitUntil(() => (isTrigger || !unitBase.CheckIsState(UNIT_STATE.SKILL)), cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            if (!isTrigger)
            {
                MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill1_bullet" : "FX_Anubis_Boss_a_skill1_bullet", tfBullet.gameObject);
                tfBullet = null;
                return;
            }

            UnitBase target = unitBase.EnemyTarget;
            if (target is null)
            {
                MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill1_bullet" : "FX_Anubis_Boss_a_skill1_bullet", tfBullet.gameObject);
                tfBullet = null;
                return;
            }

            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Final_Boss01_ac_s1_2", 1.0f);

            while (true)
            {
                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());

                tfBullet.position += 40.0f * Time.deltaTime * (target.GetUnitCenterPos() - tfBullet.position).normalized;

                if (target.CheckIsState(UNIT_STATE.DEATH) || MathLib.CheckIsPosDistanceInRange(tfBullet.position, target.GetUnitCenterPos(), 0.5f))
                    break;
            }

            MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill1_bullet" : "FX_Anubis_Boss_a_skill1_bullet", tfBullet.gameObject);
            tfBullet = null;

            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, target, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);
            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill1,2_hit" : "FX_Anubis_Boss_a_skill1,2_hit", target.GetUnitCenterPos());
        }
    }
}
