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

    public enum OverWorldPhase { None, RidingUp, JumpingOut, WalkingToStore, WalkingToWell, JumpIn, RideDown };
    OverWorldPhase overWorldPhase = OverWorldPhase.None;

    public void RideUp()
    {
        var cTrans = Camera.main.transform;

        cTrans.position = cameraPosition.position;
        cTrans.rotation = cameraPosition.rotation;

        Bucket.instance.RideUpOverworld();
        overWorldPhase = OverWorldPhase.RidingUp;
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
        overWorldPhase = OverWorldPhase.WalkingToStore;
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
        overWorldPhase = OverWorldPhase.WalkingToWell;
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

        if (overWorldPhase == OverWorldPhase.RidingUp && !Bucket.instance.Riding)
        {
            Bucket.instance.JumpOutOfBucket(
                PlayerController.instance.transform,
                standNextToWellPosition
            );
            overWorldPhase = OverWorldPhase.JumpingOut;
        } else if (overWorldPhase == OverWorldPhase.JumpingOut && !Bucket.instance.Jumping) 
        {
            StartWalkToStore();
        } else if (overWorldPhase == OverWorldPhase.WalkingToStore && !PlayerController.instance.walking & !cameraSliding)
        {
            overWorldPhase = OverWorldPhase.None;
            StoreUI.instance.ShowStore();
        }
        else if (overWorldPhase == OverWorldPhase.WalkingToWell && !PlayerController.instance.walking)
        {
            overWorldPhase = OverWorldPhase.JumpIn;
            Bucket.instance.JumpIntoBucket(PlayerController.instance.transform);
        } else if (overWorldPhase == OverWorldPhase.JumpIn && !Bucket.instance.Jumping)
        {
            Bucket.instance.RideToLevel(levels[currentLevel]);
            overWorldPhase = OverWorldPhase.None;
        }
    }
}
