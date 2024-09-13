using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C3_Final_Boss02_ac_S2", menuName = "UnitSkillEvent/Monster/C3_Final_Boss02_ac_S2")]
public class C3_Final_Boss02_ac_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        private GameObject objVFX = null;
        private Vector3 v3Pos;
        private bool isRunningSkill = false;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C3_Final_Boss02_ac_s2_f", 0.75f);
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL) && !isRunningSkill)
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
            UnitBase target = MgrBattleSystem.Instance.GetNearestXEnemyUnit(unitBase);
            if (target is null)
                return;

            isRunningSkill = true;

            v3Pos = target.transform.position;
            objVFX = MgrObjectPool.Instance.ShowObj("FX_Dullahan_boss_a_skill2_ice", v3Pos);
            objVFX.transform.GetChild(0).GetChild(0).GetComponent<SkeletonAnimation>().skeleton.SetSkin(unitBase.UnitIndex.Equals("C3_Final_Boss02_c") ? "c" : "a");
            PlayableDirector pd = objVFX.transform.GetChild(0).GetComponent<PlayableDirector>();
            pd.Play();

            TaskSkill(pd.duration).Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2_a,c");
            unitBase.PlayTimeline();
        }

        private List<UnitBase> listAlreadyEffect = new List<UnitBase>();
        private async UniTaskVoid TaskSkill(double _duration)
        {
            float tickTimer = 0.0f;
            float allyTickTimer = -0.5f;
            float duration = (float)_duration;

            listAlreadyEffect.Clear();

            bool isFirstHit = false;
            while (duration > 0.0f)
            {
                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());

                duration -= Time.deltaTime;
                tickTimer += Time.deltaTime;
                allyTickTimer += Time.deltaTime;

                if(tickTimer >= 1.0f)
                {
                    if (!isFirstHit)
                    {
                        isFirstHit = true;
                        MgrCamera.Instance.SetCameraShake(0.3f, 1.0f, 30);
                    }

                    MgrSound.Instance.PlayOneShotSFX("SFX_C3_Final_Boss02_ac_s2_d", 0.5f);

                    tickTimer -= 1.0f;

                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, v3Pos, (float)bossSkillData.Param[0]));

                    foreach(UnitBase unit in listUnit)
                        MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[5]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
                }
                if(allyTickTimer >= 0.5f)
                {
                    allyTickTimer -= 0.5f;

                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, v3Pos, (float)bossSkillData.Param[0], _isAlly: true));

                    foreach (UnitBase unit in listUnit)
                    {
                        unit.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[2]), unitBase, unit, new float[] { (float)bossSkillData.Param[4], 0.6f });
                        unit.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[3]), unitBase, unit, new float[] { (float)bossSkillData.Param[4], 0.6f });

                        if (listAlreadyEffect.Contains(unit))
                            continue;

                        listAlreadyEffect.Add(unit);
                        MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", unit.transform.position).transform.SetParent(unit.transform);

                        MgrSound.Instance.PlayOneShotSFX("SFX_Buff_ab", 0.5f);
                    }
                }
            }

            MgrObjectPool.Instance.HideObj("FX_Dullahan_boss_a_skill2_ice", objVFX);
            objVFX = null;

            isRunningSkill = false;
        }
    }
}
