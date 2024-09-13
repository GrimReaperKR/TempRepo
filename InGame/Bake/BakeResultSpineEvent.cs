using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Spine;
using DG.Tweening;

public class BakeResultSpineEvent : MonoBehaviour
{
    [SerializeField] private SkeletonGraphic skg;

    public void SetResult(string _grade)
    {
        skg.DOKill();
        skg.Initialize(true);

        skg.Skeleton.SetSkin(_grade);
        skg.color = Color.white;
        skg.gameObject.SetActive(true);
        skg.AnimationState.SetAnimation(0, "open", false);

        skg.AnimationState.Complete -= OnComplete;
        skg.AnimationState.Complete += OnComplete;
    }

    private void OnComplete(TrackEntry trackEntry)
    {
        string animationName = trackEntry.Animation.Name;
        if (animationName.Equals("open"))
            skg.DOFade(0.0f, 0.5f).OnComplete(() => skg.gameObject.SetActive(false));
    }
}
