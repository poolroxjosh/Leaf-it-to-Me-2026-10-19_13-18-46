using UnityEngine;
using TMPro;

public class LeafShopHintUI : MonoBehaviour
{
    [Header("References")]
    public LeafShopUI shopUI;

    [Header("UI")]
    public TMP_Text hintText;

    [Header("Messages")]
    public string closedMessage = "Open Shop";
    public string openMessage = "Close Shop";

    private bool lastShopState;
    private bool initialized;

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        if (shopUI == null || hintText == null)
            return;

        if (!initialized ||
            lastShopState != shopUI.IsOpen)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (shopUI == null || hintText == null)
            return;

        lastShopState = shopUI.IsOpen;
        initialized = true;

        hintText.text = lastShopState
            ? openMessage
            : closedMessage;
    }
}