using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Spine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SO_CC_Snowman", menuName = "InGameEffect/CC/Snowman")]
public class SO_CC_Snowman : SOBase_UnitEffectEvent
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
            Duration = EffectFloatValue[0];
            if (MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp) Duration *= 0.5f;

            Victim.ListEffectPersonalVariable.Add(this);
            Victim.AddEffectUpdateEvent(OnUpdate);

            Victim.ChangeForceToIdle();

            TaskSnowmanAlpha().Forget();

            skaVFX = MgrObjectPool.Instance.ShowObj("FX_Snowman", Victim.transform.position).GetComponent<SkeletonAnimation>();
            skaVFX.transform.rotation = Quaternion.Euler(0.0f, Victim.GetUnitLookDirection() == Vector3.left ? 0.0f : -180.0f, 0.0f);
            skaVFX.AnimationState.SetAnimation(0, "skill2_a", false);
            skaVFX.AnimationState.AddAnimation(0, "skill2_b", false, 0.0f);
            skaVFX.transform.SetParent(Victim.transform);

            skaVFX.AnimationState.Complete -= OnComplete;
            skaVFX.AnimationState.Complete += OnComplete;

            Victim.DicCCStatus.Add(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index), this);

            MgrSound.Instance.PlayOneShotSFX("SFX_CC_Snow_a", 1.0f);
        }

        private async UniTaskVoid TaskSnowmanAlpha()
        {
            await UniTask.Delay(250, cancellationToken: Victim.GetCancellationTokenOnDestroy());
            Victim.Ska.skeleton.SetColor(new Color(1.0f, 1.0f, 1.0f, 0.0f));
        }

        private void OnComplete(TrackEntry trackEntry)
        {
            string animationName = trackEntry.Animation.Name;

            //if (animationName.Equals("skill2_a"))
            //    skaVFX.AnimationState.SetAnimation(0, "skill2_b", false);

            if (animationName.Equals("skill2_c"))
            {
                MgrObjectPool.Instance.HideObj("FX_Snowman", skaVFX.gameObject);
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
            MgrObjectPool.Instance.ResetParent("FX_Snowman", skaVFX.gameObject);

            MgrSound.Instance.PlayOneShotSFX("SFX_CC_Snow_b", 1.0f);

            skaVFX.AnimationState.ClearTrack(0);
            //skaVFX.transform.SetParent(MgrObjectPool.Instance.GainFolderObj("FX_Snowman").objRootFolder.transform);
            skaVFX.AnimationState.SetAnimation(0, "skill2_c", false);

            Victim.Ska.skeleton.SetColor(new Color(1.0f, 1.0f, 1.0f, 1.0f));

            Victim.RemoveEffectUpdateEvent(OnUpdate);
            Victim.ListEffectPersonalVariable.Remove(this);
            Victim.DicCCStatus.Remove(new UnitEffect_Status(Caster is null ? "World" : Caster.UnitIndex, Victim, Index));

            MgrInGameEvent.Instance.BroadcastDamageEvent(Caster, Victim, Victim.UnitStat.MaxHP * 0.1f, 0.0f, 1.0f, 10);
        }
    }
}
