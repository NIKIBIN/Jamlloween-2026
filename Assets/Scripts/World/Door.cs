using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Circus
{
    /// <summary>
    /// Hinged door. E opens/closes, hold Q locks (if lockable and the "portas trancáveis" flag is on).
    /// Closed+locked doors carve the NavMesh; the Zatan forces them or paths around.
    /// Unlocked doors open by themselves when the Zatan walks up to them.
    /// </summary>
    public class Door : MonoBehaviour, IInteractable, IHoldInteractable
    {
        public static readonly List<Door> All = new List<Door>();

        public string id;
        public bool lockable;
        [SerializeField] Transform panel;
        [SerializeField] NavMeshObstacle obstacle;
        [SerializeField] Renderer lockIndicator;
        [SerializeField] Material lockedMat, unlockedMat;
        [SerializeField] float openAngle = 100f;
        [SerializeField] float swingSpeed = 240f;
        [SerializeField] float noiseRadius = 6f;
        [SerializeField] float zatanAutoOpenDistance = 1.8f;

        public bool IsOpen { get; private set; }
        public bool IsLocked { get; private set; }
        public bool CanBeLocked => lockable && Tunables.I.lockableDoors;
        public Vector3 Center => panel.position;

        float angle;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Start() => Refresh();

        void Update()
        {
            if (IsLocked && !Tunables.I.lockableDoors) SetLocked(false);

            float target = IsOpen ? openAngle : 0f;
            angle = Mathf.MoveTowards(angle, target, swingSpeed * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0f, angle, 0f) * baseRot;

            if (!IsOpen && !IsLocked && ZatanLocator.IsActiveAgent && ZatanLocator.Transform != null
                && Vector3.Distance(ZatanLocator.Transform.position, Center) < zatanAutoOpenDistance)
                SetOpen(true, true);
        }

        Quaternion baseRot = Quaternion.identity;
        void Awake() => baseRot = transform.localRotation;

        public bool CanInteract => true;
        public string Prompt => IsLocked ? "TRANCADA" : IsOpen ? "FECHAR" : "ABRIR";

        public void Interact(Interactor who)
        {
            if (IsLocked) return;
            SetOpen(!IsOpen, false);
        }

        public bool CanHold => CanBeLocked && !IsOpen;
        public string HoldPrompt => IsLocked ? "DESTRANCAR" : "TRANCAR";
        public float HoldTime => Tunables.I.doorLockTime;
        public void HoldComplete(Interactor who) => SetLocked(!IsLocked);

        public void SetOpen(bool open, bool byZatan)
        {
            if (open && IsLocked) return;
            IsOpen = open;
            NoiseSystem.Emit(Center, noiseRadius, NoiseSource.Door, byZatan);
            Refresh();
        }

        public void SetLocked(bool locked)
        {
            if (locked && IsOpen) IsOpen = false;
            IsLocked = locked && lockable;
            Refresh();
        }

        /// <summary>Zatan broke through.</summary>
        public void Break()
        {
            IsLocked = false;
            SetOpen(true, true);
        }

        void Refresh()
        {
            if (obstacle) obstacle.enabled = IsLocked;
            if (lockIndicator) lockIndicator.sharedMaterial = IsLocked ? lockedMat : unlockedMat;
        }
    }
}
