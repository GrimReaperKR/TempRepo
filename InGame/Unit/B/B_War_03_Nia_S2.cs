using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using DG.Tweening;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_B_War_03_Nia_S2", menuName = "UnitSkillEvent/B_War_03_Nia_S2")]
public class B_War_03_Nia_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetDeathEvent(personal.OnDeath);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private int atkCnt = 0;
        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || (_dmgChannel != 1 && _dmgChannel != 2))
                return;

            if (_dmgChannel == 1) MgrSound.Instance.PlayOneShotSFX("SFX_B_War_03_s2_6", 1.0f);
            if (_dmgChannel == 2) MgrSound.Instance.PlayOneShotSFX("SFX_B_War_03_s2_7", 1.0f);
        }

        public override bool CheckCanUseSkill()
        {
            return false;
        }

        public void OnDeath(UnitBase _attacker, UnitBase _victim, int _dmgChannel)
        {
            _attacker.OnKillEvent(unitBase, _dmgChannel);

            unitBase.UnitPlayableDirector.Stop();
            unitBase.UnitPlayableDirector.playableAsset = null;
            unitBase.SetUnitUseSkill(unitBase.soSkillEvent[1]);
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (!_animationName.Contains("skill2"))
                return;

            unitBase.OnDefaultDeath(unitBase, unitBase, -1);
        }

        public override void EventTriggerSkill()
        {
            atkCnt++;

            if (atkCnt <= 5)
            {
                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, unitBase.transform.position, 3.0f, _isContainBlockedTarget: true));

                foreach(UnitBase unit in listUnit)
                {
                    MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
                    MgrObjectPool.Instance.ShowObj("FX_B_War_03_Nia_skill2_hit", unit.GetUnitCenterPos());
                }
            }
            else
            {
                listUnit.Clear();
                listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInLine(unitBase, unitBase.transform.position, 2.0f, 2.0f, _isContainBlockedTarget: true));

                if(listUnit.Count > 0)
                {
                    MgrObjectPool.Instance.ShowObj("FX_Nia_Skill2_Rainbow_Hit", listUnit[0].GetUnitCenterPos()).transform.SetParent(listUnit[0].transform);
                    MgrObjectPool.Instance.ShowObj("FX_B_War_03_Nia_skill2_Rainbow", listUnit[0].transform.position + (Vector3.up * listUnit[0].GetUnitHeight())).transform.SetParent(listUnit[0].transform);
                    TaskHitSkill(listUnit[0]).Forget();
                }
            }
        }

        public override void OnSkill()
        {
            for (int i = unitBase.ListEffectPersonalVariable.Count - 1; i >= 0; i--)
                unitBase.ListEffectPersonalVariable[i].OnEnd();

            atkCnt = 0;

            unitBase.SetUnitState(UNIT_STATE.DEATH, true, true);
            unitBase.SetUnitAnimation("skill2");
            unitBase.PlayTimeline();

            unitBase.transform.rotation = Quaternion.Euler(0.0f, unitBase.TeamNum == 0 ? 0.0f : -180.0f, 0.0f);

            float moveRange = unitBase.GetUnitLookDirection() == Vector3.left ? -(float)unitSkillData.Param[0] : (float)unitSkillData.Param[0];
            unitBase.transform.DOMoveX(unitBase.transform.position.x + moveRange, 1.5f).SetEase(Ease.OutSine);
            //TaskSkill().Forget();
        }

        //private async UniTaskVoid TaskSkill()
        //{
        //    float moveRange = unitBase.transform.rotation.y == -1.0f ? -(float)unitSkillData.Param[0] : (float)unitSkillData.Param[0];
        //    unitBase.transform.DOMoveX(unitBase.transform.position.x + moveRange, 1.5f).SetEase(Ease.OutSine);
        //}

        private async UniTaskVoid TaskHitSkill(UnitBase _unitTarget)
        {
            await UniTask.Delay(500, cancellationToken: unitBase.GetCancellationTokenOnDestroy());
            MgrSound.Instance.PlayOneShotSFX("SFX_B_War_03_s2_4", 1.0f);
            MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, _unitTarget, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[2]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 2);
        }
    }
}
