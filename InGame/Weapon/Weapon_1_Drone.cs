using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Spine;
using Unity.VisualScripting;
using DG.Tweening;

[CreateAssetMenu(fileName = "Weapon_1_Drone", menuName = "Weapon/1_Drone")]
public class Weapon_1_Drone : SOBase_Weapon
{
    public override void OnInitialize(WeaponSystem _weapon, UnitBase _unitbase)
    {
        _weapon.WeaponPersonalVariable = new PersonalVariable();
        _weapon.WeaponPersonalVariable.SetData(_weapon);

        PersonalVariable personal = _weapon.WeaponPersonalVariable as PersonalVariable;
        personal.SetAllyBase(_unitbase);
    }

    private class PersonalVariable : WeaponPersonalVariableInstance
    {
        private UnitBase unitBaseAlly;
        private List<UnitBase> listUnit = new List<UnitBase>();
        private int atkCnt;
        private int shootCnt;
        private int maxShootCnt;

        public void SetAllyBase(UnitBase _target)
        {
            unitBaseAlly = _target;
            weaponComp.SetCoolDown((float)weaponComp.WeaponBoosterData.StartCooldown);
        }

        public override void OnMove()
        {
            if(!IsAttack)
            {
                if (MathLib.CheckIsPosDistanceInRange(weaponComp.transform.position, unitBaseAlly.transform.position + Vector3.up * 2.0f + (unitBaseAlly.TeamNum == 0 ? Vector3.left : Vector3.right) * 1.5f, 0.1f))
                {
                    if (!weaponComp.Ska.AnimationName.Equals("idle") && !weaponComp.Ska.AnimationName.Equals("death"))
                        weaponComp.SetWeaponAnimation("idle", true);
                }
                else
                {
                    if (!weaponComp.Ska.AnimationName.Equals("walk") && !weaponComp.Ska.AnimationName.Equals("death"))
                        weaponComp.SetWeaponAnimation("walk", true);
                }
            }

            weaponComp.transform.position = Vector3.Lerp(weaponComp.transform.position, unitBaseAlly.transform.position + Vector3.up * 2.0f + (unitBaseAlly.TeamNum == 0 ? Vector3.left : Vector3.right) * 1.5f, Time.deltaTime);
        }

        public override bool CheckCanUseSkill()
        {
            UnitBase unit = MgrBattleSystem.Instance.GetNearestXEnemyUnit(unitBaseAlly);
            return unit is not null;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill"))
                return;

            IsAttack = false;
            weaponComp.SetCoolDown((float)weaponComp.WeaponBoosterData.Cooldown);
            weaponComp.Ska.timeScale = 1.0f;
        }

        public override void EventTriggerSkill()
        {
            atkCnt++;

            if (atkCnt == 1)
            {
                int bulletCnt = (int)weaponComp.WeaponBoosterData.Params[1];

                UnitBase targetUnit = MgrBattleSystem.Instance.GetNearestXEnemyUnit(unitBaseAlly);
                if (weaponComp.WeaponOptionLevel >= 3 && targetUnit is not null && targetUnit.transform.position.x - unitBaseAlly.transform.position.x < 10.0f)
                    bulletCnt += (int)BCH.Database.DataManager.Instance.GetGearOptionValue("gear_weapon_0000", 2);

                TaskAttack(bulletCnt).Forget();
            }
        }

        public override void OnSkill()
        {
            IsAttack = true;
            atkCnt = 0;
            shootCnt = 0;

            maxShootCnt = MgrBoosterSystem.Instance.DicWeapon["gear_weapon_0000"] >= 5 ? 2 : 1;

            weaponComp.SetWeaponAnimation("skill1");
            weaponComp.PlayTimeline(0);
        }

        private async UniTaskVoid TaskAttack(int _bulletCnt)
        {
            MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0000_a", 1.0f);

            shootCnt++;

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBaseAlly));
            listUnit.Shuffle();

            GameObject objBullet;
            for (int i = 0; i < _bulletCnt; i++)
            {
                UnitBase unit = listUnit.Count > 0 ? listUnit[0] : MgrBattleSystem.Instance.GetRandomUnit(unitBaseAlly);
                if (unit)
                {
                    objBullet = MgrBulletPool.Instance.ShowObj(shootCnt == 2 ? "Bullet_Drone_2" : "Bullet_Drone_1", weaponComp.transform.position + Vector3.up * 3.0f);
                    objBullet.GetComponent<Bullet>().SetBullet(unitBaseAlly, unit);

                    if (listUnit.Count > 0)
                        listUnit.RemoveAt(0);

                    await UniTask.Delay(50, cancellationToken:weaponComp.GetCancellationTokenOnDestroy());
                }
            }

            if (shootCnt < maxShootCnt)
            {
                await UniTask.Delay(300, cancellationToken: weaponComp.GetCancellationTokenOnDestroy());

                atkCnt = 0;
                weaponComp.SetWeaponAnimation("skill1");
                weaponComp.PlayTimeline(1);
            }
        }
    }
}
