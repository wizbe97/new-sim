using UnityEngine;

namespace Project.Player
{
    [CreateAssetMenu(
        fileName = "PlayerStats",
        menuName = "Project/Player/Player Stats"
    )]
    public sealed class PlayerStats : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 4.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 7.5f;
        [SerializeField, Min(0f)] private float acceleration = 20f;
        [SerializeField, Min(0f)] private float airAcceleration = 8f;

        [Header("Jumping")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.25f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        [Tooltip("When jump is released early while moving upward, vertical velocity is multiplied by this value.")]
        [SerializeField, Range(0.1f, 1f)] private float jumpCutMultiplier = 0.45f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float groundedGravity = -2f;

        [Tooltip("Gravity multiplier while falling. Higher values make the player fall faster.")]
        [SerializeField, Min(1f)] private float fallGravityMultiplier = 1.6f;

        [Tooltip("Gravity multiplier near the top of the jump. Lower values add a small apex hang.")]
        [SerializeField, Range(0.1f, 1f)] private float apexGravityMultiplier = 0.55f;

        [Tooltip("Vertical speed range around zero where apex gravity is used.")]
        [SerializeField, Min(0f)] private float apexVelocityThreshold = 1.5f;

        [Header("Look")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;

        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float Acceleration => acceleration;
        public float AirAcceleration => airAcceleration;

        public float JumpHeight => jumpHeight;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public float JumpCutMultiplier => jumpCutMultiplier;

        public float Gravity => gravity;
        public float GroundedGravity => groundedGravity;
        public float FallGravityMultiplier => fallGravityMultiplier;
        public float ApexGravityMultiplier => apexGravityMultiplier;
        public float ApexVelocityThreshold => apexVelocityThreshold;

        public float MouseSensitivity => mouseSensitivity;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
    }
}