
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LeafPickupController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public LeafManager leafManager;
    public LeafPickupVisuals pickupVisuals;

    [Header("Targeting")]
    public LayerMask groundLayers = ~0;

    [Min(0.1f)]
    public float pickupRange = 4f;

    [Min(0.05f)]
    public float searchRadius = 0.75f;

    [Min(1f)]
    public float targetScreenRadius = 35f;

    [Header("Pickup Upgrades")]
    [Min(1)]
    public int leavesPerPickup = 1;

    [Min(0.01f)]
    public float pickupCooldown = 0.4f;

    [Header("Input")]
    public bool holdToPickup = true;

    private float nextPickupTime;
    private int targetLeafId = -1;

    private readonly List<int> candidates =
        new List<int>(128);

    private readonly List<int> selected =
        new List<int>(16);

    public int TargetLeafId => targetLeafId;

    public float CooldownRemaining =>
        Mathf.Max(0f, nextPickupTime - Time.time);

    public bool CanPickup => CooldownRemaining <= 0f;

    private void Update()
    {
        if (playerCamera == null ||
            leafManager == null ||
            pickupVisuals == null)
            return;

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            targetLeafId = -1;
            pickupVisuals.ClearHighlight();
            return;
        }

        UpdateTarget();

        bool wantsPickup =
            Mouse.current != null &&
            (holdToPickup
                ? Mouse.current.leftButton.isPressed
                : Mouse.current.leftButton.wasPressedThisFrame);

        if (wantsPickup && CanPickup && targetLeafId >= 0)
        {
            TryPickup();
        }
    }

    private void UpdateTarget()
    {
        targetLeafId = -1;

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            pickupRange,
            groundLayers,
            QueryTriggerInteraction.Ignore))
        {
            pickupVisuals.ClearHighlight();
            return;
        }

        leafManager.FindLeavesNear(
            hit.point,
            searchRadius,
            candidates
        );

        float bestScore = float.MaxValue;

        Vector2 screenCenter = new Vector2(
            playerCamera.pixelRect.center.x,
            playerCamera.pixelRect.center.y
        );

        foreach (int id in candidates)
        {
            LeafData leaf = leafManager.leaves[id];

            if (!pickupVisuals.CanDisplayVariant(
                leaf.variantIndex))
                continue;

            Vector3 screenPoint =
                playerCamera.WorldToScreenPoint(leaf.position);

            if (screenPoint.z <= 0f)
                continue;

            float screenDistance = Vector2.Distance(
                new Vector2(screenPoint.x, screenPoint.y),
                screenCenter
            );

            if (screenDistance > targetScreenRadius)
                continue;

            float worldDistance = Vector3.Distance(
                playerCamera.transform.position,
                leaf.position
            );

            if (worldDistance > pickupRange)
                continue;

            // Prefer the leaf nearest to the crosshair.
            // World distance acts as a tie-breaker.
            float score =
                screenDistance +
                worldDistance * 0.1f;

            if (score < bestScore)
            {
                bestScore = score;
                targetLeafId = id;
            }
        }

        if (targetLeafId >= 0)
            pickupVisuals.SetHighlight(targetLeafId);
        else
            pickupVisuals.ClearHighlight();
    }

    private void TryPickup()
    {
        if (pickupVisuals.AvailableSlots <= 0)
            return;

        selected.Clear();
        selected.Add(targetLeafId);

        int desiredCount = Mathf.Min(
            Mathf.Max(1, leavesPerPickup),
            pickupVisuals.AvailableSlots
        );

        // Future upgrades can collect multiple nearby
        // leaves during one pickup action.
        if (desiredCount > 1)
        {
            Vector3 primaryPosition =
                leafManager.leaves[targetLeafId].position;

            candidates.Sort((a, b) =>
            {
                float da = (
                    leafManager.leaves[a].position -
                    primaryPosition
                ).sqrMagnitude;

                float db = (
                    leafManager.leaves[b].position -
                    primaryPosition
                ).sqrMagnitude;

                return da.CompareTo(db);
            });

            foreach (int id in candidates)
            {
                if (selected.Count >= desiredCount)
                    break;

                if (id == targetLeafId)
                    continue;

                if (leafManager.leaves[id].state !=
                    LeafState.Resting)
                    continue;

                if (!pickupVisuals.CanDisplayVariant(
                    leafManager.leaves[id].variantIndex))
                    continue;

                selected.Add(id);
            }
        }

        int started = 0;

        foreach (int id in selected)
        {
            if (!leafManager.BeginPickup(
                id, out LeafData leaf))
                continue;

            if (!pickupVisuals.PlayPickup(id, leaf))
            {
                leafManager.CancelPickup(id);
                continue;
            }

            started++;
        }

        if (started > 0)
        {
            nextPickupTime =
                Time.time + pickupCooldown;

            targetLeafId = -1;
            pickupVisuals.ClearHighlight();
        }
    }
}
