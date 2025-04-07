using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiceOffer : MonoBehaviour
{
    [SerializeField, Range(0, 4)]
    float diceSpinSpeed = 0.5f;

    [SerializeField]
    Dice prefab;

    [SerializeField]
    int cost;

    [SerializeField]
    TextMeshProUGUI costText;

    [SerializeField]
    Button buyButton;

    Dice dice;

    [SerializeField]
    Transform diceParent;

    private void OnEnable()
    {
        if (dice == null)
        {
            dice = Instantiate(prefab, diceParent);
        }
        dice.Spin(diceSpinSpeed);
        costText.text = cost.ToString();
        SyncButton();

        PlayerController.OnCoinsChange += PlayerController_OnCoinsChange;
    }

    private void PlayerController_OnCoinsChange(int coins)
    {
        SyncButton();
    }

    void SyncButton()
    {
        buyButton.interactable = !DiceHand.instance.Full && 
            PlayerController.instance.Coins >= cost;
    }

    private void OnDisable()
    {
        dice.EndSpin();

        PlayerController.OnCoinsChange -= PlayerController_OnCoinsChange;
    }

    public void Buy()
    {
        if (!DiceHand.instance.Full)
        {
            var die = Instantiate(prefab);
            if (!DiceHand.instance.AddDie(die))
            {
                Debug.LogError("We couldn't add bought die to hand");
                PlayerController.instance.Coins -= cost;
                Destroy(die.gameObject);
            }
        }
    }
}
