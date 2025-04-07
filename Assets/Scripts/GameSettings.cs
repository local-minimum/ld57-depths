using LMCore.AbstractClasses;
using UnityEngine;

public class GameSettings : Singleton<GameSettings, GameSettings> 
{
    public bool EndWalksEarly
    {
        get
        {
            return PlayerPrefs.GetInt("EndEarly", 1) == 1;
        }

        set
        {
            PlayerPrefs.SetInt("EndEarly", value ? 1 : 0);
        }
    }

    public bool AutoAttack
    {
        get
        {
            return PlayerPrefs.GetInt("AutoAttack", 1) == 1;
        }

        set
        {
            PlayerPrefs.SetInt("AutoAttack", value ? 1 : 0);
        }
    }
}
