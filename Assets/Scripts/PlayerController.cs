using LMCore.AbstractClasses;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public delegate void PlayerEnterTileEvent(PlayerController player);
public delegate void CoinChangeEvent(int coins);

public class PlayerController : Singleton<PlayerController, PlayerController>
{
    public static event PlayerEnterTileEvent OnEnterTile;
    public static event CoinChangeEvent OnCoinsChange;

    [SerializeField]
    Renderer billboard;

    [SerializeField]
    Texture defaultTexture;

    [SerializeField, Header("HUD")]
    Button endMoveButton;

    [SerializeField]
    TextMeshProUGUI stepsText;

    [SerializeField]
    TextMeshProUGUI dieHealth;

    [SerializeField]
    TextMeshProUGUI coinsText;

    [SerializeField, Header("Walk Animation")]
    float walkSpeed = 0.7f;

    [SerializeField]
    AnimationCurve translationEasing;

    [SerializeField]
    AnimationCurve verticalTranslationEasing;

    [SerializeField]
    float verticalTranslationHeight;


    private int _hp = 6;
    public int HP {
        get => _hp;
        set {
            _hp = Mathf.Max(1, value);
            SyncHUD();
        } 
    }

    int _coins;
    public int Coins { 
        get => _coins; 
        set
        {
            _coins = Mathf.Max(0, value);
            OnCoinsChange?.Invoke(_coins);
            SyncHUD();
        }
    }

    Tile _currentTile;
    public Tile currentTile
    {
        get
        {
            if (_currentTile == null)
            {
                _currentTile = Tile.ClosestTile(transform.position);
            }
            return _currentTile;
        }

        set
        {
            _currentTile = value;
            OnEnterTile?.Invoke(this);
        }
    }

    bool _inFight = false;
    public bool InFight { 
        get => _inFight;
        set {
            _inFight = value;
            SyncHUD();
        } 
    }

    int _FightWalkDistance;
    public int FightWalkDistance { 
        get => _FightWalkDistance;
        set
        {
            _FightWalkDistance = Mathf.Max(0, value);
            SyncHUD();

            if (_FightWalkDistance == 0 && FightActionUI.Active != null && !walking)
            {
                FightActionUI.Active.ConsumedWalk();
            }
        }
    }

    public void SetCustomTexture(Texture texture)
    {
        if (billboard != null)
        {
            billboard.material.SetTexture("_BaseMap", texture);
        }

    }

    public void RemoveCustomTexture()
    {
        if (billboard != null)
        {
            billboard.material.SetTexture("_BaseMap", defaultTexture);
        }
    }

