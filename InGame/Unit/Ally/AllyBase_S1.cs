using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;
using Spine;
using BCH.Database;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_AllyBase_S1", menuName = "UnitSkillEvent/AllyBase_S1")]
public class AllyBase_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetTakeDamageEvent(personal.OnDamagedAction);
        MgrInGameEvent.Instance.AddBoosterEvent(personal.OnBoosterUpgradeAction);
        personal.InitGearData();

        _unitBase.UnitStat.Range = MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp ? 17.5f : 12.0f;
        _unitBase.UnitStat.MoveSpeed = 2.0f;

        if (MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode)
            _unitBase.UnitStat.MoveSpeed *= 1.3f;

        // 무기는 시작 레벨이 1 이므로 임시로 할당 (나중에 장착한 무기 외 리스트에서 제외하며 startLevel 로 지정하는 로직으로 변경 예정)
        MgrBattleSystem.Instance.SetAllyHPBar(_unitBase, _unitBase.UnitStat.HP, _unitBase.UnitStat.MaxHP, _unitBase.Shield);

        if(_unitBase.TeamNum == 0)
        {
            if (MgrInGameUserData.Instance is not null)
            {
                if (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter) MgrBattleSystem.Instance.WeaponSys.SetWeapon("gear_weapon_0000", 10, _unitBase);
                else
                {
                    UserGear gearWeapon = DataManager.Instance.GetUsingGearInfo(0);
                    if (!(gearWeapon is null)) MgrBattleSystem.Instance.WeaponSys.SetWeapon(gearWeapon.gearId, gearWeapon.gearRarity, _unitBase);
                    else MgrBoosterSystem.Instance.RemoveUpgradeList(BoosterType.Weapon);
                }
            }
            else MgrBattleSystem.Instance.WeaponSys.SetWeapon("gear_weapon_0003", 10, _unitBase);
        }
        else
        {
            UserGear gearWeapon = DataManager.Instance.GetUsingGearInfo(0);
            if (gearWeapon is not null)
                MgrBattleSystem.Instance.WeaponSysOppo.SetWeapon($"gear_weapon_000{Random.Range(0, 5)}", gearWeapon.gearRarity, _unitBase);
        }
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private float healPassiveTimer;
        private UserGear gearCore;

        private List<UnitBase> listUnit = new List<UnitBase>();

        public void InitGearData()
        {
            gearCore = DataManager.Instance.GetUsingGearInfo(1);
        }

        public void OnDamagedAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if(!MgrBattleSystem.Instance.GlobalOption.isGearCoreActive_002 && gearCore is not null && gearCore.gearId.Equals("gear_core_0002") && gearCore.gearRarity >= 1)
            {
                MgrBattleSystem.Instance.GlobalOption.isGearCoreActive_002 = true;
                _victim.AddUnitEffect(UNIT_EFFECT.BUFF_SHIELD, unitBase, unitBase, new float[] { (float)DataManager.Instance.GetGearOptionValue("gear_core_0002", 0) * unitBase.UnitStat.MaxHP, 0.0f });
            }

            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return;

            if (MgrBattleSystem.Instance.GameMode != GAME_MODE.Pvp && (_attacker.transform.position - _victim.transform.position).magnitude <= 5.0f && unitBase.CheckIsState(UNIT_STATE.IDLE) && !_attacker.CheckIsState(UNIT_STATE.DEATH) && _dmgChannel > -1)
                unitBase.SetUnitUseSkill(unitBase.soSkillEvent[0]);
        }

        public void OnBoosterUpgradeAction(string _index)
        {
            switch (_index)
            {
                case "skill_passive_002":
                    float prevMaxHP = unitBase.UnitStat.MaxHP;
                    unitBase.UnitStat.MaxHP = unitBase.DefaultMaxHP * (1.0f + (float)BCH.Database.DataManager.Instance.GetBoosterSkillData($"{_index}_{MgrBoosterSystem.Instance.DicEtc[_index] - 1}").Params[0]);
                    unitBase.UnitStat.HP += unitBase.UnitStat.MaxHP - prevMaxHP;
                    MgrBattleSystem.Instance.SetAllyHPBar(unitBase, unitBase.UnitStat.HP, unitBase.UnitStat.MaxHP, unitBase.Shield);
                    break;
                case "skill_passive_004":
                    if (MgrBoosterSystem.Instance.DicEtc[_index] == 1)
                    {
                        healPassiveTimer = (float)DataManager.Instance.GetBoosterSkillData($"skill_passive_004_{MgrBoosterSystem.Instance.DicEtc["skill_passive_004"] - 1}").Cooldown;
                        TaskHealPassive().Forget();
                    }
                    break;
                case "Etc_2":
                    MgrObjectPool.Instance.ShowObj("FX_PC-Heal", unitBase.GetUnitCenterPos());
                    unitBase.SetHeal(unitBase.UnitStat.MaxHP * 0.2f);
                    break;
                default:
                    break;
            }
        }

        public override bool CheckCanUseSkill()
        {
            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {

        }

        public override void EventTriggerSkill()
        {

        }

        public override void OnSkill()
        {
            if (gearCore is not null && gearCore.gearId.Equals("gear_core_0004") && gearCore.gearRarity >= 3)
            {
                Vector3 v3Pos = unitBase.transform.position + Vector3.right * 4.0f;
                MgrObjectPool.Instance.ShowObj("FX_Pillar Fire", v3Pos);
                MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Core_0004_A", 1.0f);

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, v3Pos, 3.25f));

                foreach (UnitBase unit in listUnit)
                    MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 2)), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);

                if (gearCore.gearRarity >= 10)
                    TaskPillar().Forget();
            }

            TaskBackStep().Forget();
        }

        private async UniTaskVoid TaskPillar()
        {
            await UniTask.Delay(200, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            Vector3 v3Pos = unitBase.transform.position + Vector3.right * 4.0f;
            MgrObjectPool.Instance.ShowObj("FX_Pillar Fire", v3Pos);
            MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Core_0004_A", 1.0f);

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, v3Pos, 3.25f));

            foreach (UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 2)), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);
        }

        private async UniTaskVoid TaskHealPassive()
        {
            while (MgrBattleSystem.Instance.isStageStart)
            {
                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());

                healPassiveTimer -= Time.deltaTime;
                if (healPassiveTimer <= 0.0f)
                {
                    if(!unitBase.CheckIsState(UNIT_STATE.DEATH))
                    {
                        unitBase.SetHeal(unitBase.UnitStat.MaxHP * (float)DataManager.Instance.GetBoosterSkillData($"skill_passive_004_{MgrBoosterSystem.Instance.DicEtc["skill_passive_004"] - 1}").Params[0]);
                        MgrBattleSystem.Instance.SetAllyHPBar(unitBase, unitBase.UnitStat.HP, unitBase.UnitStat.MaxHP, unitBase.Shield);
                        MgrObjectPool.Instance.ShowObj("FX_Buff_Dot Heal", unitBase.GetUnitCenterPos());
                    }
                    healPassiveTimer = (float)DataManager.Instance.GetBoosterSkillData($"skill_passive_004_{MgrBoosterSystem.Instance.DicEtc["skill_passive_004"] - 1}").Cooldown;
                }
            }
        }

        private int backStepSFXChannel = -1;
        private async UniTaskVoid TaskBackStep()
        {
            unitBase.Ska.AnimationState.SetAnimation(0, "fly_ready", false);

            unitBase.Ska.AnimationState.Complete -= OnComplete;
            unitBase.Ska.AnimationState.Complete += OnComplete;

            float backStepTimer = 5.0f;
            bool isCancelled = false;

            float moveSpeed = unitBase.UnitStat.MoveSpeed;
            if (!(gearCore is null) && gearCore.gearId.Equals("gear_core_0004"))
            {
                if(gearCore.gearRarity >= 1) moveSpeed *= 1.0f + (float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 0);
                //if(gearCore.gearRarity >= 10) backStepTimer *= 1.0f + (float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 4);
            }

            while (backStepTimer > 0.0f)
            {
                await UniTask.Yield(unitBase.GetCancellationTokenOnDestroy());
                backStepTimer -= Time.deltaTime;

                unitBase.transform.position += moveSpeed * Time.deltaTime * (unitBase.TeamNum == 0 ? Vector3.left : Vector3.right);

                if(unitBase.CheckIsState(UNIT_STATE.DEATH) || unitBase.CurrUnitSkill != soSkillEvent || !MgrBattleSystem.Instance.isStageStart)
                {
                    unitBase.Ska.AnimationState.Complete -= OnComplete;
                    isCancelled = true;
                    break;
                }
            }

            MgrSound.Instance.StopSFX("SFX_Base_Backstep", backStepSFXChannel);

            if(!isCancelled || !MgrBattleSystem.Instance.isStageStart)
                unitBase.SetUnitState(UNIT_STATE.IDLE);

            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), 2.0f);
        }

        private void OnComplete(TrackEntry trackEntry)
        {
            string animationName = trackEntry.Animation.Name;
            if(animationName.Equals("fly_ready"))
            {
                unitBase.Ska.AnimationState.Complete -= OnComplete;

                if(!unitBase.CheckIsState(UNIT_STATE.DEATH))
                {
                    backStepSFXChannel = MgrSound.Instance.PlaySFX("SFX_Base_Backstep", 0.25f, true);
                    unitBase.Ska.AnimationState.SetAnimation(0, "fly_move", true);
                }
            }
        }
    }
}
