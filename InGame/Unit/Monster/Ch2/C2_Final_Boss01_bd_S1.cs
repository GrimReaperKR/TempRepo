using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C2_Final_Boss01_bd_S1", menuName = "UnitSkillEvent/Monster/C2_Final_Boss01_bd_S1")]
public class C2_Final_Boss01_bd_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetTakeDamageEvent(personal.OnDamagedAction);

        _unitBase.Ska.skeleton.SetSkin(_unitBase.UnitIndex.Equals("C2_Final_Boss01_d") ? "d" : "b");
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private float nextHPToSpawn = 0.8f;
        private float spawnToDelay = 0.0f;

        private ParticleSystem parsysLaser = null;
        private ParticleSystem.MainModule mainModule;
        private ParticleSystem.MinMaxCurve curve;

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

            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Final_Boss01_bd_s1_2", 1.0f);
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
            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unitBase.EnemyTarget, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);
            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_d") ? "FX_Anubis_Boss_d_skill1_hit" : "FX_Anubis_Boss_b_skill1_hit", unitBase.EnemyTarget.GetUnitCenterPos());
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1_b,d");
            unitBase.PlayTimeline();

            if(parsysLaser is null)
            {
                ControlTrack ct = null;
                foreach (var output in unitBase.ArrPlayableAssets[0][0].outputs)
                {
                    if (output.sourceObject is ControlTrack && output.sourceObject.name.Equals("Laser"))
                    {
                        ct = (ControlTrack)output.sourceObject;
                        break;
                    }
                }
                if(!(ct is null))
                {
                    foreach(TimelineClip clip in ct.GetClips())
                    {
                        ControlPlayableAsset ctAsset = (ControlPlayableAsset)clip.asset;

                        bool idValid;
                        var obj = unitBase.UnitPlayableDirector.GetReferenceValue(ctAsset.sourceGameObject.exposedName, out idValid);
                        if(idValid)
                        {
                            parsysLaser = ((GameObject)obj).GetComponent<ParticleSystem>();
                            mainModule = parsysLaser.main;
                            curve = mainModule.startRotation;
                        }
                    }
                }
            }

            Vector2 v2Vel = unitBase.EnemyTarget.GetUnitCenterPos() - parsysLaser.transform.position;
            float angle = Mathf.Atan2(v2Vel.y, v2Vel.x);

            mainModule.startSizeYMultiplier = v2Vel.magnitude;
            curve.constant = (90.0f + (angle * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
            mainModule.startRotation = curve;
            //mainModule.flipRotation = unitBase.transform.rotation.y == -1.0f ? 0.0f : 1.0f;
        }
    }
}
