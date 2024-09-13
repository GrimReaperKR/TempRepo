using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public abstract class SOBase_Weapon : ScriptableObject
{
    [System.Serializable]
    public class WeaponTimeline
    {
        public TimelineAsset timelineAsset; // 해당 스킬 타임라인
        public GameObject[] ArrObjTimelineVFXPrefab; // 타임라인에 사용되는 VFX 프리팹
    }

    public WeaponTimeline[] ArrSkillTimeline;

    public abstract void OnInitialize(WeaponSystem _weapon, UnitBase _unitbase);
}
