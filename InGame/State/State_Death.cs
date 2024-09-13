using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State_Death : IState
{
    private UnitBase unitBase;

    public void InitializeState(UnitBase _unitBase) => unitBase = _unitBase;

    public void OnEnter()
    {
        unitBase.SetUnitAnimation(unitBase.animDeathName);
        unitBase.UpdateSpineUnitSpineSpeed();
    }

    public void OnExit()
    {

    }

    public void OnUpdate()
    {

    }
}
