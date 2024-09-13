using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BCH.Database;
using Cysharp.Threading.Tasks;

public enum GAME_MODE
{
    Chapter,
    GoldMode,
    Pvp,
    Training,
    Survival,
    Farm,
}

public enum UNIT_EFFECT
{
    NONE,

    BUFF_SHIELD = 9100, BUFF_CRI_RATE, BUFF_ATK, BUFF_TAKE_DMG, BUFF_GIVE_DMG, BUFF_IMMORTALITY, BUFF_ATK_SPEED, BUFF_MOVE_SPEED, BUFF_TAKE_ELEMENT_DMG, BUFF_GHOST_ARMOR,

    DEBUFF_WEAK = 9200, DEBUFF_SLOW, DEBUFF_FROSTBITE, DEBUFF_BLACK_FIRE,

    CC_STUN = 9300, CC_KNOCKBACK, CC_RESTRICTION, CC_TAUNT, CC_FEAR, CC_SNOWMAN, CC_WATERBALL, CC_BLEEDING, CC_IGNITE, CC_FREEZE, CC_SHOCK, CC_SUPER_KONCKBACK,

    ETC_GOD = 9400, ETC_REVIVE, ETC_INSTANT_DEATH, ETC_DODGE
}

public enum ANCHOR_TYPE
{
    CENTER = 0,
    TOP,
    BOTTOM,
    LEFT,
    RIGHT,
    TOP_LEFT,
    TOP_RIGHT,
    BOTTOM_LEFT,
    BOTTOM_RIGHT
}

public class InGameGlobalClass
{
    #region 특성
    #region 일반 특성 스탯 변수
    public float TraitUnitAtk { get; private set; }
    public float TraitUnitHP { get; private set; }
    public float TraitAllyBaseAtk { get; private set; }
    public float TraitAllyBaseHP { get; private set; }
    #endregion

    #region 고급 특성 옵션 변수
    public int Option_BoosterReRollCnt { get; private set; }
    public int Option_AddStartBoosterLv { get; private set; }

    public int Option_MaxCost { get; private set; }
    public int Option_StartCost { get; private set; }
    public float Option_BossHPRegen { get; private set; }
    public float Option_UnitCriRate { get; private set; }
    public float Option_AllyBaseSkillCoolDown { get; private set; }
    public float Option_UnitSpawnCoolDown { get; private set; }
    public float Option_AllyBaseWeaponCoolDown { get; private set; }
    public float Option_AllyBaseCriRate { get; private set; }
    public float Option_AllyBaseCriDmg { get; private set; }
    public float Option_ChapterClearExp { get; private set; }
    public float Option_UnitHeal { get; private set; }
    public float Option_AutoAbility { get; private set; }
    public float Option_UnitCriDmg { get; private set; }
    public float Option_UnitMoveSpeed { get; private set; }
    public float Option_EnemyMoveSpeed { get; private set; }
    #endregion

