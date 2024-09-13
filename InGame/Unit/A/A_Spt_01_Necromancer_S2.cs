using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;
using System.Threading;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_A_Spt_01_Necromancer_S2", menuName = "UnitSkillEvent/A_Spt_01_Necromancer_S2")]
public class A_Spt_01_Necromancer_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        private GameObject objVFX_Follow;
        private GameObject objVFX_Line;
        private SkeletonAnimation skaBone;

        private CancellationTokenSource token_Skill;

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL) && MgrBattleSystem.Instance.CheckIsEnemyUnitInEllipse(unitBase, unitBase.transform.position, skillRange))
                return true;

            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Equals("skill2_finish"))
                return;

            unitBase.SetUnitState(UNIT_STATE.IDLE);
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);

            //ResetVFX();
        }

        public override void EventTriggerSkill()
        {

        }

        public override void OnSkill()
        {
            MgrSound.Instance.PlayOneShotSFX("SFX_A_Spt_01_s2_1", 1.0f);

            unitBase.Ska.AnimationState.Complete -= OnComplete;
            unitBase.Ska.AnimationState.Complete += OnComplete;

            unitBase.SetUnitAnimation("skill2_ready");

            objVFX_Follow = MgrObjectPool.Instance.ShowObj("NecroMancer_Bonefollower_S2", unitBase.transform.position);
            objVFX_Follow.transform.SetParent(unitBase.transform);
            objVFX_Follow.GetComponent<BoneFollower>().skeletonRenderer = unitBase.Ska;
            objVFX_Follow.GetComponent<BoneFollower>().Initialize();
        }

        private void OnComplete(Spine.TrackEntry trackEntry)
        {
            string animName = trackEntry.Animation.Name;

            if (animName.Equals("skill2_ready") && !unitBase.CheckIsState(UNIT_STATE.DEATH) && unitBase.CurrUnitSkill == unitBase.soSkillEvent[1])
            {
                unitBase.Ska.AnimationState.Complete -= OnComplete;

                unitBase.SetUnitAnimation("skill2_idle", true);

                MgrSound.Instance.PlayOneShotSFX("SFX_A_Spt_01_s2_2", 1.0f);

                objVFX_Line = MgrObjectPool.Instance.ShowObj("FX_A_Spt_01_Necromancer_skill2_2_cast", unitBase.transform.position);
                objVFX_Line.transform.localScale = unitBase.transform.localScale;
                objVFX_Line.transform.SetParent(unitBase.transform);

                skaBone = MgrObjectPool.Instance.ShowObj("A_Spt_01_Necromancer_skill2_spine", unitBase.transform.position).GetComponent<SkeletonAnimation>();
                skaBone.AnimationState.SetAnimation(0, "skill2_walk", true);

                token_Skill?.Cancel();
                token_Skill?.Dispose();
                token_Skill = new CancellationTokenSource();
                TaskSkill().Forget();
            }
        }

        private async UniTaskVoid TaskSkill()
        {
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, skillRange));

            listUnit.Reverse();

            int aiType = 0;
            float healAmount = 0.0f;
            while(aiType == 0)
            {
                await UniTask.Yield(token_Skill.Token);

                if(listUnit.Count == 0)
                    break;

                if (listUnit[0].CheckIsState(UNIT_STATE.DEATH))
                {
                    listUnit.Remove(listUnit[0]);
                    continue;
                }

                skaBone.transform.position += 6.0f * Time.deltaTime * (listUnit[0].transform.position - skaBone.transform.position).normalized;
                if (MathLib.CheckIsPosDistanceInRange(skaBone.transform.position, listUnit[0].transform.position, 0.1f))
                {
                    aiType = 1;
                    break;
                }
            }

            // 공격할 적이 없을 경우 스킬 취소
            if(listUnit.Count == 0)
            {
                skaBone.AnimationState.SetAnimation(0, "skill2_finish", false);
                skaBone.transform.GetChild(0).gameObject.SetActive(false);
                await UniTask.Delay(500, cancellationToken: token_Skill.Token);

                if (!unitBase.CheckIsState(UNIT_STATE.DEATH))
                    unitBase.SetUnitAnimation("skill2_finish");

                ResetVFX();
                return;
            }

            // 공격
            MgrSound.Instance.PlayOneShotSFX("SFX_A_Spt_01_s2_3", 1.0f);
            skaBone.AnimationState.SetAnimation(0, "skill2_attack", false);
            await UniTask.Delay(250, cancellationToken: token_Skill.Token);

            // 체력 감소 및 히트 VFX 활성화
            skaBone.transform.GetChild(0).gameObject.SetActive(true);
            healAmount += unitBase.UnitStat.MaxHP * (float)unitSkillData.Param[0];

            listUnit[0].DecreaseHP(unitBase, healAmount, 1);

            // WALK 애니메이션 실행
            await UniTask.Delay(250, cancellationToken: token_Skill.Token);
            skaBone.AnimationState.SetAnimation(0, "skill2_walk", true);

            // 힐 구체 활성화
            await UniTask.Delay(200, cancellationToken: token_Skill.Token);
            MgrSound.Instance.PlayOneShotSFX("SFX_A_Spt_01_s2_4", 1.0f);
            skaBone.transform.GetChild(1).gameObject.SetActive(true);

            // 타겟을 향해 이동
            UnitBase target = MgrBattleSystem.Instance.GetLowestHPUnit(unitBase, true);
            while (aiType == 1)
            {
                await UniTask.Yield(token_Skill.Token);

                if (target is null)
                    break;

                if(target.CheckIsState(UNIT_STATE.DEATH))
                {
                    target = MgrBattleSystem.Instance.GetLowestHPUnit(unitBase, true);
                    continue;
                }

                skaBone.transform.position += 6.0f * Time.deltaTime * (target.transform.position - skaBone.transform.position).normalized;
                if (MathLib.CheckIsPosDistanceInRange(skaBone.transform.position, target.transform.position, 0.1f))
                    break;
            }

            // 타겟이 존재하면 힐
            if(target is not null)
            {
                target.SetHeal(healAmount * 2.0f);
                MgrObjectPool.Instance.ShowObj("FX_PC-Heal", skaBone.transform.GetChild(1).position);
            }

            //if (unitBase.CheckIsState(UNIT_STATE.DEATH))
            //    ResetVFX();

            skaBone.AnimationState.SetAnimation(0, "skill2_finish", false);
            MgrSound.Instance.PlayOneShotSFX("SFX_A_Spt_01_s2_5", 1.0f);

            skaBone.transform.GetChild(1).gameObject.SetActive(false);

            await UniTask.Delay(500, cancellationToken: token_Skill.Token);

            if(!unitBase.CheckIsState(UNIT_STATE.DEATH))
                unitBase.SetUnitAnimation("skill2_finish");

            ResetVFX();
        }

        private void ResetVFX()
        {
            if(objVFX_Follow is not null)
            {
                MgrObjectPool.Instance.HideObj("NecroMancer_Bonefollower_S2", objVFX_Follow);
                objVFX_Follow = null;
            }

            if(objVFX_Line is not null)
            {
                MgrObjectPool.Instance.HideObj("FX_A_Spt_01_Necromancer_skill2_2_cast", objVFX_Line);
                objVFX_Line = null;
            }

            if (skaBone is not null)
                TaskFinish().Forget();
        }

        public override void ResetSkill()
        {
            token_Skill?.Cancel();
            if (skaBone is not null)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_A_Spt_01_s2_5", 1.0f);
                skaBone.AnimationState.SetAnimation(0, "skill2_finish", false);
                skaBone.transform.GetChild(0).gameObject.SetActive(false);
                skaBone.transform.GetChild(1).gameObject.SetActive(false);
            }
            ResetVFX();
        }

        private async UniTaskVoid TaskFinish()
        {
            await UniTask.Delay(1000, cancellationToken: skaBone.GetCancellationTokenOnDestroy());

            if (skaBone is not null)
            {
                MgrObjectPool.Instance.HideObj("A_Spt_01_Necromancer_skill2_spine", skaBone.gameObject);
                skaBone = null;
            }
        }
    }
}
