using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerAreaAttack : AbsPlayerAttack
{
    [SerializeField]
    bool EightNeighbours;

    [SerializeField]
    bool distantTarget = false;

    public override void Initiate()
    {
        Focus = this;

        if (!distantTarget)
        {
            SetTarget(PlayerController.instance.currentTile);
            return;
        }

        Phase = AttackPhase.PlayerSelectTile;
    }

    List<Tile> targets;
    LookAtCamera cameraControl;
    bool animating;

    [SerializeField]
    float delayBetweenHits = 0.1f;

    [SerializeField]
    float initialDelay = 0.5f;

    [SerializeField]
    float finalDelay = 0.25f;

    [SerializeField]
    bool spinsPlayer = true;

    [SerializeField]
    float rotationSpeed = 180f;

    float yRotation;
    float nextHit;
    int hitIndex;

    public override void SetTarget(Tile tile)
    {
        BeginTextureSwap();

        Phase = AttackPhase.Animating;
        targets = tile.Neighbours.ToList();

        if (tile != PlayerController.instance.currentTile)
        {
            targets.Add(tile);
        }

        if (EightNeighbours)
        {
            foreach (var neighbour in targets.SelectMany(t => t.Neighbours).ToList())
            {
                if (neighbour == tile) continue;
                if (targets.Contains(neighbour)) continue;
                if (tile.coordinates.ChebyshevDistance(neighbour.coordinates) == 1)
                {
                    targets.Add(neighbour);
                }
            }
        }

        targets = targets
            .Where(t => t.occupyingEnemy != null && t.occupyingEnemy.Alive)
            .ToList();

        if (spinsPlayer)
        {
            cameraControl = PlayerController.instance.GetComponentInChildren<LookAtCamera>();
            if (cameraControl != null)
            {
                cameraControl.enabled = false;
            }
        } else
        {
            cameraControl = null;
        }

        Debug.Log($"{name} attacks {tile} with {targets.Count} targets");

        animating = true;
        yRotation = PlayerController.instance.transform.eulerAngles.y;
        hitIndex = 0;
        nextHit = Time.timeSinceLevelLoad + initialDelay;
        invokedDamage = false;
    }

    bool invokedDamage;

    void EndAttack()
    {
        if (cameraControl != null)
        {
            cameraControl.enabled = true;
        }

        EndTextureSwap();

        animating = false;
        Focus = null;

        Action.EndActivation();
    }

    private void Update()
    {
        if (!animating) return;

        if (spinsPlayer)
        {
            yRotation += rotationSpeed * Time.deltaTime;
            yRotation %= 360;
            PlayerController.instance.transform.eulerAngles = new Vector3(0f, yRotation, 0f);
        }

        if (Time.timeSinceLevelLoad < nextHit) return;
        
        if (hitIndex < targets.Count)
        {
            var enemy = targets[hitIndex].occupyingEnemy;
            if (enemy != null)
            {
                if (!invokedDamage)
                {
                    invokedDamage = true;
                    if (enemy.Alive)
                    {
                        enemy.HP -= Action.Value;
                    }
                } else if (enemy.Alive || !CoinFountain.instance.Playing)
                {
                    hitIndex++;
                    nextHit = Time.timeSinceLevelLoad + (hitIndex < targets.Count ? delayBetweenHits : finalDelay);
                    invokedDamage = false;
                }
            } else
            {
                hitIndex++;
            }
        } else
        {
            EndAttack();

        }
    }
}
