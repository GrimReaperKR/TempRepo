using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C1_Mid_Boss00_bd_S2", menuName = "UnitSkillEvent/Monster/C1_Mid_Boss00_bd_S2")]
public class C1_Mid_Boss00_bd_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        private GameObject objVFX;

        private int atkCnt = 0;
        private int atkMaxCnt = 0;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C1_Mid_Boss00_bd_s2_3", 1.0f);

            _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[2]), _attacker, _victim, new float[] { (float)bossSkillData.Param[1] });
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

            if(_animationName.Equals("skill2_b,d_1") || _animationName.Equals("skill2_b,d_2"))
            {
                if (atkCnt < atkMaxCnt)
                {
                    unitBase.Ska.transform.position = listUnit[atkCnt].transform.position + Vector3.right * 4.0f;

                    if (objVFX != null)
                        objVFX.transform.localPosition = unitBase.Ska.transform.localPosition + new Vector3(2.019f, 1.118f, 0);

                    unitBase.SetUnitAnimation("skill2_b,d_2");
                    unitBase.PlayTimeline();
                }
                else
                {
                    unitBase.Ska.transform.localPosition = Vector3.zero;

                    if (objVFX != null)
                        objVFX.transform.localPosition = new Vector3(2.019f, 1.118f, 0);

                    unitBase.SetUnitAnimation("skill2_b,d_3");
                }
            }
            else if(_animationName.Equals("skill2_b,d_3"))
            {
                unitBase.RemoveUnitEffect(UNIT_EFFECT.ETC_GOD, unitBase, true);

                unitBase.SetUnitState(UNIT_STATE.IDLE);
                unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);
            }
        }

        public override void EventTriggerSkill()
        {
            MgrCamera.Instance.SetCameraShake(0.35f, 0.4f, 30);

            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C1_Mid_Boss00_d") ? "FX_monster rat_mid boss_d_skill-hit" : "FX_monster rat_mid boss_b_skill-hit", listUnit[atkCnt].GetUnitCenterPos());
            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, listUnit[atkCnt], unitBase.GetAtkRateToDamage((float)bossSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);

            atkCnt++;
        }

        public override void OnSkill()
        {
            if (objVFX is null)
                objVFX = unitBase.transform.Find(unitBase.UnitIndex.Equals("C1_Mid_Boss00_d") ? "FX_monster rat_mid boss_d_skill2_cast(Clone)" : "FX_monster rat_mid boss_b_skill2_cast(Clone)").gameObject;

            unitBase.AddUnitEffect(UNIT_EFFECT.ETC_GOD, unitBase, unitBase, new float[] { 0.0f }, false);

            unitBase.SetUnitAnimation("skill2_b,d_1");

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase));

            if (listUnit.Count < 3 && MgrBattleSystem.Instance.GetAllyBase() is not null)
                listUnit.Add(MgrBattleSystem.Instance.GetAllyBase());

            listUnit.Shuffle();

            atkCnt = 0;
            atkMaxCnt = listUnit.Count > 3 ? 3 : listUnit.Count;

            MgrSound.Instance.PlayOneShotSFX("SFX_C1_Mid_Boss00_bd_s2_1", 1.0f);
        }

        public override void ResetSkill()
        {
            unitBase.Ska.transform.localPosition = Vector3.zero;
            unitBase.RemoveUnitEffect(UNIT_EFFECT.ETC_GOD, unitBase, true);

            if(objVFX != null)
                objVFX.transform.localPosition = new Vector3(2.019f, 1.118f, 0);
        }
    }
}
