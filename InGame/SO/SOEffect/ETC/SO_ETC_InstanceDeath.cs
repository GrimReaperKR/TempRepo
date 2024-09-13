using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_ETC_InstanceDeath", menuName = "InGameEffect/Etc/InstanceDeath")]
public class SO_ETC_InstanceDeath : SOBase_UnitEffectEvent
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
            if (Victim.CheckHasUnitEffect(UNIT_EFFECT.ETC_GOD))
                return;

            MgrObjectPool.Instance.ShowObj("FX_Instant-Death", Victim.transform.position);
            Victim.UnitStat.HP = 0.0f;
            Victim.DoHPBarEffect();
            Victim.OnDefaultDeath(Caster, Victim, -1);

            MgrSound.Instance.PlayOneShotSFX("SFX_Effect_Death_ab", 1.0f);
        }

        public override void OnUpdate()
        {

        }

        public override void OnEnd(bool _isForcedRemove = false)
        {

        }
    }
}
