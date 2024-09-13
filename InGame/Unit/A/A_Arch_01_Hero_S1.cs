using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_A_Arch_01_Hero_S1", menuName = "UnitSkillEvent/A_Arch_01_Hero_S1")]
public class A_Arch_01_Hero_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private ParticleSystem parsysLaser;
        private ParticleSystem.MainModule mainModule;
        private ParticleSystem.MinMaxCurve curve;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 0)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_A_Arch_01_s1_2", 1.0f);
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
            MgrSound.Instance.PlayOneShotSFX("SFX_A_Arch_01_s1_1", 1.0f);

            bool isKnockback = false;
            if (MgrBattleSystem.Instance.GetAllyBase() && unitBase.transform.position.x > MgrBattleSystem.Instance.GetAllyBase().transform.position.x)
            {
                isKnockback = true;
                TaskSkill().Forget();
            }

            Vector2 v2Vel = unitBase.EnemyTarget.GetUnitCenterPos() - parsysLaser.transform.position;
            float angle = Mathf.Atan2(v2Vel.y, v2Vel.x);

            mainModule.startSizeYMultiplier = v2Vel.magnitude + (isKnockback ? 1.0f : 0.0f);
            curve.constant = (90.0f + (angle * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
            mainModule.startRotation = curve;

            parsysLaser.Play();

            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unitBase.EnemyTarget, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);
            MgrObjectPool.Instance.ShowObj("FX_A_Arch_01_Hero_skill1_hit", unitBase.EnemyTarget.GetUnitCenterPos());
        }

        public override void OnSkill()
        {
            if (parsysLaser is null)
            {
                parsysLaser = unitBase.transform.Find("FX_Laser(Clone)").GetComponent<ParticleSystem>();
                mainModule = parsysLaser.main;
                curve = mainModule.startRotation;

                parsysLaser.gameObject.SetActive(true);
            }

            int randMotion = Random.Range(1, 4);
            parsysLaser.transform.localPosition = new Vector3(0.0f, randMotion == 3 ? 0.9f : 0.88f, 0.0f);

            unitBase.SetUnitAnimation($"skill1_{randMotion}");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskSkill()
        {
            float duration = 0.2f;
            Vector3 v3EndPos = unitBase.transform.position + (unitBase.GetUnitLookDirection(true));
            if (unitBase.GetUnitLookDirection() == Vector3.right && MgrBattleSystem.Instance.GetAllyBase() && v3EndPos.x < MgrBattleSystem.Instance.GetAllyBase().transform.position.x)
                v3EndPos.x = MgrBattleSystem.Instance.GetAllyBase().transform.position.x;

            while (duration > 0.0f)
            {
                if (unitBase.CheckHasBlockedMoveCC() || unitBase.CheckIsState(UNIT_STATE.DEATH))
                    break;

                duration -= Time.deltaTime;

                unitBase.transform.position = Vector3.Lerp(unitBase.transform.position, v3EndPos, (0.2f - duration) / 0.2f);

                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());
            }
        }
    }
}
