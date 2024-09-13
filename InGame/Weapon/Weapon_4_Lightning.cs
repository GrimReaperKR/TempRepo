using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Spine.Unity;
using Spine;

[CreateAssetMenu(fileName = "Weapon_4_Lightning", menuName = "Weapon/4_Lightning")]
public class Weapon_4_Lightning : SOBase_Weapon
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
        private Vector3 v3LightningPos;

        private int atkCnt;

        public void SetAllyBase(UnitBase _target)
        {
            unitBaseAlly = _target;
            unitBaseAlly.SetGiveDamageEvent(OnGiveDamageAction);
            weaponComp.Ska.GetComponent<MeshRenderer>().sortingLayerName = "Unit";
            weaponComp.SetCoolDown((float)weaponComp.WeaponBoosterData.StartCooldown);
        }

        public void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBaseAlly || _dmgChannel != 1)
                return;

            if (unitTarget == _victim && weaponComp.WeaponOptionLevel >= 1)
                _victim.AddUnitEffect(UNIT_EFFECT.CC_STUN, unitBaseAlly, _victim, new float[] { 3.0f });

            if(unitTarget != _victim && weaponComp.WeaponOptionLevel >= 10 && MathLib.CheckPercentage((float)BCH.Database.DataManager.Instance.GetGearOptionValue("gear_weapon_0003", 4)))
                _victim.AddUnitEffect(UNIT_EFFECT.CC_SHOCK, unitBaseAlly, _victim, new float[] { 3.0f });
        }

        public override void OnMove()
        {
            Vector3 v3End = unitBaseAlly.transform.position + Vector3.down * 0.5f + (unitBaseAlly.TeamNum == 0 ? Vector3.left : Vector3.right) * 0.5f;
            v3End.z = v3End.y * 0.01f;
            if (!IsAttack)
            {
                if (MathLib.CheckIsPosDistanceInRange(weaponComp.transform.position, v3End, 0.1f))
                {
                    if (!weaponComp.Ska.AnimationName.Equals("idle") && !weaponComp.Ska.AnimationName.Equals("death"))
                        weaponComp.SetWeaponAnimation("idle", true);
                }
                else
                {
                    if (!weaponComp.Ska.AnimationName.Equals("walk") && !weaponComp.Ska.AnimationName.Equals("death"))
                        weaponComp.SetWeaponAnimation("walk", true);
                }

                //Vector3 v3Dir = (v3End - weaponComp.transform.position).normalized;
                //float xValue = v3Dir.x < 0.0f ? -1.0f : v3Dir.x > 0.0f ? 1.0f : weaponComp.transform.localScale.x;
                //weaponComp.transform.localScale = new Vector3(xValue, 1.0f, 1.0f);

                weaponComp.transform.position = Vector3.Lerp(weaponComp.transform.position, v3End, Time.deltaTime * 3.0f);
            }
        }

        public override bool CheckCanUseSkill() => !(MgrBattleSystem.Instance.GetNearestXEnemyUnit(unitBaseAlly) is null);

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill"))
                return;

            IsAttack = false;

            float cooldown = (float)weaponComp.WeaponBoosterData.Cooldown;
            if (weaponComp.WeaponOptionLevel >= 3)
                cooldown += (float)BCH.Database.DataManager.Instance.GetGearOptionValue("gear_weapon_0003", 2);

            weaponComp.SetCoolDown(cooldown);
            weaponComp.Ska.timeScale = 1.0f;
        }

        public override void EventTriggerSkill()
        {
            atkCnt++;

            if(atkCnt == 1)
            {
                unitTarget = MgrBattleSystem.Instance.GetHighestAtkEnemyUnit(unitBaseAlly);
                if(!(unitTarget is null))
                {
                    v3LightningPos = unitTarget.transform.position;
                    v3LightningPos.y = -2.25f;

                    MgrObjectPool.Instance.ShowObj(MgrBoosterSystem.Instance.DicWeapon["gear_weapon_0003"] >= 3 ? "FX_base_weapon_05_lightning_skill1_Lightning_LV.3" : "FX_base_weapon_05_lightning_skill1_Lightning", v3LightningPos + Vector3.up * 1.75f);
                }
            }
            if(atkCnt == 2 && !v3LightningPos.Equals(Vector3.zero))
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0003_b", 1.0f);

                MgrObjectPool.Instance.ShowObj("FX_base_weapon_05_lightning_target-hit", unitTarget.GetUnitCenterPos());
                MgrObjectPool.Instance.ShowObj(MgrBoosterSystem.Instance.DicWeapon["gear_weapon_0003"] >= 3 ? "FX_base_weapon_05_lightning_skill1_zone_LV.3" : "FX_base_weapon_05_lightning_skill1_zone", v3LightningPos);
            }
            
            if(atkCnt >= 2 && !v3LightningPos.Equals(Vector3.zero))
            {
                float resultDmgRate = (float)weaponComp.WeaponBoosterData.Params[1];
                float maxXDist = (float)weaponComp.WeaponBoosterData.Params[0];

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBaseAlly));

                foreach (UnitBase unit in listUnit)
                {
                    float xDist = unit.transform.position.x - v3LightningPos.x;
                    xDist = xDist < 0.0f ? -xDist : xDist;

                    if (xDist > maxXDist * 0.5f)
                        continue;
                    
                    MgrObjectPool.Instance.ShowObj("FX_base_weapon_05_lightning_hit", unit.GetUnitCenterPos());
                    MgrInGameEvent.Instance.BroadcastDamageEvent(unitBaseAlly, unit, unitBaseAlly.GetAtkRateToDamage(resultDmgRate), unitBaseAlly.UnitStat.CriRate, unitBaseAlly.UnitStat.CriDmg, 1);
                }
            }

            if(atkCnt == 4 && MgrBoosterSystem.Instance.DicWeapon["gear_weapon_0003"] >= 5 && !v3LightningPos.Equals(Vector3.zero))
                TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            IsAttack = true;

            unitTarget = null;
            v3LightningPos = Vector3.zero;

            MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0003_a", 1.0f);

            atkCnt = 0;
            weaponComp.SetWeaponAnimation("skill1");
            weaponComp.PlayTimeline(0);
        }

        private async UniTaskVoid TaskSkill()
        {
            await UniTask.Delay(500, cancellationToken: weaponComp.GetCancellationTokenOnDestroy());

            MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0003_b", 1.0f);

            MgrObjectPool.Instance.ShowObj("FX_base_weapon_05_lightning_target-hit", unitTarget.GetUnitCenterPos());
            MgrObjectPool.Instance.ShowObj(MgrBoosterSystem.Instance.DicWeapon["gear_weapon_0003"] >= 3 ? "FX_base_weapon_05_lightning_skill1_Lightning_LV.3" : "FX_base_weapon_05_lightning_skill1_Lightning", v3LightningPos + Vector3.up * 1.75f);

            await UniTask.Delay(125, cancellationToken: weaponComp.GetCancellationTokenOnDestroy());

            MgrObjectPool.Instance.ShowObj(MgrBoosterSystem.Instance.DicWeapon["gear_weapon_0003"] >= 3 ? "FX_base_weapon_05_lightning_skill1_zone_LV.3" : "FX_base_weapon_05_lightning_skill1_zone", v3LightningPos);

            for (int i = 0; i < 3; i++)
            {
                float resultDmgRate = (float)weaponComp.WeaponBoosterData.Params[1];
                float maxXDist = (float)weaponComp.WeaponBoosterData.Params[0];

                resultDmgRate *= 0.5f;

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBaseAlly));

                foreach (UnitBase unit in listUnit)
                {
                    float xDist = unit.transform.position.x - v3LightningPos.x;
                    xDist = xDist < 0.0f ? -xDist : xDist;

                    if (xDist > maxXDist * 0.5f)
                        continue;

                    MgrObjectPool.Instance.ShowObj("FX_base_weapon_05_lightning_hit", unit.GetUnitCenterPos());
                    MgrInGameEvent.Instance.BroadcastDamageEvent(unitBaseAlly, unit, unitBaseAlly.GetAtkRateToDamage(resultDmgRate), unitBaseAlly.UnitStat.CriRate, unitBaseAlly.UnitStat.CriDmg, 1);
                }

                await UniTask.Delay(200, cancellationToken: weaponComp.GetCancellationTokenOnDestroy());
            }
        }
    }
}
