using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOBullet_Bullet_A_Arch_03_S1", menuName = "Bullet/Bullet_A_Arch_03_S1")]
public class SOBullet_Bullet_A_Arch_03_S1 : SOBase_Bullet
{
    [SerializeField] private GameObject objDistanceVFX;
    [SerializeField] private GameObject objDotHitVFX;

    public override void OnInitialize(Bullet _bullet)
    {
        _bullet.BulletPersonalVariable = new PersonalVariable();
        _bullet.BulletPersonalVariable.SetData(_bullet);

        _bullet.transform.localScale = Vector3.one;
    }

    private class PersonalVariable : BulletPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private float multipleSpeed = 0.0f;

        public override void OnMove()
        {
            multipleSpeed += 10.0f * Time.deltaTime;
            bulletComp.transform.position += (bulletComp.target.transform.position - bulletComp.transform.position).normalized * (bulletComp.bulletSetting.bulletSpeedOrTime + multipleSpeed) * Time.deltaTime;

            if ((bulletComp.transform.position - bulletComp.target.transform.position).sqrMagnitude <= 0.5f * 0.5f)
                bulletComp.IsReach = true;
        }

        public override void OnHit()
        {
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(bulletComp.owner, bulletComp.target.transform.position, bulletComp.owner.GetUnitSkillFloatDataValue(0, "param.0"), _isContainBlockedTarget: true));

            foreach(UnitBase unit in listUnit)
            {
                MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, unit, bulletComp.bulletAtk * bulletComp.owner.GetUnitSkillFloatDataValue(0, "param.1"), bulletComp.bulletCriRate, bulletComp.bulletCriDmg);
                MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, unit.GetUnitCenterPos());
            }

            MgrSound.Instance.PlayOneShotSFX("SFX_A_Arch_03_s1_4", 1.0f);
            MgrSound.Instance.PlayOneShotSFX("SFX_A_Arch_03_s2_1", 1.0f);

            bulletComp.transform.DOScale(Vector3.zero, 0.25f);
            TaskSkill().Forget();
        }

        private async UniTaskVoid TaskSkill()
        {
            SOBullet_Bullet_A_Arch_03_S1 soBullet = bulletComp.SoBullet as SOBullet_Bullet_A_Arch_03_S1;

            GameObject objDistance = MgrObjectPool.Instance.ShowObj(soBullet.objDistanceVFX.name, bulletComp.target.transform.position);

            float duration = bulletComp.owner.GetUnitSkillFloatDataValue(1, "param.3");
            float hitTimer = bulletComp.owner.GetUnitSkillFloatDataValue(1, "param.1");
            int hitCnt = (int)(duration / hitTimer);

            for(int i = 0; i < hitCnt; i++)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(hitTimer), cancellationToken: bulletComp.GetCancellationTokenOnDestroy());

                MgrSound.Instance.PlayOneShotSFX("SFX_A_Arch_03_s2_2", 1.0f);

                MgrObjectPool.Instance.ShowObj(soBullet.objDotHitVFX.name, objDistance.transform.position);

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(bulletComp.owner, objDistance.transform.position, bulletComp.owner.GetUnitSkillFloatDataValue(1, "param.0"), _isContainBlockedTarget: true));

                foreach(UnitBase unit in listUnit)
                    MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, unit, bulletComp.bulletAtk * bulletComp.owner.GetUnitSkillFloatDataValue(1, "param.2"), bulletComp.bulletCriRate, bulletComp.bulletCriDmg);
            }

            MgrObjectPool.Instance.HideObj(soBullet.objDistanceVFX.name, objDistance);
            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
        }
    }
}
