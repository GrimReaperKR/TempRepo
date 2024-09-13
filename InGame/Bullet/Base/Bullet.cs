using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class Bullet : MonoBehaviour
{
    // Bullet 내 필요 변수
    public UnitBase owner { get; private set; }
    public UnitBase target { get; private set; }
    public Vector3 v3LastPos { get; private set; }
    public GameObject objBulletVFX { get; private set; }

    public bool IsReach { get; set; }

    // Bullet 발생 시점 스탯 저장
    public float bulletAtk;
    public float bulletCriRate;
    public float bulletCriDmg;

    // SO SETTING
    public BulletData.BulletSetting bulletSetting { get; private set; }
    public SOBase_Bullet SoBullet { get; private set; }
    public BulletPersonalVariableInstance BulletPersonalVariable { get; set; }

    public void SetBulletSetting(BulletData.BulletSetting _setting)
    {
        bulletSetting = _setting;
        objBulletVFX = Instantiate(bulletSetting.objBulletVFX, transform);
    }
    public void SetOwner(UnitBase _owner) => owner = _owner;
    public void SetTarget(UnitBase _target) => target = _target;
    public void SetTargetPos(Vector3 _v3Pos) => v3LastPos = _v3Pos;

    public void SetBullet(UnitBase _owner, UnitBase _target = null, Vector3 _v3Pos = new Vector3())
    {
        owner = _owner;
        target = _target;
        v3LastPos = _v3Pos == Vector3.zero ? _target.transform.position : _v3Pos;

        bulletAtk = owner.GetAtk();
        bulletCriRate = owner.GetCriRate();
        bulletCriDmg = owner.UnitStat.CriDmg;

        IsReach = false;
        SoBullet = bulletSetting.soBullet;
        SoBullet.OnInitialize(this);

        objBulletVFX.SetActive(true);

        TaskBulletUpdate().Forget();
    }

    public void ChangeTarget(UnitBase _target) => target = _target;

    private async UniTaskVoid TaskBulletUpdate()
    {
        while (!IsReach)
        {
            // 이동
            BulletPersonalVariable?.OnMove();

            await UniTask.Yield(this.GetCancellationTokenOnDestroy());
        }

        // 히트!
        BulletPersonalVariable?.OnHit();
    }
}
