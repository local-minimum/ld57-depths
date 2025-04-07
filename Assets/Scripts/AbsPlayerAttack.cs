using UnityEngine;

public enum AttackPhase { NotActive, PlayerSelectTile, Animating };

public abstract class AbsPlayerAttack : MonoBehaviour
{
    public static AbsPlayerAttack Focus { get; set; }

    public AttackPhase Phase { get; set; }

    public int selectTileMinRange = 1;
    public int selectTileMaxRange = 1;

    public bool AttacksDirectNeighbor => selectTileMaxRange == 1 && selectTileMinRange == 1;

    [SerializeField]
    Texture customPlayerTexture;

    protected void BeginTextureSwap()
    {
        if (customPlayerTexture != null)
        {
            PlayerController.instance.SetCustomTexture(customPlayerTexture);
        }
    }

    protected void EndTextureSwap()
    {
        if (customPlayerTexture != null)
        {
            PlayerController.instance.RemoveCustomTexture();
        }
    }

    public abstract void Initiate();
    public abstract void SetTarget(Tile tile);

    protected FightActionUI Action
    {
        get
        {
            return GetComponent<FightActionUI>();
        }
    }
}
