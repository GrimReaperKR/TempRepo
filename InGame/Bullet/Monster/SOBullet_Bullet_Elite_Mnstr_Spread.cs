using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_Elite_Mnstr_Spread", menuName = "Bullet/Bullet_Elite_Mnstr_Spread")]
public class SOBullet_Bullet_Elite_Mnstr_Spread : SOBase_Bullet
{
    public override void OnInitialize(Bullet _bullet)
    {
        _bullet.BulletPersonalVariable = new PersonalVariable();
        _bullet.BulletPersonalVariable.SetData(_bullet);
    }

    private class PersonalVariable : BulletPersonalVariableInstance
    {
        public override void OnMove()
        {
            Vector3 v3Dir = (bulletComp.target.GetUnitCenterPos() - bulletComp.transform.position).normalized;
            float angle = Mathf.Atan2(v3Dir.y, v3Dir.x) * Mathf.Rad2Deg;
            bulletComp.transform.rotation = Quaternion.AngleAxis(angle - 180.0f, Vector3.forward);

            bulletComp.transform.position += (bulletComp.target.GetUnitCenterPos() - bulletComp.transform.position).normalized * bulletComp.bulletSetting.bulletSpeedOrTime * Time.deltaTime;

            if ((bulletComp.transform.position - bulletComp.target.GetUnitCenterPos()).sqrMagnitude <= 0.5f * 0.5f)
                bulletComp.IsReach = true;
        }

        public override void OnHit()
        {
            MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, bulletComp.target.GetUnitCenterPos());

            bulletComp.target.AddUnitEffect(UNIT_EFFECT.DEBUFF_BLACK_FIRE, bulletComp.target, bulletComp.target, new float[] { 0.3f, 0.0f });

            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
        }
    }
}
