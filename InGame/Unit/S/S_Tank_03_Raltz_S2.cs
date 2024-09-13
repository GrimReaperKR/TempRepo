using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_S_Tank_03_Raltz_S2", menuName = "UnitSkillEvent/S_Tank_03_Raltz_S2")]
public class S_Tank_03_Raltz_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private List<UnitBase> listUnitHit = new List<UnitBase>();
        private float totalDmgToShield;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || (_dmgChannel != 1 && _dmgChannel != 2))
                return;

            if (_dmgChannel == 1)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_S_Tank_03_s2_3", 1.0f);
                totalDmgToShield += _damage * (float)unitSkillData.Param[4];
            }
            if (_dmgChannel == 2)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_S_Tank_03_s2_4", 1.0f);
                MgrObjectPool.Instance.ShowObj("FX_S_Tank_03_Raltz_skill2_dot hit", _victim.GetUnitCenterPos());
            }
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            Vector3 v3Pos = unitBase.GetUnitLookDirection();
            if (unitBase.EnemyTarget && MgrBattleSystem.Instance.CheckIsEnemyUnitInLine(unitBase, unitBase.transform.position + Vector3.up * 1.75f, skillRange + 2.0f, 4.0f)) return true;
            else
            {
                if (unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    return false;

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position + Vector3.up * 1.75f, skillRange + 2.0f, 4.0f));
                UnitBase unitInEllipse = listUnit.Count > 0 ? listUnit[0] : null;
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
            Vector3 v3Pos = unitBase.GetUnitLookDirection();

            MgrObjectPool.Instance.ShowObj("FX_Fire sphere", unitBase.transform.position + v3Pos * 4.0f + Vector3.up * 1.75f + new Vector3(0.0f, 0.0f, -0.0025f));

            listUnitHit.Clear();
            listUnitHit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position + Vector3.up * 1.75f, skillRange + 2.0f, 4.0f, _isContainBlockedTarget: true));

            TaskSkillHit().Forget();

            TaskShield().Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();

            totalDmgToShield = 0;
        }

        private async UniTaskVoid TaskShield()
        {
            await UniTask.Delay(1500, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            if(totalDmgToShield > 0.0f)
            {
                if (totalDmgToShield > unitBase.UnitStat.MaxHP * 0.2f)
                    totalDmgToShield = unitBase.UnitStat.MaxHP * 0.2f;

                unitBase.AddUnitEffect(UNIT_EFFECT.BUFF_SHIELD, unitBase, unitBase, new float[] { totalDmgToShield, (float)unitSkillData.Param[5] });
            }
        }

        private async UniTaskVoid TaskSkillHit()
        {
            for(int x = 0; x < 3; x++)
            {
                for(int i = listUnitHit.Count - 1; i >= 0; i--)
                {
                    if (listUnitHit[i].CheckIsState(UNIT_STATE.DEATH))
                    {
                        listUnitHit.Remove(listUnitHit[i]);
                        continue;
                    }

                    MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, listUnitHit[i], unitBase.GetAtkRateToDamage((float)unitSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
                }

                await UniTask.Delay(500, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            }

            if (Random.Range(0.001f, 1.0f) <= (float)unitSkillData.Param[1])
            {
                for (int i = listUnitHit.Count - 1; i >= 0; i--)
                {
                    if (listUnitHit[i].CheckIsState(UNIT_STATE.DEATH))
                    {
                        listUnitHit.Remove(listUnitHit[i]);
                        continue;
                    }

                    TaskSkillDotHit(listUnitHit[i], (float)unitSkillData.Param[2]).Forget();
                }
            }
        }

        private async UniTaskVoid TaskSkillDotHit(UnitBase _target, float _dotSkillPower)
        {
            int hitCnt = (int)(float)unitSkillData.Param[3];

            while(hitCnt > 0)
            {
                await UniTask.Delay(1000, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

                if (_target.CheckIsState(UNIT_STATE.DEATH))
                    break;

                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, _target, unitBase.GetAtkRateToDamage(_dotSkillPower), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 2);

                hitCnt--;
            }
        }
    }
}
