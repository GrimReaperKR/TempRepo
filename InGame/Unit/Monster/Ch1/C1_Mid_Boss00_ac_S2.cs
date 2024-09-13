using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_C1_Mid_Boss00_ac_S2", menuName = "UnitSkillEvent/Monster/C1_Mid_Boss00_ac_S2")]
public class C1_Mid_Boss00_ac_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_C1_Mid_Boss00_ac_s2_3", 1.0f);

            _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)bossSkillData.Param[3]), _attacker, _victim, new float[] { (float)bossSkillData.Param[2] });
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL) || MgrBattleSystem.Instance.GameMode == GAME_MODE.Training)
                return false;

            if (unitBase.EnemyTarget && MgrBattleSystem.Instance.CheckIsEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, 2.0f)) return true;
            else
            {
                if (unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    return false;

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, 2.0f));
                UnitBase unitInEllipse = listUnit.Count > 0 ? listUnit[0] : null;
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
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, (float)bossSkillData.Param[0]));

            foreach (UnitBase unit in listUnit)
            {
                MgrObjectPool.Instance.ShowObj(unitBase.UnitIndex.Equals("C1_Mid_Boss00_c") ? "FX_monster rat_mid boss_c_skill-hit" : "FX_monster rat_mid boss_a_skill-hit", unit.GetUnitCenterPos());

                if (unit == unitBase.EnemyTarget) MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
                else MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)bossSkillData.Param[1]) * 0.5f, unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
            }

            MgrCamera.Instance.SetCameraShake(0.35f, 0.4f, 30);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2_a,c");
            unitBase.PlayTimeline();

            if(MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter && MgrBattleSystem.Instance.TutorialStep == 15)
                TaskTutorial().Forget();
        }

        private async UniTaskVoid TaskTutorial()
        {
            if (MgrBoosterSystem.Instance.IsOpenBoosterCard)
                await UniTask.WaitUntil(() => !MgrBoosterSystem.Instance.IsOpenBoosterCard, cancellationToken:unitBase.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.SetTutorialTimeScale(true);
            MgrBattleSystem.Instance.ShowTutorialTextUI(16, ANCHOR_TYPE.BOTTOM_LEFT, new Vector2(0.0f, 0.5f), new Vector2(330.0f, 420.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(175.0f, 425.0f), new Vector2(175.0f, 175.0f), ANCHOR_TYPE.BOTTOM_LEFT, 1);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.ResetTutorialSideSkillCoolDown();

            MgrBattleSystem.Instance.TutorialStep = 16;
            MgrBattleSystem.Instance.ShowTutorialFingerUI(new Vector2(175.0f, 495.0f), ANCHOR_TYPE.BOTTOM_LEFT, 4);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);
        }
    }
}
