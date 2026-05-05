using Project.Input;
using UnityEngine;

namespace Project.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private PlayerStats stats;

        [Header("References")]
        [SerializeField] private Transform cameraTarget;

        private CharacterController characterController;
        private PlayerInputHandler input;

        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float cameraPitch;

        private bool isSprinting;
        private bool jumpWasCut;
        private bool canMove = true;

        private float lastGroundedTime = -999f;
        private float lastJumpPressedTime = -999f;

        public Transform CameraTarget => cameraTarget;
        public bool CanMove => canMove;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputHandler>();

            ValidateReferences();
        }

        private void OnEnable()
        {
            input.JumpPressed += HandleJumpPressed;
            input.JumpReleased += HandleJumpReleased;
            input.SprintStarted += HandleSprintStarted;
            input.SprintEnded += HandleSprintEnded;
        }

        private void OnDisable()
        {
            input.JumpPressed -= HandleJumpPressed;
            input.JumpReleased -= HandleJumpReleased;
            input.SprintStarted -= HandleSprintStarted;
            input.SprintEnded -= HandleSprintEnded;
        }

        private void Update()
        {
            if (!canMove)
            {
                return;
            }

            HandleLook();
            HandleMovement();
        }

        public void SetStats(PlayerStats newStats)
        {
            stats = newStats;
        }

        public void SetCanMove(bool value)
        {
            canMove = value;

            if (canMove)
            {
                return;
            }

            horizontalVelocity = Vector3.zero;
            isSprinting = false;
            lastJumpPressedTime = -999f;
        }

        private void HandleLook()
        {
            if (stats == null || cameraTarget == null)
            {
                return;
            }

            Vector2 lookInput = input.LookInput;

            float yaw = lookInput.x * stats.MouseSensitivity;
            float pitch = lookInput.y * stats.MouseSensitivity;

            transform.Rotate(Vector3.up * yaw);

            cameraPitch -= pitch;
            cameraPitch = Mathf.Clamp(cameraPitch, stats.MinPitch, stats.MaxPitch);

            cameraTarget.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            if (stats == null)
            {
                return;
            }

            bool isGrounded = characterController.isGrounded;

            UpdateGroundedTimer(isGrounded);
            TryConsumeBufferedJump();

            HandleHorizontalMovement(isGrounded);
            HandleVerticalMovement(isGrounded);

            Vector3 finalVelocity =
                horizontalVelocity +
                Vector3.up * verticalVelocity;

            characterController.Move(finalVelocity * Time.deltaTime);
        }

        private void UpdateGroundedTimer(bool isGrounded)
        {
            if (isGrounded)
            {
                lastGroundedTime = Time.time;
                jumpWasCut = false;
            }
        }

        private void TryConsumeBufferedJump()
        {
            bool hasBufferedJump =
                Time.time - lastJumpPressedTime <= stats.JumpBufferTime;

            bool canUseCoyoteJump =
                Time.time - lastGroundedTime <= stats.CoyoteTime;

            if (!hasBufferedJump || !canUseCoyoteJump)
            {
                return;
            }

            ExecuteJump();

            lastJumpPressedTime = -999f;
            lastGroundedTime = -999f;
        }

        private void HandleHorizontalMovement(bool isGrounded)
        {
            Vector2 moveInput = input.MoveInput;

            Vector3 moveDirection =
                transform.right * moveInput.x +
                transform.forward * moveInput.y;

            moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

            float targetSpeed = isSprinting
                ? stats.SprintSpeed
                : stats.WalkSpeed;

            Vector3 targetVelocity = moveDirection * targetSpeed;

            float currentAcceleration = isGrounded
                ? stats.Acceleration
                : stats.AirAcceleration;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                currentAcceleration * Time.deltaTime
            );
        }

        private void HandleVerticalMovement(bool isGrounded)
        {
            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = stats.GroundedGravity;
            }

            float gravityMultiplier = GetGravityMultiplier();
            verticalVelocity += stats.Gravity * gravityMultiplier * Time.deltaTime;
        }

        private float GetGravityMultiplier()
        {
            if (verticalVelocity < 0f)
            {
                return stats.FallGravityMultiplier;
            }

            bool isNearJumpApex =
                Mathf.Abs(verticalVelocity) <= stats.ApexVelocityThreshold;

            if (isNearJumpApex && !jumpWasCut)
            {
                return stats.ApexGravityMultiplier;
            }

            return 1f;
        }

        private void HandleJumpPressed()
        {
            if (!canMove)
            {
                return;
            }

            lastJumpPressedTime = Time.time;
        }

        private void HandleJumpReleased()
        {
            if (!canMove)
            {
                return;
            }

            if (verticalVelocity <= 0f)
            {
                return;
            }

            verticalVelocity *= stats.JumpCutMultiplier;
            jumpWasCut = true;
        }

        private void ExecuteJump()
        {
            verticalVelocity = Mathf.Sqrt(stats.JumpHeight * -2f * stats.Gravity);
            jumpWasCut = false;
        }

        private void HandleSprintStarted()
        {
            if (!canMove)
            {
                return;
            }

            isSprinting = true;
        }

        private void HandleSprintEnded()
        {
            isSprinting = false;
        }

        private void ValidateReferences()
        {
            if (stats == null)
            {
                Debug.LogError($"{nameof(FirstPersonController)} on {name} is missing PlayerStats.");
            }

            if (cameraTarget == null)
            {
                Debug.LogError($"{nameof(FirstPersonController)} on {name} is missing Camera Target.");
            }
        }
    }
}