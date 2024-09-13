using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_S_Spt_01_Garam_S1", menuName = "UnitSkillEvent/S_Spt_01_Garam_S1")]
public class S_Spt_01_Garam_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new GaramPersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    public class GaramPersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        public void OnDamagedChain(UnitBase _attacker, UnitBase _victim, float _damage)
        {
            foreach(UnitBase unit in unitBase.ListGaramTarget)
            {
                if (unit == _victim)
                    continue;

                MgrObjectPool.Instance.ShowObj("FX_S_Spt_01_Garam_skill1_mark-hit", unit.GetUnitCenterPos());
                MgrInGameEvent.Instance.BroadcastDamageEvent(_attacker, unit, _damage, 0.0f, 1.0f, -1);
            }
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL) && MgrBattleSystem.Instance.CheckCanAddGaramEventUnitInEllipse(unitBase, unitBase.transform.position, skillRange))
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
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitCanAddGaramEventInEllipse(unitBase, unitBase.transform.position, skillRange, 3));

            if (listUnit.Count > 0)
                MgrSound.Instance.PlayOneShotSFX("SFX_S_Spt_01_s1_2", 1.0f);

            foreach(UnitBase unit in listUnit)
            {
                GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_S_Spt_01_S1", unitBase.GetUnitCenterPos());
                objBullet.GetComponent<Bullet>().SetBullet(unitBase, unit);
            }
        }

        public override void OnSkill()
        {
            unitBase.ListGaramTarget.Clear();

            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();
        }
    }
}
