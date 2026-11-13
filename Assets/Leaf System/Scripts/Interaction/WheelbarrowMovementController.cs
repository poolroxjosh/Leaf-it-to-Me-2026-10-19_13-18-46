
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class WheelbarrowMovementController : MonoBehaviour
{
    [Header("References")]
    public Transform wheelbarrow;
    public Transform wheelPivot;
    public Transform handleGrip;
    public Camera playerCamera;
    public FirstPersonController firstPerson;
    public LeafShopUI shopUI;
    public PlayerToolController toolController;

    [Header("Interaction")]
    public float grabDistance = 1.6f;
    [Range(0f, 1f)]
    public float behindThreshold = 0.5f;
    public float maxLookDistance = 3f;
    public LayerMask interactionLayers = ~0;

    [Header("Movement")]
    public float forwardSpeed = 2f;
    public float reverseSpeed = 0.8f;
    public float acceleration = 2f;
    public float deceleration = 3.5f;
    public float turnSpeed = 55f;
    public float steeringAcceleration = 120f;

    [Header("Player Position")]
    public float distanceBehindHandles = 0.35f;

    [Header("Camera")]
    public float lookSensitivity = 0.2f;
    public float maxLookYaw = 35f;
    public float maxLookUp = 25f;
    public float maxLookDown = 40f;

    [Header("Collision")]
    public LayerMask obstacleLayers;
    public float collisionRadius = 0.3f;
    public float collisionHeight = 0.8f;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    [Header("Wheelbarrow Collision")]
    public Collider[] wheelbarrowColliders;

    [Min(0.01f)]
    public float collisionPadding = 0.03f;

    [Header("Player-Wheelbarrow Collision")]
    public bool ignorePlayerCollisionWhilePushing = true;

    public bool IsPushing { get; private set; }

    private CharacterController characterController;

    private float currentSpeed;
    private float currentTurnSpeed;
    private float lookYaw;
    private float lookPitch;

    private Vector3 localForward;
    private Vector3 localPlayerOffset;

    private readonly Collider[] collisionResults =
        new Collider[32];

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (wheelbarrow == null ||
            wheelPivot == null ||
            handleGrip == null)
            return;

        // Determine the actual wheelbarrow direction
        // from its handles toward its front wheel.
        Vector3 forward =
            wheelPivot.position - handleGrip.position;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            Debug.LogError(
                "Wheelbarrow: WheelPivot and HandleGrip " +
                "must have different horizontal positions."
            );
            enabled = false;
            return;
        }

        localForward = wheelbarrow.InverseTransformDirection(
            forward.normalized
        );

        localForward.y = 0f;
        localForward.Normalize();
    }

    private Vector3 Forward
    {
        get
        {
            Vector3 direction =
                wheelbarrow.TransformDirection(localForward);

            direction.y = 0f;
            return direction.normalized;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null ||
            wheelbarrow == null ||
            wheelPivot == null ||
            handleGrip == null ||
            playerCamera == null)
            return;

        if (shopUI != null && shopUI.IsOpen)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (IsPushing)
                ReleaseWheelbarrow();
            else if (CanGrabWheelbarrow())
                GrabWheelbarrow();
        }

        if (!IsPushing)
            return;

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        HandleCameraLook();
    }

    private void LateUpdate()
    {
        if (!IsPushing ||
            Cursor.lockState != CursorLockMode.Locked)
            return;

        HandleWheelbarrowMovement(Time.deltaTime);
    }

    public bool CanGrabWheelbarrow()
    {
        if (IsPushing ||
            wheelbarrow == null ||
            playerCamera == null ||
            handleGrip == null)
            return false;

        if (Cursor.lockState != CursorLockMode.Locked)
            return false;

        Vector3 toPlayer =
            transform.position - handleGrip.position;

        toPlayer.y = 0f;

        if (toPlayer.magnitude > grabDistance)
            return false;

        if (toPlayer.sqrMagnitude < 0.001f)
            return false;

        // Player must be behind the handles.
        float behind = Vector3.Dot(
            toPlayer.normalized,
            -Forward
        );

        if (behind < behindThreshold)
            return false;

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            maxLookDistance,
            interactionLayers,
            QueryTriggerInteraction.Ignore))
            return false;

        return hit.collider.transform == wheelbarrow ||
               hit.collider.transform.IsChildOf(wheelbarrow);
    }

    private void GrabWheelbarrow()
    {
        IsPushing = true;

        if (ignorePlayerCollisionWhilePushing)
        {
            SetPlayerWheelbarrowCollision(true);
        }

        currentSpeed = 0f;
        currentTurnSpeed = 0f;
        lookYaw = 0f;

        // Align the player with the wheelbarrow.
        transform.rotation = Quaternion.LookRotation(
            Forward,
            Vector3.up
        );

        float pitch = playerCamera.transform.localEulerAngles.x;

        if (pitch > 180f)
            pitch -= 360f;

        lookPitch = Mathf.Clamp(
            pitch,
            -maxLookUp,
            maxLookDown
        );

        localPlayerOffset = new Vector3(
            0f, 0f, -distanceBehindHandles
        );

        // Position player behind the handles.
        MovePlayerToHandles();

        ApplyCameraRotation();

        if (toolController != null)
            toolController.SetToolVisualsVisible(false);

        Debug.Log("Wheelbarrow grabbed.");
    }

    private void ReleaseWheelbarrow()
    {
        IsPushing = false;

        currentSpeed = 0f;
        currentTurnSpeed = 0f;

        if (firstPerson != null)
            firstPerson.SyncCameraPitch();

        if (toolController != null)
            toolController.SetToolVisualsVisible(true);

        if (ignorePlayerCollisionWhilePushing)
        {
            SetPlayerWheelbarrowCollision(false);
        }

        Debug.Log("Wheelbarrow released.");
    }

    private void HandleCameraLook()
    {
        if (Mouse.current == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        lookYaw += delta.x * lookSensitivity;
        lookPitch -= delta.y * lookSensitivity;

        lookYaw = Mathf.Clamp(
            lookYaw,
            -maxLookYaw,
            maxLookYaw
        );

        lookPitch = Mathf.Clamp(
            lookPitch,
            -maxLookUp,
            maxLookDown
        );

        ApplyCameraRotation();
    }

    private void ApplyCameraRotation()
    {
        playerCamera.transform.localRotation =
            Quaternion.Euler(
                lookPitch,
                lookYaw,
                0f
            );
    }

    private void HandleWheelbarrowMovement(float dt)
    {
        float forwardInput = 0f;
        float turnInput = 0f;

        if (Keyboard.current.wKey.isPressed)
            forwardInput += 1f;

        if (Keyboard.current.sKey.isPressed)
            forwardInput -= 1f;

        if (Keyboard.current.aKey.isPressed)
            turnInput -= 1f;

        if (Keyboard.current.dKey.isPressed)
            turnInput += 1f;

        float targetSpeed = forwardInput >= 0f
            ? forwardInput * forwardSpeed
            : forwardInput * reverseSpeed;

        float speedChange = Mathf.Abs(targetSpeed) >
                            Mathf.Abs(currentSpeed)
            ? acceleration
            : deceleration;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            speedChange * dt
        );

        float targetTurnSpeed =
            turnInput * turnSpeed;

        currentTurnSpeed = Mathf.MoveTowards(
            currentTurnSpeed,
            targetTurnSpeed,
            steeringAcceleration * dt
        );

        Vector3 pivot = wheelPivot.position;

        Vector3 movement =
            Forward * currentSpeed * dt;

        float rotationAmount =
            currentTurnSpeed * dt;

        // Predict the new pivot position and orientation.
        Vector3 newPivot = pivot + movement;

        Quaternion newRotation =
            Quaternion.AngleAxis(
                rotationAmount,
                Vector3.up
            ) * wheelbarrow.rotation;

        Vector3 pivotLocal =
            wheelbarrow.InverseTransformPoint(pivot);

        Vector3 newRootPosition =
            newPivot - newRotation * pivotLocal;

        // Reject movement if it would enter an obstacle.
        Vector3 proposedPlayerPosition =
            GetProposedPlayerPosition(
                newRootPosition,
                newRotation
            );

        bool wheelbarrowBlocked =
            IsWheelbarrowMovementBlocked(
                newRootPosition,
                newRotation
            );

        bool playerBlocked =
            IsPlayerMovementBlocked(
                proposedPlayerPosition
            );

        if (wheelbarrowBlocked || playerBlocked)
        {
            currentSpeed = 0f;
            currentTurnSpeed = 0f;
            return;
        }

        wheelbarrow.SetPositionAndRotation(
            newRootPosition,
            newRotation
        );

        Physics.SyncTransforms();

        transform.rotation = Quaternion.LookRotation(
            Forward,
            Vector3.up
        );

        MovePlayerToHandles();
    }

    private bool IsMovementBlocked(
        Vector3 proposedPosition,
        Quaternion proposedRotation)
    {
        // Initial conservative approximation:
        // test a volume around the wheelbarrow's body.
        Vector3 center =
            proposedPosition +
            proposedRotation *
            new Vector3(0f, collisionHeight, 0f);

        int count = Physics.OverlapBoxNonAlloc(
            center,
            new Vector3(
                collisionRadius,
                collisionHeight * 0.5f,
                collisionRadius
            ),
            collisionResults,
            proposedRotation,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );

        if (count >= collisionResults.Length)
            return true;

        for (int i = 0; i < count; i++)
        {
            Collider hit = collisionResults[i];

            if (hit == null)
                continue;

            // Ignore the wheelbarrow's own colliders.
            if (hit.transform == wheelbarrow ||
                hit.transform.IsChildOf(wheelbarrow))
                continue;

            // Ignore the player's own collider.
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
                continue;

            return true;
        }

        return false;
    }

    private void MovePlayerToHandles()
    {
        // Position the player behind the handles using
        // the wheelbarrow's calculated forward direction.
        Vector3 target =
            handleGrip.position -
            Forward * distanceBehindHandles;

        // For Phase 5A, retain the player's current height.
        target.y = transform.position.y;

        Vector3 delta = target - transform.position;

        characterController.Move(delta);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos ||
            wheelPivot == null ||
            handleGrip == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(wheelPivot.position, 0.12f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(handleGrip.position, 0.12f);

        Gizmos.DrawLine(
            handleGrip.position,
            wheelPivot.position
        );
    }

    private Vector3 GetProposedPlayerPosition(
        Vector3 proposedRootPosition,
        Quaternion proposedRotation)
    {
        Vector3 gripLocal =
            wheelbarrow.InverseTransformPoint(
                handleGrip.position
            );

        Vector3 proposedGrip =
            proposedRootPosition +
            proposedRotation * gripLocal;

        Vector3 proposedForward =
            proposedRotation * localForward;

        proposedForward.y = 0f;
        proposedForward.Normalize();

        Vector3 playerTarget =
            proposedGrip -
            proposedForward * distanceBehindHandles;

        playerTarget.y = transform.position.y;

        return playerTarget;
    }

    private bool IsPlayerMovementBlocked(
        Vector3 targetPosition)
    {
        Vector3 current = transform.position;

        Vector3 movement = targetPosition - current;

        float distance = movement.magnitude;

        if (distance < 0.0001f)
            return false;

        Vector3 center = current +
            characterController.center;

        float radius = characterController.radius;

        float halfHeight = Mathf.Max(
            0f,
            characterController.height * 0.5f - radius
        );

        Vector3 bottom =
            center + Vector3.down * halfHeight;

        Vector3 top =
            center + Vector3.up * halfHeight;

        return Physics.CapsuleCast(
            bottom,
            top,
            radius,
            movement.normalized,
            distance + collisionPadding,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private bool IsWheelbarrowMovementBlocked(
        Vector3 proposedPosition,
        Quaternion proposedRotation)
    {
        if (wheelbarrowColliders == null ||
            wheelbarrowColliders.Length == 0)
        {
            Debug.LogWarning(
                "Wheelbarrow has no collision shapes assigned!"
            );
            return false;
        }

        foreach (Collider col in wheelbarrowColliders)
        {
            if (col == null || !col.enabled || col.isTrigger)
                continue;

            if (col is BoxCollider box)
            {
                // Calculate the collider's local position
                // relative to the wheelbarrow root.
                Vector3 localCenter =
                    wheelbarrow.InverseTransformPoint(
                        box.transform.TransformPoint(box.center)
                    );

                Vector3 worldCenter =
                    proposedPosition +
                    proposedRotation * localCenter;

                // Combine wheelbarrow rotation with
                // the collider's local orientation.
                Quaternion localRotation =
                    Quaternion.Inverse(wheelbarrow.rotation) *
                    box.transform.rotation;

                Quaternion worldRotation =
                    proposedRotation * localRotation;

                // Account for collider and parent scaling.
                Vector3 scale = box.transform.lossyScale;

                Vector3 halfExtents =
                    Vector3.Scale(
                        box.size * 0.5f,
                        new Vector3(
                            Mathf.Abs(scale.x),
                            Mathf.Abs(scale.y),
                            Mathf.Abs(scale.z)
                        )
                    );

                halfExtents +=
                    Vector3.one * collisionPadding;

                if (Physics.CheckBox(
                    worldCenter,
                    halfExtents,
                    worldRotation,
                    obstacleLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void SetPlayerWheelbarrowCollision(bool ignore)
    {
        if (characterController == null ||
            wheelbarrowColliders == null)
            return;

        foreach (Collider col in wheelbarrowColliders)
        {
            if (col == null)
                continue;

            Physics.IgnoreCollision(
                characterController,
                col,
                ignore
            );
        }
    }
}