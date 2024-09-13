using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C2_Mid_Boss01_bd_S2", menuName = "UnitSkillEvent/Monster/C2_Mid_Boss01_bd_S2")]
public class C2_Mid_Boss01_bd_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private Vector3 v3Pos;

        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Mid_Boss01_bd_s2_3", 1.0f);

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
            UnitBase target = MgrBattleSystem.Instance.GetNearestXEnemyUnit(unitBase);
            if (target is null)
                return;

            v3Pos = target.transform.position;
            v3Pos.y = -2.0f;
            
            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Mid_Boss01_d") ? "FX_Sphinx_mid boss_d_skill2_zone" : "FX_Sphinx_mid boss_b_skill2_zone", v3Pos);

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, v3Pos, skillRange, 8.0f));

            foreach (UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);

            TaskBackStep().Forget();

            MgrCamera.Instance.SetCameraShake(0.35f, 0.4f, 30);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2_b,d");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskBackStep()
        {
            float duration = 1.2f;
            Vector3 v3EndPos = unitBase.transform.position + (unitBase.GetUnitLookDirection(true)) * 4.0f;

            while (duration > 0.0f)
            {
                if (unitBase.CheckHasBlockedMoveCC() || unitBase.CheckIsState(UNIT_STATE.DEATH))
                    break;

                duration -= Time.deltaTime;

                unitBase.transform.position = Vector3.Lerp(unitBase.transform.position, v3EndPos, (1.2f - duration) / 1.2f);

                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());
            }
        }
    }
}
