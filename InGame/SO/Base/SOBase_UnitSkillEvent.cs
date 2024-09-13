using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public abstract class SOBase_UnitSkillEvent : ScriptableObject
{
    [System.Serializable]
    public class SkillTimeline
    {
        public TimelineAsset timelineAsset; // 해당 스킬 타임라인
        public GameObject[] ArrObjTimelineVFXPrefab; // 타임라인에 사용되는 VFX 프리팹
    }

    public SkillTimeline[] ArrSkillTimeline;

    //public bool isPassive; // 패시브 스킬인지 여부

    public abstract void InitializeSkill(UnitBase _unitBase); // 스킬 초기 세팅
    //public virtual void ResetInit(UnitBase _unitBase) { } // 스킬 취소 시 초기화 시킬 항목 함수
}
