using Spine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_CC_Freeze", menuName = "InGameEffect/CC/Freeze")]
public class SO_CC_Freeze : SOBase_UnitEffectEvent
{
    public override void OnInitialize(UnitBase _caster, UnitBase _victim, UNIT_EFFECT _index, float[] _value, bool _canRemove)
    {
        PersonalVariable personal = new PersonalVariable();
        personal.SetData(this, _caster, _victim, _index, _value, _canRemove);
        personal.OnStart();
    }

    private class PersonalVariable : UnitEffectPersonalVariableInstance
    {
        private SkeletonAnimation skaVFX;

        public override void OnStart()
        {
            Victim.FreezeDmg = 0.0f;

            Duration = EffectFloatValue[0];
            if (MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp) Duration *= 0.5f;

            Victim.ListEffectPersonalVariable.Add(this);
            Victim.AddEffectUpdateEvent(OnUpdate);

            Victim.ChangeForceToIdle();

            MgrObjectPool.Instance.ShowObj("FX_CC_Freeze_hit", Victim.GetUnitCenterPos());

            skaVFX = MgrObjectPool.Instance.ShowObj("FX_CC_Freeze", Victim.transform.position + Vector3.back * 0.001f).GetComponent<SkeletonAnimation>();
            skaVFX.transform.localScale = new Vector3(-Victim.transform.localScale.x, 1.0f, 1.0f);
            skaVFX.AnimationState.SetAnimation(0, "skill1", false);
            skaVFX.transform.SetParent(Victim.transform);

            skaVFX.AnimationState.Complete -= OnComplete;
            skaVFX.AnimationState.Complete += OnComplete;

            Victim.UpdateSpineUnitSpineSpeed();
            Victim.UpdateSpineTint();

            Victim.DicCCStatus.Add(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index), this);

            MgrSound.Instance.PlayOneShotSFX("SFX_CC_Freezing_a", 1.0f);
        }

        private void OnComplete(TrackEntry trackEntry)
        {
            string animationName = trackEntry.Animation.Name;

            if (animationName.Equals("death"))
            {
                MgrObjectPool.Instance.HideObj("FX_CC_Freeze", skaVFX.gameObject);
                skaVFX.AnimationState.Complete -= OnComplete;
                skaVFX = null;
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
            skaVFX.AnimationState.SetAnimation(0, "death", false);

            MgrSound.Instance.PlayOneShotSFX("SFX_CC_Freezing_b", 1.0f);

            Victim.RemoveEffectUpdateEvent(OnUpdate);
            Victim.ListEffectPersonalVariable.Remove(this);
            Victim.DicCCStatus.Remove(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index));

            MgrInGameEvent.Instance.BroadcastDamageEvent(Victim, Victim, Victim.FreezeDmg, 0.0f, 1.0f, 10);
            Victim.FreezeDmg = 0;

            Victim.UpdateSpineUnitSpineSpeed();
            Victim.UpdateSpineTint();
        }
    }
}