    /// <summary>
    /// 특성 레벨에 따른 변수값 세팅
    /// </summary>
    /// <param name="_traitLevel">현재 특성 레벨</param>
    public void Init_GlobalVariable(int _traitLevel)
    {
        Trait traitData;
        for (int i = 0; i < _traitLevel; i++)
        {
            traitData = DataManager.Instance.GetTraitData(i);
            TraitUnitAtk += (float)traitData.UnitAtk * 2.0f;
            TraitUnitHP += (float)traitData.UnitHp * 2.0f;
            TraitAllyBaseAtk += (float)traitData.NexusAtk * 2.0f;
            TraitAllyBaseHP += (float)traitData.NexusHp * 2.0f;

            switch(traitData.OptionId)
            {
                case "option_2012": Option_BoosterReRollCnt += (int)traitData.OptionValue; break; // 부스터 n회 재선택
                case "option_2013": Option_AddStartBoosterLv += (int)traitData.OptionValue; break; // 전투 시작 시 부스터 n개 획득
                case "option_2002": Option_MaxCost += (int)traitData.OptionValue; break; // 최대 코스트 +n
                case "option_2003": Option_StartCost += (int)traitData.OptionValue; break; // 초기 코스트 +n
                case "option_2014": Option_BossHPRegen += (float)traitData.OptionValue; break; // 보스 자연 회복력 +n%
                case "option_0046": Option_UnitCriRate += (float)traitData.OptionValue; break; // 유닛 치확 +n%
                case "option_1007": Option_AllyBaseSkillCoolDown += (float)traitData.OptionValue; break; // 기지 스킬 쿨타임 +n%
                case "option_0047": Option_UnitSpawnCoolDown += (float)traitData.OptionValue; break; // 소환 쿨타임 +n%
                case "option_1018": Option_AllyBaseWeaponCoolDown += (float)traitData.OptionValue; break; // 기지 무기 쿨타임 +n%
                case "option_1019": Option_AllyBaseCriRate += (float)traitData.OptionValue; break; // 기지 치확 +n%
                case "option_1020": Option_AllyBaseCriDmg += (float)traitData.OptionValue; break; // 기지 치피 +n%
                case "option_2015": Option_ChapterClearExp += (float)traitData.OptionValue; break; // 챕터 클리어 경험치 +n%
                case "option_0007": Option_UnitHeal += (float)traitData.OptionValue; break; // 유닛 받는 회복량 +n%
                case "option_2000": Option_AutoAbility += (float)traitData.OptionValue; break; // 자동 효율
                case "option_0048": Option_UnitCriDmg += (float)traitData.OptionValue; break; // 유닛 치피 +n%
                case "option_0016": Option_UnitMoveSpeed += (float)traitData.OptionValue; break; // 유닛 이속 +n%
                case "option_2001": Option_EnemyMoveSpeed += (float)traitData.OptionValue; break; // 적 이속 +n%
            }
        }

        if (!(DataManager.Instance.UserInventory.traitSubOptions is null))
        {
            traitData = DataManager.Instance.GetTraitData(_traitLevel);
            if (DataManager.Instance.UserInventory.traitSubOptions.Contains("unit_atk_0")) TraitUnitAtk += (float)traitData.UnitAtk;
            if (DataManager.Instance.UserInventory.traitSubOptions.Contains("unit_atk_1")) TraitUnitAtk += (float)traitData.UnitAtk;
            if (DataManager.Instance.UserInventory.traitSubOptions.Contains("unit_hp_0")) TraitUnitHP += (float)traitData.UnitHp;
            if (DataManager.Instance.UserInventory.traitSubOptions.Contains("unit_hp_1")) TraitUnitHP += (float)traitData.UnitHp;
            if (DataManager.Instance.UserInventory.traitSubOptions.Contains("nexus_atk_0")) TraitAllyBaseAtk += (float)traitData.NexusAtk;
            if (DataManager.Instance.UserInventory.traitSubOptions.Contains("nexus_atk_1")) TraitAllyBaseAtk += (float)traitData.NexusAtk;
            if (DataManager.Instance.UserInventory.traitSubOptions.Contains("nexus_hp_0")) TraitAllyBaseHP += (float)traitData.NexusHp;
            if (DataManager.Instance.UserInventory.traitSubOptions.Contains("nexus_hp_1")) TraitAllyBaseHP += (float)traitData.NexusHp;
        }
    }
    #endregion

    public int GearCore_0001_AllyUnitDeathCnt;

    private int gearStove_0003_EnemyKillCnt;
    private int gearStove_0003_AllyUnitDeathCnt;

    private float warriorDeathStackFishCnt;
    private List<GameObject> listObjFish = new List<GameObject>();

    public bool isGearCoreActive_000 { get; set; }
    public bool isGearCoreActive_002 { get; set; }

    public bool isAllyActiveSkill_2 { get; set; }
    public bool isAllyActiveSkill_5 { get; set; }

    public int GearTail_0003_SpawnCnt { get; private set; }

