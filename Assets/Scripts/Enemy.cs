using TMPro;
using UnityEngine;

public delegate void EnemyEnterTileEvent(Enemy enemy);

public class Enemy : MonoBehaviour
{
    public static event EnemyEnterTileEvent OnEnterTile;

    [SerializeField]
    int startHP;

    [SerializeField]
    TextMeshProUGUI hpUI;

    private int _hp = -1;
    public int HP { 
        get => _hp; 
        set {
            var wasAlive = Alive;
            _hp = Mathf.Max(0, value);
            var alive = Alive;
            hpUI.text = alive ? $"♥ {_hp}" : "Dead";
            if (wasAlive && !alive)
            {
                Kill();
            }
        }
    }

    public bool Alive => HP != 0;

    Room _room;
    public Room room
    {
        get
        {
            if (_room == null)
            {
                _room = GetComponentInParent<Room>(true);
            }
            return _room;
        }
    }

    EnemyAttack _attack;
    public EnemyAttack Attack
    {
        get
        {
            if (_attack == null)
            {
                _attack = GetComponentInChildren<EnemyAttack>(true);
            }
            return _attack;
        }
    }

    Tile _currentTile;
    public Tile currentTile
    {
        get
        {
            if (_currentTile == null)
            {
                _currentTile = Tile.ClosestTile(transform.position);
                OnEnterTile?.Invoke(this);
            }
            return _currentTile;
        }

        set
        {
            _currentTile = value;
            OnEnterTile?.Invoke(this);
        }
    }

    private void Start()
    {
        InitState();
    }

    [ContextMenu("Sync")]
    void InitState()
    {
        // Get them read in if not already
        var tile = currentTile;
        var room = this.room;

        // Set and sync health
        if (HP < 0)
        {
            HP = startHP;
        }
    }

    void Kill()
    {
        // TODO: Add coins effect
        // TODO: Add death
        room.CheckStillDanger();
    }
}
