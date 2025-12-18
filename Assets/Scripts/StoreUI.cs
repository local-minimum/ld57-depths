using LMCore.AbstractClasses;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class StoreUI : Singleton<StoreUI, StoreUI> 
{
    [SerializeField]
    List<GameObject> offers = new List<GameObject>();

    [SerializeField]
    List<GameObject> hideStats = new List<GameObject>();

    [SerializeField]
    GameObject leaveButton;

    bool showingStore;

    public void ShowStore()
    {
        showingStore = true;
        foreach (var offer in offers)
        {
            offer.SetActive(true);
        }

        foreach (var stat in hideStats)
        {
            stat.SetActive(false);
        }

        leaveButton.SetActive(true);

        DiceHand.instance.RollHand();

    }

    void HideStore()
    {
        showingStore = false;
        foreach (var offer in offers)
        {
            offer.SetActive(false);
        }

        foreach (var stat in hideStats)
        {
            stat.SetActive(false);
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


    public void Interact(InputAction.CallbackContext context)
    {
        Debug.Log($"{context.performed} {focusDie} {showingStore} {clickCounter}");
        if (context.performed && focusDie != null && showingStore)
        {
            clickCounter++;
            if (clickCounter >= 2)
            {
                Debug.Log($"Selling and Destroying {focusDie}");
                DiceHand.instance.RemoveDie(focusDie);
                Destroy(focusDie.gameObject);
                Dice.Focus = null;
                focusDie = null;
                clickCounter = 0;
                PlayerController.instance.Coins += 2;
            }
        }
    }

    Dice focusDie;
    int clickCounter;
    string message;

    private void Update()
    {
        if (showingStore && Dice.Focus != null)
        {
            if (focusDie != Dice.Focus)
            {
                if (DiceHand.instance.Size == 1)
                {
                    message = "Cannot sell last die";
                    focusDie = null;
                } else
                {
                    focusDie = Dice.Focus;
                    clickCounter = 0;
                    message = "Click die twice to sell for 2 coins";
                }
                HintUI.instance.SetText(message);
            }
        } else if (focusDie != null)
        {
            HintUI.instance.RemoveText(message);
            message = null;
            focusDie = null;
        } else if (focusDie == null && message != null)
        {
            HintUI.instance.RemoveText(message);
            message = null;
        }
    }
}