    public float GetWorldAtkBonus(UnitBase _unitbase)
    {
        float resultAtkRate = 0.0f;

        // 생명의 코어 부활 이후 공증
        UserGear gearCore = DataManager.Instance.GetUsingGearInfo(1);
        if (isGearCoreActive_000 && !(gearCore is null) && gearCore.gearRarity >= 3 && _unitbase.UnitSetting.unitType == UnitType.AllyBase)
            resultAtkRate += (float)DataManager.Instance.GetGearOptionValue("gear_core_0000", 2);

        // 유닛 공증 엑티브 부스터 스킬
        if (isAllyActiveSkill_5 && _unitbase.UnitSetting.unitType == UnitType.Unit)
        {
            switch(MgrBoosterSystem.Instance.DicSkill["skill_active_004"])
            {
                case 1:
                    resultAtkRate += 0.2f;
                    break;
                case 2:
                case 3:
                case 4:
                    resultAtkRate += 0.3f;
                    break;
                case 5:
                    resultAtkRate += 0.5f;
                    break;
            }
        }

        if (_unitbase.UnitSetting.unitType == UnitType.Unit)
        {
            UserGear gearNeck = DataManager.Instance.GetUsingGearInfo(4);
            if(gearNeck is not null)
            {
                if (_unitbase.UnitStat.HP >= _unitbase.UnitStat.MaxHP && gearNeck.gearId.Equals("gear_necklace_0002") && gearNeck.gearRarity >= 1)
                    resultAtkRate += (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 0);

                if (_unitbase.UnitStat.HP / _unitbase.UnitStat.MaxHP <= 0.3f && gearNeck.gearId.Equals("gear_necklace_0003") && gearNeck.gearRarity >= 1)
                    resultAtkRate += (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 0);

                if (_unitbase.UnitSetting.unitClass == UnitClass.Warrior && gearNeck.gearId.Equals("gear_necklace_0004") && gearNeck.gearRarity >= 1)
                {
                    int unitCnt = MgrBattleSystem.Instance.GetEnemyUnitListInClass(_unitbase, UnitClass.Warrior, true).Count;
                    unitCnt = unitCnt > 4 ? 4 : unitCnt;
                    resultAtkRate += (float)DataManager.Instance.GetGearOptionValue(gearNeck.gearId, 0) * unitCnt;
                }
            }
        }

        // 도전 모드
        if (MgrBattleSystem.Instance.IsChallengeMode && _unitbase.UnitSetting.unitType != UnitType.Unit && _unitbase.UnitSetting.unitType != UnitType.AllyBase)
        {
            switch(MgrBattleSystem.Instance.ChallengeLevel)
            {
                case 0:
                    resultAtkRate += (float)DataManager.Instance.GetChallengePenaltyData("penalty_000000").Param[0];
                    break;
                case 1:
                    resultAtkRate += (float)DataManager.Instance.GetChallengePenaltyData("penalty_000001").Param[0];
                    break;
                case 2:
                    resultAtkRate += (float)DataManager.Instance.GetChallengePenaltyData("penalty_000004").Param[0];
                    break;
            }
        }

        return resultAtkRate;
    }

    private Vector3[] arrV3bezier = new Vector3[6];
    public void AddWarriorFishCount(UnitBase _unitbase, float _value)
    {
        warriorDeathStackFishCnt += _value;

        if(warriorDeathStackFishCnt >= 1.0f)
        {
            int fishCnt = Mathf.FloorToInt(warriorDeathStackFishCnt);
            warriorDeathStackFishCnt -= fishCnt;
            
            for(int i = 0; i < fishCnt; i++)
                TaskDropBakedFish(_unitbase.transform.position + Vector3.up * _unitbase.GetUnitHeight(), _unitbase.transform.position).Forget();
        }
    }

    public void Add_GearStove_0003_EnemyKill(UnitBase _victim)
    {
        gearStove_0003_EnemyKillCnt++;

        if(gearStove_0003_EnemyKillCnt >= (int)DataManager.Instance.GetGearOptionValue("gear_stove_0003", 2))
        {
            gearStove_0003_EnemyKillCnt -= (int)DataManager.Instance.GetGearOptionValue("gear_stove_0003", 2);

            for (int i = 0; i < 2; i++)
                TaskDropBakedFish(_victim.transform.position + Vector3.up * _victim.GetUnitHeight(), _victim.transform.position).Forget();
        }
    }

    public void Add_GearCore_0001_AllyDeath()
    {
        if(GearCore_0001_AllyUnitDeathCnt < 50)
        {
            GearCore_0001_AllyUnitDeathCnt++;

            UserGear gearCore = DataManager.Instance.GetUsingGearInfo(1);
            MgrBattleSystem.Instance.ReduceSideSkillCoolDown((float)DataManager.Instance.GetGearOptionValue(gearCore.gearId, 0));
        }
    }
    
