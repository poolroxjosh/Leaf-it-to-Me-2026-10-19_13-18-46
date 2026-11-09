
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

    private float nextStrokeTime;

    private readonly List<int> candidates =
        new List<int>(1024);

    private void Update()
    {
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
        int blocked = 0;
        int noGround = 0;

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

            float sideways = Vector3.Dot(
                relative, right
            );

            // Check rectangular rake area.
            if (depth < nearDistance ||
                depth > farDistance ||
                Mathf.Abs(sideways) > halfWidth)
                continue;

            insideArea++;

            float allowedPull = Mathf.Min(
                pullDistance,
                depth - nearDistance
            );

            if (allowedPull <= 0f)
                continue;

            Vector3 desiredPosition =
                leaf.position - forward * allowedPull;

            Vector3 travel =
                desiredPosition - leaf.position;

            float travelDistance = travel.magnitude;

            // Prevent leaves crossing solid obstacles.
            if (travelDistance > 0.001f &&
                Physics.SphereCast(
                    leaf.position + Vector3.up * 0.1f,
                    collisionRadius,
                    travel.normalized,
                    out RaycastHit obstacleHit,
                    travelDistance,
                    obstacleLayers,
                    QueryTriggerInteraction.Ignore))
            {
                float safeDistance = Mathf.Max(
                    0f,
                    obstacleHit.distance - 0.01f
                );

                desiredPosition =
                    leaf.position +
                    travel.normalized * safeDistance;
            }

            // Find the ground at the destination.
            Vector3 destinationRayOrigin =
                desiredPosition +
                Vector3.up * groundRayHeight;

            if (!Physics.Raycast(
                destinationRayOrigin,
                Vector3.down,
                out RaycastHit groundHit,
                groundRayDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore))
            {
                noGround++;
                continue;
            }

            Vector3 finalPosition =
                groundHit.point +
                groundHit.normal * leafClearance;

            // Check whether destination is blocked.
            if (Physics.CheckSphere(
                finalPosition + Vector3.up * 0.08f,
                collisionRadius,
                obstacleLayers,
                QueryTriggerInteraction.Ignore))
            {
                blocked++;
                continue;
            }

            if ((finalPosition - leaf.position).sqrMagnitude
                < 0.000001f)
                continue;

            if (leafManager.MoveLeaf(id, finalPosition))
                moved++;
        }

        if (logStrokes)
        {
            Debug.Log(
                $"RAKE STROKE | " +
                $"Candidates: {candidates.Count} | " +
                $"Inside Area: {insideArea} | " +
                $"Moved: {moved} | " +
                $"Blocked: {blocked} | " +
                $"No Ground: {noGround}"
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