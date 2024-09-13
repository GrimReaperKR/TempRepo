using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C3_Mid_Boss00_bd_S2", menuName = "UnitSkillEvent/Monster/C3_Mid_Boss00_bd_S2")]
public class C3_Mid_Boss00_bd_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private bool isJumpEnd = false;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C3_Mid_Boss00_bd_s2_3", 1.0f);

            if (MathLib.CheckPercentage((float)bossSkillData.Param[2]))
                _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[4]), _attacker, _victim, new float[] { 0.2f, (float)bossSkillData.Param[3] });
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
            isJumpEnd = true;

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, (float)bossSkillData.Param[0]));

            foreach (UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);

            MgrCamera.Instance.SetCameraShake(0.3f, 0.5f, 30);
        }

        public override void OnSkill()
        {
            isJumpEnd = false;

            unitBase.SetUnitAnimation("skill2_b,d");
            unitBase.PlayTimeline();

            TaskSkill().Forget();
        }

        private async UniTaskVoid TaskSkill()
        {
            await UniTask.Delay(200, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            while(!isJumpEnd)
            {
                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());

                UnitBase target = MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, skillRange);
                if(!(target is null))
                    unitBase.transform.position = Vector3.Lerp(unitBase.transform.position, target.transform.position + Vector3.right * 2.0f, Time.deltaTime * 2.5f);
            }
        }
    }
}
