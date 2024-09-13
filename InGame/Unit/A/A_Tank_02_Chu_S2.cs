using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_A_Tank_02_Chu_S2", menuName = "UnitSkillEvent/A_Tank_02_Chu_S2")]
public class A_Tank_02_Chu_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private int sfxChannel;

        private UnitBase unitTarget;
        private List<UnitBase> listUnit = new List<UnitBase>();

        private GameObject objVFX = null;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_A_Tank_02_s2_3", 1.0f);

            if (MathLib.CheckPercentage((float)unitSkillData.Param[1]))
                _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitSkillData.Param[2]), _attacker, _victim, new float[] { 3.0f });
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            unitTarget = unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT) ? unitBase.EnemyTarget : MgrBattleSystem.Instance.GetNearestXEnemyUnit(unitBase);
            if (!(unitTarget is null))
                return true;

            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill"))
                return;

            unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, true);

            unitBase.SetUnitState(UNIT_STATE.IDLE);
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);
        }

        public override void EventTriggerSkill()
        {
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, 2.5f, _isContainBlockedTarget: true));

            foreach (UnitBase unit in listUnit)
            {
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
                MgrObjectPool.Instance.ShowObj("FX_A_Tank_02_Chu_skill2_hit", unit.GetUnitCenterPos());
            }
        }

        public override void OnSkill()
        {
            unitBase.AddUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, unitBase, new float[] { (float)unitSkillData.Param[3], 0.0f }, false);

            if (MathLib.CheckIsPosDistanceInRange(unitTarget.transform.position, unitBase.transform.position, skillRange))
            {
                sfxChannel = -1;

                unitBase.SetUnitAnimation("skill2");
                unitBase.PlayTimeline();
            }
            else
            {
                sfxChannel = MgrSound.Instance.PlaySFX("SFX_A_Tank_02_s2_1", 0.5f);

                unitBase.SetUnitAnimation("walk", true);
                TaskSkill().Forget();
            }
        }

        private async UniTaskVoid TaskSkill()
        {
            objVFX = MgrObjectPool.Instance.ShowObj("rootTrail", unitBase.transform.position);
            objVFX.transform.SetParent(unitBase.transform);
            objVFX.transform.rotation = Quaternion.Euler(0.0f, unitBase.GetUnitLookDirection() == Vector3.left ? -180.0f : 0.0f, 0.0f);

            while (unitBase.CheckIsState(UNIT_STATE.SKILL))
            {
                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());

                if (unitTarget.CheckIsState(UNIT_STATE.DEATH))
                {
                    unitBase.SetUnitState(UNIT_STATE.IDLE);
                    unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), 1.0f);
                    break;
                }

                if (MathLib.CheckIsPosDistanceInRange(unitTarget.transform.position, unitBase.transform.position, skillRange))
                {
                    unitBase.SetUnitAnimation("skill2");
                    unitBase.PlayTimeline();
                    break;
                }

                Vector3 v3Dir = (unitTarget.transform.position - unitBase.transform.position).normalized;

                //unitBase.transform.localScale = new Vector3(v3Dir.x > 0.0f ? 1.0f : -1.0f, 1.0f, 1.0f);
                unitBase.transform.rotation = Quaternion.Euler(0.0f, v3Dir.x > 0.0f ? 0.0f : -180.0f, 0.0f);
                unitBase.SetRotationHpBar();
                unitBase.transform.position += unitBase.UnitStat.MoveSpeed * 3.0f * Time.deltaTime * v3Dir;
            }

            if(objVFX is not null)
            {
                MgrObjectPool.Instance.HideObj("rootTrail", objVFX);
                objVFX = null;
            }
            MgrSound.Instance.StopSFX("SFX_A_Tank_02_s2_1", sfxChannel);
            sfxChannel = -1;
        }

        public override void ResetSkill()
        {
            unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, true);
        }
    }
}
