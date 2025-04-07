using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LMCore.Extensions;

public class Room : MonoBehaviour
{
    public static Room FightRoom;

    Level _level;
    Level level
    {
        get
        {
            if (_level == null)
            {
                _level = GetComponentInParent<Level>(true);
            }
            return _level;
        }
    }

    [SerializeField]
    Transform cameraViewPosition;

    [SerializeField]
    List<Door> doors = new List<Door>();

    List<Tile> tiles
    {
        get
        {
            return GetComponentsInChildren<Tile>(true).ToList();
        }
    }

    List<Enemy> enemies
    {
        get
        {
            return GetComponentsInChildren<Enemy>(true).ToList();
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

        level.CheckCleared();
        Debug.Log($"Room {name} cleared");
    }

    private void Start()
    {
        HideRoom();
    }

    [ContextMenu("Hide Room")]
    public void HideRoom()
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

    [ContextMenu("Show Room")]
    void ShowRoom()
    {
        foreach (var tile in tiles)
        {
            tile.gameObject.SetActive(true);
            tile.GetComponent<Collider>().enabled = true;
        }
        foreach (var door in doors)
        {
            door.gameObject.SetActive(true);
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
    public bool Revealed => revealed;
    List<Tile> animateInOrder;
    float animStart;

    public void AnimateIn(Vector3Int origin)
    {
        if (animating || revealed) return;

        Debug.Log($"Revealing {name} from {origin}");

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

    bool playerInRoom;
    bool easeCamera;
    float cameraEaseStart;
    Quaternion cameraStartRotation;
    Vector3 cameraStartPosition;

    private void PlayerController_OnEnterTile(PlayerController player)
    {
        if (tiles.Contains(player.currentTile))
        {
            AnimateIn(player.currentTile.coordinates);

            if (HasDanger)
            {
                FightRoom = this;
            }

            if (!playerInRoom)
            {
                playerInRoom = true;
                easeCamera = cameraViewPosition != null;
                cameraEaseStart = Time.timeSinceLevelLoad;
                cameraStartPosition = Camera.main.transform.position;
                cameraStartRotation = Camera.main.transform.rotation;
            }
        } else
        {
            playerInRoom = false;
            easeCamera = false;
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
        bool progressToNext = false;
        if (waitingForSaveThrow)
        {
            waitingForSaveThrow = PlayerController.instance.phase == PlayerController.PlayerPhase.SaveThrow;
            if (waitingForSaveThrow)
            {
                return;
            }
            progressToNext = true;
        }

        if (activeAttacker.Attack.Completed)
        {
            Debug.Log($"{activeAttacker.name} completed its attack");

            if (activeAttacker.Attack.Attacked && !progressToNext)
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
                Debug.Log($"{activeAttacker} will start its attack");
                activeAttacker.Attack.Perform();
            }
        }
    }

    [SerializeField, Header("Camera")]
    AnimationCurve cameraEasing;

    [SerializeField]
    float cameraEaseDuration = 1f;

    private void Update()
    {
        if (animating) AnimateInRoom();
        if (enemyAttacks) HandleEnemyAttacks();

        if (easeCamera)
        {
            var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - cameraEaseStart) / cameraEaseDuration);

            var cam = Camera.main.transform;

            var t = cameraEasing.Evaluate(progress);
            cam.position = Vector3.Lerp(cameraStartPosition, cameraViewPosition.position, t);
            cam.rotation = Quaternion.Lerp(cameraStartRotation, cameraViewPosition.rotation, t);

            easeCamera = progress < 1f;
        }
    }
}
