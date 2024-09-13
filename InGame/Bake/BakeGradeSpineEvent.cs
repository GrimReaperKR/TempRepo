using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Spine;
using DG.Tweening;

public class BakeGradeSpineEvent : MonoBehaviour
{
    [SerializeField] private SkeletonGraphic skg;
    [SerializeField] private SOBase_BakeGrade soGrade;

    public void SetGrade(string _grade)
    {
        switch(_grade)
        {
            case "Excellent":
                skg.skeletonDataAsset = soGrade.skdaExcellent;
                skg.transform.localScale = Vector3.one * 0.6f;
                break;
            case "Great":
                skg.skeletonDataAsset = soGrade.skdaGreat;
                skg.transform.localScale = Vector3.one * 0.8f;
                break;
            case "Good":
                skg.skeletonDataAsset = soGrade.skdaGood;
                skg.transform.localScale = Vector3.one * 0.8f;
                break;
            case "NotBad":
                skg.skeletonDataAsset = soGrade.skdaNotBad;
                skg.transform.localScale = Vector3.one * 0.7f;
                break;
            case "Bad":
                skg.skeletonDataAsset = soGrade.skdaBad;
                skg.transform.localScale = Vector3.one * 0.8f;
                break;
            default:
                break;
        }

        skg.DOKill();
        skg.Initialize(true);
        skg.color = Color.white;
        skg.gameObject.SetActive(true);
        skg.AnimationState.SetAnimation(0, "grade", false);

        skg.AnimationState.Complete -= OnComplete;
        skg.AnimationState.Complete += OnComplete;
    }

    private void OnComplete(TrackEntry trackEntry)
    {
        string animationName = trackEntry.Animation.Name;
        if (animationName.Equals("grade"))
            skg.DOFade(0.0f, 0.5f).OnComplete(() => skg.gameObject.SetActive(false));
    }
}
