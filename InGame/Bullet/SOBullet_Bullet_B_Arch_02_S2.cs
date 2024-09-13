using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOBullet_Bullet_B_Arch_02_S2", menuName = "Bullet/Bullet_B_Arch_02_S2")]
public class SOBullet_Bullet_B_Arch_02_S2 : SOBase_Bullet
{
    [SerializeField] private GameObject objCloudVFX;

    public override void OnInitialize(Bullet _bullet)
    {
        _bullet.BulletPersonalVariable = new PersonalVariable();
        _bullet.BulletPersonalVariable.SetData(_bullet);

        PersonalVariable personal = _bullet.BulletPersonalVariable as PersonalVariable;
        personal.SoBullet = this;
        personal.OnInit();
    }

    private class PersonalVariable : BulletPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        public SOBullet_Bullet_B_Arch_02_S2 SoBullet;
        private GameObject objCloud;

        public void OnInit()
        {
            objCloud = MgrObjectPool.Instance.ShowObj(SoBullet.objCloudVFX.name, bulletComp.v3LastPos);
            MgrSound.Instance.PlayOneShotSFX("SFX_B_Arch_02_s2_2", 1.0f);
        }

        public override void OnMove()
        {
            bulletComp.transform.position += (bulletComp.v3LastPos - bulletComp.transform.position).normalized * bulletComp.bulletSetting.bulletSpeedOrTime * Time.deltaTime;

            if ((bulletComp.transform.position - bulletComp.v3LastPos).sqrMagnitude <= 0.5f * 0.5f)
                bulletComp.IsReach = true;
        }

        public override void OnHit()
        {
            bulletComp.objBulletVFX.SetActive(false);
            TaskCloud().Forget();
        }

        private async UniTaskVoid TaskCloud()
        {
            int hitCnt = 4;
            float hitTimer = 0.8f;
            float yPos = objCloud.transform.position.y;

            while(hitCnt > 0)
            {
                await UniTask.Yield(bulletComp.GetCancellationTokenOnDestroy());

                // 구름 이동
                if(bulletComp.target.CheckIsState(UNIT_STATE.DEATH))
                {
                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(bulletComp.owner, bulletComp.owner.transform.position, bulletComp.owner.UnitSkillPersonalVariable[1].skillRange * 2.0f));
                    listUnit.Sort((a, b) => (a.UnitStat.HP / a.UnitStat.MaxHP).CompareTo(b.UnitStat.HP / b.UnitStat.MaxHP));

                    if (listUnit.Count > 1)
                        listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());

                    bulletComp.ChangeTarget(listUnit.Count > 0 ? listUnit[0] : bulletComp.target); 
                }

                objCloud.transform.position = Vector3.Lerp(objCloud.transform.position, new Vector3(bulletComp.target.transform.position.x, yPos, 0.0f), 0.25f);

                hitTimer += Time.deltaTime;
                if(hitTimer >= 1.0f)
                {
                    hitTimer -= 1.0f;
                    hitCnt--;

                    int hitLimitCnt = bulletComp.owner.GetUnitSkillIntDataValue(1, "param.1");

                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(bulletComp.owner, bulletComp.target.transform.position, bulletComp.owner.GetUnitSkillFloatDataValue(1, "param.0"), hitLimitCnt));

                    if (listUnit.Contains(MgrBattleSystem.Instance.GetAllyBase()) && listUnit.Count > hitLimitCnt)
                        listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());

                    if(listUnit.Count > 0)
                        MgrSound.Instance.PlayOneShotSFX("SFX_B_Arch_02_s2_3", 1.0f);

                    foreach (UnitBase unit in listUnit)
                    {
                        MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, unit, bulletComp.bulletAtk * bulletComp.owner.GetUnitSkillFloatDataValue(1, "param.2"), bulletComp.bulletCriRate, bulletComp.bulletCriDmg, 1);
                        MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, unit.transform.position);
                    }
                }
            }

            await UniTask.Delay(500, cancellationToken: bulletComp.GetCancellationTokenOnDestroy());

            MgrObjectPool.Instance.HideObj(SoBullet.objCloudVFX.name, objCloud);
            objCloud = null;
            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
        }
    }
}
