using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C1_Final_Boss02_bd_S2", menuName = "UnitSkillEvent/Monster/C1_Final_Boss02_bd_S2")]
public class C1_Final_Boss02_bd_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private List<UnitBase> listHitUnit = new List<UnitBase>();

        private Vector3 v3StartPos;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C1_Final_Boss02_bd_s3_3", 1.0f);

            _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[3]), _attacker, _victim, new float[] { (float)bossSkillData.Param[2] });
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL))
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
            MgrCamera.Instance.SetCameraShake(0.35f, 1.0f, 30);

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase));
            listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());

            UnitBase target = null;
            if (listUnit.Count == 0) target = MgrBattleSystem.Instance.GetAllyBase();
            else target = listUnit[listUnit.Count - 1];

            if (target is null)
                return;

            v3StartPos = target.transform.position;

            TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2_b,d");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskSkill()
        {
            listHitUnit.Clear();

            int createCnt = 0;
            bool isMaxY = false, isMinY = false;

            CreateRootVFX(v3StartPos);
            SetRootDamage(v3StartPos);

            while (!(isMaxY && isMinY))
            {
                await UniTask.Delay(50, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

                createCnt++;

                if (!isMaxY)
                {
                    Vector3 v3MaxPos = v3StartPos + (createCnt * 1.25f) * Vector3.up;
                    CreateRootVFX(v3MaxPos);
                    SetRootDamage(v3MaxPos);

                    if (v3MaxPos.y >= -1.0f)
                        isMaxY = true;
                }

                if (!isMinY)
                {
                    Vector3 v3MinPos = v3StartPos + (createCnt * 1.25f) * Vector3.down;
                    CreateRootVFX(v3MinPos);
                    SetRootDamage(v3MinPos);

                    if (v3MinPos.y <= -3.25f)
                        isMinY = true;
                }
            }
        }

        private void CreateRootVFX(Vector3 _v3Pos)
        {
            GameObject objVFX = MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C1_Final_Boss02_d") ? "FX_Ent_Boss_Root_C,D" : "FX_Ent_Boss_Root_A,B", _v3Pos);
            objVFX.transform.rotation = Quaternion.Euler(0.0f, Random.Range(0, 2) == 0 ? -180.0f : 0.0f, 0.0f);
            objVFX.transform.GetChild(0).localScale = new Vector3(Random.Range(0, 2) == 0 ? 1.0f : -1.0f, 1.0f, 1.0f);
            objVFX.transform.GetChild(0).GetComponent<SkeletonAnimation>().AnimationState.SetAnimation(0, "skill1", false);
        }

        private void SetRootDamage(Vector3 _v3Pos)
        {
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, _v3Pos, (float)bossSkillData.Range));

            foreach (UnitBase unit in listUnit)
            {
                if (listHitUnit.Contains(unit))
                    continue;

                listHitUnit.Add(unit);
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
            }
        }
    }
}
