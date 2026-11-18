
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class LeafTarpController : MonoBehaviour
{
    [Header("References")]
    public PlayerToolController toolController;
    public LeafManager leafManager;
    public LeafShopUI shopUI;
    public WheelbarrowMovementController wheelbarrowMovement;
    public Camera playerCamera;
    public Transform toolHolder;

    [Header("Prefabs")]
    public LeafTarpSurface deployedTarpPrefab;
    public GameObject bundledBagPrefab;

    [Header("Placement")]
    public LayerMask groundLayers;
    public LayerMask obstacleLayers;
    public float placementRange = 5f;
    public float maxPlacementSlope = 20f;
    public float placementOffset = 0.025f;

    [Header("Gathering")]
    public float gatherRange = 3f;
    public LayerMask tarpInteractionLayers;

    [Header("Carried Bag")]
    public Vector3 bagLocalPosition =
        new Vector3(0.35f, -0.3f, 0.5f);

    [Header("Placement Hologram")]
    public GameObject hologramPrefab;

    public Color validHologramColor =
        new Color(0.3f, 1f, 0.4f, 0.4f);

    public Color invalidHologramColor =
        new Color(1f, 0.25f, 0.25f, 0.4f);

    [Min(0f)]
    public float hologramHeightOffset = 0.01f;

    public LeafTarpSurface ActiveTarp { get; private set; }

    public bool IsDeployed =>
        ActiveTarp != null && ActiveTarp.IsDeployed;

    public bool IsCarryingBag { get; private set; }

    public int BagLeafCount { get; private set; }

    private GameObject carriedBag;
    private TMP_Text bagCountText;

    private readonly List<int> capturedIds =
        new List<int>(2048);

    private GameObject hologramInstance;
    private Renderer[] hologramRenderers;

    private Vector3 previewPosition;
    private Quaternion previewRotation;
    private bool previewValid;

    private MaterialPropertyBlock hologramProperties;

    private void Update()
    {
        // Always update the hologram first.
        // This also handles hiding it when necessary.
        UpdateHologram();

        if (toolController == null ||
            leafManager == null ||
            playerCamera == null)
            return;

        if (!toolController.HasTarp)
            return;

        if (shopUI != null && shopUI.IsOpen)
            return;

        if (wheelbarrowMovement != null &&
            wheelbarrowMovement.IsPushing)
            return;

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        // Gather the deployed tarp with E.
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame &&
            CanGatherTarp())
        {
            TryGatherTarp();
        }

        if (IsCarryingBag || IsDeployed)
            return;

        if (toolController.EquippedTool != PlayerTool.Tarp)
            return;

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            previewValid)
        {
            TryPlaceTarp();
        }
    }

    private void TryPlaceTarp()
    {
        if (!previewValid ||
            deployedTarpPrefab == null ||
            leafManager == null)
            return;

        ActiveTarp = Instantiate(
            deployedTarpPrefab,
            previewPosition,
            previewRotation
        );

        ActiveTarp.leafManager = leafManager;
        leafManager.activeTarp = ActiveTarp;

        ActiveTarp.Deploy();

        SetHologramVisible(false);
        previewValid = false;

        Debug.Log("Tarp placed successfully.");
    }

    private void TryGatherTarp()
    {
        if (ActiveTarp == null)
            return;

        if (!CanGatherTarp())
            return;

        if (bundledBagPrefab == null ||
            toolHolder == null)
        {
            Debug.LogWarning(
                "Tarp bag prefab or ToolHolder missing."
            );
            return;
        }

        GameObject newBag = Instantiate(
            bundledBagPrefab,
            toolHolder,
            false
        );

        if (newBag == null)
            return;

        // Keep the prefab's original rotation and scale.
        // Only override its position relative to ToolHolder.
        newBag.transform.localPosition = bagLocalPosition;

        // Snapshot the leaves captured by the tarp.
        ActiveTarp.GetCapturedLeaves(capturedIds);

        int gathered = 0;

        foreach (int id in capturedIds)
        {
            if (leafManager.CollectTarpLeaf(id))
                gathered++;
        }

        BagLeafCount = gathered;

        // Release the leaves that were underneath.
        ActiveTarp.ClearSurface();

        if (leafManager.activeTarp == ActiveTarp)
            leafManager.activeTarp = null;

        Destroy(ActiveTarp.gameObject);
        ActiveTarp = null;

        carriedBag = newBag;

        bagCountText =
            carriedBag.GetComponentInChildren<TMP_Text>(
                true
            );

        UpdateBagLabel();

        IsCarryingBag = true;
        toolController.SetCarryingTarp(true);

        Debug.Log(
            $"Tarp gathered with {BagLeafCount} leaves."
        );
    }

    private void UpdateBagLabel()
    {
        if (bagCountText != null)
            bagCountText.text = BagLeafCount.ToString("N0");
    }

    public int DepositIntoWheelbarrow(
        LeafWheelbarrow wheelbarrow,
        LeafWallet wallet)
    {
        if (!IsCarryingBag ||
            wheelbarrow == null ||
            wallet == null)
            return 0;

        // An empty bag can also be put away.
        if (BagLeafCount == 0)
        {
            FinishDeposit();
            return 0;
        }

        int accepted =
            wheelbarrow.Deposit(BagLeafCount);

        if (accepted <= 0)
            return 0;

        wallet.EarnFromLeaves(accepted);

        BagLeafCount -= accepted;
        UpdateBagLabel();

        Debug.Log(
            $"Tarp deposited {accepted} leaves."
        );

        if (BagLeafCount == 0)
            FinishDeposit();

        return accepted;
    }

    private void FinishDeposit()
    {
        IsCarryingBag = false;
        BagLeafCount = 0;

        if (carriedBag != null)
            Destroy(carriedBag);

        carriedBag = null;
        bagCountText = null;

        toolController.SetCarryingTarp(false);

        Debug.Log("Tarp emptied and ready to reuse.");
    }

    private void Start()
    {
        if (hologramPrefab == null)
            return;

        hologramInstance = Instantiate(hologramPrefab);

        hologramInstance.name = "Tarp Placement Hologram";

        hologramRenderers =
            hologramInstance.GetComponentsInChildren<Renderer>();

        hologramProperties = new MaterialPropertyBlock();

        hologramInstance.SetActive(false);
    }

    private void SetHologramVisible(bool visible)
    {
        if (hologramInstance != null &&
            hologramInstance.activeSelf != visible)
        {
            hologramInstance.SetActive(visible);
        }
    }

    private bool CalculatePlacement(
        out Vector3 position,
        out Quaternion rotation,
        out bool valid)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        valid = false;

        if (playerCamera == null ||
            deployedTarpPrefab == null)
            return false;

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        int rayMask =
            groundLayers.value | obstacleLayers.value;

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            placementRange,
            rayMask,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Vector3 forward = playerCamera.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;

        rotation = Quaternion.LookRotation(
            forward.normalized,
            Vector3.up
        );

        position =
            hit.point + Vector3.up * placementOffset;

        int hitLayer = 1 << hit.collider.gameObject.layer;

        bool validGround =
            (groundLayers.value & hitLayer) != 0;

        bool validSlope =
            Vector3.Angle(hit.normal, Vector3.up) <=
            maxPlacementSlope;

        // Account for the deployed tarp prefab's scale.
        Vector3 tarpScale =
            deployedTarpPrefab.transform.lossyScale;

        float halfWidth =
            deployedTarpPrefab.width *
            Mathf.Abs(tarpScale.x) * 0.5f;

        float halfLength =
            deployedTarpPrefab.length *
            Mathf.Abs(tarpScale.z) * 0.5f;

        // Small inset to avoid rejecting tiny edge contacts.
        float edgeInset = 0.025f;

        halfWidth = Mathf.Max(0.01f, halfWidth - edgeInset);
        halfLength = Mathf.Max(0.01f, halfLength - edgeInset);

        // The tarp is flat, so use a thin collision volume.
        float thickness = 0.04f;

        bool blocked = Physics.CheckBox(
            position + Vector3.up * (thickness * 0.5f),
            new Vector3(
                halfWidth,
                thickness * 0.5f,
                halfLength
            ),
            rotation,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );

        valid = validGround &&
                validSlope &&
                !blocked;

        return true;
    }

    private void UpdateHologram()
    {
        if (hologramInstance == null)
            return;

        bool canPreview =
            toolController != null &&
            toolController.HasTarp &&
            toolController.EquippedTool == PlayerTool.Tarp &&
            !IsDeployed &&
            !IsCarryingBag &&
            (shopUI == null || !shopUI.IsOpen) &&
            (wheelbarrowMovement == null ||
            !wheelbarrowMovement.IsPushing) &&
            Cursor.lockState == CursorLockMode.Locked;

        if (!canPreview)
        {
            SetHologramVisible(false);
            previewValid = false;
            return;
        }

        if (!CalculatePlacement(
            out previewPosition,
            out previewRotation,
            out previewValid))
        {
            previewValid = false;
            SetHologramVisible(false);
            return;
        }

        hologramInstance.transform.SetPositionAndRotation(
            previewPosition +
            Vector3.up * hologramHeightOffset,
            previewRotation
        );

        SetHologramVisible(true);

        Color color = previewValid
            ? validHologramColor
            : invalidHologramColor;

        foreach (Renderer renderer in hologramRenderers)
        {
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(hologramProperties);

            hologramProperties.SetColor("_BaseColor", color);
            hologramProperties.SetColor("_Color", color);

            renderer.SetPropertyBlock(hologramProperties);
        }
    }

    private void OnDestroy()
    {
        if (hologramInstance != null)
            Destroy(hologramInstance);
    }

    public bool CanGatherTarp()
    {
        if (!IsDeployed ||
            IsCarryingBag ||
            playerCamera == null)
            return false;

        if (shopUI != null && shopUI.IsOpen)
            return false;

        if (wheelbarrowMovement != null &&
            wheelbarrowMovement.IsPushing)
            return false;

        if (Cursor.lockState != CursorLockMode.Locked)
            return false;

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            gatherRange,
            tarpInteractionLayers,
            QueryTriggerInteraction.Collide))
            return false;

        LeafTarpSurface hitTarp =
            hit.collider.GetComponentInParent<LeafTarpSurface>();

        return hitTarp == ActiveTarp;
    }
}