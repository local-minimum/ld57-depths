using LMCore.AbstractClasses;
using UnityEngine;

public class Bucket : Singleton<Bucket, Bucket> 
{
    [SerializeField]
    Transform bucketRoot;

    [SerializeField]
    Transform inBucketPosition;

    [SerializeField, Header("Jump In")]
    float jumpHeight = 1f;

    [SerializeField]
    AnimationCurve jumpHeightEasing;

    [SerializeField]
    AnimationCurve jumpEasing;

    [SerializeField]
    float jumpDuration = 0.4f;

    float jumpStart;
    Transform jumper;
    Vector3 jumpFrom;

    bool jumpingIn;
    public bool Jumping => jumpingIn || jumpingOut;

    [SerializeField, Header("Jump out")]
    float jumpOutHeight = 1f;

    [SerializeField]
    AnimationCurve jumpOutHeightEasing;

    [SerializeField]
    AnimationCurve jumpOutEasing;

    [SerializeField]
    float jumpOutDuration = 0.4f;

    Transform jumpTo;
    bool jumpingOut;

    [SerializeField, Header("Ride Up")]
    float rideDuration = 2f;

    [SerializeField]
    AnimationCurve rideUpEasing;

    bool ridingUp;
    public bool Riding => ridingUp || ridingUpOverworld;
    float rideStart;
    Vector3 rideFrom;

    [SerializeField, Header("Ride Up Overworld")]
    float rideUpOverworldDuration = 1f;
    [SerializeField]
    AnimationCurve rideUpOverworldEasing;
    bool ridingUpOverworld;


    [ContextMenu("Jump Player Into Bucket")]
    void JumpPlayerInto()
    {
        JumpIntoBucket(PlayerController.instance.transform);
    }

    public void JumpIntoBucket(Transform jumper)
    {
        jumpStart = Time.timeSinceLevelLoad;

        this.jumper = jumper;
        jumper.SetParent(transform);

        var lookTarget = inBucketPosition.position;
        lookTarget.y = jumper.position.y;
        jumper.LookAt(lookTarget);

        jumpFrom = jumper.transform.position;
        jumpingIn = true;
        jumpingOut = false;
    }

    public void JumpOutOfBucket(Transform jumper, Transform target)
    {
        jumpStart = Time.timeSinceLevelLoad;
        this.jumper = jumper;
        jumper.SetParent(target);

        var lookTarget = target.position;
        lookTarget.y = jumper.position.y;
        jumper.LookAt(lookTarget);

        jumpTo = target;
        jumpingIn = false;
        jumpingOut = true;
    }

    [ContextMenu("Ride up")]
    public void RideUp()
    {
        rideFrom = bucketRoot.position;
        rideStart = Time.timeSinceLevelLoad;
        ridingUp = true;
        ridingUpOverworld = false;
    }

    public void RideUpOverworld()
    {
        rideStart = Time.timeSinceLevelLoad;
        ridingUpOverworld = true;
        ridingUp = false;
    }

    private void Update()
    {
        if (jumpingIn)
        {
            ProgressJumpIn();
        } else if (jumpingOut)
        {
            ProgressJumpOut();
        }

        if (ridingUp)
        {
            ProgressRideUp();
        }
        else if (ridingUpOverworld)
        {
            ProgressRideUpOverworld();
        }
    }

    void ProgressJumpIn()
    {
        var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - jumpStart) / jumpDuration);

        jumper.transform.position =
            Vector3.Lerp(jumpFrom, inBucketPosition.position, jumpEasing.Evaluate(progress)) +
            Vector3.up * jumpHeightEasing.Evaluate(progress) * jumpHeight;

        if (progress == 1)
        {
            jumpingIn = false;
        }
    }

    void ProgressJumpOut()
    {
        var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - jumpStart) / jumpDuration);

        jumper.transform.position =
            Vector3.Lerp(inBucketPosition.position, jumpTo.position, jumpOutEasing.Evaluate(progress)) +
            Vector3.up * jumpOutHeightEasing.Evaluate(progress) * jumpOutHeight;

        if (progress == 1)
        {
            jumpingOut = false;
        }
    }

    void ProgressRideUp()
    {
        var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - rideStart) / rideDuration);
        bucketRoot.position = rideFrom + Vector3.up * rideUpEasing.Evaluate(progress);

        if (progress == 1)
        {
            ridingUp = false;
        }
    }

    void ProgressRideUpOverworld()
    {
        var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - rideStart) / rideUpOverworldDuration);
        var offset = bucketRoot.position - transform.position;

        bucketRoot.position = 
            Overworld.instance.BucketRestingPosition + 
            offset + 
            Vector3.up * rideUpOverworldEasing.Evaluate(progress);

        if (progress == 1)
        {
            ridingUpOverworld = false;
        }
    }
}
