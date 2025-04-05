using LMCore.AbstractClasses;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public delegate void PlayerEnterTileEvent(PlayerController instance);

public class PlayerController : Singleton<PlayerController, PlayerController>
{
    public static event PlayerEnterTileEvent OnEnterTile;

    [SerializeField]
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

    public bool InFight { get; set; } = false;
    public int FightWalkDistance { get; set; } = 0;

    bool walking;
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
                }
            }
        } else if (context.canceled)
        {
            if (draggedDie != null)
            {

                if (FightActionDiceSlotUI.FocusedSlot != null)
                {
                    FightActionDiceSlotUI.FocusedSlot.TakeDie(draggedDie);
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
