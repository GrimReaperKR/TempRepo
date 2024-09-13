using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using Spine.Unity;
using DG.Tweening;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C3_Final_Boss02_bd_S3", menuName = "UnitSkillEvent/Monster/C3_Final_Boss02_bd_S3")]
public class C3_Final_Boss02_bd_S3 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private int releaseCnt = 0;
        private int sfxChannel = -1;

        private List<UnitBase> listUnit = new List<UnitBase>();
        private GameObject objVFX_1;
        private GameObject objVFX_2;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 2)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C3_Final_Boss02_bd_s3_b_2", 1.0f);

            if(_dmgChannel == 2) _victim.AddUnitEffect(UNIT_EFFECT.CC_FREEZE, _attacker, _victim, new float[] { (float)bossSkillData.Param[3] });
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL))
            {
                if (releaseCnt == 0 && unitBase.UnitStat.HP / unitBase.UnitStat.MaxHP <= 0.8f) return true;
                if (releaseCnt == 1 && unitBase.UnitStat.HP / unitBase.UnitStat.MaxHP <= 0.3f) return true;
            }

            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (_animationName.Contains("skill3_b,d1"))
            {
                unitBase.SetUnitAnimation("skill3_b,d2", true);

                objVFX_1 = MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C3_Final_Boss02_d") ? "FX_splash_force_d" : "FX_splash_force_b", unitBase.transform.position);
                objVFX_2 = MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C3_Final_Boss02_d") ? "FX_Dullahan_boss_d_skill3-energy zone" : "FX_Dullahan_boss_b_skill3-energy zone", unitBase.transform.position);
                
                unitBase.AddUnitEffect(UNIT_EFFECT.BUFF_SHIELD, unitBase, unitBase, new float[] { unitBase.UnitStat.MaxHP * (float)bossSkillData.Param[0], 0.0f }, false);
                unitBase.AddUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, unitBase, new float[] { 0.5f, 0.0f }, false);

                TaskSkill().Forget();
            }
            if (_animationName.Contains("skill3_b,d3_success"))
            {
                if (releaseCnt == 0 && unitBase.UnitStat.HP / unitBase.UnitStat.MaxHP <= 0.3f)
                    releaseCnt++;

                unitBase.IsStackPosition = false;

                releaseCnt++;
                unitBase.SetUnitState(UNIT_STATE.IDLE);
                unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);
            }
        }

        public override void EventTriggerSkill()
        {
            Vector3 v3Pos = MgrCamera.Instance.CameraMain.transform.position;
            v3Pos.y -= 1.5f;
            v3Pos.z = 0.0f;
            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C3_Final_Boss02_d") ? "FX_Dullahan_boss_d_skill3-success_bomb" : "FX_Dullahan_boss_b_skill3-success_bomb", v3Pos);

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase));

            foreach(UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[2]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 2);

            if(MgrBattleSystem.Instance.GetAllyBase() is not null)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, MgrBattleSystem.Instance.GetAllyBase(), unitBase.GetAtkRateToDamage((float)bossSkillData.Param[2]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 2);

            MgrCamera.Instance.SetCameraShake(0.3f, 1.5f, 30);
        }

        public override void OnSkill()
        {
            unitBase.IsStackPosition = true;
            unitBase.SetUnitAnimation("skill3_b,d1");
        }

        private async UniTaskVoid TaskSkill()
        {
            sfxChannel = MgrSound.Instance.PlaySFX("SFX_C3_Final_Boss02_bd_s3_a", 1.0f, true);

            float duration = 10.0f;
            float hpDrainDuration = 1.0f;
            float fallIceDuration = 1.0f / 7.0f;
            while(duration > 0.0f)
            {
                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());

                duration -= Time.deltaTime;

                hpDrainDuration -= Time.deltaTime;
                fallIceDuration -= Time.deltaTime;

                if(hpDrainDuration <= 0.0f)
                {
                    hpDrainDuration = 1.0f;

                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase));

                    float healAmount = 0.0f;
                    foreach (UnitBase unit in listUnit)
                    {
                        unit.DecreaseHP(unitBase, unit.UnitStat.MaxHP * (float)bossSkillData.Param[4]);
                        healAmount += unit.UnitStat.MaxHP * (float)bossSkillData.Param[4];
                    }

                    if (MgrBattleSystem.Instance.GetAllyBase() is not null)
                    {
                        MgrBattleSystem.Instance.GetAllyBase().DecreaseHP(unitBase, MgrBattleSystem.Instance.GetAllyBase().UnitStat.MaxHP * (float)bossSkillData.Param[4]);
                        healAmount += MgrBattleSystem.Instance.GetAllyBase().UnitStat.MaxHP * (float)bossSkillData.Param[4];
                    }

                    unitBase.SetHeal(healAmount);
                    MgrObjectPool.Instance.ShowObj("FX_Buff_Dot Heal", unitBase.GetUnitCenterPos());
                }

                if(fallIceDuration <= 0.0f)
                {
                    fallIceDuration = 1.0f / 7.0f;

                    TaskIcicle().Forget();
                }

                if (!unitBase.CheckHasUnitEffect(UNIT_EFFECT.BUFF_SHIELD))
                    break;
            }

            MgrSound.Instance.StopSFX("SFX_C3_Final_Boss02_bd_s3_a", sfxChannel);
            sfxChannel = -1;

            unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_SHIELD, unitBase, true);
            unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, true);

            if (duration > 0.0f)
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_C3_Final_Boss02_bd_s3_c_1", 1.0f);
                MgrSound.Instance.PlayOneShotSFX("SFX_C3_Final_Boss02_bd_s3_c_2", 1.0f);

                MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C3_Final_Boss02_d") ? "FX_splash_force_d" : "FX_splash_force_b", objVFX_1);
                MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C3_Final_Boss02_d") ? "FX_Dullahan_boss_d_skill3-energy zone" : "FX_Dullahan_boss_b_skill3-energy zone", objVFX_2);

                MgrCamera.Instance.SetCameraShake(0.3f, 1.5f, 30);

                unitBase.SetUnitAnimation("skill3_b,d3_fail", true);
                await UniTask.Delay(2000, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

                if (releaseCnt == 0 && unitBase.UnitStat.HP / unitBase.UnitStat.MaxHP <= 0.3f)
                    releaseCnt++;

                unitBase.IsStackPosition = false;

                releaseCnt++;
                unitBase.SetUnitState(UNIT_STATE.IDLE);
                unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);
            }
            else
            {
                unitBase.SetUnitAnimation("skill3_b,d3_success");
                unitBase.PlayTimeline();
            }
        }

        private async UniTaskVoid TaskIcicle()
        {
            Vector3 v3Pos = MgrBattleSystem.Instance.GetAllyBase() is null ? unitBase.transform.position : MgrBattleSystem.Instance.GetAllyBase().transform.position;
            v3Pos.x = Random.Range(v3Pos.x - 2.0f, v3Pos.x + 20.0f);
            v3Pos.y = Random.Range(0.0f, -3.5f);
            v3Pos.z = v3Pos.y * 0.01f;
            SkeletonAnimation skaIcicle = MgrObjectPool.Instance.ShowObj("Icicle", v3Pos + Vector3.up * 17.5f).GetComponent<SkeletonAnimation>();
            skaIcicle.AnimationState.SetAnimation(0, "skill1", false);
            skaIcicle.AnimationState.AddAnimation(0, "skill1_death", false, 0.0f);

            skaIcicle.transform.DOMoveY(v3Pos.y, 0.525f).SetDelay(0.34f).SetEase(Ease.InCirc).OnComplete(() => MgrObjectPool.Instance.ShowObj("FX_ice-hit", v3Pos + Vector3.down * 0.1f).transform.localScale = Vector3.one * 0.5f);

            await UniTask.Delay(950, cancellationToken: skaIcicle.GetCancellationTokenOnDestroy());

            MgrSound.Instance.PlayOneShotSFX("SFX_C3_Final_Boss02_bd_s3_d", 1.0f);

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, v3Pos, 2.5f, _isContainBlockedTarget: true));

            foreach (UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[5]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 3);

            await UniTask.Delay(883, cancellationToken: skaIcicle.GetCancellationTokenOnDestroy());

            MgrObjectPool.Instance.HideObj("Icicle", skaIcicle.gameObject);
        }
    }
}
