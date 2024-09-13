using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Spine;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_S_Arch_01_Tanya_S1", menuName = "UnitSkillEvent/S_Arch_01_Tanya_S1")]
public class S_Arch_01_Tanya_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;

        _unitBase.UnitStat.Range = (float)personal.unitSkillData.Param[0];
        _unitBase.UnitStat.WidthRange = 2.0f;

        UnitBase target = MgrBattleSystem.Instance.GetFarestXEnemyUnitInRange(_unitBase, personal.skillRange);
        if (target is not null)
        {
            float yPos = target.transform.position.y + Random.Range(-0.25f, 0.25f);
            if (yPos > 0.0f) yPos = 0.0f;
            if (yPos < -3.5f) yPos = -3.5f;
            _unitBase.transform.position = new Vector3(_unitBase.transform.position.x + 1.9f, yPos, yPos * 0.01f);
        }
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        private bool isFirstSniperMode = true;
        private bool isSniperMode;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 0)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_S_Arch_01_s1_b_3", 1.0f);

            if (MathLib.CheckPercentage((float)unitSkillData.Param[2]))
                _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitSkillData.Param[3]), _attacker, _victim, new float[] { (float)unitSkillData.Param[4] });
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            float checkRange = isSniperMode ? skillRange : (float)unitSkillData.Param[0];
            if (unitBase.EnemyTarget && MgrBattleSystem.Instance.CheckIsEnemyUnitInLine(unitBase, unitBase.transform.position, checkRange, 2.0f)) return true;
            else
            {
                if (unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    return false;

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, checkRange, 2.0f));
                UnitBase unitInEllipse = listUnit.Count > 0 ? listUnit[0] : null;
                if (unitInEllipse is not null)
                {
                    unitBase.EnemyTarget = unitInEllipse;
                    return true;
                }
            }

            // 자동 회수
            if (isSniperMode)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_S_Arch_01_return_1", 1.0f);
                TaskReturnSFX().Forget();

                unitBase.UnitPlayableDirector.Stop();
                unitBase.UnitPlayableDirector.playableAsset = null;
                unitBase.SetUnitState(UNIT_STATE.DEATH, true, true);
                unitBase.SetUnitAnimation("return");

                unitBase.Ska.AnimationState.Complete -= OnComplete;
                unitBase.Ska.AnimationState.Complete += OnComplete;
            }

            return false;
        }

        private async UniTaskVoid TaskReturnSFX()
        {
            await UniTask.Delay(1000, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            MgrSound.Instance.PlayOneShotSFX("SFX_S_Arch_01_return_1_2", 1.0f);
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill"))
                return;

            if(_animationName.Equals("skill1_ready"))
            {
                isFirstSniperMode = false;
                unitBase.RemoveUnitEffect(UNIT_EFFECT.ETC_GOD, unitBase, true);
            }

            unitBase.SetUnitState(UNIT_STATE.IDLE);

            if (!_animationName.Equals("skill1_ready"))
                unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);

            unitBase.PlayTimeline(1, soSkillEvent, true);
        }

        public override void EventTriggerSkill()
        {
            Vector3 v3Dir = unitBase.GetUnitLookDirection();

            GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_S_Arch_01_S1", unitBase.transform.position + v3Dir * 2.0f + Vector3.up * 0.5f);
            objBullet.GetComponent<Bullet>().SetBullet(unitBase, unitBase.EnemyTarget);
        }

        public override void OnSkill()
        {
            if(!isSniperMode)
            {
                if(isFirstSniperMode)
                    unitBase.AddUnitEffect(UNIT_EFFECT.ETC_GOD, unitBase, unitBase, new float[] { 0.0f }, false);

                unitBase.SetUnitAnimation("skill1_ready");
                unitBase.animIdleName = "skill1_idle";
                isSniperMode = true;
                //unitBase.IsStackPosition = true;
                //unitBase.UnitStat.MoveSpeed = 0.0f;
                unitBase.PlayTimeline(0);

                unitBase.UnitStat.Range = skillRange;

                TaskUnitHasCCEffect().Forget();
            }
            else
            {
                unitBase.EnemyTarget = MgrBattleSystem.Instance.GetFarestXEnemyUnitInRange(unitBase, skillRange);

                unitBase.SetUnitAnimation("skill1_attack");
                unitBase.PlayTimeline(2);
            }
        }

        private void OnComplete(TrackEntry trackEntry)
        {
            string animationName = trackEntry.Animation.Name;

            if (animationName.Contains("return"))
            {
                int dropFishCnt = (int)(unitBase.UnitSetting.unitCost * (float)unitSkillData.Param[5]);
                TaskDropFish(dropFishCnt, unitBase.transform.position + Vector3.up * unitBase.GetUnitHeight(), unitBase.transform.position).Forget();

                unitBase.Ska.AnimationState.Complete -= OnComplete;
                unitBase.OnAfterDeath();

                if(unitBase.TeamNum == 0)
                    MgrBattleSystem.Instance.ReduceUnitSpawnCooldown(unitBase.UnitSetting.unitIndex, 0.5f);
            }
        }

        private async UniTaskVoid TaskDropFish(int _dropCnt, Vector3 _v3Start, Vector3 _v3End)
        {
            while(_dropCnt > 0)
            {
                _dropCnt--;
                MgrBattleSystem.Instance.GlobalOption.TaskDropBakedFish(_v3Start, _v3End).Forget();

                await UniTask.Yield(cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            }
        }

        private async UniTaskVoid TaskUnitHasCCEffect()
        {
            while(isSniperMode)
            {
                if(unitBase.CheckHasBlockedMoveSkillCC())
                {
                    isSniperMode = false;
                    unitBase.animIdleName = "idle";
                    unitBase.UnitStat.Range = (float)unitSkillData.Param[0];
                    break;
                }

                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());
            }
        }

        public override void ResetSkill()
        {
            if(isFirstSniperMode)
            {
                isFirstSniperMode = false;
                unitBase.RemoveUnitEffect(UNIT_EFFECT.ETC_GOD, unitBase, true);
            }
        }
    }
}
