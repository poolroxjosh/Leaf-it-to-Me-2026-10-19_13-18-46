
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LeafRakeController : MonoBehaviour
{
    [Header("References")]
    public PlayerToolController toolController;
    public LeafManager leafManager;
    public Camera playerCamera;
    public LeafShopUI shopUI;

    [Header("Stroke Settings")]
    [Min(0.05f)]
    public float strokeCooldown = 0.8f;

    [Min(0.1f)]
    public float rakeWidth = 1.2f;

    [Min(0.2f)]
    public float rakeReach = 3f;

    [Min(0.01f)]
    public float pullDistance = 0.6f;

    [Min(1)]
    public int maxLeavesPerStroke = 500;

    [Header("Collision")]
    public LayerMask groundLayers;
    public LayerMask obstacleLayers;

    public float groundRayHeight = 3f;
    public float groundRayDistance = 8f;
    public float leafClearance = 0.04f;
    public float collisionRadius = 0.06f;

    [Header("Debug")]
    public bool showRakeArea = true;
    public bool logStrokes = true;

    [Header("Physics")]
    public LeafPhysicsSystem leafPhysics;

    [Header("Wheelbarrow Movement")]
    public WheelbarrowMovementController wheelbarrowMovement;

    [Min(0.1f)]
    public float rakeImpulse = 3.5f;

    [Range(0f, 1f)]
    public float sidewaysVariation = 0.15f;

    private float nextStrokeTime;

    private readonly List<int> candidates =
        new List<int>(1024);

    private void Update()
    {
        if (wheelbarrowMovement != null &&
            wheelbarrowMovement.IsPushing)
            return;
        if (toolController == null ||
            leafManager == null ||
            playerCamera == null)
            return;

        if (toolController.EquippedTool != PlayerTool.Rake)
            return;

        if (shopUI != null && shopUI.IsOpen)
            return;

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        if (Mouse.current == null ||
            !Mouse.current.leftButton.isPressed)
            return;

        if (Time.time < nextStrokeTime)
            return;

        PerformStroke();

        nextStrokeTime =
            Time.time + Mathf.Max(0.05f, strokeCooldown);
    }

    private void PerformStroke()
    {
        // Horizontal direction the player is facing.
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 playerPosition = transform.position;

        float nearDistance = 0.5f;
        float farDistance = Mathf.Max(
            nearDistance + 0.1f,
            rakeReach
        );

        float halfWidth = rakeWidth * 0.5f;

        // Center of the rake's affected area.
        Vector3 center = playerPosition +
            forward * ((nearDistance + farDistance) * 0.5f);

        // Find the ground height at the center.
        Vector3 groundOrigin =
            center + Vector3.up * groundRayHeight;

        if (Physics.Raycast(
            groundOrigin,
            Vector3.down,
            out RaycastHit centerGround,
            groundRayDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore))
        {
            center.y = centerGround.point.y;
        }

        // Radius large enough to contain the
        // entire rectangular rake area.
        float halfLength =
            (farDistance - nearDistance) * 0.5f;

        float searchRadius = Mathf.Sqrt(
            halfLength * halfLength +
            halfWidth * halfWidth
        );

        leafManager.FindLeavesNear(
            center,
            searchRadius + 0.5f,
            candidates
        );

        int moved = 0;
        int insideArea = 0;

        foreach (int id in candidates)
        {
            if (moved >= maxLeavesPerStroke)
                break;

            LeafData leaf = leafManager.leaves[id];

            if (leaf.state != LeafState.Resting)
                continue;

            Vector3 relative =
                leaf.position - playerPosition;

            float depth = Vector3.Dot(
                relative, forward
            );

            float sidewaysDistance = Vector3.Dot(
                relative, right
            );

            // Check rectangular rake area.
            if (depth < nearDistance ||
                depth > farDistance ||
                Mathf.Abs(sidewaysDistance) > halfWidth)
                continue;

            insideArea++;

            float strength = Mathf.Max(
                0.1f,
                pullDistance
            );

            // Pull toward the player.
            Vector3 pullDirection = -forward;

            // Add subtle random sideways movement.
            float randomSideways = Random.Range(
                -sidewaysVariation,
                sidewaysVariation
            );

            Vector3 impulseDirection =
                (pullDirection + right * randomSideways).normalized;

            // Leaves farther away receive slightly
            // more force during the rake stroke.
            float depthFactor = Mathf.InverseLerp(
                nearDistance,
                farDistance,
                depth
            );

            float impulseMagnitude =
                rakeImpulse *
                strength *
                Mathf.Lerp(0.75f, 1.25f, depthFactor);

            Vector3 impulse =
                impulseDirection * impulseMagnitude;

            if (leafPhysics != null &&
                leafPhysics.ApplyImpulse(id, impulse))
            {
                moved++;
            }
        }

        if (logStrokes)
        {
            Debug.Log(
                $"RAKE STROKE | " +
                $"Candidates: {candidates.Count} | " +
                $"Inside Area: {insideArea} | " +
                $"Impulses Applied: {moved} | " +
                $"Active Physics Leaves: " +
                $"{(leafPhysics != null ? leafPhysics.ActiveLeafCount : 0)}"
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showRakeArea)
            return;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        float nearDistance = 0.5f;
        float farDistance = Mathf.Max(
            nearDistance + 0.1f,
            rakeReach
        );

        float length = farDistance - nearDistance;

        Vector3 center = transform.position +
            forward * ((nearDistance + farDistance) * 0.5f);

        Gizmos.color = Color.cyan;

        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            center,
            transform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(rakeWidth, 0.1f, length)
        );

        Gizmos.matrix = previousMatrix;
    }
}