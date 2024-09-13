using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Spine.Unity;
using Spine;

[CreateAssetMenu(fileName = "Weapon_2_MachineGun", menuName = "Weapon/2_MachineGun")]
public class Weapon_2_MachineGun : SOBase_Weapon
{
    public override void OnInitialize(WeaponSystem _weapon, UnitBase _unitbase)
    {
        _weapon.WeaponPersonalVariable = new PersonalVariable();
        _weapon.WeaponPersonalVariable.SetData(_weapon);

        PersonalVariable personal = _weapon.WeaponPersonalVariable as PersonalVariable;
        personal.SetAllyBase(_unitbase);
    }

    private class PersonalVariable : WeaponPersonalVariableInstance
    {
        private UnitBase unitBaseAlly;

        private List<UnitBase> listUnit = new List<UnitBase>();
        private UnitBase unitTarget;
        private float shootDuration;

        private bool isReload;
        private bool isRun3Op;
        private bool isRun5Op;
        private float sameTargetTimer;

        public void SetAllyBase(UnitBase _target)
        {
            unitBaseAlly = _target;
            weaponComp.SetCoolDown((float)weaponComp.WeaponBoosterData.StartCooldown);
            unitBaseAlly.SetGiveDamageEvent(OnGiveDamageAction);
        }

        public void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBaseAlly || _dmgChannel != 1)
                return;

