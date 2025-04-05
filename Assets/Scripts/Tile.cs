using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Tile : MonoBehaviour
{
    private static Dictionary<Vector3Int, Tile> Tiles = new Dictionary<Vector3Int, Tile>();

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

    private void OnEnable()
    {
        Tiles.Add(coordinates, this);
    }

    private void OnDisable()
    {
        if (Tiles.TryGetValue(coordinates, out var tile) && tile == this) 
        {
            Tiles.Remove(coordinates);
        }
    }

    public static Tile focusTile { get; private set; }

    private void OnMouseEnter()
    {
        focusTile = this;
    }

    private void OnMouseExit()
    {
        if (focusTile == this)
        {
            focusTile = null;
        }
    }


    IEnumerable<Tile> Neighbours
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

    public bool ClosestPathTo(Tile target, out List<Tile> path, int maxDepth = 20)
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

                if (neighbour == target)
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
                    seen.Enqueue(neighbour);
                    depths[neighbour] = depth + 1;
                }
            }
        }

        path = null;
        return false;
    }
}
