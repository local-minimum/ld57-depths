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

    [SerializeField]
    List<GameObject> infoHuds = new List<GameObject>();

    [SerializeField]
    bool returnsPlayerToSurface = true;

    public Vector3 BucketPosition =>
        origin.transform.position + Vector3.left * 2f + Vector3.up * 0.25f;
       

    List<Room> rooms
    {
        get
        {
            return GetComponentsInChildren<Room>(true).ToList();
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
            if (returnsPlayerToSurface)
            {
                escapePhase = EscapePhase.WaitingToStart;
            }
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

    [ContextMenu("Jump into bucket")]
    void JumpIntoBucket()
    {
        FightActionUI.ClearAllCooldowns();
        escapePhase = EscapePhase.JumpingIntoBucket;
        HintUI.instance.RemoveText(walkText);
        Debug.Log("Take the bucket up");
        Bucket.instance.JumpIntoBucket(PlayerController.instance.transform);
    }

    public void EnterLevel()
    {
        foreach (var infohud in infoHuds)
        {
            infohud.SetActive(true);
        }

        SyncHud();
        PlayerController.instance.HP = 6;

        Bucket.instance.JumpOutOfBucket(PlayerController.instance.transform, origin.transform);
        PlayerController.instance.currentTile = origin;
        // TODO: Listen to when we're done
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
            escapePhase = EscapePhase.None;
            Debug.Log($"Leaving level: {name}");
            Overworld.instance.RideUp();
        }
    }
}