            if (MgrBoosterSystem.Instance.DicWeapon["gear_weapon_0001"] >= 3 && MathLib.CheckPercentage((float)weaponComp.WeaponBoosterData.Params[2]))
                _victim.AddUnitEffect(UNIT_EFFECT.CC_KNOCKBACK, unitBaseAlly, _victim, new float[] { 1.0f });
        }

        public override void OnMove()
        {
            if (!IsAttack)
            {
                if (MathLib.CheckIsPosDistanceInRange(weaponComp.transform.position, unitBaseAlly.transform.position + Vector3.up * 2.0f + (unitBaseAlly.TeamNum == 0 ? Vector3.left : Vector3.right) * 1.5f, 0.1f))
                {
                    if (!weaponComp.Ska.AnimationName.Equals("idle") && !weaponComp.Ska.AnimationName.Equals("death"))
                        weaponComp.SetWeaponAnimation("idle", true);
                }
                else
                {
                    if (!weaponComp.Ska.AnimationName.Equals("walk") && !weaponComp.Ska.AnimationName.Equals("death"))
                        weaponComp.SetWeaponAnimation("walk", true);
                }
            }

            weaponComp.transform.position = Vector3.Lerp(weaponComp.transform.position, unitBaseAlly.transform.position + Vector3.up * 2.0f + (unitBaseAlly.TeamNum == 0 ? Vector3.left : Vector3.right) * 1.5f, Time.deltaTime);
        }

        public override bool CheckCanUseSkill()
        {
            UnitBase unit = MgrBattleSystem.Instance.GetNearestXEnemyUnit(unitBaseAlly);

            return !(unit is null) && (unit.transform.position.x - unitBaseAlly.transform.position.x) <= weaponComp.WeaponBoosterData.Range;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if(_animationName.Equals("reload") && IsAttack)
            {
                isReload = false;

                weaponComp.SetWeaponAnimation("skill1");
                weaponComp.PlayTimeline(0, true);
            }
        }

        public override void EventTriggerSkill()
        {
            //TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            IsAttack = true;

            shootDuration = (float)weaponComp.WeaponBoosterData.Params[0];

            isReload = false;
            isRun3Op = false;
            isRun5Op = false;

            unitTarget = null;

            weaponComp.SetWeaponAnimation("skill1");
            weaponComp.PlayTimeline(0, true);

            TaskSkill().Forget();
        }

        private async UniTaskVoid TaskSkill()
        {
            SetUnitTarget(false);

            float shootDelay = 0.0f;
            while(shootDuration > 0.0f)
            {
                await UniTask.Yield(cancellationToken: weaponComp.GetCancellationTokenOnDestroy());

                if (unitBaseAlly.CheckIsState(UNIT_STATE.DEATH))
                    break;

                shootDuration -= Time.deltaTime;
                shootDelay += Time.deltaTime;

                if (unitTarget is null || unitTarget.CheckIsState(UNIT_STATE.DEATH) || unitTarget.transform.position.x - unitBaseAlly.transform.position.x > weaponComp.WeaponBoosterData.Range) SetUnitTarget();
                else sameTargetTimer += Time.deltaTime;

                float shootPerSec;
                if (MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode || MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp) shootPerSec = 0.105f;
                else shootPerSec = 0.15f;

                if (shootDelay >= shootPerSec)
                {
                    shootDelay -= shootPerSec;

                    if (unitTarget is not null)
                    {
                        if(!isReload)
                        {
                            MgrSound.Instance.PlayOneShotSFX(isRun3Op ? "SFX_Gear_Weapon_0001_Lv5" : "SFX_Gear_Weapon_0001_a", 1.0f);

                            // 대미지 및 이펙트
                            float dmgRate = (float)weaponComp.WeaponBoosterData.Params[1];

                            if (isRun3Op) dmgRate += 0.025f;

                            if (sameTargetTimer >= 2.0f && MgrBoosterSystem.Instance.DicWeapon["gear_weapon_0001"] >= 5)
                            {
                                MgrObjectPool.Instance.ShowObj("FX_base_weapon_01_Gatling_skill1_Double shot hit_1shot", unitTarget.GetUnitCenterPos());
                                dmgRate *= 1.0f + (float)weaponComp.WeaponBoosterData.Params[5];
                            }

                            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBaseAlly, unitTarget, unitBaseAlly.GetAtkRateToDamage(dmgRate), unitBaseAlly.UnitStat.CriRate, unitBaseAlly.UnitStat.CriDmg, 1);
                            MgrObjectPool.Instance.ShowObj(isRun3Op ? "FX_base_weapon_01_Gatling_skill1_hit_1shot_3op" : "FX_base_weapon_01_Gatling_skill1_hit_1shot", unitTarget.GetUnitCenterPos());
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if(shootDuration <= 0.0f)
                {
                    if(!isRun3Op && weaponComp.WeaponOptionLevel >= 3)
                    {
                        isRun3Op = true;
                        shootDuration += (float)BCH.Database.DataManager.Instance.GetGearOptionValue("gear_weapon_0001", 2);

                        weaponComp.PlayTimeline(1, true);
                        continue;
                    }
                    if(!isRun5Op && weaponComp.WeaponOptionLevel >= 10)
                    {
                        isRun5Op = true;
                        isReload = true;
                        isRun3Op = false;

                        sameTargetTimer = 0.0f;
                        unitTarget = null;

                        shootDuration = (float)weaponComp.WeaponBoosterData.Params[0] + 0.5f;

                        weaponComp.UnitPlayableDirector.Stop();
                        weaponComp.UnitPlayableDirector.playableAsset = null;

                        MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0001_Reload", 1.0f);
                        weaponComp.SetWeaponAnimation("reload");
                        continue;
                    }
                }
            }

            weaponComp.UnitPlayableDirector.Stop();
            weaponComp.UnitPlayableDirector.playableAsset = null;

            IsAttack = false;
            weaponComp.SetCoolDown((float)weaponComp.WeaponBoosterData.Cooldown);
            weaponComp.Ska.timeScale = 1.0f;
        }

        private void SetUnitTarget(bool _isCanReload = true)
        {
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBaseAlly));

            UnitBase[] arrUnit = new UnitBase[5] { null, null, null, null, null };

            foreach(UnitBase unit in listUnit)
            {
                if (unit.transform.position.x - unitBaseAlly.transform.position.x > 17.0f)
                    continue;

                switch(unit.UnitSetting.unitType)
                {
                    case UnitType.Boss:
                        if (arrUnit[0] is not null)
                            continue;

                        arrUnit[0] = unit;
                        break;
                    case UnitType.MidBoss:
                        if (arrUnit[1] is not null)
                            continue;

                        arrUnit[1] = unit;
                        break;
                    case UnitType.Elite:
                        if (arrUnit[2] is not null)
                            continue;

                        arrUnit[2] = unit;
                        break;
                    case UnitType.EnemyUnit:
                        if (arrUnit[3] is not null)
                            continue;

                        arrUnit[3] = unit;
                        break;
                    default:
                        if (arrUnit[4] is not null)
                            continue;

                        arrUnit[4] = unit;
                        break;
                }
            }

            if (arrUnit[0] is not null) unitTarget = arrUnit[0];
            else if (arrUnit[1] is not null) unitTarget = arrUnit[1];
            else if (arrUnit[2] is not null) unitTarget = arrUnit[2];
            else if (arrUnit[3] is not null) unitTarget = arrUnit[3];
            else if (arrUnit[4] is not null) unitTarget = arrUnit[4];
            else unitTarget = null;

            sameTargetTimer = 0.0f;

            if (unitTarget is null)
                return;

            if(_isCanReload && weaponComp.WeaponOptionLevel < 1)
            {
                isReload = true;

                weaponComp.UnitPlayableDirector.Stop();
                weaponComp.UnitPlayableDirector.playableAsset = null;

                MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0001_Reload", 1.0f);
                weaponComp.SetWeaponAnimation("reload");
            }
        }
    }
}
