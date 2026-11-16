
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerTool
{
    Hands,
    Rake,
    Tarp
}

public class PlayerToolController : MonoBehaviour
{
    [Header("References")]
    public LeafShopUI shopUI;
    public WheelbarrowMovementController wheelbarrowMovement;

    [Header("Visuals")]
    public Transform toolHolder;
    public GameObject rakeVisualPrefab;

    public PlayerTool EquippedTool { get; private set; }
        = PlayerTool.Hands;

    public bool HasRake { get; private set; }
    public bool HasTarp { get; private set; }

    // Set by the tarp controller when carrying a bundle.
    public bool IsCarryingTarp { get; private set; }

    public event Action OnToolChanged;

    private GameObject rakeVisual;
    private bool toolVisualsVisible = true;

    private PlayerTool toolBeforeCarrying = PlayerTool.Hands;

    private void Start()
    {
        if (rakeVisualPrefab != null && toolHolder != null)
        {
            rakeVisual = Instantiate(
                rakeVisualPrefab, toolHolder
            );

            rakeVisual.SetActive(false);
        }

        RefreshToolVisuals();
    }

    private void Update()
    {
        if (wheelbarrowMovement != null &&
            wheelbarrowMovement.IsPushing)
            return;

        if (shopUI != null && shopUI.IsOpen)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            Equip(PlayerTool.Hands);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            Equip(PlayerTool.Rake);

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            Equip(PlayerTool.Tarp);
    }

    public void UnlockRake()
    {
        if (HasRake)
            return;

        HasRake = true;
        OnToolChanged?.Invoke();
    }

    public void UnlockTarp()
    {
        if (HasTarp)
            return;

        HasTarp = true;
        OnToolChanged?.Invoke();
    }

    public void Equip(PlayerTool tool)
    {
        // A bundled tarp occupies both hands.
        if (IsCarryingTarp && tool != PlayerTool.Tarp)
            return;

        // Cannot change tools while pushing.
        if (wheelbarrowMovement != null &&
            wheelbarrowMovement.IsPushing)
            return;

        if (tool == PlayerTool.Rake && !HasRake)
            return;

        if (tool == PlayerTool.Tarp && !HasTarp)
            return;

        if (EquippedTool == tool)
            return;

        EquippedTool = tool;

        RefreshToolVisuals();
        OnToolChanged?.Invoke();
    }

    public void SetCarryingTarp(bool carrying)
    {
        if (carrying && !HasTarp)
            return;

        if (IsCarryingTarp == carrying)
            return;

        if (carrying)
        {
            toolBeforeCarrying = EquippedTool;

            IsCarryingTarp = true;
            EquippedTool = PlayerTool.Tarp;
        }
        else
        {
            IsCarryingTarp = false;

            PlayerTool restoreTool = toolBeforeCarrying;

            // If the player gathered while holding the
            // tarp tool, return to Hands instead.
            if (restoreTool == PlayerTool.Tarp)
                restoreTool = PlayerTool.Hands;

            if (restoreTool == PlayerTool.Rake && !HasRake)
                restoreTool = PlayerTool.Hands;

            EquippedTool = restoreTool;
        }

        RefreshToolVisuals();
        OnToolChanged?.Invoke();
    }

    public void SetToolVisualsVisible(bool visible)
    {
        toolVisualsVisible = visible;
        RefreshToolVisuals();
    }

    private void RefreshToolVisuals()
    {
        if (rakeVisual != null)
        {
            rakeVisual.SetActive(
                toolVisualsVisible &&
                EquippedTool == PlayerTool.Rake &&
                !IsCarryingTarp
            );
        }
    }
}