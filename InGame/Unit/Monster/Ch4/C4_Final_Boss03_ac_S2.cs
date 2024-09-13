using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Final_Boss03_ac_S2", menuName = "UnitSkillEvent/Monster/C4_Final_Boss03_ac_S2")]
public class C4_Final_Boss03_ac_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    public class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private CancellationTokenSource token_SpawnGod;
        private List<UnitBase> listUnit_Spawn = new List<UnitBase>();
        public int SpawnedCnt = 0;

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
            UnitBase target = MgrBattleSystem.Instance.GetNearestXEnemyUnit(unitBase);
            if (target is not null)
                TaskUnitSpawn(target).Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2_a,c");
            unitBase.PlayTimeline(unitBase.UnitIndex.Equals("C4_Final_Boss03_c") ? 1 : 0);

            UnitBase target = MgrBattleSystem.Instance.GetNearestXEnemyUnit(unitBase);
            if (target is not null)
            {
                if (unitBase.transform.position.x - target.transform.position.x < (float)bossSkillData.Param[0] + 3.0f)
                    TaskBackStep().Forget();
            }
        }

        private async UniTaskVoid TaskBackStep()
        {
            float duration = 1.5f;
            Vector3 v3EndPos = unitBase.transform.position + (unitBase.GetUnitLookDirection(true)) * (float)bossSkillData.Param[0];

            while (duration > 0.0f)
            {
                if (unitBase.CheckHasBlockedMoveCC() || unitBase.CheckIsState(UNIT_STATE.DEATH))
                    break;

                duration -= Time.deltaTime;

                unitBase.transform.position = Vector3.Lerp(unitBase.transform.position, v3EndPos, (1.5f - duration) / 1.5f);

                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());
            }
        }

        private async UniTaskVoid TaskUnitSpawn(UnitBase _target)
        {
            SetGod();

            int spawnCnt = (int)bossSkillData.Param[1];
            while(spawnCnt > 0)
            {
                spawnCnt--;
                SpawnedCnt++;

                Vector3 v3Pos = unitBase.transform.position + Vector3.left * Random.Range(0.0f, unitBase.transform.position.x - _target.transform.position.x);
                v3Pos.y = Random.Range(0.0f, -3.5f);
                MgrObjectPool.Instance.ShowObj("FX_Ghost_Summon", v3Pos);
                listUnit_Spawn.Add(MgrUnitPool.Instance.ShowEnemyMonsterObj(unitBase.UnitIndex.Equals("C4_Final_Boss03_c") ? "C4_Final_Boss03_c_s" : "C4_Final_Boss03_a_s", unitBase.TeamNum, v3Pos, true, unitBase).GetComponent<UnitBase>());

                MgrSound.Instance.PlayOneShotSFX("SFX_C4_Mini_Doll_Summon_ab", 1.0f);

                await UniTask.Delay(100, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            }
        }

        public async UniTaskVoid TaskUnitRespawn(int _cnt)
        {
            SetGod();

            int spawnCnt = _cnt > 50 ? 50 : _cnt;
            float delayTime = spawnCnt < 25 ? 0.1f : (2.5f / spawnCnt);
            while (spawnCnt > 0)
            {
                spawnCnt--;

                Vector3 v3Pos = unitBase.transform.position + Vector3.right * Random.Range(0.0f, 3.5f);
                v3Pos.y = Random.Range(0.0f, -3.5f);

                UnitBase spawnUnit = MgrUnitPool.Instance.ShowEnemyMonsterObj(unitBase.UnitIndex.Equals("C4_Final_Boss03_c") ? "C4_Final_Boss03_c_s" : "C4_Final_Boss03_a_s", unitBase.TeamNum, v3Pos, true, unitBase).GetComponent<UnitBase>();
                spawnUnit.SetUnitState(UNIT_STATE.DEATH, true, true);
                spawnUnit.PlayTimeline(1, spawnUnit.soSkillEvent[0]);
                listUnit_Spawn.Add(spawnUnit);

                MgrSound.Instance.PlayOneShotSFX("SFX_C4_Mini_Doll_AC_2", 1.0f);

                await UniTask.Delay(System.TimeSpan.FromSeconds(delayTime), cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            }
        }

        private void SetGod()
        {
            unitBase.AddUnitEffect(UNIT_EFFECT.ETC_GOD, unitBase, unitBase, new float[] { 0.0f }, false);
            unitBase.AddUnitEffectVFX(UNIT_EFFECT.ETC_GOD, "FX_Ghost_Necromancer_Boss_Invincibility", unitBase.GetUnitCenterPos());

            token_SpawnGod?.Cancel();
            token_SpawnGod?.Dispose();
            token_SpawnGod = new CancellationTokenSource();
            TaskGodCheck().Forget();
        }

        private async UniTaskVoid TaskGodCheck()
        {
            await UniTask.NextFrame(cancellationToken: token_SpawnGod.Token);

            while(listUnit_Spawn.Count > 0)
            {
                for (int i = listUnit_Spawn.Count - 1; i >= 0; i--)
                {
                    if (listUnit_Spawn[i].CheckIsState(UNIT_STATE.DEATH))
                        listUnit_Spawn.RemoveAt(i);
                }

                await UniTask.Yield(token_SpawnGod.Token);
            }

            unitBase.RemoveUnitEffect(UNIT_EFFECT.ETC_GOD, unitBase, true);
            unitBase.RemoveUnitEffectVFX(UNIT_EFFECT.ETC_GOD, "FX_Ghost_Necromancer_Boss_Invincibility");
        }
    }
}
