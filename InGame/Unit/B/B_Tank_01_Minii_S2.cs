using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_B_Tank_01_Minii_S2", menuName = "UnitSkillEvent/B_Tank_01_Minii_S2")]
public class B_Tank_01_Minii_S2 : SOBase_UnitSkillEvent
{
    public SkeletonDataAsset skda;

    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private bool isFormChanged;
        private SkeletonAnimation ska;

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] == 0.0f && !unitBase.CheckIsState(UNIT_STATE.SKILL) && unitBase.UnitStat.HP / unitBase.UnitStat.MaxHP <= (float)unitSkillData.Param[0] && !isFormChanged)
                return true;

            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill"))
                return;

            unitBase.Ska.skeletonDataAsset = ska.skeletonDataAsset;
            unitBase.Ska.Initialize(true);

            unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, true);

            Destroy(ska.gameObject);
            ska = null;

            unitBase.InitSpineEvent();

            unitBase.SetUnitState(UNIT_STATE.IDLE);
            unitBase.SetUnitSkillCoolDown(unitBase.GetSkillIndex(soSkillEvent), skillCooldown);

            isFormChanged = true;
            unitBase.SetHeal(unitBase.UnitStat.MaxHP * (float)unitSkillData.Param[1]);
            MgrObjectPool.Instance.ShowObj("FX_PC-Heal", unitBase.GetUnitCenterPos());

            unitBase.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitBase.UnitSkillPersonalVariable[0].unitSkillData.Param[2]), unitBase, unitBase, new float[] { (float)unitBase.UnitSkillPersonalVariable[0].unitSkillData.Param[3], 0.0f }, false);
            MgrObjectPool.Instance.ShowObj("FX_Buff_Enhance", unitBase.transform.position).transform.SetParent(unitBase.transform);
            MgrSound.Instance.PlayOneShotSFX("SFX_Buff_ab", 1.0f);
            //unitBase.AddUnitEffect(UNIT_EFFECT.BUFF_ATK_SPEED, unitBase, unitBase, new float[] { 0.5f, 0.0f }, false);
        }

        public override void EventTriggerSkill()
        {
            //MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unitBase.EnemyTarget, 1.0f);
        }

        public override void OnSkill()
        {
            unitBase.AddUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, unitBase, new float[] { 0.8f, 0.0f }, false);

            B_Tank_01_Minii_S2 comp = unitBase.soSkillEvent[1] as B_Tank_01_Minii_S2;

            GameObject objTemp = new GameObject();
            objTemp.transform.SetParent(unitBase.transform);
            objTemp.transform.localPosition = Vector3.zero;
            ska = objTemp.AddComponent<SkeletonAnimation>();
            ska.tintBlack = true;
            ska.skeletonDataAsset = comp.skda;
            ska.Initialize(true);
            ska.AnimationState.SetAnimation(0, "skill2", false);
            ska.GetComponent<MeshRenderer>().sortingLayerName = "Unit";

            ska.transform.rotation = unitBase.transform.rotation;

            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();
        }

        public override void ResetSkill()
        {
            unitBase.RemoveUnitEffect(UNIT_EFFECT.BUFF_TAKE_DMG, unitBase, true);

            if(!(ska is null))
            {
                Destroy(ska.gameObject);
                ska = null;
            }
        }
    }
}
