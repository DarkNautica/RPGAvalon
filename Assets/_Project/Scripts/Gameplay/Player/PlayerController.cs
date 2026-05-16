using UnityEngine;
using UnityEngine.InputSystem;
using PlayerInputActions = @PlayerInput;

namespace DarkNautica.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.5f;
        [SerializeField] private float runSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float rotationSpeed = 10f;

        private CharacterController _controller;
        private Animator _animator;
        private PlayerInputActions _input;
        private Transform _cameraTransform;

        private Vector2 _moveInput;
        private float _verticalVelocity;
        private bool _isSprinting;
        private bool _isJumping;
        private float _fallingDuration;
        private float _inputHeldTimer;

        // Synty animator parameter hashes
        private static readonly int AnimMoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int AnimCurrentGait = Animator.StringToHash("CurrentGait");
        private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int AnimIsJumping = Animator.StringToHash("IsJumping");
        private static readonly int AnimIsStopped = Animator.StringToHash("IsStopped");
        private static readonly int AnimIsWalking = Animator.StringToHash("IsWalking");
        private static readonly int AnimIsCrouching = Animator.StringToHash("IsCrouching");
        private static readonly int AnimIsTurningInPlace = Animator.StringToHash("IsTurningInPlace");
        private static readonly int AnimIsStrafing = Animator.StringToHash("IsStrafing");
        private static readonly int AnimMovementInputTapped = Animator.StringToHash("MovementInputTapped");
        private static readonly int AnimMovementInputPressed = Animator.StringToHash("MovementInputPressed");
        private static readonly int AnimMovementInputHeld = Animator.StringToHash("MovementInputHeld");
        private static readonly int AnimFallingDuration = Animator.StringToHash("FallingDuration");
        private static readonly int AnimForwardStrafe = Animator.StringToHash("ForwardStrafe");
        private static readonly int AnimStrafeDirectionX = Animator.StringToHash("StrafeDirectionX");
        private static readonly int AnimStrafeDirectionZ = Animator.StringToHash("StrafeDirectionZ");
        private static readonly int AnimInclineAngle = Animator.StringToHash("InclineAngle");
        private static readonly int AnimLocomotionStartDirection = Animator.StringToHash("LocomotionStartDirection");
        private static readonly int AnimLeanValue = Animator.StringToHash("LeanValue");

        // Input state tracking
        private bool _moveInputActive;
        private bool _moveInputPressedThisFrame;
        private bool _moveInputReleasedThisFrame;

        private const float TapThreshold = 0.15f;
        private const float HeldThreshold = 0.2f;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();
            _input = new PlayerInputActions();
            _cameraTransform = Camera.main.transform;
        }

        private void OnEnable()
        {
            _input.Player.Enable();

            _input.Player.Move.performed += OnMovePerformed;
            _input.Player.Move.canceled += OnMoveCanceled;
            _input.Player.Jump.performed += OnJump;
            _input.Player.Sprint.performed += OnSprintPerformed;
            _input.Player.Sprint.canceled += OnSprintCanceled;
        }

        private void OnDisable()
        {
            _input.Player.Move.performed -= OnMovePerformed;
            _input.Player.Move.canceled -= OnMoveCanceled;
            _input.Player.Jump.performed -= OnJump;
            _input.Player.Sprint.performed -= OnSprintPerformed;
            _input.Player.Sprint.canceled -= OnSprintCanceled;

            _input.Player.Disable();
        }

        private void OnDestroy()
        {
            _input?.Dispose();
        }

        private void Update()
        {
            bool isGrounded = _controller.isGrounded;
            bool hasInput = _moveInput.sqrMagnitude > 0.01f;

            // --- Gravity & falling ---
            if (isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
                _fallingDuration = 0f;

                if (_isJumping)
                    _isJumping = false;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
                if (!isGrounded)
                    _fallingDuration += Time.deltaTime;
            }

            // --- Movement input state tracking ---
            if (hasInput)
            {
                _inputHeldTimer += Time.deltaTime;
            }

            if (_moveInputReleasedThisFrame)
            {
                _moveInputReleasedThisFrame = false;
            }

            if (_moveInputPressedThisFrame)
            {
                _moveInputPressedThisFrame = false;
            }

            // --- Calculate movement direction relative to camera ---
            Vector3 forward = _cameraTransform.forward;
            Vector3 right = _cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = forward * _moveInput.y + right * _moveInput.x;

            // --- Determine gait and speed ---
            int currentGait;
            float currentSpeed;

            if (!hasInput)
            {
                currentGait = 0; // idle
                currentSpeed = 0f;
            }
            else if (_moveInput.magnitude < 0.5f)
            {
                currentGait = 1; // walk
                currentSpeed = walkSpeed;
            }
            else if (_isSprinting)
            {
                currentGait = 3; // sprint
                currentSpeed = runSpeed * sprintMultiplier;
            }
            else
            {
                currentGait = 2; // run
                currentSpeed = runSpeed;
            }

            // --- Apply movement ---
            Vector3 velocity = moveDirection * currentSpeed;
            velocity.y = _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);

            // --- Rotate character to face movement direction ---
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // --- Drive Synty animator parameters ---
            float horizontalSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;

            // MoveSpeed: actual horizontal speed for blend trees
            _animator.SetFloat(AnimMoveSpeed, horizontalSpeed, 0.1f, Time.deltaTime);

            // Gait
            _animator.SetInteger(AnimCurrentGait, currentGait);

            // Ground & jump state
            _animator.SetBool(AnimIsGrounded, isGrounded);
            _animator.SetBool(AnimIsJumping, _isJumping);
            _animator.SetFloat(AnimFallingDuration, _fallingDuration);

            // Movement state bools
            _animator.SetBool(AnimIsStopped, !hasInput);
            _animator.SetBool(AnimIsWalking, currentGait == 1);
            _animator.SetBool(AnimIsCrouching, false); // not implemented yet
            _animator.SetBool(AnimIsTurningInPlace, false); // not implemented yet

            // Input state (Synty uses these for start/stop transitions)
            _animator.SetBool(AnimMovementInputHeld, hasInput && _inputHeldTimer >= HeldThreshold);
            _animator.SetBool(AnimMovementInputPressed, hasInput);
            _animator.SetBool(AnimMovementInputTapped, !hasInput && _inputHeldTimer > 0f && _inputHeldTimer < TapThreshold);

            // Strafing: 0 for non-strafing (our default camera-relative mode)
            _animator.SetFloat(AnimIsStrafing, 0f);
            _animator.SetFloat(AnimForwardStrafe, 1f); // forward-facing locomotion
            _animator.SetFloat(AnimStrafeDirectionX, 0f);
            _animator.SetFloat(AnimStrafeDirectionZ, hasInput ? 1f : 0f);

            // Locomotion start direction (0 = forward)
            if (hasInput)
            {
                float angle = Mathf.Atan2(_moveInput.x, _moveInput.y) * Mathf.Rad2Deg;
                _animator.SetFloat(AnimLocomotionStartDirection, angle);
            }

            // Incline (flat ground for now)
            _animator.SetFloat(AnimInclineAngle, 0f);

            // Lean (none for now)
            _animator.SetFloat(AnimLeanValue, 0f);
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            Vector2 newInput = ctx.ReadValue<Vector2>();
            if (!_moveInputActive)
            {
                _moveInputPressedThisFrame = true;
                _inputHeldTimer = 0f;
            }
            _moveInput = newInput;
            _moveInputActive = true;
        }

        private void OnMoveCanceled(InputAction.CallbackContext ctx)
        {
            _moveInputReleasedThisFrame = true;
            _moveInputActive = false;
            _moveInput = Vector2.zero;
            _inputHeldTimer = 0f;
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (_controller.isGrounded)
            {
                _verticalVelocity = jumpForce;
                _isJumping = true;
            }
        }

        private void OnSprintPerformed(InputAction.CallbackContext ctx)
        {
            _isSprinting = true;
        }

        private void OnSprintCanceled(InputAction.CallbackContext ctx)
        {
            _isSprinting = false;
        }
    }
}
