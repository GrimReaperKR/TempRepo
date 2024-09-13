using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "SO_CC_SuperKnockback", menuName = "InGameEffect/CC/Super_Knockback")]
public class SO_CC_SuperKnockback : SOBase_UnitEffectEvent
{
    public override void OnInitialize(UnitBase _caster, UnitBase _victim, UNIT_EFFECT _index, float[] _value, bool _canRemove)
    {
        // 넉백은 탱커에게는 적용하지 않음
        if (_victim.IsStackPosition)
            return;

        PersonalVariable personal = new PersonalVariable();
        personal.SetData(this, _caster, _victim, _index, _value, _canRemove);
        personal.OnStart();
    }

    private class PersonalVariable : UnitEffectPersonalVariableInstance
    {
        private float Power;
        private Vector3 v3Dir;

        public override void OnStart()
        {
            Power = EffectFloatValue[0];

            if (Caster == Victim) v3Dir = Caster.TeamNum == 1 ? Vector3.right : Vector3.left;
            else v3Dir = Caster.transform.position.x < Victim.transform.position.x ? Vector3.right : Vector3.left;

            Victim.ListEffectPersonalVariable.Add(this);
            //Victim.AddCCUpdateEvent(OnUpdate);

            Victim.ChangeForceToIdle();

            Victim.transform.DOKill();
            Victim.transform.DOMoveX(Victim.transform.position.x + (v3Dir.x * Power), Power * 0.2f).SetEase(Ease.OutCubic).OnComplete(() => OnEnd());

            Victim.DicCCStatus.Add(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index), this);
        }

        public override void OnUpdate()
        {

        }

        public override void OnEnd(bool _isForcedRemove = false)
        {
            //Victim.RemoveCCUpdateEvent(OnUpdate);
            Victim.ListEffectPersonalVariable.Remove(this);
            Victim.DicCCStatus.Remove(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index));
        }
    }
}
