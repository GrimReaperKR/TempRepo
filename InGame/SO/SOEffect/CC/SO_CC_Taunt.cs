using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_CC_Taunt", menuName = "InGameEffect/CC/Taunt")]
public class SO_CC_Taunt : SOBase_UnitEffectEvent
{
    public override void OnInitialize(UnitBase _caster, UnitBase _victim, UNIT_EFFECT _index, float[] _value, bool _canRemove)
    {
        PersonalVariable personal = new PersonalVariable();
        personal.SetData(this, _caster, _victim, _index, _value, _canRemove);
        personal.OnStart();
    }

    private class PersonalVariable : UnitEffectPersonalVariableInstance
    {
        private GameObject objVFX;

        public override void OnStart()
        {
            Duration = EffectFloatValue[0];
            if (MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp) Duration *= 0.5f;

            Victim.EnemyTarget = Caster;

            Victim.ListEffectPersonalVariable.Add(this);
            Victim.AddEffectUpdateEvent(OnUpdate);

            Victim.ChangeForceToIdle();

            objVFX = MgrObjectPool.Instance.ShowObj("FX_CC_Taunt", Victim.GetUnitCenterPos());
            objVFX.transform.SetParent(Victim.transform);

            Victim.DicCCStatus.Add(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index), this);
        }

        public override void OnUpdate()
        {
            if(Duration > 0.0f)
            {
                if (Caster.CheckIsState(UNIT_STATE.DEATH))
                    OnEnd();

                if(!Victim.CheckIsState(UNIT_STATE.SKILL) && !Victim.CheckIsState(UNIT_STATE.DEATH) && Victim.UnitStat.MoveSpeed > 0.0f)
                {
                    if(!MathLib.CheckIsInEllipse(Victim.transform.position, Victim.UnitStat.Range, Caster.transform.position))
                    {
                        if (!Victim.Ska.AnimationName.Equals(Victim.animMoveName))
                            Victim.SetUnitAnimation(Victim.animMoveName, true);

                        Victim.UnitSetting.moveSO.Move(Victim);
                    }
                    else
                    {
                        if (!Victim.Ska.AnimationName.Equals(Victim.animIdleName))
                            Victim.SetUnitAnimation(Victim.animIdleName, true);
                    }
                }

                Duration -= Time.deltaTime;
                if(Duration <= 0.0f)
                    OnEnd();
            }
        }

        public override void OnEnd(bool _isForcedRemove = false)
        {
            MgrObjectPool.Instance.HideObj("FX_CC_Taunt", objVFX);
            objVFX = null;

            Victim.RemoveEffectUpdateEvent(OnUpdate);
            Victim.ListEffectPersonalVariable.Remove(this);
            Victim.DicCCStatus.Remove(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index));
        }
    }
}
