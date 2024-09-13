using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_S_Tank_01_Prince_Spawn_S1", menuName = "UnitSkillEvent/S_Tank_01_Prince_Spawn_S1")]
public class S_Tank_01_Prince_Spawn_S1 : SOBase_UnitSkillEvent
{
    public SkeletonDataAsset[] skdaInfo;

    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        if (_unitBase.TeamNum == 1)
        {
            _unitBase.transform.rotation = Quaternion.Euler(0.0f, -180.0f, 0.0f);
            _unitBase.SetRotationHpBar();
        }

        _unitBase.UnitStat.MaxHP = _unitBase.UnitBaseParent.UnitStat.MaxHP * _unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(1, "param.2");
        _unitBase.UnitStat.HP = _unitBase.UnitStat.MaxHP;
        _unitBase.UnitStat.MoveSpeed = 0.0f;
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private bool isSpawn = false;

        public override bool CheckCanUseSkill()
        {
            return !isSpawn;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill"))
                return;

            unitBase.SetUnitState(UNIT_STATE.IDLE);
        }

        public override void EventTriggerSkill()
        {
            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(1, "param.0"), _isContainBlockedTarget: true));

            GameObject objVFX = MgrObjectPool.Instance.ShowObj("FX_S_Tank_01_Prince_skill2_zone", unitBase.transform.position);
            TaskSkill(objVFX).Forget();

            MgrSound.Instance.PlayOneShotSFX("SFX_S_Tank_01_s2_b_3", 1.0f);

            foreach (UnitBase unit in listUnit)
            {
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase.UnitBaseParent, unit, unitBase.UnitBaseParent.GetAtkRateToDamage(unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(1, "param.3")), unitBase.UnitBaseParent.GetCriRate(), unitBase.UnitBaseParent.UnitStat.CriDmg, 1);
                unit.AddUnitEffect(UNIT_EFFECT.CC_TAUNT, unitBase, unit, new float[] { unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(1, "param.4") });
                MgrObjectPool.Instance.ShowObj("FX_S_Tank_01_Prince_skill2_hit", unit.GetUnitCenterPos());
            }
        }

        public override void OnSkill()
        {
            isSpawn = true;

            MgrSound.Instance.PlayOneShotSFX("SFX_S_Tank_01_s2_b_1", 1.0f);

            S_Tank_01_Prince_Spawn_S1 skill = soSkillEvent as S_Tank_01_Prince_Spawn_S1;
            unitBase.Ska.skeletonDataAsset = skill.skdaInfo[Random.Range(0, 2)];
            unitBase.Ska.Initialize(true);

            unitBase.InitSpineEvent();

            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }

        private async UniTaskVoid TaskSkill(GameObject _vfx)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(unitBase.UnitBaseParent.GetUnitSkillFloatDataValue(1, "param.1")), cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            unitBase.SetUnitState(UNIT_STATE.DEATH);
            MgrObjectPool.Instance.HideObj("FX_S_Tank_01_Prince_skill2_zone", _vfx);
        }
    }
}
