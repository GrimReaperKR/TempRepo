using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_B_Spt_02_S1", menuName = "Bullet/Bullet_B_Spt_02_S1")]
public class SOBullet_Bullet_B_Spt_02_S1 : SOBase_Bullet
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
        private Vector3[] arrV3Corner = new Vector3[4];

        private float currTime;

        public void SetBezier()
        {
            arrV3Corner[0] = bulletComp.transform.position;
            arrV3Corner[1] = bulletComp.transform.position + Vector3.up * 5.0f;
            arrV3Corner[2] = bulletComp.v3LastPos + Vector3.up * 5.0f;
            arrV3Corner[3] = bulletComp.v3LastPos;

            listV3Pos.Clear();
            listV3Pos.AddRange(MathLib.CalculateBezierCurves(arrV3Corner, 100));
        }

        public override void OnMove()
        {
            float maxTime = bulletComp.bulletSetting.bulletSpeedOrTime;

            int currIndex = (int)Mathf.Lerp(0.0f, listV3Pos.Count - 1, currTime / maxTime);
            bulletComp.transform.position = listV3Pos[currIndex];

            currTime += Time.deltaTime;

            if (currIndex >= listV3Pos.Count - 1)
            {
                bulletComp.transform.position = bulletComp.v3LastPos;
                bulletComp.IsReach = true;
            }
        }

        public override void OnHit()
        {
            MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, bulletComp.transform.position);

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(bulletComp.owner, bulletComp.transform.position, bulletComp.owner.GetUnitSkillFloatDataValue(0, "param.0"), bulletComp.owner.GetUnitSkillIntDataValue(0, "param.1"), _isAlly: true));
            listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());

            foreach (UnitBase unit in listUnit)
            {
                if(unit.UnitIndex.Equals("B_Spt_02")) unit.SetHeal(bulletComp.owner.UnitStat.MaxHP * bulletComp.owner.GetUnitSkillFloatDataValue(0, "param.2") * 0.5f);
                else unit.SetHeal(bulletComp.owner.UnitStat.MaxHP * bulletComp.owner.GetUnitSkillFloatDataValue(0, "param.2"));
                MgrObjectPool.Instance.ShowObj("FX_PC-Heal", unit.GetUnitCenterPos());
            }

            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
        }
    }
}
