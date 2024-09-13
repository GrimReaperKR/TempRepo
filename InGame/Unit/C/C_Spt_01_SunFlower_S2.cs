using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C_Spt_01_SunFlower_S2", menuName = "UnitSkillEvent/C_Spt_01_SunFlower_S2")]
public class C_Spt_01_SunFlower_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private int checkEffectIndex = 0;

        public override bool CheckCanUseSkill()
        {
            if (checkEffectIndex == 0)
                checkEffectIndex = (int)unitSkillData.Param[1];

            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL) && MgrBattleSystem.Instance.CheckHasEffectUnitInEllipse(unitBase, unitBase.transform.position, skillRange, MgrInGameData.Instance.GetUnitEffectByIndexNum(checkEffectIndex), true))
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
            listUnit.AddRange(MgrBattleSystem.Instance.GetHasEffectUnitInEllipse(unitBase, unitBase.transform.position, skillRange, MgrInGameData.Instance.GetUnitEffectByIndexNum(checkEffectIndex), true));

            if (listUnit.Contains(unitBase))
            {
                listUnit.Remove(unitBase);
                listUnit.Add(unitBase);
            }

            MgrObjectPool.Instance.ShowObj("FX_Cleanse-A", unitBase.GetUnitCenterPos());

            TaskSkill().Forget();
        }
        
        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskSkill()
        {
            await UniTask.Delay(200, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            int targetCnt = (int)unitSkillData.Param[0];
            foreach (UnitBase unit in listUnit)
            {
                if (targetCnt == 0)
                    break;

                targetCnt--;
                unit.RemoveUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum(checkEffectIndex), _isForceWorldRemove: true);
                MgrObjectPool.Instance.ShowObj("FX_Cleanse-A", unit.GetUnitCenterPos());
            }
        }
    }
}
