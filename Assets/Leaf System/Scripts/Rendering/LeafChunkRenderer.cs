
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(LeafManager))]
[RequireComponent(typeof(LeafSpawner))]
public class LeafChunkRenderer : MonoBehaviour
{
    [Header("Rendering")]
    public Material leafMaterial;
    public Camera targetCamera;

    public bool castShadows = false;
    public bool receiveShadows = false;

    [Header("Culling")]
    public bool enableFrustumCulling = true;

    private const int BatchSize = 1023;

    private LeafManager manager;
    private LeafSpawner spawner;

    private readonly Dictionary<int, Mesh> meshes =
        new Dictionary<int, Mesh>();

    private readonly Dictionary<Vector2Int, Chunk> chunks =
        new Dictionary<Vector2Int, Chunk>();

    private readonly HashSet<Vector2Int> dirtyChunks =
        new HashSet<Vector2Int>();

    private MaterialPropertyBlock propertyBlock;

    private static readonly int LeafColorID =
        Shader.PropertyToID("_LeafColor");

    private class RenderBatch
    {
        public Mesh mesh;
        public Matrix4x4[] matrices;
        public Vector4[] colors;
    }

    private class Chunk
    {
        public Bounds bounds;
        public List<RenderBatch> batches =
            new List<RenderBatch>();
    }

    private void Awake()
    {
        manager = GetComponent<LeafManager>();
        spawner = GetComponent<LeafSpawner>();

        propertyBlock = new MaterialPropertyBlock();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (leafMaterial != null)
            leafMaterial.enableInstancing = true;
    }

    private void OnEnable()
    {
        if (manager == null)
            manager = GetComponent<LeafManager>();

        manager.OnChunkChanged += MarkChunkDirty;
        manager.OnLeavesCleared += ClearChunks;
        manager.OnBulkSpawnCompleted += RebuildAllChunks;
    }

    private void Start()
    {
        if (leafMaterial == null)
        {
            Debug.LogError(
                "LeafChunkRenderer: No material assigned."
            );

            enabled = false;
            return;
        }

        LoadMeshes();
    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.OnChunkChanged -= MarkChunkDirty;
            manager.OnLeavesCleared -= ClearChunks;
        }
        manager.OnBulkSpawnCompleted -= RebuildAllChunks;
    }

    private void MarkChunkDirty(Vector2Int coordinate)
    {
        dirtyChunks.Add(coordinate);
    }

    private void ClearChunks()
    {
        chunks.Clear();
        dirtyChunks.Clear();
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
                continue;

            meshes[i] = filter.sharedMesh;
        }
    }

    private void LateUpdate()
    {
        RebuildDirtyChunks();
        RenderChunks();
    }

    private void RebuildDirtyChunks()
    {
        if (dirtyChunks.Count == 0)
            return;

        foreach (Vector2Int coordinate in dirtyChunks)
        {
            RebuildChunk(coordinate);
        }

        dirtyChunks.Clear();
    }

    private void RebuildChunk(Vector2Int coordinate)
    {
        float cellSize = manager.cellSize;

        Vector3 center = new Vector3(
            (coordinate.x + 0.5f) * cellSize,
            0f,
            (coordinate.y + 0.5f) * cellSize
        );

        Chunk chunk = new Chunk();

        // Conservative vertical bounds for now.
        chunk.bounds = new Bounds(
            center,
            new Vector3(cellSize + 2f, 200f, cellSize + 2f)
        );

        Dictionary<int, List<LeafData>> grouped =
            new Dictionary<int, List<LeafData>>();

        if (manager.TryGetLeavesInChunk(
            coordinate,
            out List<int> leafIds))
        {
            foreach (int id in leafIds)
            {
                LeafData leaf = manager.leaves[id];

                if (leaf.state == LeafState.Collected)
                    continue;

                if (!meshes.ContainsKey(leaf.variantIndex))
                    continue;

                if (!grouped.TryGetValue(
                    leaf.variantIndex,
                    out List<LeafData> group))
                {
                    group = new List<LeafData>();

                    grouped.Add(
                        leaf.variantIndex,
                        group
                    );
                }

                group.Add(leaf);
            }
        }

        foreach (var entry in grouped)
        {
            int variantIndex = entry.Key;
            Mesh mesh = meshes[variantIndex];
            List<LeafData> leaves = entry.Value;

            for (int start = 0;
                 start < leaves.Count;
                 start += BatchSize)
            {
                int count = Mathf.Min(
                    BatchSize,
                    leaves.Count - start
                );

                RenderBatch batch = new RenderBatch
                {
                    mesh = mesh,
                    matrices = new Matrix4x4[count],
                    colors = new Vector4[count]
                };

                for (int i = 0; i < count; i++)
                {
                    LeafData leaf = leaves[start + i];

                    Quaternion meshRotation =
                        Quaternion.Euler(
                            spawner.variants[variantIndex]
                                .meshRotationOffset
                        );

                    batch.matrices[i] = Matrix4x4.TRS(
                        leaf.position,
                        leaf.rotation * meshRotation,
                        leaf.scale
                    );

                    batch.colors[i] = leaf.color;
                }

                chunk.batches.Add(batch);
            }
        }

        chunks[coordinate] = chunk;
    }

    private void RenderChunks()
    {
        if (leafMaterial == null)
            return;

        Plane[] planes = null;

        if (enableFrustumCulling && targetCamera != null)
        {
            planes = GeometryUtility.CalculateFrustumPlanes(
                targetCamera
            );
        }

        foreach (Chunk chunk in chunks.Values)
        {
            if (planes != null &&
                !GeometryUtility.TestPlanesAABB(
                    planes,
                    chunk.bounds))
                continue;

            foreach (RenderBatch batch in chunk.batches)
            {
                propertyBlock.Clear();

                propertyBlock.SetVectorArray(
                    LeafColorID,
                    batch.colors
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
                    batch.mesh,
                    0,
                    batch.matrices
                );
            }
        }
    }
    
    private void RebuildAllChunks()
    {
        chunks.Clear();
        dirtyChunks.Clear();

        HashSet<Vector2Int> coordinates =
            new HashSet<Vector2Int>();

        foreach (LeafData leaf in manager.leaves)
        {
            if (leaf.state == LeafState.Collected)
                continue;

            coordinates.Add(
                manager.GetChunkCoordinate(leaf.position)
            );
        }

        foreach (Vector2Int coordinate in coordinates)
        {
            RebuildChunk(coordinate);
        }
    }

}
