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
    float timeBetweenDice = 0.5f;

    [SerializeField]
    List<Transform> dicePositions = new List<Transform>();

    [SerializeField]
    Transform saveThrowPosition;

    List<Dice> dice = new List<Dice>();

    public bool Full => dicePositions.Count <= dice.Count;

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
        die.transform.SetParent(saveThrowPosition);
        die.transform.localPosition = Vector3.zero;
        die.InstaRoll();
    }

    public void RemoveDie(Dice die)
    {
        dice.Remove(die);
    }

    public bool AddDie(Dice die)
    {
        if (!Full)
        {
            dice.Add(die);
            NewRollDie(die);
            return true;
        }
        return false;
    }

    public void HideHand()
    {
        rollingHand = false;
        foreach (var die in dice)
        {
            die.gameObject.SetActive(false);
        }
        Dice.Focus = null;
    }

    bool rollingHand;
    int rollIdx = 0;
    float nextRoll;


    void NewRollDie(Dice die)
    {
        var idx = dice.IndexOf(die);
        if (idx < 0) return;
        die.gameObject.SetActive(true);
        die.transform.SetParent(dicePositions[rollIdx]);
        die.transform.localPosition = Vector3.zero;
        die.InstaRoll();
    }

    void RollHandUpdate()
    {
        if (!rollingHand || Time.timeSinceLevelLoad < nextRoll) return;

        nextRoll = Time.timeSinceLevelLoad + timeBetweenDice;
        var die = dice[rollIdx];
        NewRollDie(die);
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
