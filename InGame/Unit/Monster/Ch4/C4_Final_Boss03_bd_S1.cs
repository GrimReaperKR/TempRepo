using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C4_Final_Boss03_bd_S1", menuName = "UnitSkillEvent/Monster/C4_Final_Boss03_bd_S1")]
public class C4_Final_Boss03_bd_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetTakeDamageEvent(personal.OnDamagedAction);

        _unitBase.Ska.skeleton.SetSkin(_unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "d" : "b");

        UnitBase spawnUnit = MgrUnitPool.Instance.ShowEnemyMonsterObj(_unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "C4_Final_Boss03_d_s" : "C4_Final_Boss03_b_s", _unitBase.TeamNum, _unitBase.transform.position + Vector3.left * 2.0f, true, _unitBase).GetComponent<UnitBase>();
        personal.SetUnitDoll(spawnUnit);
    }

    public class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        private ParticleSystem[] arrParsysLaser = new ParticleSystem[3];
        private ParticleSystem.MainModule mainModule;
        private ParticleSystem.MinMaxCurve curve;

        private float nextHPToSpawn = 0.8f;
        private float spawnToDelay = 0.0f;

        public UnitBase unitDoll;

        private int hitCnt = 0;

        public void SetUnitDoll(UnitBase _target)
        {
            unitDoll = _target;
            TaskCheckDoll().Forget();
        }

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

            if (_animationName.Contains("skill1_b,d_finish"))
            {
                unitBase.SetUnitState(UNIT_STATE.IDLE);
                unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);
            }
        }

        public override void EventTriggerSkill()
        {
            unitBase.SetUnitAnimation("skill1_b,d_idle", true);

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase));
            listUnit.Sort((a, b) => (a.UnitStat.HP / a.UnitStat.MaxHP).CompareTo(b.UnitStat.HP / b.UnitStat.MaxHP));

            if (listUnit.Count > 3)
                listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());

            TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            if (arrParsysLaser[0] is null) arrParsysLaser[0] = unitBase.transform.Find(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "BoneFollower_d_s1(Clone)" : "BoneFollower_b_s1(Clone)").Find("FX_Line-1").GetComponent<ParticleSystem>();
            if (arrParsysLaser[1] is null) arrParsysLaser[1] = unitBase.transform.Find(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "BoneFollower_d_s1(Clone)" : "BoneFollower_b_s1(Clone)").Find("FX_Line-2").GetComponent<ParticleSystem>();
            if (arrParsysLaser[2] is null) arrParsysLaser[2] = unitBase.transform.Find(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "BoneFollower_d_s1(Clone)" : "BoneFollower_b_s1(Clone)").Find("FX_Line-3").GetComponent<ParticleSystem>();

            unitBase.SetUnitAnimation("skill1_b,d_ready");
            unitBase.PlayTimeline(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? 1 : 0);
        }

        private Dictionary<UnitBase, GameObject> dicDrawVFX = new Dictionary<UnitBase, GameObject>();
        private async UniTaskVoid TaskSkill()
        {
            hitCnt = 4;
            float duration = 0.5f;

            float drainHP = 0.0f;

            dicDrawVFX.Clear();

            GameObject objParentVFX = MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "FX_Ghost_Necromancer_Boss_Bear_d_heal cast" : "FX_Ghost_Necromancer_Boss_Bear_b_heal cast", unitBase.transform.position);
            objParentVFX.transform.SetParent(unitBase.transform);
            GameObject objDollVFX = MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "FX_Ghost_Necromancer_Boss_Bear_d_heal cast" : "FX_Ghost_Necromancer_Boss_Bear_b_heal cast", unitDoll.transform.position);
            objDollVFX.transform.SetParent(unitDoll.transform);
            objDollVFX.transform.localScale = Vector3.one;

            while (hitCnt > 0 && unitBase.CheckIsState(UNIT_STATE.SKILL))
            {
                int effectCnt = listUnit.Count > 3 ? 3 : listUnit.Count;
                for(int i = 0; i < 3; i++)
                {
                    mainModule = arrParsysLaser[i].main;
                    curve = mainModule.startRotation;

                    if (i < effectCnt)
                    {
                        Vector2 v2Vel = listUnit[i].GetUnitCenterPos() - arrParsysLaser[i].transform.position;
                        mainModule.startSizeYMultiplier = listUnit[i].CheckIsState(UNIT_STATE.DEATH) ? 0.0f : v2Vel.magnitude * 1.1f;
                        float angle = 180.0f + (Mathf.Atan2(v2Vel.y, v2Vel.x) * Mathf.Rad2Deg);
                        arrParsysLaser[i].transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

                        if(dicDrawVFX.TryGetValue(listUnit[i], out GameObject value))
                        {
                            if (listUnit[i].CheckIsState(UNIT_STATE.DEATH))
                            {
                                MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "FX_Ghost_Necromancer_Boss_d_skill1_hit" : "FX_Ghost_Necromancer_Boss_b_skill1_hit", value);
                                dicDrawVFX.Remove(listUnit[i]);
                            }
                        }
                        else
                        {
                            if (!listUnit[i].CheckIsState(UNIT_STATE.DEATH))
                            {
                                GameObject objVFX = MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "FX_Ghost_Necromancer_Boss_d_skill1_hit" : "FX_Ghost_Necromancer_Boss_b_skill1_hit", listUnit[i].GetUnitCenterPos());
                                objVFX.transform.SetParent(listUnit[i].transform);
                                dicDrawVFX.Add(listUnit[i], objVFX);
                            }
                        }
                    }
                    else
                    {
                        mainModule.startSizeYMultiplier = 0.0f;
                    }
                }

                duration -= Time.deltaTime;
                if(duration <= 0.0f)
                {
                    duration += 0.5f;
                    hitCnt--;

                    int targetCnt = listUnit.Count > 3 ? 3 : listUnit.Count;

                    for (int i = 0; i < targetCnt; i++)
                    {
                        if (listUnit[i].CheckIsState(UNIT_STATE.DEATH))
                            continue;

                        drainHP += listUnit[i].UnitStat.MaxHP * (float)bossSkillData.Param[0];
                        listUnit[i].DecreaseHP(unitBase, listUnit[i].UnitStat.MaxHP * (float)bossSkillData.Param[0]);
                    }
                }

                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());
            }

            for(int i = 0; i < listUnit.Count; i++)
            {
                if (dicDrawVFX.TryGetValue(listUnit[i], out GameObject value))
                {
                    MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "FX_Ghost_Necromancer_Boss_d_skill1_hit" : "FX_Ghost_Necromancer_Boss_b_skill1_hit", value);
                    dicDrawVFX.Remove(listUnit[i]);
                }
            }

            MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "FX_Ghost_Necromancer_Boss_Bear_d_heal cast" : "FX_Ghost_Necromancer_Boss_Bear_b_heal cast", objParentVFX);
            MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C4_Final_Boss03_d") ? "FX_Ghost_Necromancer_Boss_Bear_d_heal cast" : "FX_Ghost_Necromancer_Boss_Bear_b_heal cast", objDollVFX);

            //unitBase.SetUnitAnimation("skill1_b,d_finish");

            unitBase.SetHeal(drainHP);
            if (!unitDoll.CheckIsState(UNIT_STATE.DEATH))
                unitDoll.SetHeal(drainHP);
        }

        public override void ResetSkill()
        {
            hitCnt = 0;
        }

        private async UniTaskVoid TaskCheckDoll()
        {
            while(!unitBase.CheckIsState(UNIT_STATE.DEATH))
            {
                unitBase.IsBlockedTarget = !unitDoll.CheckIsState(UNIT_STATE.DEATH);

                await UniTask.Yield();
            }
        }
    }
}
