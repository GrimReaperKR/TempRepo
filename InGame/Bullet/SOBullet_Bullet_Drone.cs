using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_Drone", menuName = "Bullet/Bullet_Drone")]
public class SOBullet_Bullet_Drone : SOBase_Bullet
{
    public override void OnInitialize(Bullet _bullet)
    {
        _bullet.BulletPersonalVariable = new PersonalVariable();
        _bullet.BulletPersonalVariable.SetData(_bullet);

        PersonalVariable personal = _bullet.BulletPersonalVariable as PersonalVariable;
        personal.SetBezier();
    }

    private class PersonalVariable : BulletPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private List<Vector3> listV3Pos = new List<Vector3>();
        private Vector3[] arrV3Corner = new Vector3[3];

        private float currTime;

        public void SetBezier()
        {
            arrV3Corner[0] = bulletComp.transform.position;
            arrV3Corner[1] = bulletComp.transform.position + Vector3.left * Random.Range(2.0f, 6.0f) + Vector3.up * Random.Range(-2.5f, 5.0f);
            arrV3Corner[2] = bulletComp.v3LastPos;

            listV3Pos.Clear();
            listV3Pos.AddRange(MathLib.CalculateBezierCurves(arrV3Corner, 100));
        }

        public override void OnMove()
        {
            float maxTime = bulletComp.bulletSetting.bulletSpeedOrTime;

            int currIndex = (int)Mathf.Lerp(0.0f, listV3Pos.Count - 1, currTime / maxTime);

            Vector3 v3Dir = (listV3Pos[currIndex] - bulletComp.transform.position).normalized;
            bulletComp.transform.position = listV3Pos[currIndex];

            float angle = Mathf.Atan2(v3Dir.y, v3Dir.x) * Mathf.Rad2Deg;
            bulletComp.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            currTime += Time.deltaTime;

            if (currIndex >= listV3Pos.Count - 1)
            {
                bulletComp.transform.position = bulletComp.v3LastPos;
                bulletComp.IsReach = true;
            }
        }

        public override void OnHit()
        {
            float resultDmg = bulletComp.bulletAtk * (float)WeaponSys.WeaponBoosterData.Params[0];

            if(WeaponSys.WeaponOptionLevel >= 1)
            {
                float multiplyValue = bulletComp.transform.position.x - MgrBattleSystem.Instance.GetAllyBase().transform.position.x;
                multiplyValue = multiplyValue < 0.0f ? -multiplyValue : multiplyValue;
                //multiplyValue = multiplyValue * (float)BCH.Database.DataManager.Instance.GetGearOptionValue("gear_weapon_0000", 0);
                if (multiplyValue > 10.0f)
                    multiplyValue = 10.0f;

                resultDmg *= 1.0f + (Mathf.Lerp(0.5f, 0.0f, multiplyValue * 0.1f));
            }

            MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, bulletComp.target, resultDmg, bulletComp.bulletCriRate, bulletComp.bulletCriDmg);

            if (WeaponSys.WeaponOptionLevel >= 10)
            {
                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(bulletComp.owner, bulletComp.target.transform.position, 0.3f));

                listUnit.Remove(bulletComp.target);

                float dmgMultiply = (float)BCH.Database.DataManager.Instance.GetGearOptionValue("gear_weapon_0000", 4);
                foreach (UnitBase unit in listUnit)
                {
                    MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, unit, resultDmg * dmgMultiply, bulletComp.bulletCriRate, bulletComp.bulletCriDmg);
                    MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletIndex.Equals("Bullet_Drone_2") ? "FX_base_weapon_02_drone_skill1_hit_5op_red" : "FX_base_weapon_02_drone_skill1_hit_5op", unit.GetUnitCenterPos());
                }
            }

            MgrObjectPool.Instance.ShowObj(WeaponSys.WeaponOptionLevel >= 10 ? (bulletComp.bulletSetting.bulletIndex.Equals("Bullet_Drone_2") ? "FX_base_weapon_02_drone_skill1_hit_5op_red" : "FX_base_weapon_02_drone_skill1_hit_5op") : bulletComp.bulletSetting.bulletHitToPlayerPrefabName, bulletComp.target.GetUnitCenterPos());
            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);

            MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0000_c", 1.0f);
        }
    }
}