    public void Add_GearStove_0003_AllyDeath(UnitBase _victim)
    {
        gearStove_0003_AllyUnitDeathCnt++;

        if (gearStove_0003_AllyUnitDeathCnt >= (int)DataManager.Instance.GetGearOptionValue("gear_stove_0003", 4))
        {
            gearStove_0003_AllyUnitDeathCnt -= (int)DataManager.Instance.GetGearOptionValue("gear_stove_0003", 4);

            for(int i = 0; i < 3; i++)
                TaskDropBakedFish(_victim.transform.position + Vector3.up * _victim.GetUnitHeight(), _victim.transform.position).Forget();
        }
    }

    private List<UnitBase> listUnitTemp = new List<UnitBase>();
    public void Add_GearTail_0003()
    {
        if (GearTail_0003_SpawnCnt >= 45)
            return;

        GearTail_0003_SpawnCnt++;

        listUnitTemp.Clear();
        listUnitTemp.AddRange(MgrBattleSystem.Instance.GetEnemyUnitList(MgrBattleSystem.Instance.GetAllyBase()));

        if(listUnitTemp.Count > 0)
            MgrSound.Instance.PlayOneShotSFX("SFX_Gear_Tail_0003", 1.0f);

        foreach (UnitBase unit in listUnitTemp)
        {
            if (unit.UnitSetting.unitType != UnitType.Monster && unit.UnitSetting.unitType != UnitType.EnemyUnit)
                continue;

            GameObject objVFX = MgrObjectPool.Instance.ShowObj("FX_Debuff_Weaken", unit.transform.position);
            objVFX.transform.SetParent(unit.transform);
            TaskVFXReturn(objVFX).Forget();
        }
    }

    private async UniTaskVoid TaskVFXReturn(GameObject _objVFX)
    {
        await UniTask.Delay(1000, cancellationToken: _objVFX.GetCancellationTokenOnDestroy());
        MgrObjectPool.Instance.HideObj("FX_Debuff_Weaken", _objVFX);
    }

