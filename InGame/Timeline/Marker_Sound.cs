using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CustomStyle("SoundMarker")]
public class Marker_Sound : Marker, INotification
{
    public string SoundName;
    [Range(0.0f, 1.0f)]
    public float SoundVolume = 1.0f;

    public PropertyName id { get { return new PropertyName("Sound"); } }
}
