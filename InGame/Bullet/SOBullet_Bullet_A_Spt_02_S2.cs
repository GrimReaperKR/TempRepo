using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_A_Spt_02_S2", menuName = "Bullet/Bullet_A_Spt_02_S2")]
public class SOBullet_Bullet_A_Spt_02_S2 : SOBase_Bullet
{
    public override void OnInitialize(Bullet _bullet)
    {
        _bullet.BulletPersonalVariable = new PersonalVariable();
        _bullet.BulletPersonalVariable.SetData(_bullet);
    }

    private class PersonalVariable : BulletPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private List<UnitBase> listHit = new List<UnitBase>();
        private Vector3 v3PrevPos;

        public override void OnMove()
        {
            v3PrevPos = bulletComp.transform.position;

            Vector3 v3Dir = (bulletComp.target.GetUnitCenterPos() - bulletComp.transform.position).normalized;
            float angle = Mathf.Atan2(v3Dir.y, v3Dir.x) * Mathf.Rad2Deg;
            bulletComp.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            bulletComp.transform.position += (bulletComp.target.GetUnitCenterPos() - bulletComp.transform.position).normalized * bulletComp.bulletSetting.bulletSpeedOrTime * Time.deltaTime;

            SetSlow();

            if ((bulletComp.transform.position - bulletComp.target.GetUnitCenterPos()).sqrMagnitude <= 0.5f * 0.5f)
                bulletComp.IsReach = true;
        }

        public override void OnHit()
        {
            bulletComp.transform.position = bulletComp.target.GetUnitCenterPos();

            SetSlow();

            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
        }

        private void SetSlow()
        {
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitInPolygon(bulletComp.owner, v3PrevPos, bulletComp.transform.position, 2.0f, _isContainBlockedTarget: true, _isCheckCenterPos: true));

            foreach (UnitBase unit in listUnit)
            {
                if (listHit.Contains(unit))
                    continue;

                unit.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum(bulletComp.owner.GetUnitSkillIntDataValue(1, "param.1")), bulletComp.owner, unit, new float[] { bulletComp.owner.GetUnitSkillFloatDataValue(1, "param.0"), bulletComp.owner.GetUnitSkillFloatDataValue(1, "param.2") }); ;
                listHit.Add(unit);

                MgrSound.Instance.PlayOneShotSFX("SFX_A_Spt_02_s2_2", 0.5f);
            }
        }
    }
}
