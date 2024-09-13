using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State_Skill : IState
{
    private UnitBase unitBase;

    public void InitializeState(UnitBase _unitBase) => unitBase = _unitBase;

    public void OnEnter()
    {
        unitBase.UpdateSpineUnitSpineSpeed();
    }

    public void OnExit()
    {

    }

    public void OnUpdate()
    {

    }
}
