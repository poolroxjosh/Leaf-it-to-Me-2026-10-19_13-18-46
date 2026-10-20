
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LeafManager))]
public class LeafSpawner : MonoBehaviour
{
    [Header("Leaf Variants")]
    public LeafVariant[] variants;

    [Header("Spawn Area")]
    public Vector2 spawnArea = new Vector2(50f, 50f);

    [Min(0)]
    public int leafCount = 100000;

    public LayerMask groundLayers = ~0;

    [Header("Surface Detection")]
    public float raycastHeight = 100f;
    public float raycastDistance = 250f;

    [Range(0f, 90f)]
    public float maxSlope = 45f;

    public float surfaceOffset = 0.015f;

    [Header("Performance")]
    [Min(1)]
    public int attemptsPerFrame = 5000;

    [Min(1)]
    public int maxAttemptsPerLeaf = 5;

    public int randomSeed = 12345;

    private LeafManager manager;

    private IEnumerator Start()
    {
        manager = GetComponent<LeafManager>();

        if (variants == null || variants.Length == 0)
        {
            Debug.LogError(
                "LeafSpawner: No leaf variants assigned."
            );
            yield break;
        }

        manager.ClearLeaves();
        manager.BeginBulkSpawn();

        Random.State previousState = Random.state;
        Random.InitState(randomSeed);

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = leafCount * maxAttemptsPerLeaf;

        while (spawned < leafCount &&
               attempts < maxAttempts)
        {
            int frameAttempts = 0;

            while (frameAttempts < attemptsPerFrame &&
                   spawned < leafCount &&
                   attempts < maxAttempts)
            {
                attempts++;
                frameAttempts++;

                float x = Random.Range(
                    -spawnArea.x * 0.5f,
                     spawnArea.x * 0.5f
                );

                float z = Random.Range(
                    -spawnArea.y * 0.5f,
                     spawnArea.y * 0.5f
                );

                Vector3 origin = transform.position +
                    new Vector3(x, raycastHeight, z);

                if (!Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit hit,
                    raycastDistance,
                    groundLayers,
                    QueryTriggerInteraction.Ignore))
                    continue;

                float slope = Vector3.Angle(
                    hit.normal, Vector3.up
                );

                if (slope > maxSlope)
                    continue;

                int variantIndex = PickVariant();

                if (variantIndex < 0)
                    break;

                LeafVariant variant = variants[variantIndex];

                float minSize = Mathf.Min(
                    variant.sizeMultiplier.x,
                    variant.sizeMultiplier.y
                );

                float maxSize = Mathf.Max(
                    variant.sizeMultiplier.x,
                    variant.sizeMultiplier.y
                );

                float size = Random.Range(minSize, maxSize);

                Quaternion groundAlignment =
                    Quaternion.FromToRotation(
                        Vector3.up,
                        hit.normal
                    );

                Quaternion randomYaw = Quaternion.Euler(
                    0f,
                    Random.Range(0f, 360f),
                    0f
                );

                Quaternion subtleTilt = Quaternion.Euler(
                    Random.Range(-5f, 5f),
                    0f,
                    Random.Range(-5f, 5f)
                );

                Quaternion rotation =
                    groundAlignment *
                    randomYaw *
                    subtleTilt;

                LeafData leaf = new LeafData
                {
                    variantIndex = variantIndex,
                    position = hit.point +
                        hit.normal * surfaceOffset,
                    rotation = rotation,
                    scale = Vector3.one * size,
                    velocity = Vector3.zero,
                    color = variant.colors.Evaluate(Random.value),
                    state = LeafState.Resting
                };

                manager.AddLeaf(leaf);
                spawned++;
            }

            yield return null;
        }

        Random.state = previousState;

        manager.EndBulkSpawn();

        Debug.Log(
            $"LeafSpawner: Spawned {spawned:N0} leaves " +
            $"after {attempts:N0} attempts."
        );
    }

    private int PickVariant()
    {
        float totalWeight = 0f;

        foreach (LeafVariant variant in variants)
        {
            if (variant != null &&
                variant.prefab != null)
                totalWeight += Mathf.Max(0f, variant.spawnWeight);
        }

        if (totalWeight <= 0f)
            return -1;

        float selection = Random.value * totalWeight;

        for (int i = 0; i < variants.Length; i++)
        {
            LeafVariant variant = variants[i];

            if (variant == null || variant.prefab == null)
                continue;

            selection -= Mathf.Max(0f, variant.spawnWeight);

            if (selection <= 0f)
                return i;
        }

        for (int i = variants.Length - 1; i >= 0; i--)
        {
            if (variants[i] != null &&
                variants[i].prefab != null &&
                variants[i].spawnWeight > 0f)
                return i;
        }

        return -1;
    }
}
