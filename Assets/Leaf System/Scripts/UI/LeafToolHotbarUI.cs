
using System.Collections.Generic;
using UnityEngine;

public class LeafToolHotbarUI : MonoBehaviour
{
    [Header("References")]
    public PlayerToolController toolController;

    [Header("Prefab")]
    public LeafToolSlotUI slotPrefab;

    [Header("Container")]
    public Transform slotContainer;

    [Header("Tool Icons")]
    public Sprite handsIcon;
    public Sprite rakeIcon;

    [Header("Visibility")]
    public LeafShopUI shopUI;
    public WheelbarrowMovementController wheelbarrowMovement;
    public CanvasGroup canvasGroup;

    private readonly Dictionary<PlayerTool, LeafToolSlotUI> slots =
        new Dictionary<PlayerTool, LeafToolSlotUI>();

    private void OnEnable()
    {
        if (toolController != null)
            toolController.OnToolChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (toolController != null)
            toolController.OnToolChanged -= Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (toolController == null ||
            slotPrefab == null ||
            slotContainer == null)
            return;

        // Hands are always available.
        EnsureSlot(
            PlayerTool.Hands,
            "Hands",
            "1",
            handsIcon
        );

        // Rake appears only after purchase.
        if (toolController.HasRake)
        {
            EnsureSlot(
                PlayerTool.Rake,
                "Rake",
                "2",
                rakeIcon
            );
        }

        // Update the selected appearance.
        foreach (var pair in slots)
        {
            if (pair.Value != null)
            {
                pair.Value.SetSelected(
                    toolController.EquippedTool == pair.Key
                );
            }
        }
    }

    private void EnsureSlot(
        PlayerTool tool,
        string name,
        string key,
        Sprite icon)
    {
        if (slots.ContainsKey(tool))
            return;

        LeafToolSlotUI slot =
            Instantiate(slotPrefab, slotContainer, false);

        slot.gameObject.name = name + " Slot";

        slot.Initialize(
            tool,
            name,
            key,
            icon
        );

        slots.Add(tool, slot);
    }

    private void Update()
    {
        if (canvasGroup == null)
            return;

        bool shopOpen =
            shopUI != null && shopUI.IsOpen;

        bool pushingWheelbarrow =
            wheelbarrowMovement != null &&
            wheelbarrowMovement.IsPushing;

        bool shouldShow =
            !shopOpen && !pushingWheelbarrow;

        canvasGroup.alpha = shouldShow ? 1f : 0f;
    }
}