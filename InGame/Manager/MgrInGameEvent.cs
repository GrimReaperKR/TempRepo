using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MgrInGameEvent : Singleton<MgrInGameEvent>
{
    public List<IDamageEvent> listDamageEvent = new List<IDamageEvent>();
    public List<System.Action<string>> listBoosterEvent = new List<System.Action<string>>();
    public List<System.Action<string>> listActiveSkillEvent = new List<System.Action<string>>();
    public List<System.Action> listCanSpawnEvent = new List<System.Action>();

    public void AddDamageEvent(IDamageEvent _event) => listDamageEvent.Add(_event);
    public void RemoveDamageEvent(IDamageEvent _event) => listDamageEvent.Remove(_event);

    public void AddBoosterEvent(System.Action<string> _action) => listBoosterEvent.Add(_action); 
    public void RemoveBoosterEvent(System.Action<string> _action) => listBoosterEvent.Remove(_action); 
    
    public void AddActiveSkillEvent(System.Action<string> _action) => listActiveSkillEvent.Add(_action); 
    public void RemoveActiveSkillEvent(System.Action<string> _action) => listActiveSkillEvent.Remove(_action); 
    
    public void AddCanSpawnEvent(System.Action _action) => listCanSpawnEvent.Add(_action); 
    public void RemoveCanSpawnEvent(System.Action _action) => listCanSpawnEvent.Remove(_action); 

    public void BroadcastDamageEvent(UnitBase _attacker, UnitBase _victim, float _damage = 1.0f, float _criPer = 0.0f, float _criDmg = 1.5f, int _dmgChannel = 0)
    {
        for(int i = listDamageEvent.Count - 1; i >= 0; i--)
            listDamageEvent[i].OnDamage(_attacker, _victim, _damage, _criPer, _criDmg, _dmgChannel);
    }

    public void BroadcastBoosterUpgradeEvent(string _index)
    {
        foreach(System.Action<string> action in listBoosterEvent)
            action?.Invoke(_index);
    }

    public void BroadcastActiveSkillEvent(string _index)
    {
        foreach(System.Action<string> action in listActiveSkillEvent)
            action?.Invoke(_index);
    }

    public void BroadcastCanSpawnEvent()
    {
        foreach(System.Action action in listCanSpawnEvent)
            action?.Invoke();
    }
}
