using System.Collections.Generic;
using UnityEngine;

public delegate void DoorBreachEvent(Door door);

public class Door : MonoBehaviour
{
    public static event DoorBreachEvent OnBreach;

    [SerializeField]
    Room leadsTo;

    [SerializeField]
    Tile breachOrigin;

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

    public bool LeadsToDanger => leadsTo.HasDanger;

    private void Awake()
    {
        foreach (var fragment in doorFragments)
        {
            fragment.gameObject.SetActive(false);
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

    private void PlayerController_OnEnterTile(PlayerController instance)
    {
        if (!breached && instance.currentTile == breachOrigin)
        {
            Breach();
        }
    }

    bool breached;
    bool exploding = false;
    float explodingEnd;

    [ContextMenu("Breach")]
    public void Breach()
    {
        if (breached) return;

        leadsTo.AnimateIn(breachOrigin.coordinates);
        solidDoor.SetActive(false);

        var explosionSource = breachOrigin.transform.position + Vector3.up * explosionHeight;

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

                OnBreach?.Invoke(this);
            }
        }
    }
}
