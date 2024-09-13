using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitEffectData", menuName = "Data/UnitEffectData")]
public class UnitEffectData : ScriptableObject
{
    [System.Serializable]
    public class UnitEffectSetting
    {
        public string Name; // CC 이름
        public UNIT_EFFECT Index; // CC 인덱스 번호
        public bool IsMovedEffect; // 이동 방해 CC 효과인지
        public bool IsBlockedSkillEffect; // 스킬 사용 방해 CC 효과인지
        public SOBase_UnitEffectEvent soUnitEffectEvent; // CC 이벤트
    }

    public UnitEffectSetting[] unitEffectSetting;
}
