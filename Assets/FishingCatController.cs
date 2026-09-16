using UnityEngine;

public class FishingCatController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 6f;

    [Header("Third Person Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float cameraDistance = 6f;
    [SerializeField] private float cameraLookHeight = 1.2f;
    [SerializeField] private float cameraFollowSpeed = 12f;
    [SerializeField] private float cameraSensitivity = 3f;
    [SerializeField] private float cameraMinPitch = -15f;
    [SerializeField] private float cameraMaxPitch = 55f;
    [SerializeField] private LayerMask cameraCollisionLayer = ~0;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Grappling")]
    [SerializeField] private LayerMask grappleLayer;
    [SerializeField] private float grappleRange = 30f;
    [SerializeField] private float grappleSpeed = 25f;
    [SerializeField] private float ropeLength = 2f;
    [SerializeField] private KeyCode grappleKey = KeyCode.Mouse0;
    [SerializeField] private float grappleCooldown = 3f;
    [SerializeField] private float wallClimbSpeed = 4f;
    [SerializeField] private float wallClimbStamina = 5f;
    [SerializeField] private float wallClimbStaminaDrain = 1f;
    [SerializeField] private float wallClimbStaminaRecovery = 1.5f;
    [SerializeField] private float wallAngleLimit = 55f;

    [Header("Fishing")]
    [SerializeField] private Transform rodTip;
    [SerializeField] private LayerMask fishingLayer;
    [SerializeField] private float fishingRange = 25f;
    [SerializeField] private float biteDelay = 1.5f;
    [SerializeField] private float fishingLineLength = 25f;
    [SerializeField] private float reelSpeed = 4f;
    [SerializeField] private float maxLineTension = 0.92f;
    [SerializeField] private KeyCode castFishingKey = KeyCode.F;
    [SerializeField] private KeyCode reelFishingKey = KeyCode.R;

    private Rigidbody rb;
    private bool isGrounded;
    private bool isGrappling;
    private bool isWallClimbing;
    private Vector3 grapplePoint;
    private LineRenderer ropeVisual;
    private float cameraYaw;
    private float cameraPitch = 15f;
    private float nextGrappleTime;
    private float currentWallClimbStamina;
    private FishingFish hookedFish;
    private LineRenderer fishingLineVisual;
    private FishingState fishingState;
    private float biteTimer;
    private float fishingTension;

    private const float RAYCAST_BUFFER = 0.1f;

    private enum FishingState
    {
        Idle,
        WaitingForBite,
        Hooked
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera != null)
        {
            cameraYaw = transform.eulerAngles.y;
        }

        currentWallClimbStamina = wallClimbStamina;
        InitializeRopeVisual();
        InitializeFishingLineVisual();
    }

    void Update()
    {
        CheckGround();
        HandleJump();
        HandleGrappleInput();
        HandleFishingInput();
        HandleCameraInput();
        UpdateRopeVisual();
        UpdateFishingLineVisual();
    }

    void FixedUpdate()
    {
        if (!isGrappling)
        {
            HandleMovement();
            RecoverWallClimbStamina();
        }
        else
        {
            HandleGrappleMovement();
        }

        SimulateFishing();
    }

    void LateUpdate()
    {
        UpdateThirdPersonCamera();
    }

    #region Movement

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Transform cameraTransform = playerCamera != null ? playerCamera.transform : transform;
        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 movement = (cameraRight * horizontal + cameraForward * vertical).normalized;
        Vector3 targetVelocity = movement * moveSpeed;

        rb.linearVelocity = new Vector3(
            targetVelocity.x,
            rb.linearVelocity.y,
            targetVelocity.z
        );
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void CheckGround()
    {
        Vector3 rayStartPos = transform.position + Vector3.up * RAYCAST_BUFFER;
        isGrounded = Physics.Raycast(
            rayStartPos,
            Vector3.down,
            groundCheckDistance,
            groundLayer
        );
    }

    #endregion

    #region Fishing

    void HandleFishingInput()
    {
        if (Input.GetKeyDown(castFishingKey) && fishingState == FishingState.Idle)
        {
            TryCastFishingLine();
        }

        if (Input.GetKeyDown(reelFishingKey) && fishingState == FishingState.WaitingForBite)
        {
            ReleaseFishingLine();
        }

        if (Input.GetKeyDown(KeyCode.G) && fishingState != FishingState.Idle)
        {
            ReleaseFishingLine();
        }
    }

    void TryCastFishingLine()
    {
        Camera fishingCamera = playerCamera != null ? playerCamera : Camera.main;
        if (fishingCamera == null)
        {
            return;
        }

        Vector3 origin = rodTip != null ? rodTip.position : fishingCamera.transform.position;
        Ray ray = new Ray(fishingCamera.transform.position, fishingCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, fishingRange, fishingLayer, QueryTriggerInteraction.Ignore))
        {
            FishingFish fish = hit.collider.GetComponentInParent<FishingFish>();
            if (fish != null)
            {
                hookedFish = fish;
                hookedFish.ResetFight();
                fishingState = FishingState.WaitingForBite;
                biteTimer = biteDelay;
                grapplePoint = hit.point;
                return;
            }
        }

        fishingState = FishingState.WaitingForBite;
        biteTimer = biteDelay;
        grapplePoint = origin + fishingCamera.transform.forward * fishingRange;
    }

    void SimulateFishing()
    {
        if (fishingState == FishingState.Idle)
        {
            fishingTension = 0f;
            return;
        }

        if (fishingState == FishingState.WaitingForBite)
        {
            biteTimer -= Time.deltaTime;
            if (hookedFish != null && biteTimer <= 0f)
            {
                fishingState = FishingState.Hooked;
            }
            return;
        }

        if (hookedFish == null)
        {
            ReleaseFishingLine();
            return;
        }

        bool isReeling = Input.GetKey(reelFishingKey);
        float lineLength = Mathf.Max(0.1f, fishingLineLength);
        fishingTension = hookedFish.SimulateFight(
            GetFishingOrigin(),
            isReeling,
            reelSpeed,
            lineLength,
            Time.deltaTime);

        if (fishingTension >= maxLineTension)
        {
            ReleaseFishingLine();
            return;
        }

        if (isReeling && hookedFish.IsTired &&
            Vector3.Distance(GetFishingOrigin(), hookedFish.transform.position) <= hookedFish.CatchDistance)
        {
            CatchFish();
        }
    }

    Vector3 GetFishingOrigin()
    {
        return rodTip != null ? rodTip.position : transform.position;
    }

    void CatchFish()
    {
        fishingState = FishingState.Idle;
        FishingFish caughtFish = hookedFish;
        hookedFish = null;
        fishingTension = 0f;
        Destroy(caughtFish.gameObject);
    }

    void ReleaseFishingLine()
    {
        fishingState = FishingState.Idle;
        hookedFish = null;
        fishingTension = 0f;
    }

    void InitializeFishingLineVisual()
    {
        fishingLineVisual = gameObject.AddComponent<LineRenderer>();
        fishingLineVisual.material = new Material(Shader.Find("Standard"));
        fishingLineVisual.startWidth = 0.035f;
        fishingLineVisual.endWidth = 0.02f;
        fishingLineVisual.positionCount = 2;
        fishingLineVisual.enabled = false;
    }

    void UpdateFishingLineVisual()
    {
        if (fishingLineVisual == null)
        {
            return;
        }

        if (fishingState == FishingState.Idle)
        {
            fishingLineVisual.enabled = false;
            return;
        }

        fishingLineVisual.enabled = true;
        fishingLineVisual.SetPosition(0, GetFishingOrigin());
        fishingLineVisual.SetPosition(1, hookedFish != null ? hookedFish.transform.position : grapplePoint);
    }

    #endregion

    #region Third Person Camera

    void HandleCameraInput()
    {
        if (playerCamera == null || !Input.GetMouseButton(1))
            return;

        cameraYaw += Input.GetAxis("Mouse X") * cameraSensitivity;
        cameraPitch -= Input.GetAxis("Mouse Y") * cameraSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, cameraMinPitch, cameraMaxPitch);
    }

    void UpdateThirdPersonCamera()
    {
        if (playerCamera == null)
            return;

        Vector3 lookTarget = transform.position + Vector3.up * cameraLookHeight;
        Quaternion cameraRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        Vector3 desiredPosition = lookTarget - cameraRotation * Vector3.forward * cameraDistance;

        Vector3 castDirection = desiredPosition - lookTarget;
        float desiredDistance = castDirection.magnitude;
        Vector3 castOrigin = lookTarget + castDirection.normalized * 0.3f;
        if (Physics.SphereCast(
            castOrigin,
            0.2f,
            castDirection.normalized,
            out RaycastHit hit,
            desiredDistance - 0.3f,
            cameraCollisionLayer,
            QueryTriggerInteraction.Ignore))
        {
            desiredPosition = lookTarget + castDirection.normalized * Mathf.Max(0.5f, hit.distance - 0.2f);
        }

        float followFactor = 1f - Mathf.Exp(-cameraFollowSpeed * Time.deltaTime);
        playerCamera.transform.position = Vector3.Lerp(
            playerCamera.transform.position,
            desiredPosition,
            followFactor);
        playerCamera.transform.rotation = Quaternion.Slerp(
            playerCamera.transform.rotation,
            Quaternion.LookRotation(lookTarget - playerCamera.transform.position),
            followFactor);
    }

    #endregion

    #region Grappling

    void HandleGrappleInput()
    {
        if (Input.GetKeyDown(grappleKey) && Time.time >= nextGrappleTime)
        {
            if (TryStartGrapple())
            {
                isGrappling = true;
                nextGrappleTime = Time.time + grappleCooldown;
            }
        }

        if (Input.GetKeyUp(grappleKey))
        {
            ReleaseGrapple();
        }
    }

    bool TryStartGrapple()
    {
        Camera grappleCamera = playerCamera != null ? playerCamera : Camera.main;
        Vector3 rayOrigin = grappleCamera != null ? grappleCamera.transform.position : transform.position;
        Ray ray = new Ray(rayOrigin, GetCameraForwardDirection());

        if (Physics.Raycast(ray, out RaycastHit hit, grappleRange, grappleLayer, QueryTriggerInteraction.Ignore))
        {
            grapplePoint = hit.point;
            isWallClimbing = IsWallSurface(hit.normal);
            return true;
        }

        return false;
    }

    Vector3 GetCameraForwardDirection()
    {
        Camera grappleCamera = playerCamera != null ? playerCamera : Camera.main;
        return grappleCamera != null ? grappleCamera.transform.forward : transform.forward;
    }

    void HandleGrappleMovement()
    {
        Vector3 directionToGrapple = (grapplePoint - transform.position).normalized;
        float distanceToGrapple = Vector3.Distance(transform.position, grapplePoint);
        float effectiveRopeLength = Mathf.Max(0.1f, ropeLength);

        float speedMultiplier = Mathf.Clamp01(distanceToGrapple / effectiveRopeLength);
        Vector3 grappleVelocity = directionToGrapple * grappleSpeed * speedMultiplier;

        if (isWallClimbing)
        {
            float climbInput = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(climbInput) > 0.01f && currentWallClimbStamina > 0f)
            {
                currentWallClimbStamina = Mathf.Max(
                    0f,
                    currentWallClimbStamina - wallClimbStaminaDrain * Time.fixedDeltaTime);
                grappleVelocity += Vector3.up * climbInput * wallClimbSpeed;
            }

            if (currentWallClimbStamina <= 0f)
            {
                ReleaseGrapple();
            }
        }

        rb.linearVelocity = grappleVelocity;

        if (!isWallClimbing && distanceToGrapple < effectiveRopeLength)
        {
            ReleaseGrapple();
        }
    }

    void ReleaseGrapple()
    {
        isGrappling = false;
        isWallClimbing = false;
    }

    bool IsWallSurface(Vector3 surfaceNormal)
    {
        return Vector3.Angle(surfaceNormal, Vector3.up) >= wallAngleLimit;
    }

    void RecoverWallClimbStamina()
    {
        currentWallClimbStamina = Mathf.MoveTowards(
            currentWallClimbStamina,
            wallClimbStamina,
            wallClimbStaminaRecovery * Time.fixedDeltaTime);
    }

    void InitializeRopeVisual()
    {
        ropeVisual = GetComponent<LineRenderer>();
        if (ropeVisual == null)
        {
            ropeVisual = gameObject.AddComponent<LineRenderer>();
            ropeVisual.material = new Material(Shader.Find("Standard"));
            ropeVisual.startWidth = 0.1f;
            ropeVisual.endWidth = 0.1f;
        }
        ropeVisual.enabled = false;
    }

    void UpdateRopeVisual()
    {
        if (isGrappling && ropeVisual != null)
        {
            ropeVisual.enabled = true;
            ropeVisual.SetPosition(0, transform.position);
            ropeVisual.SetPosition(1, grapplePoint);
        }
        else if (ropeVisual != null)
        {
            ropeVisual.enabled = false;
        }
    }

    #endregion
}

