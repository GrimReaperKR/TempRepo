using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonVisibleChecker : MonoBehaviour
{
    private UnitBase unitBaseParent;
    private bool isExit;

    private void Awake()
    {
        unitBaseParent = transform.parent.GetComponent<UnitBase>();
        isExit = false;
    }

    private void OnApplicationQuit()
    {
        isExit = true;
    }

    private void OnBecameInvisible()
    {
        if (!MgrBattleSystem.Instance.isStageStart || unitBaseParent.CheckIsState(UNIT_STATE.DEATH) || isExit)
            return;

        unitBaseParent.OnInvisibleInCamera();
    }

    private void OnBecameVisible()
    {
        if (!MgrBattleSystem.Instance.isStageStart || unitBaseParent.CheckIsState(UNIT_STATE.DEATH) || isExit)
            return;

        unitBaseParent.OnVisibleInCamera();
    }
}
