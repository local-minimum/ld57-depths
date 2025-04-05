using LMCore.AbstractClasses;
using TMPro;
using UnityEngine;

public class HintUI : Singleton<HintUI, HintUI> 
{
    [SerializeField]
    TextMeshProUGUI Text;

    string lastText;

    public void SetText(string text)
    {

        lastText = text;
        Text.text = text;
        Text.enabled = true;
    }

    public void HideText(string text)
    {
        if (lastText == text)
        {
            Text.text = null;
            Text.enabled = false;
            lastText = null;
        }
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(lastText))
        {
            Text.text = null;
            Text.enabled = false;
            lastText = null;
        }
    }
}
