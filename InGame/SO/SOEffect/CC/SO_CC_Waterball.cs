using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "SO_CC_Waterball", menuName = "InGameEffect/CC/Waterball")]
public class SO_CC_Waterball : SOBase_UnitEffectEvent
{
    public override void OnInitialize(UnitBase _caster, UnitBase _victim, UNIT_EFFECT _index, float[] _value, bool _canRemove)
    {
        PersonalVariable personal = new PersonalVariable();
        personal.SetData(this, _caster, _victim, _index, _value, _canRemove);
        personal.OnStart();
    }

    private class PersonalVariable : UnitEffectPersonalVariableInstance
    {
        private float xMove;
        private float maxDuration;

        private Vector3 v3Dir;
        private GameObject objVFX;

        public override void OnStart()
        {
            xMove = EffectFloatValue[0];
            Duration = EffectFloatValue[1];
            if (MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp) Duration *= 0.5f;

            maxDuration = Duration;

            v3Dir = Victim.TeamNum == 0 ? Vector3.left : Vector3.right;

            Victim.ListEffectPersonalVariable.Add(this);
            Victim.AddEffectUpdateEvent(OnUpdate);

            Victim.ChangeForceToIdle();

            objVFX = MgrObjectPool.Instance.ShowObj("FX_CC_Waterball", Victim.GetUnitCenterPos() + Vector3.left * 0.25f + Vector3.up * 0.5f);
            objVFX.transform.SetParent(Victim.transform);

            Victim.Ska.transform.position += Vector3.up * 0.5f;

            //Victim.transform.DOKill();
            //Victim.transform.DOMoveX(Victim.transform.position.x + (v3Dir.x * xMove), Duration).SetEase(Ease.Linear).OnComplete(() => OnEnd());

            Victim.DicCCStatus.Add(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index), this);

            MgrSound.Instance.PlayOneShotSFX("SFX_CC_Bubble", 1.0f);
        }

        public override void OnUpdate()
        {
            if (Duration > 0.0f)
            {
                Victim.transform.position += (xMove / maxDuration) * Time.deltaTime * v3Dir;

                Duration -= Time.deltaTime;
                if (Duration <= 0.0f)
                    OnEnd();
            }
        }

        public override void OnEnd(bool _isForcedRemove = false)
        {
            MgrObjectPool.Instance.HideObj("FX_CC_Waterball", objVFX);
            objVFX = null;

            //Victim.transform.DOKill();
            Victim.Ska.transform.position += Vector3.down * 0.5f;

            Victim.RemoveEffectUpdateEvent(OnUpdate);
            Victim.ListEffectPersonalVariable.Remove(this);
            Victim.DicCCStatus.Remove(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index));
        }
    }
}
