using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_B_Tank_02_Golem_S2", menuName = "UnitSkillEvent/B_Tank_02_Golem_S2")]
public class B_Tank_02_Golem_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetDeathEvent(personal.OnEventDeath);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private bool isRevived;

        public void OnEventDeath(UnitBase _attacker, UnitBase _victim, int _dmgChannel)
        {
            if (isRevived)
            {
                unitBase.OnDefaultDeath(_attacker, _victim, _dmgChannel);
            }
            else
            {
                unitBase.UnitPlayableDirector.Stop();
                unitBase.UnitPlayableDirector.playableAsset = null;
                unitBase.SetUnitUseSkill(unitBase.soSkillEvent[1]);
                isRevived = true;
            }
        }

        public override bool CheckCanUseSkill()
        {
            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill"))
                return;

            unitBase.RemoveUnitEffect(UNIT_EFFECT.ETC_REVIVE, unitBase);
            unitBase.SetUnitState(UNIT_STATE.IDLE);
        }

        public override void EventTriggerSkill()
        {

        }

        public override void OnSkill()
        {
            for (int i = unitBase.ListEffectPersonalVariable.Count - 1; i >= 0; i--)
                unitBase.ListEffectPersonalVariable[i].OnEnd();

            unitBase.AddUnitEffect(UNIT_EFFECT.ETC_REVIVE, unitBase, unitBase, new float[] { 0.0f });

            unitBase.SetUnitState(UNIT_STATE.DEATH, true, true);
            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
            TaskUnitRevive().Forget();
        }

        private async UniTaskVoid TaskUnitRevive()
        {
            await UniTask.Delay(800, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            unitBase.SetUnitState(UNIT_STATE.SKILL, true);
            unitBase.UnitStat.HP = unitBase.UnitStat.MaxHP * (float)unitSkillData.Param[0];
            MgrObjectPool.Instance.ShowObj("FX_Resurrection", unitBase.GetUnitCenterPos());
        }
    }
}
