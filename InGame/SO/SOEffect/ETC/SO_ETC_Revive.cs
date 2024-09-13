using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_ETC_Revive", menuName = "InGameEffect/Etc/Revive")]
public class SO_ETC_Revive : SOBase_UnitEffectEvent
{
    public override void OnInitialize(UnitBase _caster, UnitBase _victim, UNIT_EFFECT _index, float[] _value, bool _canRemove)
    {
        PersonalVariable personal = new PersonalVariable();
        personal.SetData(this, _caster, _victim, _index, _value, _canRemove);
        personal.OnStart();
    }

    private class PersonalVariable : UnitEffectPersonalVariableInstance
    {
        public override void OnStart()
        {
            Duration = EffectFloatValue[0];

            Victim.ListEffectPersonalVariable.Add(this);
            Victim.AddEffectUpdateEvent(OnUpdate);

            Victim.DicCCStatus.Add(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index), this);
        }

        public override void OnUpdate()
        {
            if(Duration > 0.0f)
            {
                Duration -= Time.deltaTime;
                if(Duration <= 0.0f)
                    OnEnd();
            }
        }

        public override void OnEnd(bool _isForcedRemove = false)
        {
            Victim.RemoveEffectUpdateEvent(OnUpdate);
            Victim.ListEffectPersonalVariable.Remove(this);
            Victim.DicCCStatus.Remove(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index));
        }
    }
}
