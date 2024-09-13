 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Spine.Unity;
using Spine;
using DG.Tweening;

[CreateAssetMenu(fileName = "Weapon_3_LaserGun", menuName = "Weapon/3_LaserGun")]
public class Weapon_3_LaserGun : SOBase_Weapon
{
    public override void OnInitialize(WeaponSystem _weapon, UnitBase _unitbase)
    {
        _weapon.WeaponPersonalVariable = new PersonalVariable();
        _weapon.WeaponPersonalVariable.SetData(_weapon);

        PersonalVariable personal = _weapon.WeaponPersonalVariable as PersonalVariable;
        personal.SetAllyBase(_unitbase);

        MgrInGameEvent.Instance.AddBoosterEvent(personal.OnBoosterUpgradeAction);
    }

    private class PersonalVariable : WeaponPersonalVariableInstance
    {
        private int shootCnt;

        private UnitBase unitBaseAlly;
        private List<UnitBase> listUnit = new List<UnitBase>();
        private List<UnitBase> listAlreadyUnit = new List<UnitBase>();
        private SkeletonAnimation[] arrSka = new SkeletonAnimation[3];
        private UnitBase[] arrUnitTarget = new UnitBase[3];

        public void OnBoosterUpgradeAction(string _index)
        {
            switch (_index)
            {
                case "gear_weapon_0002":
                    if (MgrBoosterSystem.Instance.DicWeapon[_index] == 4 && weaponComp.WeaponCooldown > 0.0f)
                        weaponComp.SetCoolDown(weaponComp.WeaponCooldown - 1.0f);
                    break;
                default:
                    break;
            }
        }

        public void SetAllyBase(UnitBase _target)
        {
            unitBaseAlly = _target;
            weaponComp.SetCoolDown((float)weaponComp.WeaponBoosterData.StartCooldown);
        }

        public override void OnMove()
        {
            if (!IsAttack)
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

            return !(unit is null) && (unit.transform.position.x - unitBaseAlly.transform.position.x) <= weaponComp.WeaponBoosterData.Range;
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
            TaskSkill(shootCnt).Forget();
        }

        public override void OnSkill()
        {
            IsAttack = true;

            weaponComp.SetWeaponAnimation("skill1");
            weaponComp.PlayTimeline(0);

            listAlreadyUnit.Clear();

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBaseAlly));
            listUnit.Reverse();

            shootCnt = (int)weaponComp.WeaponBoosterData.Params[0];

            TaskTargetDelay(shootCnt).Forget();
            TaskSkaCheck().Forget();
        }

        private async UniTaskVoid TaskTargetDelay(int _shootCnt)
        {
            for(int i = 0; i < _shootCnt; i++)
            {
                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBaseAlly));
                listUnit.Reverse();

                for (int x = 0; x < listAlreadyUnit.Count; x++)
                    listUnit.Remove(listAlreadyUnit[x]);

                UnitBase unitAdd;
                if (listUnit.Count > 0) unitAdd = listUnit[0];
                else unitAdd = listAlreadyUnit[Random.Range(0, listAlreadyUnit.Count)];
                listAlreadyUnit.Add(unitAdd);

                arrSka[i] = MgrObjectPool.Instance.ShowObj("LaserGun_Target", unitAdd.GetUnitCenterPos()).GetComponent<SkeletonAnimation>();
                arrSka[i].transform.SetParent(unitAdd.transform);
                arrSka[i].AnimationState.SetAnimation(0, "idle", false);

                arrUnitTarget[i] = unitAdd;

                await UniTask.Delay(200, cancellationToken: weaponComp.GetCancellationTokenOnDestroy());
            }
        }

        private async UniTaskVoid TaskSkaCheck()
        {
            float duration = 0.5f;
            bool isPlayedSound = false;
            while(IsAttack)
            {
                duration -= Time.deltaTime;
                if(duration < 0.0f && !isPlayedSound)
                {
                    MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0002_a", 1.0f);
                    isPlayedSound = true;
                }

                for (int i = 0; i < shootCnt; i++)
                {
                    if (arrUnitTarget[i] is not null && arrUnitTarget[i].CheckIsState(UNIT_STATE.DEATH) && arrSka[i] is not null && arrSka[i].transform.parent == arrUnitTarget[i].transform)
                        MgrObjectPool.Instance.ResetParent("LaserGun_Target", arrSka[i].gameObject);
                }

                await UniTask.Yield(weaponComp.GetCancellationTokenOnDestroy());
            }
        }

        private void MoveTargetSpine(int _index)
        {
            if (arrSka[_index] is not null)
            {
                arrSka[_index].transform.DOKill();
                arrSka[_index].transform.DOMove(arrUnitTarget[_index].GetUnitCenterPos(), 0.2f).OnComplete(() =>
                {
                    if(arrSka[_index] is not null)
                    {
                        arrSka[_index].transform.position = arrUnitTarget[_index].GetUnitCenterPos();
                        arrSka[_index].transform.SetParent(arrUnitTarget[_index].transform);
                    }
                });
            }
        }

        private async UniTaskVoid TaskSkill(int _shootCnt)
        {
            GameObject objBullet;
            for (int i = 0; i < _shootCnt; i++)
            {
                if (arrUnitTarget[i].CheckIsState(UNIT_STATE.DEATH))
                {
                    listUnit.Clear();
                    listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(unitBaseAlly));
                    listUnit.Reverse();

                    if (listUnit.Count > 0)
                    {
                        arrUnitTarget[i] = listUnit[Random.Range(0, listUnit.Count)];

                        MoveTargetSpine(i);

                        await UniTask.Delay(200, cancellationToken: weaponComp.GetCancellationTokenOnDestroy());
                    }
                    else
                    {
                        arrUnitTarget[i] = null;
                    }
                }

                MgrObjectPool.Instance.HideObj("LaserGun_Target", arrSka[i].gameObject);
                arrSka[i] = null;

                if(arrUnitTarget[i] is not null)
                {
                    MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0002_b", 1.0f);
                    objBullet = MgrBulletPool.Instance.ShowObj("Bullet_LaserGun", weaponComp.transform.position + Vector3.up * 1.5f);
                    objBullet.GetComponent<Bullet>().SetBullet(unitBaseAlly, arrUnitTarget[i]);
                }

                await UniTask.Delay(250, cancellationToken: weaponComp.GetCancellationTokenOnDestroy());
            }
        }

        public override void OnReset()
        {
            base.OnReset();

            foreach (SkeletonAnimation ska in arrSka)
            {
                if (ska)
                    MgrObjectPool.Instance.HideObj("LaserGun_Target", ska.gameObject);
            }
            arrSka[0] = null;
            arrSka[1] = null;
            arrSka[2] = null;
            arrUnitTarget[0] = null;
            arrUnitTarget[1] = null;
            arrUnitTarget[2] = null;
        }
    }
}