public class FishingFish : MonoBehaviour
{
    [Header("Fish Behavior")]
    [SerializeField] private float stamina = 100f;
    [SerializeField] private float escapeForce = 5f;
    [SerializeField] private float escapeChangeTime = 1.5f;
    [SerializeField] private float staminaDrainWhileReeling = 14f;
    [SerializeField] private float staminaRecovery = 5f;
    [SerializeField] private float catchDistance = 1.4f;

    private float currentStamina;
    private float escapeTimer;
    private Vector3 escapeDirection;

    public bool IsTired => currentStamina <= 0.01f;
    public float CatchDistance => catchDistance;

    void Awake()
    {
        currentStamina = stamina;
        PickEscapeDirection(Vector3.forward);
    }

    public void ResetFight()
    {
        currentStamina = stamina;
        escapeTimer = 0f;
    }

    public float SimulateFight(Vector3 rodPosition, bool isReeling, float reelSpeed, float maxLineLength, float deltaTime)
    {
        Vector3 awayFromRod = transform.position - rodPosition;
        awayFromRod.y = 0f;
        if (awayFromRod.sqrMagnitude < 0.01f)
        {
            awayFromRod = Vector3.forward;
        }

        escapeTimer -= deltaTime;
        if (escapeTimer <= 0f)
        {
            PickEscapeDirection(awayFromRod.normalized);
        }

        if (isReeling)
        {
            currentStamina = Mathf.Max(0f, currentStamina - staminaDrainWhileReeling * deltaTime);
        }
        else
        {
            currentStamina = Mathf.Min(stamina, currentStamina + staminaRecovery * deltaTime);
        }

        float strength = IsTired ? 0.15f : Mathf.Lerp(0.25f, 1f, stamina <= 0f ? 0f : currentStamina / stamina);
        Vector3 movement = escapeDirection * escapeForce * strength;

        if (isReeling && Vector3.Distance(transform.position, rodPosition) > catchDistance)
        {
            movement += (rodPosition - transform.position).normalized * reelSpeed;
        }

        transform.position += movement * deltaTime;
        float distance = Vector3.Distance(transform.position, rodPosition);
        float distanceTension = Mathf.Clamp01(distance / Mathf.Max(0.1f, maxLineLength));
        float escapeTension = strength * 0.35f;
        return Mathf.Clamp01(distanceTension + escapeTension);
    }

    private void PickEscapeDirection(Vector3 awayFromRod)
    {
        Vector3 sideways = Vector3.Cross(Vector3.up, awayFromRod).normalized;
        escapeDirection = (awayFromRod + sideways * Random.Range(-0.7f, 0.7f)).normalized;
        escapeTimer = escapeChangeTime * Random.Range(0.7f, 1.3f);
    }
}