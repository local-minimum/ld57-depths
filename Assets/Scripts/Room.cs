using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LMCore.Extensions;

public class Room : MonoBehaviour
{
    public static Room FightRoom;

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

    public void CheckStillDanger()
    {
        if (HasDanger) return;

        // Remove fight state
        if (FightRoom == this)
        {
            FightRoom = null;
        }

        PlayerController.instance.InFight = false;
        DiceHand.instance.HideHand();
        // TODO: Clean up actions
        // TODO: XXX
        Debug.Log($"Room {name} cleared");
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
                    revealed = true;
                }
            } else
            {
                tile.SetPosition((1f - progress) * yAnimationOffset);
            }

        }
    }
}