    public async UniTaskVoid TaskDropBakedFish(Vector3 _v3StartPos, Vector3 _v3EndPos)
    {
        Vector3 v3End = _v3EndPos + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-0.5f, 0.5f), -0.001f);

        GameObject objFish = MgrObjectPool.Instance.ShowObj("bread_fieldDrop", _v3StartPos);
        ChangeLayerAllChild(objFish.transform, LayerMask.NameToLayer("Default"));
        SpriteRenderer sprrdFish = objFish.GetComponent<SpriteRenderer>();
        sprrdFish.sortingLayerName = "Unit";
        sprrdFish.sortingOrder = 0;

        objFish.transform.localScale = Vector3.one * 0.75f;
        objFish.transform.rotation = Quaternion.identity;

        listObjFish.Add(objFish);
        
        // DoTween Path 경로 지정
        Vector3 v3Dir = (v3End - objFish.transform.position).normalized;
        float distance = (v3End - objFish.transform.position).magnitude;
        Vector3 v3Way0 = objFish.transform.position + Vector3.up * 1.5f;
        v3Way0.x = v3End.x;

        arrV3bezier[0] = v3Way0;
        arrV3bezier[1] = objFish.transform.position + (v3Dir.x < 0.0f ? Vector3.left : Vector3.right);
        arrV3bezier[2] = v3Way0 - v3Dir;
        arrV3bezier[3] = v3End;
        arrV3bezier[4] = v3Way0 + v3Dir;
        arrV3bezier[5] = v3End + Vector3.up;

        objFish.transform.DORotate(new Vector3(0.0f, 0.0f, 360.0f), 0.5f, RotateMode.FastBeyond360);
        await objFish.transform.DOPath(arrV3bezier, 0.5f, PathType.CubicBezier).SetEase(MgrBattleSystem.Instance.AnimDropFishCurve).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(cancellationToken:objFish.GetCancellationTokenOnDestroy());

        FieldDropBakedFish fieldDrop = objFish.GetComponent<FieldDropBakedFish>();

        await UniTask.Delay(500, cancellationToken:objFish.GetCancellationTokenOnDestroy());

        fieldDrop.ParsysStarTrail.Stop();

        ChangeLayerAllChild(objFish.transform, LayerMask.NameToLayer("UI"));
        objFish.transform.position = MgrCamera.Instance.CameraUI.transform.position + (objFish.transform.position - MgrCamera.Instance.CameraMain.transform.position);
        objFish.transform.rotation = Quaternion.identity;

        fieldDrop.TrailrdBakedFish.Clear();
        fieldDrop.ParsysStarTrail.Play();

        Vector3 v3FishEndPos = MgrBakingSystem.Instance.rtMovedPosition.transform.position;
        v3FishEndPos.z = 0.0f;
        v3Dir = (v3FishEndPos - objFish.transform.position).normalized;
        v3Dir.z = 0.0f;
        distance = (v3FishEndPos - objFish.transform.position).magnitude;
        v3Way0 = objFish.transform.position + (distance * 0.5f * v3Dir) + Quaternion.Euler(0, 0, 90.0f) * v3Dir * 2.0f;
        v3Way0.z = 0.0f;

        arrV3bezier[0] = v3Way0;
        arrV3bezier[1] = objFish.transform.position + Vector3.up * 2.0f;
        arrV3bezier[2] = v3Way0 - v3Dir;
        arrV3bezier[3] = v3FishEndPos;
        arrV3bezier[4] = v3Way0 + v3Dir;
        arrV3bezier[5] = v3FishEndPos + Vector3.up * 2.0f;

        sprrdFish.sortingLayerName = "UI";
        sprrdFish.sortingOrder = 2;

        Sequence seq = DOTween.Sequence();
        seq.Append(objFish.transform.DOPath(arrV3bezier, 0.5f, PathType.CubicBezier).SetEase(Ease.InCubic).OnComplete(() => {
            MgrBakingSystem.Instance.parsysDropStall.Play();
            MgrBakingSystem.Instance.AddBakedFish(1);

            MgrObjectPool.Instance.HideObj("bread_fieldDrop", listObjFish[0]);
            listObjFish.RemoveAt(0);
        }));
        seq.Join(objFish.transform.DORotate(new Vector3(0.0f, 0.0f, Random.Range(180.0f, 450.0f)), 0.5f, RotateMode.FastBeyond360));
        seq.Join(objFish.transform.DOScale(Vector3.one * 0.4f, 0.5f).SetEase(Ease.InCirc));
    }

    private void ChangeLayerAllChild(Transform _tfRoot, int _layer)
    {
        var child = _tfRoot.GetComponentsInChildren<Transform>(true);
        foreach (var tf in child)
            tf.gameObject.layer = _layer;
    }
}

public abstract class UnitSkillPersonalVariableInstance
{
    public UnitBase unitBase; // 시전 유닛
    public SOBase_UnitSkillEvent soSkillEvent; // 시전중인 스킬 SO

    public float skillRange;
    public float skillCooldown;

    public UnitSkill unitSkillData;
    public BossSkill bossSkillData;

