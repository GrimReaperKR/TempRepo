using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Final_Boss03_ac_S1", menuName = "UnitSkillEvent/Monster/C4_Final_Boss03_ac_S1")]
public class C4_Final_Boss03_ac_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetTakeDamageEvent(personal.OnDamagedAction);

        _unitBase.Ska.skeleton.SetSkin(_unitBase.UnitIndex.Equals("C4_Final_Boss03_c") ? "c" : "a");
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        private float nextHPToSpawn = 0.8f;
        private float spawnToDelay = 0.0f;

        public void OnDamagedAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if(unitBase.UnitStat.HP / unitBase.UnitStat.MaxHP <= nextHPToSpawn)
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
                nextHPToSpawn -= 0.2f;
            }
        }

        private async UniTaskVoid TaskSpawnDelay()
        {
            while(spawnToDelay > 0.0f)
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
            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Final_Boss03_ac_s1_1_", 1.0f);

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase));
            listUnit.Sort((a, b) => (a.UnitStat.HP / a.UnitStat.MaxHP).CompareTo(b.UnitStat.HP / b.UnitStat.MaxHP));

            if (listUnit.Count > (int)bossSkillData.Param[0])
                listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());

            TaskSkill().Forget();
        }

        private async UniTaskVoid TaskSkill()
        {
            int targetCnt = listUnit.Count > (int)bossSkillData.Param[0] ? (int)bossSkillData.Param[0] : listUnit.Count;
            for (int i = 0; i < targetCnt; i++)
            {
                GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_C4_Final_Boss03_a", unitBase.transform.position + new Vector3((unitBase.GetUnitLookDirection() == Vector3.left ? -4.5f : 4.5f), 4.0f, 0.0f));
                objBullet.GetComponent<Bullet>().SetBullet(unitBase, listUnit[i]);

                await UniTask.Delay(50, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            }
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill1_a,c");
            unitBase.PlayTimeline(unitBase.UnitIndex.Equals("C4_Final_Boss03_c") ? 1 : 0);
        }
    }
}
