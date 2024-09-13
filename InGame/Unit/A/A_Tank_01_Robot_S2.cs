using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.VisualScripting;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_A_Tank_01_Robot_S2", menuName = "UnitSkillEvent/A_Tank_01_Robot_S2")]
public class A_Tank_01_Robot_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetDeathEvent(personal.OnDeath);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_A_Tank_01_s2_3", 1.0f);

            if (MathLib.CheckPercentage((float)unitSkillData.Param[2]))
                _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitSkillData.Param[3]), _attacker, _victim, new float[] { (float)unitSkillData.Param[4] });
        }

        public override bool CheckCanUseSkill()
        {
            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill2"))
                return;

            TaskDeathDelay().Forget();
        }

        public override void EventTriggerSkill()
        {
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, (float)unitSkillData.Param[0], _isContainBlockedTarget: true));

            MgrObjectPool.Instance.ShowObj("FX_A_Robot_Explosion", unitBase.transform.position + Vector3.up);
            MgrCamera.Instance.SetCameraShake(1.0f, 0.5f, 50);

            foreach(UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
        }

        public override void OnSkill()
        {
            for (int i = unitBase.ListEffectPersonalVariable.Count - 1; i >= 0; i--)
                unitBase.ListEffectPersonalVariable[i].OnEnd();

            unitBase.SetUnitState(UNIT_STATE.DEATH, true, true);
            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }

        public void OnDeath(UnitBase _attacker, UnitBase _victim, int _dmgChannel)
        {
            _attacker.OnKillEvent(unitBase, _dmgChannel);

            unitBase.UnitPlayableDirector.Stop();
            unitBase.UnitPlayableDirector.playableAsset = null;
            unitBase.SetUnitUseSkill(soSkillEvent);
        }

        private async UniTaskVoid TaskDeathDelay()
        {
            await UniTask.Delay(1000, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            unitBase.OnDefaultDeath(unitBase, unitBase, -1);
        }
    }
}
