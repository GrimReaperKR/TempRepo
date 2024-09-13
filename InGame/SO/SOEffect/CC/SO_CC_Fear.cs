using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_CC_Fear", menuName = "InGameEffect/CC/Fear")]
public class SO_CC_Fear : SOBase_UnitEffectEvent
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

            objVFX = MgrObjectPool.Instance.ShowObj("FX_CC_Flee", Victim.transform.position + (Victim.GetUnitHeight() * Vector3.up));
            objVFX.transform.SetParent(Victim.transform);

            Victim.DicCCStatus.Add(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index), this);

            MgrSound.Instance.PlayOneShotSFX("SFX_CC_Fear", 1.0f);
        }

        public override void OnUpdate()
        {
            if(Duration > 0.0f)
            {
                if(Victim.UnitStat.MoveSpeed > 0.0f && !Victim.CheckIsState(UNIT_STATE.DEATH))
                {
                    if (!Victim.Ska.AnimationName.Equals(Victim.animMoveName))
                        Victim.SetUnitAnimation(Victim.animMoveName, true);

                    float yAngle = Caster.transform.position.x <= Victim.transform.position.x ? 0.0f : -180.0f;
                    if (Caster == Victim) yAngle = Victim.TeamNum == 0 ? -180.0f : 0.0f;
                    Victim.transform.rotation = Quaternion.Euler(0.0f, yAngle, 0.0f);
                    Victim.SetRotationHpBar();
                    Victim.transform.position += Victim.UnitStat.MoveSpeed * 0.5f * Time.deltaTime * (yAngle == 0.0f ? Vector3.right : Vector3.left);
                }

                Duration -= Time.deltaTime;
                if(Duration <= 0.0f)
                    OnEnd();
            }
        }

        public override void OnEnd(bool _isForcedRemove = false)
        {
            MgrObjectPool.Instance.HideObj("FX_CC_Flee", objVFX);
            objVFX = null;

            Victim.RemoveEffectUpdateEvent(OnUpdate);
            Victim.ListEffectPersonalVariable.Remove(this);
            Victim.DicCCStatus.Remove(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index));
        }
    }
}
