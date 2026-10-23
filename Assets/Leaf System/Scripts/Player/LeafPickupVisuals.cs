
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LeafPickupVisuals : MonoBehaviour
{
    [Header("References")]
    public LeafManager leafManager;
    public LeafSpawner leafSpawner;
    public Transform pickupLocation;
    public Material leafMaterial;
    public LeafInventory inventory;

    [Header("Highlight")]
    public Color highlightColor = Color.yellow;
    public float highlightScale = 1.15f;
    public float highlightHeight = 0.025f;

    [Header("Flying Leaves")]
    public int maxFlyingLeaves = 32;
    public float flightDuration = 0.5f;
    public float arcHeight = 0.6f;
    public float spinDegrees = 360f;
    public float finalScale = 0.15f;

    private class Visual
    {
        public GameObject obj;
        public MeshFilter filter;
        public MeshRenderer renderer;

        public int leafId;
        public Vector3 startPosition;
        public Quaternion startRotation;
        public Vector3 startScale;

        public float elapsed;
        public float duration;
        public float spinDirection;
    }

    private Visual highlight;

    private readonly List<Visual> pool =
        new List<Visual>();

    private readonly List<Visual> flying =
        new List<Visual>();

    private readonly Dictionary<int, Mesh> meshes =
        new Dictionary<int, Mesh>();

    private MaterialPropertyBlock properties;

    private static readonly int LeafColorID =
        Shader.PropertyToID("_LeafColor");

    public int AvailableSlots =>
        Mathf.Max(0, maxFlyingLeaves - flying.Count);

    private void Awake()
    {
        properties = new MaterialPropertyBlock();
    }

    private void Start()
    {
        if (leafSpawner == null ||
            leafManager == null ||
            leafMaterial == null)
        {
            Debug.LogError(
                "LeafPickupVisuals: Missing references."
            );

            enabled = false;
            return;
        }

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

        highlight = CreateVisual("Leaf Highlight");
    }

    private Visual CreateVisual(string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform, true);

        MeshFilter filter = obj.AddComponent<MeshFilter>();
        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

        renderer.sharedMaterial = leafMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        obj.SetActive(false);

        return new Visual
        {
            obj = obj,
            filter = filter,
            renderer = renderer
        };
    }

    private Quaternion GetLeafRotation(LeafData leaf)
    {
        Vector3 offset =
            leafSpawner.variants[leaf.variantIndex]
                .meshRotationOffset;

        return leaf.rotation * Quaternion.Euler(offset);
    }

    private void SetColor(Visual visual, Color color)
    {
        properties.Clear();
        properties.SetColor(LeafColorID, color);
        visual.renderer.SetPropertyBlock(properties);
    }

    public bool CanDisplayVariant(int variantIndex)
    {
        return meshes.ContainsKey(variantIndex);
    }

    public void SetHighlight(int leafId)
    {
        if (highlight == null)
            return;

        if (leafId < 0 ||
            leafId >= leafManager.leaves.Count)
        {
            ClearHighlight();
            return;
        }

        LeafData leaf = leafManager.leaves[leafId];

        if (leaf.state != LeafState.Resting ||
            !meshes.TryGetValue(
                leaf.variantIndex, out Mesh mesh))
        {
            ClearHighlight();
            return;
        }

        highlight.filter.sharedMesh = mesh;

        highlight.obj.transform.SetPositionAndRotation(
            leaf.position + Vector3.up * highlightHeight,
            GetLeafRotation(leaf)
        );

        highlight.obj.transform.localScale =
            leaf.scale * highlightScale;

        SetColor(highlight, highlightColor);
        highlight.obj.SetActive(true);
    }

    public void ClearHighlight()
    {
        if (highlight != null)
            highlight.obj.SetActive(false);
    }

    public bool PlayPickup(int leafId, LeafData leaf)
    {
        if (pickupLocation == null ||
            AvailableSlots <= 0 ||
            !meshes.TryGetValue(
                leaf.variantIndex, out Mesh mesh))
            return false;

        Visual visual = null;

        foreach (Visual item in pool)
        {
            if (!item.obj.activeSelf)
            {
                visual = item;
                break;
            }
        }

        if (visual == null)
        {
            visual = CreateVisual("Flying Leaf");
            pool.Add(visual);
        }

        visual.leafId = leafId;
        visual.startPosition = leaf.position;
        visual.startRotation = GetLeafRotation(leaf);
        visual.startScale = leaf.scale;

        visual.elapsed = 0f;
        visual.duration = Mathf.Max(0.05f, flightDuration);
        visual.spinDirection =
            Random.value < 0.5f ? -1f : 1f;

        visual.filter.sharedMesh = mesh;

        visual.obj.transform.SetPositionAndRotation(
            leaf.position,
            visual.startRotation
        );

        visual.obj.transform.localScale = leaf.scale;

        SetColor(visual, leaf.color);

        visual.obj.SetActive(true);
        flying.Add(visual);

        return true;
    }

    private void Update()
    {
        if (pickupLocation == null)
            return;

        for (int i = flying.Count - 1; i >= 0; i--)
        {
            Visual visual = flying[i];

            visual.elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                visual.elapsed / visual.duration
            );

            float eased = t * t * (3f - 2f * t);

            Vector3 position = Vector3.Lerp(
                visual.startPosition,
                pickupLocation.position,
                eased
            );

            position.y +=
                Mathf.Sin(t * Mathf.PI) * arcHeight;

            float spin =
                t * spinDegrees * visual.spinDirection;

            Quaternion rotation =
                visual.startRotation *
                Quaternion.Euler(
                    spin,
                    spin * 0.7f,
                    spin * 0.3f
                );

            float scale = Mathf.Lerp(
                1f,
                finalScale,
                eased
            );

            visual.obj.transform.SetPositionAndRotation(
                position,
                rotation
            );

            visual.obj.transform.localScale =
                visual.startScale * scale;

            if (t >= 1f)
            {
                if (leafManager.CompletePickup(visual.leafId))
                {
                    if (inventory != null)
                        inventory.CompleteLeafPickup();
                }
                else if (inventory != null)
                {
                    inventory.CancelLeafPickup();
                }

                visual.obj.SetActive(false);
                flying.RemoveAt(i);
            }
        }
    }

    private void OnDisable()
    {
        ClearHighlight();

        foreach (Visual visual in flying)
        {
            if (leafManager != null)
                leafManager.CancelPickup(visual.leafId);

            if (inventory != null)
                inventory.CancelLeafPickup();

            visual.obj.SetActive(false);
        }

        flying.Clear();
    }

    private void OnDestroy()
    {
        if (highlight != null &&
            highlight.obj != null)
        {
            Destroy(highlight.obj);
        }

        foreach (Visual visual in pool)
        {
            if (visual.obj != null)
                Destroy(visual.obj);
        }
    }
}
