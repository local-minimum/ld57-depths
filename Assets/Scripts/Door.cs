using System.Collections.Generic;
using UnityEngine;

public delegate void DoorBreachEvent(Door door);

public class Door : MonoBehaviour
{
    public static event DoorBreachEvent OnBreach;

    [SerializeField]
    Room leadsTo;

    [SerializeField]
    Tile leftTile;
    [SerializeField]
    Tile rightTile;

    public bool NeighboursTile(Tile tile) => 
        tile == leftTile || tile == rightTile;

    [SerializeField]
    GameObject solidDoor;

    [SerializeField]
    List<Rigidbody> doorFragments = new List<Rigidbody>();

    [SerializeField]
    float explosionHeight = 1f;
    [SerializeField]
    float explosionSize = 2f;
    [SerializeField]
    float explosionForce = 1000f;
    [SerializeField]
    float explosionDuration = 1f;

    [SerializeField]
    Collider focusCollider;

    [SerializeField]
    string breachHint = "Click <color=\"red\">door</color> to breach it!";
    [SerializeField]
    string notInFightHint = "Cannot breach doors while in a fight!";
    public bool LeadsToDanger => leadsTo.HasDanger;

    bool PlayerOnBreachTile => PlayerController.instance.currentTile == leftTile ||
        PlayerController.instance.currentTile == rightTile;

    private void Awake()
    {
        foreach (var fragment in doorFragments)
        {
            fragment.gameObject.SetActive(false);
        }

        if (!PlayerOnBreachTile)
        { 
            focusCollider.enabled = false;
        }
    }

    private void OnEnable()
    {
        PlayerController.OnEnterTile += PlayerController_OnEnterTile;
    }

    private void OnDisable()
    {
        PlayerController.OnEnterTile -= PlayerController_OnEnterTile;
    }

    bool canBreach;
    string lastHint;
    private void PlayerController_OnEnterTile(PlayerController instance)
    {
        canBreach = false;

        if (!breached && PlayerOnBreachTile)
        {
            if (instance.InFight)
            {
                lastHint = notInFightHint;
            } else if (!breached)
            {
                lastHint = breachHint;
                canBreach = true;
            } else
            {
                lastHint = null;
            }

            if (!string.IsNullOrEmpty(lastHint))
            {
                HintUI.instance.SetText(lastHint);
            }
        } else if (!string.IsNullOrEmpty(lastHint))
        {
            HintUI.instance.RemoveText(lastHint);
            lastHint = null;
        }

        focusCollider.enabled = canBreach;
    }

    public static Door FocusDoor { get; private set; }

    private void OnMouseEnter()
    {
        if (canBreach)
        {
            FocusDoor = this;
        }
    }

    private void OnMouseExit()
    {
        if (FocusDoor == this)
        {
            FocusDoor = null;
        }
        
    }

    bool breached;
    bool exploding = false;
    float explodingEnd;

    [ContextMenu("Breach")]
    public void Breach()
    {
        if (breached || !PlayerOnBreachTile) return;

        if (!string.IsNullOrEmpty(lastHint))
        {
            HintUI.instance.RemoveText(lastHint);
            lastHint = null;
        }

        var breachTile = PlayerController.instance.currentTile == leftTile ? leftTile : rightTile;

        leadsTo.AnimateIn(breachTile.coordinates);
        solidDoor.SetActive(false);

        var explosionSource = breachTile.transform.position + Vector3.up * explosionHeight;

        foreach (var fragment in doorFragments)
        {
            fragment.gameObject.SetActive(true);
            fragment.AddExplosionForce(explosionForce, explosionSource, explosionSize);
        }

        explodingEnd = Time.timeSinceLevelLoad + explosionDuration;
        exploding = true;
        Time.timeScale = 0.1f;

        breached = true;
    }

    private void Update()
    {
        if (exploding)
        {
            if (Time.timeScale < 1f)
            {
                Time.timeScale = Mathf.Min(1, Time.timeScale + Time.deltaTime);
            }

            if (Time.timeSinceLevelLoad > explodingEnd)
            {
                foreach (var fragment in doorFragments)
                {
                    fragment.gameObject.SetActive(false);
                }
                Time.timeScale = 1f;
                exploding = false;

                var roomTile = PlayerController.instance.currentTile == leftTile ? rightTile : leftTile;

                PlayerController.instance
                    .Walk(new List<Tile>() { PlayerController.instance.currentTile, roomTile });

                OnBreach?.Invoke(this);
            }
        }
    }

    Vector3 ReferencePosition => 
        Vector3.Lerp(leftTile.transform.position, rightTile.transform.position, 0.5f);

    bool positionSynced;

    public void HideIfNotSynced()
    {
        if (positionSynced) return;
        gameObject.SetActive(false);
    }

    public void SyncPosition()
    {
        transform.position = ReferencePosition;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        positionSynced = true;
    }

    public void SetPosition(float yOffset)
    {
        if (positionSynced) return;

        transform.position = ReferencePosition + Vector3.up * yOffset;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }
}
