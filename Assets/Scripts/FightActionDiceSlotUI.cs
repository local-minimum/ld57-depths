using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FightActionDiceSlotUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField]
    Image background;

    [SerializeField]
    Image diceImage;

    [SerializeField]
    TextMeshProUGUI diceText;

    [SerializeField]
    Color focusColor;

    [SerializeField]
    Color normalColor;

    Dice activeDie;

    FightActionUI _action;
    FightActionUI action
    {
        get
        {
            if (_action == null)
            {
                _action = GetComponentInParent<FightActionUI>();
            }
            return _action;
        }
    }

    public bool TakeDie(Dice dragDie)
    {
        if (!action.Available) return false;

        if (activeDie != null && activeDie != dragDie)
        {
            ReturnDie();
        }

        activeDie = dragDie;
        if (activeDie == null)
        {
            diceImage.enabled = false;
            diceText.enabled = false;
        } else
        {
            diceImage.enabled = true;
            diceText.enabled = true;

            diceImage.color = activeDie.DieColor;

            diceText.text = activeDie.Value.ToString();
            diceText.color = activeDie.DieTextColor;
        }

        action.Sync();
        return true;
    }

    public void Clear(bool clearActive = true)
    {
        diceImage.enabled = false;
        diceText.enabled = false;

        if (clearActive)
        {
            activeDie = null;
            action.Sync();
        }
    }

    public int Value => activeDie == null ? 0 : activeDie.Value;
    public bool Filled => activeDie != null;

    public static FightActionDiceSlotUI FocusedSlot { get; private set; }

    public void OnPointerEnter()
    {
        background.color = focusColor;
        FocusedSlot = this;
    }

    public void OnPointerExit()
    {
        background.color = normalColor;
        if (FocusedSlot == this)
        {
            FocusedSlot = null;
        }
    }

    private void OnEnable()
    {
        if (!Filled) Clear();
    }

    public void ReturnDie()
    {
        if (activeDie == null) return;
        activeDie.gameObject.SetActive(true);
        activeDie = null;
        Clear();
    }

    bool dragging;

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragging)
        {
            if (FocusedSlot != null)
            {
                if (FocusedSlot.TakeDie(activeDie))
                {
                    Clear();
                } else
                {
                    TakeDie(activeDie);
                }
            } else if (FightActionUI.Focus != null)
            {
                var slot = FightActionUI.Focus.FirstEmptySlot; 
                if (slot != null)
                {
                    if (slot.TakeDie(activeDie))
                    {
                        Clear();
                    } else
                    {
                        TakeDie(activeDie);
                    }
                } else
                {
                    TakeDie(activeDie);
                }
            } else
            {
                TakeDie(activeDie);
            }
            DragDieUI.instance.Clear();
        }

        dragging = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Filled)
        {
            dragging = true;
            DragDieUI.instance.SetFromDie(activeDie);
            Clear(false);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragging)
        {
            DragDieUI.instance.SyncPosition(eventData.position);
        }
    }
}
