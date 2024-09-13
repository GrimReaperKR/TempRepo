using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C3_Final_Boss02_ac_S3", menuName = "UnitSkillEvent/Monster/C3_Final_Boss02_ac_S3")]
public class C3_Final_Boss02_ac_S3 : SOBase_UnitSkillEvent
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
        private bool isRunningSkill = false;

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL) && !isRunningSkill && unitBase.UnitStat.HP / unitBase.UnitStat.MaxHP <= 0.6f)
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
            isRunningSkill = true;

            objVFX = MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C3_Final_Boss02_c") ? "FX_Dullahan_boss_c_skill3_shield" : "FX_Dullahan_boss_a_skill3_shield", unitBase.transform.position);
            objVFX.transform.SetParent(unitBase.transform);
            unitBase.AddUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, null, unitBase, new float[] { (float)bossSkillData.Param[1], 0.0f }, false);

            TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill3_a,c");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskSkill()
        {
            float tickDuration = 0.0f;
            while(!unitBase.CheckIsState(UNIT_STATE.DEATH))
            {
                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());

                tickDuration += Time.deltaTime;
                if(tickDuration >= (float)bossSkillData.Param[2])
                {
                    tickDuration -= (float)bossSkillData.Param[2];

                    MgrSound.Instance.PlayOneShotSFX("SFX_C3_Final_Boss02_ac_s3_2", 1.0f);

                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, (float)bossSkillData.Param[0]));

                    foreach (UnitBase unit in listUnit)
                    {
                        if(MathLib.CheckPercentage((float)bossSkillData.Param[3]))
                            unit.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[5]), unitBase, unit, new float[] { 0.2f, (float)bossSkillData.Param[4] });
                    }
                }
            }

            MgrObjectPool.Instance.HideObj(unitBase.UnitIndex.Equals("C3_Final_Boss02_c") ? "FX_Dullahan_boss_c_skill3_shield" : "FX_Dullahan_boss_a_skill3_shield", objVFX);
            objVFX = null;
        }
    }
}
