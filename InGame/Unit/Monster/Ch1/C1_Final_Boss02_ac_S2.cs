using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C1_Final_Boss02_ac_S2", menuName = "UnitSkillEvent/Monster/C1_Final_Boss02_ac_S2")]
public class C1_Final_Boss02_ac_S2 : SOBase_UnitSkillEvent
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

            MgrSound.Instance.PlayOneShotSFX("SFX_C1_Final_Boss02_ac_s3_3", 1.0f);

            _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[2]), _attacker, _victim, new float[] { (float)bossSkillData.Param[1] });
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
            MgrCamera.Instance.SetCameraShake(0.35f, 0.4f, 30);

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase));

            if (listUnit.Count > 3)
                listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());

            listUnit.Shuffle();

            int atkCnt = listUnit.Count > 3 ? 3 : listUnit.Count;
            for(int i = 0; i < atkCnt; i++)
            {
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, listUnit[i], unitBase.GetAtkRateToDamage((float)bossSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);

                MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C1_Final_Boss02_c") ? "FX_Ent_Boss_Root_C,D" : "FX_Ent_Boss_Root_A,B", listUnit[i].transform.position).transform.GetChild(0).GetComponent<SkeletonAnimation>().AnimationState.SetAnimation(0, "skill1", false);
            }
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2_a,c");
            unitBase.PlayTimeline();
        }
    }
}
