using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float airControl = 0.5f;
    [SerializeField] private float movementSmoothTime = 0.1f;
    [SerializeField] private float rotationSmoothTime = 0.05f;

    [Header("Head Bob Settings")]
    [SerializeField] private float bobFrequency = 2f;
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float bobSmoothing = 10f;
    [SerializeField] private Transform cameraTransform;

    [Header("Camera Settings")]
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float lookSmoothTime = 0.03f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Camera Tilt Settings")]
    [SerializeField] private float maxTiltAngle = 2.0f;
    [SerializeField] private float tiltSpeed = 1.0f;
    [SerializeField] private float tiltReturnSpeed = 2.0f;

    [Header("Jump Camera Effects")]
    [SerializeField] private float jumpRiseEffect = 0.2f;      // Camera rises slightly at jump start
    [SerializeField] private float jumpFallEffect = 0.3f;      // Camera lowers during fall
    [SerializeField] private float jumpTiltAmount = 3.0f;      // Forward tilt during jump
    [SerializeField] private float jumpEffectSmoothing = 0.2f; // Smoothing for jump effects
    [SerializeField] private float landingBobAmount = 0.15f;   // Intensity of landing bob
    [SerializeField] private float landingBobDuration = 0.3f;  // Duration of landing effect
    [SerializeField] private float jumpSwayAmount = 0.1f;      // Horizontal sway during jump

    [Header("Collision Detection")]
    [SerializeField] private float collisionCheckDistance = 0.3f;  // Distance to check for collisions
    [SerializeField] private LayerMask collisionLayers = -1;       // Layers to check for collisions

    [Header("Footstep Sounds")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float footstepRate = 0.5f;
    [SerializeField] private float runFootstepRateMultiplier = 1.5f;
    [SerializeField] private float footstepVolumeMin = 0.5f;
    [SerializeField] private float footstepVolumeMax = 0.8f;
    [SerializeField] private Transform leftFoot;                   // Left foot transform for sound positioning
    [SerializeField] private Transform rightFoot;                  // Right foot transform for sound positioning

    [Header("Jump/Land Sounds")]
    [SerializeField] private AudioClip[] jumpSounds;
    [SerializeField] private AudioClip[] landSounds;               // Single set of landing sounds
    [SerializeField] private float landVolumeMin = 0.3f;           // Volume for small falls
    [SerializeField] private float landVolumeMax = 1.0f;           // Volume for big falls
    [SerializeField] private float landPitchMin = 0.8f;            // Lower pitch for hard landings
    [SerializeField] private float landPitchMax = 1.2f;            // Higher pitch for soft landings
    [SerializeField] private float mediumLandThreshold = 10f;      // Velocity threshold for medium landing
    [SerializeField] private float hardLandThreshold = 20f;        // Velocity threshold for hard landing

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private LayerMask interactionLayers;
    private bool _controlsActive = true;

    // Private variables
    private CharacterController _controller;
    private Vector3 _moveDirection = Vector3.zero;
    private Vector3 _currentVelocity = Vector3.zero;
    private Vector3 _smoothMoveVelocity = Vector3.zero;
    private float _currentSpeed;
    private float _defaultCameraY;
    private float _bobTimer = 0f;
    private float _currentBobAmount = 0f;
    private float _targetBobAmount = 0f;

    // Camera rotation and tilt
    private float _rotationX = 0f;
    private float _rotationY = 0f;
    private float _currentRotationX = 0f;
    private float _currentRotationY = 0f;
    private float _rotationXVelocity = 0f;
    private float _rotationYVelocity = 0f;
    private float _currentTiltAngle = 0f;
    private float _targetTiltAngle = 0f;
    private float _tiltVelocity = 0f;

    // Jump camera effects
    private float _jumpCameraOffset = 0f;
    private float _jumpCameraOffsetVelocity = 0f;
    private float _jumpTiltEffect = 0f;
    private float _jumpTiltVelocity = 0f;
    private float _landingBobTimer = 0f;
    private bool _isLanding = false;
    private float _jumpSway = 0f;
    private float _jumpSwayVelocity = 0f;
    private Vector3 _defaultCameraPos;
    private float _verticalVelocity = 0f;
    private float _previousVerticalVelocity = 0f;

    // Movement state
    private bool _isGrounded;
    private bool _wasGroundedLastFrame;
    private bool _isRunning;
    private bool _wasMovingLastFrame;
    private bool _isJumping = false;
    private bool _isFalling = false;
    private Vector2 _lastMoveInput;
    private Vector2 _smoothMoveInput;
    private Vector2 _smoothMoveInputVelocity;
    private bool _isCollidingWithWall = false;
    private Vector3 _lastPosition;
    private float _actualMovementMagnitude = 0f;

    // Sound timing
    private float _footstepPhase = 0f;
    private bool _isLeftFoot = true;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        _defaultCameraY = cameraTransform.localPosition.y;
        _defaultCameraPos = cameraTransform.localPosition;
        _lastPosition = transform.position;

        // Create foot transforms if not assigned
        if (leftFoot == null)
        {
            GameObject leftFootObj = new GameObject("LeftFoot");
            leftFoot = leftFootObj.transform;
            leftFoot.parent = transform;
            leftFoot.localPosition = new Vector3(-0.2f, 0.1f, 0);
        }

        if (rightFoot == null)
        {
            GameObject rightFootObj = new GameObject("RightFoot");
            rightFoot = rightFootObj.transform;
            rightFoot.parent = transform;
            rightFoot.localPosition = new Vector3(0.2f, 0.1f, 0);
        }

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleInput();
        HandleMovement();
        CheckWallCollision();
        HandleCameraRotation();
        HandleCameraTilt();
        HandleJumpCameraEffects();
        HandleHeadBob();
        HandleFootstepSounds();

        if (_controlsActive)
        {
            HandleInput();
            HandleMovement();
            CheckWallCollision();
        }

        HandleInteraction(); // Always check for interaction
        HandleCameraRotation();
        HandleCameraTilt();
        HandleJumpCameraEffects();
        HandleHeadBob();
        HandleFootstepSounds();

        _wasGroundedLastFrame = _isGrounded;
        _previousVerticalVelocity = _verticalVelocity;
        _lastPosition = transform.position;

        _wasGroundedLastFrame = _isGrounded;
        _previousVerticalVelocity = _verticalVelocity;
        _lastPosition = transform.position;
    }

    private void HandleInteraction()
    {
        if (!_controlsActive) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayers))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                interactable?.Interact(this);
            }
        }
    }

    public void SetControlsActive(bool active)
    {
        _controlsActive = active;

        // Optionally freeze movement when controls are disabled
        if (!active)
        {
            _moveDirection = Vector3.zero;
            _smoothMoveInput = Vector2.zero;
        }
    }

    private void HandleInput()
    {
        // Get input
        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // Normalize input if magnitude > 1
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        // Smooth input
        _smoothMoveInput = Vector2.SmoothDamp(_smoothMoveInput, moveInput, ref _smoothMoveInputVelocity, movementSmoothTime);

        // Check if running
        _isRunning = Input.GetKey(KeyCode.LeftShift);

        // Jump
        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _moveDirection.y = jumpForce;
            _isJumping = true;

            // Play jump sound
            if (jumpSounds != null && jumpSounds.Length > 0)
            {
                // Play jump sound at player position (slightly elevated)
                Vector3 jumpSoundPos = transform.position + new Vector3(0, 0.8f, 0);
                AudioClip jumpClip = jumpSounds[Random.Range(0, jumpSounds.Length)];
                AudioSource.PlayClipAtPoint(jumpClip, jumpSoundPos, Random.Range(footstepVolumeMin, footstepVolumeMax));
            }
        }

        // Mouse input for camera rotation
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        _rotationY += mouseX;
        _rotationX -= mouseY;
        _rotationX = Mathf.Clamp(_rotationX, -maxLookAngle, maxLookAngle);
    }

    private void HandleMovement()
    {
        _isGrounded = _controller.isGrounded;
        _verticalVelocity = _moveDirection.y;

        // Detect if we're falling
        if (!_isGrounded && _verticalVelocity < 0)
        {
            _isFalling = true;
        }
        else if (_isGrounded)
        {
            _isFalling = false;
        }

        // Set target speed based on input and running state
        float targetSpeed = (_isRunning ? runSpeed : walkSpeed) * _smoothMoveInput.magnitude;
        _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _smoothMoveVelocity.x, movementSmoothTime);

        // Calculate movement direction
        Vector3 moveDir = transform.forward * _smoothMoveInput.y + transform.right * _smoothMoveInput.x;

        // Apply movement
        if (_isGrounded)
        {
            _moveDirection.x = moveDir.x * _currentSpeed;
            _moveDirection.z = moveDir.z * _currentSpeed;

            // Reset y velocity when grounded
            if (_moveDirection.y < 0)
                _moveDirection.y = -2f;

            // Handle landing
            if (!_wasGroundedLastFrame)
            {
                _isLanding = true;
                _landingBobTimer = 0f;
                _isJumping = false;

                // Play landing sound based on fall velocity
                PlayLandingSound();
            }
        }
        else
        {
            // Apply air control (reduced control while in air)
            _moveDirection.x = Mathf.Lerp(_moveDirection.x, moveDir.x * _currentSpeed, airControl * Time.deltaTime);
            _moveDirection.z = Mathf.Lerp(_moveDirection.z, moveDir.z * _currentSpeed, airControl * Time.deltaTime);
        }

        // Apply gravity
        _moveDirection.y -= gravity * Time.deltaTime;

        // Move the controller
        _controller.Move(_moveDirection * Time.deltaTime);

        // Track movement state for transitions
        _wasMovingLastFrame = _smoothMoveInput.magnitude > 0.1f;
        _lastMoveInput = _smoothMoveInput;
    }
    private void PlayLandingSound()
    {
        // Calculate fall impact based on negative vertical velocity
        float fallImpact = Mathf.Abs(_previousVerticalVelocity);

        // Skip very small falls (less than 10% of jump force)
        if (fallImpact < jumpForce * 0.1f || landSounds == null || landSounds.Length == 0)
            return;

        // Calculate normalized impact (0-1 range)
        float normalizedImpact = Mathf.Clamp01(fallImpact / hardLandThreshold);

        // Calculate volume based on fall impact (higher impact = higher volume)
        float volume = Mathf.Lerp(landVolumeMin, landVolumeMax, normalizedImpact);

        // Calculate pitch based on fall impact (higher impact = lower pitch)
        float pitch = Mathf.Lerp(landPitchMax, landPitchMin, normalizedImpact);

        // Add slight randomization
        volume = Mathf.Clamp(volume * Random.Range(0.9f, 1.1f), 0.1f, 1.0f);

        // Get landing position (at feet level)
        Vector3 landPos = transform.position + new Vector3(0, 0.1f, 0);

        // Play the landing sound
        AudioClip landClip = landSounds[Random.Range(0, landSounds.Length)];
        AudioSource.PlayClipAtPoint(landClip, landPos, volume);

        // For very hard falls, play an additional impact sound with slight delay
        if (fallImpact >= hardLandThreshold * 0.8f)
        {
            StartCoroutine(PlayDelayedLandSound(landPos, volume * 0.7f, 0.05f));
        }
    }

    private IEnumerator PlayDelayedLandSound(Vector3 position, float volume, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (landSounds != null && landSounds.Length > 0)
        {
            AudioClip landClip = landSounds[Random.Range(0, landSounds.Length)];
            AudioSource.PlayClipAtPoint(landClip, position, volume);
        }
    }

    private void CheckWallCollision()
    {
        // Calculate actual movement magnitude (horizontal only)
        Vector3 horizontalMovement = new Vector3(transform.position.x - _lastPosition.x, 0, transform.position.z - _lastPosition.z);
        _actualMovementMagnitude = horizontalMovement.magnitude / Time.deltaTime;

        // Check if we're trying to move but not actually moving much (colliding with wall)
        bool tryingToMove = _smoothMoveInput.magnitude > 0.1f;
        bool barelyMoving = _actualMovementMagnitude < (_isRunning ? runSpeed : walkSpeed) * 0.3f;

        // Additional raycast check for walls
        bool hitWall = false;
        if (tryingToMove)
        {
            // Direction we're trying to move
            Vector3 moveDir = transform.forward * _smoothMoveInput.y + transform.right * _smoothMoveInput.x;
            moveDir.Normalize();

            // Cast a ray to see if there's a wall in front of us
            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, moveDir);
            hitWall = Physics.Raycast(ray, collisionCheckDistance, collisionLayers);

            // Draw debug ray
            Debug.DrawRay(ray.origin, ray.direction * collisionCheckDistance, hitWall ? Color.red : Color.green);
        }

        // We're colliding with a wall if we're trying to move but barely moving or hit a wall with raycast
        _isCollidingWithWall = tryingToMove && (barelyMoving || hitWall);
    }

    private void HandleCameraRotation()
    {
        // Smooth camera rotation
        _currentRotationX = Mathf.SmoothDamp(_currentRotationX, _rotationX, ref _rotationXVelocity, lookSmoothTime);
        _currentRotationY = Mathf.SmoothDamp(_currentRotationY, _rotationY, ref _rotationYVelocity, rotationSmoothTime);

        // Apply rotation to player
        transform.rotation = Quaternion.Euler(0, _currentRotationY, 0);

        // Camera rotation is applied in HandleJumpCameraEffects to include all effects
    }

    private void HandleCameraTilt()
    {
        // Calculate target tilt based on strafe movement
        if (_isGrounded && Mathf.Abs(_smoothMoveInput.x) > 0.1f && !_isCollidingWithWall)
        {
            _targetTiltAngle = -_smoothMoveInput.x * maxTiltAngle;
        }
        else
        {
            _targetTiltAngle = 0f;
        }

        // Smooth the tilt
        float tiltSmoothTime = _targetTiltAngle == 0 ? 1.0f / tiltReturnSpeed : 1.0f / tiltSpeed;
        _currentTiltAngle = Mathf.SmoothDamp(_currentTiltAngle, _targetTiltAngle, ref _tiltVelocity, tiltSmoothTime);

        // Actual tilt application is done in HandleJumpCameraEffects
    }

    private void HandleJumpCameraEffects()
    {
        float targetJumpOffset = 0f;
        float targetJumpTilt = 0f;
        float targetJumpSway = 0f;

        // Jump rise effect (camera moves up slightly at jump start)
        if (_isJumping && _verticalVelocity > 0)
        {
            targetJumpOffset = jumpRiseEffect;
            targetJumpTilt = -jumpTiltAmount * 0.5f; // Tilt back slightly on jump
            targetJumpSway = (_lastMoveInput.x != 0) ? _lastMoveInput.x * jumpSwayAmount : 0;
        }
        // Fall effect (camera lowers during fall)
        else if (_isFalling)
        {
            targetJumpOffset = -jumpFallEffect * Mathf.Clamp01(-_verticalVelocity / jumpForce);
            targetJumpTilt = jumpTiltAmount; // Tilt forward during fall
            targetJumpSway = (_lastMoveInput.x != 0) ? _lastMoveInput.x * jumpSwayAmount * 0.5f : 0;
        }

        // Smooth the jump camera effects
        _jumpCameraOffset = Mathf.SmoothDamp(_jumpCameraOffset, targetJumpOffset, ref _jumpCameraOffsetVelocity, jumpEffectSmoothing);
        _jumpTiltEffect = Mathf.SmoothDamp(_jumpTiltEffect, targetJumpTilt, ref _jumpTiltVelocity, jumpEffectSmoothing);
        _jumpSway = Mathf.SmoothDamp(_jumpSway, targetJumpSway, ref _jumpSwayVelocity, jumpEffectSmoothing);

        // Handle landing effect (camera dips down and comes back up)
        if (_isLanding)
        {
            _landingBobTimer += Time.deltaTime;
            if (_landingBobTimer >= landingBobDuration)
            {
                _isLanding = false;
            }

            // Create a landing bob curve (sharp down, then back up)
            float landingCurve = 0;
            if (_landingBobTimer < landingBobDuration * 0.3f)
            {
                // Quick drop (first 30% of the effect)
                landingCurve = -Mathf.Lerp(0, landingBobAmount, _landingBobTimer / (landingBobDuration * 0.3f));
            }
            else
            {
                // Gradual rise back (remaining 70%)
                landingCurve = -Mathf.Lerp(landingBobAmount, 0,
                    (_landingBobTimer - landingBobDuration * 0.3f) / (landingBobDuration * 0.7f));
            }

            // Scale landing effect based on fall impact
            float fallImpact = Mathf.Abs(_previousVerticalVelocity);
            float impactMultiplier = Mathf.Clamp01(fallImpact / hardLandThreshold);
            landingCurve *= Mathf.Lerp(0.5f, 1.5f, impactMultiplier);

            _jumpCameraOffset += landingCurve;
        }

        // Apply all camera effects
        Vector3 finalCameraPos = _defaultCameraPos;
        finalCameraPos.y += _jumpCameraOffset + _currentBobAmount;
        finalCameraPos.x += _jumpSway;
        cameraTransform.localPosition = finalCameraPos;

        // Apply rotation with all effects combined
        Quaternion finalRotation = Quaternion.Euler(
            _currentRotationX + _jumpTiltEffect,
            0,
            _currentTiltAngle
        );
        cameraTransform.localRotation = finalRotation;
    }


    private void HandleHeadBob()
    {
        // Don't bob when in air or colliding with wall
        if (!_isGrounded || _isCollidingWithWall)
        {
            // Gradually reset bob when not applicable
            _targetBobAmount = 0;
            _currentBobAmount = Mathf.Lerp(_currentBobAmount, 0, Time.deltaTime * bobSmoothing);
            return;
        }

        if (_smoothMoveInput.magnitude > 0.1f && _actualMovementMagnitude > 0.5f)
        {
            // Increment bob timer based on speed
            _bobTimer += Time.deltaTime * (_isRunning ? bobFrequency * 1.5f : bobFrequency);

            // Calculate target bob amount
            _targetBobAmount = Mathf.Sin(_bobTimer) * (_isRunning ? bobAmplitude * 1.5f : bobAmplitude);
        }
        else
        {
            // Reset bob when not moving
            _bobTimer = 0;
            _targetBobAmount = 0;
        }

        // Smooth bob transition
        _currentBobAmount = Mathf.Lerp(_currentBobAmount, _targetBobAmount, Time.deltaTime * bobSmoothing);
    }

    private void HandleFootstepSounds()
    {
        // Don't play footsteps when in air, colliding with wall, or not moving enough
        if (!_isGrounded || _isCollidingWithWall || _actualMovementMagnitude < 0.5f ||
            footstepSounds == null || footstepSounds.Length == 0)
        {
            return;
        }

        // Only play footsteps when moving and grounded
        if (_smoothMoveInput.magnitude > 0.1f)
        {
            // Update footstep phase based on movement
            float stepFrequency = _isRunning ? footstepRate * runFootstepRateMultiplier : footstepRate;
            _footstepPhase += Time.deltaTime * stepFrequency;

            // Check if it's time for a footstep
            if (_footstepPhase >= 1.0f)
            {
                // Reset phase but keep remainder for more accurate timing
                _footstepPhase -= 1.0f;

                // Get the position of the appropriate foot
                Transform footTransform = _isLeftFoot ? leftFoot : rightFoot;
                Vector3 footPosition = footTransform.position;

                // Select random footstep sound
                AudioClip footstepSound = footstepSounds[Random.Range(0, footstepSounds.Length)];

                // Play the sound at the foot position
                float volume = Random.Range(footstepVolumeMin, footstepVolumeMax);
                AudioSource.PlayClipAtPoint(footstepSound, footPosition, volume);

                // Alternate feet
                _isLeftFoot = !_isLeftFoot;
            }
        }
        else
        {
            // Reset phase when not moving
            _footstepPhase = 0f;
        }
    }
}