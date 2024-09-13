using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Debuff_FrostBite", menuName = "InGameEffect/Debuff/FrostBite")]
public class SO_Debuff_FrostBite : SOBase_UnitEffectEvent
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
            if(Victim.CheckHasUnitEffect(UNIT_EFFECT.DEBUFF_FROSTBITE))
            {
                Victim.RemoveUnitEffect(UNIT_EFFECT.DEBUFF_FROSTBITE, _isForceRemove: true, _isForceWorldRemove: true);
                Victim.AddUnitEffect(UNIT_EFFECT.CC_FREEZE, Caster, Victim, new float[] { 3.0f });
            }
            else
            {
                Duration = EffectFloatValue[1];

                Victim.ListEffectPersonalVariable.Add(this);
                Victim.AddEffectUpdateEvent(OnUpdate);

                for (int i = 0; i < Victim.UnitSkillPersonalVariable.Length; i++)
                    Victim.SetUnitSkillCoolDown(i, Victim.GetUnitSkillCoolDown(i) + Victim.UnitSkillPersonalVariable[i].skillCooldown * 0.2f);

                Victim.UpdateSpineUnitSpineSpeed();
                Victim.UpdateSpineTint();

                Victim.DicCCStatus.Add(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index), this);
            }
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

            Victim.UpdateSpineUnitSpineSpeed();
            Victim.UpdateSpineTint();
        }

        public override float GetUnitEffectValue() => EffectFloatValue[0];
    }
}
