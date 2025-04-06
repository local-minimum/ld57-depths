using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LMCore.Extensions;

public class Room : MonoBehaviour
{
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

    public bool HasDanger => 
        enemies.Any(e => e.Alive);

    public void CheckStillDanger()
    {
        if (HasDanger) return;

        // Remove fight state
        PlayerController.instance.InFight = false;
        DiceHand.instance.HideHand();
        // TODO: Clean up actions
        // TODO: XXX
    }

    private void Start()
    {
        foreach (var tile in tiles)
        {
            tile.gameObject.SetActive(false);
            tile.GetComponent<Collider>().enabled = false;
        }
    }

    [SerializeField]
    float yAnimationOffset = 0.45f;

    [SerializeField]
    float tileDelta = 0.1f;

    [SerializeField]
    float tileAnimDuration = 0.4f;

    bool animating;
    List<Tile> animateInOrder;
    float animStart;

    public void AnimateIn(Vector3Int origin)
    {
        animateInOrder = tiles.OrderBy(t => t.coordinates.ManhattanDistance(origin)).ToList();
        animStart = Time.timeSinceLevelLoad;
        animating = true;
    }


    private void Update()
    {
        if (!animating) return;

        var delta = Time.timeSinceLevelLoad - animStart;
        for (int i = 0, l = animateInOrder.Count; i<l; i++)
        {
            var tile = animateInOrder[i];
            var progress = Mathf.Clamp01((delta - i * tileDelta) / tileAnimDuration);
            if (progress == 0) continue;
            if (progress == 1)
            {
                tile.SyncPosition();
                if (i == l - 1)
                {
                    animating = false;
                }
            } else
            {
                tile.SetPosition((1f - progress) * yAnimationOffset);
            }

        }
    }
}
