
using UnityEngine;
using UnityEngine.UI;

public class LeafCrosshairUI : MonoBehaviour
{
    [Header("References")]
    public LeafPickupController pickupController;
    public LeafWheelbarrowInteraction wheelbarrowInteraction;
    public LeafInventory inventory;
    public Image crosshairImage;

    [Header("Crosshair Colors")]
    public Color normalColor = Color.white;
    public Color leafTargetColor = Color.yellow;
    public Color fullBagColor = Color.red;
    public Color wheelbarrowColor = Color.green;

    private void Update()
    {
        if (crosshairImage == null ||
            pickupController == null ||
            inventory == null)
            return;

        // Hide crosshair while the cursor is unlocked,
        // including when the shop is open.
        bool gameplayActive =
            Cursor.lockState == CursorLockMode.Locked;

        crosshairImage.enabled = gameplayActive;

        if (!gameplayActive)
            return;

        bool bagFull = inventory.IsFull;

        bool carryingLeaves =
            inventory.CurrentLeaves > 0;

        bool targetingWheelbarrow =
            wheelbarrowInteraction != null &&
            wheelbarrowInteraction.IsAimingAtWheelbarrow();

        bool targetingLeaf =
            pickupController.TargetLeafId >= 0;

        // Highest priority:
        // Wheelbarrow is usable when carrying leaves.
        if (targetingWheelbarrow && carryingLeaves)
        {
            crosshairImage.color = wheelbarrowColor;
        }
        // Bag is full, so no more leaves can be picked up.
        else if (bagFull && targetingLeaf)
        {
            crosshairImage.color = fullBagColor;
        }
        // A leaf is targeted and can be picked up.
        else if (targetingLeaf)
        {
            crosshairImage.color = leafTargetColor;
        }
        // Nothing interactable is targeted.
        else
        {
            crosshairImage.color = normalColor;
        }
    }
}
