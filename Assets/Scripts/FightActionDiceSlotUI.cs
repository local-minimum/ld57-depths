using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FightActionDiceSlotUI : MonoBehaviour
{
    [SerializeField]
    Image diceImage;

    [SerializeField]
    TextMeshProUGUI diceText;

    Dice activeDie;

    public void Hold(DragDieUI dragDie)
    {
        activeDie = dragDie.die;
        if (activeDie == null)
        {
            diceImage.enabled = false;
            diceText.enabled = false;
        } else
        {
            diceImage.enabled = true;
            diceText.enabled = true;

            diceImage.sprite = dragDie.sprite;
            diceImage.color = activeDie.DieColor;

            diceText.text = activeDie.Value.ToString();
            diceText.color = activeDie.DieTextColor;
        }
    }

    public void Clear()
    {
        activeDie = null;
        diceImage.enabled = false;
        diceText.enabled = false;
    }

    public int Value => activeDie == null ? 0 : activeDie.Value;
    public bool Filled => activeDie != null;

    public static FightActionDiceSlotUI FocusedSlot { get; private set; }

    public void OnPointerEnter()
    {
        FocusedSlot = this;
    }

    public void OnPointerExit()
    {
        if (FocusedSlot == this)
        {
            FocusedSlot = null;
        }
    }
}
