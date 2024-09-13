using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Spine;
using BCH.Database;
using TMPro;

public class WeaponSystem : MonoBehaviour
{
    private UnitBase unitAllyBase;

    public int WeaponOptionLevel { get; private set; }

    [SerializeField] public float WeaponCooldown { get; private set; } // 무기 쿨타임
    public SkeletonAnimation Ska { get; private set; }

    public WeaponData.WeaponSetting SOWeaponData { get; private set; } // 무기 정보 스크립터블 오브젝트
    public WeaponPersonalVariableInstance WeaponPersonalVariable { get; set; }

    public BoosterWeapon WeaponBoosterData { get; private set; }

    // 스킬 타임라인 변수
    public PlayableDirector UnitPlayableDirector { get; private set; }
    public PlayableAsset[] ArrPlayableAssets { get; private set; } // [타임라인 인덱스]

    private void Awake()
    {
        Ska = transform.GetChild(0).GetComponent<SkeletonAnimation>();
        UnitPlayableDirector = GetComponent<PlayableDirector>();

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 무기 설정
    /// </summary>
    /// <param name="_index">무기 인덱스</param>
    /// <param name="_optionLevel">무기 돌파 단계</param>
    public void SetWeapon(string _index, int _optionLevel, UnitBase _unitbase)
    {
        unitAllyBase = _unitbase;

        transform.position = unitAllyBase.transform.position;
        SOWeaponData = MgrInGameData.Instance.GetWeaponData(_index);
        WeaponOptionLevel = _optionLevel;

        Ska.skeletonDataAsset = SOWeaponData.SkdaWeapon;
        Ska.Initialize(true);

        ReSettingEvent();

        ArrPlayableAssets = new PlayableAsset[SOWeaponData.SOWeapon.ArrSkillTimeline.Length];
        BindingPlayableDirector();

        SetWeaponAnimation("idle", true);

        MgrBoosterSystem.Instance.SetBoosterUpgrade(MgrInGameData.Instance.GetBoosterData(SOWeaponData.WeaponIndex), unitAllyBase.TeamNum != 0);

        SOWeaponData.SOWeapon.OnInitialize(this, unitAllyBase);

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 무기 부스터 데이터 세팅
    /// </summary>
    /// <param name="_level">부스터 레벨</param>
    public void SetWeaponBoosterData(int _level) => WeaponBoosterData = DataManager.Instance.GetBoosterWeaponData($"{SOWeaponData.WeaponIndex}_{_level - 1}");

    /// <summary>
    /// 무기 쿨타임 설정
    /// </summary>
    /// <param name="_value">설정할 쿨타임</param>
    public void SetCoolDown(float _value)
    {
        float cooldownMultiply = 1.0f;
        if(MgrBoosterSystem.Instance.DicEtc.TryGetValue("skill_passive_001", out int _level))
            cooldownMultiply -= (float)DataManager.Instance.GetBoosterSkillData($"skill_passive_001_{_level - 1}").Params[0];

        cooldownMultiply += MgrBattleSystem.Instance.GlobalOption.Option_AllyBaseWeaponCoolDown;

        if (MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode || MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp) cooldownMultiply -= 0.5f;

        if (cooldownMultiply < 0.0f)
            cooldownMultiply = 0.0f;

        WeaponCooldown = _value * cooldownMultiply;
    }

    [SerializeField] private TextMeshPro tmpQACooldown;
    private void Update()
    {
        if(WeaponPersonalVariable is not null)
        {
            WeaponPersonalVariable?.OnMove();

            if (WeaponCooldown <= 0.0f)
            {
                if (tmpQACooldown.transform.parent.gameObject.activeSelf)
                    tmpQACooldown.transform.parent.gameObject.SetActive(false);

                if (!WeaponPersonalVariable.IsAttack && WeaponPersonalVariable.CheckCanUseSkill() && !unitAllyBase.CheckIsState(UNIT_STATE.DEATH))
                {
                    if (MgrBattleSystem.Instance.GameMode == GAME_MODE.GoldMode || MgrBattleSystem.Instance.GameMode == GAME_MODE.Pvp) Ska.timeScale = 1.3f;
                    else Ska.timeScale = 1.0f;

                    WeaponPersonalVariable?.OnSkill();
                }
            }
            else
            {
                if (!MgrBattleSystem.Instance.isStageStart || (MgrBattleSystem.Instance.ChapterID == 0 && MgrBattleSystem.Instance.GameMode == GAME_MODE.Chapter && MgrBattleSystem.Instance.TutorialStep <= 15))
                    return;

                WeaponCooldown -= Time.deltaTime;

                tmpQACooldown.text = $"{WeaponCooldown:F1}";
                if(!tmpQACooldown.transform.parent.gameObject.activeSelf)
                    tmpQACooldown.transform.parent.gameObject.SetActive(true);
            }
        }
    }

    private void OnEvent(TrackEntry trackEntry, Spine.Event e)
    {
        string eventName = e.Data.Name;

        if (eventName.Equals("attack") || eventName.Equals("skill"))
            WeaponPersonalVariable.EventTriggerSkill();
    }

    private void OnComplete(TrackEntry trackEntry)
    {
        string animationName = trackEntry.Animation.Name;
        WeaponPersonalVariable.EventTriggerEnd(animationName);
    }

    #region 무기 타임라인 바인딩
    private List<ControlTrack> listControlTrack = new List<ControlTrack>();
    private void BindingPlayableDirector()
    {
        // 각 스킬 스크립터블 오브젝트 내 타임라인 VFX 프리팹들 복제 생성
        for (int timelineIndex = 0; timelineIndex < ArrPlayableAssets.Length; timelineIndex++)
        {
            ArrPlayableAssets[timelineIndex] = SOWeaponData.SOWeapon.ArrSkillTimeline[timelineIndex].timelineAsset;

            listControlTrack.Clear();

            bool isSpineSetting = false;
            foreach (var output in ArrPlayableAssets[timelineIndex].outputs)
            {
                if (output.sourceObject is ControlTrack)
                {
                    listControlTrack.Add((ControlTrack)output.sourceObject);
                    continue;
                }

                // 스파인 바인딩
                if (!isSpineSetting && output.streamName.Contains("Spine Animation State Track"))
                {
                    UnitPlayableDirector.SetGenericBinding(output.sourceObject, Ska);
                    isSpineSetting = true;
                }
            }

            // 본 팔로워 있는지 체크
            BoneFollower follower;
            int childCnt = 0;
            for (int x = 0; x < SOWeaponData.SOWeapon.ArrSkillTimeline[timelineIndex].ArrObjTimelineVFXPrefab.Length; x++)
            {
                if (SOWeaponData.SOWeapon.ArrSkillTimeline[timelineIndex].ArrObjTimelineVFXPrefab[x].TryGetComponent(out follower))
                {
                    follower.skeletonRenderer = Ska;
                    childCnt += follower.transform.childCount;
                }
                else childCnt++;
            }

            // 타임라인 VFX 복제 및 바인딩
            int currIndex = 0;
            GameObject objTemp;
            for (int x = 0; x < SOWeaponData.SOWeapon.ArrSkillTimeline[timelineIndex].ArrObjTimelineVFXPrefab.Length; x++)
            {
                if (SOWeaponData.SOWeapon.ArrSkillTimeline[timelineIndex].ArrObjTimelineVFXPrefab[x].TryGetComponent(out follower))
                {
                    GameObject objFolower = Instantiate(SOWeaponData.SOWeapon.ArrSkillTimeline[timelineIndex].ArrObjTimelineVFXPrefab[x], transform);
                    for (int j = 0; j < objFolower.transform.childCount; j++)
                    {
                        BindingPrefab(listControlTrack[currIndex], objFolower.transform.GetChild(j).gameObject);
                        currIndex++;
                    }
                }
                else
                {
                    objTemp = Instantiate(SOWeaponData.SOWeapon.ArrSkillTimeline[timelineIndex].ArrObjTimelineVFXPrefab[x], transform);
                    BindingPrefab(listControlTrack[currIndex], objTemp);
                    objTemp.SetActive(false);
                    currIndex++;
                }
            }
        }

        UnitPlayableDirector.RebindPlayableGraphOutputs();
    }

    private void BindingPrefab(ControlTrack _track, GameObject _obj)
    {
        foreach (TimelineClip clip in _track.GetClips())
        {
            ControlPlayableAsset playableAsset = (ControlPlayableAsset)clip.asset;
            UnitPlayableDirector.SetReferenceValue(playableAsset.sourceGameObject.exposedName, _obj);
        }
    }
    #endregion

    public void PlayTimeline(int _timelineIndex = 0, bool _isLoop = false)
    {
        UnitPlayableDirector.playableAsset = ArrPlayableAssets[_timelineIndex];
        UnitPlayableDirector.extrapolationMode = _isLoop ? DirectorWrapMode.Loop : DirectorWrapMode.None;
        UnitPlayableDirector.Play();
        UpdatePlayableDirectorSpeed();
    }

    private void UpdatePlayableDirectorSpeed()
    {
        UnitPlayableDirector.playableGraph.GetRootPlayable(0).SetSpeed(Ska.timeScale);
        UnitPlayableDirector.Evaluate();
    }

    public void SetWeaponAnimation(string _name, bool _isLoop = false) => Ska.AnimationState.SetAnimation(0, _name, _isLoop);
    public void ReSettingEvent()
    {
        Ska.AnimationState.Complete -= OnComplete;
        Ska.AnimationState.Complete += OnComplete;

        Ska.AnimationState.Event -= OnEvent;
        Ska.AnimationState.Event += OnEvent;

        if(WeaponPersonalVariable is not null)
            WeaponPersonalVariable.ResetWeapon();
    }
}
