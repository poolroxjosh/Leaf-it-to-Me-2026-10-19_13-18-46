
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(LeafManager))]
public class LeafRenderer : MonoBehaviour
{
    [Header("Rendering")]
    public Material leafMaterial;

    public bool castShadows = false;
    public bool receiveShadows = false;

    private const int BatchSize = 1023;

    private LeafManager manager;
    private LeafSpawner spawner;

    private readonly Dictionary<int, Mesh> meshes =
        new Dictionary<int, Mesh>();

    private readonly List<Matrix4x4> matrices =
        new List<Matrix4x4>(BatchSize);

    private readonly List<Vector4> colors =
        new List<Vector4>(BatchSize);

    private MaterialPropertyBlock propertyBlock;

    private static readonly int LeafColorID =
        Shader.PropertyToID("_LeafColor");

    private void Awake()
    {
        manager = GetComponent<LeafManager>();
        spawner = GetComponent<LeafSpawner>();

        propertyBlock = new MaterialPropertyBlock();

        if (leafMaterial != null)
            leafMaterial.enableInstancing = true;
    }

    private void Start()
    {
        if (leafMaterial == null)
        {
            Debug.LogError(
                "LeafRenderer: No leaf material assigned."
            );
            enabled = false;
            return;
        }

        if (!SystemInfo.supportsInstancing)
        {
            Debug.LogError(
                "LeafRenderer: GPU instancing unsupported."
            );
            enabled = false;
            return;
        }

        if (spawner == null)
        {
            Debug.LogError(
                "LeafRenderer: LeafSpawner is missing."
            );
            enabled = false;
            return;
        }

        LoadMeshes();
    }

    private void LoadMeshes()
    {
        meshes.Clear();

        for (int i = 0; i < spawner.variants.Length; i++)
        {
            LeafVariant variant = spawner.variants[i];

            if (variant == null || variant.prefab == null)
                continue;

            MeshFilter filter =
                variant.prefab.GetComponent<MeshFilter>();

            if (filter == null || filter.sharedMesh == null)
            {
                Debug.LogWarning(
                    $"LeafRenderer: Variant {i} has no mesh."
                );
                continue;
            }

            meshes[i] = filter.sharedMesh;
        }
    }

    private void Update()
    {
        if (leafMaterial == null)
            return;

        foreach (KeyValuePair<int, Mesh> entry in meshes)
        {
            RenderVariant(entry.Key, entry.Value);
        }
    }

    private void RenderVariant(int variantIndex, Mesh mesh)
    {
        matrices.Clear();
        colors.Clear();

        foreach (LeafData leaf in manager.leaves)
        {
            if (leaf.state == LeafState.Collected)
                continue;

            if (leaf.variantIndex != variantIndex)
                continue;

            matrices.Add(Matrix4x4.TRS(
                leaf.position,
                leaf.rotation,
                leaf.scale
            ));

            colors.Add(leaf.color);

            if (matrices.Count >= BatchSize)
                FlushBatch(mesh);
        }

        if (matrices.Count > 0)
            FlushBatch(mesh);
    }

    private void FlushBatch(Mesh mesh)
    {
        propertyBlock.Clear();

        propertyBlock.SetVectorArray(
            LeafColorID,
            colors
        );

        RenderParams renderParams =
            new RenderParams(leafMaterial);

        renderParams.matProps = propertyBlock;

        renderParams.shadowCastingMode = castShadows
            ? ShadowCastingMode.On
            : ShadowCastingMode.Off;

        renderParams.receiveShadows = receiveShadows;
        renderParams.layer = gameObject.layer;

        Graphics.RenderMeshInstanced(
            renderParams,
            mesh,
            0,
            matrices.ToArray()
        );

        matrices.Clear();
        colors.Clear();
    }
}