    void SyncHUD()
    {
        if (InFight)
        {
            stepsText.text = $"Steps: {_FightWalkDistance}";
            endMoveButton.gameObject.SetActive(_FightWalkDistance > 0 && !walking);
        } else
        {
            stepsText.text = "Free walking";
            endMoveButton.gameObject.SetActive(false);
        }

        dieHealth.text = $"Dice-health: <color=\"red\">{_hp}</color>";
        coinsText.text = $"Coins: {(Coins == 0 ? "<color=\"red\">0</color>" : Coins.ToString())}";
    }

    public void EndMovement()
    {
        FightWalkDistance = 0;
    }

    public enum PlayerPhase { Waiting, FreeWalk, Walk, SelectAction, SelectAttackTarget, SaveThrow };
    public PlayerPhase phase
    {
        get
        {
            if (walking || (currentTile != null && (currentTile.level == null || currentTile.level.ManagesPlayer))) return PlayerPhase.Waiting;
            if (saveThrowing) return PlayerPhase.SaveThrow;
            if (Room.FightRoom != null && Room.FightRoom.EnemyAttacks) return PlayerPhase.Waiting;

            if (!InFight) return PlayerPhase.FreeWalk;
            if (FightWalkDistance > 0) return PlayerPhase.Walk;
            if (AbsPlayerAttack.Focus != null && AbsPlayerAttack.Focus.Phase == AttackPhase.PlayerSelectTile)
            {
                return PlayerPhase.SelectAttackTarget;
            }
            if (InFight) return PlayerPhase.SelectAction;
            return PlayerPhase.Waiting;
        }
    }

    bool saveThrowing = false;
    bool saveThrowingWait = false;
    Dice saveThrowDie;
    string saveThrowMessage;

    public void PerformSaveThrow()
    {
        saveThrowing = true;
        DiceHand.instance.ShowDice();
        saveThrowMessage = $"Select die to roll <color=\"red\">{HP}</color>{(HP > 1 ? " or less" : "")} lest it <color=\"red\">break</color>!";
        HintUI.instance.SetText(saveThrowMessage);
    }

    bool _walking;
    public bool walking
    {
        get => _walking;
        private set
        {
            _walking = value;
            SyncHUD();
            if (!value && _FightWalkDistance == 0 && FightActionUI.Active != null)
            {
                FightActionUI.Active.ConsumedWalk();
            }
        }
    }

    int walkIndex;
    float walkStepStart;
    List<Tile> walkPath;

    public void Walk(List<Tile> path)
    {
        if (path.Last().occupyingEnemy != null)
        {
            path.RemoveAt(path.Count - 1);
        }

        walkPath = path;
        walkIndex = 0;
        walkStepStart = Time.timeSinceLevelLoad;
        walking = path != null && path.Count > 1;
        if (walking)
        {
            LookAtNextTarget();
        }
    }

    private void Start()
    {
        SyncHUD();
    }


    private void OnEnable()
    {
        CoinFountain.OnCoin += CoinFountain_OnCoin;
    }

    private void OnDisable()
    {

        CoinFountain.OnCoin -= CoinFountain_OnCoin;
    }

    private void CoinFountain_OnCoin()
    {
        Coins++;
    }


    private void Update()
    {
        if (walking) EaseWalkStep();
        if (saveThrowingWait) CheckSaveThrow();
    }

    void CheckSaveThrow()
    {
        if (saveThrowDie == null || saveThrowDie.Rolling) return;

        StartCoroutine(DelayResolveSaveThrow(saveThrowDie));
        saveThrowDie = null;
    }

    IEnumerator<WaitForSeconds> DelayResolveSaveThrow(Dice saveThrowDie)
    {
        var value = saveThrowDie.Value;
        bool breaks = value > HP;
        var msg = breaks ? "<color=\"red\">Die breaks!</color>" : "Die survives!";

        HintUI.instance.SetText(msg);
        yield return new WaitForSeconds(1f);
        HintUI.instance.RemoveText(msg);

        if (value > HP)
        {
            Debug.Log($"Oh no! Die breaks because {value} > {HP}");
            DiceHand.instance.RemoveDie(saveThrowDie);
            HP = 6;

            // TODO: Animate die breakage
            Destroy(saveThrowDie.gameObject);

            if (DiceHand.instance.Empty)
            {
                Debug.Log("Player has no more dice, player is dead.");
                SceneManager.LoadScene("DeathScene");
            }
        }

        this.saveThrowDie = null;
        saveThrowing = false;
        saveThrowingWait = false;
        // Room should let next attack or change phase automatically
    }

    void LookAtNextTarget()
    {
        var start = walkPath[walkIndex];
        var end = walkPath[walkIndex + 1];
        var direction = end.transform.position - start.transform.position;
        direction.y = 0;
        transform.LookAt(transform.position + direction);
    }

    void EaseWalkStep()
    {
        var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - walkStepStart) / walkSpeed);

        if (progress == 1)
        {
            currentTile = walkPath[walkIndex + 1];
            transform.position = currentTile.transform.position;

            walkStepStart = Time.timeSinceLevelLoad;
            walkIndex++;
            if (walkIndex >= walkPath.Count - 1)
            {
                walking = false;
            } else
            {
                LookAtNextTarget();
            }
        } else
        {
            var start = walkPath[walkIndex];
            var end = walkPath[walkIndex + 1];
            transform.position = Vector3.Lerp(start.transform.position, end.transform.position, translationEasing.Evaluate(progress)) +
                Vector3.up * verticalTranslationHeight * verticalTranslationEasing.Evaluate(progress);
        }
    }

    Dice draggedDie;
    public void Interact(InputAction.CallbackContext context)
    {
        if (phase == PlayerPhase.Waiting)
        {
            /*
            Debug.Log($"Waiting can't let you do stuff: " +
                $"Walking={walking} " +
                $"LevelManages={currentTile != null && currentTile.level.ManagesPlayer} " +
                $"EnemyAttacks={Room.FightRoom != null && Room.FightRoom.EnemyAttacks}");
            */
            return;
        }

        if (context.performed)
        {
            if (AbsPlayerAttack.Focus != null && AbsPlayerAttack.Focus.Phase == AttackPhase.PlayerSelectTile)
            {
                var targetTile = Tile.focusTile;
                if (targetTile != null)
                {
                    if (currentTile.ClosestPathTo(
                        targetTile, 
                        out var path, 
                        AbsPlayerAttack.Focus.selectTileMaxRange,
                        requireRoom: Room.FightRoom))
                    {
                        if (path.Count - 1 >= AbsPlayerAttack.Focus.selectTileMinRange && path.Count > 0)
                        {
                            AbsPlayerAttack.Focus.SetTarget(path.Last());
                        }
                    }
                }
                return;
            }

            if (Dice.Focus != null)
            {
                if (phase == PlayerPhase.SaveThrow)
                {
                    if (!saveThrowingWait)
                    {
                        saveThrowDie = Dice.Focus;
                        DiceHand.instance.SaveThrowRoll(Dice.Focus);
                        saveThrowingWait = true;
                        HintUI.instance.RemoveText(saveThrowMessage);
                    }
                } else
                {
                    draggedDie = Dice.Focus;
                    DragDieUI.instance.SetFromDie(draggedDie);
                    draggedDie.gameObject.SetActive(false);
                    Cursor.visible = false;
                    Dice.Focus = null;  
                }
                return;
            }

            if (walking || InFight && FightWalkDistance <= 0) return;

            if (Door.FocusDoor != null)
            {
                Door.FocusDoor.Breach();
                return;
            }

            var focusTile = Tile.focusTile;
            if (focusTile != null)
            {
                if (currentTile.ClosestPathTo(
                    focusTile, 
                    out var path, 
                    InFight ? FightWalkDistance : 100,
                    requireRoom: Room.FightRoom))
                {
                    Walk(path);
                    if (InFight)
                    {
                        if (GameSettings.instance.EndWalksEarly)
                        {
                            FightWalkDistance = 0;
                        } else
                        {
                            FightWalkDistance -= path.Count - 1;
                        }
                    }
                }
            }
        } else if (context.canceled)
        {
            if (draggedDie != null)
            {

                if (FightActionDiceSlotUI.FocusedSlot != null)
                {
                    if (!FightActionDiceSlotUI.FocusedSlot.TakeDie(draggedDie))
                    {
                        draggedDie.gameObject.SetActive(true);
                    }
                } else if (FightActionUI.Focus != null)
                {
                    var slot = FightActionUI.Focus.FirstEmptySlot;
                    if (slot != null)
                    {
                        slot.TakeDie(draggedDie);
                    } else
                    {
                        draggedDie.gameObject.SetActive(true);
                    }
                } else
                {
                    draggedDie.gameObject.SetActive(true);
                }

                DragDieUI.instance.Clear();
                draggedDie = null;
                Cursor.visible = true;
            }
        }
    }

    public void OnMovePointer(InputAction.CallbackContext context)
    {
        if (draggedDie != null)
        {
            var position = context.ReadValue<Vector2>();
            DragDieUI.instance.SyncPosition(position);
        }
    }

    [ContextMenu("Info")]
    void Info()
    {
        Debug.Log($"Player at: {currentTile}");
    }
}
