using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Debuff_Weak", menuName = "InGameEffect/Debuff/Weak")]
public class SO_Debuff_Weak : SOBase_UnitEffectEvent
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
            Duration = EffectFloatValue[1];

            Victim.ListEffectPersonalVariable.Add(this);
            Victim.AddEffectUpdateEvent(OnUpdate);

            Victim.AddUnitEffectVFX(Index, "FX_Debuff_Weaken", Victim.transform.position);

            Victim.DicCCStatus.Add(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index), this);

            MgrSound.Instance.PlayOneShotSFX("SFX_Debuff_Weak", 0.33f);
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

            Victim.RemoveUnitEffectVFX(Index, "FX_Debuff_Weaken");
        }

        public override float GetUnitEffectValue() => EffectFloatValue[0];
    }
}
