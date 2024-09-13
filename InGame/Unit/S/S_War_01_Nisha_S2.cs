using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity.Examples;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_S_War_01_Nisha_S2", menuName = "UnitSkillEvent/S_War_01_Nisha_S2")]
public class S_War_01_Nisha_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        personal.Init();
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private int atkCnt;
        private SkeletonGhost skag;

        public void Init()
        {
            if(!unitBase.Ska.TryGetComponent(out skag))
            {
                skag = unitBase.Ska.gameObject.AddComponent<SkeletonGhost>();
                skag.color = new Color(0.172549f, 0.1490196f, 0.2941177f, 0.4f);
                skag.additive = true;
                skag.spawnInterval = 0.01f;
                skag.maximumGhosts = 8;
                skag.fadeSpeed = 0.1f;
                skag.textureFade = 0.5f;
                skag.sortWithDistanceOnly = true;
            }
            skag.ghostingEnabled = false;
        }

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_S_War_01_s2_3", 1.0f);
            unitBase.SetHeal(_damage * (float)unitSkillData.Param[3], false);
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

            unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, true);
            unitBase.SetUnitState(UNIT_STATE.IDLE);
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);
            skag.ghostingEnabled = false;
        }

        public override void EventTriggerSkill()
        {
            atkCnt++;
            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unitBase.EnemyTarget, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);

            if(atkCnt == 1)
            {
                MgrObjectPool.Instance.ShowObj(unitBase.UnitLvData.promotion >= 5 ? "FX_S_War_01_Nisha_skill2_2_hit" : "FX_S_War_01_Nisha_skill2_1_hit", unitBase.EnemyTarget.GetUnitCenterPos());
                skag.fadeSpeed = 10.0f;
            }

            if(atkCnt == (int)unitSkillData.Param[2])
                MgrObjectPool.Instance.ShowObj("FX_PC-Heal", unitBase.GetUnitCenterPos());
        }

        public override void OnSkill()
        {
            unitBase.AddUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, unitBase, new float[] { (float)unitSkillData.Param[0], 0.0f }, false);

            atkCnt = 0;
            unitBase.SetUnitAnimation(unitBase.UnitLvData.promotion >= 5 ? "skill2_lv5" : "skill2");
            unitBase.PlayTimeline(unitBase.UnitLvData.promotion >= 5 ? 1 : 0);
            skag.fadeSpeed = 0.1f;
            skag.ghostingEnabled = true;
        }

        public override void ResetSkill()
        {
            unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, true);
            skag.ghostingEnabled = false;
            skag.CleanUp();
        }
    }
}
