
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeafToolSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image background;
    public Image toolIcon;
    public TMP_Text keyText;
    public TMP_Text toolNameText;

    [Header("Colors")]
    public Color selectedColor =
        new Color32(181, 166, 151, 255);

    public Color normalColor =
        new Color32(255, 249, 239, 255);

    private PlayerTool tool;

    public void Initialize(
        PlayerTool toolType,
        string displayName,
        string key,
        Sprite icon)
    {
        tool = toolType;

        if (toolNameText != null)
            toolNameText.text = displayName;

        if (keyText != null)
            keyText.text = key;

        if (toolIcon != null)
        {
            toolIcon.sprite = icon;
            toolIcon.enabled = icon != null;
        }
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
        {
            background.color = selected
                ? selectedColor
                : normalColor;
        }
    }
}