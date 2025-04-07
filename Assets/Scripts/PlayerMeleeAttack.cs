using System.Linq;
using UnityEngine;

public class PlayerMeleeAttack : AbsPlayerAttack
{
    [SerializeField, Header("Animation")]
    float animationDuration = 1f;

    [SerializeField]
    AnimationCurve rotationOriginLookAhead;

    [SerializeField]
    AnimationCurve rotationAngle;

    [SerializeField, Range(0, 1)]
    float invokeDamageProgress; 

    public override void Initiate()
    {
        Focus = this;
        if (GameSettings.instance.AutoAttack && AttacksDirectNeighbor)
        {
            var tile = PlayerController.instance.currentTile;
            var options = tile
                .Neighbours
                .Where(n => n.occupyingEnemy != null).ToList();
            if (options.Count == 1)
            {
                SetTarget(options[0]);
                return;
            }
        }
        Phase = AttackPhase.PlayerSelectTile;
    }

    float animationStart;
    bool animating;
    bool invokedDamage;
    Tile targetTile;

    public override void SetTarget(Tile tile)
    {
        BeginTextureSwap();

        Phase = AttackPhase.Animating;

        targetTile = tile;
        animating = true;
        invokedDamage = false;
        animationStart = Time.timeSinceLevelLoad;
        PlayerController.instance.transform.LookAt(tile.transform);
    }

    private void Update()
    {
        if (!animating) return;

        var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - animationStart) / animationDuration);

        if (!invokedDamage && progress > invokeDamageProgress)
        {
            var enemy = targetTile.occupyingEnemy;
            if (enemy != null)
            {
                var attackDmg = Action.Value;
                enemy.HP -= attackDmg;
            }

            invokedDamage = true;
        }


        var pTileTransform = PlayerController.instance.currentTile.transform;
        var referencePosition = Vector3.Lerp(
            pTileTransform.position,
            targetTile.transform.position,
            rotationOriginLookAhead.Evaluate(progress));

        var angle = Mathf.Deg2Rad * (180 - rotationAngle.Evaluate(progress));
        var up = Mathf.Sin(angle) * 0.5f;
        var lateral = Mathf.Cos(angle) * 0.5f;
        var pTransform = PlayerController.instance.transform;
        var forward = (targetTile.transform.position - pTransform.position).normalized;

        pTransform.position = referencePosition + forward * lateral + up * Vector3.up;
        pTransform.LookAt(referencePosition);


        if (progress == 1f)
        {
            EndTextureSwap();

            pTransform.position = pTileTransform.position;
            pTileTransform.LookAt(targetTile.transform.position);

            animating = false;
            Focus = null;

            Action.EndActivation();
        }
    }
}
