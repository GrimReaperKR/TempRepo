using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_C4_Final_Boss03_ac", menuName = "Bullet/Monster/Bullet_C4_Final_Boss03_ac")]
public class SOBullet_Bullet_C4_Final_Boss03_ac : SOBase_Bullet
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
        private List<Vector3> listV3Pos = new List<Vector3>();
        private Vector3[] arrV3Corner = new Vector3[4];

        private float currTime;
        private float totalRangeLine;

        public void SetBezier()
        {
            Vector3 v3BulletDir = (bulletComp.target.GetUnitCenterPos() - bulletComp.transform.position).normalized;
            totalRangeLine = (bulletComp.target.GetUnitCenterPos() - bulletComp.transform.position).magnitude;

            arrV3Corner[0] = bulletComp.transform.position;
            if (MathLib.CheckPercentage(0.5f))
            {
                float upVel = Random.Range(0.5f, 2.0f);
                arrV3Corner[1] = bulletComp.transform.position + (totalRangeLine * 0.33f) * v3BulletDir + upVel * Vector3.up;
                arrV3Corner[2] = bulletComp.transform.position + (totalRangeLine * 0.66f) * v3BulletDir + (upVel * 0.33f) * Vector3.down;
            }
            else
            {
                float dowmVel = Random.Range(0.5f, 2.0f);
                arrV3Corner[1] = bulletComp.transform.position + (totalRangeLine * 0.33f) * v3BulletDir + dowmVel * Vector3.down;
                arrV3Corner[2] = bulletComp.transform.position + (totalRangeLine * 0.66f) * v3BulletDir + (dowmVel * 0.5f) * Vector3.up;
            }
            arrV3Corner[3] = bulletComp.target.GetUnitCenterPos();

            listV3Pos.Clear();
            listV3Pos.AddRange(MathLib.CalculateBezierCurves(arrV3Corner, 100));
        }

        private void RefreshBezier()
        {
            arrV3Corner[3] = bulletComp.target.GetUnitCenterPos();

            listV3Pos.Clear();
            listV3Pos.AddRange(MathLib.CalculateBezierCurves(arrV3Corner, 100));
        }

        public override void OnMove()
        {
            RefreshBezier();

            float maxTime = bulletComp.bulletSetting.bulletSpeedOrTime + totalRangeLine * 0.05f;

            int currIndex = (int)Mathf.Lerp(0.0f, listV3Pos.Count - 1, currTime / maxTime);

            Vector3 v3Dir = (listV3Pos[currIndex] - bulletComp.transform.position).normalized;
            bulletComp.transform.position = listV3Pos[currIndex];

            float angle = Mathf.Atan2(v3Dir.y, v3Dir.x) * Mathf.Rad2Deg;
            bulletComp.transform.rotation = Quaternion.AngleAxis(angle + 180.0f, Vector3.forward);

            currTime += Time.deltaTime;

            if (currIndex >= listV3Pos.Count - 1)
            {
                bulletComp.IsReach = true;
                OnHit();
            }
        }

        public override void OnHit()
        {
            MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, bulletComp.target, bulletComp.bulletAtk * bulletComp.owner.GetUnitSkillFloatDataValue(0, "param.1"), bulletComp.bulletCriRate, bulletComp.bulletCriDmg);
            MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, bulletComp.target.GetUnitCenterPos()); // 임시, 나중에 맞은 적에게 출력하는 식으로 변경 예정
            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);

            MgrSound.Instance.PlayOneShotSFX("SFX_C4_Final_Boss03_ac_s1_2_", 1.0f);

            bulletComp.BulletPersonalVariable = null;
        }
    }
}
