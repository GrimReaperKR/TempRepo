using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Linq;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_S_Tank_03_Raltz_S1", menuName = "UnitSkillEvent/S_Tank_03_Raltz_S1")]
public class S_Tank_03_Raltz_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        _unitBase.UnitStat.WidthRange = 2.0f;
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 0)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_S_Tank_03_s1_2", 1.0f);
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            unitBase.UnitStat.Range = skillRange * (unitBase.Shield > 0.0f ? 2.0f : 1.0f);

            if (unitBase.EnemyTarget && MgrBattleSystem.Instance.CheckIsEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange * (unitBase.Shield > 0.0f ? 2.0f : 1.0f), 2.0f)) return true;
            else
            {
                if (unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    return false;

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange * (unitBase.Shield > 0.0f ? 2.0f : 1.0f), 2.0f));
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
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, unitBase.UnitStat.Range, 2.0f, _isContainBlockedTarget: true));

            foreach (UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);

            if(unitBase.Shield > 0.0f)
            {
                List<UnitEffectPersonalVariableInstance> listPersonal = unitBase.DicCCStatus.Values.ToList();

                for (int i = 0; i < listPersonal.Count; i++)
                {
                    if (listPersonal[i].Index != UNIT_EFFECT.BUFF_SHIELD)
                        continue;

                    listPersonal[i].Duration += (float)unitSkillData.Param[1];
                }
            }
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline(unitBase.Shield > 0.0f ? 1 : 0);
        }
    }
}
