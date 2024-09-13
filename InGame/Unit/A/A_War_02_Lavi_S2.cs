using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;
using Spine;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_A_War_02_Lavi_S2", menuName = "UnitSkillEvent/A_War_02_Lavi_S2")]
public class A_War_02_Lavi_S2 : SOBase_UnitSkillEvent
{
    [SerializeField] private SkeletonDataAsset skda;

    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private List<UnitBase> listHitUnit = new List<UnitBase>();

        private GameObject objSkda;
        private SkeletonAnimation ska;

        private int hitCnt = 0;

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_A_War_02_s2_3", 1.0f);

            _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitSkillData.Param[1]), _attacker, _victim, new float[] { (float)unitSkillData.Param[5] });
            TaskSkill(_attacker, _victim).Forget();
        }

        private async UniTaskVoid TaskSkill(UnitBase _attacker, UnitBase _victim)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(unitSkillData.Param[5] * 0.18), cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitSkillData.Param[3]), _attacker, _victim, new float[] { (float)unitSkillData.Param[2], (float)unitSkillData.Param[4] });
        }

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            if (unitBase.EnemyTarget && MgrBattleSystem.Instance.CheckIsEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, 2.0f)) return true;
            else
            {
                if (unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    return false;

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, 2.0f));
                UnitBase unitInEllipse = listUnit.Count > 0 ? listUnit[0] : null;
                if (unitInEllipse is not null)
                {
                    unitBase.EnemyTarget = unitInEllipse;
                    return true;
                }
            }

            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill"))
                return;

            unitBase.SetUnitState(UNIT_STATE.IDLE);
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);
        }

        public override void EventTriggerSkill()
        {
            if (objSkda == null)
            {
                A_War_02_Lavi_S2 skill = unitBase.soSkillEvent[1] as A_War_02_Lavi_S2;
                objSkda = new GameObject();
                objSkda.transform.position = unitBase.transform.position;
                objSkda.transform.localScale = unitBase.transform.localScale;
                objSkda.transform.SetParent(unitBase.transform);
                ska = objSkda.AddComponent<SkeletonAnimation>();
                ska.skeletonDataAsset = skill.skda;
                ska.Initialize(true);
                ska.tintBlack = true;
                ska.GetComponent<MeshRenderer>().sortingLayerName = "Unit";
            }
            else objSkda.SetActive(true);

            ska.transform.rotation = unitBase.transform.rotation;

            ska.AnimationState.Event -= OnEvent;
            ska.AnimationState.Event += OnEvent;

            ska.AnimationState.Complete -= OnComplete;
            ska.AnimationState.Complete += OnComplete;
            ska.AnimationState.SetAnimation(0, "skill2", false);
        }

        public override void OnSkill()
        {
            hitCnt = 0;
            listHitUnit.Clear();

            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }

        private void OnEvent(TrackEntry trackEntry, Spine.Event e)
        {
            hitCnt++;

            Vector3 v3Dir = unitBase.GetUnitLookDirection();

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position + (hitCnt * skillRange * 0.33f * v3Dir), skillRange * 0.33f, 2.0f, _isContainBlockedTarget: true));

            foreach(UnitBase unit in listUnit)
            {
                if (listHitUnit.Contains(unit))
                    continue;

                listHitUnit.Add(unit);

                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[0]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
                MgrObjectPool.Instance.ShowObj("FX_A_War_02_Lavi_skill2_hit", unit.GetUnitCenterPos());
            }
        }

        private void OnComplete(TrackEntry trackEntry)
        {
            objSkda.SetActive(false);
        }
    }
}
