using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Final_Boss03_bd_S2", menuName = "UnitSkillEvent/Monster/C4_Final_Boss03_bd_S2")]
public class C4_Final_Boss03_bd_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
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
            float atkAmount = 0.0f;

            UnitBase target = MgrBattleSystem.Instance.GetHighestAtkEnemyUnit(unitBase);
            if (target is not null)
                atkAmount = target.GetAtk() * (float)bossSkillData.Param[1];

            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Final_Boss03_bd_s2_2", 1.0f);

            unitBase.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[2]), unitBase, unitBase, new float[] { atkAmount, (float)bossSkillData.Param[0] });
            unitBase.AddUnitEffectVFX(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[2]), unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "FX_Ghost_Necromancer_Boss_d_Ghost Shield" : "FX_Ghost_Necromancer_Boss_b_Ghost Shield", unitBase.GetUnitCenterPos());
            MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", unitBase.transform.position).transform.SetParent(unitBase.transform);
            TaskSkill_Parent().Forget();

            C4_Final_Boss03_bd_S1.PersonalVariable personal = unitBase.UnitSkillPersonalVariable[0] as C4_Final_Boss03_bd_S1.PersonalVariable;
            if(!personal.unitDoll.CheckIsState(UNIT_STATE.DEATH))
            {
                personal.unitDoll.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[2]), unitBase, personal.unitDoll, new float[] { atkAmount, (float)bossSkillData.Param[0] });
                personal.unitDoll.AddUnitEffectVFX(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[2]), unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "FX_Ghost_Necromancer_Boss_d_Bear_Ghost Shield" : "FX_Ghost_Necromancer_Boss_b_Bear_Ghost Shield", personal.unitDoll.GetUnitCenterPos(), true);
                MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", personal.unitDoll.transform.position).transform.SetParent(personal.unitDoll.transform);
                TaskSkill_Spawn().Forget();
            }

        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2_b,d");
            unitBase.PlayTimeline(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? 1 : 0);
        }

        private async UniTaskVoid TaskSkill_Parent()
        {
            await UniTask.WaitUntil(() => !unitBase.CheckHasUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[2])), cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            unitBase.RemoveUnitEffectVFX(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[2]), unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "FX_Ghost_Necromancer_Boss_d_Ghost Shield" : "FX_Ghost_Necromancer_Boss_b_Ghost Shield");
        }

        private async UniTaskVoid TaskSkill_Spawn()
        {
            C4_Final_Boss03_bd_S1.PersonalVariable personal = unitBase.UnitSkillPersonalVariable[0] as C4_Final_Boss03_bd_S1.PersonalVariable;
            await UniTask.WaitUntil(() => !personal.unitDoll.CheckHasUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[2])), cancellationToken: personal.unitDoll.GetCancellationTokenOnDestroy());
            personal.unitDoll.RemoveUnitEffectVFX(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[2]), unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "FX_Ghost_Necromancer_Boss_d_Ghost Shield" : "FX_Ghost_Necromancer_Boss_b_Ghost Shield");
        }
    }
}
