
using System.Collections.Generic;
using UnityEngine;

public class LeafTarpSurface : MonoBehaviour
{
    [Header("References")]
    public LeafManager leafManager;

    [Header("Tarp Dimensions")]
    [Min(0.1f)]
    public float width = 3f;

    [Min(0.1f)]
    public float length = 3f;

    [Header("Surface")]
    public float surfaceHeight = 0.025f;

    [Min(0f)]
    public float heightTolerance = 0.15f;

    [Header("Surface Physics")]
    [Min(0f)]
    public float frictionMultiplier = 0.65f;

    [Header("Visual Coverage")]
    [Min(0f)]
    public float visualCoverageMargin = 0.08f;

    [Header("Visual Occlusion")]
    [Min(0f)]
    public float visibleEdgeWidth = 0.12f;

    private readonly HashSet<int> coveredLeaves =
        new HashSet<int>();

    private readonly HashSet<int> capturedLeaves =
        new HashSet<int>();

    private readonly List<int> candidates =
        new List<int>(2048);

    public int CapturedCount => capturedLeaves.Count;

    public bool IsDeployed { get; private set; }

    public bool ContainsPosition(Vector3 worldPosition)
    {
        Vector3 local =
            transform.InverseTransformPoint(worldPosition);

        return Mathf.Abs(local.x) <= width * 0.5f &&
               Mathf.Abs(local.z) <= length * 0.5f;
    }

    public bool IsCovered(int leafId)
    {
        return IsDeployed &&
               coveredLeaves.Contains(leafId);
    }

    public bool IsCaptured(int leafId)
    {
        return IsDeployed &&
               capturedLeaves.Contains(leafId);
    }

    public bool IsProtected(int leafId)
    {
        return IsCovered(leafId);
    }

    public void Deploy()
    {
        coveredLeaves.Clear();
        capturedLeaves.Clear();

        if (leafManager == null)
        {
            Debug.LogError(
                "LeafTarpSurface: LeafManager missing."
            );
            return;
        }

        IsDeployed = true;

        Vector3 scale = transform.lossyScale;

        float worldWidth =
            width * Mathf.Abs(scale.x);

        float worldLength =
            length * Mathf.Abs(scale.z);

        float radius = Mathf.Sqrt(
            worldWidth * worldWidth +
            worldLength * worldLength
        ) * 0.5f;

        leafManager.FindLeavesInArea(
            transform.position,
            radius + 0.5f,
            candidates
        );

        foreach (int id in candidates)
        {
            if (id < 0 || id >= leafManager.leaves.Count)
                continue;

            LeafData leaf = leafManager.leaves[id];

            if (leaf.state == LeafState.Collected)
                continue;

            if (!ContainsVisualPosition(leaf.position))
                continue;

            float localY =
                transform.InverseTransformPoint(
                    leaf.position
                ).y;

            // Only classify leaves below or very close
            // to the tarp surface as covered.
            if (localY > surfaceHeight + heightTolerance)
                continue;

            coveredLeaves.Add(id);
        }
        RefreshCoveredLeafVisuals();

        Debug.Log(
            $"Tarp deployed over {coveredLeaves.Count} leaves."
        );
    }

    public void GetCapturedLeaves(List<int> results)
    {
        results.Clear();

        foreach (int id in capturedLeaves)
            results.Add(id);
    }

    public void ClearSurface()
    {
        if (!IsDeployed)
            return;

        // Notify the renderer before clearing the tarp.
        RefreshCoveredLeafVisuals();

        IsDeployed = false;

        coveredLeaves.Clear();
        capturedLeaves.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Matrix4x4 previous = Gizmos.matrix;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(
                width,
                0.03f,
                length
            )
        );

        Gizmos.matrix = previous;
    }

    public float GetSurfaceY(Vector3 position)
    {
        // Assumes the tarp is placed horizontally.
        return transform.position.y + surfaceHeight;
    }

    public void UpdateLeafMembership(
        int leafId,
        Vector3 previousPosition,
        Vector3 newPosition)
    {
        if (!IsDeployed || IsCovered(leafId))
            return;

        bool wasInside = ContainsPosition(previousPosition);
        bool isInside = ContainsPosition(newPosition);

        if (!wasInside && isInside)
        {
            // Leaf has entered the tarp.
            capturedLeaves.Add(leafId);
        }
        else if (wasInside && !isInside)
        {
            // Leaf has left the tarp.
            capturedLeaves.Remove(leafId);
        }
    }

    public void RefreshCoveredLeafVisuals()
    {
        if (leafManager == null)
            return;

        leafManager.FindLeavesInArea(
            transform.position,
            GetVisualSearchRadius(),
            candidates
        );

        leafManager.RefreshLeafVisuals(candidates);
    }

    public bool ContainsVisualPosition(Vector3 worldPosition)
    {
        Vector3 local =
            transform.InverseTransformPoint(worldPosition);

        Vector3 scale = transform.lossyScale;

        float marginX =
            visualCoverageMargin /
            Mathf.Max(0.001f, Mathf.Abs(scale.x));

        float marginZ =
            visualCoverageMargin /
            Mathf.Max(0.001f, Mathf.Abs(scale.z));

        return Mathf.Abs(local.x) <=
                width * 0.5f + marginX &&
            Mathf.Abs(local.z) <=
                length * 0.5f + marginZ;
    }

    public bool ShouldHideLeafVisual(
        int leafId,
        Vector3 worldPosition)
    {
        if (!IsDeployed)
            return false;

        // Leaves raked onto the tarp must remain visible.
        if (IsCaptured(leafId))
            return false;

        // Convert the leaf's world position into
        // the tarp's local coordinate system.
        Vector3 local =
            transform.InverseTransformPoint(worldPosition);

        // Account for the tarp's scale.
        Vector3 scale = transform.lossyScale;

        float edgeX = visibleEdgeWidth /
            Mathf.Max(0.001f, Mathf.Abs(scale.x));

        float edgeZ = visibleEdgeWidth /
            Mathf.Max(0.001f, Mathf.Abs(scale.z));

        // Only hide leaves inside the tarp.
        float innerHalfWidth =
            Mathf.Max(0f, width * 0.5f - edgeX);

        float innerHalfLength =
            Mathf.Max(0f, length * 0.5f - edgeZ);

        return Mathf.Abs(local.x) < innerHalfWidth &&
            Mathf.Abs(local.z) < innerHalfLength;
    }

    private float GetVisualSearchRadius()
    {
        Vector3 scale = transform.lossyScale;

        float worldWidth = width * Mathf.Abs(scale.x);
        float worldLength = length * Mathf.Abs(scale.z);

        return Mathf.Sqrt(
            worldWidth * worldWidth +
            worldLength * worldLength
        ) * 0.5f + visualCoverageMargin + 0.5f;
    }
}