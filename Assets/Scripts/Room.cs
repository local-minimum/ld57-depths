using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LMCore.Extensions;

public class Room : MonoBehaviour
{
    public static Room FightRoom;

    [SerializeField]
    List<Door> doors = new List<Door>();

    List<Tile> _tiles;
    List<Tile> tiles
    {
        get
        {
            if (_tiles == null)
            {
                _tiles = GetComponentsInChildren<Tile>(true).ToList();
            }

            return _tiles;
        }
    }

    List<Enemy> _enemies;
    List<Enemy> enemies
    {
        get
        {
            if (_enemies == null)
            {
                _enemies = GetComponentsInChildren<Enemy>(true).ToList();
            }
            return _enemies;
        }
    }

    public bool HasTile(Tile tile) => tiles.Contains(tile);

    public bool HasDanger => 
        enemies.Any(e => e.Alive);

    List<Enemy> attackers;
    bool enemyAttacks;
    public bool EnemyAttacks => enemyAttacks;
    int attackingEnemyIdx = 0;

    public void CheckStillDanger()
    {
        if (HasDanger)
        {
            if (!DiceHand.instance.HasRemainingDice)
            {
                if (
                    AbsPlayerAttack.Focus != null ||
                    PlayerController.instance.walking)
                {
                    Debug.Log("Waiting for player");
                    return;
                }

                Debug.Log("Enemy phase");

                var pCoords = PlayerController.instance.currentTile.coordinates;
                attackers = enemies
                    .Where(e => e.Alive)
                    .OrderBy(e => e.currentTile.coordinates.ManhattanDistance(pCoords))
                    .ToList();


                Debug.Log($"{attackers.Count} enemies wants to attack");

                bool first = true;
                foreach (var enemy in attackers)
                {
                    enemy.Attack.Completed = false;
                    if (first)
                    {
                        activeAttacker = enemy;
                        enemy.Attack.Perform();
                        first = false;
                        waitingForSaveThrow = false;
                    }
                }

                if (attackers.Count > 0)
                {
                    enemyAttacks = true;
                    MainCanvasController.instance.HideFightActions();
                    attackingEnemyIdx = 0;
                } else
                {
                    Debug.LogWarning("There were no enemies to attack back");
                    // This is odd and shouldn't happen
                    DiceHand.instance.RollHand();
                    MainCanvasController.instance.ShowFightActions();
                }
            }
            return;
        }

        EndRoomDanger();
    }

    void EndRoomDanger()
    {
        // Remove fight state
        if (FightRoom == this)
        {
            FightRoom = null;
        }

        PlayerController.instance.InFight = false;
        DiceHand.instance.HideHand();
        MainCanvasController.instance.ClearAllDice();
        MainCanvasController.instance.HideFightActions();
        Debug.Log($"Room {name} cleared");
    }

    private void Start()
    {
        foreach (var tile in tiles)
        {
            tile.gameObject.SetActive(false);
            tile.GetComponent<Collider>().enabled = false;
        }
        foreach (var door in doors)
        {
            door.HideIfNotSynced();
        }
    }

    [SerializeField]
    float yAnimationOffset = 0.45f;

    [SerializeField]
    float tileDelta = 0.1f;

    [SerializeField]
    float tileAnimDuration = 0.4f;

    bool animating;
    bool revealed;
    List<Tile> animateInOrder;
    float animStart;

    public void AnimateIn(Vector3Int origin)
    {
        if (animating || revealed) return;

        animateInOrder = tiles.OrderBy(t => t.coordinates.ManhattanDistance(origin)).ToList();
        animStart = Time.timeSinceLevelLoad;
        animating = true;
    }

    private void OnEnable()
    {
        PlayerController.OnEnterTile += PlayerController_OnEnterTile;
    }

    private void OnDisable()
    {
        PlayerController.OnEnterTile -= PlayerController_OnEnterTile;
    }

    private void PlayerController_OnEnterTile(PlayerController player)
    {
        if (tiles.Contains(player.currentTile))
        {
            AnimateIn(player.currentTile.coordinates);

            if (HasDanger)
            {
                FightRoom = this;
            }
        }
    }

    void AnimateInRoom()
    {
        var delta = Time.timeSinceLevelLoad - animStart;
        for (int i = 0, l = animateInOrder.Count; i<l; i++)
        {
            var tile = animateInOrder[i];
            var progress = Mathf.Clamp01((delta - i * tileDelta) / tileAnimDuration);
            var tileDoors = doors.Where(d => d.NeighboursTile(tile));

            if (progress == 0) continue;
            if (progress == 1)
            {
                tile.SyncPosition();
                foreach (var door in tileDoors)
                {
                    door.SyncPosition();
                }
                if (i == l - 1)
                {
                    animating = false;
                    revealed = true;
                    if (HasDanger)
                    {
                        MainCanvasController.instance.ShowFightActions();
                    }
                }
            } else
            {
                var offset = (1f - progress) * yAnimationOffset;
                tile.SetPosition(offset);
                foreach (var door in tileDoors)
                {
                    door.SetPosition(offset);
                }
            }
        }
    }

    Enemy activeAttacker;
    bool waitingForSaveThrow;

    void HandleEnemyAttacks()
    {
        if (waitingForSaveThrow)
        {
            waitingForSaveThrow = PlayerController.instance.phase == PlayerController.PlayerPhase.SaveThrow;
            if (waitingForSaveThrow)
            {
                return;
            }
        }

        if (activeAttacker.Attack.Completed)
        {
            Debug.Log($"{activeAttacker.name} completed its attack");

            if (activeAttacker.Attack.Attacked && !waitingForSaveThrow)
            {
                PlayerController.instance.PerformSaveThrow();
                waitingForSaveThrow = true;
                return;
            }

            waitingForSaveThrow = false;
            attackingEnemyIdx++;

            if (attackingEnemyIdx >= attackers.Count)
            {
                attackers = null;
                enemyAttacks = false;

                Debug.Log("Enemies phase end");

                if (HasDanger)
                {
                    DiceHand.instance.RollHand();
                    MainCanvasController.instance.ShowFightActions();
                } else
                {
                    Debug.LogWarning("For some reason enemies died during their phase, ending danger");
                    // Enemies shouldn't have died while attacking but lets be sure
                    EndRoomDanger();
                }
            } else
            {
                activeAttacker = attackers[attackingEnemyIdx];
                activeAttacker.Attack.Perform();
            }
        }
    }

    private void Update()
    {
        if (animating) AnimateInRoom();
        if (enemyAttacks) HandleEnemyAttacks();
    }
}
