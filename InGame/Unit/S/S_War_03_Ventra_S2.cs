using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_S_War_03_Ventra_S2", menuName = "UnitSkillEvent/S_War_03_Ventra_S2")]
public class S_War_03_Ventra_S2 : SOBase_UnitSkillEvent
{
    [SerializeField] private Material matCustom;

    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        Spine.Slot slot = _unitBase.Ska.skeleton.FindSlot("V FX");
        if (!_unitBase.Ska.CustomSlotMaterials.ContainsKey(slot))
            _unitBase.Ska.CustomSlotMaterials.Add(slot, matCustom);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private int atkCnt;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_S_War_03_s2_5", 1.0f);
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            if (unitBase.EnemyTarget && MgrBattleSystem.Instance.CheckIsEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, 2.0f)) return true;
            else
            {
                if (unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    return false;

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, 2.0f));
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

            unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, true);
            unitBase.SetUnitState(UNIT_STATE.IDLE);
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);
        }

        public override void EventTriggerSkill()
        {
            atkCnt++;

            if (unitBase.UnitLvData.promotion < 5 && (atkCnt == 13 || atkCnt == 14))
                return;

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, 2.0f, (int)unitSkillData.Param[1], _isContainBlockedTarget: true));
            
            foreach (UnitBase unit in listUnit)
            {
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[2]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);

                if(atkCnt <= 4 || atkCnt >= 15) MgrObjectPool.Instance.ShowObj("FX_S_War_03_Ventra_skill2_hit1", unit.GetUnitCenterPos());
                else MgrObjectPool.Instance.ShowObj("FX_S_War_03_Ventra_skill2_hit2", unit.GetUnitCenterPos());
            }
        }

        public override void OnSkill()
        {
            unitBase.AddUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, unitBase, new float[] { (float)unitSkillData.Param[0], 0.0f }, false);

            atkCnt = 0;

            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }

        public override void ResetSkill()
        {
            unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, true);
        }
    }
}
