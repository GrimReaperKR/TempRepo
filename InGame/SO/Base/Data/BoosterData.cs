using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BoosterType
{
    Active = 0,
    Passive,
    Weapon,
    UnitUpgrade,
    Tycoon,
    Etc,
}

[CreateAssetMenu(fileName = "BoosterData", menuName = "Data/BoosterData")]
public class BoosterData : ScriptableObject
{
    [System.Serializable]
    public class BoosterInfo
    {
        public string Index; // 인덱스
        public string TitleName; // 이름
        public Sprite Icon; // 아이콘
        public BoosterType Type; // 부스터 분류 타입
        public int StartLevel = 0; // 초기 레벨
        public int MaxLevel = 5; // 최대 레벨
        public string[] Desc; // 레벨 당 업그레이드 설명
        public string[] SkillBtnDesc; // 스킬 버튼 설명
    }

    public BoosterInfo[] boosterInfo;
}
