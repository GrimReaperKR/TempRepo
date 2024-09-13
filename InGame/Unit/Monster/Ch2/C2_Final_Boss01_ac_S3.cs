using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C2_Final_Boss01_ac_S3", menuName = "UnitSkillEvent/Monster/C2_Final_Boss01_ac_S3")]
public class C2_Final_Boss01_ac_S3 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private Vector3 v3Pos;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 2)
                return;

            if(MathLib.CheckPercentage((float)bossSkillData.Param[2]))
                _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[4]), _attacker, _victim, new float[] { (float)bossSkillData.Param[3] });
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
            UnitBase target = MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, skillRange);
            if (target is not null)
                v3Pos = target.transform.position;

            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_c") ? "FX_Anubis_Boss_c_skill3_meteor" : "FX_Anubis_Boss_a_skill3_meteor", v3Pos);
            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Final_Boss01_ac_s3_2", 1.0f);

            TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill3_a,c");
            unitBase.PlayTimeline();

            UnitBase target = MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, skillRange);
            if (target is null)
                return;

            v3Pos = target.transform.position;
        }

        private async UniTaskVoid TaskSkill()
        {
            await UniTask.Delay(300, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, v3Pos, (float)bossSkillData.Param[0]));

            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Final_Boss01_ac_s3_3", 1.0f);

            foreach (UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 2);

            MgrCamera.Instance.SetCameraShake(0.3f, 1.0f, 30);
        }
    }
}
