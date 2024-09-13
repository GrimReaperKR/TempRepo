using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using BCH.Database;
using System.Linq;

public class MgrInGameData : Singleton<MgrInGameData>
{
    private List<UNIT_EFFECT> listUnitEffect;
    private Dictionary<int, UNIT_EFFECT> dicUnitEffectByIndexNum = new Dictionary<int, UNIT_EFFECT>();

    public Dictionary<string, Dictionary<string, string>> DicLocalizationCSVData { get; private set; } = new Dictionary<string, Dictionary<string, string>>();

    [field: SerializeField] public UnitData SOUnitData { get; private set; }
    [field: SerializeField] public UnitData SOMonsterData { get; private set; }
    [field: SerializeField] public BulletData SOBulletData { get; private set; }
    [field: SerializeField] public GameObject objUnitPrefab { get; private set; }
    [field: SerializeField] public GameObject objBulletPrefab { get; private set; }

    [field: SerializeField] public BoosterData SOBoosterData { get; private set; }
    [field: SerializeField] public WeaponData SOWeaponData { get; private set; }

    [field: SerializeField] public UnitEffectData SOUnitEffectData { get; private set; }

    [field: SerializeField] public SOBase_ClassImage SoClassImage { get; private set; }

    [field: SerializeField] public BackGroundData SoBackGroundData { get; private set; }

    public MaterialPropertyBlock MpbSlow { get; private set; } // MPB 슬로우
    public MaterialPropertyBlock MpbFrostBite { get; private set; } // MPB 동상
    public MaterialPropertyBlock MpbGod { get; private set; } // MPB 불사,무적

    public bool isCSVLoaded { get; private set; }

    private void Awake()
    {
        Application.targetFrameRate = 60;

        listUnitEffect = System.Enum.GetValues(typeof(UNIT_EFFECT)).Cast<UNIT_EFFECT>().ToList();
        for (int i = 0; i < listUnitEffect.Count; i++)
            dicUnitEffectByIndexNum.Add((int)listUnitEffect[i], listUnitEffect[i]);

        Color color, black;

        ColorUtility.TryParseHtmlString("#898CC8", out color);
        ColorUtility.TryParseHtmlString("#16187B", out black);
        MpbSlow = new MaterialPropertyBlock();
        MpbSlow.SetColor("_Color", color);
        MpbSlow.SetColor("_Black", black);

        ColorUtility.TryParseHtmlString("#9CFEFF", out color);
        ColorUtility.TryParseHtmlString("#405FB0", out black);
        MpbFrostBite = new MaterialPropertyBlock();
        MpbFrostBite.SetColor("_Color", color);
        MpbFrostBite.SetColor("_Black", black);
        
        ColorUtility.TryParseHtmlString("#FFF8E7", out color);
        ColorUtility.TryParseHtmlString("#745313", out black);
        MpbGod = new MaterialPropertyBlock();
        MpbGod.SetColor("_Color", color);
        MpbGod.SetColor("_Black", black);

        isCSVLoaded = false;
        TaskInitData().Forget();
    }

    private async UniTaskVoid TaskInitData()
    {
        if(!DataManager.Instance.IsDataLoaded)
        {
            var serverController = FindObjectOfType<ServerController>();
            // 로그인
            await serverController.AuthenticateAsync();

            // 테이블 컨트롤러 생성 및 데이터 초기화
            var tableController = new DBTableController(serverController, $"/table/{serverController.CurrentEnvironment}/");
            var tmp = await tableController.SetTableDataAsync();

            // 유저 데이터 컨트롤러 생성 및 초기화
            var dbUserDataController = new DBUserDataController(serverController);

            DataManager.Instance.InitController(tableController, dbUserDataController);

            await DataManager.Instance.InitailizeUserDataAsync();
            DataManager.Instance.IsDataLoaded = true;
        }

        await MgrSound.Instance.LoadSound(MgrSound.LOAD_SOUND.INGAME);

        DicLocalizationCSVData = await CSVReader.ReadToDicAsync("CSV/InGameLocalizationTable.csv");
        isCSVLoaded = true;

        Debug.Log($"데이터( DB / CSV / 사운드 ) 로드 완료");
    }

