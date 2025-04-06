using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Profiling;
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

    public enum EscapePhase { None, WaitingToStart, Walking, JumpingIntoBucket, RidingUp };
    EscapePhase escapePhase = EscapePhase.None;

    public bool ManagesPlayer => escapePhase != EscapePhase.None;

    public void CheckCleared()
    {
        if (Cleared)
        {
            escapePhase = EscapePhase.WaitingToStart;
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
            escapePhase = EscapePhase.Walking;
            PlayerController.instance.Walk(path);
        } else
        {
            Debug.LogError("Could not find path to bucket");
            JumpIntoBucket();
        }
    }

    void SyncHud()
    {
        var remaining = RemainingRooms;
        roomsHUD.text = remaining > 0 ? $"Rooms: {remaining}" : "Level cleared";
    }

    void JumpIntoBucket()
    {
        escapePhase = EscapePhase.JumpingIntoBucket;
        HintUI.instance.RemoveText(walkText);
        Debug.Log("Take the bucket up");
        Bucket.instance.JumpIntoBucket(PlayerController.instance.transform);
    }


    private void Start()
    {
        SyncHud();
    }

    private void Update()
    {
        if (escapePhase == EscapePhase.WaitingToStart && !CoinFountain.instance.Playing)
        {
            StartWalking();
        } else if (escapePhase == EscapePhase.Walking && !PlayerController.instance.walking)
        {
            JumpIntoBucket();
        } else if (escapePhase == EscapePhase.JumpingIntoBucket && !Bucket.instance.Jumping)
        {
            Bucket.instance.RideUp();
            escapePhase = EscapePhase.RidingUp;
        } else if (escapePhase == EscapePhase.RidingUp && !Bucket.instance.Riding)
        {
            // TODO: Overworld!
            Debug.Log("Do overworld");
            escapePhase = EscapePhase.None;
        }
    }
}
