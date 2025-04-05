using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(CanvasGroup))]
public class FightActionUI : MonoBehaviour
{
    [SerializeField, Header("General")]
    int coolDownAfterUse = 0;

    [SerializeField]
    bool givesWalk;

    [SerializeField]
    bool givesAttack;

    [SerializeField]
    Image backgroundImage;

    [SerializeField]
    Color availableColor;

    [SerializeField]
    Color unavailableColor;

    [SerializeField]
    Color activeColor;

    [SerializeField, Header("Dice")]
    List<FightActionDiceSlotUI> diceSlots = new List<FightActionDiceSlotUI>();

    IEnumerable<FightActionDiceSlotUI> Slots => diceSlots.Where(s => s.gameObject.activeSelf);

    [SerializeField, Header("Action Button Settings")]
    Sprite activateSprite;

    [SerializeField]
    Sprite cooldownSprite;

    [SerializeField]
    Sprite undoDiceSprite;

    [SerializeField, Header("ActionButtons")]
    Button actionButton1;

    [SerializeField]
    Image actionButtonImg1;

    [SerializeField]
    TextMeshProUGUI actionButtonText1;

    [SerializeField]
    Button actionButton2;

    [SerializeField]
    Image actionButtonImg2;

    [SerializeField]
    TextMeshProUGUI actionButtonText2;

    int cooldown;

    public bool Available => cooldown <= 0;

    public FightActionDiceSlotUI FirstEmptySlot
    {
        get
        {
            if (!Available) return null;
            return Slots.FirstOrDefault(s => !s.Filled);
        }
    }

    public int Value
    {
        get
        {
            var lastValue = 0;
            var totalValue = 0;
            for (int i=0, l = diceSlots.Count; i<l; i++)
            {
                var die = diceSlots[i];
                if (!die.gameObject.activeSelf) continue;

                var value = die.Value;
                if (value == 0)
                {
                    lastValue = 0;
                    continue;
                }

                if (value == lastValue)
                {
                    totalValue += 3 * value;
                } else
                {
                    totalValue += value;
                    lastValue = value;
                }
            }

            return totalValue;
        }
    }

    bool AllSlotsFilled => Slots.All(slot => slot.Filled);
    bool AnySlotFilled => Slots.Any(slot => slot.Filled);

    enum ButtonAction { None, Undo, Activate };

    ButtonAction FirstActionButton;
    ButtonAction SecondActionButton;

    [ContextMenu("Sync")]
    public void Sync(bool allowActions = true)
    {
        if (cooldown > 0)
        {
            actionButton1.interactable = false;

            actionButtonText1.text = cooldown.ToString();
            actionButtonText1.enabled = true;

            actionButtonImg1.sprite = cooldownSprite;

            actionButton1.gameObject.SetActive(true);

            actionButton2.gameObject.SetActive(false);

            FirstActionButton = ButtonAction.None;
            SecondActionButton = ButtonAction.None;

            backgroundImage.color = unavailableColor;
        } else
        {
            actionButton1.interactable = Active == null && AnySlotFilled && allowActions; 
            actionButtonText1.enabled = false;
            actionButtonImg1.sprite = undoDiceSprite;
            actionButton1.gameObject.SetActive(true);
            FirstActionButton = actionButton1.interactable ? ButtonAction.Undo : ButtonAction.None;

            actionButton2.interactable = Active == null && AllSlotsFilled && allowActions;
            actionButtonText2.enabled = false;
            actionButtonImg2.sprite = activateSprite;
            actionButton2.gameObject.SetActive(true);
            SecondActionButton = actionButton2.interactable ? ButtonAction.Activate : ButtonAction.None;

            backgroundImage.color = availableColor;
        }

        if (Active == this)
        {
            backgroundImage.color = activeColor;
        }
    }

    private void OnEnable()
    {
        Sync();
    }

    public void ClickFirstButton() => PerformAction(FirstActionButton);

    public void ClickSecondButton() => PerformAction(SecondActionButton);

    void PerformAction(ButtonAction action)
    {
        switch (action)
        {
            case ButtonAction.Undo:
                foreach (var slot in Slots)
                {
                    slot.ReturnDie();
                }
                break;
            case ButtonAction.Activate:
                Active = this;
                Sync(false);

                backgroundImage.color = activeColor;
                cooldown = coolDownAfterUse;
                if (givesWalk)
                {
                    PlayerController.instance.FightWalkDistance = Value;
                }
                // TODO: Disable all actions while active
                // TODO: Listen for when we are done
                break;
        }
    }

    public void ConsumedWalk()
    {
        if (!givesAttack)
        {
            if (Active == this)
            {
                Active = null;
                Sync();
                ClearSlotsWithoutReturning();
            }
        }
    }

    void ClearSlotsWithoutReturning()
    {
        foreach (var slot in Slots)
        {
            slot.Clear();
        }
    }

    public static FightActionUI Focus { get; private set; }
    public static FightActionUI Active { get; private set; }

    public void OnPointerEnter()
    {
        Focus = this;
    }

    public void OnPointerExit()
    {
        if (Focus == this)
        {
            Focus = null;
        }
    }
}
