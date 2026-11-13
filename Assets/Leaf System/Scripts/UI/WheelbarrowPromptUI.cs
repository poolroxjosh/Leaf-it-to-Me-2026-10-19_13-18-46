
using UnityEngine;
using TMPro;

public class WheelbarrowPromptUI : MonoBehaviour
{
    [Header("References")]
    public WheelbarrowMovementController movementController;
    public LeafShopUI shopUI;
    public Camera playerCamera;
    public TMP_Text promptText;

    [Header("Position")]
    public Transform followTarget;
    public Vector3 worldOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Text")]
    public string message = "[E] Push Wheelbarrow";

    private void Awake()
    {
        if (promptText != null)
        {
            promptText.text = message;
            promptText.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (movementController == null ||
            playerCamera == null ||
            promptText == null)
            return;

        // Never show the prompt while pushing.
        if (movementController.IsPushing)
        {
            HidePrompt();
            return;
        }

        // Hide while the shop is open.
        if (shopUI != null && shopUI.IsOpen)
        {
            HidePrompt();
            return;
        }

        // Only show when the player can actually
        // grab the wheelbarrow.
        bool shouldShow =
            movementController.CanGrabWheelbarrow();

        if (promptText.gameObject.activeSelf != shouldShow)
            promptText.gameObject.SetActive(shouldShow);

        if (!shouldShow)
            return;

        // Follow the wheelbarrow's handles.
        if (followTarget != null)
        {
            transform.position =
                followTarget.position + worldOffset;
        }

        // Billboard: match the camera's orientation.
        transform.rotation =
            playerCamera.transform.rotation;
    }

    private void HidePrompt()
    {
        if (promptText != null &&
            promptText.gameObject.activeSelf)
        {
            promptText.gameObject.SetActive(false);
        }
    }
}