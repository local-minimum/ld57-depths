using LMCore.AbstractClasses;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Poem : Singleton<Poem, Poem> 
{
    [SerializeField, TextArea]
    List<string> parts = new List<string>();

    [SerializeField]
    TextMeshProUGUI textArea;

    [SerializeField]
    GameObject background;

    int showningPart = -1;
    public int Part => showningPart;

    public void Show(int part)
    {
        if (part < parts.Count && part >= 0)
        {
            showningPart = part;
            textArea.text = parts[part];
            textArea.gameObject.SetActive(true);
            background.SetActive(true);
        } else
        {
            Debug.LogError($"{part} doesn't exist, only 0-{parts.Count - 1}");
        }
    }

    public void Hide(int part)
    {
        if (showningPart == part)
        {
            textArea.gameObject.SetActive(false);
            showningPart = -1;

            background.SetActive(false);
        }
    }

    private void Start()
    {
        if (showningPart == -1)
        {
            textArea.gameObject.SetActive(false);
        }
    }
}
