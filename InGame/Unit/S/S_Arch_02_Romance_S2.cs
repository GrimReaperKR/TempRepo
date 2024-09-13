using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_S_Arch_02_Romance_S2", menuName = "UnitSkillEvent/S_Arch_02_Romance_S2")]
public class S_Arch_02_Romance_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private int currHitCnt;

        private List<UnitBase> listUnit = new List<UnitBase>();
        private List<UnitBase> listHitUnit = new List<UnitBase>();

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            if (unitBase.EnemyTarget && MgrBattleSystem.Instance.CheckIsEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, 10.0f)) return true;
            else
            {
                if (unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    return false;

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, 10.0f));
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
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase, _isContainBlockedTarget: true));

            currHitCnt = listUnit.Count > 0 ? listUnit.Count - 1 : 0;

            GameObject objVFX = MgrObjectPool.Instance.ShowObj("FX_S_Arch_02_Romance_skill2_a", unitBase.transform.position);
            objVFX.transform.position = new Vector3(objVFX.transform.position.x, -2.0f, objVFX.transform.position.z);
            objVFX.transform.rotation = Quaternion.Euler(0.0f, unitBase.GetUnitLookDirection() == Vector3.left ? -180.0f : 0.0f, 0.0f);
            objVFX.transform.GetChild(0).GetComponent<PlayableDirector>().Play();
            TaskSkillVFX(objVFX).Forget();

            //TaskSkill(unitBase.GetAtkRateToDamage(resultDamageRate)).Forget();
            TaskSkill(objVFX).Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskSkillVFX(GameObject _objVFX)
        {
            await UniTask.Delay(5000, cancellationToken: _objVFX.GetCancellationTokenOnDestroy());

            MgrObjectPool.Instance.HideObj("FX_S_Arch_02_Romance_skill2_a", _objVFX);
        }

        private async UniTaskVoid TaskSkill(GameObject _objVFX)
        {
            listHitUnit.Clear();

            Vector3 v3Dir = unitBase.GetUnitLookDirection();

            int distanceCnt = 0;
            while(distanceCnt < 10)
            {
                CalculateHitCnt();

                float divAmount = currHitCnt * (float)unitSkillData.Param[3];
                divAmount = divAmount > (float)unitSkillData.Param[4] ? (float)unitSkillData.Param[4] : divAmount;

                float resultDamageRate = ((float)unitSkillData.Param[1] * (int)unitSkillData.Param[2]) - divAmount;
                resultDamageRate = resultDamageRate / (int)unitSkillData.Param[2];

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, _objVFX.transform.position + (distanceCnt * skillRange * 0.1f * v3Dir), skillRange * 0.1f, 10.0f, _isContainBlockedTarget: true));

                foreach (UnitBase unit in listUnit)
                {
                    if (listHitUnit.Contains(unit))
                        continue;

                    listHitUnit.Add(unit);
                    TaskSkillUnitHit(unit, unitBase.GetAtkRateToDamage(resultDamageRate)).Forget();
                }

                distanceCnt++;

                await UniTask.Delay(350, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            }
        }

        private async UniTaskVoid TaskSkillUnitHit(UnitBase _target, float _dmg)
        {
            int hitCnt = (int)unitSkillData.Param[2];
            int damageTimer = 1000 / hitCnt;

            while(hitCnt > 0)
            {
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, _target, _dmg, unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);

                hitCnt--;

                await UniTask.Delay(damageTimer, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            }
        }

        private List<UnitBase> listCalcUnit = new List<UnitBase>();
        private void CalculateHitCnt()
        {
            if(listHitUnit.Count >= currHitCnt)
            {
                listCalcUnit.Clear();
                listCalcUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase, _isContainBlockedTarget: true));

                foreach(UnitBase unit in listCalcUnit)
                {
                    if (listHitUnit.Contains(unit))
                        continue;

                    currHitCnt++;
                }
            }
        }
    }
}
