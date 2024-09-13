using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BulletData", menuName = "Data/Bullet")]
public class BulletData : ScriptableObject
{
    [System.Serializable]
    public class BulletSetting
    {
        public string bulletIndex;
        public string bulletHitToPlayerPrefabName;
        public GameObject objBulletVFX;
        public float bulletSpeedOrTime; // 총알 속도 또는 시간 (시간은 베지어 이동 등에 사용)
        public SOBase_Bullet soBullet;
    }

    public BulletSetting[] bulletSetting;
}
