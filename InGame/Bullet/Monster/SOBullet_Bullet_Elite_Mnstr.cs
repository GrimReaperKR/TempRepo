using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_Elite_Mnstr", menuName = "Bullet/Bullet_Elite_Mnstr")]
public class SOBullet_Bullet_Elite_Mnstr : SOBase_Bullet
{
    public override void OnInitialize(Bullet _bullet)
    {
        _bullet.BulletPersonalVariable = new PersonalVariable();
        _bullet.BulletPersonalVariable.SetData(_bullet);
    }

    private class PersonalVariable : BulletPersonalVariableInstance
    {
        private float duration = 0.75f;

        public override void OnMove()
        {
            if(duration > 0.0f)
            {
                duration -= Time.deltaTime;
                bulletComp.transform.position += Vector3.up * bulletComp.bulletSetting.bulletSpeedOrTime * 0.25f * Time.deltaTime;
            }
            else
            {
                Vector3 v3Dir = (bulletComp.target.GetUnitCenterPos() - bulletComp.transform.position).normalized;
                float angle = Mathf.Atan2(v3Dir.y, v3Dir.x) * Mathf.Rad2Deg;
                bulletComp.transform.rotation = Quaternion.AngleAxis(angle - 180.0f, Vector3.forward);

                bulletComp.transform.position += (bulletComp.target.GetUnitCenterPos() - bulletComp.transform.position).normalized * bulletComp.bulletSetting.bulletSpeedOrTime * Time.deltaTime;

                if ((bulletComp.transform.position - bulletComp.target.GetUnitCenterPos()).sqrMagnitude <= 0.5f * 0.5f)
                    bulletComp.IsReach = true;
            }
        }

        public override void OnHit()
        {
            MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, bulletComp.target.GetUnitCenterPos());

            bulletComp.target.AddUnitEffect(UNIT_EFFECT.DEBUFF_BLACK_FIRE, bulletComp.target, bulletComp.target, new float[] { 0.3f, 1.0f });

            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
        }
    }
}
