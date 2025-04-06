using LMCore.AbstractClasses;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public delegate void PlayerEnterTileEvent(PlayerController player);

public class PlayerController : Singleton<PlayerController, PlayerController>
{
    public static event PlayerEnterTileEvent OnEnterTile;

    [SerializeField, Header("HUD")]
    Button endMoveButton;

    [SerializeField]
    TextMeshProUGUI stepsText;

    [SerializeField, Header("Walk Animation")]
    float walkSpeed = 0.7f;

    [SerializeField]
    AnimationCurve translationEasing;

    [SerializeField]
    AnimationCurve verticalTranslationEasing;

    [SerializeField]
    float verticalTranslationHeight;

    Tile _currentTile;
    public Tile currentTile
    {
        get
        {
            if (_currentTile == null)
            {
                _currentTile = Tile.ClosestTile(transform.position);
                OnEnterTile?.Invoke(this);
            }
            return _currentTile;
        }

        private set
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

            if (_FightWalkDistance == 0 && FightActionUI.Active != null)
            {
                FightActionUI.Active.ConsumedWalk();
            }
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
    }

    public void EndMovement()
    {
        FightWalkDistance = 0;
    }

    public enum PlayerPhase { Waiting, FreeWalk, Walk, SelectAttackTarget };
    public PlayerPhase phase
    {
        get
        {
            if (walking) return PlayerPhase.Waiting;

            if (!InFight) return PlayerPhase.FreeWalk;
            if (FightWalkDistance > 0) return PlayerPhase.Walk;
            if (AbsPlayerAttack.Focus != null && AbsPlayerAttack.Focus.Phase == AttackPhase.PlayerSelectTile)
            {
                return PlayerPhase.SelectAttackTarget;
            }
            // TODO: Fill out states
            return PlayerPhase.Waiting;
        }
    }

    bool _walking;
    bool walking
    {
        get => _walking;
        set
        {
            _walking = value;
            SyncHUD();
        }
    }

    int walkIndex;
    float walkStepStart;
    List<Tile> walkPath;

    public void Walk(List<Tile> path)
    {
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

    private void Update()
    {
        if (walking) EaseWalkStep();
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
        if (context.performed)
        {
            if (AbsPlayerAttack.Focus != null && AbsPlayerAttack.Focus.Phase == AttackPhase.PlayerSelectTile)
            {
                var targetTile = Tile.focusTile;
                if (targetTile != null)
                {
                    if (currentTile.ClosestPathTo(targetTile, out var path, AbsPlayerAttack.Focus.selectTileMaxRange))
                    {
                        if (path.Count >= AbsPlayerAttack.Focus.selectTileMinRange && path.Count > 0)
                        {
                            AbsPlayerAttack.Focus.SetTarget(path.Last());
                        }
                    }
                }
                return;
            }

            if (Dice.Focus != null)
            {
                draggedDie = Dice.Focus;
                DragDieUI.instance.SetFromDie(draggedDie);
                draggedDie.gameObject.SetActive(false);
                Cursor.visible = false;
                Dice.Focus = null;  
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
                if (currentTile.ClosestPathTo(focusTile, out var path, InFight ? FightWalkDistance : 100))
                {
                    Walk(path);
                    if (InFight)
                    {
                        FightWalkDistance -= path.Count - 1;
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
