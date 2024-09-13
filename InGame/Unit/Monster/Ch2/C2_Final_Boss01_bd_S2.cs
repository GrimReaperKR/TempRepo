using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C2_Final_Boss01_bd_S2", menuName = "UnitSkillEvent/Monster/C2_Final_Boss01_bd_S2")]
public class C2_Final_Boss01_bd_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private UnitBase[] unitSummon = new UnitBase[5];
        private bool[] isSpawn = new bool[5];
        private List<int> listSpawn = new List<int>();

        public void OnSummonDeath(UnitBase _attacker, UnitBase _victim, int _dmgChannel)
        {
            _attacker.OnKillEvent(unitBase, _dmgChannel);

            _victim.UnitPlayableDirector.Stop();
            _victim.UnitPlayableDirector.playableAsset = null;

            for(int i = 0; i < unitSummon.Length; i++)
            {
                if(unitSummon[i] == _victim)
                {
                    isSpawn[i] = false;
                    unitSummon[i] = null;

                    break;
                }
            }

            _victim.OnDefaultDeath(_attacker, _victim, _dmgChannel);
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL) && MgrBattleSystem.Instance.GetEnemyUnitInSameIndex(unitBase, unitBase.UnitIndex.Equals("C2_Final_Boss01_d") ? "C2_Final_Boss01_d_s" : "C2_Final_Boss01_b_s", true).Count < 3)
                return true;

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
            int spawnCnt = MgrBattleSystem.Instance.GetEnemyUnitInSameIndex(unitBase, unitBase.UnitIndex.Equals("C2_Final_Boss01_d") ? "C2_Final_Boss01_d_s" : "C2_Final_Boss01_b_s", true).Count;

            Vector3 v3Pos = unitBase.transform.position + Vector3.right * 4.0f;

            int spawnIndex = -1;

            listSpawn.Clear();
            if (spawnCnt >= 2 && !isSpawn[0]) spawnIndex = 0;
            else
            {
                for(int i = 1; i < 5; i++)
                {
                    if (!isSpawn[i])
                        listSpawn.Add(i);
                }
                listSpawn.Shuffle();

                spawnIndex = listSpawn[0];
            }
            v3Pos.y = spawnIndex * -0.8f;
            v3Pos.z = v3Pos.y * 0.01f;

            UnitBase spawnUnit = MgrUnitPool.Instance.ShowEnemyMonsterObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_d") ? "C2_Final_Boss01_d_s" : "C2_Final_Boss01_b_s", unitBase.TeamNum, v3Pos, true, unitBase).GetComponent<UnitBase>();
            spawnUnit.SetUnitState(UNIT_STATE.DEATH, true, true);
            spawnUnit.SetUnitAnimation("summon");
            spawnUnit.SetDeathEvent(OnSummonDeath);

            unitSummon[spawnIndex] = spawnUnit;
            isSpawn[spawnIndex] = true;

            MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C2_Final_Boss01_d") ? "FX_Dust_Summon_D" : "FX_Dust_Summon_B", v3Pos);

            MgrSound.Instance.PlayOneShotSFX("SFX_C2_Final_Boss01_bd_s2_Turret_a", 1.0f);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2_b,d");
            unitBase.PlayTimeline();
        }
    }
}
