using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Final_Boss03_bd_Spawn_S3", menuName = "UnitSkillEvent/Monster/C4_Final_Boss03_bd_Spawn_S3")]
public class C4_Final_Boss03_bd_Spawn_S3 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
        
        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetDeathEvent(personal.OnDeath);
    }

    public class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private bool isBiggerActivate;
        private CancellationTokenSource token_Skill;

        public void OnDeath(UnitBase _attacker, UnitBase _victim, int _dmgChannel)
        {
            _attacker.OnKillEvent(unitBase, _dmgChannel);

            unitBase.UnitPlayableDirector.Stop();
            unitBase.UnitPlayableDirector.playableAsset = null;

            unitBase.UnitBaseParent.RemoveUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, true);

            unitBase.RemoveSpineEvent();

            unitBase.SetUnitState(UNIT_STATE.DEATH, true, true);
            unitBase.SetUnitAnimation("death");

            //if(isBiggerActivate)
            //{
            //    token_Skill?.Cancel();

            //    unitBase.Ska.skeleton.SetSkin("1");
            //    unitBase.transform.DOScale(1.0f, 0.125f);

            //    unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_ATK, unitBase);
            //    unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_ATK_SPEED, unitBase);

            //    unitBase.UnitStat.MaxHP = unitBase.DefaultMaxHP;

            //    isBiggerActivate = false;
            //}

            //unitBase.transform.DOKill();
            //unitBase.transform.DOMoveX(unitBase.transform.position.x + 3.0f, 0.25f);

            unitBase.Ska.AnimationState.Complete -= OnComplete;
            unitBase.Ska.AnimationState.Complete += OnComplete;
        }

        public override bool CheckCanUseSkill()
        {
            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {

        }

        public override void EventTriggerSkill()
        {
        }

        public override void OnSkill()
        {

        }

        public void OnBiggerSkill()
        {
            if (unitBase.CheckIsState(UNIT_STATE.DEATH))
                return;

            unitBase.DecreaseHP(unitBase, unitBase.UnitStat.HP * unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(2, "param.0"));
            unitBase.UnitStat.MaxHP = unitBase.DefaultMaxHP * (1.0f + unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(2, "param.2"));
            if (!isBiggerActivate) unitBase.transform.DOScale(1.7f, 0.125f);
            unitBase.AddUnitEffect(UNIT_EFFECT.BUFF_ATK, unitBase, unitBase, new float[] { unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(2, "param.2"), unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(2, "param.1") });
            unitBase.AddUnitEffect(UNIT_EFFECT.BUFF_ATK_SPEED, unitBase, unitBase, new float[] { unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(2, "param.2"), unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(2, "param.1") });
            unitBase.Ska.skeleton.SetSkin("2");

            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "FX_Ghost_Necromancer_Boss_d_skill3_Bear Zone" : "FX_Ghost_Necromancer_Boss_b_skill3_Bear Zone", unitBase.transform.position);

            isBiggerActivate = true;

            token_Skill?.Cancel();
            token_Skill?.Dispose();
            token_Skill = new CancellationTokenSource();
            TaskSkill().Forget();
        }

        private async UniTaskVoid TaskSkill()
        {
            float duration = unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(2, "param.1");
            float healTimer = 1.0f;
            while (duration > 0.0f)
            {
                duration -= Time.deltaTime;
                healTimer -= Time.deltaTime;
                if (healTimer <= 0.0f)
                {
                    healTimer += 1.0f;
                    if (!unitBase.CheckIsState(UNIT_STATE.DEATH))
                        unitBase.SetHeal(unitBase.UnitStat.MaxHP * unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(2, "param.5"));
                }

                await UniTask.Yield(cancellationToken: token_Skill.Token);
            }

            unitBase.UnitStat.MaxHP = unitBase.DefaultMaxHP;
            if (unitBase.UnitStat.HP > unitBase.UnitStat.MaxHP)
                unitBase.UnitStat.HP = unitBase.UnitStat.MaxHP;

            unitBase.Ska.skeleton.SetSkin("1");
            unitBase.transform.DOScale(1.0f, 0.125f);

            isBiggerActivate = false;
        }

        private async UniTaskVoid TaskRevive()
        {
            await UniTask.Delay(5000, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            if (unitBase.UnitBaseParent.CheckIsState(UNIT_STATE.DEATH))
            {
                unitBase.Ska.AnimationState.Complete -= OnComplete;
                return;
            }

            unitBase.SetHeal(unitBase.DefaultMaxHP);
            unitBase.SetUnitAnimation("resurrection");
        }

        private void OnComplete(Spine.TrackEntry trackEntry)
        {
            string animationName = trackEntry.Animation.Name;

            if(animationName.Equals("death"))
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_C4_Huge_Doll_Death", 1.0f);
                TaskRevive().Forget();
            }

            if(animationName.Equals("resurrection"))
            {
                unitBase.InitSpineEvent();
                unitBase.SetUnitState(UNIT_STATE.IDLE, true);
                unitBase.UnitBaseParent.AddUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, unitBase.UnitBaseParent, new float[] { unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(0, "param.5"), 0.0f }, false);
                unitBase.Ska.AnimationState.Complete -= OnComplete;
            }
        }
    }
}
