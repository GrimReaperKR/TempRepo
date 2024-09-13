using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public enum UnitType
{
    Unit = 0,
    AllyBase,
    Monster,
    EnemyUnit,
    Elite,
    MidBoss,
    Boss
}

public enum UnitClass
{
    None = 0,
    Warrior,
    Arch,
    Tank,
    Supporter
}

public enum UnitGrade
{
    None = 0,
    C,
    B,
    A,
    S
}

[CreateAssetMenu(fileName = "UnitData", menuName = "Data/UnitData")]
public class UnitData : ScriptableObject
{
    [System.Serializable]
    public class UnitSetting
    {
        public string unitName;
        public string unitIndex;
        public bool isActivate = true;
        public Sprite unitIcon;
        public Sprite unitCameraOutIcon;
        public UnitGrade unitGrade = UnitGrade.None;
        public UnitType unitType = UnitType.Unit;
        public UnitClass unitClass = UnitClass.None;
        public int unitCost;
        public SkeletonDataAsset spineDataAsset;
        public SOBase_MoveUnit moveSO;
        public SOBase_UnitSkillEvent[] unitSkill;
    }

    public UnitSetting[] unitSetting;
}
