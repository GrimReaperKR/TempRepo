using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Spine.Unity;
using Spine;

[CreateAssetMenu(fileName = "Weapon_5_Mine", menuName = "Weapon/5_Mine")]
public class Weapon_5_Mine : SOBase_Weapon
{
    public GameObject[] objMine;

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
        private UnitBase unitBaseAlly;
        private SkeletonAnimation[] arrSkaMine = new SkeletonAnimation[5];
        private int maxMine;
        private Vector3[] arrV3MinePos = new Vector3[]
        {
            new Vector3(-0.75f, 0.4f, 0.0f ),
            new Vector3(-0.85f, 1.0f, 0.001f ),
            new Vector3(-0.5f, 1.5f, 0.002f ),
            new Vector3(-1.4f, 0.7f, 0.003f ),
            new Vector3(-1.2f, 1.4f, 0.004f )
        };
        private int shootMineCnt;

        private List<UnitBase> listUnit = new List<UnitBase>();

        public void SetAllyBase(UnitBase _target)
        {
            unitBaseAlly = _target;
            weaponComp.Ska.GetComponent<MeshRenderer>().sortingLayerName = "Unit";

            for(int i = 0; i < arrSkaMine.Length; i++)
            {
                Weapon_5_Mine mine = weaponComp.SOWeaponData.SOWeapon as Weapon_5_Mine;

                arrSkaMine[i] = Instantiate(mine.objMine[0], weaponComp.transform.position, Quaternion.identity).GetComponent<SkeletonAnimation>();
                arrSkaMine[i].AnimationState.SetAnimation(0, "generate", false);
                arrSkaMine[i].transform.GetChild(0).gameObject.SetActive(false);
            }

            maxMine = (int)weaponComp.WeaponBoosterData.Params[1];
            if (weaponComp.WeaponOptionLevel >= 3)
                maxMine += (int)BCH.Database.DataManager.Instance.GetGearOptionValue("gear_weapon_0004", 2);

            SetMine();
            weaponComp.SetCoolDown((float)weaponComp.WeaponBoosterData.StartCooldown + 2.0f);
        }

        private void SetMine()
        {
            for(int i = 0; i < 5; i++)
            {
                arrSkaMine[i].gameObject.SetActive(i < maxMine);

                if(i == 0) arrSkaMine[i].skeleton.SetSkin("a");
                else if(i == 1) arrSkaMine[i].skeleton.SetSkin("b");
                else if (i == 2) arrSkaMine[i].skeleton.SetSkin("c");
                else if (i == 3) arrSkaMine[i].skeleton.SetSkin("d");
                else if (i == 4) arrSkaMine[i].skeleton.SetSkin("e");
                else arrSkaMine[i].skeleton.SetSkin("a");
                arrSkaMine[i].skeleton.SetSlotsToSetupPose();

                arrSkaMine[i].AnimationState.SetAnimation(0, "generate", false);
                arrSkaMine[i].AnimationState.AddAnimation(0, "idle", true, 0.0f);

                arrSkaMine[i].timeScale = 1.0f + i * 0.01f;
            }
        }

        public void OnBoosterUpgradeAction(string _index)
        {
            switch (_index)
            {
                case "gear_weapon_0004":
                    maxMine = (int)weaponComp.WeaponBoosterData.Params[1];
                    if (weaponComp.WeaponOptionLevel >= 3)
                        maxMine += (int)BCH.Database.DataManager.Instance.GetGearOptionValue("gear_weapon_0004", 2);

                    if (MgrBoosterSystem.Instance.DicWeapon[_index] >= 5)
                    {
                        for (int i = 0; i < arrSkaMine.Length; i++)
                        {
                            Weapon_5_Mine mine = weaponComp.SOWeaponData.SOWeapon as Weapon_5_Mine;

                            Destroy(arrSkaMine[i].gameObject);
                            arrSkaMine[i] = Instantiate(mine.objMine[1], weaponComp.transform.position, Quaternion.identity).GetComponent<SkeletonAnimation>();
                            arrSkaMine[i].AnimationState.SetAnimation(0, "generate", false);
                            arrSkaMine[i].transform.GetChild(0).gameObject.SetActive(false);
                        }
                    }

                    if (!IsAttack)
                        SetMine();
                    break;
                default:
                    break;
            }
        }

