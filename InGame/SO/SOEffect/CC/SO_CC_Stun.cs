using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_CC_Stun", menuName = "InGameEffect/CC/Stun")]
public class SO_CC_Stun : SOBase_UnitEffectEvent
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

            Victim.ListEffectPersonalVariable.Add(this);
            Victim.AddEffectUpdateEvent(OnUpdate);

            Victim.ChangeForceToIdle();

            objVFX = MgrObjectPool.Instance.ShowObj("FX_CC_Stun", Victim.transform.position + (Victim.GetUnitHeight() - 0.35f) * Vector3.up);
            objVFX.transform.SetParent(Victim.transform);

            Victim.DicCCStatus.Add(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index), this);

            MgrSound.Instance.PlayOneShotSFX("SFX_CC_Stern", 0.5f);
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
            MgrObjectPool.Instance.HideObj("FX_CC_Stun", objVFX);
            objVFX = null;

            Victim.RemoveEffectUpdateEvent(OnUpdate);
            Victim.ListEffectPersonalVariable.Remove(this);
            Victim.DicCCStatus.Remove(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index));
        }
    }
}
