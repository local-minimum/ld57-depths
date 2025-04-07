using LMCore.AbstractClasses;
using System.Collections.Generic;
using UnityEngine;

public class StoreUI : Singleton<StoreUI, StoreUI> 
{
    [SerializeField]
    List<GameObject> offers = new List<GameObject>();

    [SerializeField]
    List<GameObject> hideStats = new List<GameObject>();

    [SerializeField]
    GameObject leaveButton;

    public void ShowStore()
    {
        foreach (var offer in offers)
        {
            offer.SetActive(true);
        }

        leaveButton.SetActive(true);

        DiceHand.instance.RollHand();

    }

    void HideStore()
    {
        foreach (var stat in hideStats)
        {
            stat.SetActive(false);
        }

        foreach (var offer in offers)
        {
            offer.SetActive(false);
        }

        leaveButton.SetActive(false);

        DiceHand.instance.HideHand();
    }
    public void LeaveStore()
    {
        HideStore();
        Overworld.instance.DiveDeeper();
    }

    private void Start()
    {
        HideStore();
    }
}
