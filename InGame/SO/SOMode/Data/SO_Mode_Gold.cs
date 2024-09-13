using System.Collections;
using System.Collections.Generic;
using BCH.Database;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Mode_Gold", menuName = "Data/Mode_Gold")]
public class SO_Mode_Gold : SOMode_InGame
{
    private float modeTimer;
    
    public override void InitMode(MgrBattleSystem _sys)
    {
        base.InitMode(_sys);
        
        if (MgrInGameUserData.Instance is not null)
        {
            for (int i = 0; i < battleSys.ArrUnitSlotBtn.Length; i++)
                battleSys.ArrUnitSlotBtn[i].SetUnitSlot(DataManager.Instance.UserInventory.unitDeck[MgrInGameUserData.Instance.CurrUnitDeckIndex][i]);

            MgrBakingSystem.Instance.S_Combo = DataManager.Instance.UserChapter.sRankCombo;
            MgrBakingSystem.Instance.S_MaxCombo = DataManager.Instance.UserChapter.maxSRankCombo;
        }

        MgrUnitPool.Instance.ShowObj("Catbot_001", 0, new Vector3(-12.0f, 0.0f, 0.0f));

    }

    public override async UniTaskVoid InitModeShowEffect()
    {
        battleSys.ShowEnterEffect_Left();
        battleSys.TaskChapterTitle().Forget();
        
        await UniTask.Delay(500, cancellationToken: battleSys.GetCancellationTokenOnDestroy());
        
        await battleSys.ShowEnterEffect_Bottom().AttachExternalCancellation(battleSys.GetCancellationTokenOnDestroy());
        
        await UniTask.Delay(66, cancellationToken: battleSys.GetCancellationTokenOnDestroy());
        
        battleSys.ShowEnterEffect_Top();

        await UniTask.Delay(1000, cancellationToken: battleSys.GetCancellationTokenOnDestroy());
        
        MgrInGameEvent.Instance.BroadcastCanSpawnEvent();
        MgrBakingSystem.Instance.InitBakingData();
    }

    public override void StartMode()
    {
        battleSys.TaskWaveSystem().Forget();
    }

    public override float SetSpawnTime(bool _isNullData)
    {
        if (_isNullData)
            return 20.0f;
        
        return (float)battleSys.EnemySpawnData.SpawnTime;
    }

    public override bool CheckIsCanNextWave()
    {
        if (!battleSys.IsBossAppeared)
        {
            battleSys.AddCurrWave();
        }
        else
        {
            if(!battleSys.CheckIsBossAlive())
            {
                battleSys.TaskWaveSystem().Forget();
                return false;
            }
        }

        return true;
    }

    public override void ModeSpawn()
    {
        if(!battleSys.IsBossAppeared)
        {
            TaskSpawn().Forget();
            battleSys.IsBossAppeared = true;
            
            battleSys.ModeTimer = 10000.0f;
            battleSys.GoldCollectAmount = 0.0f;
            battleSys.ObjTimerSlider.SetActive(true);
            battleSys.ImgTimerSlider.fillAmount = 0.0f;
            TaskGoldModeTimer().Forget();
        }
    }
    
    private async UniTaskVoid TaskSpawn()
    {
        float spawnTimer = 0.0f;
        while (battleSys.isStageStart)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer < 0.0f)
            {
                List<string> listRandomBoss = new List<string>();
                UnitData monsterData = MgrInGameData.Instance.SOMonsterData;
                foreach (var unitData in monsterData.unitSetting)
                {
                    if (!unitData.isActivate || unitData.unitType != UnitType.Monster)
                        continue;

                    listRandomBoss.Add(unitData.unitIndex);
                }

                int spawnCnt = Random.Range(2, 5 + 1);
                battleSys.ReserveSpawnCnt += spawnCnt;
                battleSys.TaskSpawnEnemy(listRandomBoss[Random.Range(0, listRandomBoss.Count)], spawnCnt, 0.0f, false, 0).Forget();
                
                spawnTimer = 5.0f;
            }

            await UniTask.Yield(battleSys.GetCancellationTokenOnDestroy());
        }
    }

    private async UniTaskVoid TaskGoldModeTimer()
    {
        while(battleSys.isStageStart && battleSys.GoldCollectAmount < battleSys.ModeTimer)
        {
            //GoldModeAngryLevel = (int)((90.0f - modeTimer) / 5.0f);
            battleSys.ImgTimerSlider.fillAmount = battleSys.GoldCollectAmount / battleSys.ModeTimer;
            await UniTask.Yield(cancellationToken: battleSys.GetCancellationTokenOnDestroy());
        }
        battleSys.ImgTimerSlider.fillAmount = 1.0f;

        if (battleSys.isStageStart)
            battleSys.SetEndBattle(false);
    }
}
