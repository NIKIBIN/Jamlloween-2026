using Spike2.Core;
using UnityEngine;

namespace Spike2.Player
{
    /// <summary>Player, interaction, pocket and bottles (SPIKE2.md 6, 5.6, 5.8). Seeded in wave 0; owned by agent B.</summary>
    [CreateAssetMenu(menuName = "Spike2/Tunables/Player", fileName = "PlayerTunables")]
    public class PlayerTunables : TunablesBase
    {
        public override string Tab => "Player";

        [Header("Movimento")]
        [Range(1f, 8f)] public float walkSpeed = 4f;
        [Range(1f, 10f)] public float runSpeed = 6f;
        [Range(0.5f, 4f)] public float crouchSpeed = 1.6f;
        [Range(0.02f, 0.4f)] public float mouseSensitivity = 0.12f;
        public float gravity = -20f;

        [Header("Barulho")]
        [Range(0f, 30f)] public float runNoiseRadius = 12f;
        [Range(0f, 10f)] public float walkNoiseRadius = 4f;
        [Range(0f, 5f)] public float crouchNoiseRadius = 0f;
        [Range(0.1f, 1f)] public float footstepNoiseInterval = 0.35f;

        [Header("Lanterna")]
        [Range(5f, 40f)] public float flashlightRange = 18f;
        [Range(0.5f, 6f)] public float flashlightIntensity = 2.5f;
        [Range(20f, 90f)] public float flashlightAngle = 55f;

        [Header("Interação")]
        [Range(0.05f, 0.6f)] public float interactSphereRadius = 0.25f;
        [Range(1f, 4f)] public float interactDistance = 2.2f;
        [Range(0.3f, 3f)] public float doorLockHoldTime = 1f;

        [Header("Prancheta")]
        [Range(0.05f, 1f)] public float clipboardRaiseTime = 0.25f;

        [Header("Garrafas")]
        [Range(1, 4)] public int maxBottles = 2;
        [Range(5f, 30f)] public float bottleRange = 18f;
        [Range(2f, 30f)] public float bottleNoiseRadius = 15f;
    }
}
