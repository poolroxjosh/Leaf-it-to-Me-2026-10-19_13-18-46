using System.Collections.Generic;
using UnityEngine;

public class LeafHighlightRenderer : MonoBehaviour
{
    [Header("References")]
    public LeafManager leafManager;
    public LeafSpawner leafSpawner;
    public Material highlightMaterial;

    public static LeafHighlightRenderer Instance
    {
        get;
        private set;
    }

    private readonly List<int> highlightedIds =
        new List<int>(32);

    private readonly Dictionary<int, Mesh> meshes =
        new Dictionary<int, Mesh>();

    public struct HighlightDraw
    {
        public Mesh mesh;
        public Matrix4x4 matrix;
    }

    public bool HasHighlights =>
        isActiveAndEnabled &&
        highlightedIds.Count > 0;

    private void Awake()
    {
        if (leafManager == null)
            leafManager = FindAnyObjectByType<LeafManager>();

        if (leafSpawner == null)
            leafSpawner = FindAnyObjectByType<LeafSpawner>();
    }

    private void OnEnable()
    {
        Instance = this;
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

            if (filter != null && filter.sharedMesh != null)
                meshes[i] = filter.sharedMesh;
        }
    }

    public void SetHighlights(List<int> ids)
    {
        highlightedIds.Clear();

        if (ids != null)
            highlightedIds.AddRange(ids);
    }

    public void ClearHighlights()
    {
        highlightedIds.Clear();
    }

    public void GetDraws(List<HighlightDraw> results)
    {
        results.Clear();

        if (leafManager == null || leafSpawner == null)
            return;

        foreach (int id in highlightedIds)
        {
            if (id < 0 || id >= leafManager.leaves.Count)
                continue;

            LeafData leaf = leafManager.leaves[id];

            if (leaf.state != LeafState.Resting ||
                leafManager.IsLeafProtected(id))
                continue;

            if (leafManager.activeTarp != null &&
                leafManager.activeTarp.ShouldHideLeafVisual(
                    id, leaf.position))
                continue;

            if (!meshes.TryGetValue(
                leaf.variantIndex, out Mesh mesh))
                continue;

            Quaternion rotation =
                leaf.rotation * Quaternion.Euler(
                    leafSpawner.variants[leaf.variantIndex]
                        .meshRotationOffset
                );

            results.Add(new HighlightDraw
            {
                mesh = mesh,

                matrix = Matrix4x4.TRS(
                    leaf.position,
                    rotation,
                    leaf.scale
                )
            });
        }
    }

    private void OnDisable()
    {
        ClearHighlights();

        if (Instance == this)
            Instance = null;
    }
}