using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldDropBakedFish : MonoBehaviour
{
    [SerializeField] private TrailRenderer bakedTrail;
    public TrailRenderer TrailrdBakedFish => bakedTrail;
    [field: SerializeField] public ParticleSystem ParsysStarTrail { get; private set; }
}
