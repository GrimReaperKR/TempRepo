using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class BoosterPoint : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private TimelineAsset[] arrTimeline;

    public void SetBoosterPointTimeline(int _index)
    {
        playableDirector.playableAsset = arrTimeline[_index];
        playableDirector.Play();
    }
}