    public UnitInfo GetUnitDBData(string _unitIndex)
    {
        return DataManager.Instance.GetUnitInfoData(_unitIndex);
    }

    public EnemyInfo GetEnemyDBData(string _unitIndex)
    {
        return DataManager.Instance.GetEnemyInfoData(_unitIndex);
    }

    public CatbotInfo GetCatbotDBData(string _unitIndex)
    {
        return DataManager.Instance.GetCatbotInfo(_unitIndex);
    }

    public UnitSkill GetUnitSkillDBData(string _unitIndex, int _skillIndex, int _skillLv)
    {
        return DataManager.Instance.GetUnitSkillData($"{_unitIndex}_{_unitIndex}_s{_skillIndex + 1}_{_skillLv}");
    }

    public BossSkill GetBossSkillDBData(string _unitIndex, int _skillIndex)
    {
        string[] bossString = _unitIndex.Split('_');
        string uniqueID = $"{_unitIndex}_{bossString[0]}_{bossString[1]}_{bossString[2]}_{(bossString[3].Equals("a") || bossString[3].Equals("c") ? "ac" : "bd")}_s{_skillIndex + 1}";
        return DataManager.Instance.GetBossSkillData(uniqueID);
    }

    public UnitData.UnitSetting GetUnitData(string _indexName)
    {
        foreach(UnitData.UnitSetting setting in SOUnitData.unitSetting)
        {
            if (setting.unitIndex.Equals(_indexName))
                return setting;
        }
        return null;
    }

    //private List<UnitData.UnitSetting> listUnitSetting = new List<UnitData.UnitSetting>();
    //public UnitData.UnitSetting GetRandomUnitData(string _rank, bool _isIncludSupporter = false)
    //{
    //    listUnitSetting.Clear();
    //    foreach(UnitData.UnitSetting setting in SOUnitData.unitSetting)
    //    {
    //        if (setting.unitIndex.Contains($"{_rank}_") && ((!_isIncludSupporter && setting.unitClass != UnitClass.Supporter) || (_isIncludSupporter)))
    //            listUnitSetting.Add(setting);
    //    }
    //    listUnitSetting.Shuffle();
    //    return listUnitSetting.Count > 0 ? listUnitSetting[0] : null;
    //}

    public UnitData.UnitSetting GetMonsterData(string _indexName)
    {
        foreach(UnitData.UnitSetting setting in SOMonsterData.unitSetting)
        {
            if (setting.unitIndex.Equals(_indexName))
                return setting;
        }
        return null;
    }

    public BulletData.BulletSetting GetBulletData(string _indexName)
    {
        foreach (BulletData.BulletSetting setting in SOBulletData.bulletSetting)
        {
            if (setting.bulletIndex.Equals(_indexName))
                return setting;
        }
        return null;
    }

    public WeaponData.WeaponSetting GetWeaponData(string _indexName)
    {
        foreach (WeaponData.WeaponSetting setting in SOWeaponData.data)
        {
            if (setting.WeaponIndex.Equals(_indexName))
                return setting;
        }
        return null;
    }

    public BoosterData.BoosterInfo GetBoosterData(string _indexName)
    {
        foreach (BoosterData.BoosterInfo setting in SOBoosterData.boosterInfo)
        {
            if (setting.Index.Equals(_indexName))
                return setting;
        }
        return null;
    }

    public UnitEffectData.UnitEffectSetting GetUnitEffectData(SOBase_UnitEffectEvent _effectEvent)
    {
        foreach (UnitEffectData.UnitEffectSetting cc in SOUnitEffectData.unitEffectSetting)
        {
            if (cc.soUnitEffectEvent == _effectEvent)
                return cc;
        }
        return null;
    }

    public UnitEffectData.UnitEffectSetting GetEffectData(UNIT_EFFECT _index)
    {
        foreach (UnitEffectData.UnitEffectSetting cc in SOUnitEffectData.unitEffectSetting)
        {
            if (cc.Index == _index)
                return cc;
        }
        return null;
    }

    public UNIT_EFFECT GetUnitEffectByIndexNum(int _indexNum)
    {
        return dicUnitEffectByIndexNum.TryGetValue(_indexNum, out UNIT_EFFECT _result) ? _result : UNIT_EFFECT.NONE;
    }
}
