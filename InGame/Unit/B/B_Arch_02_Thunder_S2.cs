using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_B_Arch_02_Thunder_S2", menuName = "UnitSkillEvent/B_Arch_02_Thunder_S2")]
public class B_Arch_02_Thunder_S2 : SOBase_UnitSkillEvent
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
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_B_Arch_02_s2_4", 1.0f);

            if (MathLib.CheckPercentage((float)unitSkillData.Param[3]))
                _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitSkillData.Param[4]), _attacker, _victim, new float[] { (float)unitSkillData.Param[5] });
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
            UnitBase target = listUnit.Count > 0 ? listUnit[0] : unitBase.EnemyTarget;

            GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_B_Arch_02_S2", unitBase.GetUnitCenterPos());
            objBullet.GetComponent<Bullet>().SetBullet(unitBase, target, target.transform.position + Vector3.up * 6.0f);
        }

        public override void OnSkill()
        {
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, skillRange * 2.0f));
            listUnit.Sort((a, b) => (a.UnitStat.HP / a.UnitStat.MaxHP).CompareTo(b.UnitStat.HP / b.UnitStat.MaxHP));

            if (listUnit.Count > 1)
                listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());

            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }
    }
}
