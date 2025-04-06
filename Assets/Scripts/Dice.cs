using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate void DiceRollEvent(Dice dice);

public class Dice : MonoBehaviour
{
    public static DiceRollEvent OnRoll;

    [SerializeField, Header("Dice properties")]
    int upValue = 1;
    [SerializeField]
    Transform upSentinel;

    [SerializeField]
    int downValue = 6;
    [SerializeField]
    Transform downSentinel;

    [SerializeField]
    int westValue = 2;
    [SerializeField]
    Transform westSentinel;

    [SerializeField]
    int eastValue = 5;
    [SerializeField]
    Transform eastSentinel;

    [SerializeField]
    int northValue = 3;
    [SerializeField]
    Transform northSentinel;

    [SerializeField]
    int southValue = 4;
    [SerializeField]
    Transform southSentinel;

    [SerializeField, Header("Roll")]
    int bounces = 3;

    [SerializeField]
    Vector2 arcScale = new Vector2(0.5f, 1f);

    [SerializeField]
    float arcSizeDecay = 0.5f;

    [SerializeField]
    float rollDuration = 1.5f;

    [SerializeField]
    float spinFactor = 100f;

    [SerializeField]
    Color dieColor;
    public Color DieColor => dieColor;

    [SerializeField]
    Color dieTextColor;
    public Color DieTextColor => dieTextColor;

    IEnumerable<Transform> Sentinels
    {
        get
        {
            yield return upSentinel;
            yield return downSentinel;
            yield return westSentinel;
            yield return eastSentinel;
            yield return northSentinel;
            yield return southSentinel;
        }
    }

    int CalculateValue()
    {

            var highest = Sentinels.OrderBy(s => s.transform.position.y).Last();

            if (highest == upSentinel) return upValue;
            if (highest == downSentinel) return downValue;
            if (highest == westSentinel) return westValue;
            if (highest == eastSentinel) return eastValue;
            if (highest == northSentinel) return northValue;
            return southValue;
    }

    void SetValue()
    {
        _Value = CalculateValue();
    }

    void RemoveValue()
    {
        _Value = 0;
    }

    public bool Used { get; set; }

    private int _Value;
    public int Value => _Value;

    [ContextMenu("Roll")]
    public void Roll() => Roll(Vector3.zero, Vector3.right, Vector3.up);

    bool rolling;
    Vector3 rollOrigin;
    List<float> checkpoints;
    float[] relativeParts;
    int lastArc = -1;
    Vector3 spinVector;
    Vector3 rollDirection;
    Vector3 rollUpDirection;

    public void Roll(Vector3 origin, Vector3 direction, Vector3 up)
    {
        if (rolling) return;

        RemoveValue();

        rollDirection = direction;
        rollUpDirection = up;

        rolling = true;
        rollOrigin = origin;

        float t0 = Time.timeSinceLevelLoad;
        checkpoints = new List<float>() { t0 } ;

        relativeParts = new float[bounces];
        relativeParts[0] = 1f;

        float partSize = 1f;
        float totalParts = 1f;
        for (int i = 1; i<relativeParts.Length; i++)
        {
            partSize *= arcSizeDecay;
            relativeParts[i] = partSize;
            totalParts += partSize;
        }

        float timePerPart = rollDuration / totalParts;
        float time = t0;
        for (int i = 0; i<relativeParts.Length; i++)
        {
            time += timePerPart * relativeParts[i];
            checkpoints.Add(time);
        }

        lastArc = -1;
    }

    private void Update()
    {
        if (!rolling) return;

        var t = Time.timeSinceLevelLoad;
        int arcIdx = checkpoints.FindIndex(c => c > t) - 1;
        float arcProgress = 0;
        bool finalizeRoll = false;

        if (arcIdx < 0)
        {
            arcProgress = 1f;
            arcIdx = relativeParts.Length - 1;
            finalizeRoll = true;
        } else
        {

            arcProgress = Mathf.Clamp01((t - checkpoints[arcIdx]) / (checkpoints[arcIdx + 1] - checkpoints[arcIdx]));
        }

        if (arcIdx > lastArc)
        {
            SetSpinn();
            lastArc = arcIdx;
        }

        var e = transform.eulerAngles;
        e += spinVector * Time.deltaTime * spinFactor;
        transform.eulerAngles = e;

        // Debug.Log($"{arcIdx}: {arcProgress} / {relativeParts[arcIdx]}");

        float yOffset = relativeParts[arcIdx] * 
            arcScale.y * 
            Mathf.Sin(arcIdx == 0 ? Mathf.Lerp(Mathf.PI / 2, 0, arcProgress) : Mathf.Lerp(Mathf.PI, 0, arcProgress));

        float xOffset = 0;
        for (int i=0; i<arcIdx;i++)
        {
            if (i == 0)
            {
                xOffset += arcScale.x * relativeParts[arcIdx];
            } else
            {
                xOffset += arcScale.x * 3 * relativeParts[arcIdx];
            }
        }

        if (arcIdx == 0)
        {
            xOffset += relativeParts[arcIdx] * arcScale.x * 0.5f * Mathf.Cos(Mathf.Lerp(Mathf.PI / 2, 0, arcProgress));
        } else
        {
            xOffset += relativeParts[arcIdx] * arcScale.x * 0.5f * (1f + Mathf.Cos(Mathf.Lerp(Mathf.PI, 0, arcProgress)));
        }

        // Debug.Log(xOffset);

        transform.position = rollOrigin + rollDirection * xOffset + rollUpDirection * yOffset;

        if (finalizeRoll) FinalizeRoll();
    }

    void SetSpinn()
    {
        spinVector = new Vector3(Random.Range(-90, 90), Random.Range(-90, 90), Random.Range(-90, 90));
        float v = Random.value;
        if (v < 0.45f)
        {
            spinVector.x = 0f;
        }
        else if (v < 0.66f)
        {
            spinVector.y = 0f;
        }
        else
        {
            spinVector.z = 0f;
        }
        spinVector.Normalize();
    }

    void FinalizeRoll()
    {
        rolling = false;
        var euler = transform.eulerAngles;
        euler.x = Mathf.Round(euler.x / 90f) * 90f;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        euler.z = Mathf.Round(euler.z / 90f) * 90f;
        transform.eulerAngles = euler;

        SetValue();
        OnRoll?.Invoke(this);
    }

    [ContextMenu("Info")]
    void Info()
    {
        Debug.Log($"{name}: {Value}");
    }

    public static Dice Focus { get; set; }

    private void OnMouseEnter()
    {
        if (!rolling)
        {
            Focus = this;
        }
        
    }

    private void OnMouseExit()
    {
        if (Focus == this)
        {
            Focus = null;
        }
        
    }
}