        public override void OnMove()
        {
            Vector3 v3End = unitBaseAlly.transform.position + Vector3.down * 0.5f + (unitBaseAlly.TeamNum == 0 ? Vector3.left : Vector3.right) * 0.5f;
            v3End.z = v3End.y * 0.01f;
            if (!IsAttack)
            {
                if (MathLib.CheckIsPosDistanceInRange(weaponComp.transform.position, v3End, 0.1f))
                {
                    if (!weaponComp.Ska.AnimationName.Equals("idle") && !weaponComp.Ska.AnimationName.Equals("death"))
                        weaponComp.SetWeaponAnimation("idle", true);
                }
                else
                {
                    if (!weaponComp.Ska.AnimationName.Equals("walk") && !weaponComp.Ska.AnimationName.Equals("death"))
                        weaponComp.SetWeaponAnimation("walk", true);
                }

                //Vector3 v3Dir = (v3End - weaponComp.transform.position).normalized;
                //float xValue = v3Dir.x < 0.0f ? -1.0f : v3Dir.x > 0.0f ? 1.0f : weaponComp.transform.localScale.x;
                //weaponComp.transform.localScale = new Vector3(xValue, 1.0f, 1.0f);

                weaponComp.transform.position = Vector3.Lerp(weaponComp.transform.position, v3End, Time.deltaTime * 3.0f);
            }

            for (int i = 0; i < maxMine; i++)
            {
                v3End = weaponComp.transform.position + new Vector3(weaponComp.transform.localScale.x == 1.0f ? arrV3MinePos[i].x : -arrV3MinePos[i].x, arrV3MinePos[i].y, arrV3MinePos[i].z);

                if (!IsAttack)
                {
                    if (MathLib.CheckIsPosDistanceInRange(arrSkaMine[i].transform.position, v3End, 0.1f))
                    {
                        if (!arrSkaMine[i].AnimationName.Equals("idle") && !arrSkaMine[i].AnimationName.Equals("generate"))
                            arrSkaMine[i].AnimationState.SetAnimation(0, "idle", true);
                    }
                    else
                    {
                        if (!arrSkaMine[i].AnimationName.Equals("walk") && !arrSkaMine[i].AnimationName.Equals("generate"))
                            arrSkaMine[i].AnimationState.SetAnimation(0, "walk", true);
                    }
                }

                arrSkaMine[i].transform.position = Vector3.Lerp(arrSkaMine[i].transform.position, v3End, Time.deltaTime * 2.5f);
            }
        }

        public override bool CheckCanUseSkill() => true;

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill"))
                return;

            IsAttack = false;
            SetMine();
            weaponComp.SetCoolDown((float)weaponComp.WeaponBoosterData.Cooldown);
            weaponComp.Ska.timeScale = 1.0f;
        }

        public override void EventTriggerSkill()
        {
            TaskSkill().Forget();
        }

        public override void OnSkill()
        {
            IsAttack = true;

            MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0004_a", 1.0f);

            shootMineCnt = maxMine;

            weaponComp.SetWeaponAnimation("skill1");
        }

        private async UniTaskVoid TaskSkill()
        {
            GameObject objBullet;
            SkeletonAnimation skaTemp;
            string skinName = "a";
            for (int i = 0; i < shootMineCnt; i++)
            {
                arrSkaMine[i].gameObject.SetActive(false);

                Vector3 v3Random = unitBaseAlly.transform.position + Vector3.right * (Random.Range(1.0f, 17.0f)) + Vector3.down * Random.Range(0.0f, 4.0f);

                // 기지 최하단 땅 보정
                if (v3Random.x < unitBaseAlly.transform.position.x + 7.0f && v3Random.y < -2.0f)
                    v3Random += Vector3.up * 2.0f;

                // 무기 옵션 [기지 앞에 지뢰 설치]
                bool isLastMine = false;
                if (weaponComp.WeaponOptionLevel >= 3 && i == shootMineCnt - 1)
                {
                    isLastMine = true;
                    v3Random = unitBaseAlly.transform.position + Vector3.right * 2.0f;
                }

                // 기지 전방으로 이동 중일 때 보정
                if (MgrBattleSystem.Instance.GetAllyBase().CheckIsState(UNIT_STATE.MOVE))
                    v3Random += Vector3.right * 4.0f;

                // 50% 확률로 타겟 유닛에게 랜덤 보정
                if(!isLastMine && MathLib.CheckPercentage((float)weaponComp.WeaponBoosterData.Params[3]))
                {
                    UnitBase target = MgrBattleSystem.Instance.GetRandomUnit(unitBaseAlly);
                    if(!(target is null) && target.transform.position.x - unitBaseAlly.transform.position.x <= (float)weaponComp.WeaponBoosterData.Range)
                        v3Random = target.transform.position + (Vector3.right * Random.Range(-(float)weaponComp.WeaponBoosterData.Params[4], (float)weaponComp.WeaponBoosterData.Params[4]));
                }

                objBullet = MgrBulletPool.Instance.ShowObj(MgrBoosterSystem.Instance.DicWeapon["gear_weapon_0004"] >= 5 ? "Bullet_Mine_lv5" : "Bullet_Mine", arrSkaMine[i].transform.position);
                objBullet.GetComponent<Bullet>().SetBullet(unitBaseAlly, _v3Pos: v3Random);

                if (i == 0) skinName = "a";
                else if (i == 1) skinName = "b";
                else if (i == 2) skinName = "c";
                else if (i == 3) skinName = "d";
                else if (i == 4) skinName = "e";

                skaTemp = objBullet.transform.GetChild(0).GetComponent<SkeletonAnimation>();
                skaTemp.skeleton.SetSkin(skinName);
                skaTemp.skeleton.SetSlotsToSetupPose();

                MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Weapon_0004_b", 1.0f);

                await UniTask.Delay(100, cancellationToken: weaponComp.GetCancellationTokenOnDestroy());
            }
        }
    }
}
