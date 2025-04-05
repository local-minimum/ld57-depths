using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DragDieUI : MonoBehaviour
{
    [SerializeField]
    Image image;

    [SerializeField]
    TextMeshProUGUI text;

    public Dice die { get; private set; }
    public Sprite sprite => image.sprite;

    public void SetFromDie(Dice die)
    {
        this.die = die;
        image.color = die.DieColor;
        text.text = die.Value.ToString();
        text.color = die.DieTextColor;
    }
}
