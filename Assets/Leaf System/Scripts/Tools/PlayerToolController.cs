
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerTool
{
    Hands,
    Rake
}

public class PlayerToolController : MonoBehaviour
{
    [Header("References")]
    public LeafShopUI shopUI;

    [Header("Visuals")]
    public Transform toolHolder;
    public GameObject rakeVisualPrefab;

    [Header("Wheelbarrow Movement")]
    public WheelbarrowMovementController wheelbarrowMovement;

    [Header("Tool Visuals")]
    public PlayerToolController toolController;

    public PlayerTool EquippedTool { get; private set; }
        = PlayerTool.Hands;

    public bool HasRake { get; private set; }

    public event Action OnToolChanged;

    private GameObject rakeVisual;

    private void Start()
    {
        if (rakeVisualPrefab != null && toolHolder != null)
        {
            rakeVisual = Instantiate(
                rakeVisualPrefab, toolHolder);

            rakeVisual.SetActive(false);
        }
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
    }

    public void UnlockRake()
    {
        HasRake = true;
        OnToolChanged?.Invoke();
    }

    public void Equip(PlayerTool tool)
    {
        if (tool == PlayerTool.Rake && !HasRake)
        {
            Debug.Log("Cannot equip rake: not purchased.");
            return;
        }

        EquippedTool = tool;

        if (rakeVisual != null)
            rakeVisual.SetActive(tool == PlayerTool.Rake);

        Debug.Log($"Equipped tool: {EquippedTool}");

        OnToolChanged?.Invoke();
    }

    public void SetToolVisualsVisible(bool visible)
    {
        if (rakeVisual != null)
        {
            rakeVisual.SetActive(
                visible && EquippedTool == PlayerTool.Rake
            );
        }
    }
}