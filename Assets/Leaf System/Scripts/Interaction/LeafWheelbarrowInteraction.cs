
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class LeafWheelbarrowInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public LeafInventory inventory;
    public Transform pickupLocation;
    public LeafWheelbarrow wheelbarrow;
    public Transform dropoffLocation;
    public LeafSpawner leafSpawner;
    public Material leafMaterial;

    [Header("Interaction")]
    public float interactionRange = 3f;
    public LayerMask interactionLayers = ~0;

    [Header("Visuals")]
    [Range(1, 32)]
    public int maxVisualLeaves = 12;

    public float depositDuration = 0.65f;
    public float arcHeight = 0.7f;
    public float visualScale = 0.2f;

    private class FlyingVisual
    {
        public GameObject obj;
        public float elapsed;
        public float duration;
        public Vector3 start;
        public float phase;

        // Rotation animation
        public Quaternion startRotation;
        public Quaternion endRotation;
    }

    private readonly List<FlyingVisual> visuals =
        new List<FlyingVisual>();

    private readonly List<FlyingVisual> active =
        new List<FlyingVisual>();

    private Mesh leafMesh;
    private float leafBaseScale = 1f;
    private Quaternion leafMeshRotation = Quaternion.identity;
    private MaterialPropertyBlock properties;

    private static readonly int LeafColorID =
        Shader.PropertyToID("_LeafColor");

    public bool IsAimingAtWheelbarrow()
    {
        if (playerCamera == null || wheelbarrow == null)
            return false;

        if (Cursor.lockState != CursorLockMode.Locked)
            return false;

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionRange,
            interactionLayers,
            QueryTriggerInteraction.Ignore))
            return false;

        LeafWheelbarrow target =
            hit.collider.GetComponentInParent<LeafWheelbarrow>();

        return target == wheelbarrow;
    }

    private void Start()
    {
        properties = new MaterialPropertyBlock();

        if (leafSpawner != null &&
            leafSpawner.variants != null)
        {
            foreach (LeafVariant variant in leafSpawner.variants)
            {
                if (variant == null || variant.prefab == null)
                    continue;

                MeshFilter filter =
                    variant.prefab.GetComponent<MeshFilter>();

                if (filter != null && filter.sharedMesh != null)
                {
                    leafMesh = filter.sharedMesh;

                    // Match the scale used by our leaf spawner.
                    leafBaseScale = Mathf.Max(
                        0.01f,
                        (variant.sizeMultiplier.x +
                        variant.sizeMultiplier.y) * 0.5f
                    );

                    // Match the orientation used by our renderer.
                    leafMeshRotation = Quaternion.Euler(
                        variant.meshRotationOffset
                    );

                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            IsAimingAtWheelbarrow())
        {
            TryDeposit();
        }

        AnimateVisuals();
    }

    private void TryDeposit()
    {
        if (inventory == null || wheelbarrow == null)
            return;

        if (inventory.CurrentLeaves <= 0)
            return;

        int amount = inventory.CurrentLeaves;

        int accepted = wheelbarrow.Deposit(amount);

        if (accepted <= 0)
            return;

        inventory.RemoveLeaves(accepted);

        Debug.Log(
            $"Deposited {accepted} leaves. " +
            $"Wheelbarrow total: {wheelbarrow.StoredLeaves:N0}"
        );

        SpawnDepositVisuals(accepted);
    }

    private void SpawnDepositVisuals(int amount)
    {
        if (leafMesh == null ||
            leafMaterial == null ||
            pickupLocation == null ||
            dropoffLocation == null)
            return;

        int count = Mathf.Min(amount, maxVisualLeaves);

        for (int i = 0; i < count; i++)
        {
            FlyingVisual visual = GetVisual();

            visual.elapsed = 0f;
            visual.duration = Mathf.Max(
                0.05f,
                depositDuration + Random.Range(-0.1f, 0.1f)
            );

            visual.start = pickupLocation.position;
            visual.phase = Random.Range(0f, Mathf.PI * 2f);

            // Start facing a completely random direction.
            visual.startRotation = Random.rotationUniform;

            // End flat and right-side up, with a random yaw.
            visual.endRotation =
                Quaternion.Euler(
                    0f,
                    Random.Range(0f, 360f),
                    0f
                ) * leafMeshRotation;

            // Apply the starting rotation immediately.
            visual.obj.transform.rotation = visual.startRotation;

            visual.obj.transform.position = visual.start;
            visual.obj.transform.localScale =
                Vector3.one * leafBaseScale * visualScale;

            visual.obj.SetActive(true);
            active.Add(visual);
        }
    }

    private FlyingVisual GetVisual()
    {
        foreach (FlyingVisual visual in visuals)
        {
            if (!visual.obj.activeSelf)
                return visual;
        }

        GameObject obj = new GameObject("Deposit Leaf");
        obj.transform.SetParent(transform, true);

        MeshFilter filter = obj.AddComponent<MeshFilter>();
        filter.sharedMesh = leafMesh;

        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = leafMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        properties.Clear();

        Color color = Color.Lerp(
            new Color(0.8f, 0.3f, 0.1f),
            new Color(1f, 0.8f, 0.2f),
            Random.value
        );

        properties.SetColor(LeafColorID, color);
        renderer.SetPropertyBlock(properties);

        obj.SetActive(false);

        FlyingVisual result = new FlyingVisual
        {
            obj = obj
        };

        visuals.Add(result);
        return result;
    }

    private void AnimateVisuals()
    {
        if (dropoffLocation == null)
            return;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            FlyingVisual visual = active[i];

            visual.elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                visual.elapsed / visual.duration
            );

            float eased = t * t * (3f - 2f * t);

            Vector3 position = Vector3.Lerp(
                visual.start,
                dropoffLocation.position,
                eased
            );

            position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            position.x +=
                Mathf.Sin(t * Mathf.PI * 2f + visual.phase) *
                0.12f * (1f - t);

            visual.obj.transform.position = position;

            // Smoothly transition from a random orientation
            // to a flat, right-side-up leaf.
            float rotationProgress = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            visual.obj.transform.rotation =
                Quaternion.Slerp(
                    visual.startRotation,
                    visual.endRotation,
                    rotationProgress
                );

            visual.obj.transform.localScale =
                Vector3.one *
                leafBaseScale *
                visualScale *
                Mathf.Lerp(1f, 0.15f, eased);

            if (t >= 1f)
            {
                visual.obj.SetActive(false);
                active.RemoveAt(i);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (FlyingVisual visual in visuals)
        {
            if (visual.obj != null)
                Destroy(visual.obj);
        }
    }
}
