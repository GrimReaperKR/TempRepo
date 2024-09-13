using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOBullet_Bullet_S_Spt_01_S1", menuName = "Bullet/Bullet_S_Spt_01_S1")]
public class SOBullet_Bullet_S_Spt_01_S1 : SOBase_Bullet
{
    public override void OnInitialize(Bullet _bullet)
    {
        _bullet.BulletPersonalVariable = new PersonalVariable();
        _bullet.BulletPersonalVariable.SetData(_bullet);
    }

    private class PersonalVariable : BulletPersonalVariableInstance
    {
        public override void OnMove()
        {
            bulletComp.transform.position += (bulletComp.target.GetUnitCenterPos() - bulletComp.transform.position).normalized * bulletComp.bulletSetting.bulletSpeedOrTime * Time.deltaTime;

            if ((bulletComp.transform.position - bulletComp.target.GetUnitCenterPos()).sqrMagnitude <= 0.5f * 0.5f)
                bulletComp.IsReach = true;
        }

        public override void OnHit()
        {
            if (!bulletComp.target.CheckHasGaramDamagedEvent() && !bulletComp.target.CheckIsState(UNIT_STATE.DEATH))
            {
                MgrSound.Instance.PlayOneShotSFX("SFX_S_Spt_01_s1_3", 1.0f);

                S_Spt_01_Garam_S1.GaramPersonalVariable personal = bulletComp.owner.UnitSkillPersonalVariable[0] as S_Spt_01_Garam_S1.GaramPersonalVariable;
                bulletComp.target.AddGaramDamagedEvent(personal.OnDamagedChain);
                bulletComp.owner.ListGaramTarget.Add(bulletComp.target);

                GameObject objVFX = MgrObjectPool.Instance.ShowObj("FX_S_Spt_01_Garam_skill1_mark", bulletComp.target.GetUnitCenterPos());
                objVFX.transform.SetParent(bulletComp.target.transform);

                BoneFollower bf = objVFX.GetComponent<BoneFollower>();
                bf.skeletonRenderer = bulletComp.target.Ska;
                if (!bf.SetBone("head"))
                    Debug.LogError($"{bulletComp.target.gameObject.name} >> head 본 없음");
                bf.Initialize();
                bf.enabled = true;

                TaskSkill(bulletComp.owner, bulletComp.target, objVFX).Forget();
            }
            MgrObjectPool.Instance.ShowObj(bulletComp.bulletSetting.bulletHitToPlayerPrefabName, bulletComp.transform.position);
            MgrBulletPool.Instance.HideObj(bulletComp.gameObject.name, bulletComp.gameObject);
        }

        private async UniTaskVoid TaskSkill(UnitBase _owner, UnitBase _target, GameObject _objVFX)
        {
            float duration = bulletComp.owner.GetUnitSkillFloatDataValue(0, "param.0");

            while(duration > 0.0f)
            {
                if (_target.CheckIsState(UNIT_STATE.DEATH))
                    break;

                duration -= Time.deltaTime;
                await UniTask.Yield(cancellationToken: bulletComp.GetCancellationTokenOnDestroy());
            }

            S_Spt_01_Garam_S1.GaramPersonalVariable personal = _owner.UnitSkillPersonalVariable[0] as S_Spt_01_Garam_S1.GaramPersonalVariable;
            _target.RemoveGaramDamagedEvent(personal.OnDamagedChain);
            _owner.ListGaramTarget.Remove(_target);

            _objVFX.GetComponent<BoneFollower>().enabled = false;

            MgrObjectPool.Instance.HideObj("FX_S_Spt_01_Garam_skill1_mark", _objVFX);
        }
    }
}
