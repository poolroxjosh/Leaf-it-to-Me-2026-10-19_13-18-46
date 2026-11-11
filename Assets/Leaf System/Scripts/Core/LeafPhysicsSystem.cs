
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LeafManager))]
public class LeafPhysicsSystem : MonoBehaviour
{
    [Header("References")]
    public LeafManager leafManager;

    [Header("Ground Movement")]
    [Min(0f)]
    public float friction = 4f;

    [Min(0.001f)]
    public float sleepSpeed = 0.08f;

    [Min(0f)]
    public float maximumSpeed = 6f;

    [Header("Surface Detection")]
    public LayerMask groundLayers;
    public LayerMask obstacleLayers;

    [Min(0.1f)]
    public float groundRayHeight = 2f;

    [Min(0.1f)]
    public float groundRayDistance = 5f;

    [Min(0f)]
    public float surfaceOffset = 0.04f;

    [Min(0.001f)]
    public float collisionRadius = 0.06f;

    [Header("Performance")]
    [Min(1)]
    public int maxActiveLeaves = 2000;

    public int ActiveLeafCount => activeLeaves.Count;

    private readonly List<int> activeLeaves =
        new List<int>(2048);

    private readonly HashSet<int> activeLookup =
        new HashSet<int>();

    private void Awake()
    {
        if (leafManager == null)
            leafManager = GetComponent<LeafManager>();
    }

    private void OnEnable()
    {
        if (leafManager == null)
            leafManager = GetComponent<LeafManager>();

        leafManager.OnLeavesCleared += ClearSimulation;
    }

    private void OnDisable()
    {
        if (leafManager != null)
            leafManager.OnLeavesCleared -= ClearSimulation;

        ClearSimulation();
    }

    private void ClearSimulation()
    {
        activeLeaves.Clear();
        activeLookup.Clear();
    }

    public bool ApplyImpulse(int leafId, Vector3 impulse)
    {
        if (leafManager == null ||
            leafId < 0 ||
            leafId >= leafManager.leaves.Count)
            return false;

        LeafData leaf = leafManager.leaves[leafId];

        if (leaf.state != LeafState.Resting &&
            leaf.state != LeafState.Moving)
            return false;

        if (!activeLookup.Contains(leafId))
        {
            if (activeLeaves.Count >= maxActiveLeaves)
                return false;

            activeLookup.Add(leafId);
            activeLeaves.Add(leafId);
        }

        Vector3 velocity = leaf.velocity + impulse;

        // Initial rake physics is ground-constrained.
        velocity.y = 0f;

        velocity = Vector3.ClampMagnitude(
            velocity,
            maximumSpeed
        );

        leafManager.SetLeafMotion(
            leafId,
            velocity,
            true
        );

        return true;
    }

    private void FixedUpdate()
    {
        if (leafManager == null)
            return;

        float dt = Time.fixedDeltaTime;

        for (int i = activeLeaves.Count - 1; i >= 0; i--)
        {
            int id = activeLeaves[i];

            if (id < 0 || id >= leafManager.leaves.Count)
            {
                RemoveActive(i, id);
                continue;
            }

            LeafData leaf = leafManager.leaves[id];

            if (leaf.state != LeafState.Moving)
            {
                RemoveActive(i, id);
                continue;
            }

            Vector3 velocity = leaf.velocity;

            // Exponential friction gives smooth slowing.
            velocity *= Mathf.Exp(-friction * dt);
            velocity.y = 0f;

            if (velocity.magnitude <= sleepSpeed)
            {
                leafManager.SetLeafMotion(
                    id,
                    Vector3.zero,
                    false
                );

                RemoveActive(i, id);
                continue;
            }

            Vector3 displacement = velocity * dt;
            float distance = displacement.magnitude;

            if (distance <= 0.000001f)
                continue;

            Vector3 direction = displacement / distance;

            // Stop before hitting a solid obstacle.
            Vector3 castOrigin =
                leaf.position + Vector3.up * 0.1f;

            if (Physics.SphereCast(
                castOrigin,
                collisionRadius,
                direction,
                out RaycastHit obstacleHit,
                distance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore))
            {
                float safeDistance = Mathf.Max(
                    0f,
                    obstacleHit.distance - 0.01f
                );

                displacement = direction * safeDistance;

                // Simple collision response:
                // stop rather than pass through the object.
                velocity = Vector3.zero;
            }

            Vector3 proposedPosition =
                leaf.position + displacement;

            // Snap the moving leaf to valid ground.
            Vector3 rayOrigin =
                proposedPosition +
                Vector3.up * groundRayHeight;

            if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit groundHit,
                groundRayDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore))
            {
                leafManager.SetLeafMotion(
                    id, Vector3.zero, false
                );

                RemoveActive(i, id);
                continue;
            }

            Vector3 finalPosition =
                groundHit.point +
                groundHit.normal * surfaceOffset;

            // Reject destinations overlapping obstacles.
            if (Physics.CheckSphere(
                finalPosition + Vector3.up * 0.08f,
                collisionRadius,
                obstacleLayers,
                QueryTriggerInteraction.Ignore))
            {
                leafManager.SetLeafMotion(
                    id, Vector3.zero, false
                );

                RemoveActive(i, id);
                continue;
            }

            leafManager.MoveLeaf(id, finalPosition);

            if (velocity.sqrMagnitude <
                sleepSpeed * sleepSpeed)
            {
                leafManager.SetLeafMotion(
                    id, Vector3.zero, false
                );

                RemoveActive(i, id);
            }
            else
            {
                leafManager.SetLeafMotion(
                    id, velocity, true
                );
            }
        }
    }

    private void RemoveActive(int index, int id)
    {
        int last = activeLeaves.Count - 1;

        activeLeaves[index] = activeLeaves[last];
        activeLeaves.RemoveAt(last);

        activeLookup.Remove(id);
    }
}