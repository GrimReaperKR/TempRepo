using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class Marker_Attack : Marker, INotification
{
    public PropertyName id { get { return new PropertyName("Attack"); } }
}
