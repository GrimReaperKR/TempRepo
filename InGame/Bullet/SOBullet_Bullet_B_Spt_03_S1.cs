using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_B_Spt_03_S1", menuName = "Bullet/Bullet_B_Spt_03_S1")]
public class SOBullet_Bullet_B_Spt_03_S1 : SOBase_Bullet
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
            bulletComp.transform.position += (bulletComp.target.GetUnitCenterPos() - bulletComp.transform.position).normalized * bulletComp.bulletSetting.bulletSpeedOrTime * Time.deltaTime;

            if ((bulletComp.transform.position - bulletComp.target.GetUnitCenterPos()).sqrMagnitude <= 0.5f * 0.5f)
                bulletComp.IsReach = true;
        }

        public override void OnHit()
        {
            bulletComp.target.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum(bulletComp.owner.GetUnitSkillIntDataValue(0, "param.0")), bulletComp.owner, bulletComp.target, new float[] { bulletComp.owner.GetUnitSkillFloatDataValue(0, "param.2"), bulletComp.owner.GetUnitSkillFloatDataValue(0, "param.1") });

            MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, bulletComp.transform.position);
            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
        }
    }
}
