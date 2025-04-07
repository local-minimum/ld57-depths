using System.Collections.Generic;
using TMPro;
using UnityEngine;

public delegate void EnemyEnterTileEvent(Enemy enemy);

public class Enemy : MonoBehaviour
{
    public static event EnemyEnterTileEvent OnEnterTile;

    [SerializeField]
    int startHP;

    [SerializeField]
    int coinReward = 3;

    [SerializeField]
    float coinStartHeight = 1f;

    [SerializeField]
    TextMeshProUGUI hpUI;

    [SerializeField, Header("Graphics")]
    Texture tex;

    [SerializeField]
    Texture texDead;

    [SerializeField]
    Renderer billboard;

    [SerializeField]
    Transform avatar;

    [SerializeField]
    ParticleSystem particles;

    [SerializeField]
    int emitOnHit = 100;

    private int _hp = -1;
    public int HP { 
        get => _hp; 
        set {
            var wasAlive = Alive;

            if (value < _hp && particles)
            {
                particles.Emit(emitOnHit);
            }

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
        billboard.material.SetTexture("_BaseMap", tex);
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
        // Alive is already false due to 0 hp
        currentTile.RemoveDeadEnemy(this);

        billboard.material.SetTexture("_BaseMap", texDead);
        GetComponent<LookAtCamera>().enabled = false;

        CoinFountain.instance.transform.position = transform.position + Vector3.up * coinStartHeight;
        CoinFountain.instance.Emit(coinReward);

        avatar.localEulerAngles = new Vector3(95, 0);
        avatar.localPosition = Vector3.up * 0.15f;
        room.CheckStillDanger();

        StartCoroutine(DelayHide());
    }

    IEnumerator<WaitForSeconds> DelayHide()
    {
        yield return new WaitForSeconds(2f);
        gameObject.SetActive(false);
    }
}
