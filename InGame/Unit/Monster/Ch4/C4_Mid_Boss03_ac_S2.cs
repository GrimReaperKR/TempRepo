using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Mid_Boss03_ac_S2", menuName = "UnitSkillEvent/Monster/C4_Mid_Boss03_ac_S2")]
public class C4_Mid_Boss03_ac_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnitTarget = new List<UnitBase>();
        private List<UnitBase> listUnitAlly = new List<UnitBase>();

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Mid_Boss03_ac_s2_3", 1.0f);
            _victim.AddUnitEffect(UNIT_EFFECT.CC_KNOCKBACK, _attacker, _victim, new float[] { (float)bossSkillData.Param[2] });
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            return true;
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
            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Mid_Boss03_ac_s2_2", 1.0f);

            Vector3 v3GrdPos = unitBase.transform.position;
            v3GrdPos.y = -2.0f;
            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C4_Mid_Boss03_c") ? "FX_Ghost_Puppeteer_mid boss_c_skill2_GRD" : "FX_Ghost_Puppeteer_mid boss_a_skill2_GRD", v3GrdPos);

            listUnitTarget.Clear();
            listUnitTarget.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, (float)bossSkillData.Param[0], 10.0f, _isContainBlockedTarget: true));

            listUnitAlly.Clear();
            listUnitAlly.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase, _isAlly: true, _isContainBlockedTarget: true));

            TaskGhost().Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2_a,c");
            unitBase.PlayTimeline(unitBase.UnitIndex.Equals("C4_Mid_Boss03_c") ? 1 : 0);
        }

        private async UniTaskVoid TaskGhost()
        {
            Vector3 v3StartGhostPos = unitBase.transform.position;
            v3StartGhostPos.x += 5.0f;
            v3StartGhostPos.y = -2.0f;
            GameObject objGhost = MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C4_Mid_Boss03_c") ? "FX_Ghost_Puppeteer_mid boss_c_skill2_Ghost" : "FX_Ghost_Puppeteer_mid boss_a_skill2_Ghost", v3StartGhostPos);
            objGhost.transform.DOMoveX(v3StartGhostPos.x - 30.0f, 1.0f).SetEase(Ease.Linear);

            float duration = 1.0f;
            Vector3 v3PrevPos = objGhost.transform.position + Vector3.right * 30.0f;

            if(listUnitAlly.Count > 0)
                MgrSound.Instance.PlayOneShotSFX("SFX_Buff_ab", 1.0f);

            while (duration > 0.0f)
            {
                duration -= Time.deltaTime;

                for (int i = listUnitAlly.Count - 1; i >= 0; i--)
                {
                    if (objGhost.transform.position.x <= listUnitAlly[i].transform.position.x && listUnitAlly[i].transform.position.x < v3PrevPos.x && !listUnitAlly[i].CheckIsState(UNIT_STATE.DEATH))
                    {
                        listUnitAlly[i].AddUnitEffect(UNIT_EFFECT.BUFF_ATK, unitBase, listUnitAlly[i], new float[] { (float)bossSkillData.Param[4], (float)bossSkillData.Param[3] });
                        listUnitAlly[i].AddUnitEffect(UNIT_EFFECT.BUFF_MOVE_SPEED, unitBase, listUnitAlly[i], new float[] { (float)bossSkillData.Param[4], (float)bossSkillData.Param[3] });
                        MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", listUnitAlly[i].transform.position).transform.SetParent(listUnitAlly[i].transform);
                        listUnitAlly.RemoveAt(i);
                    }
                }

                for (int i = listUnitTarget.Count - 1; i >= 0; i--)
                {
                    if (objGhost.transform.position.x <= listUnitTarget[i].transform.position.x && listUnitTarget[i].transform.position.x < v3PrevPos.x && !listUnitTarget[i].CheckIsState(UNIT_STATE.DEATH))
                    {
                        MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, listUnitTarget[i], unitBase.GetAtkRateToDamage((float)bossSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
                        MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C4_Mid_Boss03_c") ? "FX_Ghost_Puppeteer_mid boss_c_skill2_hit" : "FX_Ghost_Puppeteer_mid boss_a_skill2_hit", listUnitTarget[i].GetUnitCenterPos()).transform.SetParent(listUnitTarget[i].transform);
                        listUnitTarget.RemoveAt(i);
                    }
                }
                
                v3PrevPos = objGhost.transform.position;
                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());
            }

            //MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C4_Mid_Boss03_c") ? "FX_Ghost_Puppeteer_mid boss_c_skill2_Ghost" : "FX_Ghost_Puppeteer_mid boss_a_skill2_Ghost", objGhost);
        }
    }
}
