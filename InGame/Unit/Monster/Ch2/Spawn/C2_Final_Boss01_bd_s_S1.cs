using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C2_Final_Boss01_bd_s_S1", menuName = "UnitSkillEvent/Monster/C2_Final_Boss01_bd_s_S1")]
public class C2_Final_Boss01_bd_s_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        _unitBase.UnitStat.MaxHP = _unitBase.UnitBaseParent.UnitStat.MaxHP * _unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(1, "param.1");
        _unitBase.UnitStat.HP = _unitBase.UnitStat.MaxHP;
        _unitBase.UnitStat.MoveSpeed = 0.0f;

        _unitBase.Ska.skeleton.SetSkin(_unitBase.UnitIndex.Equals("C2_Final_Boss01_d_s") ? "d" : "b");

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.Ska.AnimationState.Complete -= personal.OnComplete;
        _unitBase.Ska.AnimationState.Complete += personal.OnComplete;

        //_unitBase.IsStackPosition = true;
        if (MgrBattleSystem.Instance.IsBossAngry)
            MgrBattleSystem.Instance.SetUnitAngry(_unitBase);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Final_Boss01_bd_s2_Turret_c", 0.5f);
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL))
                return true;

            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill"))
                return;

            unitBase.SetUnitState(UNIT_STATE.IDLE);
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(1, "param.2"));
        }

        public override void EventTriggerSkill()
        {
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, unitBase.UnitBaseParent.UnitSkillPersonalVariable[1].skillRange, (float)unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(1, "param.0")));

            foreach(UnitBase unit in listUnit)
            {
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(1, "param.3")), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
                MgrObjectPool.Instance.ShowObj("FX_anubis_boss_skill2_turret_Skill_hit", unit.GetUnitCenterPos());
            }
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();
        }

        public void OnComplete(Spine.TrackEntry trackEntry)
        {
            string animationName = trackEntry.Animation.Name;

            if (animationName.Equals("summon"))
            {
                unitBase.SetUnitState(UNIT_STATE.IDLE, true);

                if (unitBase.UnitBaseParent.CheckIsState(UNIT_STATE.DEATH))
                    unitBase.OnDefaultDeath(MgrBattleSystem.Instance.GetAllyBase(), unitBase, -1);

                unitBase.Ska.AnimationState.Complete -= OnComplete;
            }
        }
    }
}
