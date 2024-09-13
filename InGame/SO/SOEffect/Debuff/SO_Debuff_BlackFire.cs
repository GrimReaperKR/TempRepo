using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Debuff_BlackFire", menuName = "InGameEffect/Debuff/BlackFire")]
public class SO_Debuff_BlackFire : SOBase_UnitEffectEvent
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
        private float canMovedCnt;

        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnStart()
        {
            Duration = 0.0f;
            canMovedCnt = EffectFloatValue[1];

            Victim.ListEffectPersonalVariable.Add(this);
            Victim.AddEffectUpdateEvent(OnUpdate);

            Victim.AddUnitEffectVFX(Index, "FX_Debuff_DarkFlame", Victim.transform.position);

            Victim.DicCCStatus.Add(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index), this);

            MgrSound.Instance.PlayOneShotSFX("SFX_Debuff_Black_Fire_1", 1.0f);
        }

        public override void OnUpdate()
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= 2.0f)
            {
                tickTimer -= 2.0f;
                MgrInGameEvent.Instance.BroadcastDamageEvent(Caster, Victim, Victim.UnitStat.MaxHP * 0.1f, 0.0f, 1.0f, -1);
                MgrObjectPool.Instance.ShowObj("FX_Debuff_DarkFlame_hit", Victim.GetUnitCenterPos());
                MgrSound.Instance.PlayOneShotSFX("SFX_Debuff_Black_Fire_2", 0.5f);
            }
            
            if(!MgrBattleSystem.Instance.isStageStart)
                OnEnd();

            if (Duration > 0.0f)
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

            Victim.RemoveUnitEffectVFX(Index, "FX_Debuff_DarkFlame");

            if (MgrBattleSystem.Instance.isStageStart && !_isForcedRemove && canMovedCnt > 0.0f)
            {
                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(Victim, true));
                listUnit.Sort((a, b) => (a.UnitStat.MaxHP).CompareTo(b.UnitStat.MaxHP));

                listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());

                foreach(UnitBase unit in listUnit)
                {
                    if (unit.CheckHasUnitEffect(UNIT_EFFECT.DEBUFF_BLACK_FIRE))
                        continue;

                    GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_Elite2", Victim.GetUnitCenterPos());
                    objBullet.GetComponent<Bullet>().SetBullet(Victim, unit);
                    break;
                }
            }
        }

        public override float GetUnitEffectValue() => EffectFloatValue[0];
    }
}
