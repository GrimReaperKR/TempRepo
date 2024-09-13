using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

[CreateAssetMenu(fileName = "BackGroundData", menuName = "Data/BackGroundData")]
public class BackGroundData : ScriptableObject
{
    [System.Serializable]
    public class ChapterBackGround
    {
        public string Name;

        [Space(5)]
        public GAME_MODE GameMode;
        public int ModeLevel;
        public SOMode_InGame SOMode;

        [Space(5)]
        public AudioClip ClipBGM;

        [Space(15)]
        public Sprite sprBackGround;
        public Sprite sprBackGround_Changed;
        public float yPosBackGround;
        [Space(5)]
        public Sprite sprBackDistance;
        public Sprite sprBackDistance_Changed;
        public float yPosBackDistance;
        [Space(5)]
        public Sprite sprMiddleBack_Distance;
        public Sprite sprMiddleBack_Distance_Changed;
        public float yPosMiddleBack_Distance;
        [Space(5)]
        public Sprite sprNearBack_Distance;
        public Sprite sprNearBack_Distance_Changed;
        public float yPosNearBack_Distance;
        [Space(5)]
        public Sprite sprFloor;
        public Sprite sprFloor_Changed;
        public float yPosFloor;
        [Space(5)]
        public Sprite sprNear;
        public Sprite sprNear_Changed;
        public float yPosNear;
    }

    public ChapterBackGround[] data;
}
