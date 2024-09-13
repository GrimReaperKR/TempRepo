using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
    public IState IStateCurState { get; private set; }

    public StateMachine(IState _defaultState) => IStateCurState = _defaultState;

    public void ChangeState(IState _state, bool _isChangeOnly = false)
    {
        if(!_isChangeOnly)
            IStateCurState.OnExit();

        IStateCurState = _state;

        if (!_isChangeOnly)
            IStateCurState.OnEnter();
    }

    public void OperateUpdate() => IStateCurState.OnUpdate();
}
