using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_CC_Ignite", menuName = "InGameEffect/CC/Ignite")]
public class SO_CC_Ignite : SOBase_UnitEffectEvent
{
    public override void OnInitialize(UnitBase _caster, UnitBase _victim, UNIT_EFFECT _index, float[] _value, bool _canRemove)
    {
        PersonalVariable personal = new PersonalVariable();
        personal.SetData(this, _caster, _victim, _index, _value, _canRemove);
        personal.OnStart();
    }

    private class PersonalVariable : UnitEffectPersonalVariableInstance
    {
        private float tickTimer;
        private GameObject objVFX;

        public override void OnStart()
        {
            Duration = EffectFloatValue[0];
            tickTimer = 0.0f;

            Victim.ListEffectPersonalVariable.Add(this);
            Victim.AddEffectUpdateEvent(OnUpdate);

            objVFX = MgrObjectPool.Instance.ShowObj("FX_CC_Ignite", Victim.transform.position);
            objVFX.transform.SetParent(Victim.transform);

            Victim.DicCCStatus.Add(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index), this);
        }

        public override void OnUpdate()
        {
            if(Duration > 0.0f)
            {
                tickTimer += Time.deltaTime;
                if(tickTimer >= 1.0f)
                {
                    tickTimer -= 1.0f;
                    MgrInGameEvent.Instance.BroadcastDamageEvent(Caster, Victim, Victim.UnitStat.MaxHP * 0.02f, 0.0f, 1.0f, 10);

                    MgrObjectPool.Instance.ShowObj("FX_CC_Ignite_hit", Victim.GetUnitCenterPos());
                }

                Duration -= Time.deltaTime;
                if(Duration <= 0.0f)
                    OnEnd();
            }
        }

        public override void OnEnd(bool _isForcedRemove = false)
        {
            MgrObjectPool.Instance.HideObj("FX_CC_Ignite", objVFX);
            objVFX = null;

            Victim.RemoveEffectUpdateEvent(OnUpdate);
            Victim.ListEffectPersonalVariable.Remove(this);
            Victim.DicCCStatus.Remove(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index));
        }
    }
}
