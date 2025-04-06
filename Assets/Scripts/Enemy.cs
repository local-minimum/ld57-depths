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
            _hp = Mathf.Max(0, value);
            hpUI.text = _hp.ToString();
        }
    }

    public bool Alive => HP != 0;

    Room _room;
    Room room
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

        private set
        {
            _currentTile = value;
            OnEnterTile?.Invoke(this);
        }
    }

    private void Start()
    {
        InitState();
    }

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
