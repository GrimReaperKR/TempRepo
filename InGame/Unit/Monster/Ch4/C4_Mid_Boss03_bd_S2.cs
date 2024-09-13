using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Mid_Boss03_bd_S2", menuName = "UnitSkillEvent/Monster/C4_Mid_Boss03_bd_S2")]
public class C4_Mid_Boss03_bd_S2 : SOBase_UnitSkillEvent
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

            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Mid_Boss03_bd_s2_4", 1.0f);
            _victim.AddUnitEffect(UNIT_EFFECT.CC_KNOCKBACK, _attacker, _victim, new float[] { 4.0f });
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
            Vector3 v3Pos = unitBase.transform.position + ((float)bossSkillData.Param[1] * 0.5f * Vector3.left);
            v3Pos.y = -2.0f;
            v3Pos.z = v3Pos.y * 0.01f;
            GameObject objVFX = MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C4_Mid_Boss03_d") ? "FX_Ghost_Puppeteer_mid boss_d_skill2_Ghost" : "FX_Ghost_Puppeteer_mid boss_b_skill2_Ghost", v3Pos);
            objVFX.transform.GetChild(0).GetComponent<PlayableDirector>().Play();
            TaskVFXDestroy(objVFX).Forget();

            unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, true);

            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Mid_Boss03_bd_s2_3", 1.0f);

            TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2_b,d");
            unitBase.PlayTimeline(unitBase.UnitIndex.Equals("C4_Mid_Boss03_d") ? 1 : 0);

            unitBase.AddUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, unitBase, new float[] { (float)bossSkillData.Param[0], 0.0f }, false);
        }

        private async UniTaskVoid TaskSkill()
        {
            await UniTask.Delay(150, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, (float)bossSkillData.Param[1], 10.0f, _isContainBlockedTarget: true));
            foreach (UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[2]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase, _isAlly: true, _isContainBlockedTarget: true));

            if(listUnit.Count > 0)
                MgrSound.Instance.PlayOneShotSFX("SFX_Buff_ab", 1.0f);

            foreach (UnitBase unit in listUnit)
            {
                unit.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[5]), unitBase, unit, new float[] { (float)bossSkillData.Param[4], (float)bossSkillData.Param[3] });
                MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", unit.transform.position).transform.SetParent(unit.transform);
            }
        }

        private async UniTaskVoid TaskVFXDestroy(GameObject _objVFX)
        {
            await UniTask.Delay(1500, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C4_Mid_Boss03_d") ? "FX_Ghost_Puppeteer_mid boss_d_skill2_Ghost" : "FX_Ghost_Puppeteer_mid boss_b_skill2_Ghost", _objVFX);
        }

        public override void ResetSkill()
        {
            unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, true);
        }
    }
}
