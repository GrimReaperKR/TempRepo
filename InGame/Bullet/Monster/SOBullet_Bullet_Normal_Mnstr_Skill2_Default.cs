using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_Normal_Mnstr_Skill2_Default", menuName = "Bullet/Bullet_Normal_Mnstr_Skill2_Default")]
public class SOBullet_Bullet_Normal_Mnstr_Skill2_Default : SOBase_Bullet
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

        private int hitCnt = 0;

        private float dmgRate;
        private Vector3 v3Dir;

        public void SetDir()
        {
            v3Dir = bulletComp.owner.GetUnitLookDirection();
            dmgRate = Mathf.Lerp(0.5f, 0.15f, MgrBattleSystem.Instance.currWave / 36.0f);
        }

        public override void OnMove()
        {
            bulletComp.transform.position += v3Dir * bulletComp.bulletSetting.bulletSpeedOrTime * Time.deltaTime;

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(bulletComp.owner, bulletComp.transform.position + (Vector3.down * ((bulletComp.owner.GetUnitHeight() * 0.5f) - 0.2f)), 2.0f));

            float multiplyDmgRate = 1.0f;

            foreach (UnitBase unit in listUnit)
            {
                if (listHitUnit.Contains(unit))
                    continue;

                if(hitCnt > 2)
                    multiplyDmgRate = 1.0f - (hitCnt - 2 >= 5 ? 0.5f : (hitCnt - 2) * 0.1f);

                listHitUnit.Add(unit);
                MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, unit, bulletComp.bulletAtk * dmgRate * multiplyDmgRate, bulletComp.bulletCriRate, bulletComp.bulletCriDmg, 1);
                MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, unit.GetUnitCenterPos());

                hitCnt++;
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
