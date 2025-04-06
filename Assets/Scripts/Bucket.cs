using LMCore.AbstractClasses;
using UnityEngine;

public class Bucket : Singleton<Bucket, Bucket> 
{
    [SerializeField]
    Transform bucketRoot;

    [SerializeField]
    Transform inBucketPosition;

    [SerializeField, Header("Jumping")]
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
    public bool Jumping => jumpingIn;

    [SerializeField, Header("Ride Up")]
    float rideDuration = 2f;

    [SerializeField]
    AnimationCurve rideUpEasing;

    bool ridingUp;
    public bool Riding => ridingUp;
    float rideStart;
    Vector3 rideFrom;

    [ContextMenu("Jump Player Into Bucket")]
    void JumpPlayerInto()
    {
        JumpIntoBucket(PlayerController.instance.transform);
    }

    public void JumpIntoBucket(Transform jumper)
    {
        jumpStart = Time.timeSinceLevelLoad;

        this.jumper = jumper;
        jumper.transform.SetParent(transform);

        var lookTarget = inBucketPosition.position;
        lookTarget.y = jumper.position.y;
        jumper.LookAt(lookTarget);

        jumpFrom = jumper.transform.position;
        jumpingIn = true;
    }

    [ContextMenu("Ride up")]
    public void RideUp()
    {
        rideFrom = bucketRoot.position;
        rideStart = Time.timeSinceLevelLoad;
        ridingUp = true;
    }

    private void Update()
    {
        if (jumpingIn) ProgressJumpIn();
        if (ridingUp) ProgressRideUp();
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

    void ProgressRideUp()
    {
        var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - rideStart) / rideDuration);
        bucketRoot.position = rideFrom + Vector3.up * rideUpEasing.Evaluate(progress);

        if (progress == 1)
        {
            ridingUp = false;
        }
    }
}
