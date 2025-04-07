using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public Level level
    {
        get
        {
            return GetComponentInParent<Level>(true);
        }
    }

    private static Dictionary<Vector3Int, Tile> Tiles = new Dictionary<Vector3Int, Tile>();

    [SerializeField]
    Image hudImage;
    [SerializeField]
    TextMeshProUGUI hudText;

    [SerializeField]
    Color defaultColor;
    [SerializeField]
    Color goodColor;
    [SerializeField]
    Color badColor;

    public static Tile ClosestTile(Vector3 position)
    {
        var coordinates = new Vector3Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), Mathf.RoundToInt(position.z));
        return Tiles.OrderBy(kvp => kvp.Key.ManhattanDistance(coordinates)).Select(kvp => kvp.Value).FirstOrDefault();
    }

    bool inited = false;

    Vector3Int _coordinates;
    public Vector3Int coordinates
    {
        get
        {
            if (!inited)
            {
                inited = true;
                var pos = transform.position;
                _coordinates = new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));
            }

            return _coordinates;
        }
    }

    public void SyncPosition()
    {
        transform.position = coordinates;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        GetComponent<Collider>().enabled = true;
    }

    public void SetPosition(float yOffset)
    {
        transform.position = coordinates + Vector3.up * yOffset;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    void ClearHUD()
    {
        hudImage.enabled = false;
        hudText.enabled = false;
    }

    public enum HUDState { Default, Good, Bad };

    Color HudColor(HUDState state)
    {
        switch (state)
        {
            case HUDState.Good:
                return goodColor;
            case HUDState.Bad:
                return badColor;
            default:
                return defaultColor;
        }
    }
    public void ShowHUD(string message, HUDState state = HUDState.Default)
    {
        var color = HudColor(state);
        hudText.text = message;
        hudText.color = color;
        hudText.enabled = true;

        hudImage.color = color;
        hudImage.enabled = true;
    }

    private void Start()
    {
        ClearHUD();
    }

    private void OnEnable()
    {
        name = $"Tile {coordinates}";
        PlayerController.OnEnterTile += PlayerController_OnEnterTile;
        Enemy.OnEnterTile += Enemy_OnEnterTile;

        if (level != null)
        {
            Tiles.Add(coordinates, this);
        }
    }

    private void OnDisable()
    {
        PlayerController.OnEnterTile -= PlayerController_OnEnterTile;
        Enemy.OnEnterTile -= Enemy_OnEnterTile;

        if (Tiles.TryGetValue(coordinates, out var tile) && tile == this) 
        {
            Tiles.Remove(coordinates);
        }
        ClearHUD();
    }

    public Enemy occupyingEnemy { get; private set; }
    public PlayerController occupyingPlayer { get; private set; }

    public bool Occupied => occupyingEnemy != null || occupyingPlayer != null;

    private void PlayerController_OnEnterTile(PlayerController player)
    {
        occupyingPlayer = player.currentTile == this ? player : null;
    }

    private void Enemy_OnEnterTile(Enemy enemy)
    {
        if (enemy.currentTile == this)
        {
            occupyingEnemy = enemy;
        } else if (enemy == occupyingEnemy)
        {
            occupyingEnemy = null;
        }
    }

    public static Tile focusTile { get; private set; }

    List<Tile> highlightTiles;

    private void OnMouseEnter()
    {
        focusTile = this;
        var playerPhase = PlayerController.instance.phase;

        if (playerPhase == PlayerController.PlayerPhase.FreeWalk || playerPhase == PlayerController.PlayerPhase.Walk)
        {
            if (PlayerController.instance.currentTile.ClosestPathTo(focusTile, out var path, maxDepth: 100))
            {
                bool restricted = playerPhase == PlayerController.PlayerPhase.Walk;
                int maxDistance = PlayerController.instance.FightWalkDistance;

                for (int i=0, l= path.Count; i<l;i++)
                {
                    var tile = path[i];
                    bool illegalTile = Room.FightRoom != null && !Room.FightRoom.HasTile(tile);

                    if (i == 0)
                    {
                        tile.ShowHUD(null, HUDState.Good);
                    } else if (!illegalTile && (!restricted || i <= maxDistance))
                    {
                        tile.ShowHUD(restricted ? i.ToString() : null, HUDState.Good);
                    } else
                    {
                        tile.ShowHUD("X", HUDState.Bad);
                    }
                }

                highlightTiles = path;
            }
        } else if (playerPhase == PlayerController.PlayerPhase.SelectAttackTarget)
        {
            var attack = AbsPlayerAttack.Focus;
            if (attack != null)
            {
                if (PlayerController.instance.currentTile.ClosestPathTo(focusTile, out var path, maxDepth: 100))
                {
                    for (int i=0, l=path.Count; i<l; i++)
                    {
                        var goodDistance = i >= attack.selectTileMinRange && i <= attack.selectTileMaxRange;
                        var tile = path[i];
                        bool illegalTile = Room.FightRoom != null && !Room.FightRoom.HasTile(tile);

                        if (i < attack.selectTileMinRange)
                        {
                            tile.ShowHUD(null, HUDState.Good);
                        }
                        else if (illegalTile) {
                            tile.ShowHUD("X", HUDState.Bad);
                        }
                        else if (tile == this)
                        {
                            tile.ShowHUD(goodDistance ? "O" : "X", goodDistance ? HUDState.Good : HUDState.Bad);
                        }
                        else
                        {
                            tile.ShowHUD(goodDistance ? "." : "X", goodDistance ? HUDState.Good : HUDState.Bad);
                        }
                    }

                    highlightTiles = path;
                }
            }
        }
    }

    private void OnMouseExit()
    {
        if (focusTile == this)
        {
            focusTile = null;
        }

        if (highlightTiles != null)
        {
            for (int i = 0, l=highlightTiles.Count; i<l; i++)
            {
                highlightTiles[i].ClearHUD();
            }

            highlightTiles = null;
        }
    }

    public IEnumerable<Tile> Neighbours
    {
        get
        {
            var coords = coordinates;
            var neighbour = coords + Vector3Int.forward;
            if (Tiles.ContainsKey(neighbour)) yield return Tiles[neighbour];
            neighbour = coords + Vector3Int.back;
            if (Tiles.ContainsKey(neighbour)) yield return Tiles[neighbour];
            neighbour = coords + Vector3Int.left;
            if (Tiles.ContainsKey(neighbour)) yield return Tiles[neighbour];
            neighbour = coords + Vector3Int.right;
            if (Tiles.ContainsKey(neighbour)) yield return Tiles[neighbour];
        }
    }

    public bool ClosestPathTo(
        Tile target, 
        out List<Tile> path, 
        int maxDepth = 20,
        bool allowItermediateOccupation = false,
        Room requireRoom = null)
    {
        if (target == this)
        {
            path = new List<Tile>() { this };
            return maxDepth >= 0;
        }

        List<Tile> visited = new List<Tile>();
        Queue<Tile> seen = new Queue<Tile>();
        seen.Enqueue(this);
        Dictionary<Tile, Tile> inversePath = new Dictionary<Tile, Tile>();
        Dictionary<Tile, int> depths = new Dictionary<Tile, int>();
        depths.Add(this, 0);

        while (seen.Count > 0)
        {
            var current = seen.Dequeue();
            visited.Add(current);
            var depth = depths[current];

            foreach (var neighbour in current.Neighbours)
            {
                if (seen.Contains(neighbour) || visited.Contains(neighbour)) continue;

                inversePath[neighbour] = current;
                var passesRoomCheck = requireRoom == null || requireRoom.HasTile(neighbour);

                if (neighbour == target && passesRoomCheck)
                {
                    var inverse = new List<Tile>() { target };
                    current = target;
                    while (inversePath.ContainsKey(current))
                    {
                        current = inversePath[current];
                        inverse.Add(current);
                    }

                    path = inverse.Reverse<Tile>().ToList();
                    return true;
                } else if (depth < maxDepth - 1)
                {
                    if (passesRoomCheck && (!neighbour.Occupied || allowItermediateOccupation))
                    {
                        seen.Enqueue(neighbour);
                        depths[neighbour] = depth + 1;
                    } else
                    {
                        // We have fully considered it even though we didn't really
                        // visit so that's fine
                        visited.Add(neighbour);
                    }
                }
            }
        }

        path = null;
        return false;
    }
}
