using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;
using BCH.Database;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_AllyBase_S2", menuName = "UnitSkillEvent/AllyBase_S2")]
public class AllyBase_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        MgrInGameEvent.Instance.AddActiveSkillEvent(personal.OnActiveSkillAction);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private string skillIndex;

        private GameObject objVFX = null;

        private int atkCnt;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase)
                return;

            if(_dmgChannel == 10)
                MgrSound.Instance.PlayOneShotSFX("SFX_Base_Skill1_c", 1.0f);

            UserGear gearCore = DataManager.Instance.GetUsingGearInfo(1);
            if ((_victim.UnitSetting.unitType == UnitType.Monster || _victim.UnitSetting.unitType == UnitType.EnemyUnit) && gearCore is not null && gearCore.gearId.Equals("gear_core_0003") && gearCore.gearRarity >= 1 && MathLib.CheckPercentage((float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 0)))
                _victim.AddUnitEffect(UNIT_EFFECT.ETC_INSTANT_DEATH, _attacker, _victim, null);
        }

        public override bool CheckCanUseSkill()
        {
            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill"))
                return;

            if(!(objVFX is null))
            {
                MgrObjectPool.Instance.HideObj("AllyBase_Laser_1", objVFX);
                objVFX = null;
            }

            unitBase.SetUnitState(UNIT_STATE.IDLE);

            if(MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter && MgrBattleSystem.Instance.TutorialStep == 17)
                TaskTutorial().Forget();
        }

        public override void EventTriggerSkill()
        {
            switch(skillIndex)
            {
                case "Catbot_skill_001":
                    atkCnt++;

                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase, _isContainBlockedTarget: true));

                    if (atkCnt == 1)
                    {
                        MgrSound.Instance.PlayOneShotSFX("SFX_Base_Skill1_a", 1.0f);
                        TaskLaserSFX().Forget();
                    }

                    foreach (UnitBase unit in listUnit)
                    {
                        if (atkCnt == 1) unit.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitBase.CatBotSkillData.Params[1]), unit, unit, new float[] { 5.0f });
                        else MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)unitBase.CatBotSkillData.Params[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 10);
                    }

                    break;
                default:
                    break;
            }
        }

        private async UniTaskVoid TaskLaserSFX()
        {
            await UniTask.Delay(500, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            if(!unitBase.CheckIsState(UNIT_STATE.DEATH))
                MgrSound.Instance.PlayOneShotSFX("SFX_Base_Skill1_b", 1.0f);
        }

        public override void OnSkill()
        {
            if(unitBase.CheckIsState(UNIT_STATE.DEATH))
            {
                skillIndex = string.Empty;
                return;
            }

            unitBase.SetUnitAnimation("skill1");

            switch (skillIndex)
            {
                // 적군 전체 넉백 대미지
                case "Catbot_skill_001":
                    atkCnt = 0;

                    unitBase.PlayTimeline();

                    objVFX = MgrObjectPool.Instance.ShowObj("AllyBase_Laser_1", unitBase.transform.position);
                    objVFX.transform.SetParent(unitBase.transform);
                    objVFX.GetComponent<SkeletonAnimation>().AnimationState.SetAnimation(0, "skill1", false);
                    TaskAct_0().Forget();
                    break;
                default:
                    skillIndex = string.Empty;
                    break;
            }
        }

        public void OnActiveSkillAction(string _index)
        {
            switch (_index)
            {
                case "Catbot_skill_001":
                    skillIndex = _index;
                    unitBase.SetUnitUseSkill(soSkillEvent);
                    break;
                case "skill_active_000":
                case "skill_active_001":
                case "skill_active_004":
                case "skill_active_005":
                case "skill_active_007":
                    RunActiveSkill(_index);
                    break;
                default:
                    skillIndex = string.Empty;
                    break;
            }
        }

        private async UniTaskVoid TaskAct_0()
        {
            await UniTask.Delay(2500, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            if (!(objVFX is null))
            {
                MgrObjectPool.Instance.HideObj("AllyBase_Laser_1", objVFX);
                objVFX = null;
            }
        }

        private void RunActiveSkill(string _skillIndex)
        {
            BoosterSkill skillData = null;

            switch (_skillIndex)
            {
                // 아군 쿨초기화
                case "skill_active_000":
                    float useHP = unitBase.UnitStat.MaxHP * (float)DataManager.Instance.GetBoosterSkillData($"{_skillIndex}_{MgrBoosterSystem.Instance.DicSkill[_skillIndex] - 1}").Params[0];
                    float remainHP = unitBase.UnitStat.HP - useHP;
                    unitBase.UnitStat.HP = remainHP > 1.0f ? remainHP : 1.0f;
                    MgrBattleSystem.Instance.SetAllyHPBar(unitBase, unitBase.UnitStat.HP, unitBase.UnitStat.MaxHP, unitBase.Shield);

                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase, true));
                    listUnit.Remove(unitBase);

                    foreach (UnitBase unit in listUnit)
                    {
                        unit.SetUnitSkillCoolDown(0, 0.0f);
                        unit.SetUnitSkillCoolDown(1, 0.0f);

                        MgrSound.Instance.PlayOneShotSFX("SFX_Unit_Summon_Cooldown", 1.0f);
                        MgrObjectPool.Instance.ShowObj("FX_Buff_Cooltime-Return", unit.transform.position).transform.SetParent(unit.transform);
                    }

                    MgrBattleSystem.Instance.ResetAllUnitSpawnCooldown();
                    break;
                // 아군 불사 효과
                case "skill_active_001":
                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase, true));
                    listUnit.Remove(unitBase);

                    skillData = DataManager.Instance.GetBoosterSkillData($"skill_active_001_{MgrBoosterSystem.Instance.DicSkill["skill_active_001"] - 1}");
                    foreach (UnitBase unit in listUnit)
                    {
                        unit.AddUnitEffect(UNIT_EFFECT.BUFF_IMMORTALITY, unitBase, unit, new float[] { (float)skillData.Params[1], (float)skillData.Params[0] }, false);
                        MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", unit.transform.position).transform.SetParent(unit.transform);
                    }

                    if(listUnit.Count > 0)
                        MgrSound.Instance.PlayOneShotSFX("SFX_Buff_ab", 1.0f);

                    break;
                // 아군 공격력 증가 (월드)
                case "skill_active_004":
                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase, true));
                    listUnit.Remove(unitBase);

                    foreach (UnitBase unit in listUnit)
                    {
                        MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", unit.transform.position).transform.SetParent(unit.transform);
                        MgrObjectPool.Instance.ShowObj("tmpDmg", unit.GetUnitCenterPos()).GetComponent<DamageText>().SetDamageText(0.0f, _customText: MgrInGameData.Instance.DicLocalizationCSVData["Booster/Active_Nm_004"]["korea"]);
                    }

                    if (listUnit.Count > 0)
                        MgrSound.Instance.PlayOneShotSFX("SFX_Buff_ab", 1.0f);

                    TaskAct5Skill().Forget();
                    break;
                // 아군 쉴드 버프 부여
                case "skill_active_005":
                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBase, true));
                    listUnit.Remove(unitBase);

                    skillData = DataManager.Instance.GetBoosterSkillData($"{_skillIndex}_{MgrBoosterSystem.Instance.DicSkill[_skillIndex] - 1}");

                    foreach (UnitBase unit in listUnit)
                        unit.AddUnitEffect(UNIT_EFFECT.BUFF_SHIELD, unitBase, unit, new float[] { unit.UnitStat.MaxHP * (float)skillData.Params[1], (float)skillData.Params[0] }, false);

                    break;
                // 긴급 회복
                case "skill_active_007":
                    float healAmount = (float)DataManager.Instance.GetBoosterSkillData($"{_skillIndex}_{MgrBoosterSystem.Instance.DicSkill[_skillIndex] - 1}").Params[0];

                    MgrObjectPool.Instance.ShowObj("FX_PC-Heal", unitBase.GetUnitCenterPos());
                    unitBase.SetHeal(unitBase.UnitStat.MaxHP * healAmount);

                    TaskAct8Skill().Forget();
                    break;
                default:
                    break;
            }
        }

        private async UniTaskVoid TaskAct5Skill()
        {
            MgrBattleSystem.Instance.GlobalOption.isAllyActiveSkill_5 = true;
            await UniTask.Delay(System.TimeSpan.FromSeconds((float)DataManager.Instance.GetBoosterSkillData($"skill_active_004_{MgrBoosterSystem.Instance.DicSkill["skill_active_004"] - 1}").Params[0]), cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            MgrBattleSystem.Instance.GlobalOption.isAllyActiveSkill_5 = false;
        }

        private async UniTaskVoid TaskAct8Skill()
        {
            int healCnt = 10;

            float healAmount = (float)DataManager.Instance.GetBoosterSkillData($"skill_active_007_{MgrBoosterSystem.Instance.DicSkill["skill_active_007"] - 1}").Params[1];
            while (healCnt > 0)
            {
                await UniTask.Delay(1000, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

                if (unitBase.CheckIsState(UNIT_STATE.DEATH))
                    break;

                healCnt--;

                MgrObjectPool.Instance.ShowObj("FX_Buff_Dot Heal", unitBase.GetUnitCenterPos());
                unitBase.SetHeal(unitBase.UnitStat.MaxHP * healAmount);
            }
        }

        private async UniTaskVoid TaskTutorial()
        {
            MgrBattleSystem.Instance.SetTutorialTimeScale(true);
            MgrBattleSystem.Instance.ShowTutorialTextUI(17, ANCHOR_TYPE.BOTTOM_LEFT, new Vector2(0.0f, 0.5f), new Vector2(330.0f, 420.0f));
            MgrBattleSystem.Instance.ShowTutorialMaskBackGround(new Vector2(175.0f, 425.0f), new Vector2(175.0f, 175.0f), ANCHOR_TYPE.BOTTOM_LEFT, 1);
            MgrBattleSystem.Instance.ToggleTutorialUI(true);

            await UniTask.WaitUntil(() => !MgrBattleSystem.Instance.ObjCanvTutorial.activeSelf, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            MgrBattleSystem.Instance.TutorialStep = 18;
            MgrBattleSystem.Instance.SetTutorialTimeScale(false);
        }
    }
}
