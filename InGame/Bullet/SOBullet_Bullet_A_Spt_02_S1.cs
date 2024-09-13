using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_A_Spt_02_S1", menuName = "Bullet/Bullet_A_Spt_02_S1")]
public class SOBullet_Bullet_A_Spt_02_S1 : SOBase_Bullet
{
    public override void OnInitialize(Bullet _bullet)
    {
        _bullet.BulletPersonalVariable = new PersonalVariable();
        _bullet.BulletPersonalVariable.SetData(_bullet);
    }

    private class PersonalVariable : BulletPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnMove()
        {
            bulletComp.transform.position += (bulletComp.target.GetUnitCenterPos() - bulletComp.transform.position).normalized * bulletComp.bulletSetting.bulletSpeedOrTime * Time.deltaTime;

            if ((bulletComp.transform.position - bulletComp.target.GetUnitCenterPos()).sqrMagnitude <= 0.5f * 0.5f)
                bulletComp.IsReach = true;
        }

        public override void OnHit()
        {
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(bulletComp.owner, bulletComp.target.transform.position, bulletComp.owner.GetUnitSkillFloatDataValue(0, "param.0"), _isAlly: true));
            listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());

            foreach(UnitBase unit in listUnit)
                unit.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum(bulletComp.owner.GetUnitSkillIntDataValue(0, "param.2")), bulletComp.owner, unit, new float[] { bulletComp.owner.UnitStat.MaxHP * bulletComp.owner.GetUnitSkillFloatDataValue(0, "param.1"), bulletComp.owner.GetUnitSkillFloatDataValue(0, "param.3") });

            MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, bulletComp.target.transform.position);
            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);

            MgrSound.Instance.PlayOneShotSFX("SFX_A_Spt_02_s1_2", 1.0f);
        }
    }
}