    public void SetData(UnitBase _unitBase, SOBase_UnitSkillEvent _event)
    {
        unitBase = _unitBase;
        soSkillEvent = _event;

        switch(unitBase.UnitSetting.unitType)
        {
            case UnitType.Unit:
                unitSkillData = MgrInGameData.Instance.GetUnitSkillDBData(_unitBase.UnitIndex, _unitBase.GetSkillIndex(soSkillEvent), GetSkillLV());
                break;
            case UnitType.EnemyUnit:
            case UnitType.Elite:
                unitSkillData = MgrInGameData.Instance.GetUnitSkillDBData(_unitBase.UnitIndex, _unitBase.GetSkillIndex(soSkillEvent), 0); // 유닛은 차후 제일 마지막 파라메터 스킬 레벨로 따로 빼야함
                break;
            case UnitType.MidBoss:
            case UnitType.Boss:
                bossSkillData = MgrInGameData.Instance.GetBossSkillDBData(_unitBase.UnitIndex, _unitBase.GetSkillIndex(soSkillEvent));
                break;
        }

        if (unitSkillData is not null)
        {
            skillRange = (float)unitSkillData.Range;
            skillCooldown = (float)unitSkillData.Cooldown;

            // 사정 거리 설정
            if (_unitBase.GetSkillIndex(soSkillEvent) == 0 && unitBase.UnitStat.Range == 0.0f)
                unitBase.UnitStat.Range = skillRange;
            // 초기 쿨타임 설정
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), (float)unitSkillData.StartCooldown);
        }
        if (bossSkillData is not null)
        {
            skillRange = (float)bossSkillData.Range;
            skillCooldown = (float)bossSkillData.Cooldown;

            // 사정 거리 설정
            if (_unitBase.GetSkillIndex(soSkillEvent) == 0 && unitBase.UnitStat.Range == 0.0f)
                unitBase.UnitStat.Range = skillRange;
            // 초기 쿨타임 설정
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), (float)bossSkillData.StartCooldown);
        }

        unitBase.SetGiveDamageEvent(OnGiveDamageAction);
    }

    private int GetSkillLV()
    {
        int skillLv = 0;
        if (unitBase.GetSkillIndex(soSkillEvent) == 0)
        {
            if (unitBase.UnitLvData.promotion >= 4) skillLv = 2;
            else if (unitBase.UnitLvData.promotion >= 2) skillLv = 1;
        }
        else if (unitBase.GetSkillIndex(soSkillEvent) == 1)
        {
            if (unitBase.UnitLvData.promotion >= 5) skillLv = 3;
            else if (unitBase.UnitLvData.promotion >= 3) skillLv = 2;
            else if (unitBase.UnitLvData.promotion >= 1) skillLv = 1;
        }
        return skillLv;
    }
    
    public abstract bool CheckCanUseSkill(); // 스킬 사용 가능한지 체크
    public abstract void OnSkill(); // 스킬 시전
    public abstract void EventTriggerSkill(); // 스킬 이벤트 트리거 발동
    public abstract void EventTriggerEnd(string _animationName); // 종료 이벤트 트리거 발동
    public virtual void ResetSkill() { } // 강제 취소 시 발동
    public virtual void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage) { } // 대미지 주었을 때 체크
}

public abstract class BulletPersonalVariableInstance
{
    public Bullet bulletComp;
    public WeaponSystem WeaponSys;

    public void SetData(Bullet _bullet)
    {
        bulletComp = _bullet;

        if (_bullet.owner.UnitSetting.unitType == UnitType.AllyBase)
            WeaponSys = _bullet.owner.TeamNum == 0 ? MgrBattleSystem.Instance.WeaponSys : MgrBattleSystem.Instance.WeaponSysOppo;
    }

    public abstract void OnMove();
    public abstract void OnHit();
}

public abstract class WeaponPersonalVariableInstance
{
    public WeaponSystem weaponComp;
    public bool IsAttack;

    public void ResetWeapon()
    {
        IsAttack = false;
        OnReset();
    }

    public void SetData(WeaponSystem _weapon) => weaponComp = _weapon;

    public abstract bool CheckCanUseSkill();
    public abstract void OnMove();
    public abstract void OnSkill();
    public abstract void EventTriggerSkill(); // 스킬 이벤트 트리거 발동
    public abstract void EventTriggerEnd(string _animationName); // 종료 이벤트 트리거 발동
    public virtual void OnReset() { }
}

public abstract class UnitEffectPersonalVariableInstance
{
    public UnitBase Caster;
    public UnitBase Victim;
    public UNIT_EFFECT Index;
    public bool CanRemove;

    public float Duration;

    public float[] EffectFloatValue;

    public SOBase_UnitEffectEvent effectEvent;

    public void SetData(SOBase_UnitEffectEvent _effectEvent, UnitBase _caster, UnitBase _victim, UNIT_EFFECT _index, float[] _value, bool _canRemove)
    {
        effectEvent = _effectEvent;
        Caster = _caster;
        Victim = _victim;
        Index = _index;
        EffectFloatValue = _value;
        CanRemove = _canRemove;
    }

    public abstract void OnStart();
    public abstract void OnUpdate();
    public abstract void OnEnd(bool _isForcedRemove = false);
    public virtual float GetUnitEffectValue() => 0.0f;
}

public interface IDamageEvent
{
    public void OnDamage(UnitBase _attacker, UnitBase _victim, float _damage, float _criPer, float _criDmg, int _dmgChannel);
}