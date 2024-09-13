using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;
using Spine;
using System.Threading;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_S_Spt_02_Mico_S1", menuName = "UnitSkillEvent/S_Spt_02_Mico_S1")]
public class S_Spt_02_Mico_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        _unitBase.UnitSetting.moveSO = MgrInGameData.Instance.GetUnitData(_unitBase.UnitIndex).moveSO;

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetTakeDamageEvent(personal.OnTakeDamagedAction);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private bool isRunSkill = false;
        private bool isActiveSkill = false;

        private SkeletonAnimation skaLeft;
        private SkeletonAnimation skaRight;

        private GameObject objZoneVFX;

        private List<UnitBase> listUnit = new List<UnitBase>();
        private List<UnitBase> listApplyUnit = new List<UnitBase>();

        private float absorbDmgAmount;
        private int atkCnt;

        private CancellationTokenSource token_Hit;

        public void OnTakeDamagedAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_dmgChannel != 99 || _victim != unitBase)
                return;

            unitBase.SetHeal(_damage * (float)unitSkillData.Param[2]);
            // anim effect
        }

        public float OnDamagedInZone(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            float resultDamage = _damage;

            if (!isRunSkill || _dmgChannel < 0)
                return resultDamage;

            MgrObjectPool.Instance.ShowObj("FX_S_Spt_02_Mico_skill1_hit_shield", _victim.GetUnitCenterPos());

            float absorbDamage = resultDamage * (float)unitSkillData.Param[1];
            absorbDmgAmount += absorbDamage;
            MgrInGameEvent.Instance.BroadcastDamageEvent(_attacker, unitBase, absorbDamage, 0.0f, 1.0f, 99);

            token_Hit?.Cancel();
            token_Hit?.Dispose();
            token_Hit = new CancellationTokenSource();
            TaskHitSkill().AttachExternalCancellation(token_Hit.Token);

            return resultDamage - absorbDamage;
        }

        public override bool CheckCanUseSkill()
        {
            UnitBase unitTarget = MgrBattleSystem.Instance.GetFarestXAllyUnit(unitBase);
            if (!(unitTarget is null) && unitTarget != unitBase && !unitBase.CheckIsState(UNIT_STATE.MOVE))
            {
                if(!MathLib.CheckIsInEllipse(unitBase.transform.position, (float)unitSkillData.Param[0], unitTarget.transform.position))
                {
                    float moveSpd = unitBase.UnitStat.MoveSpeed * (1.0f - unitBase.GetUnitEffectTotalValue(UNIT_EFFECT.DEBUFF_SLOW));
                    //unitBase.transform.localScale = new Vector3(unitTarget.transform.position.x < unitBase.transform.position.x ? -1.0f : 1.0f, 1.0f, 1.0f);
                    unitBase.transform.rotation = Quaternion.Euler(0.0f, unitTarget.transform.position.x < unitBase.transform.position.x ? -180.0f : 0.0f, 0.0f);
                    unitBase.SetRotationHpBar();

                    Vector3 v3Dir = (unitTarget.transform.position - unitBase.transform.position).normalized;
                    unitBase.transform.position += moveSpd * Time.deltaTime * v3Dir;

                    if (!unitBase.Ska.AnimationName.Equals(unitBase.animMoveName))
                        unitBase.Ska.AnimationState.SetAnimation(0, unitBase.animMoveName, true);
                }
                else
                {
                    if (!unitBase.Ska.AnimationName.Equals(unitBase.animIdleName))
                        unitBase.Ska.AnimationState.SetAnimation(0, unitBase.animIdleName, true);
                }
            }

            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL) && !isRunSkill)
            {
                if(!(unitTarget is null) && MathLib.CheckIsInEllipse(unitBase.transform.position, (float)unitSkillData.Param[0], unitTarget.transform.position))
                    return true;
            }

            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (_animationName.Equals("skill1_ready"))
            {
                isActiveSkill = true;

                skaLeft.AnimationState.SetAnimation(0, "skill1_idle", true);
                skaRight.AnimationState.SetAnimation(0, "skill1_idle", true);

                unitBase.SetUnitState(UNIT_STATE.IDLE);
                unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);
            }

            if (_animationName.Equals("skill1_attack"))
                ResetMicoSkill();

            if (_animationName.Equals("skill1_finish"))
                unitBase.SetUnitState(UNIT_STATE.IDLE);
        }

        private void OnComplete(TrackEntry trackEntry)
        {
            string animationName = trackEntry.Animation.Name;

            if (animationName.Equals("skill1_hit"))
            {
                unitBase.Ska.AnimationState.Complete -= OnComplete;

                if (!unitBase.CheckIsState(UNIT_STATE.DEATH))
                    unitBase.SetUnitAnimation("skill1_idle", true);
            }
        }

        public override void EventTriggerSkill()
        {
            if (isRunSkill)
                return;

            atkCnt++;

            unitBase.SetHeal(absorbDmgAmount * (float)unitSkillData.Param[3] * 0.2f);

            if(atkCnt == 5)
            {
                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, (float)unitSkillData.Param[0], _isAlly: true));
                listUnit.Remove(unitBase);
                listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());
                listUnit.Shuffle();

                int removeDebuffCnt = (int)unitSkillData.Param[4];
                foreach (UnitBase unit in listUnit)
                {
                    if(!unit.UnitIndex.Equals("S_Spt_02"))
                        unit.SetHeal(absorbDmgAmount * (float)unitSkillData.Param[3]);

                    MgrObjectPool.Instance.ShowObj("FX_PC-Heal", unit.GetUnitCenterPos());
                    if(removeDebuffCnt > 0)
                    {
                        removeDebuffCnt--;
                        MgrObjectPool.Instance.ShowObj("FX_Cleanse_All", unit.GetUnitCenterPos());
                        unit.RemoveDebuffUnitEffect();
                    }
                }
            }
        }

        public override void OnSkill()
        {
            isActiveSkill = false;

            if (!isRunSkill)
            {
                isRunSkill = true;
                absorbDmgAmount = 0.0f;

                skaLeft = MgrObjectPool.Instance.ShowObj("S_Spt_02_Mico_skill1_L", unitBase.transform.position).GetComponent<SkeletonAnimation>();
                skaLeft.transform.SetParent(unitBase.transform);
                skaLeft.transform.rotation = Quaternion.Euler(0.0f, unitBase.TeamNum == 0 ? 0.0f : -180.0f, 0.0f);
                skaRight = MgrObjectPool.Instance.ShowObj("S_Spt_02_Mico_skill1_R", unitBase.transform.position).GetComponent<SkeletonAnimation>();
                skaRight.transform.SetParent(unitBase.transform);
                skaRight.transform.rotation = Quaternion.Euler(0.0f, unitBase.TeamNum == 0 ? 0.0f : -180.0f, 0.0f);

                skaLeft.AnimationState.SetAnimation(0, "skill1_ready", false);
                skaRight.AnimationState.SetAnimation(0, "skill1_ready", false);

                unitBase.SetUnitAnimation("skill1_ready");
                unitBase.PlayTimeline(0);

                unitBase.animIdleName = "skill1_idle";
                unitBase.animMoveName = "skill1_walk";

                TaskReadySkill().Forget();
            }
            else
            {
                isRunSkill = false;
                atkCnt = 0;

                unitBase.animIdleName = "idle";
                unitBase.animMoveName = "walk";

                unitBase.SetUnitAnimation("skill1_attack");
                unitBase.PlayTimeline(1);

                skaLeft.AnimationState.SetAnimation(0, "skill1_attack", false);
                skaRight.AnimationState.SetAnimation(0, "skill1_attack", false);
            }
        }

        private async UniTask TaskHitSkill()
        {
            if (unitBase.CheckIsState(UNIT_STATE.DEATH))
                return;

            unitBase.SetUnitAnimation("skill1_hit");
            unitBase.PlayTimeline(2, soSkillEvent);

            unitBase.Ska.AnimationState.Complete -= OnComplete;
            unitBase.Ska.AnimationState.Complete += OnComplete;

            skaRight.AnimationState.SetAnimation(0, "skill1_hit", false);
            skaRight.AnimationState.AddAnimation(0, "skill1_idle", true, 0.0f);

            await UniTask.Delay(250, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            if (!isRunSkill || unitBase.CheckIsState(UNIT_STATE.DEATH))
                return;

            GameObject objVFX = MgrObjectPool.Instance.ShowObj("FX_W-Heal", unitBase.transform.position);
            objVFX.transform.SetParent(unitBase.transform);
            objVFX.transform.localPosition = new Vector3(-1.03f, 2.47f, 0.0f);

            skaLeft.AnimationState.SetAnimation(0, "skill1_hit", false);
            skaLeft.AnimationState.AddAnimation(0, "skill1_idle", true, 0.0f);
        }

        private async UniTaskVoid TaskReadySkill()
        {
            await UniTask.Delay(1500, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            if(!unitBase.CheckIsState(UNIT_STATE.SKILL))
            {
                ResetMicoSkill();
                return;
            }

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, (float)unitSkillData.Param[0], _isAlly: true));
            listUnit.Remove(unitBase);

            if(listUnit.Count > 0)
                MgrSound.Instance.PlayOneShotSFX("SFX_Buff_ab", 1.0f);

            foreach (UnitBase unit in listUnit)
            {
                unit.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitBase.UnitSkillPersonalVariable[1].unitSkillData.Param[1]), unitBase, unit, new float[] { (float)unitBase.UnitSkillPersonalVariable[1].unitSkillData.Param[0], (float)unitBase.UnitSkillPersonalVariable[1].unitSkillData.Param[2] });
                MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", unit.transform.position).transform.SetParent(unit.transform);
            }

            objZoneVFX = MgrObjectPool.Instance.ShowObj("FX_S_Spt_02_Mico_skill1_zone", unitBase.transform.position);
            objZoneVFX.transform.SetParent(unitBase.transform);

            float duration = skillCooldown;
            float listRefreshTimer = 1.0f;

            while (duration > 0.0f)
            {
                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());

                if (unitBase.CheckIsState(UNIT_STATE.DEATH))
                    break;

                if (!isActiveSkill)
                    continue;

                duration -= Time.deltaTime;

                listRefreshTimer += Time.deltaTime;
                if (listRefreshTimer >= 1.0f)
                {
                    listRefreshTimer -= 1.0f;

                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, (float)unitSkillData.Param[0], _isAlly: true));

                    foreach (UnitBase unit in listUnit)
                    {
                        if (unit == unitBase || unit.CheckHasMicoZoneEvent() || unit.UnitIndex.Equals("S_Spt_02") || listApplyUnit.Contains(unit))
                            continue;

                        unit.AddMicoZoneEvent(OnDamagedInZone);
                        listApplyUnit.Add(unit);
                    }

                    for (int i = listApplyUnit.Count - 1; i >= 0; i--)
                    {
                        if (!MathLib.CheckIsInEllipse(unitBase.transform.position, (float)unitSkillData.Param[0], listApplyUnit[i].transform.position) || listApplyUnit[i].CheckIsState(UNIT_STATE.DEATH))
                        {
                            listApplyUnit[i].RemoveMicoZoneEvent(OnDamagedInZone);
                            listApplyUnit.Remove(listApplyUnit[i]);
                        }
                    }
                }
            }

            await UniTask.WaitUntil(() => !unitBase.CheckHasBlockedSkillCC() || unitBase.CheckIsState(UNIT_STATE.DEATH));

            token_Hit?.Cancel();

            for (int i = listApplyUnit.Count - 1; i >= 0; i--)
            {
                listApplyUnit[i].RemoveMicoZoneEvent(OnDamagedInZone);
                listApplyUnit.Remove(listApplyUnit[i]);
            }

            if (unitBase.CheckIsState(UNIT_STATE.DEATH))
            {
                ResetMicoSkill();
                return;
            }

            unitBase.SetUnitUseSkill(soSkillEvent);
        }

        private void ResetMicoSkill()
        {
            if(skaLeft) MgrObjectPool.Instance.HideObj("S_Spt_02_Mico_skill1_L", skaLeft.gameObject);
            if(skaRight) MgrObjectPool.Instance.HideObj("S_Spt_02_Mico_skill1_R", skaRight.gameObject);

            if(objZoneVFX) MgrObjectPool.Instance.HideObj("FX_S_Spt_02_Mico_skill1_zone", objZoneVFX);

            objZoneVFX = null;

            skaLeft = null;
            skaRight = null;

            unitBase.animIdleName = "idle";
            unitBase.animMoveName = "walk";
            isRunSkill = false;
        }

        public override void ResetSkill()
        {
            if (!isRunSkill)
                unitBase.Skill_CoolDown[0] = 0.0f;

            ResetMicoSkill();
        }
    }
}
