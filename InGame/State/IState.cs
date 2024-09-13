
public interface IState
{
    public void InitializeState(UnitBase _unitBase);
    public void OnEnter();
    public void OnUpdate();
    public void OnExit();
}
