using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_S_Arch_02_Romance_S1", menuName = "UnitSkillEvent/S_Arch_02_Romance_S1")]
public class S_Arch_02_Romance_S1 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        personal.InitWidthRange();
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();
        private List<GameObject> listObjBullet = new List<GameObject>();
        private int skillEventCnt = 0;

        public void InitWidthRange() => unitBase.UnitStat.WidthRange = (float)unitSkillData.Param[0];

        public override bool CheckCanUseSkill()
        {
            if (unitBase.Skill_CoolDown[unitBase.GetSkillIndex(soSkillEvent)] > 0.0f || unitBase.CheckIsState(UNIT_STATE.SKILL))
                return false;

            //if (unitBase.EnemyTarget && MathLib.CheckIsInEllipse(unitBase.transform.position, unitBase.GetSkillFloatDataValue(0, "range"), unitBase.EnemyTarget.transform.position)) return true;
            if (unitBase.EnemyTarget && MgrBattleSystem.Instance.CheckIsEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, (float)unitSkillData.Param[0])) return true;
            else
            {
                if (unitBase.CheckHasUnitEffect(UNIT_EFFECT.CC_TAUNT))
                    return false;

                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, skillRange, (float)unitSkillData.Param[0]));
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
            if(skillEventCnt == 0)
            {
                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitInPolygon(unitBase, unitBase.transform.position, unitBase.transform.position + skillRange * unitBase.GetUnitLookDirection(), (float)unitSkillData.Param[0], _isContainBlockedTarget: true));

                TaskSkillHit().Forget();
            }

            skillEventCnt++;
        }

        public override void OnSkill()
        {
            skillEventCnt = 0;
            unitBase.SetUnitAnimation("skill1");
            unitBase.PlayTimeline();

            TaskSkillVFX().Forget();
        }

        private async UniTaskVoid TaskSkillVFX()
        {
            listObjBullet.Clear();

            await UniTask.Delay(300, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            int bulletCnt = 10;
            int damageTimer = 1000 / bulletCnt;

            GameObject obj;
            while (bulletCnt > 0)
            {
                await UniTask.Delay(damageTimer, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

                obj = MgrObjectPool.Instance.ShowObj("FX_S_Arch_02_romance_skill1_bullet", unitBase.transform.position + Vector3.up * Random.Range(0.0f, 3.0f));
                listObjBullet.Add(obj);

                int bulletRandom = Random.Range(0, obj.transform.childCount);
                for (int i = 0; i < obj.transform.childCount; i++)
                    obj.transform.GetChild(i).gameObject.SetActive(i == bulletRandom);

                obj.transform.DOMoveX(obj.transform.position.x + ((unitBase.GetUnitLookDirection() == Vector3.right ? skillRange : -skillRange) * Random.Range(0.75f, 1.25f)), Random.Range(0.35f, 0.45f)).SetEase(Ease.Linear).OnComplete(() =>
                {
                    MgrObjectPool.Instance.HideObj("FX_S_Arch_02_romance_skill1_bullet", listObjBullet[0]);
                    listObjBullet.RemoveAt(0);
                });

                MgrSound.Instance.PlayOneShotSFX(bulletRandom % 2 == 0 ? "SFX_S_Arch_02_s1_2" : "SFX_S_Arch_02_s1_3", 0.5f);

                bulletCnt--;
            }
        }

        private async UniTaskVoid TaskSkillHit()
        {
            int hitCnt = (int)unitSkillData.Param[2];
            int damageTimer = 750 / hitCnt;

            while (hitCnt > 0)
            {
                await UniTask.Delay(damageTimer);

                foreach (UnitBase unit in listUnit)
                {
                    MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg);
                    MgrObjectPool.Instance.ShowObj("FX_S_Arch_02_romance_skill1_hit", unit.GetUnitCenterPos());
                }

                hitCnt--;
            }
        }
    }
}
