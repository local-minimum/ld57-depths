using LMCore.AbstractClasses;
using System.Collections.Generic;
using System.Linq;
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
    Transform cameraPositionStore;

    [SerializeField]
    Transform standNextToWellPosition;

    [SerializeField]
    float cameraWalkSlideDuration = 1f;

    [SerializeField]
    List<Tile> pathToStore = new List<Tile>();

    [SerializeField]
    List<Level> levels = new List<Level>();

    int currentLevel;

    public enum RidingPhase { None, RidingUp, JumpingOut, WalkingToStore, WalkingToWell };
    RidingPhase ridingPhase = RidingPhase.None;

    public void RideUp()
    {
        var cTrans = Camera.main.transform;

        cTrans.position = cameraPosition.position;
        cTrans.rotation = cameraPosition.rotation;

        Bucket.instance.RideUpOverworld();
        ridingPhase = RidingPhase.RidingUp;
    }

    bool cameraSliding;
    Transform cameraSlideStart;
    Transform cameraSlideEnd;
    float cameraSlideStartTime;

    void UpdateCameraSlide()
    {
        float progress = Mathf.Clamp01((Time.timeSinceLevelLoad - cameraSlideStartTime) / cameraWalkSlideDuration);
        var cTrans = Camera.main.transform;
        cTrans.position = Vector3.Lerp(cameraSlideStart.position, cameraSlideEnd.position, progress);
        cTrans.rotation = Quaternion.Lerp(cameraSlideStart.rotation, cameraSlideEnd.rotation, progress);

        if (progress == 1)
        {
            cameraSliding = false;
        }

    }

    [ContextMenu("Start Walk to Store")]
    void StartWalkToStore()
    {
        ridingPhase = RidingPhase.WalkingToStore;
        PlayerController.instance.currentTile = pathToStore.First();
        PlayerController.instance.Walk(pathToStore);

        cameraSlideStartTime = Time.timeSinceLevelLoad;
        cameraSlideStart = cameraPosition;
        cameraSlideEnd = cameraPositionStore;
        cameraSliding = true;
    }

    [ContextMenu("Start Walk to Well from Store")]
    void StartWalkToWell()
    {
        ridingPhase = RidingPhase.WalkingToWell;
        PlayerController.instance.currentTile = pathToStore.Last();
        PlayerController.instance.Walk(pathToStore.Reverse<Tile>().ToList());

        cameraSlideStartTime = Time.timeSinceLevelLoad;
        cameraSlideStart = cameraPositionStore;
        cameraSlideEnd = cameraPosition;
        cameraSliding = true;
    }

    public void DiveDeeper()
    {
        currentLevel++;
        StartWalkToWell();
    }

    private void Update()
    {
        if (cameraSliding) UpdateCameraSlide();

        if (ridingPhase == RidingPhase.RidingUp && !Bucket.instance.Riding)
        {
            Bucket.instance.JumpOutOfBucket(
                PlayerController.instance.transform,
                standNextToWellPosition
            );
            ridingPhase = RidingPhase.JumpingOut;
        } else if (ridingPhase == RidingPhase.JumpingOut && !Bucket.instance.Jumping) 
        {
            StartWalkToStore();
        } else if (ridingPhase == RidingPhase.WalkingToStore && !PlayerController.instance.walking & !cameraSliding)
        {
            ridingPhase = RidingPhase.None;
            StoreUI.instance.ShowStore();
        }
        else if (ridingPhase == RidingPhase.WalkingToWell && !PlayerController.instance.walking)
        {
            ridingPhase = RidingPhase.None;
            Debug.Log("Do next level!");
        }
    }
}
