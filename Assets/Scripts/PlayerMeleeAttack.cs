using UnityEngine;

public class PlayerMeleeAttack : AbsPlayerAttack
{
    public override void Initiate()
    {
        Phase = AttackPhase.PlayerSelectTile;
        Focus = this;
    }

    public override void SetTarget(Tile tile)
    {
        // TODO: Stuff
        Phase = AttackPhase.Animating;
        var value = Action.Value;
        var enemy = tile.occupyingEnemy;

        if (enemy == null)
        {
            // TODO: Handle miss
            Focus = null;
            Action.EndActivation();
        } else
        {
            enemy.HP -= value;
            // TODO: Add juice
            Focus = null;
            Action.EndActivation();
        }
    }
}
