using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_B_Spt_01_Ghost_S1", menuName = "UnitSkillEvent/B_Spt_01_Ghost_S1")]
public class B_Spt_01_Ghost_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private List<UnitBase> listApplyUnit = new List<UnitBase>();
        private Vector3 v3Pos;

        private bool isRunningSkill = false;

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL) && !isRunningSkill)
                return true;

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
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase, true));
            if(unitBase.TeamNum == 0) listUnit.Sort((a, b) => (b.transform.position.x).CompareTo(a.transform.position.x));
            else if(unitBase.TeamNum == 1) listUnit.Sort((a, b) => (a.transform.position.x).CompareTo(b.transform.position.x));

            for (int i = listUnit.Count - 1; i >= 0; i--)
            {
                if (listUnit[i].UnitGhostoEffect is not null)
                    listUnit.RemoveAt(i);
            }

            if (listUnit.Count == 0)
                listUnit.Add(MgrBattleSystem.Instance.GetFarestXAllyUnit(unitBase));

            v3Pos = listUnit[0].transform.position;
            MgrObjectPool.Instance.ShowObj(unitBase.UnitLvData.promotion >= 4 ? "FX_B_Spt_01_Ghost_shield_S 5.0" : "FX_B_Spt_01_Ghost_shield_S 3.0", v3Pos);

            TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskSkill()
        {
            isRunningSkill = true;

            await UniTask.Delay(250, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            float duration = (float)unitSkillData.Param[0];
            float refreshTimer = 1.0f;

            MgrSound.Instance.PlayOneShotSFX("SFX_B_Spt_01_s1", 1.0f);

            while(duration > 0.0f)
            {
                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());

                duration -= Time.deltaTime;

                if(refreshTimer >= 1.0f)
                {
                    refreshTimer -= 1.0f;

                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, v3Pos, (float)unitSkillData.Param[0], _isAlly: true));

                    foreach (UnitBase unit in listUnit)
                    {
                        if (unit == unitBase || unit.UnitGhostoEffect is not null || listApplyUnit.Contains(unit))
                            continue;

                        unit.UnitGhostoEffect = unitBase;
                        listApplyUnit.Add(unit);
                    }

                    for (int i = listApplyUnit.Count - 1; i >= 0; i--)
                    {
                        if (!MathLib.CheckIsInEllipse(v3Pos, (float)unitSkillData.Param[0], listApplyUnit[i].transform.position) || listApplyUnit[i].CheckIsState(UNIT_STATE.DEATH))
                        {
                            listApplyUnit[i].UnitGhostoEffect = null;
                            listApplyUnit.Remove(listApplyUnit[i]);
                        }
                    }
                }
            }

            for (int i = listApplyUnit.Count - 1; i >= 0; i--)
            {
                listApplyUnit[i].UnitGhostoEffect = null;
                listApplyUnit.Remove(listApplyUnit[i]);
            }

            isRunningSkill = false;
        }
    }
}
