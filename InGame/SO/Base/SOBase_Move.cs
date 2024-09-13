using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SOBase_MoveUnit : ScriptableObject
{
    public abstract bool CheckIsChangeToMoveState(UnitBase _unitBase);
    public abstract void Move(UnitBase _unitBase);
}
