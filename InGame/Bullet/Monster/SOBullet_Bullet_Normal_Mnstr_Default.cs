using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_Normal_Mnstr_Default", menuName = "Bullet/Bullet_Normal_Mnstr_Default")]
public class SOBullet_Bullet_Normal_Mnstr_Default : SOBase_Bullet
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
            MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, bulletComp.target, bulletComp.bulletAtk, bulletComp.bulletCriRate, bulletComp.bulletCriDmg);
            MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, bulletComp.target.GetUnitCenterPos()); // 임시, 나중에 맞은 적에게 출력하는 식으로 변경 예정
            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
        }
    }
}
