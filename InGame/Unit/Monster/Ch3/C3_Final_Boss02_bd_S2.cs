using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C3_Final_Boss02_bd_S2", menuName = "UnitSkillEvent/Monster/C3_Final_Boss02_bd_S2")]
public class C3_Final_Boss02_bd_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private int atkCnt;
        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C3_Final_Boss02_bd_s2_4", 1.0f);

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
            atkCnt++;

            listUnit.Clear();

            if (atkCnt == 1)
            {
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position + Vector3.up * 1.75f, 4.2f, 10.0f));

                foreach (UnitBase unit in listUnit)
                    unit.AddUnitEffect(UNIT_EFFECT.CC_KNOCKBACK, unitBase, unit, new float[] { 3.0f });

                TaskSkill().Forget();
            }
            else
            {
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, (float)bossSkillData.Param[0], 10.0f));

                foreach (UnitBase unit in listUnit)
                    MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);

                MgrCamera.Instance.SetCameraShake(0.3f, 0.5f, 30);
            }
        }

        public override void OnSkill()
        {
            atkCnt = 0;

            unitBase.SetUnitAnimation("skill2_b,d");
            unitBase.PlayTimeline();
        }

        public override void ResetSkill()
        {
            atkCnt = 0;
        }

        private async UniTaskVoid TaskSkill()
        {
            Vector3 v3StartPos = unitBase.transform.position;
            Vector3 v3EndPos = MgrBattleSystem.Instance.GetNearestXEnemyUnit(unitBase).transform.position;
            v3EndPos.x = (unitBase.transform.position.x - v3EndPos.x) < 3.0f ? v3EndPos.x + 3.0f : unitBase.transform.position.x;
            v3EndPos.y = -2.0f;
            v3EndPos.z = v3EndPos.y * 0.01f;

            float duration = 2.0f;
            while(duration > 0.0f && atkCnt == 1)
            {
                duration -= Time.deltaTime;
                unitBase.transform.position = Vector3.Lerp(v3StartPos, v3EndPos, (2.0f - duration) / 2.0f);

                await UniTask.Yield(cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            }
        }
    }
}
