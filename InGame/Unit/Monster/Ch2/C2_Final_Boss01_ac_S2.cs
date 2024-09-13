using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C2_Final_Boss01_ac_S2", menuName = "UnitSkillEvent/Monster/C2_Final_Boss01_ac_S2")]
public class C2_Final_Boss01_ac_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private bool isTrigger;
        private Transform[] tfBullet = new Transform[5];

        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Final_Boss01_ac_s2_3", 1.0f);
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
            unitBase.SetUnitAnimation("skill2_a,c");
            unitBase.PlayTimeline();

            isTrigger = false;

            TaskSkill().Forget();
        }

        private async UniTaskVoid TaskSkill()
        {
            Vector3 v3BasePos = unitBase.transform.position + Vector3.up * unitBase.GetUnitHeight() + (2.0f * unitBase.GetUnitLookDirection(true));
            Vector3[] v3CreatePos = new Vector3[] {
                v3BasePos + Vector3.up * 0.25f + (0.25f * unitBase.GetUnitLookDirection(true)),
                v3BasePos + Vector3.up * 1.0f + (0.5f * unitBase.GetUnitLookDirection()),
                v3BasePos + Vector3.up * 2.0f,
                v3BasePos + Vector3.up * -1.0f + (0.5f * unitBase.GetUnitLookDirection(true)),
                v3BasePos + Vector3.up * -1.0f + (1.5f * unitBase.GetUnitLookDirection(true)),
            };

            for(int i = 0; i < v3CreatePos.Length; i++)
            {
                TaskCreateSkill2_Bullet(i, v3CreatePos[i]).Forget();
                await UniTask.Delay(100, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            }

            await UniTask.WaitUntil(() => (isTrigger || !unitBase.CheckIsState(UNIT_STATE.SKILL)), cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            if (!isTrigger)
            {
                ResetSkillVFX();
                return;
            }
            
            UnitBase target = MgrBattleSystem.Instance.GetFarestXEnemyUnitInRange(unitBase, skillRange);
            if (target is null)
                target = MgrBattleSystem.Instance.GetAllyBase();

            if (target is null)
            {
                ResetSkillVFX();
                return;
            }

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitInSameIndex(unitBase, target.UnitSetting.unitIndex));

            listUnit.Shuffle();

            int shootCnt = 5 + (listUnit.Count - 1);
            for (int i = 0; i < shootCnt; i++)
            {
                if ((shootCnt - i) - 5 > 0) TaskMoveBullet(MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill2_bullet" : "FX_Anubis_Boss_a_skill2_bullet", v3CreatePos[i % 5]).transform, i >= listUnit.Count ? listUnit[Random.Range(0, listUnit.Count)] : listUnit[i]).Forget();
                else
                {
                    MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill2_bullet" : "FX_Anubis_Boss_a_skill2_bullet", tfBullet[i % 5].gameObject);
                    tfBullet[i % 5] = null;
                    TaskMoveBullet(MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill2_bullet" : "FX_Anubis_Boss_a_skill2_bullet", v3CreatePos[i % 5]).transform, i >= listUnit.Count ? listUnit[Random.Range(0, listUnit.Count)] : listUnit[i]).Forget();
                }

                await UniTask.Delay(100, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            }
        }

        private async UniTaskVoid TaskCreateSkill2_Bullet(int _index, Vector3 _v3Pos)
        {
            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Final_Boss01_ac_s2_1", 1.0f);
            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill2_bullet_summon" : "FX_Anubis_Boss_a_skill2_bullet_summon", _v3Pos);
            await UniTask.Delay(125, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            tfBullet[_index] = MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill2_bullet" : "FX_Anubis_Boss_a_skill2_bullet", _v3Pos).transform;
        }

        private async UniTaskVoid TaskMoveBullet(Transform _tfBullet, UnitBase _target)
        {
            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Final_Boss01_ac_s2_2", 1.0f);

            while (true)
            {
                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());

                _tfBullet.position += 40.0f * Time.deltaTime * (_target.GetUnitCenterPos() - _tfBullet.position).normalized;

                if (_target.CheckIsState(UNIT_STATE.DEATH) || MathLib.CheckIsPosDistanceInRange(_tfBullet.position, _target.GetUnitCenterPos(), 0.5f))
                    break;
            }

            MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill2_bullet" : "FX_Anubis_Boss_a_skill2_bullet", _tfBullet.gameObject);

            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, _target, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill1,2_hit" : "FX_Anubis_Boss_a_skill1,2_hit", _target.GetUnitCenterPos());
        }

        private void ResetSkillVFX()
        {
            for (int i = 0; i < tfBullet.Length; i++)
            {
                if (tfBullet[i] is not null)
                {
                    MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill2_bullet" : "FX_Anubis_Boss_a_skill2_bullet", tfBullet[i].gameObject);
                    tfBullet[i] = null;
                }
            }
        }
    }
}
