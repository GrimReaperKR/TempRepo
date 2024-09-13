using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

[CreateAssetMenu(fileName = "SO_BakedGrade", menuName = "SO_Ingame_System/SO_BakedGrade")]
public class SOBase_BakeGrade : ScriptableObject
{
    public SkeletonDataAsset skdaExcellent;
    public SkeletonDataAsset skdaGreat;
    public SkeletonDataAsset skdaGood;
    public SkeletonDataAsset skdaNotBad;
    public SkeletonDataAsset skdaBad;
}
