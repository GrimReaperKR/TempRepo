using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Mid_Boss03_bd_S1", menuName = "UnitSkillEvent/Monster/C4_Mid_Boss03_bd_S1")]
public class C4_Mid_Boss03_bd_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetTakeDamageEvent(personal.OnDamagedAction);

        _unitBase.Ska.skeleton.SetSkin(_unitBase.UnitIndex.Equals("C4_Mid_Boss03_d") ? "d" : "b");
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        private float nextHPToSpawn = 0.66f;
        private float spawnToDelay = 0.0f;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 0)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Mid_Boss03_bd_s1_4", 1.0f);
        }

        public void OnDamagedAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (unitBase.UnitStat.HP / unitBase.UnitStat.MaxHP <= nextHPToSpawn)
            {
                if (MgrBattleSystem.Instance.IsChallengeMode && MgrBattleSystem.Instance.ChallengeLevel == 2 && MgrBattleSystem.Instance.GetCurrentThema() == 4)
                {
                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase));
                    foreach (UnitBase unit in listUnit)
                        unit.AddUnitEffect(UNIT_EFFECT.CC_FEAR, unitBase, unit, new float[] { 3.0f });
                }

                if (spawnToDelay <= 0.0f && nextHPToSpawn > 0.0f)
                {
                    MgrBattleSystem.Instance.SpawnWaveUnit();
                    spawnToDelay = 5.0f;
                    TaskSpawnDelay().Forget();
                }
                nextHPToSpawn -= 0.33f;
            }
        }

        private async UniTaskVoid TaskSpawnDelay()
        {
            while (spawnToDelay > 0.0f)
            {
                await UniTask.Yield();
                spawnToDelay -= Time.deltaTime;
            }
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            if (unitBase.EnemyTarget && MathLib.CheckIsInEllipse(unitBase.transform.position, skillRange, unitBase.EnemyTarget.transform.position)) return true;
            else
            {
                if (unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    return false;

                UnitBase unitInEllipse = MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, skillRange);
                if (unitInEllipse is not null)
                {
                    unitBase.EnemyTarget = unitInEllipse;
                    return true;
                }
            }

            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill"))
                return;

            unitBase.SetUnitState(UNIT_STATE.IDLE);
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);
        }

        public override void EventTriggerSkill()
        {
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase));
            listUnit.Sort((a, b) => (a.UnitStat.HP / a.UnitStat.MaxHP).CompareTo(b.UnitStat.HP / b.UnitStat.MaxHP));

            if (listUnit.Count < 2 && MgrBattleSystem.Instance.GetAllyBase() != null)
                listUnit.Add(MgrBattleSystem.Instance.GetAllyBase());

            int targetCnt = listUnit.Count > 2 ? 2 : listUnit.Count;

            for (int i = 0; i < targetCnt; i++)
            {
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, listUnit[i], unitBase.GetAtkRateToDamage((float)bossSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 0);

                GameObject objVFX = MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C4_Mid_Boss03_d") ? "FX_Ghost_Puppeteer_mid boss_d_skill1_Ghost" : "FX_Ghost_Puppeteer_mid boss_b_skill1_Ghost", listUnit[i].transform.position);
                objVFX.transform.GetChild(0).GetComponent<PlayableDirector>().Play();
                TaskVFXDestroy(objVFX).Forget();
            }

            for(int i = 0; i < (int)bossSkillData.Param[2]; i++)
            {
                Vector3 v3Pos = unitBase.transform.position + Vector3.right * Random.Range(0.1f, 3.0f);
                v3Pos.y = Random.Range(0.0f, -3.5f);
                MgrUnitPool.Instance.ShowEnemyMonsterObj(unitBase.UnitIndex.Equals("C4_Mid_Boss03_d") ? "C4_Mid_Boss03_d_s" : "C4_Mid_Boss03_b_s", unitBase.TeamNum, v3Pos, true, unitBase);
                MgrObjectPool.Instance.ShowObj("FX_Ghost_Summon", v3Pos);
            }
            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Mini_Doll_Summon_ab", 1.0f);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1_b,d");
            unitBase.PlayTimeline(unitBase.UnitIndex.Equals("C4_Mid_Boss03_d") ? 1 : 0);

            UnitBase target = MgrBattleSystem.Instance.GetNearestXEnemyUnit(unitBase);
            if (target is not null)
            {
                if (Mathf.Abs(unitBase.transform.position.x - target.transform.position.x) < (float)bossSkillData.Param[5] + 3.0f)
                    TaskBackStep().Forget();
            }
        }

        private async UniTaskVoid TaskBackStep()
        {
            float duration = 1.5f;
            Vector3 v3EndPos = unitBase.transform.position + (unitBase.GetUnitLookDirection(true)) * (float)bossSkillData.Param[5];

            while (duration > 0.0f)
            {
                if (unitBase.CheckHasBlockedMoveCC() || unitBase.CheckIsState(UNIT_STATE.DEATH))
                    break;

                duration -= Time.deltaTime;

                unitBase.transform.position = Vector3.Lerp(unitBase.transform.position, v3EndPos, (1.5f - duration) / 1.5f);

                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());
            }
        }

        private async UniTaskVoid TaskVFXDestroy(GameObject _objVFX)
        {
            await UniTask.Delay(833, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C4_Mid_Boss03_d") ? "FX_Ghost_Puppeteer_mid boss_d_skill1_Ghost" : "FX_Ghost_Puppeteer_mid boss_b_skill1_Ghost", _objVFX);
        }
    }
}
