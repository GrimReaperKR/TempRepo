using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;

[CreateAssetMenu(fileName = "SOUnitSkillEvent_S_Arch_01_Tanya_S2", menuName = "UnitSkillEvent/S_Arch_01_Tanya_S2")]
public class S_Arch_01_Tanya_S2 : SOBase_UnitSkillEvent
{
    public override void InitializeSkill(UnitBase _unitBase)
    {
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] = new PersonalVariable();
        _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)].SetData(_unitBase, this);

        PersonalVariable personal = _unitBase.UnitSkillPersonalVariable[_unitBase.GetSkillIndex(this)] as PersonalVariable;
        _unitBase.SetKillEvent(personal.OnKillAction);
    }

    private class PersonalVariable : UnitSkillPersonalVariableInstance
    {
        private List<UnitBase> listUnit = new List<UnitBase>();

        public override void OnGiveDamageAction(UnitBase _attacker, UnitBase _victim, int _dmgChannel, float _damage)
        {
            if (_damage <= 0.0f || _attacker != unitBase || _dmgChannel != 1)
                return;

            MgrSound.Instance.PlayOneShotSFX("SFX_S_Arch_01_s2_3", 1.0f);

            if (MathLib.CheckPercentage((float)unitSkillData.Param[2]))
                _victim.AddUnitEffect(MgrInGameData.Instance.GetUnitEffectByIndexNum((int)unitSkillData.Param[3]), _attacker, _victim, new float[] { (float)unitSkillData.Param[4] });
        }

        public void OnKillAction(UnitBase _victim, int _dmgChannel)
        {
            if (_dmgChannel != 0)
                return;

            // 생성이다앙
            GameObject objVFX = MgrObjectPool.Instance.ShowObj("FX_S_Arch_01_Tanya_skill2", _victim.transform.position);
            objVFX.transform.GetChild(0).GetComponent<PlayableDirector>().Play();

            MgrSound.Instance.PlayOneShotSFX("SFX_S_Arch_01_s2_1", 1.0f);

            TaskSkill(objVFX).Forget();
        }

        private async UniTaskVoid TaskSkill(GameObject _objVFX)
        {
            await UniTask.Delay(1000, cancellationToken: _objVFX.GetCancellationTokenOnDestroy());

            listUnit.Clear();
            listUnit.AddRange(MgrBattleSystem.Instance.GetNearestEnemyUnitInEllipse(unitBase, _objVFX.transform.position, (float)unitSkillData.Param[0]));

            MgrSound.Instance.PlayOneShotSFX("SFX_S_Arch_01_s2_2", 1.0f);

            foreach (UnitBase unit in listUnit)
                MgrInGameEvent.Instance.BroadcastDamageEvent(unitBase, unit, unitBase.GetAtkRateToDamage((float)unitSkillData.Param[1]), unitBase.GetCriRate(), unitBase.UnitStat.CriDmg, 1);

            MgrObjectPool.Instance.ShowObj("FX_S_Arch_01_Tanya_skill2_hit", _objVFX.transform.position);
            MgrObjectPool.Instance.HideObj("FX_S_Arch_01_Tanya_skill2", _objVFX);
        }

        public override bool CheckCanUseSkill()
        {
            return false;
        }

        public override void EventTriggerEnd(string _animationName)
        {
            return;
        }

        public override void EventTriggerSkill()
        {

        }

        public override void OnSkill()
        {

        }
    }
}
