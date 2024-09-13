using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Normal_Mnstr01_ad_S1", menuName = "UnitSkillEvent/Monster/C4_Normal_Mnstr01_ad_S1")]
public class C4_Normal_Mnstr01_ad_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        _unitBase.Ska.skeleton.SetSkin(_unitBase.UnitIndex.Equals("C4_Normal_Mnstr01_d") ? "d" : "a");

        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].skillCooldown = 1.0f;
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 0)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Normal_Mnstr01_s1_2", 1.0f);
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            if (unitBase.EnemyTarget && MathLib.CheckIsInEllipse(unitBase.transform.position, unitBase.UnitStat.Range, unitBase.EnemyTarget.transform.position))
            {
                if(unitBase.EnemyTarget.UnitSetting.unitType == UnitType.AllyBase)
                {
                    UnitBase unitInEllipse = MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, skillRange);
                    if (unitInEllipse is not null)
                        unitBase.EnemyTarget = unitInEllipse;
                }
                return true;
            }
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
            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Normal_Mnstr01_s1_1", 1.0f);

            UnitBase unitTarget = unitBase.EnemyTarget;

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, unitBase.UnitStat.Range * 2.0f));

            if (listUnit.Count > 0)
            {
                listUnit.Shuffle();

                if (!unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    unitTarget = SetRandomTarget();
            }

            GameObject objBullet = MgrBulletPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C4_Normal_Mnstr01_d") ? "Bullet_C4_Normal_Mnstr_01_d_S1" : "Bullet_C4_Normal_Mnstr_01_a_S1", unitBase.GetUnitCenterPos());
            objBullet.GetComponent<Bullet>().SetBullet(unitBase, unitTarget);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline(unitBase.UnitIndex.Equals("C4_Normal_Mnstr01_d") ? 1 : 0);
        }

        private List<UnitBase> listWar = new List<UnitBase>();
        private List<UnitBase> listArch = new List<UnitBase>();
        private List<UnitBase> listTank = new List<UnitBase>();
        private List<UnitBase> listSup = new List<UnitBase>();
        private UnitBase SetRandomTarget()
        {
            UnitBase resultTarget = null;

            listWar.Clear();
            listArch.Clear();
            listTank.Clear();
            listSup.Clear();

            foreach (UnitBase unit in listUnit)
            {
                if (unit.UnitSetting.unitClass == UnitClass.Warrior) listWar.Add(unit);
                if (unit.UnitSetting.unitClass == UnitClass.Arch) listArch.Add(unit);
                if (unit.UnitSetting.unitClass == UnitClass.Tank) listTank.Add(unit);
                if (unit.UnitSetting.unitClass == UnitClass.Supporter) listSup.Add(unit);
            }

            float[] classPer = new float[4];
            classPer[0] = listWar.Count > 0 ? 20.0f : 0.0f; // 근
            classPer[1] = listArch.Count > 0 ? 15.0f : 0.0f; // 원
            classPer[2] = listTank.Count > 0 ? 50.0f : 0.0f; // 탱
            classPer[3] = listSup.Count > 0 ? 15.0f : 0.0f; // 섶

            float maxAmount = 0.0f;
            foreach (float value in classPer)
                maxAmount += value;

            float pivot = Random.value * maxAmount;

            int currIndex = -1;
            for (int i = 0; i < classPer.Length; i++)
            {
                if (pivot < classPer[i] && classPer[i] > 0.0f)
                {
                    currIndex = i;
                    break;
                }
                else
                {
                    pivot -= classPer[i];
                }
            }

            switch (currIndex)
            {
                case 0: resultTarget = listWar[0]; break;
                case 1: resultTarget = listArch[0]; break;
                case 2: resultTarget = listTank[0]; break;
                case 3: resultTarget = listSup[0]; break;
                default: resultTarget = MgrBattleSystem.Instance.GetAllyBase(); break;
            }

            return resultTarget;
        }
    }
}
