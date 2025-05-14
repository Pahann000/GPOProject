using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private Text tooltipText;

    public void SetText(string text)
    {
        tooltipText.text = text;
        tooltipPanel.SetActive(true);
    }

    public void Hide()
    {
        tooltipPanel.SetActive(false);
    }
}