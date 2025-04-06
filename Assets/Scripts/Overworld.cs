using LMCore.AbstractClasses;
using UnityEngine;

public class Overworld : Singleton<Overworld, Overworld> 
{
    [SerializeField]
    Transform bucketRestingPosition;

    public Vector3 BucketRestingPosition => 
        bucketRestingPosition.position;

    [SerializeField]
    Transform cameraPosition;

    [SerializeField]
    Transform standNextToWellPosition;

    public enum RidingPhase { None, RidingUp, JumpingOut };
    RidingPhase ridingPhase = RidingPhase.None;

    public void RideUp()
    {
        var cTrans = Camera.main.transform;

        cTrans.position = cameraPosition.position;
        cTrans.rotation = cameraPosition.rotation;

        Bucket.instance.RideUpOverworld();
        ridingPhase = RidingPhase.RidingUp;
    }

    private void Update()
    {
        if (ridingPhase == RidingPhase.RidingUp && !Bucket.instance.Riding)
        {
            Bucket.instance.JumpOutOfBucket(
                PlayerController.instance.transform,
                standNextToWellPosition
            );
            ridingPhase = RidingPhase.JumpingOut;
        } else if (ridingPhase == RidingPhase.JumpingOut && !Bucket.instance.Jumping) 
        {
            // TODO: Store time!
            Debug.Log("Time for store!");
            ridingPhase = RidingPhase.None;
        }
    }
}
