using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Spine.Unity;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_A_Arch_01_Hero_S2", menuName = "UnitSkillEvent/A_Arch_01_Hero_S2")]
public class A_Arch_01_Hero_S2 : SOBase_UnitSkillEvent
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
        private ParticleSystem parsysLaser;
        private ParticleSystem.MainModule mainModule;
        private ParticleSystem.MinMaxCurve curve;

        private List<UnitBase> listUnit = new List<UnitBase>();
        private int hitCnt = 0;

        public override bool CheckCanUseSkill()
        {
            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            if (_animationName.Contains("skill2_ready"))
            {
                unitBase.transform.position += (float)unitSkillData.Param[5] * (unitBase.TeamNum == 0 ? Vector3.left : Vector3.right);
                unitBase.transform.rotation = Quaternion.Euler(0.0f, unitBase.TeamNum == 0 ? 0.0f : -180.0f, 0.0f);

                TaskKnockback().Forget();
            }

            if (_animationName.Contains("skill2_attack"))
                unitBase.OnDefaultDeath(unitBase, unitBase, -1);
        }

        public override void EventTriggerSkill()
        {
            hitCnt++;
            if (hitCnt % 2 == 0 && (int)unitSkillData.Param[2] == 3)
                return;

            Vector3 v3Dir = unitBase.TeamNum == 0 ? Vector3.right : Vector3.left;

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitInPanShape(unitBase, unitBase.transform.position + (v3Dir * -2.0f), v3Dir, (float)unitSkillData.Param[0], 30.0f, _isContainBlockedTarget: true));

            foreach(UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);
        }

        public override void OnSkill()
        {
            for (int i = unitBase.ListEffectPersonalVariable.Count - 1; i >= 0; i--)
                unitBase.ListEffectPersonalVariable[i].OnEnd();

            if (parsysLaser is null)
            {
                parsysLaser = unitBase.transform.Find("FX_Laser_s2(Clone)").GetComponent<ParticleSystem>();
                mainModule = parsysLaser.main;
                curve = mainModule.startRotation;
            }

            curve.constant = (unitBase.TeamNum == 0 ? -90.0f : 90.0f) * Mathf.Deg2Rad;
            mainModule.startRotation = curve;

            hitCnt = 0;
            unitBase.SetUnitState(UNIT_STATE.DEATH, true, true);
            unitBase.SetUnitAnimation("skill2_ready");
            unitBase.PlayTimeline();
        }

        public void OnDeath(UnitBase _attacker, UnitBase _victim, int _dmgChannel)
        {
            _attacker.OnKillEvent(unitBase, _dmgChannel);

            unitBase.UnitPlayableDirector.Stop();
            unitBase.UnitPlayableDirector.playableAsset = null;
            unitBase.SetUnitUseSkill(unitBase.soSkillEvent[1]);
        }

        private async UniTaskVoid TaskKnockback()
        {
            await UniTask.Delay(1333, cancellationToken: unitBase.GetCancellationTokenOnDestroy());

            Vector3 v3Dir = unitBase.TeamNum == 0 ? Vector3.right : Vector3.left;

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetEnemyUnitInPanShape(unitBase, unitBase.transform.position + (v3Dir * -2.0f), v3Dir, (float)unitSkillData.Param[0], 30.0f, _isContainBlockedTarget: true));
            listUnit.Remove(MgrBattleSystem.Instance.GetAllyBase());

            foreach (UnitBase unit in listUnit)
                unit.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitSkillData.Param[3]), unit, unit, new float[] { (float)unitSkillData.Param[4] });
        }
    }
}
