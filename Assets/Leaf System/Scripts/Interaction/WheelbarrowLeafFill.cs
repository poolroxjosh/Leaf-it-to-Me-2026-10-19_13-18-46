using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class WheelbarrowLeafFill : MonoBehaviour
{
    [Header("References")]
    public LeafWheelbarrow wheelbarrow;

    [Header("Fill Animation")]
    [Min(0.01f)]
    public float fillAnimationSpeed = 0.5f;

    [Header("Rounded Surface")]
    public WheelbarrowLeafSurface leafSurface;

    private LeafManager leafManager;
    private MeshRenderer pileRenderer;
    private MaterialPropertyBlock propertyBlock;

    private int initialLeafCount;
    private float currentFill;
    private float targetFill;
    private float lastAppliedFill = -1f;

    private static readonly int FillAmountID =
        Shader.PropertyToID("_FillAmount");

    private static readonly int BottomHeightID =
        Shader.PropertyToID("_BottomHeight");

    private static readonly int TopHeightID =
        Shader.PropertyToID("_TopHeight");

    private void Awake()
    {

        if (leafSurface == null)
        {
            leafSurface =
                GetComponentInParent<WheelbarrowLeafSurface>();

            if (leafSurface == null)
            {
                leafSurface =
                    GetComponentInParent<LeafWheelbarrow>()
                        ?.GetComponentInChildren<WheelbarrowLeafSurface>(true);
            }
        }

        pileRenderer = GetComponent<MeshRenderer>();

        if (wheelbarrow == null)
            wheelbarrow = GetComponentInParent<LeafWheelbarrow>();

        leafManager = FindAnyObjectByType<LeafManager>();

        propertyBlock = new MaterialPropertyBlock();

        // Hide the pile immediately at startup.
        pileRenderer.enabled = false;
    }

    private void OnEnable()
    {
        if (leafManager == null)
            leafManager = FindAnyObjectByType<LeafManager>();

        if (leafManager != null)
            leafManager.OnBulkSpawnCompleted += InitializeLeafCount;
    }

    private void Start()
    {
        // Handles cases where spawning completed
        // before this component started.
        if (leafManager != null &&
            leafManager.leaves.Count > 0)
        {
            InitializeLeafCount();
        }

        UpdateTargetFill();
        ApplyFill(true);
    }

    private void OnDisable()
    {
        if (leafManager != null)
            leafManager.OnBulkSpawnCompleted -= InitializeLeafCount;
    }

    private void InitializeLeafCount()
    {
        if (leafManager == null)
            return;

        initialLeafCount = leafManager.leaves.Count;
        UpdateTargetFill();
    }

    private void Update()
    {
        if (wheelbarrow == null || pileRenderer == null)
            return;

        UpdateTargetFill();

        currentFill = Mathf.MoveTowards(
            currentFill,
            targetFill,
            fillAnimationSpeed * Time.deltaTime
        );

        if (Mathf.Abs(currentFill - lastAppliedFill) > 0.00001f)
            ApplyFill(false);
    }

    private void UpdateTargetFill()
    {
        if (wheelbarrow == null || initialLeafCount <= 0)
        {
            targetFill = 0f;
            return;
        }

        targetFill = Mathf.Clamp01(
            (float)wheelbarrow.StoredLeaves /
            initialLeafCount
        );
    }

    private void ApplyFill(bool force)
    {
        if (pileRenderer == null)
            return;

        bool hasLeaves = currentFill > 0.000001f;

        if (pileRenderer.enabled != hasLeaves)
            pileRenderer.enabled = hasLeaves;

        if (!force &&
            Mathf.Approximately(currentFill, lastAppliedFill))
            return;

        // Use world-space bounds because the
        // imported FBX is rotated relative to Unity.
        Bounds bounds = pileRenderer.bounds;

        pileRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat(
            FillAmountID,
            currentFill
        );

        propertyBlock.SetFloat(
            BottomHeightID,
            bounds.min.y
        );

        propertyBlock.SetFloat(
            TopHeightID,
            bounds.max.y
        );

        if (leafSurface != null)
        {
            leafSurface.SetFill(currentFill);
        }

        pileRenderer.SetPropertyBlock(propertyBlock);

        lastAppliedFill = currentFill;
    }
}