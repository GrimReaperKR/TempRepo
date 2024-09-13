using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Final_Boss03_ac_S3", menuName = "UnitSkillEvent/Monster/C4_Final_Boss03_ac_S3")]
public class C4_Final_Boss03_ac_S3 : SOBase_UnitSkillEvent
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
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            return true;
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
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase, true, true));
            foreach(UnitBase unit in listUnit)
            {
                unit.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[1]), unitBase, unit, new float[] { (float)bossSkillData.Param[2], (float)bossSkillData.Param[0] });
                unit.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[3]), unitBase, unit, new float[] { (float)bossSkillData.Param[4], (float)bossSkillData.Param[0] });
                MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", unit.transform.position).transform.SetParent(unit.transform);
            }

            if(listUnit.Count > 0)
                MgrSound.Instance.PlayOneShotSFX("SFX_Buff_ab", 1.0f);

            C4_Final_Boss03_ac_S2.PersonalVariable personal = unitBase.UnitSkillPersonalVariable[1] as C4_Final_Boss03_ac_S2.PersonalVariable;
            int totalSpawnCnt = personal.SpawnedCnt - MgrBattleSystem.Instance.GetUnitInSameIndex(1, unitBase.UnitIndex.Equals("C4_Final_Boss03_c") ? "C4_Final_Boss03_c_s" : "C4_Final_Boss03_a_s").Count;
            personal.TaskUnitRespawn(totalSpawnCnt).Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill3_a,c");
            unitBase.PlayTimeline(unitBase.UnitIndex.Equals("C4_Final_Boss03_c") ? 1 : 0);
        }
    }
}
