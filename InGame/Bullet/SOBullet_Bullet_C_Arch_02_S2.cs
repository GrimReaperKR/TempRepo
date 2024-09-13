using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_C_Arch_02_S2", menuName = "Bullet/Bullet_C_Arch_02_S2")]
public class SOBullet_Bullet_C_Arch_02_S2 : SOBase_Bullet
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
            MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, bulletComp.target, bulletComp.bulletAtk * bulletComp.owner.GetUnitSkillFloatDataValue(1, "param.1"), bulletComp.bulletCriRate, bulletComp.bulletCriDmg);
            MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, bulletComp.transform.position); // 임시, 나중에 맞은 적에게 출력하는 식으로 변경 예정
            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
            MgrSound.Instance.PlayOneShotSFX("SFX_C_Arch_02_s2_3", 1.0f);
        }
    }
}
