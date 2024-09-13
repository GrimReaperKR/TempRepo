using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_LaserGun", menuName = "Bullet/Bullet_LaserGun")]
public class SOBullet_Bullet_LaserGun : SOBase_Bullet
{
    public override void OnInitialize(Bullet _bullet)
    {
        _bullet.BulletPersonalVariable = new PersonalVariable();
        _bullet.BulletPersonalVariable.SetData(_bullet);
    }

    private class PersonalVariable : BulletPersonalVariableInstance
    {
        private Vector3 v3PrevPos;
        private Vector3 v3PrevDir;
        private List<UnitBase> listUnit = new List<UnitBase>();
        private List<UnitBase> listHitUnit = new List<UnitBase>();

        public override void OnMove()
        {
            v3PrevPos = bulletComp.transform.position;

            Vector3 v3Dir = (bulletComp.target.GetUnitCenterPos() - bulletComp.transform.position).normalized;
            float angle = Mathf.Atan2(v3Dir.y, v3Dir.x) * Mathf.Rad2Deg;
            bulletComp.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            bulletComp.transform.position += (bulletComp.target.GetUnitCenterPos() - bulletComp.transform.position).normalized * bulletComp.bulletSetting.bulletSpeedOrTime * Time.deltaTime;

            if (WeaponSys.WeaponOptionLevel >= 10)
            {
                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitInPolygon(bulletComp.owner, v3PrevPos, bulletComp.transform.position, 2.0f, _isCheckCenterPos: true));
                listUnit.Remove(bulletComp.target);

                float resultDmg = (float)WeaponSys.WeaponBoosterData.Params[1];

                resultDmg *= (float)BCH.Database.DataManager.Instance.GetGearOptionValue("gear_weapon_0002", 4);

                foreach(UnitBase unit in listUnit)
                {
                    if (listHitUnit.Contains(unit) || unit == bulletComp.target)
                        continue;

                    MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0002_SS", 1.0f);

                    listHitUnit.Add(unit);
                    MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, unit, bulletComp.bulletAtk * resultDmg, bulletComp.bulletCriRate, bulletComp.bulletCriDmg, 1);
                    MgrObjectPool.Instance.ShowObj("FX_base_weapon_03_Laser_skill1_hit", unit.GetUnitCenterPos());
                }
            }

            if ((bulletComp.transform.position - bulletComp.target.GetUnitCenterPos()).sqrMagnitude <= 0.5f * 0.5f)
            {
                v3PrevDir = v3Dir;
                bulletComp.IsReach = true;
            }
        }

        public override void OnHit()
        {
            bool isDeath = bulletComp.target.CheckIsState(UNIT_STATE.DEATH);
            float resultDmg = (float)WeaponSys.WeaponBoosterData.Params[1];

            MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0002_c", 1.0f);
            MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, bulletComp.target, bulletComp.bulletAtk * resultDmg, WeaponSys.WeaponOptionLevel >= 1 ? 1.0f : bulletComp.bulletCriRate, bulletComp.bulletCriDmg, 1);

            if(WeaponSys.WeaponOptionLevel >= 3)
                bulletComp.target.AddUnitEffect(UNIT_EFFECT.CC_STUN, bulletComp.owner, bulletComp.target, new float[] { (float)BCH.Database.DataManager.Instance.GetGearOptionValue("gear_weapon_0002", 2) });

            if(!isDeath)
                MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, bulletComp.target.GetUnitCenterPos());

            if(isDeath || WeaponSys.WeaponOptionLevel >= 10) TaskBullet().Forget();
            else MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
            //if (WeaponSys.WeaponOptionLevel >= 10) TaskBullet().Forget();
            //else MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
        }

        private async UniTaskVoid TaskBullet()
        {
            float duration = 2.0f;
            while(duration > 0.0f)
            {
                duration -= Time.deltaTime;

                bulletComp.transform.position += v3PrevDir * bulletComp.bulletSetting.bulletSpeedOrTime * Time.deltaTime;

                if (WeaponSys.WeaponOptionLevel >= 10)
                {
                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitInPolygon(bulletComp.owner, v3PrevPos, bulletComp.transform.position, 2.0f, _isCheckCenterPos: true));

                    float resultDmg = (float)WeaponSys.WeaponBoosterData.Params[1];
                    resultDmg *= (float)BCH.Database.DataManager.Instance.GetGearOptionValue("gear_weapon_0002", 4);

                    foreach (UnitBase unit in listUnit)
                    {
                        if (listHitUnit.Contains(unit) || unit == bulletComp.target)
                            continue;

                        MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0002_SS", 1.0f);

                        listHitUnit.Add(unit);
                        MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, unit, bulletComp.bulletAtk * resultDmg, bulletComp.bulletCriRate, bulletComp.bulletCriDmg, 1);
                        MgrObjectPool.Instance.ShowObj("FX_base_weapon_03_Laser_skill1_hit", unit.GetUnitCenterPos());
                    }
                }

                await UniTask.Yield(bulletComp.GetCancellationTokenOnDestroy());
            }
            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
        }
    }
}
