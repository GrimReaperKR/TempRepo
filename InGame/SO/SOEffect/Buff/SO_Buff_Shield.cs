using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Buff_Shield", menuName = "InGameEffect/Buff/Shield")]
public class SO_Buff_Shield : SOBase_UnitEffectEvent
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

            Victim.Shield = EffectFloatValue[0];

            Victim.DicCCStatus.Add(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index), this);

            if (Victim.UnitSetting.unitType == UnitType.AllyBase)
            {
                MgrBattleSystem.Instance.SetAllyHPBar(Victim, Victim.UnitStat.HP, Victim.UnitStat.MaxHP, Victim.Shield);
                Victim.AddUnitEffectVFX(Index, "FX_Ally base_Shield", Victim.transform.position);
            }
            else if (Victim.UnitSetting.unitType == UnitType.Boss || Victim.UnitSetting.unitType == UnitType.MidBoss)
            {
                MgrBattleSystem.Instance.SetBossHPBar(Victim.UnitStat.HP, Victim.UnitStat.MaxHP, Victim.Shield, Victim);
            }
            else
            {
                MgrObjectPool.Instance.ShowObj("FX_Buff_Barrier_Start", Victim.GetUnitCenterPos()).transform.SetParent(Victim.transform);
                Victim.DoHPBarEffect();

                MgrSound.Instance.PlayOneShotSFX("SFX_Buff_Shied", 0.33f);
            }
        }

        public override void OnUpdate()
        {
            if(Duration > 0.0f)
            {
                Duration -= Time.deltaTime;
                if(Duration <= 0.0f || Victim.Shield <= 0.0f)
                    OnEnd();
            }
        }

        public override void OnEnd(bool _isForcedRemove = false)
        {
            Victim.RemoveEffectUpdateEvent(OnUpdate);
            Victim.ListEffectPersonalVariable.Remove(this);
            Victim.DicCCStatus.Remove(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index));

            if(Caster.UnitSetting.unitType == UnitType.AllyBase)
            {
                if (Victim.UnitSetting.unitType == UnitType.AllyBase)
                {
                    BCH.Database.UserGear gearCore = BCH.Database.DataManager.Instance.GetUsingGearInfo(1);
                    if(gearCore is not null && gearCore.gearId.Equals("gear_core_0002") && gearCore.gearRarity >= 10)
                    {
                        MgrObjectPool.Instance.ShowObj("FX_PC-Heal", Victim.GetUnitCenterPos());
                        Victim.SetHeal(Victim.UnitStat.MaxHP * (float)BCH.Database.DataManager.Instance.GetGearOptionValue(gearCore.gearId, 4));
                    }
                }
                else
                {
                    if (MgrBoosterSystem.Instance.DicSkill.ContainsKey("skill_active_005") && MgrBoosterSystem.Instance.DicSkill["skill_active_005"] >= 5 && Victim.Shield > 0.0f)
                    {
                        MgrObjectPool.Instance.ShowObj("FX_PC-Heal", Victim.GetUnitCenterPos());
                        Victim.SetHeal(Victim.Shield * 0.5f);
                    }
                }
            }

            if(Victim.UnitSetting.unitType == UnitType.AllyBase)
                Victim.RemoveUnitEffectVFX(Index, "FX_Ally base_Shield");

            Victim.Shield = 0.0f;
            Victim.DoHPBarEffect();
        }

        public override float GetUnitEffectValue() => EffectFloatValue[0];
    }
}
