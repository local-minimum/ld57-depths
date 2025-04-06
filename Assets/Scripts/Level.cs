using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Level : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI roomsHUD;

    [SerializeField]
    Tile origin;

    List<Room> _rooms;
    List<Room> rooms
    {
        get
        {
            if (_rooms == null)
            {
                _rooms = GetComponentsInChildren<Room>(true).ToList();
            }
            return _rooms;
        }
    }

    bool Cleared => rooms.All(r => r.Revealed && !r.HasDanger);

    int RemainingRooms => rooms.Count(r => !r.Revealed && !r.HasTile(origin));

    bool walkingPlayer;
    bool shouldWalkPlayer;
    bool escapeLevel;

    public bool ManagesPlayer => walkingPlayer || shouldWalkPlayer || escapeLevel;

    public void CheckCleared()
    {
        if (Cleared)
        {
            shouldWalkPlayer = true;
            escapeLevel = true;
            SyncHud();
        }
    }

    string walkText = "Returning to entrance";
    void StartWalking()
    {
        HintUI.instance.SetText(walkText);
        var start = PlayerController.instance.currentTile;
        if (start.ClosestPathTo(origin, out var path, maxDepth: 1000))
        {
            walkingPlayer = true;
            PlayerController.instance.Walk(path);
        } else
        {
            Debug.LogError("Could not find path to bucket");
            EscapeToSurface();
        }
        shouldWalkPlayer = false;
    }

    void SyncHud()
    {
        var remaining = RemainingRooms;
        roomsHUD.text = remaining > 0 ? $"Rooms: {remaining}" : "Level cleared";
    }

    void EscapeToSurface()
    {
        HintUI.instance.RemoveText(walkText);
        Debug.Log("Take the bucket up");
        escapeLevel = false;
    }


    private void Start()
    {
        SyncHud();
    }

    private void Update()
    {
        if (shouldWalkPlayer && !CoinFountain.instance.Playing)
        {
            StartWalking();
        }

        if (walkingPlayer && escapeLevel && !PlayerController.instance.walking)
        {
            EscapeToSurface();
        }
    }
}
