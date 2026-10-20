using System;
using System.Collections.Generic;
using UnityEngine;

public class LeafManager : MonoBehaviour
{

    public event Action<Vector2Int> OnChunkChanged;
    public event Action OnLeavesCleared;

    public Vector2Int GetChunkCoordinate(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    [Min(0.5f)]
    public float cellSize = 2f;

    public readonly List<LeafData> leaves =
        new List<LeafData>();

    private readonly Dictionary<Vector2Int, List<int>> grid =
        new Dictionary<Vector2Int, List<int>>();

    public int TotalLeaves => leaves.Count;
    public int CollectedLeaves { get; private set; }
    public int PendingPickupLeaves { get; private set; }
    public int RemainingLeaves => TotalLeaves - CollectedLeaves;

    private Vector2Int GetCell(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    public void ClearLeaves()
    {
        leaves.Clear();
        grid.Clear();

        CollectedLeaves = 0;
        PendingPickupLeaves = 0;
        IsBulkSpawning = false;

        OnLeavesCleared?.Invoke();
    }

    public void AddLeaf(LeafData leaf)
    {
        leaf.id = leaves.Count;

        leaves.Add(leaf);

        Vector2Int cell = GetCell(leaf.position);

        if (!grid.TryGetValue(cell, out List<int> ids))
        {
            ids = new List<int>();
            grid.Add(cell, ids);
        }

        ids.Add(leaf.id);

        if (!IsBulkSpawning)
        {
            OnChunkChanged?.Invoke(cell);
        }
    }

    public int CollectLeaves(Vector3 center, float radius)
    {
        int collected = 0;
        float radiusSquared = radius * radius;

        Vector2Int min = GetCell(
            center - new Vector3(radius, 0f, radius)
        );

        Vector2Int max = GetCell(
            center + new Vector3(radius, 0f, radius)
        );

        for (int x = min.x; x <= max.x; x++)
        {
            for (int z = min.y; z <= max.y; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);

                if (!grid.TryGetValue(
                    cell, out List<int> ids))
                    continue;

                foreach (int id in ids)
                {
                    LeafData leaf = leaves[id];

                    if (leaf.state == LeafState.Collected)
                        continue;

                    if ((leaf.position - center).sqrMagnitude >
                        radiusSquared)
                        continue;

                    leaf.state = LeafState.Collected;
                    leaves[id] = leaf;
                    OnChunkChanged?.Invoke(cell);

                    collected++;
                }
            }
        }

        CollectedLeaves += collected;

        return collected;
    }

    public bool TryGetLeavesInChunk(
        Vector2Int coordinate,
        out List<int> leafIds)
    {
        return grid.TryGetValue(coordinate, out leafIds);
    }

    public bool IsBulkSpawning { get; private set; }

    public event Action OnBulkSpawnCompleted;

    public void BeginBulkSpawn()
    {
        IsBulkSpawning = true;
    }

    public void EndBulkSpawn()
    {
        IsBulkSpawning = false;
        OnBulkSpawnCompleted?.Invoke();
    }

    public void FindLeavesNear(
        Vector3 center,
        float radius,
        List<int> results)
    {
        results.Clear();

        if (radius <= 0f)
            return;

        float radiusSq = radius * radius;

        Vector2Int min = GetChunkCoordinate(
            center - new Vector3(radius, 0f, radius)
        );

        Vector2Int max = GetChunkCoordinate(
            center + new Vector3(radius, 0f, radius)
        );

        for (int x = min.x; x <= max.x; x++)
        {
            for (int z = min.y; z <= max.y; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);

                if (!grid.TryGetValue(
                    cell, out List<int> ids))
                    continue;

                foreach (int id in ids)
                {
                    LeafData leaf = leaves[id];

                    if (leaf.state != LeafState.Resting)
                        continue;

                    if ((leaf.position - center).sqrMagnitude
                        > radiusSq)
                        continue;

                    results.Add(id);
                }
            }
        }
    }

    public bool BeginPickup(int id, out LeafData leaf)
    {
        leaf = default(LeafData);

        if (id < 0 || id >= leaves.Count)
            return false;

        leaf = leaves[id];

        if (leaf.state != LeafState.Resting)
            return false;

        leaf.state = LeafState.BeingCollected;
        leaves[id] = leaf;

        PendingPickupLeaves++;

        OnChunkChanged?.Invoke(
            GetChunkCoordinate(leaf.position)
        );

        return true;
    }

    public bool CompletePickup(int id)
    {
        if (id < 0 || id >= leaves.Count)
            return false;

        LeafData leaf = leaves[id];

        if (leaf.state != LeafState.BeingCollected)
            return false;

        leaf.state = LeafState.Collected;
        leaves[id] = leaf;

        PendingPickupLeaves--;
        CollectedLeaves++;

        return true;
    }

    public bool CancelPickup(int id)
    {
        if (id < 0 || id >= leaves.Count)
            return false;

        LeafData leaf = leaves[id];

        if (leaf.state != LeafState.BeingCollected)
            return false;

        leaf.state = LeafState.Resting;
        leaves[id] = leaf;

        PendingPickupLeaves--;

        OnChunkChanged?.Invoke(
            GetChunkCoordinate(leaf.position)
        );

        return true;
    }

}
