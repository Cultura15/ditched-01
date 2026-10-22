using UnityEngine;

/// <summary>Smooth WASD movement with acceleration, coyote time, jump buffering, crouch,
/// and a third-person (or first-person) mouse-look camera.</summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public enum CameraMode { ThirdPerson, FirstPerson }

    public Camera playerCamera;
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    [Header("Movement Feel")]
    [SerializeField, Min(0f), Tooltip("How fast the player reaches full speed (m/s per second).")]
    private float _acceleration = 45f;
    [SerializeField, Min(0f), Tooltip("How fast the player stops when keys are released.")]
    private float _deceleration = 60f;
    [SerializeField, Range(0f, 1f)] private float _airControl = 0.5f;
    [SerializeField, Min(0f), Tooltip("Small downward push that keeps the player glued to slopes and steps.")]
    private float _groundStickForce = 2f;
    [SerializeField, Min(0f)] private float _coyoteTime = 0.12f;
    [SerializeField, Min(0f)] private float _jumpBufferTime = 0.12f;
    [SerializeField, Min(0f)] private float _crouchTransitionSpeed = 8f;

    [Header("Camera")]
    [SerializeField] private CameraMode _cameraMode = CameraMode.ThirdPerson;
    [SerializeField, Tooltip("Point the camera orbits around, relative to the player's feet.")]
    private Vector3 _cameraPivot = new Vector3(0f, 1.6f, 0f);
    [SerializeField, Min(0.5f)] private float _thirdPersonDistance = 4.5f;
    [SerializeField] private float _shoulderOffset = 0.35f;
    [SerializeField, Min(0f)] private float _cameraCollisionRadius = 0.25f;
    [SerializeField] private LayerMask _cameraCollisionMask = ~0;
    [SerializeField, Min(0f), Tooltip("How quickly the camera moves back out after being pushed in by a wall.")]
    private float _cameraReturnSpeed = 6f;

    private static readonly RaycastHit[] CameraHits = new RaycastHit[8];

    private CharacterController _characterController;
    private Vector3 _horizontalVelocity;
    private float _verticalVelocity;
    private float _pitch;
    private float _lastGroundedTime = float.NegativeInfinity;
    private float _lastJumpPressedTime = float.NegativeInfinity;
    private float _currentCameraDistance;
    private Vector3 _headLocalPosition;
    private float _feetLocalY;
    private bool _canMove = true;

    /// <summary>World-space velocity of the player this frame.</summary>
    public Vector3 Velocity => _characterController != null ? _characterController.velocity : Vector3.zero;

    public bool IsGrounded => _characterController != null && _characterController.isGrounded;

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _feetLocalY = _characterController.center.y - _characterController.height * 0.5f;
        _currentCameraDistance = _thirdPersonDistance;

        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        _headLocalPosition = playerCamera != null ? playerCamera.transform.localPosition : _cameraPivot;

        DisableExtraChildPhysics();
        LockCursor(true);
    }

    void Update()
    {
        HandleCursor();
        if (_canMove) HandleLook();
        HandleMovement();
    }

    void LateUpdate()
    {
        UpdateCamera();
    }

    private void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);

        _canMove = Cursor.lockState == CursorLockMode.Locked;
    }

    private void HandleLook()
    {
        _pitch = Mathf.Clamp(_pitch - Input.GetAxis("Mouse Y") * lookSpeed, -lookXLimit, lookXLimit);
        transform.rotation *= Quaternion.Euler(0f, Input.GetAxis("Mouse X") * lookSpeed, 0f);
    }

    private void HandleMovement()
    {
        Vector2 input = _canMove
            ? Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f)
            : Vector2.zero;
        bool isCrouching = _canMove && Input.GetKey(KeyCode.R);
        bool isRunning = _canMove && Input.GetKey(KeyCode.LeftShift) && !isCrouching;
        float speed = isCrouching ? crouchSpeed : isRunning ? runSpeed : walkSpeed;

        bool grounded = _characterController.isGrounded;
        if (grounded) _lastGroundedTime = Time.time;
        if (_canMove && Input.GetButtonDown("Jump")) _lastJumpPressedTime = Time.time;

        Vector3 targetVelocity = (transform.right * input.x + transform.forward * input.y) * speed;
        float rate = input.sqrMagnitude > 0.01f ? _acceleration : _deceleration;
        if (!grounded) rate *= _airControl;
        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, rate * Time.deltaTime);

        UpdateVerticalVelocity(grounded, isCrouching);
        UpdateCrouch(isCrouching);

        Vector3 motion = (_horizontalVelocity + Vector3.up * _verticalVelocity) * Time.deltaTime;
        CollisionFlags flags = _characterController.Move(motion);
        if ((flags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f) _verticalVelocity = 0f;
    }

    private void UpdateVerticalVelocity(bool grounded, bool isCrouching)
    {
        if (grounded && _verticalVelocity < 0f) _verticalVelocity = -_groundStickForce;

        bool withinCoyoteTime = Time.time - _lastGroundedTime <= _coyoteTime;
        bool jumpBuffered = Time.time - _lastJumpPressedTime <= _jumpBufferTime;
        if (withinCoyoteTime && jumpBuffered && !isCrouching)
        {
            _verticalVelocity = jumpPower;
            _lastJumpPressedTime = float.NegativeInfinity;
            _lastGroundedTime = float.NegativeInfinity;
        }

        _verticalVelocity -= gravity * Time.deltaTime;
    }

    private void UpdateCrouch(bool isCrouching)
    {
        float targetHeight = isCrouching ? crouchHeight : defaultHeight;
        float height = Mathf.MoveTowards(_characterController.height, targetHeight, _crouchTransitionSpeed * Time.deltaTime);
        Vector3 center = _characterController.center;
        center.y = _feetLocalY + height * 0.5f;
        _characterController.height = height;
        _characterController.center = center;
    }

    private void UpdateCamera()
    {
        if (playerCamera == null) return;

        float crouchDrop = Mathf.Max(0f, defaultHeight - _characterController.height);
        Vector3 pivot = _headLocalPosition - Vector3.up * crouchDrop;
        Quaternion pitch = Quaternion.Euler(_pitch, 0f, 0f);
        Transform cam = playerCamera.transform;

        if (_cameraMode == CameraMode.FirstPerson)
        {
            cam.localPosition = pivot;
            cam.localRotation = pitch;
            return;
        }

        Vector3 localOffset = pitch * new Vector3(_shoulderOffset, 0f, -_thirdPersonDistance);
        Vector3 worldPivot = transform.TransformPoint(pivot);
        Vector3 worldDirection = transform.TransformDirection(localOffset.normalized);
        float targetDistance = ResolveCameraDistance(worldPivot, worldDirection, localOffset.magnitude);

        _currentCameraDistance = targetDistance < _currentCameraDistance
            ? targetDistance
            : Mathf.MoveTowards(_currentCameraDistance, targetDistance, _cameraReturnSpeed * Time.deltaTime);

        cam.localPosition = pivot + localOffset.normalized * _currentCameraDistance;
        cam.localRotation = pitch;
    }

    private float ResolveCameraDistance(Vector3 origin, Vector3 direction, float desired)
    {
        int count = Physics.SphereCastNonAlloc(origin, _cameraCollisionRadius, direction, CameraHits, desired,
            _cameraCollisionMask, QueryTriggerInteraction.Ignore);

        float nearest = desired;
        for (int i = 0; i < count; i++)
        {
            Transform hit = CameraHits[i].transform;
            if (hit == transform || hit.IsChildOf(transform)) continue;
            if (CameraHits[i].distance < nearest) nearest = CameraHits[i].distance;
        }
        return nearest < desired ? Mathf.Max(nearest - 0.05f, 0.3f) : desired;
    }

    // The old capsule visual has its own Rigidbody, collider and CharacterController, which fight this one.
    private void DisableExtraChildPhysics()
    {
        foreach (CharacterController controller in GetComponentsInChildren<CharacterController>(true))
            if (controller != _characterController) controller.enabled = false;

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
            if (col.gameObject != gameObject) col.enabled = false;

        foreach (Rigidbody body in GetComponentsInChildren<Rigidbody>(true))
            if (body.gameObject != gameObject) body.isKinematic = true;
    }

    private static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}

// test lol
