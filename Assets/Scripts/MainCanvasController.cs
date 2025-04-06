using LMCore.AbstractClasses;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainCanvasController : Singleton<MainCanvasController, MainCanvasController> 
{
    [SerializeField]
    Animator anim;

    [SerializeField]
    string showFightActions;

    [SerializeField]
    string hideFightActions;

    List<FightActionUI> _actions;
    List<FightActionUI> actions
    {
        get
        {
            if (_actions == null)
            {
                _actions = GetComponentsInChildren<FightActionUI>(true).ToList();
            }
            return _actions;
        }
    }

    public void ShowFightActions() => anim.SetTrigger(showFightActions);
    public void HideFightActions() => anim.SetTrigger(hideFightActions);
    public void ClearAllDice()
    {
        foreach (var action in actions)
        {
            action.ClearDice();
        }
    }
}
