using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit_Stat
{
    // 기본 데이터 정보
    public int UnitLevel; // 유닛 레벨..?

    public float Cost; // 코스트
    public float Atk; // 공격력
    public float HP; // 체력 (방어력은 현재 능력치에서 제외)
    public float MaxHP; // 최대 체력
    public float MoveSpeed; // 이동 속도
    public float Range; // 사정거리 (표기 X)
    public float WidthRange; // 사정거리[높이] (직선 상 유닛 체크 용)
    public float CriRate; // 치명타 확률
    public float CriDmg = 1.5f; // 치명타 대미지 (1.25배율로 모든 유닛 통일) -> 스킬 효과로 증가 여부 대비하여 변수 설정

    public SOBase_MoveUnit SoMove;
    // 인게임 용 정보
    //public int UnitUpgradeLevel;
}

public enum UNIT_STATE
{
    IDLE = 0,
    MOVE,
    SKILL,
    DEATH
}
