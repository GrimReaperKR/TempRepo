using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class Notification_SoundMarker : MonoBehaviour, INotificationReceiver
{
    private PropertyName propertySound = new PropertyName("Sound");
    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification.id.Equals(propertySound))
        {
            Marker_Sound sound = notification as Marker_Sound;
            MgrSound.Instance.PlayOneShotSFX(sound.SoundName, sound.SoundVolume);
        }
    }
}
