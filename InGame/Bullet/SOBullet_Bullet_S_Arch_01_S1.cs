using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_S_Arch_01_S1", menuName = "Bullet/Bullet_S_Arch_01_S1")]
public class SOBullet_Bullet_S_Arch_01_S1 : SOBase_Bullet
{
    public override void OnInitialize(Bullet _bullet)
    {
        _bullet.BulletPersonalVariable = new PersonalVariable();
        _bullet.BulletPersonalVariable.SetData(_bullet);

        PersonalVariable personal = _bullet.BulletPersonalVariable as PersonalVariable;
        personal.SetDir();
    }

    private class PersonalVariable : BulletPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private List<UnitBase> listHitUnit = new List<UnitBase>();

        private Vector3 v3Dir;
        private Vector3 v3PrevPos;

        public void SetDir()
        {
            v3Dir = bulletComp.owner.GetUnitLookDirection();
            v3PrevPos = bulletComp.owner.transform.position;
        }

        public override void OnMove()
        {
            bulletComp.transform.position += v3Dir * bulletComp.bulletSetting.bulletSpeedOrTime * Time.deltaTime;

            Vector3 v3BulletPos = bulletComp.transform.position;
            v3BulletPos.y = v3PrevPos.y;

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(bulletComp.owner, v3PrevPos, 2.0f, 2.0f, _isContainBlockedTarget: true));

            v3PrevPos = v3BulletPos;

            foreach (UnitBase unit in listUnit)
            {
                if (listHitUnit.Contains(unit))
                    continue;

                listHitUnit.Add(unit);
                MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, unit, bulletComp.bulletAtk * bulletComp.owner.GetUnitSkillFloatDataValue(0, "param.1"), bulletComp.bulletCriRate, bulletComp.bulletCriDmg);
                MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, unit.GetUnitCenterPos());
            }

            float moveDist = bulletComp.transform.position.x - bulletComp.owner.GetUnitCenterPos().x;
            moveDist = moveDist < 0.0f ? -moveDist : moveDist;
            if (moveDist >= 60.0f)
                bulletComp.IsReach = true;
        }

        public override void OnHit()
        {
            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
        }
    }
}
