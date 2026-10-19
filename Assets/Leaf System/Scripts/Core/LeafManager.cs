
using System.Collections.Generic;
using UnityEngine;

public class LeafManager : MonoBehaviour
{
    [Min(0.5f)]
    public float cellSize = 2f;

    public readonly List<LeafData> leaves =
        new List<LeafData>();

    private readonly Dictionary<Vector2Int, List<int>> grid =
        new Dictionary<Vector2Int, List<int>>();

    public int TotalLeaves => leaves.Count;
    public int CollectedLeaves { get; private set; }
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

                    collected++;
                }
            }
        }

        CollectedLeaves += collected;

        return collected;
    }
}
