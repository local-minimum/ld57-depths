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
    Vector3 upRotation;

    [SerializeField]
    int downValue = 6;
    [SerializeField]
    Transform downSentinel;
    [SerializeField]
    Vector3 downRotation;

    [SerializeField]
    int westValue = 2;
    [SerializeField]
    Transform westSentinel;
    [SerializeField]
    Vector3 westRotation;

    [SerializeField]
    int eastValue = 5;
    [SerializeField]
    Transform eastSentinel;
    [SerializeField]
    Vector3 eastRotation;

    [SerializeField]
    int northValue = 3;
    [SerializeField]
    Transform northSentinel;
    [SerializeField]
    Vector3 northRotation;

    [SerializeField]
    int southValue = 4;
    [SerializeField]
    Transform southSentinel;
    [SerializeField]
    Vector3 southRotation;

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
    public bool Rolling => rolling;
    Vector3 rollOrigin;
    List<float> checkpoints;
    float[] relativeParts;
    int lastArc = -1;
    Vector3 spinVector;
    Vector3 rollDirection;
    Vector3 rollUpDirection;

    public void InstaRoll()
    {
        switch (Random.Range(1, 6))
        {
            case 1:
                transform.localEulerAngles = northRotation;
                _Value = northValue;
                break;
            case 2:
                transform.localEulerAngles = southRotation;
                _Value = southValue;
                break;
            case 3:
                transform.localEulerAngles = upRotation;
                _Value = upValue;
                break;
            case 4:
                transform.localEulerAngles = downRotation;
                _Value = downValue;
                break;
            case 5:
                transform.localEulerAngles = westRotation;
                _Value = westValue;
                break;
            case 6:
                transform.localEulerAngles = eastRotation;
                _Value = eastValue;
                break;
        }
    }

    /// <summary>
    /// Rolls in localspace, except for the up 
    /// </summary>
    /// <param name="origin">World space origin</param>
    /// <param name="direction">World space direction</param>
    /// <param name="up">World up</param>
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

    void OldRoll()
    {
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

        transform.position = transform.parent.TransformPoint(rollOrigin + rollDirection * xOffset + rollUpDirection * yOffset);

        if (finalizeRoll) FinalizeRoll();

    }

    int nextSpinTarget;
    float spinStepDuration;
    float spinStepStartTime;
    Quaternion spinStepStartRotation;
    Quaternion spinStepEndRotation;
    bool spinning;

    public void Spin(float speed = 0.3f, int startSide = -1)
    {
        spinStepDuration = speed;
        nextSpinTarget = startSide < 0 ? Random.Range(0, 5) : Mathf.Min(5, startSide);
        spinning = true;
        SetNextSpinTarget();
    }

    public void EndSpin()
    {
        spinning = false;
        transform.localRotation = GetRotation(nextSpinTarget);
    }

    Quaternion GetRotation(int side)
    {
        switch (side)
        {
            case 0:
                return Quaternion.Euler(upRotation);
            case 1:
                return Quaternion.Euler(northRotation);
            case 2:
                return Quaternion.Euler(eastRotation);
            case 3:
                return Quaternion.Euler(downRotation);
            case 4:
                return Quaternion.Euler(westRotation);
            default:
                return Quaternion.Euler(southRotation);
        }
    }

    void SetNextSpinTarget()
    {
        spinStepStartRotation = GetRotation(nextSpinTarget);
        nextSpinTarget = (nextSpinTarget + 1) % 6;
        spinStepEndRotation = GetRotation(nextSpinTarget);
        spinStepStartTime = Time.timeSinceLevelLoad;
        // Debug.Log($"Spinning from {spinStepStartRotation} to {spinStepEndRotation}");
    }

    private void Update()
    {
        if (rolling) OldRoll();

        if (spinning)
        {
            var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - spinStepStartTime) / spinStepDuration);
            transform.rotation = Quaternion.Lerp(spinStepStartRotation, spinStepEndRotation, progress);
            if (progress == 1)
            {
                SetNextSpinTarget();
            }
        }
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
