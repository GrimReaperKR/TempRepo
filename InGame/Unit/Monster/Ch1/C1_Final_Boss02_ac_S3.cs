using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C1_Final_Boss02_ac_S3", menuName = "UnitSkillEvent/Monster/C1_Final_Boss02_ac_S3")]
public class C1_Final_Boss02_ac_S3 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 2)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C1_Final_Boss02_ac_s2_3", 1.0f);

            _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[3]), _attacker, _victim, new float[] { (float)bossSkillData.Param[2] });
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
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, (float)bossSkillData.Param[0]));

            foreach(UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 2);

            MgrCamera.Instance.SetCameraShake(0.3f, 1.0f, 30);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill3_a,c");
            unitBase.PlayTimeline();
        }
    }
}
