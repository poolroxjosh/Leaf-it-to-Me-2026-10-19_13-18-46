using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LeafHighlightRenderer : MonoBehaviour
{
    [Header("References")]
    public LeafManager leafManager;
    public LeafSpawner leafSpawner;
    public Material highlightMaterial;

    [Header("Rendering")]
    [Range(1f, 1.5f)]
    public float outlineScale = 1.12f;

    [Min(0f)]
    public float heightOffset = 0.005f;

    private readonly List<int> highlightedIds =
        new List<int>(32);

    private readonly Dictionary<int, Mesh> meshes =
        new Dictionary<int, Mesh>();

    private readonly Dictionary<int, List<Matrix4x4>> batches =
        new Dictionary<int, List<Matrix4x4>>();

    private void Awake()
    {
        if (leafManager == null)
            leafManager = FindAnyObjectByType<LeafManager>();

        if (leafSpawner == null)
            leafSpawner = FindAnyObjectByType<LeafSpawner>();

        if (highlightMaterial != null)
            highlightMaterial.enableInstancing = true;
    }

    private void Start()
    {
        if (leafSpawner == null ||
            leafSpawner.variants == null)
            return;

        for (int i = 0; i < leafSpawner.variants.Length; i++)
        {
            LeafVariant variant = leafSpawner.variants[i];

            if (variant == null || variant.prefab == null)
                continue;

            MeshFilter filter =
                variant.prefab.GetComponent<MeshFilter>();

            if (filter == null || filter.sharedMesh == null)
                continue;

            meshes[i] = filter.sharedMesh;
            batches[i] = new List<Matrix4x4>(32);
        }
    }

    public void SetHighlights(List<int> ids)
    {
        highlightedIds.Clear();

        if (ids == null)
            return;

        highlightedIds.AddRange(ids);
    }

    public void ClearHighlights()
    {
        highlightedIds.Clear();
    }

    private void LateUpdate()
    {
        if (leafManager == null ||
            leafSpawner == null ||
            highlightMaterial == null ||
            highlightedIds.Count == 0)
            return;

        foreach (List<Matrix4x4> batch in batches.Values)
            batch.Clear();

        foreach (int id in highlightedIds)
        {
            if (id < 0 || id >= leafManager.leaves.Count)
                continue;

            LeafData leaf = leafManager.leaves[id];

            if (leaf.state != LeafState.Resting)
                continue;

            if (!batches.TryGetValue(
                leaf.variantIndex,
                out List<Matrix4x4> matrices))
                continue;

            LeafVariant variant =
                leafSpawner.variants[leaf.variantIndex];

            Quaternion rotation =
                leaf.rotation *
                Quaternion.Euler(variant.meshRotationOffset);

            Vector3 position =
                leaf.position + Vector3.up * heightOffset;

            // The outline shader enlarges the mesh
            // using its own Outline Scale property.
            Matrix4x4 matrix = Matrix4x4.TRS(
                position,
                rotation,
                leaf.scale
            );

            matrices.Add(matrix);
        }

        foreach (var entry in batches)
        {
            if (entry.Value.Count == 0)
                continue;

            if (!meshes.TryGetValue(
                entry.Key,
                out Mesh mesh))
                continue;

            RenderParams renderParams =
                new RenderParams(highlightMaterial);

            renderParams.shadowCastingMode =
                ShadowCastingMode.Off;

            renderParams.receiveShadows = false;
            renderParams.layer = gameObject.layer;

            // Grasp currently supports at most 21
            // leaves, so a single batch is sufficient.
            Graphics.RenderMeshInstanced(
                renderParams,
                mesh,
                0,
                entry.Value.ToArray()
            );
        }
    }
}