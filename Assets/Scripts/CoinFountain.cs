using LMCore.AbstractClasses;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate void CoinFountainEvent();

public class CoinFountain : Singleton<CoinFountain, CoinFountain> 
{
    public static event CoinFountainEvent OnCoin;

    [SerializeField]
    Rigidbody coinPrefab;

    [SerializeField]
    float betweenCoinTime = 0.5f;

    [SerializeField]
    float afterCoinTime = 1f;

    [SerializeField]
    float torqueAxisMax = 10f;

    [SerializeField]
    float upForce = 100f;

    [SerializeField]
    float lateralForceMax = 10f;

    bool playing;
    public bool Playing => playing;

    [ContextMenu("Test")]
    void Test()
    {
        Emit(5);
    }

    int toEmit;
    float nextCoinTime;

    public void Emit(int coins)
    {
        toEmit += coins;
        if (!playing)
        {
            nextCoinTime = Time.timeSinceLevelLoad;
            playing = true;
        }
    }

    List<Rigidbody> instances = new List<Rigidbody>();

    Rigidbody GetCoin()
    {
        var coin = instances.FirstOrDefault(i => !i.gameObject.activeSelf);
        if (coin != null)
        {
            coin.transform.SetParent(transform);
            coin.transform.localPosition = Vector3.zero;
            coin.gameObject.SetActive(true);
            coin.angularVelocity = Vector3.zero;
            coin.linearVelocity = Vector3.zero;
            return coin;
        }

        coin = Instantiate(coinPrefab, transform);
        coin.transform.localPosition = Vector3.zero;
        coin.angularVelocity = Vector3.zero;
        coin.linearVelocity = Vector3.zero;
        instances.Add(coin);
        return coin;
    }

    void ShootCoin(Rigidbody coin)
    {
        coin.AddTorque(
            new Vector3(
                Random.Range(-torqueAxisMax, torqueAxisMax),
                Random.Range(-torqueAxisMax, torqueAxisMax),
                Random.Range(-torqueAxisMax, torqueAxisMax)),
            ForceMode.Impulse);

        coin.AddForce(
            new Vector3(
                Random.Range(-lateralForceMax, lateralForceMax),
                upForce,
                Random.Range(-lateralForceMax, lateralForceMax)),
            ForceMode.Impulse);
    }

    List<Rigidbody> emitted = new List<Rigidbody>();

    private void Update()
    {
        if (!playing || Time.timeSinceLevelLoad < nextCoinTime) return;

        if (toEmit <= 0)
        {
            toEmit = 0;
            playing = false;

            foreach (var em in emitted)
            {
                em.gameObject.SetActive(false);
            }
            emitted.Clear();
            return;
        }

        var coin = GetCoin();
        emitted.Add(coin);
        ShootCoin(coin);
        OnCoin?.Invoke();

        toEmit--;
        nextCoinTime = Time.timeSinceLevelLoad + (toEmit > 0 ? betweenCoinTime : afterCoinTime);
    }
}
