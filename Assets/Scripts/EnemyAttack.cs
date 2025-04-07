using LMCore.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField]
    int numberOfHits = 1;

    [SerializeField]
    int walkDistance = 7;

    [SerializeField, Header("Walk Animation")]
    float walkSpeed = 0.7f;

    [SerializeField]
    AnimationCurve translationEasing;

    [SerializeField]
    AnimationCurve verticalTranslationEasing;

    [SerializeField]
    float verticalTranslationHeight;

    [SerializeField, Header("Attack Animation")]
    float attackAnimationDuration = 1f;

    [SerializeField]
    AnimationCurve attackRotationOriginLookAhead;

    [SerializeField]
    AnimationCurve attackRotationAngle;

    [SerializeField, Range(0, 1)]
    float attackInvokeDamageProgress;

    Enemy _enemy;
    Enemy enemy
    {
        get
        {
            if (_enemy == null)
            {
                _enemy = GetComponentInParent<Enemy>(true);
            }
            return _enemy;
        }
    }

    List<Tile> walkPath;
    bool walking;
    int walkIndex;
    float walkStepStart;

    public void Perform()
    {
        Debug.Log($"{name} starts its attack");
        Attacked = false;
        Completed = false;
        var myTile = enemy.currentTile;
        var target = PlayerController.instance.currentTile;
        if (myTile.ClosestPathTo(target, out var path, requireRoom: enemy.room))
        {
            walkPath = path.SkipLast(1).Take(walkDistance + 1).ToList();
            walking = walkPath.Count > 1;
            walkIndex = 0;
            walkStepStart = Time.timeSinceLevelLoad;
            if (!walking)
            {
                Debug.Log($"{name} is next to player it seems. Original path was {path.Count} long");
            }
            Debug.Log($"{name} will start walking on {walkPath.Count} length path (max steps: {walkDistance})");
        } else
        {
            walking = false;
            Debug.LogWarning($"{name} found no path to player");
        }

        if (!walking) AttackPlayer();
    }

    public bool Completed { get; set; }
    public bool Attacked { get; private set; }

    bool attacking;
    bool invokedDamage;
    float animationStart;
    int attackIdx;

    void AttackPlayer()
    {
        attacking = enemy.currentTile.coordinates
            .ManhattanDistance(PlayerController.instance.currentTile.coordinates) == 1;

        attackIdx = 0;

        enemy.transform.LookAt(PlayerController.instance.transform);

        if (attacking)
        {
            Debug.Log($"{name} will start its actual first attack");
            StartAttack();
        } else
        {
            Debug.LogWarning($"{name} not next to player, can't attack");
            Completed = true;
        }
    }

    void StartAttack()
    {
        invokedDamage = false;
        animationStart = Time.timeSinceLevelLoad;
    }

    void LookAtNextTarget()
    {
        var start = walkPath[walkIndex];
        var end = walkPath[walkIndex + 1];
        var direction = end.transform.position - start.transform.position;
        direction.y = 0;
        enemy.transform.LookAt(transform.position + direction);
    }

    void EaseWalkStep()
    {
        var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - walkStepStart) / walkSpeed);

        if (progress == 1)
        {
            enemy.currentTile = walkPath[walkIndex + 1];
            enemy.transform.position = enemy.currentTile.transform.position;

            walkStepStart = Time.timeSinceLevelLoad;
            walkIndex++;
            if (walkIndex >= walkPath.Count - 1)
            {
                walking = false;
                AttackPlayer();
            } else
            {
                LookAtNextTarget();
            }
        } else
        {
            var start = walkPath[walkIndex];
            var end = walkPath[walkIndex + 1];
            enemy.transform.position = Vector3.Lerp(start.transform.position, end.transform.position, translationEasing.Evaluate(progress)) +
                Vector3.up * verticalTranslationHeight * verticalTranslationEasing.Evaluate(progress);
        }
    }

    void AnimateAttack()
    {
        var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - animationStart) / attackAnimationDuration);
        var targetTile = PlayerController.instance.currentTile;

        if (!invokedDamage && progress > attackInvokeDamageProgress)
        {
            PlayerController.instance.HP--;
            invokedDamage = true;
            Attacked = true;
        }

        var eTileTransform = enemy.currentTile.transform;

        var referencePosition = Vector3.Lerp(
            eTileTransform.position,
            targetTile.transform.position,
            attackRotationOriginLookAhead.Evaluate(progress));

        var angle = Mathf.Deg2Rad * (180 - attackRotationAngle.Evaluate(progress));
        var up = Mathf.Sin(angle) * 0.5f;
        var lateral = Mathf.Cos(angle) * 0.5f;
        var eTransform = enemy.transform;
        var forward = (targetTile.transform.position - eTransform.position).normalized;

        eTransform.position = referencePosition + forward * lateral + up * Vector3.up;
        eTransform.LookAt(referencePosition);


        if (progress == 1f)
        {
            eTransform.position = eTileTransform.position;
            eTileTransform.LookAt(targetTile.transform.position);

            attackIdx++;

            if (attackIdx >= numberOfHits)
            {
                AfterAttacks();
            } else
            {
                StartAttack();
            }
        }

    }

    void AfterAttacks()
    {
        Debug.Log($"{name} has completed {attackIdx} attacks and player now has health {PlayerController.instance.HP}");
        attacking = false;
        Completed = true;
    }

    private void Update()
    {
        if (walking) EaseWalkStep();
        if (attacking) AnimateAttack();
    }
}
