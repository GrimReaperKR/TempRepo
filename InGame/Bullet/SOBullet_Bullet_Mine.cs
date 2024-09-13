using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "SOBullet_Bullet_Mine", menuName = "Bullet/Bullet_Mine")]
public class SOBullet_Bullet_Mine : SOBase_Bullet
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
        private List<UnitBase> listUnitAlreadyHit = new List<UnitBase>();
        private List<Vector3> listV3Pos = new List<Vector3>();
        private Vector3[] arrV3Corner = new Vector3[3];
        private SkeletonAnimation ska;

        private int currBulletLevel;
        private BCH.Database.BoosterWeapon mineWeaponData;

        private float currTime;

        public void SetBezier()
        {
            ska = bulletComp.transform.GetChild(0).GetComponent<SkeletonAnimation>();

            currBulletLevel = MgrBoosterSystem.Instance.DicWeapon["gear_weapon_0004"];
            mineWeaponData = BCH.Database.DataManager.Instance.GetBoosterWeaponData($"gear_weapon_0004_{currBulletLevel - 1}");

            arrV3Corner[0] = bulletComp.transform.position;
            arrV3Corner[1] = bulletComp.transform.position + Vector3.left * Random.Range(2.0f, 4.0f) + Vector3.up * Random.Range(-2.5f, 2.5f);
            arrV3Corner[2] = bulletComp.v3LastPos;

            listV3Pos.Clear();
            listV3Pos.AddRange(MathLib.CalculateBezierCurves(arrV3Corner, 100));

            ska.AnimationState.SetAnimation(0, "skill1_1", false);
        }

        public override void OnMove()
        {
            float maxTime = bulletComp.bulletSetting.bulletSpeedOrTime;

            int currIndex = (int)Mathf.Lerp(0.0f, listV3Pos.Count - 1, currTime / maxTime);

            bulletComp.transform.position = listV3Pos[currIndex];

            currTime += Time.deltaTime;

            if (currIndex >= listV3Pos.Count - 1)
            {
                bulletComp.transform.position = new Vector3(bulletComp.v3LastPos.x, bulletComp.v3LastPos.y, bulletComp.v3LastPos.y * 0.01f);
                bulletComp.IsReach = true;
            }
        }

        public override void OnHit()
        {
            ska.AnimationState.SetAnimation(0, "skill1_2", false);

            TaskSkill().Forget();
        }

        private async UniTaskVoid TaskSkill()
        {
            float maxDuration = 20.0f;

            float durationMultiply = 1.0f;
            if (WeaponSys.WeaponOptionLevel >= 1)
                durationMultiply += (float)BCH.Database.DataManager.Instance.GetGearOptionValue("gear_weapon_0004", 0);
            
            maxDuration *= durationMultiply;

            float detectRadius = (float)mineWeaponData.Params[0];
            while (maxDuration > 0.0f)
            {
                await UniTask.Yield(bulletComp.GetCancellationTokenOnDestroy());

                maxDuration -= Time.deltaTime;

                if (MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(bulletComp.owner, bulletComp.transform.position, detectRadius, 1).Count > 0)
                {
                    await UniTask.Delay(100, cancellationToken: bulletComp.GetCancellationTokenOnDestroy());
                    break;
                }
                else continue;
            }

            listUnitAlreadyHit.Clear();

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(bulletComp.owner, bulletComp.transform.position, detectRadius));

            float resultDmg = (float)mineWeaponData.Params[2];

            MgrSound.Instance.PlayOneShotSFX(WeaponSys.WeaponOptionLevel >= 10 ? "SFX_Gear_Weapon_0004_SS" : "SFX_Gear_Weapon_0004_c", 1.0f);

            foreach (UnitBase unit in listUnit)
            {
                MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, unit, bulletComp.bulletAtk * resultDmg, bulletComp.bulletCriRate, bulletComp.bulletCriDmg);

                listUnitAlreadyHit.Add(unit);
            }

            if(WeaponSys.WeaponOptionLevel >= 10)
                Task5OptionSkill().Forget();

            // 0 ~ -4.25
            MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, bulletComp.transform.position).transform.localScale = currBulletLevel >= 5 ? Vector3.one * 2.0f : Vector3.one;
            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
        }

        private async UniTaskVoid Task5OptionSkill()
        {
            Vector3 v3StartPos = bulletComp.transform.position;

            int explodeCnt = 0;
            bool isMaxY = false, isMinY = false;

            float detectRadius = (float)mineWeaponData.Params[0];

            float dmgMultiply = (float)BCH.Database.DataManager.Instance.GetGearOptionValue("gear_weapon_0004", 4);

            while (!(isMaxY && isMinY))
            {
                await UniTask.Delay(100, cancellationToken: bulletComp.GetCancellationTokenOnDestroy());

                explodeCnt++;

                if(!isMaxY)
                {
                    Vector3 v3MaxPos = v3StartPos + (explodeCnt * 0.5f) * Vector3.up;
                    MgrObjectPool.Instance.ShowObj("FX_base_weapon_04_Mine_hit_5op_sub", v3MaxPos).transform.localScale = currBulletLevel >= 5 ? Vector3.one * 2.0f : Vector3.one;

                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(bulletComp.owner, v3MaxPos, detectRadius));

                    foreach (UnitBase unit in listUnit)
                    {
                        if (listUnitAlreadyHit.Contains(unit))
                            continue;

                        listUnitAlreadyHit.Add(unit);
                        MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, unit, bulletComp.bulletAtk * dmgMultiply, bulletComp.bulletCriRate, bulletComp.bulletCriDmg);
                    }

                    if (v3MaxPos.y >= 0.0f)
                        isMaxY = true;
                }

                if(!isMinY)
                {
                    Vector3 v3MinPos = v3StartPos + (explodeCnt * 0.5f) * Vector3.down;
                    MgrObjectPool.Instance.ShowObj("FX_base_weapon_04_Mine_hit_5op_sub", v3MinPos).transform.localScale = currBulletLevel >= 5 ? Vector3.one * 2.0f : Vector3.one;

                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(bulletComp.owner, v3MinPos, detectRadius));

                    foreach (UnitBase unit in listUnit)
                    {
                        if (listUnitAlreadyHit.Contains(unit))
                            continue;

                        listUnitAlreadyHit.Add(unit);
                        MgrInGameEvent.Instance.BroadcastDamageEvent(bulletComp.owner, unit, bulletComp.bulletAtk * dmgMultiply, bulletComp.bulletCriRate, bulletComp.bulletCriDmg);
                    }

                    if (v3MinPos.y <= -4.25f)
                        isMinY = true;
                }
            }
        }
    }
}
