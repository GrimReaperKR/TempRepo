using System.Collections;
using System.Collections.Generic;
using BCH.Database;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Mode_Chapter", menuName = "Data/Mode_Chapter")]
public class SO_Mode_Chapter : SOMode_InGame
{
    private float challengeTimer = 0.0f;
    private float bossAngryTimer = 0.0f;

    public override void InitMode(MgrBattleSystem _sys)
    {
        base.InitMode(_sys);
        
        if(battleSys.GameMode == GAME_MODE.Chapter && battleSys.ChapterID == 0)
        {
            battleSys.ArrUnitSlotBtn[0].SetUnitSlot("S_War_01");
            battleSys.ArrUnitSlotBtn[1].SetUnitSlot("S_Arch_02");
            battleSys.ArrUnitSlotBtn[2].SetUnitSlot("B_Tank_01");
            battleSys.ArrUnitSlotBtn[3].SetUnitSlot("B_Spt_02");
            battleSys.ArrUnitSlotBtn[4].SetUnitSlot(null);
            battleSys.ArrUnitSlotBtn[5].SetUnitSlot(null);

            DataManager.Instance.UserInventory.traitLv = 0;
        }
        else
        {
            if (MgrInGameUserData.Instance is not null)
            {
                for (int i = 0; i < battleSys.ArrUnitSlotBtn.Length; i++)
                    battleSys.ArrUnitSlotBtn[i].SetUnitSlot(DataManager.Instance.UserInventory.unitDeck[MgrInGameUserData.Instance.CurrUnitDeckIndex][i]);

                MgrBakingSystem.Instance.S_Combo = DataManager.Instance.UserChapter.sRankCombo;
                MgrBakingSystem.Instance.S_MaxCombo = DataManager.Instance.UserChapter.maxSRankCombo;
            }
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
        
        // TODO : (구)타이쿤 UI
        // if (battleSys.ChapterID == 0)
        // {
        //     for (int i = 0; i < arrRtShowEffect_Right.Length; i++)
        //         arrRtShowEffect_Right[i].DOAnchorPosX(arrRtShowEffect_Right[i].anchoredPosition.x - 800.0f, 1.0f).SetEase(Ease.OutBack, 0.75f);
        // }
        
        battleSys.ShowEnterEffect_Top();

        await UniTask.Delay(1000, cancellationToken: battleSys.GetCancellationTokenOnDestroy());
        
        MgrInGameEvent.Instance.BroadcastCanSpawnEvent();
        MgrBakingSystem.Instance.InitBakingData();

        MgrBoosterSystem.Instance.AddBoosterLv(battleSys.GlobalOption.Option_AddStartBoosterLv);

        if (battleSys.ChapterID == 0)
            TaskTutorial().Forget();
    }

    public override void StartMode()
    {
        battleSys.TaskWaveSystem().Forget();
    }

    public override float SetSpawnTime(bool _isNullData)
    {
        if (_isNullData)
            return battleSys.ChapterID == 0 ? 999999.0f : 20.0f;
        
        return (float)battleSys.EnemySpawnData.SpawnTime;
    }

    public override bool CheckIsCanNextWave()
    {
        // 보스가 나온 상태가 아니면 웨이브 수 증가
        if (!battleSys.IsBossAppeared)
        {
            battleSys.AddCurrWave();
        }
        else
        {
            if(!battleSys.CheckIsBossAlive())
            {
                battleSys.TaskWaveSystem().Forget();
                battleSys.SetNextWave();
                return false;
            }
        }

        return true;
    }

    public override void SetBossAngryTimer() => bossAngryTimer = 180;

    public override void DoChallengeTask()
    {
        if(battleSys.IsChallengeMode && battleSys.ChallengeLevel == 2)
        {
            challengeTimer += Time.deltaTime;

            switch(battleSys.GetCurrentThema())
            {
                case 1:
                    if(challengeTimer >= (float)DataManager.Instance.GetChallengePenaltyData("penalty_000006").Cooldown)
                    {
                        challengeTimer -= (float)DataManager.Instance.GetChallengePenaltyData("penalty_000006").Cooldown;
                        TaskChallengePenaltyWood().Forget();
                    }
                    break;
                case 3:
                    if (challengeTimer >= (float)DataManager.Instance.GetChallengePenaltyData("penalty_000008").Cooldown)
                    {
                        challengeTimer -= (float)DataManager.Instance.GetChallengePenaltyData("penalty_000008").Cooldown;

                        List<UnitSlotBtn> listUnitSlot = new List<UnitSlotBtn>();

                        foreach (var unitSlot in battleSys.ArrUnitSlotBtn)
                        {
                            if (unitSlot.UnitInfo is null || unitSlot.ChallengeTimer > 0.0f)
                                continue;

                            listUnitSlot.Add(unitSlot);
                        }
                        listUnitSlot.Shuffle();

                        MgrSound.Instance.PlayOneShotSFX("SFX_Challenge_C3_Penalty_a", 1.0f);

                        int randomCnt = listUnitSlot.Count > (int)DataManager.Instance.GetChallengePenaltyData("penalty_000008").Param[0] ? (int)DataManager.Instance.GetChallengePenaltyData("penalty_000008").Param[0] : listUnitSlot.Count;
                        for (int i = 0; i < randomCnt; i++)
                            listUnitSlot[i].SetChallengePanalty((float)DataManager.Instance.GetChallengePenaltyData("penalty_000008").Param[1] - 0.05f);
                    }
                    break;
            }
        }
    }

    public override void DoBossAppearedTask()
    {
        // 보스 상태이면 광폭화 체크
        if (!battleSys.IsBossAngry && battleSys.CheckIsBossAlive())
        {
            bossAngryTimer -= Time.deltaTime;

            battleSys.SetBossAngryRemainTime(bossAngryTimer);

            if (bossAngryTimer < 0.0f)
                battleSys.SetBossAngry();
        }
    }

    public override void ModeSpawn()
    {
        // 테마 4 도전 3 효과
        if(battleSys.IsChallengeMode && battleSys.ChallengeLevel == 2 && battleSys.GetCurrentThema() == 4 && !battleSys.IsBossAppeared)
        {
            battleSys.ListChallengeUnit.Clear();
            battleSys.ListChallengeUnit.AddRange(battleSys.GetEnemyUnitList(battleSys.GetAllyBase(), _isAlly: true));
            foreach(UnitBase unit in battleSys.ListChallengeUnit)
                unit.AddUnitEffect(UNIT_EFFECT.CC_FEAR, unit, unit, new float[] { 3.0f });
        }

        int bossCnt = 0;
        foreach (var spawnOrder in battleSys.EnemySpawnData.Orders)
        {
            if (spawnOrder.EnemyId.Contains("Mid_Boss") || spawnOrder.EnemyId.Contains("Final_Boss"))
                bossCnt++;
        }

        foreach (var spawnOrder in battleSys.EnemySpawnData.Orders)
        {
            if (spawnOrder.EnemyId.Contains("Mid_Boss") || spawnOrder.EnemyId.Contains("Final_Boss"))
            {
                if (!battleSys.IsBossAppeared)
                {
                    battleSys.ReserveSpawnCnt += spawnOrder.EnemyCount;
                    battleSys.TaskSpawnEnemy(spawnOrder.EnemyId, spawnOrder.EnemyCount, (float)spawnOrder.DistanceTime, true, bossCnt).Forget();
                }
            }
            else
            {
                if (battleSys.IsBossAppeared)
                {
                    int spawnCnt = MathLib.CheckPercentage(0.5f) ? Mathf.CeilToInt(spawnOrder.EnemyCount * 0.5f) : Mathf.FloorToInt(spawnOrder.EnemyCount * 0.5f);
                    battleSys.ReserveSpawnCnt += spawnCnt;
                    battleSys.TaskSpawnEnemy(spawnOrder.EnemyId, spawnCnt, (float)spawnOrder.DistanceTime).Forget();
                }
                else
                {
                    battleSys.ReserveSpawnCnt += spawnOrder.EnemyCount;
                    battleSys.TaskSpawnEnemy(spawnOrder.EnemyId, spawnOrder.EnemyCount, (float)spawnOrder.DistanceTime).Forget();
                }
            }
        }

        if (battleSys.TutorialStep == 7)
            TaskTutorial_7().Forget();

        battleSys.IsBossAppeared = battleSys.EnemySpawnData.IsBossWave;
    }

    private async UniTaskVoid TaskChallengePenaltyWood()
    {
        MgrSound.Instance.PlayOneShotSFX("SFX_Challenge_C1_Penalty_a", 1.0f);

        GameObject objVFX_BG = MgrObjectPool.Instance.ShowObj("FX_wind_tornado_backgrd", new Vector3(battleSys.GetAllyBase().transform.position.x + 30.0f, -2.0f, 0.0f));
        GameObject objVFX_Wind = MgrObjectPool.Instance.ShowObj("FX_wind_tornado", new Vector3(battleSys.GetAllyBase().transform.position.x + 30.0f, 0.0f, 0.0f));

        Vector3 v3StartPos = objVFX_Wind.transform.position;
        objVFX_Wind.transform.DOMoveX(objVFX_Wind.transform.position.x - 60.0f, 2.0f).SetEase(Ease.Linear);

        battleSys.ListChallengeHitUnit.Clear();

        float duration = 2.0f;
        while(duration > 0.0f)
        {
            await UniTask.Yield(battleSys.GetCancellationTokenOnDestroy());

            duration -= Time.deltaTime;

            battleSys.ListChallengeUnit.Clear();
            battleSys.ListChallengeUnit.AddRange(battleSys.GetNearestEnemyUnitInLine(battleSys.GetAllyBase(), v3StartPos, (v3StartPos.x - objVFX_Wind.transform.position.x), 10.0f, _isAlly:true));
            battleSys.ListChallengeUnit.Remove(battleSys.GetAllyBase());

            foreach (UnitBase unit in battleSys.ListChallengeUnit)
            {
                if (battleSys.ListChallengeHitUnit.Contains(unit))
                    continue;

                MgrSound.Instance.PlayOneShotSFX("SFX_Challenge_C1_Penalty_b", 1.0f);
                MgrInGameEvent.Instance.BroadcastDamageEvent(unit, unit, unit.UnitStat.MaxHP * (float)DataManager.Instance.GetChallengePenaltyData("penalty_000006").Param[2], _dmgChannel: -1);
                unit.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)DataManager.Instance.GetChallengePenaltyData("penalty_000006").Param[1]), unit, unit, new float[] { (float)DataManager.Instance.GetChallengePenaltyData("penalty_000006").Param[0] });
                battleSys.ListChallengeHitUnit.Add(unit);
            }
        }

        MgrObjectPool.Instance.HideObj("FX_wind_tornado_backgrd", objVFX_BG);
        MgrObjectPool.Instance.HideObj("FX_wind_tornado", objVFX_Wind);
    }
    
    // 튜토리얼 관련 함수
    private async UniTaskVoid TaskTutorial()
    {
        await UniTask.Delay(2000, cancellationToken: battleSys.GetCancellationTokenOnDestroy());
        
        MgrBakingSystem.Instance.AddBakedFish(5);

        battleSys.SetTutorialTimeScale(true);
        
        battleSys.ShowTutorialTextUI(0, ANCHOR_TYPE.CENTER, new Vector2(0.0f, 0.5f), new Vector2(-400.0f, 100.0f));
        battleSys.ShowTutorialMaskBackGround(new Vector2(-900.0f, 155.0f), new Vector2(750.0f, 750.0f), ANCHOR_TYPE.CENTER, 1);
        battleSys.ToggleTutorialUI(true);
        
        await UniTask.WaitUntil(() => !battleSys.ObjCanvTutorial.activeSelf, cancellationToken:battleSys.GetCancellationTokenOnDestroy());
        
        battleSys.ShowTutorialTextUI(1, ANCHOR_TYPE.TOP_RIGHT, new Vector2(0.5f, 1.0f), new Vector2(-400.0f, -300.0f));
        battleSys.ShowTutorialMaskBackGround(new Vector2(-400.0f, -100.0f), new Vector2(400.0f, 200.0f), ANCHOR_TYPE.TOP_RIGHT, 1);
        battleSys.ToggleTutorialUI(true);
        
        await UniTask.WaitUntil(() => !battleSys.ObjCanvTutorial.activeSelf, cancellationToken:battleSys.GetCancellationTokenOnDestroy());
        
        battleSys.ShowTutorialTextUI(2, ANCHOR_TYPE.TOP_LEFT, new Vector2(0.5f, 1.0f), new Vector2(400.0f, -300.0f));
        battleSys.ShowTutorialMaskBackGround(new Vector2(350.0f, -115.0f), new Vector2(600.0f, 200.0f), ANCHOR_TYPE.TOP_LEFT, 1);
        battleSys.ToggleTutorialUI(true);
        
        await UniTask.WaitUntil(() => !battleSys.ObjCanvTutorial.activeSelf, cancellationToken:battleSys.GetCancellationTokenOnDestroy());
        
        battleSys.ShowTutorialTextUI(3, ANCHOR_TYPE.BOTTOM_LEFT, new Vector2(0.5f, 0.0f), new Vector2(1125.0f, 400.0f));
        battleSys.ShowTutorialMaskBackGround(new Vector2(1130.0f, 175.0f), new Vector2(250.0f, 250.0f), ANCHOR_TYPE.BOTTOM_LEFT, 1);
        battleSys.ToggleTutorialUI(true);

        await UniTask.WaitUntil(() => !battleSys.ObjCanvTutorial.activeSelf, cancellationToken:battleSys.GetCancellationTokenOnDestroy());
        
        battleSys.ShowTutorialTextUI(4, ANCHOR_TYPE.BOTTOM_LEFT, new Vector2(0.5f, 0.0f), new Vector2(725.0f, 400.0f));
        battleSys.ShowTutorialMaskBackGround(new Vector2(720.0f, 175.0f), new Vector2(600.0f, 250.0f), ANCHOR_TYPE.BOTTOM_LEFT, 1);
        battleSys.ToggleTutorialUI(true);

        await UniTask.WaitUntil(() => !battleSys.ObjCanvTutorial.activeSelf, cancellationToken:battleSys.GetCancellationTokenOnDestroy());
        
        battleSys.ShowTutorialTextUI(5, ANCHOR_TYPE.BOTTOM_LEFT, new Vector2(0.0f, 0.5f), new Vector2(500.0f, 150.0f));
        battleSys.ShowTutorialMaskBackGround(new Vector2(150.0f, 100.0f), new Vector2(450.0f, 450.0f), ANCHOR_TYPE.BOTTOM_LEFT, 1);
        battleSys.ToggleTutorialUI(true);

        await UniTask.WaitUntil(() => !battleSys.ObjCanvTutorial.activeSelf, cancellationToken:battleSys.GetCancellationTokenOnDestroy());

        battleSys.TutorialStep = 5;
        battleSys.ShowTutorialFingerUI(new Vector2(150.0f, 100.0f), ANCHOR_TYPE.BOTTOM_LEFT, 4);
        battleSys.ShowTutorialMaskBackGround(new Vector2(150.0f, 100.0f), new Vector2(450.0f, 450.0f), ANCHOR_TYPE.BOTTOM_LEFT, 1, false);
        battleSys.ToggleTutorialUI(true);
    }
    
    private async UniTaskVoid TaskTutorial_7()
    {
        await UniTask.Delay(4500, cancellationToken: battleSys.GetCancellationTokenOnDestroy());

        battleSys.SetTutorialTimeScale(true);
        battleSys.ShowTutorialTextUI(7, ANCHOR_TYPE.CENTER, new Vector2(1.0f, 0.5f), new Vector2(-370.0f, -135.0f));
        battleSys.ShowTutorialMaskBackGround(new Vector2(0.0f, -150.0f), new Vector2(700.0f, 700.0f), ANCHOR_TYPE.CENTER, 1);
        battleSys.ToggleTutorialUI(true);

        await UniTask.WaitUntil(() => !battleSys.ObjCanvTutorial.activeSelf, cancellationToken: battleSys.GetCancellationTokenOnDestroy());

        battleSys.ShowTutorialTextUI(8, ANCHOR_TYPE.BOTTOM_RIGHT, new Vector2(0.5f, 0.0f), new Vector2(-1510.0f, 420.0f));
        battleSys.ShowTutorialMaskBackGround(new Vector2(-1510.0f, 235.0f), new Vector2(275.0f, 250.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 0);
        battleSys.ToggleTutorialUI(true);

        await UniTask.WaitUntil(() => !battleSys.ObjCanvTutorial.activeSelf, cancellationToken: battleSys.GetCancellationTokenOnDestroy());

        battleSys.ShowTutorialFingerUI(new Vector2(-1510.0f, 250.0f), ANCHOR_TYPE.BOTTOM_RIGHT, 4);
        battleSys.ToggleTutorialUI(true);
        battleSys.TutorialStep = 8;
        battleSys.TutorialSubStep = 0;
    }
}
