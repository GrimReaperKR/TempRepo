using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class SOMode_InGame : ScriptableObject
{
    protected MgrBattleSystem battleSys { get; private set; }

    // 모드 세팅 및 진입 연출, 모드 시작 관련 함수
    public virtual void InitMode(MgrBattleSystem _sys) => battleSys = _sys;
    public abstract UniTaskVoid InitModeShowEffect();
    public abstract void StartMode();

    // 웨이브 시스템 내부 함수
    public abstract float SetSpawnTime(bool _isNullData);

    public virtual void DoChallengeTask()
    {
    }

    public virtual void DoBossAppearedTask()
    {
    }

    public abstract bool CheckIsCanNextWave();
    public abstract void ModeSpawn();

    public virtual void SetBossAngryTimer()
    {
    }
} 