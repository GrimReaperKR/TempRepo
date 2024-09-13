using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Data/WeaponData")]
public class WeaponData : ScriptableObject
{
    [System.Serializable]
    public class WeaponSetting
    {
        public string WeaponName;
        public string WeaponIndex;
        public SkeletonDataAsset SkdaWeapon;
        public SOBase_Weapon SOWeapon;
    }

    public WeaponSetting[] data;
}
