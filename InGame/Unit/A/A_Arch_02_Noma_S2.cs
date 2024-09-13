using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_A_Arch_02_Noma_S2", menuName = "UnitSkillEvent/A_Arch_02_Noma_S2")]
public class A_Arch_02_Noma_S2 : SOBase_UnitSkillEvent
{
    [SerializeField] private SkeletonDataAsset skda;

    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private UnitBase targetUnit;

        private GameObject objSkda;
        private SkeletonAnimation ska;

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            if (unitBase.EnemyTarget && MathLib.CheckIsInEllipse(unitBase.transform.position, skillRange, unitBase.EnemyTarget.transform.position)) return true;
            else
            {
                if (unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    return false;

                UnitBase unitInEllipse = MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, skillRange);
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
            GameObject objBullet = MgrBulletPool.Instance.ShowObj("Bullet_A_Arch_02_S2", unitBase.transform.GetChild(5).position);
            objBullet.GetComponent<Bullet>().SetBullet(unitBase, targetUnit is null ? unitBase.EnemyTarget : targetUnit);
        }

        public override void OnSkill()
        {
            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();

            A_Arch_02_Noma_S2 skill = unitBase.soSkillEvent[1] as A_Arch_02_Noma_S2;
            objSkda = new GameObject();
            objSkda.transform.position = unitBase.transform.position + new Vector3(0.0f, 0.0f, 0.01f);
            objSkda.transform.SetParent(unitBase.transform);
            ska = objSkda.AddComponent<SkeletonAnimation>();
            ska.skeletonDataAsset = skill.skda;
            ska.Initialize(true);
            ska.tintBlack = true;
            ska.GetComponent<MeshRenderer>().sortingLayerName = "Unit";

            ska.transform.rotation = unitBase.transform.rotation;

            ska.AnimationState.Complete -= OnComplete;
            ska.AnimationState.Complete += OnComplete;
            ska.AnimationState.SetAnimation(0, "skill2", false);

            targetUnit = MgrBattleSystem.Instance.GetFarestEnemyUnitInEllipse(unitBase, skillRange * 2.0f);
        }

        private void OnComplete(Spine.TrackEntry trackEntry)
        {
            Destroy(objSkda);
            ska = null;
            objSkda = null;
        }
    }
}
