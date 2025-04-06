using LMCore.AbstractClasses;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DiceHand : Singleton<DiceHand, DiceHand>
{
    [SerializeField]
    Dice startDice;

    [SerializeField]
    int startDiceCount = 6;

    [SerializeField]
    Vector3 diceSpacing = new Vector3(-0.25f, 0f, 0f);

    [SerializeField]
    Vector3 saveThrowPosition = new Vector3(0f, 1f, 0f);

    [SerializeField]
    float timeBetweenDice = 0.5f;

    List<Dice> dice = new List<Dice>();

    public bool HasRemainingDice => dice.Any(d => !d.Used);
    public bool Empty => dice.Count == 0;

    private void Start()
    {
        for (int i = 0; i<startDiceCount; i++)
        {
            var die = Instantiate(startDice, transform);
            die.gameObject.SetActive(false);
            dice.Add(die);
        }
        
    }

    private void OnEnable()
    {
        Door.OnBreach += Door_OnBreach;    
    }

    private void OnDisable()
    {
        Door.OnBreach -= Door_OnBreach;
    }

    private void Door_OnBreach(Door door)
    {
        if (door.LeadsToDanger)
        {
            RollHand();
            PlayerController.instance.InFight = true;
        }
    }

    public void ShowDice()
    {
        foreach (var die in dice)
        {
            die.gameObject.SetActive(true);
        }
    }

    [ContextMenu("Roll Hand")]
    public void RollHand()
    {
        foreach (var die in dice)
        {
            die.gameObject.SetActive(false);
            die.Used = false;
        }
        rollingHand = dice.Count > 0;
        nextRoll = Time.timeSinceLevelLoad;
        rollIdx = 0;
    }

    public void SaveThrowRoll(Dice die)
    {
        var start = transform.TransformPoint(saveThrowPosition);
        die.transform.position = start;
        die.Roll(start, transform.right, Vector3.up);
    }

    public void RemoveDie(Dice die)
    {
        dice.Remove(die);
    }

    public void HideHand()
    {
        foreach (var die in dice)
        {
            die.gameObject.SetActive(false);
        }
        Dice.Focus = null;
    }

    bool rollingHand;
    int rollIdx = 0;
    float nextRoll;


    void RollHandUpdate()
    {
        if (!rollingHand || Time.timeSinceLevelLoad < nextRoll) return;

        nextRoll = Time.timeSinceLevelLoad + timeBetweenDice;
        var die = dice[rollIdx];
        die.gameObject.SetActive(true);
        var start = transform.TransformPoint(diceSpacing * rollIdx);
        die.transform.position = start;
        die.Roll(start, transform.right, Vector3.up);
        rollIdx++;

        if (rollIdx >= dice.Count)
        {
            rollingHand = false;
        }
    }

    private void Update()
    {
        RollHandUpdate();
    }
}
