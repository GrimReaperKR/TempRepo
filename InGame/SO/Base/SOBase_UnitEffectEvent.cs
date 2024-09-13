using UnityEngine;

public abstract class SOBase_UnitEffectEvent : ScriptableObject
{
    public abstract void OnInitialize(UnitBase _caster, UnitBase _victim, UNIT_EFFECT _index, float[] _value = null, bool _canRemove = true);
}

[System.Serializable]
public class UnitEffect_Status
{
    public string CasterIndex;
    public UnitBase Victim;
    public UNIT_EFFECT EffectIndexNum;

    public UnitEffect_Status(string _casterIndex, UnitBase _victim, UNIT_EFFECT _index)
    {
        CasterIndex = _casterIndex;
        Victim = _victim;
        EffectIndexNum = _index;
    }

    public override int GetHashCode()
    {
        return CasterIndex.GetHashCode() + Victim.GetHashCode() + EffectIndexNum.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        UnitEffect_Status effectStatus = obj as UnitEffect_Status;
        return effectStatus != null && (effectStatus.CasterIndex == this.CasterIndex && effectStatus.Victim == this.Victim && effectStatus.EffectIndexNum == this.EffectIndexNum);
    }
}