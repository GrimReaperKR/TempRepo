using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOBullet_Bullet_B_Spt_03_S2", menuName = "Bullet/Bullet_B_Spt_03_S2")]
public class SOBullet_Bullet_B_Spt_03_S2 : SOBase_Bullet
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
            MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, bulletComp.target.GetUnitCenterPos());
            bulletComp.target.RemoveDebuffUnitEffect();

            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
            MgrSound.Instance.PlayOneShotSFX("SFX_B_Spt_03_s2_3", 1.0f);
        }
    }
}
