
using UnityEngine;
using TMPro;

public class LeafInventoryUI : MonoBehaviour
{
    [Header("References")]
    public LeafInventory inventory;
    public TMP_Text counterText;

    private void OnEnable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += UpdateCounter;

        UpdateCounter();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= UpdateCounter;
    }

    private void UpdateCounter()
    {
        if (inventory == null || counterText == null)
            return;

        counterText.text =
            $"{inventory.CurrentLeaves + inventory.PendingLeaves}/{inventory.maxCapacity}";
    }
}
