using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_B_Spt_03_Dinosaur_S1", menuName = "UnitSkillEvent/B_Spt_03_Dinosaur_S1")]
public class B_Spt_03_Dinosaur_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL) && MgrBattleSystem.Instance.CheckIsEnemyUnitInEllipse(unitBase, unitBase.transform.position, skillRange))
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
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, skillRange));
            listUnit.Sort((a, b) => b.GetAtk().CompareTo(a.GetAtk()));

            for (int i = listUnit.Count - 1; i >= 0; i--)
            {
                if (listUnit[i].UnitSetting.unitType == UnitType.AllyBase || listUnit[i].UnitSetting.unitType == UnitType.MidBoss || listUnit[i].UnitSetting.unitType == UnitType.Boss)
                    listUnit.Remove(listUnit[i]);
            }

            TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskSkill()
        {
            int shootCnt = (int)unitSkillData.Param[3] < listUnit.Count ? (int)unitSkillData.Param[3] : listUnit.Count;

            int shootIndex = 0;
            while(shootCnt > 0)
            {
                GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_B_Spt_03_S1", unitBase.GetUnitCenterPos());
                objBullet.GetComponent<Bullet>().SetBullet(unitBase, listUnit[shootIndex]);

                shootIndex++;
                shootCnt--;

                await UniTask.Delay(200, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            }
        }
    }
}
