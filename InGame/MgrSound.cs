using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class MgrSound : MonoBehaviour
{
    private static MgrSound instance = null;
    public static MgrSound Instance => instance;

    [SerializeField] private AudioSource audioBGM;
    [SerializeField] private AudioSource audioBGM_Back;
    [SerializeField] private AudioSource audioSFX;

    [SerializeField] private OptionConfigSO OptionConfigSO;

    private Dictionary<string, AudioClip> dicAudioClip = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance is not null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region LOAD/UNLOAD SOUND
    public enum LOAD_SOUND
    {
        INGAME,
    }

    private AsyncOperationHandle<IList<AudioClip>> handleIngamePreload;
    private List<AsyncOperationHandle<IList<AudioClip>>> listHandleInGameUnitSound = new List<AsyncOperationHandle<IList<AudioClip>>>();
    private List<AsyncOperationHandle<IList<AudioClip>>> listHandleThemeSound = new List<AsyncOperationHandle<IList<AudioClip>>>();

    public async UniTask LoadSound(LOAD_SOUND _index)
    {
        switch (_index)
        {
            case LOAD_SOUND.INGAME:
                handleIngamePreload = await LoadAddressableSound("INGAME_SOUND_PRELOAD");
                break;
        }

        await Resources.UnloadUnusedAssets().ToUniTask();
    }

    public async UniTask LoadInGameUnitSound(string _unitIndex)
    {
        await TaskLoadUnitSound(_unitIndex);
        await Resources.UnloadUnusedAssets().ToUniTask();
    }

    private async UniTask TaskLoadUnitSound(string _unitIndex)
    {
        string[] unitIndex = _unitIndex.Split(':');

        listHandleInGameUnitSound.Clear();
        for (int i = 0; i < unitIndex.Length; i++)
            listHandleInGameUnitSound.Add(await LoadAddressableSound($"INGAME_UNIT/{unitIndex[i]}"));
    }

    public async UniTask LoadThemeSound(int _themeIndex)
    {
        listHandleThemeSound.Clear();
        listHandleThemeSound.Add(await LoadAddressableSound($"INGAME_MONSTER/Theme{_themeIndex}"));
        listHandleThemeSound.Add(await LoadAddressableSound($"INGAME_BACK/Theme{_themeIndex}"));
        await Resources.UnloadUnusedAssets().ToUniTask();
    }

    public void UnloadSound(LOAD_SOUND _index)
    {
        switch (_index)
        {
            case LOAD_SOUND.INGAME:
                ReleaseAddressableSound(handleIngamePreload);
                ReleaseInGameListHandle();
                break;
        }
    }

    private async UniTask<AsyncOperationHandle<IList<AudioClip>>> LoadAddressableSound(string _label)
    {
        var handle = Addressables.LoadAssetsAsync<AudioClip>(_label, (loaded) => {
            if (!dicAudioClip.ContainsKey(loaded.name)) dicAudioClip.Add(loaded.name, loaded);
            else Debug.LogError($"[SOUND LOAD ERROR] - 중복된 사운드 파일 이름 {loaded.name}");
        });
        await handle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

        return handle;
    }

    private void ReleaseAddressableSound(AsyncOperationHandle<IList<AudioClip>> _handle)
    {
        if (_handle.IsValid())
        {
            for (int i = 0; i < _handle.Result.Count; i++)
                dicAudioClip.Remove(_handle.Result[i].name);

            Addressables.Release(_handle);
        }
    }

    private void ReleaseInGameListHandle()
    {
        foreach (var handle in listHandleInGameUnitSound)
            ReleaseAddressableSound(handle);

        foreach (var handle in listHandleThemeSound)
            ReleaseAddressableSound(handle);

        listHandleInGameUnitSound.Clear();
        listHandleThemeSound.Clear();
    }

    #endregion

    #region BGM
    /// <summary>
    /// BGM 재생
    /// </summary>
    /// <param name="_clip">재생할 오디오 클립</param>
    /// <param name="_volume">볼륨</param>
    public void StartBGM(AudioClip _clip, float _volume = 1.0f)
    {
        if (OptionConfigSO.IsBGMMute)
            return;

        audioBGM.clip = _clip;
        audioBGM.loop = true;
        audioBGM.volume = OptionConfigSO.BGMVolume * _volume;
        audioBGM.Play();
    }

    public void StopBGM() => audioBGM.Stop();
    #endregion

    #region SFX
    private const int MaxAudioSFXChannel = 3; // 효과음 최대 겹칠 수 있는 갯수
    private const float MaxDelay = 0.075f; // 효과음 최대 겹침 시간 제한
    private Dictionary<SFX_Info, float> dicSFXInfo = new Dictionary<SFX_Info, float>();

    /// <summary>
    /// SFX 재생 (최대 3채널 유지 가능)
    /// </summary>
    /// <param name="_clip">재생할 오디오 클립</param>
    /// <param name="_volume">볼륨</param>
    public void PlaySFX(AudioClip _clip, float _volume = 1.0f)
    {
        if (OptionConfigSO.IsSFXMute)
            return;

        int channel = GetSFXAudioChannel(_clip.name);
        if(channel > -1)
        {
            SFX_Info info = new SFX_Info();
            info.AudioClipName = _clip.name;
            info.AudioChannel = channel;
            dicSFXInfo.Add(info, MaxDelay);

            audioSFX.PlayOneShot(_clip, OptionConfigSO.SFXVolume * 0.7f * _volume);
            TaskAudioDicRemove(info).Forget();
        }
        //else 가장 오래된 사운드를 제거하고 새로 재생하는 함수
        //{
        //    TaskAudioRemoveAndPlayNew(_clip, _volume).Forget();
        //}
    }
    
    /// <summary>
    /// SFX 1회성 재생 (0.075초 내 최대 3채널 유지 가능)
    /// </summary>
    /// <param name="_clipName">재생할 오디오 클립 이름</param>
    /// <param name="_volume">볼륨</param>
    public void PlayOneShotSFX(string _clipName, float _volume = 1.0f)
    {
        if (OptionConfigSO.IsSFXMute)
            return;

        AudioClip clip;
        if(!dicAudioClip.TryGetValue(_clipName, out clip))
        {
            Debug.LogWarning($"[EMIT SOUND] - {_clipName} 로드 되지 않거나 없는 사운드 이름입니다.");
            return;
        }

        int channel = GetSFXAudioChannel(clip.name);
        if(channel > -1)
        {
            SFX_Info info = new SFX_Info();
            info.AudioClipName = clip.name;
            info.AudioChannel = channel;
            dicSFXInfo.Add(info, MaxDelay);

            audioSFX.PlayOneShot(clip, OptionConfigSO.SFXVolume * 0.7f * _volume);
            TaskAudioDicRemove(info).Forget();
        }
        //else
        //{
        //    TaskAudioRemoveAndPlayNew(_clip, _volume).Forget();
        //}
    }

    private async UniTaskVoid TaskAudioDicRemove(SFX_Info _info)
    {
        while (dicSFXInfo.TryGetValue(_info, out float duration) && duration > 0.0f)
        {
            dicSFXInfo[_info] -= Time.unscaledDeltaTime;
            await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        if (dicSFXInfo.ContainsKey(_info))
            dicSFXInfo.Remove(_info);
    }
    
    private async UniTaskVoid TaskAudioRemoveAndPlayNew(AudioClip _clip, float _volume)
    {
        SFX_Info info = new SFX_Info();
        info.AudioClipName = _clip.name;

        int index = -1;
        float prevRemainTime = 99999.0f;
        for (int i = 0; i < 3; i++)
        {
            info.AudioChannel = i;
            if (dicSFXInfo.ContainsKey(info) && dicSFXInfo[info] < prevRemainTime)
                index = i;
        }

        if (index > 0)
        {
            info.AudioChannel = index;
            dicSFXInfo.Remove(info);
        }

        await UniTask.NextFrame(cancellationToken: this.GetCancellationTokenOnDestroy());

        dicSFXInfo.Add(info, _clip.length);

        audioSFX.PlayOneShot(_clip, OptionConfigSO.SFXVolume * 0.7f * _volume);

        TaskAudioDicRemove(info).Forget();
    }

    private List<AudioSource> listSFXPool = new List<AudioSource>();
    private List<AudioSource> listSFXUsedPool = new List<AudioSource>();
    private Dictionary<SFX_Info, AudioSource> dicSFXSubInfo = new Dictionary<SFX_Info, AudioSource>();

    [SerializeField] private GameObject objSFXSub;

    /// <summary>
    /// SFX 재생 (일시 중지, 정지 가능)
    /// </summary>
    /// <param name="_clipName">사운드 파일 이름</param>
    /// <param name="_volume">볼륨</param>
    /// <returns>해당 사운드가 재생된 채널</returns>
    public int PlaySFX(string _clipName, float _volume = 1.0f, bool _isLoop = false)
    {
        if (OptionConfigSO.IsSFXMute)
            return -1;

        AudioClip clip;
        if (!dicAudioClip.TryGetValue(_clipName, out clip))
        {
            Debug.LogWarning($"[EMIT SOUND] - {_clipName} 로드 되지 않거나 없는 사운드 이름입니다.");
            return - 1;
        }

        int channel = GetSFXSubAudioChannel(clip.name);
        if (channel > -1)
        {
            AudioSource audioSub;
            if (listSFXPool.Count < 1)
            {
                audioSub = Instantiate(objSFXSub, transform).GetComponent<AudioSource>();
                listSFXUsedPool.Add(audioSub);
            }
            else
            {
                audioSub = listSFXPool[0];
                audioSub.gameObject.SetActive(true);
                listSFXPool.Remove(audioSub);
                listSFXUsedPool.Add(audioSub);
            }

            SFX_Info info = new SFX_Info();
            info.AudioClipName = clip.name;
            info.AudioChannel = channel;
            dicSFXSubInfo.Add(info, audioSub);

            audioSub.clip = clip;
            audioSub.volume = OptionConfigSO.SFXVolume * 0.7f * _volume;
            audioSub.loop = _isLoop;
            audioSub.Play();
            audioSub.time = 0.01f;

            TaskSubAudioDicRemove(audioSub, info).Forget();
        }
        return channel;
    }

    /// <summary>
    /// 특정 SFX 일시 정지
    /// </summary>
    /// <param name="_clipName">사운드 파일 이름</param>
    /// <param name="_channel">채널</param>
    public void PauseSFX(string _clipName, int _channel)
    {
        SFX_Info info = new SFX_Info();
        info.AudioClipName = _clipName;
        info.AudioChannel = _channel;
        if(dicSFXSubInfo.TryGetValue(info, out AudioSource audio))
            audio.Pause();
    }
    
    /// <summary>
    /// 특정 SFX 일시 정지 해제
    /// </summary>
    /// <param name="_clipName">사운드 파일 이름</param>
    /// <param name="_channel">채널</param>
    public void UnPauseSFX(string _clipName, int _channel)
    {
        SFX_Info info = new SFX_Info();
        info.AudioClipName = _clipName;
        info.AudioChannel = _channel;
        if(dicSFXSubInfo.TryGetValue(info, out AudioSource audio))
            audio.UnPause();
    }
    
    /// <summary>
    /// 특정 SFX 정지
    /// </summary>
    /// <param name="_clipName">사운드 파일 이름</param>
    /// <param name="_channel">체날</param>
    public void StopSFX(string _clipName, int _channel)
    {
        SFX_Info info = new SFX_Info();
        info.AudioClipName = _clipName;
        info.AudioChannel = _channel;
        if(dicSFXSubInfo.TryGetValue(info, out AudioSource audio))
            audio.Stop();
    }

    /// <summary>
    /// 모든 PlaySFX() 함수로 실행된 사운드 일시 정지
    /// </summary>
    public void PauseAllSFX()
    {
        foreach(AudioSource audio in listSFXUsedPool)
            audio.Pause();
    }

    /// <summary>
    /// 모든 PlaySFX() 함수로 실행된 사운드 일시 정지 해제
    /// </summary>
    public void UnPauseAllSFX()
    {
        foreach(AudioSource audio in listSFXUsedPool)
            audio.UnPause();
    }

    private async UniTaskVoid TaskSubAudioDicRemove(AudioSource _audioSub, SFX_Info _info)
    {
        await UniTask.WaitUntil(() => !_audioSub.isPlaying && _audioSub.time == 0, cancellationToken: this.GetCancellationTokenOnDestroy());

        if (dicSFXSubInfo.ContainsKey(_info))
            dicSFXSubInfo.Remove(_info);

        listSFXUsedPool.Remove(_audioSub);
        listSFXPool.Add(_audioSub);
        _audioSub.gameObject.SetActive(false);
    }

    private int GetSFXAudioChannel(string _audioName)
    {
        SFX_Info sfx = new SFX_Info();
        sfx.AudioClipName = _audioName;
        for (int i = 0; i < MaxAudioSFXChannel; i++)
        {
            sfx.AudioChannel = i;
            if (!dicSFXInfo.ContainsKey(sfx))
                return i;
        }
        return -1;
    }
    
    private int GetSFXSubAudioChannel(string _audioName)
    {
        SFX_Info sfx = new SFX_Info();
        sfx.AudioClipName = _audioName;
        for (int i = 0; i < MaxAudioSFXChannel; i++)
        {
            sfx.AudioChannel = i;
            if (!dicSFXSubInfo.ContainsKey(sfx))
                return i;
        }
        return -1;
    }

    private class SFX_Info
    {
        public string AudioClipName;
        public int AudioChannel;

        public override int GetHashCode()
        {
            return AudioClipName.GetHashCode() + AudioChannel.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            SFX_Info info = obj as SFX_Info;
            return info != null && (info.AudioClipName == this.AudioClipName && info.AudioChannel == this.AudioChannel);
        }
    }
    #endregion
}
