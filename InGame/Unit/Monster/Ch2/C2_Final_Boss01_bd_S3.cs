using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Spine.Unity;
using UnityEngine.Playables;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C2_Final_Boss01_bd_S3", menuName = "UnitSkillEvent/Monster/C2_Final_Boss01_bd_S3")]
public class C2_Final_Boss01_bd_S3 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private GameObject objEnergy;
        private PlayableDirector pdEnergy;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 2)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Final_Boss01_bd_s3_3", 1.0f);

            if (MathLib.CheckPercentage((float)bossSkillData.Param[2]))
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
            TaskSkillDamage().Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill3_b,d");
            unitBase.PlayTimeline();

            UnitBase unitTarget = MgrBattleSystem.Instance.GetNearestXEnemyUnit(unitBase);
            if (unitTarget is null)
                unitTarget = unitBase;

            objEnergy = MgrObjectPool.Instance.ShowObj("FX_anubis_boss_skill3_Energy", unitTarget.transform.position + Vector3.right * 1.0f);
            pdEnergy = objEnergy.transform.GetChild(0).GetComponent<PlayableDirector>();
            pdEnergy.Play();

            TaskSkill().Forget();
        }

        private async UniTaskVoid TaskSkillDamage()
        {
            await UniTask.Delay(300, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            MgrCamera.Instance.SetCameraShake(0.3f, 1.0f, 30);

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, objEnergy.transform.position - Vector3.left, skillRange, (float)bossSkillData.Param[0]));

            foreach (UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 2);
        }

        private async UniTaskVoid TaskSkill()
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(pdEnergy.duration), cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            ResetSkill();
        }

        public override void ResetSkill()
        {
            if(!(objEnergy is null))
            {
                MgrObjectPool.Instance.HideObj("FX_anubis_boss_skill3_Energy", objEnergy);
                objEnergy = null;
                pdEnergy = null;
            }
        }
    }
}
